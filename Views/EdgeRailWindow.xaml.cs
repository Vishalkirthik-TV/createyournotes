using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using HoldMyNotes.Data;
using HoldMyNotes.Models;
using HoldMyNotes.Views;

namespace HoldMyNotes
{
    public partial class EdgeRailWindow : Window
    {
        // ============================================================
        // GEOMETRY
        // ============================================================

        private const double WindowWidth = 220;
        private const double WindowHeight = 620;

        // Everything that touches the right side uses this edge.
        private const double RightEdge = WindowWidth;

        // Resting rail.
        private const double RailWidth = 14;
        private const double RailLeft = RightEdge - RailWidth;
        private const double RailTop = 258;
        private const double RailHeight = 104;

        // Resting pills.
        private const double PillWidth = 7;
        private const double PillHeight = 22;
        private const double PillGap = 2;

        // Fan-out tabs.
        private const double FanTabWidth = 48;
        private const double FanTabHeight = 58;
        private const double FanTop = 58;
        private const double FanGap = 2;

        // Expanded note.
        private const double ExpandedWidth = 155;
        private const double ExpandedHeight = 128;
        private const double ExpandedLeft = RightEdge - ExpandedWidth;

        // Add button.
        private const double AddButtonSize = 30;
        private const double AddButtonLeft = RightEdge - AddButtonSize;
        private const double AddButtonTop = 530;

        // Timing.
        private const int FanDurationMs = 210;
        private const int FanStaggerMs = 45;
        private const int HoverDurationMs = 190;
        private const int FadeDurationMs = 120;
        private const int CollapseDelayMs = 170;

        // ============================================================
        // DATA
        // ============================================================

        private readonly NoteRepository _noteRepository;

        private List<Note> _notes = new();

        // ============================================================
        // STATE
        // ============================================================

        private bool _isOpen;
        private bool _isAnimating;

        private Border? _expandedTab;

        private DispatcherTimer _collapseTimer;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public EdgeRailWindow()
        {
            InitializeComponent();

            _noteRepository = new NoteRepository();

            _collapseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(CollapseDelayMs)
            };

            _collapseTimer.Tick += CollapseTimer_Tick;

            AddButton.Click += AddButton_Click;

            LoadNotes();
            BuildIndicators();
            SetRestingState();

            PositionWindow();
        }

        // ============================================================
        // POSITION
        // ============================================================

        private void PositionWindow()
        {
            Left = SystemParameters.WorkArea.Right - WindowWidth;
            Top = (SystemParameters.WorkArea.Height - WindowHeight) / 2.0;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            PositionWindow();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            PositionWindow();
        }

        // ============================================================
        // PUBLIC API USED BY MainWindow
        // ============================================================

        public void Open()
        {
            if (_isOpen || _isAnimating)
                return;

            _collapseTimer.Stop();
            _isOpen = true;

            FanOutTabs();
        }

        public void CloseRail()
        {
            if (!_isOpen || _isAnimating)
                return;

            _isOpen = false;
            _expandedTab = null;

            CollapseTabs();
        }

        // ============================================================
        // SCREEN EDGE
        // ============================================================

        private static bool IsCursorOnPhysicalScreenEdge()
        {
            if (!GetCursorPos(out POINT cursor))
                return false;

            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int right = left + width;

            // A physical edge pixel is NOT a note-card hover target.
            return cursor.X >= right - 1;
        }

        // ============================================================
        // HIT TEST
        // ============================================================

        public bool IsMouseInsideVisibleArea(int screenX, int screenY)
        {
            // Do not use the literal screen edge as a card/rail hover
            // target. MainWindow can still use its own edge trigger
            // to open the rail.
            if (IsAtVirtualScreenEdge(screenX))
                return false;

            Point point = PointFromScreen(
                new Point(screenX, screenY)
            );

            // Resting rail.
            if (!_isOpen)
            {
                Rect railRect = new Rect(
                    RailLeft,
                    RailTop,
                    RailWidth,
                    RailHeight
                );

                return railRect.Contains(point);
            }

            // Add button.
            if (AddButton.Visibility == Visibility.Visible)
            {
                Rect addRect = new Rect(
                    AddButtonLeft,
                    AddButtonTop,
                    AddButtonSize,
                    AddButtonSize
                );

                if (addRect.Contains(point))
                    return true;
            }

            // Actual visible tabs, including an expanded tab.
            foreach (UIElement element in TabCanvas.Children)
            {
                if (element is not Border tab ||
                    tab.Visibility != Visibility.Visible)
                    continue;

                double width = tab.ActualWidth > 0
                    ? tab.ActualWidth
                    : FanTabWidth;

                double height = tab.ActualHeight > 0
                    ? tab.ActualHeight
                    : FanTabHeight;

                double left = Canvas.GetLeft(tab);
                double top = Canvas.GetTop(tab);

                Rect rect = new Rect(
                    left,
                    top,
                    width,
                    height
                );

                if (rect.Contains(point))
                    return true;
            }

            // The rail itself is still a valid hover area while open.
            Rect openRailRect = new Rect(
                RailLeft,
                RailTop,
                RailWidth,
                RailHeight
            );

            return openRailRect.Contains(point);
        }

        private static bool IsAtVirtualScreenEdge(int screenX)
        {
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int right = left + width;

            return screenX >= right - 1;
        }

        // ============================================================
        // DATABASE
        // ============================================================

        private void LoadNotes()
        {
            _notes = _noteRepository.GetAllNotes();
        }

        private void ReloadNotes()
        {
            LoadNotes();
            BuildIndicators();

            _expandedTab = null;
            TabCanvas.Children.Clear();

            if (_isOpen)
            {
                FanOutTabs();
            }
            else
            {
                SetRestingState();
            }
        }

        // ============================================================
        // RESTING INDICATORS
        // ============================================================

        private void BuildIndicators()
        {
            IndicatorStack.Children.Clear();

            foreach (Note note in _notes)
            {
                Border pill = new Border
                {
                    Width = PillWidth,
                    Height = PillHeight,
                    CornerRadius = new CornerRadius(4),
                    Background = CreateBrush(note.Color),
                    Margin = new Thickness(
                        0,
                        PillGap,
                        0,
                        PillGap
                    ),
                    Tag = note.Id
                };

                IndicatorStack.Children.Add(pill);
            }

            double requiredHeight =
                Math.Max(
                    RailHeight,
                    12 +
                    _notes.Count *
                    (PillHeight + PillGap * 2)
                );

            Rail.Height = requiredHeight;

            double centeredTop =
                RailTop +
                (requiredHeight - 8 -
                 (_notes.Count *
                  (PillHeight + PillGap * 2))) / 2;

            Canvas.SetTop(
                IndicatorStack,
                Math.Max(
                    RailTop + 5,
                    centeredTop
                )
            );
        }

        // ============================================================
        // FAN OUT
        // ============================================================

        private void FanOutTabs()
        {
            _isAnimating = true;

            _expandedTab = null;

            TabCanvas.Children.Clear();

            HideRestingRail();

            for (int i = 0; i < _notes.Count; i++)
            {
                Border tab = CreateNoteTab(_notes[i], i);

                // All fan-out tabs touch the right screen edge.
                Canvas.SetLeft(
                    tab,
                    RightEdge - FanTabWidth
                );

                Canvas.SetTop(
                    tab,
                    FanTop +
                    i * (FanTabHeight + FanGap)
                );

                Panel.SetZIndex(
                    tab,
                    200 + i
                );

                tab.Opacity = 0;

                TabCanvas.Children.Add(tab);

                AnimateFanIn(
                    tab,
                    i
                );
            }

            ShowAddButton();

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(
                    FanDurationMs +
                    (_notes.Count * FanStaggerMs) +
                    20
                )
            };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                _isAnimating = false;
            };

            timer.Start();
        }

        private void AnimateFanIn(
            Border tab,
            int index)
        {
            DoubleAnimation opacity = new DoubleAnimation
            {
                From = 0,
                To = 1,
                BeginTime = TimeSpan.FromMilliseconds(
                    index * FanStaggerMs
                ),
                Duration = TimeSpan.FromMilliseconds(110),
                EasingFunction = EaseOut()
            };

            tab.BeginAnimation(
                UIElement.OpacityProperty,
                opacity
            );
        }

        // ============================================================
        // CREATE TAB
        // ============================================================

        private Border CreateNoteTab(
            Note note,
            int index)
        {
            Border tab = new Border
            {
                Width = FanTabWidth,
                Height = FanTabHeight,

                Background =
                    CreateBrush(note.Color),

                CornerRadius =
                    new CornerRadius(
                        14,
                        0,
                        0,
                        14
                    ),

                Tag = note.Id,

                Cursor = Cursors.Hand,

                SnapsToDevicePixels = true
            };

            tab.Effect =
                new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 5,
                    ShadowDepth = 1,
                    Opacity = 0.13
                };

            Grid content = new Grid();

            // --------------------------------------------------------
            // FAN-OUT LABEL
            // --------------------------------------------------------

            TextBlock label = new TextBlock
            {
                Text = note.Title.ToUpperInvariant(),

                FontFamily =
                    new FontFamily("Segoe UI"),

                FontSize = 9,

                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            78,
                            78,
                            78
                        )
                    ),

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                VerticalAlignment =
                    VerticalAlignment.Center,

                TextAlignment =
                    TextAlignment.Center,

                TextTrimming =
                    TextTrimming.CharacterEllipsis
            };

            label.LayoutTransform =
                new RotateTransform(90);

            content.Children.Add(label);

            // --------------------------------------------------------
            // EXPANDED CONTENT
            // --------------------------------------------------------

            Grid preview =
                CreateExpandedPreview(note);

            preview.Visibility =
                Visibility.Collapsed;

            preview.Opacity = 0;

            content.Children.Add(preview);

            tab.Child = content;

            // --------------------------------------------------------
            // HOVER
            // --------------------------------------------------------

            tab.MouseEnter += (_, _) =>
            {
                TryExpandFromPointer(
                    tab,
                    label,
                    preview
                );
            };

            /*
             * MouseMove is intentional here.
             *
             * If the cursor enters the tab while it is sitting on the
             * physical screen edge, MouseEnter fires while the edge guard
             * rejects the expansion. Moving one pixel inward does not fire
             * MouseEnter again because the cursor is still inside the same
             * Border. MouseMove closes that WPF event gap.
             */
            tab.MouseMove += (_, _) =>
            {
                TryExpandFromPointer(
                    tab,
                    label,
                    preview
                );
            };

            tab.MouseLeave += (_, _) =>
            {
                if (!_isOpen)
                    return;

                CollapseTab(
                    tab,
                    label,
                    preview
                );
            };

            // --------------------------------------------------------
            // CLICK
            // --------------------------------------------------------

            tab.MouseLeftButtonUp +=
                NoteTab_Click;

            return tab;
        }

        // ============================================================
        // EXPANDED NOTE UI
        // ============================================================

        private Grid CreateExpandedPreview(
            Note note)
        {
            Grid preview = new Grid
            {
                Margin =
                    new Thickness(
                        16,
                        12,
                        14,
                        12
                    )
            };

            preview.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        GridLength.Auto
                }
            );

            preview.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            1,
                            GridUnitType.Star
                        )
                }
            );

            TextBlock title = new TextBlock
            {
                Text = note.Title,

                FontFamily =
                    new FontFamily("Segoe UI"),

                FontSize = 15,

                FontWeight =
                    FontWeights.Medium,

                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            42,
                            42,
                            42
                        )
                    ),

                TextTrimming =
                    TextTrimming.CharacterEllipsis
            };

            Grid.SetRow(title, 0);

            preview.Children.Add(title);

            Grid bodyArea = new Grid();

            TextBlock body = new TextBlock
            {
                Text =
                    (note.Content ?? string.Empty)
                    .Replace(
                        Environment.NewLine,
                        " "
                    )
                    .Replace(
                        "\n",
                        " "
                    )
                    .Trim(),

                FontFamily =
                    new FontFamily("Segoe UI"),

                FontSize = 12,

                LineHeight = 16,

                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(
                            62,
                            62,
                            62
                        )
                    ),

                TextWrapping =
                    TextWrapping.Wrap,

                TextTrimming =
                    TextTrimming.CharacterEllipsis,

                Margin =
                    new Thickness(
                        0,
                        7,
                        0,
                        0
                    )
            };

            bodyArea.Children.Add(body);

            // Subtle bottom fade for long notes.
            LinearGradientBrush fadeBrush =
                new LinearGradientBrush
                {
                    StartPoint =
                        new Point(0, 0),

                    EndPoint =
                        new Point(0, 1)
                };

            fadeBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(
                        0,
                        255,
                        255,
                        255
                    ),
                    0
                )
            );

            fadeBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(
                        70,
                        255,
                        255,
                        255
                    ),
                    0.50
                )
            );

            fadeBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(
                        235,
                        255,
                        255,
                        255
                    ),
                    1
                )
            );

            Border fade = new Border
            {
                Height = 25,

                VerticalAlignment =
                    VerticalAlignment.Bottom,

                Background = fadeBrush,

                IsHitTestVisible = false
            };

            bodyArea.Children.Add(fade);

            Grid.SetRow(
                bodyArea,
                1
            );

            preview.Children.Add(bodyArea);

            return preview;
        }

        // ============================================================
        // HOVER GATE
        // ============================================================

        private void TryExpandFromPointer(
            Border tab,
            TextBlock label,
            Grid preview)
        {
            if (!_isOpen ||
                _isAnimating ||
                IsCursorOnPhysicalScreenEdge())
            {
                return;
            }

            ExpandTab(
                tab,
                label,
                preview
            );
        }

        // ============================================================
        // EXPAND
        // ============================================================

        private void ExpandTab(
            Border tab,
            TextBlock label,
            Grid preview)
        {
            if (_expandedTab == tab)
                return;

            if (_expandedTab != null)
            {
                CollapseCurrentlyExpanded();
            }

            _expandedTab = tab;

            Panel.SetZIndex(
                tab,
                1000
            );

            // Right edge NEVER moves.
            // ExpandedLeft + ExpandedWidth == RightEdge.
            DoubleAnimation left =
                new DoubleAnimation
                {
                    To = ExpandedLeft,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseOut()
                };

            DoubleAnimation width =
                new DoubleAnimation
                {
                    To = ExpandedWidth,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseOut()
                };

            DoubleAnimation height =
                new DoubleAnimation
                {
                    To = ExpandedHeight,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseOut()
                };

            tab.BeginAnimation(
                Canvas.LeftProperty,
                left
            );

            tab.BeginAnimation(
                FrameworkElement.WidthProperty,
                width
            );

            tab.BeginAnimation(
                FrameworkElement.HeightProperty,
                height
            );

            AnimateOpacity(
                label,
                1,
                0,
                70
            );

            preview.Visibility =
                Visibility.Visible;

            DoubleAnimation previewFade =
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            60
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            FadeDurationMs
                        ),

                    EasingFunction =
                        EaseOut()
                };

            preview.BeginAnimation(
                UIElement.OpacityProperty,
                previewFade
            );
        }

        // ============================================================
        // COLLAPSE ONE TAB
        // ============================================================

        private void CollapseTab(
            Border tab,
            TextBlock label,
            Grid preview)
        {
            if (_expandedTab != tab)
                return;

            CollapseTabInternal(
                tab,
                label,
                preview
            );

            _expandedTab = null;
        }

        private void CollapseCurrentlyExpanded()
        {
            if (_expandedTab == null)
                return;

            if (_expandedTab.Child is not Grid grid)
                return;

            TextBlock? label = null;
            Grid? preview = null;

            foreach (UIElement child in grid.Children)
            {
                if (child is TextBlock text)
                    label = text;

                if (child is Grid nested &&
                    nested.Visibility ==
                    Visibility.Visible)
                    preview = nested;
            }

            if (label != null &&
                preview != null)
            {
                CollapseTabInternal(
                    _expandedTab,
                    label,
                    preview
                );
            }

            _expandedTab = null;
        }

        private void CollapseTabInternal(
            Border tab,
            TextBlock label,
            Grid preview)
        {
            AnimateOpacity(
                preview,
                1,
                0,
                80
            );

            DoubleAnimation left =
                new DoubleAnimation
                {
                    To =
                        RightEdge -
                        FanTabWidth,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseInOut()
                };

            DoubleAnimation width =
                new DoubleAnimation
                {
                    To = FanTabWidth,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseInOut()
                };

            DoubleAnimation height =
                new DoubleAnimation
                {
                    To = FanTabHeight,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseInOut()
                };

            left.Completed += (_, _) =>
            {
                preview.Visibility =
                    Visibility.Collapsed;

                Panel.SetZIndex(
                    tab,
                    200
                );

                AnimateOpacity(
                    label,
                    0,
                    1,
                    90
                );
            };

            tab.BeginAnimation(
                Canvas.LeftProperty,
                left
            );

            tab.BeginAnimation(
                FrameworkElement.WidthProperty,
                width
            );

            tab.BeginAnimation(
                FrameworkElement.HeightProperty,
                height
            );
        }

        // ============================================================
        // FAN COLLAPSE
        // ============================================================

        private void CollapseTabs()
        {
            _isAnimating = true;

            _expandedTab = null;

            foreach (UIElement element in TabCanvas.Children)
            {
                if (element is not Border tab)
                    continue;

                tab.BeginAnimation(
                    FrameworkElement.WidthProperty,
                    null
                );

                tab.BeginAnimation(
                    FrameworkElement.HeightProperty,
                    null
                );

                DoubleAnimation left =
                    new DoubleAnimation
                    {
                        To =
                            RightEdge -
                            FanTabWidth,

                        Duration =
                            TimeSpan.FromMilliseconds(
                                FanDurationMs
                            ),

                        EasingFunction =
                            EaseIn()
                    };

                DoubleAnimation opacity =
                    new DoubleAnimation
                    {
                        To = 0,

                        Duration =
                            TimeSpan.FromMilliseconds(
                                150
                            ),

                        EasingFunction =
                            EaseIn()
                    };

                tab.BeginAnimation(
                    Canvas.LeftProperty,
                    left
                );

                tab.BeginAnimation(
                    UIElement.OpacityProperty,
                    opacity
                );
            }

            HideAddButton();

            DispatcherTimer timer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            FanDurationMs + 30
                        )
                };

            timer.Tick += (_, _) =>
            {
                timer.Stop();

                TabCanvas.Children.Clear();

                ShowRestingRail();

                _isAnimating = false;
            };

            timer.Start();
        }

        // ============================================================
        // RAIL VISIBILITY
        // ============================================================

        private void HideRestingRail()
        {
            AnimateOpacity(
                Rail,
                1,
                0,
                120
            );

            AnimateOpacity(
                IndicatorStack,
                1,
                0,
                100
            );
        }

        private void ShowRestingRail()
        {
            Rail.Visibility =
                Visibility.Visible;

            IndicatorStack.Visibility =
                Visibility.Visible;

            AnimateOpacity(
                Rail,
                0,
                1,
                140
            );

            AnimateOpacity(
                IndicatorStack,
                0,
                1,
                160
            );

            HideAddButton();
        }

        private void SetRestingState()
        {
            TabCanvas.Children.Clear();

            _expandedTab = null;

            Rail.Visibility =
                Visibility.Visible;

            IndicatorStack.Visibility =
                Visibility.Visible;

            Rail.Opacity = 1;
            IndicatorStack.Opacity = 1;

            HideAddButton();
        }

        // ============================================================
        // ADD BUTTON
        // ============================================================

        private void ShowAddButton()
        {
            Canvas.SetLeft(
                AddButton,
                AddButtonLeft
            );

            Canvas.SetTop(
                AddButton,
                AddButtonTop
            );

            AddButton.Visibility =
                Visibility.Visible;

            AddButton.Opacity = 1;
        }

        private void HideAddButton()
        {
            AddButton.Visibility =
                Visibility.Collapsed;
        }

        private void AddButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NoteWindow window =
                new NoteWindow();

            bool? result =
                window.ShowDialog();

            if (result == true)
            {
                ReloadNotes();
            }
        }

        // ============================================================
        // NOTE CLICK
        // ============================================================

        private void NoteTab_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Border tab)
                return;

            if (tab.Tag is not int noteId)
                return;

            Note? note =
                _noteRepository.GetNote(
                    noteId
                );

            if (note == null)
                return;

            NoteWindow window =
                new NoteWindow(note);

            bool? result =
                window.ShowDialog();

            if (result == true)
            {
                ReloadNotes();
            }
        }

        // ============================================================
        // OPACITY
        // ============================================================

        private void AnimateOpacity(
            UIElement element,
            double from,
            double to,
            int duration)
        {
            DoubleAnimation animation =
                new DoubleAnimation
                {
                    From = from,
                    To = to,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            duration
                        ),

                    EasingFunction =
                        EaseInOut()
                };

            element.BeginAnimation(
                UIElement.OpacityProperty,
                animation
            );
        }

        // ============================================================
        // EASING
        // ============================================================

        private static CubicEase EaseOut()
        {
            return new CubicEase
            {
                EasingMode =
                    EasingMode.EaseOut
            };
        }

        private static CubicEase EaseIn()
        {
            return new CubicEase
            {
                EasingMode =
                    EasingMode.EaseIn
            };
        }

        private static CubicEase EaseInOut()
        {
            return new CubicEase
            {
                EasingMode =
                    EasingMode.EaseInOut
            };
        }

        // ============================================================
        // COLOR
        // ============================================================

        private static SolidColorBrush CreateBrush(
            string color)
        {
            object? value =
                ColorConverter.ConvertFromString(
                    color
                );

            if (value is Color parsed)
            {
                return new SolidColorBrush(parsed);
            }

            return new SolidColorBrush(
                Color.FromRgb(
                    255,
                    215,
                    90
                )
            );
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        protected override void OnClosed(
            EventArgs e)
        {
            _collapseTimer.Stop();

            _collapseTimer.Tick -=
                CollapseTimer_Tick;

            base.OnClosed(e);
        }

        private void CollapseTimer_Tick(
            object? sender,
            EventArgs e)
        {
            _collapseTimer.Stop();

            if (IsMouseOver)
                return;

            CloseRail();
        }

        // ============================================================
        // WINDOWS API
        // ============================================================

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_CXVIRTUALSCREEN = 78;

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(
            out POINT lpPoint
        );

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(
            int nIndex
        );

        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}

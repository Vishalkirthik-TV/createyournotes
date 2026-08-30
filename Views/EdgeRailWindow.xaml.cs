using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HoldMyNotes.Data;
using HoldMyNotes.Models;
using HoldMyNotes.Views;

namespace HoldMyNotes
{
    public partial class EdgeRailWindow : Window
    {
        // ============================================================
        // LAYOUT
        // ============================================================

        private const double RailLeft = 200;
        private const double RailTop = 150;
        private const double RailWidth = 20;
        private const double RailMinHeight = 112;

        private const double PillWidth = 8;
        private const double PillHeight = 22;
        private const double PillGap = 2;

        private const double FanLeft = 150;
        private const double FanTop = 70;
        private const double FanWidth = 48;
        private const double FanHeight = 58;
        private const double FanGap = 4;

        // The tab's right edge is ALWAYS 198.
        // Normal: 150 + 48 = 198.
        // Expanded: 43 + 155 = 198.
        // Keeping this edge fixed prevents MouseLeave while expanding.
        private const double FixedRight = 198;
        private const double ExpandedWidth = 155;
        private const double ExpandedHeight = 120;

        private const double AddLeft = 187;
        private const double AddTop = 360;
        private const double AddSize = 30;

        // ============================================================
        // ANIMATION
        // ============================================================

        private const int FanDurationMs = 220;
        private const int FanStaggerMs = 35;
        private const int HoverDurationMs = 190;

        // ============================================================
        // DATA
        // ============================================================

        private readonly NoteRepository _noteRepository;
        private List<Note> _notes = new List<Note>();

        // ============================================================
        // STATE
        // ============================================================

        private bool _isOpen;
        private bool _isAnimating;
        private Border? _expandedTab;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public EdgeRailWindow()
        {
            InitializeComponent();

            _noteRepository = new NoteRepository();

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
            Left = SystemParameters.WorkArea.Right - Width;
            Top = (SystemParameters.WorkArea.Height - Height) / 2;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            PositionWindow();
        }

        // ============================================================
        // DATA
        // ============================================================

        private void LoadNotes()
        {
            _notes = _noteRepository.GetAllNotes();
        }

        private void ReloadNotes()
        {
            LoadNotes();
            BuildIndicators();

            RemoveAllTabs();

            if (_isOpen)
            {
                // Keep the rail in fan-out mode after a save/delete.
                FanOutTabs();
            }
            else
            {
                SetRestingState();
            }
        }

        // ============================================================
        // PUBLIC API - MainWindow USES THESE
        // ============================================================

        public void Open()
        {
            if (_isOpen || _isAnimating)
                return;

            _isOpen = true;
            FanOutTabs();
        }

        public void CloseRail()
        {
            if (!_isOpen)
                return;

            if (_isAnimating)
                return;

            _isOpen = false;
            CollapseTabs();
        }

        // ============================================================
        // PUBLIC HIT TEST - MainWindow USES THIS
        // ============================================================

        public bool IsMouseInsideVisibleArea(int screenX, int screenY)
        {
            Point point =
                PointFromScreen(new Point(screenX, screenY));

            // Rail.
            if (new Rect(
                    RailLeft,
                    RailTop,
                    RailWidth,
                    Rail.Height).Contains(point))
            {
                return true;
            }

            // Add button.
            if (AddButton.Visibility == Visibility.Visible &&
                new Rect(
                    AddLeft,
                    AddTop,
                    AddSize,
                    AddSize).Contains(point))
            {
                return true;
            }

            // Fan-out tabs.
            if (_isOpen)
            {
                foreach (UIElement element in TabCanvas.Children)
                {
                    if (element is not Border tab ||
                        tab.Visibility != Visibility.Visible)
                    {
                        continue;
                    }

                    double left = Canvas.GetLeft(tab);
                    double top = Canvas.GetTop(tab);

                    if (double.IsNaN(left))
                        left = FanLeft;

                    if (double.IsNaN(top))
                        top = FanTop;

                    // Width/Height return the current effective
                    // animated value in WPF.
                    double width =
                        tab.Width > 0 ? tab.Width : FanWidth;

                    double height =
                        tab.Height > 0 ? tab.Height : FanHeight;

                    if (new Rect(
                            left,
                            top,
                            width,
                            height).Contains(point))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ============================================================
        // INDICATORS
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
                    CornerRadius = new CornerRadius(5),
                    Background = CreateBrush(note.Color),
                    Margin = new Thickness(
                        0,
                        PillGap,
                        0,
                        PillGap)
                };

                IndicatorStack.Children.Add(pill);
            }

            double requiredHeight =
                Math.Max(
                    RailMinHeight,
                    _notes.Count *
                        (PillHeight + PillGap * 2) +
                    10);

            Rail.Height = requiredHeight;
        }

        // ============================================================
        // FAN OUT
        // ============================================================

        private void FanOutTabs()
        {
            RemoveAllTabs();

            Rail.IsHitTestVisible = false;
            IndicatorStack.IsHitTestVisible = false;

            Rail.Visibility = Visibility.Visible;
            IndicatorStack.Visibility = Visibility.Visible;

            if (_notes.Count == 0)
            {
                ShowAddButton();

                FadeOut(Rail, 100);
                FadeOut(IndicatorStack, 100);

                return;
            }

            _isAnimating = true;

            for (int i = 0; i < _notes.Count; i++)
            {
                Border tab = CreateTab(_notes[i]);

                Canvas.SetLeft(tab, RailLeft);
                Canvas.SetTop(tab, RailTop);

                Panel.SetZIndex(tab, 120 + i);

                TabCanvas.Children.Add(tab);

                double targetTop =
                    FanTop + i * (FanHeight + FanGap);

                Animate(
                    tab,
                    Canvas.LeftProperty,
                    RailLeft,
                    FanLeft,
                    FanDurationMs,
                    i * FanStaggerMs);

                Animate(
                    tab,
                    Canvas.TopProperty,
                    RailTop,
                    targetTop,
                    FanDurationMs,
                    i * FanStaggerMs);
            }

            FadeOut(Rail, 130);
            FadeOut(IndicatorStack, 100);

            ShowAddButton();

            FinishAnimationAfter(
                FanDurationMs +
                _notes.Count * FanStaggerMs +
                30);
        }

        // ============================================================
        // CREATE TAB
        // ============================================================

        private Border CreateTab(Note note)
        {
            Border tab = new Border
            {
                Width = FanWidth,
                Height = FanHeight,
                Background = CreateBrush(note.Color),

                // The right side is attached to the edge.
                // The left side is rounded.
                CornerRadius = new CornerRadius(
                    12,
                    0,
                    0,
                    12),

                Tag = note.Id,
                Cursor = Cursors.Hand,

                SnapsToDevicePixels = true,
                ClipToBounds = true
            };

            Grid content = new Grid();

            // --------------------------------------------------------
            // NORMAL TAB LABEL
            // --------------------------------------------------------

            TextBlock label = new TextBlock
            {
                Text =
                    (note.Title ?? "UNTITLED")
                    .ToUpperInvariant(),

                FontSize = 9,

                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(70, 70, 70)),

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                VerticalAlignment =
                    VerticalAlignment.Center,

                TextAlignment =
                    TextAlignment.Center,

                TextTrimming =
                    TextTrimming.CharacterEllipsis,

                IsHitTestVisible = false
            };

            label.LayoutTransform =
                new RotateTransform(90);

            // --------------------------------------------------------
            // EXPANDED PREVIEW
            // --------------------------------------------------------

            Grid preview =
                CreatePreview(note);

            content.Children.Add(label);
            content.Children.Add(preview);

            tab.Child = content;

            // --------------------------------------------------------
            // HOVER
            // --------------------------------------------------------

            tab.MouseEnter += (_, _) =>
            {
                if (!_isOpen)
                    return;

                ExpandTab(
                    tab,
                    label,
                    preview);
            };

            tab.MouseLeave += (_, _) =>
            {
                if (!_isOpen)
                    return;

                CollapseTab(tab);
            };

            // --------------------------------------------------------
            // CLICK
            // --------------------------------------------------------

            tab.MouseLeftButtonUp += NoteTab_Click;

            return tab;
        }

        // ============================================================
        // PREVIEW
        // ============================================================

        private Grid CreatePreview(Note note)
        {
            Grid preview = new Grid
            {
                Visibility = Visibility.Collapsed,
                Opacity = 0,

                // The preview is visual only.
                // The Border itself owns mouse hit-testing.
                IsHitTestVisible = false,

                Margin = new Thickness(
                    13,
                    10,
                    10,
                    9)
            };

            preview.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(24)
                });

            preview.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            1,
                            GridUnitType.Star)
                });

            TextBlock title = new TextBlock
            {
                Text = note.Title ?? "UNTITLED",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,

                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(40, 40, 40)),

                TextTrimming =
                    TextTrimming.CharacterEllipsis
            };

            Grid.SetRow(title, 0);
            preview.Children.Add(title);

            Grid bodyGrid = new Grid();

            TextBlock body = new TextBlock
            {
                Text = note.Content ?? string.Empty,

                FontSize = 11,
                LineHeight = 15,

                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(65, 65, 65)),

                TextWrapping =
                    TextWrapping.Wrap,

                TextTrimming =
                    TextTrimming.CharacterEllipsis
            };

            bodyGrid.Children.Add(body);

            // --------------------------------------------------------
            // SOFT BOTTOM FADE
            // --------------------------------------------------------

            LinearGradientBrush fadeBrush =
                new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1)
                };

            fadeBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(0, 255, 255, 255),
                    0.00));

            fadeBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(30, 255, 255, 255),
                    0.45));

            fadeBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(175, 255, 255, 255),
                    0.82));

            fadeBrush.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(255, 255, 255, 255),
                    1.00));

            Border fade = new Border
            {
                Height = 30,

                VerticalAlignment =
                    VerticalAlignment.Bottom,

                Background = fadeBrush,

                IsHitTestVisible = false
            };

            bodyGrid.Children.Add(fade);

            Grid.SetRow(bodyGrid, 1);
            preview.Children.Add(bodyGrid);

            return preview;
        }

        // ============================================================
        // THIRD STATE - EXPAND
        // ============================================================

        private void ExpandTab(
            Border tab,
            TextBlock label,
            Grid preview)
        {
            if (_expandedTab == tab)
                return;

            // If another card is expanded, collapse it first.
            if (_expandedTab != null &&
                _expandedTab != tab)
            {
                CollapseTab(_expandedTab);
            }

            _expandedTab = tab;

            Panel.SetZIndex(tab, 1000);

            // --------------------------------------------------------
            // IMPORTANT:
            // Keep the RIGHT EDGE FIXED.
            // --------------------------------------------------------

            double expandedLeft =
                FixedRight - ExpandedWidth;

            double currentLeft =
                Canvas.GetLeft(tab);

            if (double.IsNaN(currentLeft))
                currentLeft = FanLeft;

            double currentWidth =
                tab.Width > 0
                    ? tab.Width
                    : FanWidth;

            double currentHeight =
                tab.Height > 0
                    ? tab.Height
                    : FanHeight;

            StopAnimation(
                tab,
                Canvas.LeftProperty);

            StopAnimation(
                tab,
                WidthProperty);

            StopAnimation(
                tab,
                HeightProperty);

            // Move LEFT while growing RIGHT-EDGE remains fixed.
            Animate(
                tab,
                Canvas.LeftProperty,
                currentLeft,
                expandedLeft,
                HoverDurationMs,
                0);

            Animate(
                tab,
                WidthProperty,
                currentWidth,
                ExpandedWidth,
                HoverDurationMs,
                0);

            Animate(
                tab,
                HeightProperty,
                currentHeight,
                ExpandedHeight,
                HoverDurationMs,
                0);

            // Hide vertical title.
            FadeOut(label, 80);

            // Show content after expansion starts.
            preview.Visibility = Visibility.Visible;
            preview.Opacity = 0;

            DoubleAnimation previewAnimation =
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,

                    BeginTime =
                        TimeSpan.FromMilliseconds(45),

                    Duration =
                        TimeSpan.FromMilliseconds(135),

                    FillBehavior =
                        FillBehavior.HoldEnd,

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseOut
                        }
                };

            preview.BeginAnimation(
                UIElement.OpacityProperty,
                previewAnimation);
        }

        // ============================================================
        // THIRD STATE - COLLAPSE
        // ============================================================

        private void CollapseTab(Border tab)
        {
            if (tab.Child is not Grid content)
                return;

            TextBlock? label = null;
            Grid? preview = null;

            foreach (UIElement child in content.Children)
            {
                if (child is TextBlock text)
                    label = text;

                if (child is Grid grid)
                    preview = grid;
            }

            // Do not collapse a normal non-expanded tab.
            if (_expandedTab != tab &&
                tab.Width <= FanWidth + 0.5)
            {
                return;
            }

            double currentLeft =
                Canvas.GetLeft(tab);

            if (double.IsNaN(currentLeft))
                currentLeft = FanLeft;

            double currentWidth =
                tab.Width > 0
                    ? tab.Width
                    : FanWidth;

            double currentHeight =
                tab.Height > 0
                    ? tab.Height
                    : FanHeight;

            StopAnimation(
                tab,
                Canvas.LeftProperty);

            StopAnimation(
                tab,
                WidthProperty);

            StopAnimation(
                tab,
                HeightProperty);

            if (preview != null)
                FadeOut(preview, 70);

            Animate(
                tab,
                Canvas.LeftProperty,
                currentLeft,
                FanLeft,
                HoverDurationMs,
                0);

            Animate(
                tab,
                WidthProperty,
                currentWidth,
                FanWidth,
                HoverDurationMs,
                0);

            Animate(
                tab,
                HeightProperty,
                currentHeight,
                FanHeight,
                HoverDurationMs,
                0,
                () =>
                {
                    if (preview != null)
                    {
                        preview.Visibility =
                            Visibility.Collapsed;

                        preview.Opacity = 0;
                    }

                    if (label != null)
                    {
                        label.Opacity = 0;
                        FadeIn(label, 100);
                    }

                    Panel.SetZIndex(tab, 120);

                    if (_expandedTab == tab)
                        _expandedTab = null;
                });
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
                _noteRepository.GetNote(noteId);

            if (note == null)
                return;

            // Separate window for editing/viewing.
            NoteWindow window =
                new NoteWindow(note);

            bool? result =
                window.ShowDialog();

            if (result == true)
            {
                ReloadNotes();
            }

            e.Handled = true;
        }

        // ============================================================
        // ADD
        // ============================================================

        private void AddButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            // Separate window. No Owner is assigned.
            NoteWindow window =
                new NoteWindow();

            bool? result =
                window.ShowDialog();

            if (result == true)
            {
                ReloadNotes();
            }

            e.Handled = true;
        }

        // ============================================================
        // ADD BUTTON
        // ============================================================

        private void ShowAddButton()
        {
            Canvas.SetLeft(
                AddButton,
                AddLeft);

            Canvas.SetTop(
                AddButton,
                AddTop);

            AddButton.Visibility =
                Visibility.Visible;

            FadeIn(
                AddButton,
                120);
        }

        private void HideAddButton()
        {
            AddButton.Visibility =
                Visibility.Collapsed;

            AddButton.Opacity = 0;
        }

        // ============================================================
        // RESTING STATE
        // ============================================================

        private void SetRestingState()
        {
            Rail.Visibility =
                Visibility.Visible;

            Rail.Opacity = 1;
            Rail.IsHitTestVisible = true;

            IndicatorStack.Visibility =
                Visibility.Visible;

            IndicatorStack.Opacity = 1;
            IndicatorStack.IsHitTestVisible = false;

            HideAddButton();

            _expandedTab = null;
        }

        // ============================================================
        // COLLAPSE WHOLE FAN
        // ============================================================

        private void CollapseTabs()
        {
            if (TabCanvas.Children.Count == 0)
            {
                SetRestingState();
                return;
            }

            _isAnimating = true;
            _expandedTab = null;

            foreach (UIElement element in TabCanvas.Children)
            {
                if (element is not Border tab)
                    continue;

                double left =
                    Canvas.GetLeft(tab);

                if (double.IsNaN(left))
                    left = FanLeft;

                double top =
                    Canvas.GetTop(tab);

                if (double.IsNaN(top))
                    top = FanTop;

                double width =
                    tab.Width > 0
                        ? tab.Width
                        : FanWidth;

                double height =
                    tab.Height > 0
                        ? tab.Height
                        : FanHeight;

                StopAnimation(
                    tab,
                    Canvas.LeftProperty);

                StopAnimation(
                    tab,
                    Canvas.TopProperty);

                StopAnimation(
                    tab,
                    WidthProperty);

                StopAnimation(
                    tab,
                    HeightProperty);

                Animate(
                    tab,
                    Canvas.LeftProperty,
                    left,
                    RailLeft,
                    FanDurationMs,
                    0);

                Animate(
                    tab,
                    Canvas.TopProperty,
                    top,
                    RailTop,
                    FanDurationMs,
                    0);

                Animate(
                    tab,
                    WidthProperty,
                    width,
                    FanWidth,
                    FanDurationMs,
                    0);

                Animate(
                    tab,
                    HeightProperty,
                    height,
                    FanHeight,
                    FanDurationMs,
                    0);
            }

            HideAddButton();

            FadeIn(Rail, 150);
            FadeIn(IndicatorStack, 180);

            FinishCollapse();
        }

        private void FinishCollapse()
        {
            System.Windows.Threading.DispatcherTimer timer =
                new System.Windows.Threading.DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            FanDurationMs + 30)
                };

            timer.Tick += (_, _) =>
            {
                timer.Stop();

                RemoveAllTabs();

                SetRestingState();

                _isAnimating = false;
            };

            timer.Start();
        }

        // ============================================================
        // REMOVE TABS
        // ============================================================

        private void RemoveAllTabs()
        {
            TabCanvas.Children.Clear();
            _expandedTab = null;
        }

        // ============================================================
        // GENERIC ANIMATION
        // ============================================================

        private void Animate(
            UIElement element,
            DependencyProperty property,
            double from,
            double to,
            int durationMs,
            int delayMs,
            Action? completed = null)
        {
            DoubleAnimation animation =
                new DoubleAnimation
                {
                    From = from,
                    To = to,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            delayMs),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            durationMs),

                    // Keep the final value active.
                    // Otherwise WPF would snap back to
                    // the original Width/Left/Height.
                    FillBehavior =
                        FillBehavior.HoldEnd,

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseInOut
                        }
                };

            if (completed != null)
            {
                animation.Completed +=
                    (_, _) => completed();
            }

            element.BeginAnimation(
                property,
                animation);
        }

        private void StopAnimation(
            UIElement element,
            DependencyProperty property)
        {
            // Caller reads the effective animated value BEFORE
            // calling this method.
            element.BeginAnimation(
                property,
                null);
        }

        private void FadeIn(
            UIElement element,
            int durationMs)
        {
            element.Visibility =
                Visibility.Visible;

            DoubleAnimation animation =
                new DoubleAnimation
                {
                    From = element.Opacity,
                    To = 1,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            durationMs),

                    FillBehavior =
                        FillBehavior.HoldEnd,

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseOut
                        }
                };

            element.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }

        private void FadeOut(
            UIElement? element,
            int durationMs)
        {
            if (element == null)
                return;

            DoubleAnimation animation =
                new DoubleAnimation
                {
                    From = element.Opacity,
                    To = 0,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            durationMs),

                    FillBehavior =
                        FillBehavior.HoldEnd,

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseIn
                        }
                };

            element.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }

        private void FinishAnimationAfter(
            int milliseconds)
        {
            System.Windows.Threading.DispatcherTimer timer =
                new System.Windows.Threading.DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            milliseconds)
                };

            timer.Tick += (_, _) =>
            {
                timer.Stop();
                _isAnimating = false;
            };

            timer.Start();
        }

        // ============================================================
        // COLOR
        // ============================================================

        private SolidColorBrush CreateBrush(
            string? color)
        {
            if (!string.IsNullOrWhiteSpace(color))
            {
                object? converted =
                    ColorConverter.ConvertFromString(
                        color);

                if (converted is Color parsed)
                {
                    return new SolidColorBrush(parsed);
                }
            }

            return new SolidColorBrush(
                Color.FromRgb(
                    255,
                    215,
                    90));
        }

        // ============================================================
        // CLEANUP
        // ============================================================

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}

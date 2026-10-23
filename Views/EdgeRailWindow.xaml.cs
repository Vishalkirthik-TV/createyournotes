using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

using HoldMyNotes.Data;
using HoldMyNotes.Models;
using HoldMyNotes.Services;
using HoldMyNotes.Views;

namespace HoldMyNotes
{
    public partial class EdgeRailWindow : Window
    {
        // ============================================================
        // WINDOW / EDGE GEOMETRY
        // ============================================================

        private const double WindowWidth = 360;
        private const double WindowHeight = 720;

        private const double RightEdge = WindowWidth;


        // ============================================================
        // RESTING RAIL
        // ============================================================

        private const double RailWidth = 14;

        private const double RailLeft =
            RightEdge - RailWidth;

        private const double RailTop = 304;

        private const double RailHeight = 112;


        // ============================================================
        // RESTING PILLS
        // ============================================================

        private const double PillWidth = 7;

        private const double PillHeight = 22;

        private const double PillGap = 2;


        // ============================================================
        // FAN-OUT
        // ============================================================

        private const int MaxVisibleCards = 5;

        private const double TabWidth = 34;

        private const double TabHeight = 125;

        /*
         * Breathing room reserved at the top and bottom of a
         * fan-out tab's vertical heading. The heading is rotated
         * -90 degrees and vertically centered, so constraining
         * its length leaves symmetric padding at both ends
         * instead of letting a long title run edge to edge.
         */
        private const double TabHeadingVerticalPadding = 13;

        private const double TabGap = -7;

        private const double StackOffsetX = 6;

        private const double TabLeft =
            RightEdge - TabWidth;

        private const double TabStartTop = 40;

        private double _centeredStartTop;


        // ============================================================
        // MORE / STACKED CARD
        // ============================================================

        private const double MoreHeight = 34;

        private const double MoreLeft =
            RightEdge - TabWidth;


        // ============================================================
        // EXPANDED NOTE
        // ============================================================

        private const double ExpandedWidth = 150;

        private const double ExpandedHeight = 200;

        private const double ExpandedLeft =
            RightEdge - ExpandedWidth;


        // ============================================================
        // ADD BUTTON
        // ============================================================

        private const double AddButtonSize = 30;

        private const double AddButtonLeft =
            RightEdge - AddButtonSize;


        // ============================================================
        // ANIMATION
        // ============================================================

        private const int FanDurationMs = 230;

        private const int FanStaggerMs = 45;

        private const int HoverDurationMs = 240;

        private const int CollapseDelayMs = 170;


        // ============================================================
        // DATA
        // ============================================================

        private readonly NoteRepository _noteRepository;

        private List<Note> _notes =
            new List<Note>();


        // ============================================================
        // STATE
        // ============================================================

        private readonly DispatcherTimer _collapseTimer;

        private bool _isOpen;

        private bool _isAnimating;

        private Border? _expandedTab;

        private Border? _moreTab;


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public EdgeRailWindow()
        {
            InitializeComponent();


            _noteRepository =
                new NoteRepository();


            _collapseTimer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            CollapseDelayMs
                        )
                };


            _collapseTimer.Tick +=
                CollapseTimer_Tick;


            AddButton.Click +=
                AddButton_Click;


            LoadNotes();

            BuildIndicators();

            SetRestingState();

            PositionWindow();
        }


        // ============================================================
        // WINDOW POSITION
        // ============================================================

        private void PositionWindow()
        {
            Left =
                SystemParameters.WorkArea.Right -
                WindowWidth;


            Top =
                (
                    SystemParameters.WorkArea.Height -
                    WindowHeight
                ) / 2.0;
        }


        protected override void OnContentRendered(
            EventArgs e)
        {
            base.OnContentRendered(e);

            PositionWindow();
        }


        protected override void OnRenderSizeChanged(
            SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(
                sizeInfo
            );

            PositionWindow();
        }


        // ============================================================
        // PUBLIC API USED BY MAINWINDOW
        // ============================================================

        public void Open()
        {
            if (_isOpen)
            {
                return;
            }


            if (_isAnimating)
            {
                return;
            }


            _collapseTimer.Stop();


            _isOpen = true;


            FanOutTabs();
        }


        public void CloseRail()
        {
            if (!_isOpen)
            {
                return;
            }


            if (_isAnimating)
            {
                return;
            }


            _isOpen = false;


            _expandedTab = null;


            CollapseTabs();
        }


        // ============================================================
        // HIT TEST
        // ============================================================

        public bool IsMouseInsideVisibleArea(
            int screenX,
            int screenY)
        {
            /*
             * The literal physical screen edge is NOT part of the
             * visible hover area.
             */
            if (IsAtPhysicalRightEdge(screenX))
            {
                return false;
            }


            Point point =
                PointFromScreen(
                    new Point(
                        screenX,
                        screenY
                    )
                );


            // ========================================================
            // FAN-OUT STATE
            // ========================================================

            if (_isOpen)
            {
                // ----------------------------------------------------
                // ADD BUTTON
                // ----------------------------------------------------

                if (AddButton.Visibility ==
                    Visibility.Visible)
                {
                    double addLeft =
                        Canvas.GetLeft(
                            AddButton
                        );


                    double addTop =
                        Canvas.GetTop(
                            AddButton
                        );


                    if (double.IsNaN(addLeft))
                    {
                        addLeft =
                            AddButtonLeft;
                    }


                    if (double.IsNaN(addTop))
                    {
                        addTop =
                            GetCurrentAddButtonTop();
                    }


                    Rect addRect =
                        new Rect(
                            addLeft,
                            addTop,
                            AddButtonSize,
                            AddButtonSize
                        );


                    if (addRect.Contains(point))
                    {
                        return true;
                    }
                }


                // ----------------------------------------------------
                // NOTE TABS
                // ----------------------------------------------------

                foreach (
                    UIElement element
                    in TabCanvas.Children)
                {
                    if (element is not Border tab)
                    {
                        continue;
                    }


                    if (tab.Visibility !=
                        Visibility.Visible)
                    {
                        continue;
                    }


                    double width =
                        tab.ActualWidth > 0
                            ? tab.ActualWidth
                            : TabWidth;


                    double height =
                        tab.ActualHeight > 0
                            ? tab.ActualHeight
                            : TabHeight;


                    double left =
                        Canvas.GetLeft(tab);


                    double top =
                        Canvas.GetTop(tab);


                    if (double.IsNaN(left))
                    {
                        left =
                            TabLeft;
                    }


                    if (double.IsNaN(top))
                    {
                        top =
                            _centeredStartTop;
                    }


                    Rect tabRect =
                        new Rect(
                            left,
                            top,
                            width,
                            height
                        );


                    if (tabRect.Contains(point))
                    {
                        return true;
                    }
                }


                // ----------------------------------------------------
                // MORE-NOTES STACK
                // ----------------------------------------------------

                if (_moreTab != null &&
                    _moreTab.Visibility ==
                    Visibility.Visible)
                {
                    double left =
                        Canvas.GetLeft(
                            _moreTab
                        );


                    double top =
                        Canvas.GetTop(
                            _moreTab
                        );


                    if (double.IsNaN(left))
                    {
                        left =
                            MoreLeft;
                    }


                    if (double.IsNaN(top))
                    {
                        top =
                            GetMoreTabTop();
                    }


                    Rect moreRect =
                        new Rect(
                            left,
                            top,
                            TabWidth,
                            MoreHeight
                        );


                    if (moreRect.Contains(point))
                    {
                        return true;
                    }
                }


                return false;
            }


            // ========================================================
            // RESTING RAIL
            // ========================================================

            Rect railRect =
                new Rect(
                    RailLeft,
                    RailTop,
                    RailWidth,
                    Rail.Height
                );


            return railRect.Contains(point);
        }


        // ============================================================
        // PHYSICAL SCREEN EDGE
        // ============================================================

        private static bool IsAtPhysicalRightEdge(
            int screenX)
        {
            int left =
                GetSystemMetrics(
                    SM_XVIRTUALSCREEN
                );


            int width =
                GetSystemMetrics(
                    SM_CXVIRTUALSCREEN
                );


            int right =
                left + width;


            return screenX >= right - 1;
        }


        private static bool IsCursorOnPhysicalScreenEdge()
        {
            if (!GetCursorPos(
                    out POINT cursor
                ))
            {
                return false;
            }


            return IsAtPhysicalRightEdge(
                cursor.X
            );
        }


        // ============================================================
        // DATA
        // ============================================================

        private void LoadNotes()
        {
            _notes =
                _noteRepository.GetAllNotes()
                ?? new List<Note>();
        }


        private void ReloadNotes()
        {
            LoadNotes();


            BuildIndicators();


            _expandedTab = null;

            _moreTab = null;


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
        // RESTING RAIL
        // ============================================================

        private void BuildIndicators()
        {
            IndicatorStack.Children.Clear();


            int visibleCount =
                Math.Min(
                    MaxVisibleCards,
                    _notes.Count
                );


            for (
                int i = 0;
                i < visibleCount;
                i++)
            {
                Note note =
                    _notes[i];


                Border pill =
                    new Border
                    {
                        Width =
                            PillWidth,

                        Height =
                            PillHeight,

                        CornerRadius =
                            new CornerRadius(4),

                        Background =
                            CreateBrush(
                                note.Color
                            ),

                        Margin =
                            new Thickness(
                                0,
                                PillGap,
                                0,
                                PillGap
                            ),

                        Tag =
                            note.Id,

                        IsHitTestVisible =
                            false
                    };


                IndicatorStack.Children.Add(
                    pill
                );
            }


            /*
             * Rail height remains based on maximum five pills.
             */
            double requiredHeight =
                Math.Max(
                    RailHeight,
                    12 +
                    visibleCount *
                    (
                        PillHeight +
                        PillGap * 2
                    )
                );


            Rail.Height =
                requiredHeight;


            double indicatorHeight =
                visibleCount *
                (
                    PillHeight +
                    PillGap * 2
                );


            Canvas.SetTop(
                IndicatorStack,
                RailTop +
                Math.Max(
                    5,
                    (
                        requiredHeight -
                        indicatorHeight
                    ) / 2
                )
            );
        }


        // ============================================================
        // FAN OUT
        // ============================================================

        private void FanOutTabs()
        {
            _isAnimating =
                true;


            _expandedTab =
                null;


            _moreTab =
                null;


            TabCanvas.Children.Clear();


            HideRestingRail();


            int visibleCount =
                Math.Min(
                    MaxVisibleCards,
                    _notes.Count
                );


            // ========================================================
            // CALCULATE THE ACTUAL CENTERED DECK POSITION
            // ========================================================

            double fanStep = 50;


            double fanHeight =
                visibleCount > 0
                    ? TabHeight +
                      (visibleCount - 1) *
                      fanStep
                    : 0;


            _centeredStartTop =
                (
                    WindowHeight -
                    fanHeight
                ) / 2.0;


            // ========================================================
            // CREATE THE FIVE REAL TABS
            // ========================================================

            for (
                int i = 0;
                i < visibleCount;
                i++)
            {
                Note note =
                    _notes[i];


                Border tab =
                    CreateNoteTab(
                        note
                    );


                /*
                 * Every tab has exactly the same X.
                 */
                Canvas.SetLeft(
                    tab,
                    TabLeft
                );


                /*
                 * Every tab starts from the SAME CENTERED
                 * starting position.
                 */
                Canvas.SetTop(
                    tab,
                    _centeredStartTop
                );


                /*
                 * Later cards are above earlier cards.
                 */
                Panel.SetZIndex(
                    tab,
                    200 + i
                );


                tab.Opacity =
                    0;


                TabCanvas.Children.Add(
                    tab
                );


                AnimateFanIn(
                    tab,
                    i
                );
            }


            // ========================================================
            // MORE THAN FIVE
            // ========================================================

            if (_notes.Count >
                MaxVisibleCards)
            {
                CreateMoreStackedTab(
                    _notes.Count -
                    MaxVisibleCards,

                    visibleCount
                );
            }


            // ========================================================
            // ADD BUTTON
            // ========================================================

            double addButtonTop;


            /*
             * With no notes there is no deck to sit below,
             * so centre + on the resting rail instead of
             * hanging it off an empty deck.
             */
            if (visibleCount <= 0)
            {
                addButtonTop =
                    RailTop +
                    (RailHeight - AddButtonSize) / 2.0;
            }
            else
            {
                /*
                 * Calculate from the SAME centered deck geometry.
                 */
                double deckBottom =
                    GetFiveCardDeckBottom(
                        visibleCount
                    );


                /*
                 * If +N exists, extend the deck to include it.
                 */
                if (_moreTab != null)
                {
                    double moreTop =
                        Canvas.GetTop(
                            _moreTab
                        );


                    if (double.IsNaN(moreTop))
                    {
                        moreTop =
                            GetMoreTabTop();
                    }


                    deckBottom =
                        Math.Max(
                            deckBottom,
                            moreTop +
                            MoreHeight
                        );
                }


                /*
                 * + lives immediately below the deck.
                 */
                addButtonTop =
                    deckBottom + 8;
            }


            Canvas.SetLeft(
                AddButton,
                AddButtonLeft
            );


            Canvas.SetTop(
                AddButton,
                addButtonTop
            );


            /*
             * + is visible only in Stage 2.
             */
            ShowAddButton();


            // ========================================================
            // FAN ANIMATION COMPLETE
            // ========================================================

            DispatcherTimer timer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            FanDurationMs +
                            visibleCount *
                            FanStaggerMs +
                            40
                        )
                };


            timer.Tick +=
                (_, _) =>
                {
                    timer.Stop();

                    _isAnimating =
                        false;
                };


            timer.Start();
        }


        // ============================================================
        // ACTUAL FIVE-CARD DECK BOTTOM
        // ============================================================

        private double GetFiveCardDeckBottom(
            int visibleCount)
        {
            if (visibleCount <= 0)
            {
                return _centeredStartTop;
            }


            double lastCardTop =
                _centeredStartTop +
                (
                    visibleCount - 1
                ) *
                50;


            return
                lastCardTop +
                TabHeight;
        }


        // ============================================================
        // FAN ANIMATION
        // ============================================================

        private void AnimateFanIn(
    Border tab,
    int index)
        {
            double targetTop =
                _centeredStartTop +
                index * 50;

            int delay =
                index *
                FanStaggerMs;


            // ------------------------------------------------------------
            // START AT THE EDGE
            // ------------------------------------------------------------

            /*
             * The card begins slightly inside the screen edge.
             *
             * Since the final X is TabLeft, we start at RightEdge
             * and slide LEFT into position.
             */
            Canvas.SetLeft(
                tab,
                RightEdge
            );


            Canvas.SetTop(
                tab,
                targetTop
            );


            // ------------------------------------------------------------
            // SLIDE IN FROM RIGHT EDGE
            // ------------------------------------------------------------

            tab.BeginAnimation(
                Canvas.LeftProperty,

                new DoubleAnimation
                {
                    From =
                        RightEdge,

                    To =
                        TabLeft,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            delay
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            FanDurationMs
                        ),

                    EasingFunction =
                        EaseOutQuint()
                }
            );


            // ------------------------------------------------------------
            // SLIGHT OPACITY FADE
            // ------------------------------------------------------------

            tab.BeginAnimation(
                UIElement.OpacityProperty,

                new DoubleAnimation
                {
                    From = 0,

                    To = 1,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            delay
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            160
                        ),

                    EasingFunction =
                        EaseOutQuint()
                }
            );
        }


        // ============================================================
        // MORE NOTES STACK
        // ============================================================

        private void CreateMoreStackedTab(
            int extraCount,
            int visibleCount)
        {
            /*
             * IMPORTANT:
             *
             * +N is positioned relative to the CENTERED deck.
             */
            double top =
                GetMoreTabTop();


            Border stack =
                new Border
                {
                    Width =
                        TabWidth,

                    Height =
                        MoreHeight,

                    Background =
                        new SolidColorBrush(
                            Color.FromRgb(
                                226,
                                226,
                                226
                            )
                        ),

                    CornerRadius =
                        new CornerRadius(
                            12,
                            0,
                            0,
                            12
                        ),

                    Cursor =
                        Cursors.Hand,

                    Tag =
                        extraCount,

                    Opacity =
                        0,

                    ToolTip =
                        $"{extraCount} more notes"
                };


            Canvas.SetLeft(
                stack,
                MoreLeft
            );


            Canvas.SetTop(
                stack,
                top
            );


            Panel.SetZIndex(
                stack,
                190
            );


            Grid content =
                new Grid();


            TextBlock label =
                new TextBlock
                {
                    Text =
                        $"+{extraCount}",

                    FontFamily =
                        new FontFamily(
                            "Segoe UI"
                        ),

                    FontSize = 9,

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                75,
                                75,
                                75
                            )
                        ),

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    IsHitTestVisible =
                        false
                };


            content.Children.Add(
                label
            );


            stack.Child =
                content;


            stack.MouseLeftButtonUp +=
                MoreTab_Click;


            TabCanvas.Children.Add(
                stack
            );


            _moreTab =
                stack;


            stack.BeginAnimation(
                UIElement.OpacityProperty,

                new DoubleAnimation
                {
                    From =
                        0,

                    To =
                        1,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            visibleCount *
                            FanStaggerMs
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            160
                        ),

                    EasingFunction =
                        EaseOutQuint()
                }
            );
        }


        // ============================================================
        // MORE TAB POSITION
        // ============================================================

        private double GetMoreTabTop()
        {
            int visibleCount =
                Math.Min(
                    MaxVisibleCards,
                    _notes.Count
                );


            if (visibleCount <= 0)
            {
                return _centeredStartTop;
            }


            double lastCardTop =
                _centeredStartTop +
                (
                    visibleCount - 1
                ) *
                50;


            /*
             * Put +N immediately after the actual bottom
             * of the fifth overlapping card.
             */
            return
                lastCardTop +
                TabHeight -
                8;
        }


        private void MoreTab_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!_isOpen ||
                _isAnimating)
            {
                return;
            }


            OpenAllNotesWindow();


            e.Handled =
                true;
        }


        // ============================================================
        // NOTE TAB
        // ============================================================

        private Border CreateNoteTab(
            Note note)
        {
            Border tab =
                new Border
                {
                    Width =
                        TabWidth,

                    Height =
                        TabHeight,

                    Background =
                        CreateBrush(
                            note.Color
                        ),

                    CornerRadius =
                        new CornerRadius(
                            12,
                            0,
                            0,
                            12
                        ),

                    Tag =
                        note.Id,

                    Cursor =
                        Cursors.Hand,

                    SnapsToDevicePixels =
                        true
                };


            tab.Effect =
                new System.Windows.Media.Effects
                    .DropShadowEffect
                {
                    BlurRadius = 16,

                    ShadowDepth = 3,

                    Direction = 270,

                    Opacity = 0.16
                };


            Grid content =
                new Grid();


            // ========================================================
            // VERTICAL TITLE
            // ========================================================

            TextBlock label =
                new TextBlock
                {
                    Text =
                        note.Title
                            .ToUpperInvariant(),

                    FontFamily =
                        new FontFamily(
                            "Segoe UI"
                        ),

                    FontSize = 13,

                    FontWeight =
                        FontWeights.SemiBold,

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                78,
                                78,
                                78
                            )
                        ),

                    HorizontalAlignment =
                        HorizontalAlignment.Left,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    TextAlignment =
                        TextAlignment.Center,

                    Margin =
                        new Thickness(
                            5,
                            0,
                            0,
                            0
                        ),

                    /*
                     * Cap the heading length so that after the
                     * -90 degree rotation it does not run the full
                     * tab height. Combined with VerticalAlignment.Center
                     * this leaves symmetric padding at the top and
                     * bottom, and long titles trim with an ellipsis
                     * instead of touching the edges.
                     */
                    MaxWidth =
                        TabHeight -
                        (2 * TabHeadingVerticalPadding),

                    TextTrimming =
                        TextTrimming.CharacterEllipsis,

                    IsHitTestVisible =
                        false
                };


            /*
             * Your current corrected orientation.
             */
            label.LayoutTransform =
                new RotateTransform(
                    -90
                );


            content.Children.Add(
                label
            );


            // ========================================================
            // STAGE 3 PREVIEW
            // ========================================================

            Grid preview =
                CreateExpandedPreview(
                    note
                );


            preview.Visibility =
                Visibility.Collapsed;


            preview.Opacity =
                0;


            content.Children.Add(
                preview
            );


            tab.Child =
                content;


            // ========================================================
            // HOVER ENTER
            // ========================================================

            tab.MouseEnter +=
                (_, _) =>
                {
                    TryExpandFromPointer(
                        tab,
                        label,
                        preview
                    );
                };


            // ========================================================
            // MOUSE MOVE
            // ========================================================

            tab.MouseMove +=
                (_, _) =>
                {
                    TryExpandFromPointer(
                        tab,
                        label,
                        preview
                    );
                };


            // ========================================================
            // HOVER LEAVE
            // ========================================================

            tab.MouseLeave +=
                (_, _) =>
                {
                    CollapseTab(
                        tab,
                        label,
                        preview
                    );
                };


            // ========================================================
            // CLICK
            // ========================================================

            tab.MouseLeftButtonUp +=
                NoteTab_Click;


            return tab;
        }


        // ============================================================
        // STAGE 3 PREVIEW
        // ============================================================

        private Grid CreateExpandedPreview(
            Note note)
        {
            Grid preview =
                new Grid
                {
                    Width =
                        ExpandedWidth,

                    Height =
                        ExpandedHeight,

                    ClipToBounds =
                        true,

                    IsHitTestVisible =
                        false
                };


            Border paper =
                new Border
                {
                    Width =
                        ExpandedWidth,

                    Height =
                        ExpandedHeight,

                    Background =
                        CreateBrush(
                            note.Color
                        ),

                    CornerRadius =
                        new CornerRadius(
                            26,
                            0,
                            0,
                            26
                        ),

                    HorizontalAlignment =
                        HorizontalAlignment.Left,

                    VerticalAlignment =
                        VerticalAlignment.Top,

                    SnapsToDevicePixels =
                        true,

                    Effect =
                        new System.Windows.Media.Effects
                            .DropShadowEffect
                        {
                            BlurRadius = 14,

                            ShadowDepth = 3,

                            Direction = 270,

                            Opacity = 0.16
                        }
                };


            Grid paperGrid =
                new Grid();


            paperGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            58
                        )
                }
            );


            paperGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width =
                        new GridLength(
                            1,
                            GridUnitType.Star
                        )
                }
            );


            // ========================================================
            // VERTICAL CATEGORY STRIP
            // ========================================================

            Grid strip =
                new Grid();


            TextBlock verticalCategory =
                new TextBlock
                {
                    Text =
                        note.Title
                            .ToUpperInvariant(),

                    FontFamily =
                        new FontFamily(
                            "Segoe UI"
                        ),

                    FontSize = 12,

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                76,
                                76,
                                76
                            )
                        ),

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    TextAlignment =
                        TextAlignment.Center,

                    TextTrimming =
                        TextTrimming
                            .CharacterEllipsis,

                    MaxWidth = 150,

                    IsHitTestVisible =
                        false
                };


            verticalCategory.LayoutTransform =
                new RotateTransform(
                    -90
                );


            strip.Children.Add(
                verticalCategory
            );


            // ========================================================
            // PERFORATION
            // ========================================================

            Rectangle perforation =
                new Rectangle
                {
                    Width = 1,

                    HorizontalAlignment =
                        HorizontalAlignment.Right,

                    VerticalAlignment =
                        VerticalAlignment.Stretch,

                    Margin =
                        new Thickness(
                            0,
                            16,
                            0,
                            16
                        ),

                    Stroke =
                        new SolidColorBrush(
                            Color.FromArgb(
                                72,
                                80,
                                80,
                                80
                            )
                        ),

                    StrokeThickness = 1,

                    StrokeDashArray =
                        new DoubleCollection
                        {
                            9,
                            8
                        },

                    IsHitTestVisible =
                        false
                };


            strip.Children.Add(
                perforation
            );


            Grid.SetColumn(
                strip,
                0
            );


            paperGrid.Children.Add(
                strip
            );


            // ========================================================
            // MAIN CONTENT
            // ========================================================

            Grid main =
                new Grid
                {
                    Margin =
                        new Thickness(
                            10,
                            24,
                            18,
                            18
                        )
                };


            main.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        GridLength.Auto
                }
            );


            main.RowDefinitions.Add(
                new RowDefinition
                {
                    Height =
                        new GridLength(
                            1,
                            GridUnitType.Star
                        )
                }
            );


            // ========================================================
            // TITLE
            // ========================================================

            TextBlock title =
                new TextBlock
                {
                    Text =
                        note.Title,

                    FontFamily =
                        new FontFamily(
                            "Segoe UI"
                        ),

                    FontSize = 18,

                    FontWeight =
                        FontWeights.SemiBold,

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                35,
                                35,
                                35
                            )
                        ),

                    TextTrimming =
                        TextTrimming
                            .CharacterEllipsis,

                    IsHitTestVisible =
                        false
                };


            Grid.SetRow(
                title,
                0
            );


            main.Children.Add(
                title
            );


            // ========================================================
            // BODY
            // ========================================================

            Grid bodyHost =
                new Grid
                {
                    Margin =
                        new Thickness(
                            0,
                            8,
                            0,
                            0
                        )
                };


            TextBlock body =
                new TextBlock
                {
                    Text =
                        note.Content ??
                        string.Empty,

                    /*
                     * Use the handwriting font chosen in Settings so the
                     * rail preview matches the note windows.
                     */
                    FontFamily =
                        new FontFamily(
                            AppSettings.NoteFontSource
                        ),

                    FontSize = 19,

                    LineHeight = 29,

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                48,
                                48,
                                48
                            )
                        ),

                    TextWrapping =
                        TextWrapping.Wrap,

                    IsHitTestVisible =
                        false
                };


            ScrollViewer bodyScroller =
                new ScrollViewer
                {
                    VerticalScrollBarVisibility =
                        ScrollBarVisibility.Hidden,

                    HorizontalScrollBarVisibility =
                        ScrollBarVisibility.Disabled,

                    CanContentScroll =
                        false,

                    IsHitTestVisible =
                        false,

                    Content =
                        body
                };


            bodyHost.Children.Add(
                bodyScroller
            );


            // ========================================================
            // COLOR-MATCHED FADE
            // ========================================================

            Color baseColor =
                GetColor(
                    note.Color
                );


            LinearGradientBrush fade =
                new LinearGradientBrush
                {
                    StartPoint =
                        new Point(
                            0,
                            0
                        ),

                    EndPoint =
                        new Point(
                            0,
                            1
                        )
                };


            fade.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(
                        0,
                        baseColor.R,
                        baseColor.G,
                        baseColor.B
                    ),
                    0
                )
            );


            fade.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(
                        35,
                        baseColor.R,
                        baseColor.G,
                        baseColor.B
                    ),
                    0.35
                )
            );


            fade.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(
                        190,
                        baseColor.R,
                        baseColor.G,
                        baseColor.B
                    ),
                    0.78
                )
            );


            fade.GradientStops.Add(
                new GradientStop(
                    Color.FromArgb(
                        255,
                        baseColor.R,
                        baseColor.G,
                        baseColor.B
                    ),
                    1
                )
            );


            bodyHost.Children.Add(
                new Border
                {
                    Height = 34,

                    VerticalAlignment =
                        VerticalAlignment.Bottom,

                    Background =
                        fade,

                    IsHitTestVisible =
                        false
                }
            );


            Grid.SetRow(
                bodyHost,
                1
            );


            main.Children.Add(
                bodyHost
            );


            Grid.SetColumn(
                main,
                1
            );


            paperGrid.Children.Add(
                main
            );


            paper.Child =
                paperGrid;


            preview.Children.Add(
                paper
            );


            return preview;
        }


        // ============================================================
        // STAGE 3 HOVER
        // ============================================================

        private void TryExpandFromPointer(
            Border tab,
            TextBlock label,
            Grid preview)
        {
            if (!_isOpen)
                return;


            if (_isAnimating)
                return;


            /*
             * Literal screen edge is never the Stage 3 trigger.
             */
            if (IsCursorOnPhysicalScreenEdge())
                return;


            ExpandTab(
                tab,
                label,
                preview
            );
        }


        private void ExpandTab(
            Border tab,
            TextBlock label,
            Grid preview)
        {
            if (_expandedTab == tab)
            {
                return;
            }


            /*
             * Only one note can be expanded.
             */
            if (_expandedTab != null)
            {
                CollapseCurrentlyExpanded();
            }


            _expandedTab =
                tab;


            Panel.SetZIndex(
                tab,
                1000
            );


            /*
             * Right edge remains fixed.
             *
             * ExpandedLeft + ExpandedWidth = RightEdge
             *
             * No From values: if the tab is mid-collapse the
             * animations blend from where it actually is
             * instead of snapping to a stale base value.
             */
            DoubleAnimation leftAnimation =
                new DoubleAnimation
                {
                    To =
                        ExpandedLeft,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseOutQuint()
                };


            DoubleAnimation widthAnimation =
                new DoubleAnimation
                {
                    To =
                        ExpandedWidth,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseOutQuint()
                };


            DoubleAnimation heightAnimation =
                new DoubleAnimation
                {
                    To =
                        ExpandedHeight,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseOutQuint()
                };


            tab.BeginAnimation(
                Canvas.LeftProperty,
                leftAnimation
            );


            tab.BeginAnimation(
                FrameworkElement.WidthProperty,
                widthAnimation
            );


            tab.BeginAnimation(
                FrameworkElement.HeightProperty,
                heightAnimation
            );


            AnimateOpacity(
                label,
                1,
                0,
                75
            );


            preview.Visibility =
                Visibility.Visible;


            preview.BeginAnimation(
                UIElement.OpacityProperty,

                new DoubleAnimation
                {
                    From = 0,

                    To = 1,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            65
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            170
                        ),

                    EasingFunction =
                        EaseOutQuint()
                }
            );
        }


        // ============================================================
        // COLLAPSE EXPANDED NOTE
        // ============================================================

        private void CollapseTab(
            Border tab,
            TextBlock label,
            Grid preview)
        {
            if (_expandedTab != tab)
            {
                return;
            }


            CollapseTabInternal(
                tab,
                label,
                preview
            );


            _expandedTab =
                null;
        }


        private void CollapseCurrentlyExpanded()
        {
            if (_expandedTab == null)
            {
                return;
            }


            if (_expandedTab.Child
                is not Grid grid)
            {
                return;
            }


            TextBlock? label = null;

            Grid? preview = null;


            foreach (
                UIElement child
                in grid.Children)
            {
                if (child is TextBlock text)
                {
                    label = text;
                }
                else if (child is Grid nested)
                {
                    preview = nested;
                }
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


            _expandedTab =
                null;
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
                85
            );


            DoubleAnimation left =
                new DoubleAnimation
                {
                    To =
                        TabLeft,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseInOutQuint()
                };


            DoubleAnimation width =
                new DoubleAnimation
                {
                    To =
                        TabWidth,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseInOutQuint()
                };


            DoubleAnimation height =
                new DoubleAnimation
                {
                    To =
                        TabHeight,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            HoverDurationMs
                        ),

                    EasingFunction =
                        EaseInOutQuint()
                };


            left.Completed +=
                (_, _) =>
                {
                    /*
                     * The tab was re-expanded while this collapse
                     * was still running. Leave the fresh expand
                     * alone, otherwise the tab flickers back to
                     * the stacked state.
                     */
                    if (_expandedTab == tab)
                    {
                        return;
                    }


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
        // COLLAPSE EVERYTHING
        // ============================================================

        private void CollapseTabs()
        {
            _isAnimating =
                true;


            _expandedTab =
                null;


            foreach (
                UIElement element
                in TabCanvas.Children)
            {
                if (element is not Border tab)
                {
                    continue;
                }


                tab.BeginAnimation(
                    FrameworkElement.WidthProperty,
                    null
                );


                tab.BeginAnimation(
                    FrameworkElement.HeightProperty,
                    null
                );


                /*
                 * All cards return directly toward the edge.
                 */
                tab.BeginAnimation(
                    Canvas.LeftProperty,

                    new DoubleAnimation
                    {
                        To =
                            RightEdge,

                        Duration =
                            TimeSpan.FromMilliseconds(
                                FanDurationMs
                            ),

                        EasingFunction =
                            EaseIn()
                    }
                );


                tab.BeginAnimation(
                    UIElement.OpacityProperty,

                    new DoubleAnimation
                    {
                        To = 0,

                        Duration =
                            TimeSpan.FromMilliseconds(
                                150
                            ),

                        EasingFunction =
                            EaseIn()
                    }
                );
            }


            /*
             * + disappears when Stage 2 closes.
             */
            HideAddButton();


            DispatcherTimer timer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            FanDurationMs + 30
                        )
                };


            timer.Tick +=
                (_, _) =>
                {
                    timer.Stop();


                    TabCanvas.Children.Clear();


                    _moreTab =
                        null;


                    ShowRestingRail();


                    _isAnimating =
                        false;
                };


            timer.Start();
        }


        // ============================================================
        // RESTING RAIL VISIBILITY
        // ============================================================

        private void HideRestingRail()
        {
            Rail.Visibility =
                Visibility.Visible;


            IndicatorStack.Visibility =
                Visibility.Visible;


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


            _expandedTab =
                null;


            _moreTab =
                null;


            Rail.Visibility =
                Visibility.Visible;


            IndicatorStack.Visibility =
                Visibility.Visible;


            Rail.Opacity =
                1;


            IndicatorStack.Opacity =
                1;


            /*
             * No + in Stage 1.
             */
            HideAddButton();
        }


        // ============================================================
        // ADD BUTTON
        // ============================================================

        private void ShowAddButton()
        {
            /*
             * Only set X here.
             *
             * FanOutTabs() owns the Y coordinate.
             */
            Canvas.SetLeft(
                AddButton,
                AddButtonLeft
            );


            AddButton.Visibility =
                Visibility.Visible;


            /*
             * Soft fade instead of an instant pop-in.
             */
            AnimateOpacity(
                AddButton,
                0,
                1,
                170
            );
        }


        private void HideAddButton()
        {
            AddButton.Visibility =
                Visibility.Collapsed;
        }


        private double GetCurrentAddButtonTop()
        {
            int visibleCount =
                Math.Min(
                    MaxVisibleCards,
                    _notes.Count
                );


            /*
             * Mirror the fan-out placement exactly so the
             * hover hit-test matches what is on screen.
             */
            if (visibleCount <= 0)
            {
                return
                    RailTop +
                    (RailHeight - AddButtonSize) / 2.0;
            }


            double deckBottom =
                GetFiveCardDeckBottom(
                    visibleCount
                );


            if (_moreTab != null)
            {
                double moreTop =
                    Canvas.GetTop(
                        _moreTab
                    );


                if (!double.IsNaN(moreTop))
                {
                    deckBottom =
                        Math.Max(
                            deckBottom,
                            moreTop +
                            MoreHeight
                        );
                }
            }


            return deckBottom + 8;
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
        // ALL NOTES
        // ============================================================

        private void OpenAllNotesWindow()
        {
            AllNotesWindow window =
                new AllNotesWindow();


            window.NotesChanged +=
                AllNotesWindow_NotesChanged;


            window.Show();


            window.Activate();
        }


        private void AllNotesWindow_NotesChanged(
            object? sender,
            EventArgs e)
        {
            ReloadNotes();
        }


        // ============================================================
        // NOTE CLICK / EDIT
        // ============================================================

        private void NoteTab_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Border tab)
            {
                return;
            }


            int noteId;


            if (tab.Tag is int intId)
            {
                noteId =
                    intId;
            }
            else if (tab.Tag is long longId)
            {
                noteId =
                    (int)longId;
            }
            else
            {
                return;
            }


            OpenNoteEditor(
                noteId
            );


            e.Handled =
                true;
        }


        private void OpenNoteEditor(
            int noteId)
        {
            Note? note =
                _noteRepository.GetNote(
                    noteId
                );


            if (note == null)
            {
                return;
            }


            NoteWindow window =
                new NoteWindow(
                    note
                );


            bool? result =
                window.ShowDialog();


            if (result == true)
            {
                ReloadNotes();
            }
        }


        // ============================================================
        // ANIMATION HELPERS
        // ============================================================

        private static void AnimateOpacity(
            UIElement element,
            double from,
            double to,
            int duration)
        {
            element.BeginAnimation(
                UIElement.OpacityProperty,

                new DoubleAnimation
                {
                    From =
                        from,

                    To =
                        to,

                    Duration =
                        TimeSpan.FromMilliseconds(
                            duration
                        ),

                    EasingFunction =
                        EaseInOutQuint()
                }
            );
        }


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


        /*
         * Quintic curves decelerate more gently than the
         * cubic ones, which is what makes UI motion feel
         * "soft" rather than snappy.
         */
        private static QuinticEase EaseOutQuint()
        {
            return new QuinticEase
            {
                EasingMode =
                    EasingMode.EaseOut
            };
        }


        private static QuinticEase EaseInOutQuint()
        {
            return new QuinticEase
            {
                EasingMode =
                    EasingMode.EaseInOut
            };
        }


        // ============================================================
        // COLOR
        // ============================================================

        private static Color GetColor(
            string color)
        {
            object? value =
                ColorConverter
                    .ConvertFromString(
                        color
                    );


            return value is Color parsed
                ? parsed
                : Color.FromRgb(
                    255,
                    215,
                    90
                );
        }


        private static SolidColorBrush CreateBrush(
            string color)
        {
            return new SolidColorBrush(
                GetColor(color)
            );
        }


        // ============================================================
        // TIMER
        // ============================================================

        private void CollapseTimer_Tick(
            object? sender,
            EventArgs e)
        {
            _collapseTimer.Stop();


            if (IsMouseOver)
            {
                return;
            }


            CloseRail();
        }


        // ============================================================
        // WINDOWS API
        // ============================================================

        private const int SM_XVIRTUALSCREEN = 76;

        private const int SM_CXVIRTUALSCREEN = 78;


        [DllImport(
            "user32.dll"
        )]
        private static extern int GetSystemMetrics(
            int nIndex
        );


        [DllImport(
            "user32.dll"
        )]
        private static extern bool GetCursorPos(
            out POINT lpPoint
        );


        [StructLayout(
            LayoutKind.Sequential
        )]
        private struct POINT
        {
            public int X;

            public int Y;
        }
    }
}
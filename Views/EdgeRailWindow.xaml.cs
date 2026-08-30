using HoldMyNotes.Data;
using HoldMyNotes.Models;
using System;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HoldMyNotes.Views
{
    public partial class EdgeRailWindow : Window
    {
        // ============================================================
        // LAYOUT
        // ============================================================

        private const double WindowWidth = 220;

        private const double RailLeft = 202;

        private const double RailTop = 95;

        private const double RailWidth = 18;

        private const double RailHeight = 112;

        /*
         * CLOSED:
         *
         * Cards are completely outside the visible window.
         *
         * Window ends at x = 220.
         */

        private const double ClosedCardLeft = 220;


        /*
         * OPEN:
         *
         * Cards slide into the window.
         */

        private const double OpenCardLeft = 50;


        private const double CardWidth = 145;

        private const double CardHeight = 72;

        private const double CardSpacing = 8;


        // ============================================================
        // ANIMATION
        // ============================================================

        private const int CardAnimationDuration = 220;

        private const int RailAnimationDuration = 130;


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


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public EdgeRailWindow()
        {
            InitializeComponent();


            _noteRepository =
                new NoteRepository();


            LoadNotes();


            BuildCards();


            BuildIndicators();


            SetClosedState();


            AddButton.Click +=
                AddButton_Click;


            PositionWindow();
        }


        // ============================================================
        // POSITION
        // ============================================================

        private void PositionWindow()
        {
            Left =
                SystemParameters.WorkArea.Right -
                Width;


            Top =
                (SystemParameters.WorkArea.Height -
                 Height) / 2;
        }


        // ============================================================
        // PUBLIC OPEN
        // ============================================================

        public void Open()
        {
            if (_isAnimating)
            {
                return;
            }


            if (_isOpen)
            {
                return;
            }


            _isOpen = true;


            AnimateOpen();
        }


        // ============================================================
        // PUBLIC CLOSE
        // ============================================================

        public void CloseRail()
        {
            if (_isAnimating)
            {
                return;
            }


            if (!_isOpen)
            {
                return;
            }


            _isOpen = false;


            AnimateClose();
        }


        // ============================================================
        // MOUSE AREA
        // ============================================================

        public bool IsMouseInsideVisibleArea(
            int screenX,
            int screenY)
        {
            Point point =
                PointFromScreen(
                    new Point(
                        screenX,
                        screenY
                    )
                );


            // --------------------------------------------------------
            // Rail
            // --------------------------------------------------------

            Rect railRect =
                new Rect(
                    RailLeft,
                    RailTop,
                    RailWidth,
                    RailHeight
                );


            if (railRect.Contains(point))
            {
                return true;
            }


            // --------------------------------------------------------
            // Add button
            // --------------------------------------------------------

            Rect addButtonRect =
                new Rect(
                    198,
                    220,
                    24,
                    24
                );


            if (addButtonRect.Contains(point))
            {
                return true;
            }


            // --------------------------------------------------------
            // Cards
            // --------------------------------------------------------

            if (_isOpen)
            {
                for (int i = 0;
                     i < _notes.Count;
                     i++)
                {
                    double top =
                        i *
                        (CardHeight +
                         CardSpacing);


                    Rect cardRect =
                        new Rect(
                            OpenCardLeft,
                            top,
                            CardWidth,
                            CardHeight
                        );


                    if (cardRect.Contains(point))
                    {
                        return true;
                    }
                }
            }


            return false;
        }


        // ============================================================
        // LOAD NOTES
        // ============================================================

        private void LoadNotes()
        {
            _notes =
                _noteRepository.GetAllNotes();
        }


        // ============================================================
        // RELOAD
        // ============================================================

        private void ReloadNotes()
        {
            LoadNotes();


            BuildCards();


            BuildIndicators();


            if (_isOpen)
            {
                SetOpenState();
            }
            else
            {
                SetClosedState();
            }
        }


        // ============================================================
        // BUILD CARDS
        // ============================================================

        private void BuildCards()
        {
            /*
             * Remove only the dynamically created cards.
             *
             * Keep:
             *
             * Rail
             * IndicatorStack
             * AddButton
             */

            List<UIElement> oldCards =
                new List<UIElement>();


            foreach (UIElement child
                     in TabCanvas.Children)
            {
                if (child is Border)
                {
                    oldCards.Add(child);
                }
            }


            foreach (UIElement card
                     in oldCards)
            {
                TabCanvas.Children.Remove(card);
            }


            // --------------------------------------------------------
            // Create cards
            // --------------------------------------------------------

            for (int i = 0;
                 i < _notes.Count;
                 i++)
            {
                Note note =
                    _notes[i];


                Border card =
                    CreateCard(
                        note,
                        i
                    );


                /*
                 * IMPORTANT:
                 *
                 * Closed cards are completely outside the
                 * visible window.
                 */

                Canvas.SetLeft(
                    card,
                    ClosedCardLeft
                );


                Canvas.SetTop(
                    card,
                    i *
                    (CardHeight +
                     CardSpacing)
                );


                /*
                 * Cards are behind the rail.
                 */

                Panel.SetZIndex(
                    card,
                    10
                );


                TabCanvas.Children.Add(
                    card
                );
            }
        }


        // ============================================================
        // CREATE CARD
        // ============================================================

        private Border CreateCard(
            Note note,
            int index)
        {
            Border card =
                new Border
                {
                    Width =
                        CardWidth,

                    Height =
                        CardHeight,

                    CornerRadius =
                        new CornerRadius(14),

                    Background =
                        CreateBrush(
                            note.Color
                        ),

                    Tag =
                        index,

                    Cursor =
                        Cursors.Hand
                };


            card.Effect =
                new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 8,

                    ShadowDepth = 2,

                    Opacity = 0.18
                };


            StackPanel content =
                new StackPanel
                {
                    Margin =
                        new Thickness(
                            14,
                            9,
                            10,
                            8
                        )
                };


            TextBlock title =
                new TextBlock
                {
                    Text =
                        note.Title,

                    FontSize =
                        15,

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
                        TextTrimming.CharacterEllipsis
                };


            content.Children.Add(
                title
            );


            string preview =
                (note.Content ?? "")
                    .Replace(
                        Environment.NewLine,
                        " "
                    )
                    .Replace(
                        "\n",
                        " "
                    )
                    .Trim();


            TextBlock body =
                new TextBlock
                {
                    Text =
                        preview,

                    FontSize =
                        12,

                    Foreground =
                        new SolidColorBrush(
                            Color.FromRgb(
                                70,
                                70,
                                70
                            )
                        ),

                    Margin =
                        new Thickness(
                            0,
                            4,
                            0,
                            0
                        ),

                    TextTrimming =
                        TextTrimming.CharacterEllipsis
                };


            content.Children.Add(
                body
            );


            card.Child =
                content;


            card.MouseLeftButtonUp +=
                NoteCard_Click;


            return card;
        }


        // ============================================================
        // BUILD DYNAMIC PILLS
        // ============================================================

        private void BuildIndicators()
        {
            IndicatorStack.Children.Clear();


            /*
             * IMPORTANT:
             *
             * DO NOT LIMIT THIS TO FOUR.
             *
             * Every actual note gets a pill.
             */

            for (int i = 0;
                 i < _notes.Count;
                 i++)
            {
                Note note =
                    _notes[i];


                Border pill =
                    new Border
                    {
                        Width = 7,

                        Height = 18,

                        CornerRadius =
                            new CornerRadius(4),

                        Background =
                            CreateBrush(
                                note.Color
                            ),

                        Margin =
                            new Thickness(
                                0,
                                2,
                                0,
                                2
                            ),

                        Tag =
                            note.Id,

                        Cursor =
                            Cursors.Hand
                    };


                pill.MouseLeftButtonUp +=
                    Indicator_Click;


                IndicatorStack.Children.Add(
                    pill
                );
            }


            /*
             * Make the rail tall enough to contain
             * all pills.
             *
             * Minimum = reference size.
             */

            double requiredHeight =
                Math.Max(
                    RailHeight,
                    (_notes.Count * 22) + 8
                );


            Rail.Height =
                requiredHeight;


            IndicatorStack.Height =
                requiredHeight - 8;
        }


        // ============================================================
        // INDICATOR CLICK
        // ============================================================

        private void Indicator_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Border pill)
            {
                return;
            }


            if (pill.Tag is not int noteId)
            {
                return;
            }


            Note? note =
                _noteRepository.GetNote(
                    noteId
                );


            if (note == null)
            {
                return;
            }


            OpenNoteWindow(
                note
            );
        }


        // ============================================================
        // COLOR
        // ============================================================

        private SolidColorBrush CreateBrush(
            string color)
        {
            object? value =
                ColorConverter.ConvertFromString(
                    color
                );


            if (value is Color parsed)
            {
                return new SolidColorBrush(
                    parsed
                );
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
        // CLOSED STATE
        // ============================================================

        private void SetClosedState()
        {
            foreach (UIElement child
                     in TabCanvas.Children)
            {
                if (child is not Border card)
                {
                    continue;
                }


                /*
                 * Completely outside the window.
                 */

                Canvas.SetLeft(
                    card,
                    ClosedCardLeft
                );


                card.Opacity =
                    1;
            }


            /*
             * Grey rail visible.
             */

            Rail.Opacity =
                1;


            /*
             * Colored pills visible.
             */

            IndicatorStack.Opacity =
                1;


            /*
             * Add button visible.
             */

            AddButton.Visibility =
                Visibility.Visible;


            AddButton.Opacity =
                1;
        }


        // ============================================================
        // OPEN STATE
        // ============================================================

        private void SetOpenState()
        {
            foreach (UIElement child
                     in TabCanvas.Children)
            {
                if (child is not Border card)
                {
                    continue;
                }


                Canvas.SetLeft(
                    card,
                    OpenCardLeft
                );


                card.Opacity =
                    1;
            }


            /*
             * Rail and pills hidden.
             */

            Rail.Opacity =
                0;


            IndicatorStack.Opacity =
                0;


            AddButton.Visibility =
                Visibility.Visible;


            AddButton.Opacity =
                1;
        }


        // ============================================================
        // OPEN ANIMATION
        // ============================================================

        private void AnimateOpen()
        {
            _isAnimating =
                true;


            /*
             * Make the cards physically move from completely
             * outside the window to their visible position.
             */

            foreach (UIElement child
                     in TabCanvas.Children)
            {
                if (child is not Border card)
                {
                    continue;
                }


                DoubleAnimation cardAnimation =
                    new DoubleAnimation
                    {
                        From =
                            ClosedCardLeft,

                        To =
                            OpenCardLeft,

                        Duration =
                            TimeSpan.FromMilliseconds(
                                CardAnimationDuration
                            ),

                        EasingFunction =
                            new CubicEase
                            {
                                EasingMode =
                                    EasingMode.EaseOut
                            }
                    };


                card.BeginAnimation(
                    Canvas.LeftProperty,
                    cardAnimation
                );
            }


            /*
             * Rail starts disappearing slightly after
             * the cards begin moving.
             */

            DoubleAnimation railAnimation =
                new DoubleAnimation
                {
                    From = 1,

                    To = 0,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            45
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            RailAnimationDuration
                        ),

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseInOut
                        }
                };


            Rail.BeginAnimation(
                OpacityProperty,
                railAnimation
            );


            DoubleAnimation pillAnimation =
                new DoubleAnimation
                {
                    From = 1,

                    To = 0,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            30
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            RailAnimationDuration
                        ),

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseInOut
                        }
                };


            IndicatorStack.BeginAnimation(
                OpacityProperty,
                pillAnimation
            );


            /*
             * Finish.
             */

            DispatcherTimer timer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            CardAnimationDuration
                        )
                };


            timer.Tick +=
                (_, _) =>
                {
                    timer.Stop();


                    Rail.BeginAnimation(
                        OpacityProperty,
                        null
                    );


                    IndicatorStack.BeginAnimation(
                        OpacityProperty,
                        null
                    );


                    Rail.Opacity =
                        0;


                    IndicatorStack.Opacity =
                        0;


                    _isAnimating =
                        false;
                };


            timer.Start();
        }


        // ============================================================
        // CLOSE ANIMATION
        // ============================================================

        private void AnimateClose()
        {
            _isAnimating =
                true;


            /*
             * FIRST:
             *
             * Cards physically travel back outside
             * the window.
             */

            foreach (UIElement child
                     in TabCanvas.Children)
            {
                if (child is not Border card)
                {
                    continue;
                }


                DoubleAnimation cardAnimation =
                    new DoubleAnimation
                    {
                        From =
                            OpenCardLeft,

                        To =
                            ClosedCardLeft,

                        Duration =
                            TimeSpan.FromMilliseconds(
                                CardAnimationDuration
                            ),

                        EasingFunction =
                            new CubicEase
                            {
                                EasingMode =
                                    EasingMode.EaseInOut
                            }
                    };


                card.BeginAnimation(
                    Canvas.LeftProperty,
                    cardAnimation
                );
            }


            /*
             * THEN:
             *
             * The grey rail returns.
             */

            DoubleAnimation railAnimation =
                new DoubleAnimation
                {
                    From = 0,

                    To = 1,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            105
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            115
                        ),

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseOut
                        }
                };


            Rail.BeginAnimation(
                OpacityProperty,
                railAnimation
            );


            /*
             * Pills return slightly after the rail.
             */

            DoubleAnimation pillAnimation =
                new DoubleAnimation
                {
                    From = 0,

                    To = 1,

                    BeginTime =
                        TimeSpan.FromMilliseconds(
                            125
                        ),

                    Duration =
                        TimeSpan.FromMilliseconds(
                            95
                        ),

                    EasingFunction =
                        new CubicEase
                        {
                            EasingMode =
                                EasingMode.EaseOut
                        }
                };


            IndicatorStack.BeginAnimation(
                OpacityProperty,
                pillAnimation
            );


            /*
             * Finish.
             */

            DispatcherTimer timer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            CardAnimationDuration
                        )
                };


            timer.Tick +=
                (_, _) =>
                {
                    timer.Stop();


                    Rail.BeginAnimation(
                        OpacityProperty,
                        null
                    );


                    IndicatorStack.BeginAnimation(
                        OpacityProperty,
                        null
                    );


                    SetClosedState();


                    _isAnimating =
                        false;
                };


            timer.Start();
        }


        // ============================================================
        // NOTE CARD CLICK
        // ============================================================

        private void NoteCard_Click(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Border card)
            {
                return;
            }


            if (card.Tag is not int index)
            {
                return;
            }


            if (index < 0 ||
                index >= _notes.Count)
            {
                return;
            }


            Note note =
                _notes[index];


            OpenNoteWindow(
                note
            );
        }


        // ============================================================
        // OPEN NOTE WINDOW
        // ============================================================

        private void OpenNoteWindow(
            Note note)
        {
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
        // ADD NOTE
        // ============================================================

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
        // WINDOW
        // ============================================================

        protected override void OnRenderSizeChanged(
            SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(
                sizeInfo
            );


            PositionWindow();
        }


        // ============================================================
        // CLEANUP
        // ============================================================

        protected override void OnClosed(
            EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
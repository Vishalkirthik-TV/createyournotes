using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HoldMyNotes.Models;

namespace HoldMyNotes.Views
{
    public partial class DockWindow : Window
    {
        private const int OpenAnimationDuration = 220;
        private const int CloseAnimationDuration = 160;

        private const double ClosedX = 262;
        private const double OpenX = 220;

        private bool _isShown;

        private readonly List<Note> _notes = new()
        {
            new Note
            {
                Title = "Groceries",
                Content = "Milk, eggs, bananas",
                Color = "#FFFFD95A"
            },

            new Note
            {
                Title = "Work",
                Content = "Finish the project",
                Color = "#FF7CC7FF"
            },

            new Note
            {
                Title = "Ideas",
                Content = "Build something awesome",
                Color = "#FF9BE28F"
            }
        };

        public IReadOnlyList<Note> Notes => _notes;


        public DockWindow()
        {
            InitializeComponent();

            DataContext = this;

            Loaded += DockWindow_Loaded;

            PositionDock();
        }


        private void DockWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    PositionTabs(false);
                }),
                System.Windows.Threading.DispatcherPriority.Loaded
            );
        }


        private void PositionDock()
        {
            Left = SystemParameters.WorkArea.Right - Width;

            Top = (SystemParameters.WorkArea.Height - Height) / 2;
        }


        public void ShowDock()
        {
            if (_isShown)
            {
                return;
            }

            _isShown = true;

            PositionDock();

            if (!IsVisible)
            {
                Show();
            }

            PositionTabs(true);
        }


        public void HideDock()
        {
            if (!_isShown)
            {
                return;
            }

            _isShown = false;

            PositionTabs(false);

            /*
             * Give the tabs enough time to slide back before
             * hiding the window.
             */

            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    if (!_isShown)
                    {
                        Hide();
                    }
                }),
                System.Windows.Threading.DispatcherPriority.Background
            );
        }


        private void PositionTabs(bool animate)
        {
            if (NoteTabs.Items.Count == 0)
            {
                return;
            }

            double centerY = Height / 2.0;

            double tabHeight = 58;

            double spacing = 8;

            double totalHeight =
                NoteTabs.Items.Count * tabHeight +
                (NoteTabs.Items.Count - 1) * spacing;

            double startY =
                centerY - totalHeight / 2.0;


            for (int i = 0; i < NoteTabs.Items.Count; i++)
            {
                ContentPresenter? presenter =
                    NoteTabs.ItemContainerGenerator
                        .ContainerFromIndex(i)
                        as ContentPresenter;

                if (presenter == null)
                {
                    continue;
                }

                double targetY =
                    startY +
                    i * (tabHeight + spacing);

                double targetX =
                    animate
                        ? OpenX
                        : ClosedX;

                AnimateTab(
                    presenter,
                    targetX,
                    targetY,
                    animate,
                    i
                );
            }
        }


        private void AnimateTab(
            UIElement tab,
            double targetX,
            double targetY,
            bool opening,
            int index)
        {
            /*
             * Canvas position is stored in attached properties.
             */

            double currentX =
                Canvas.GetLeft(tab);

            double currentY =
                Canvas.GetTop(tab);


            if (double.IsNaN(currentX))
            {
                currentX = ClosedX;
            }

            if (double.IsNaN(currentY))
            {
                currentY = targetY;
            }


            Duration duration = new Duration(
                TimeSpan.FromMilliseconds(
                    opening
                        ? OpenAnimationDuration
                        : CloseAnimationDuration
                )
            );


            CubicEase easing = new CubicEase
            {
                EasingMode = EasingMode.EaseOut
            };


            /*
             * Small stagger between notes.
             *
             * This creates the fan-out feeling rather than
             * making every tab move at exactly the same time.
             */

            TimeSpan delay =
                TimeSpan.FromMilliseconds(
                    opening
                        ? index * 25
                        : (NoteTabs.Items.Count - index - 1) * 15
                );


            DoubleAnimation xAnimation =
                new DoubleAnimation
                {
                    From = currentX,
                    To = targetX,
                    Duration = duration,
                    BeginTime = delay,
                    EasingFunction = easing
                };


            DoubleAnimation yAnimation =
                new DoubleAnimation
                {
                    From = currentY,
                    To = targetY,
                    Duration = duration,
                    BeginTime = delay,
                    EasingFunction = easing
                };


            xAnimation.Completed +=
                (sender, e) =>
                {
                    Canvas.SetLeft(
                        tab,
                        targetX
                    );

                    Canvas.SetTop(
                        tab,
                        targetY
                    );
                };


            tab.BeginAnimation(
                Canvas.LeftProperty,
                xAnimation
            );

            tab.BeginAnimation(
                Canvas.TopProperty,
                yAnimation
            );
        }


        public bool IsMouseInside(
            int mouseX,
            int mouseY)
        {
            if (!IsVisible)
            {
                return false;
            }

            IntPtr handle =
                new System.Windows.Interop.WindowInteropHelper(this)
                    .Handle;

            if (!GetWindowRect(
                    handle,
                    out RECT rect))
            {
                return false;
            }

            return mouseX >= rect.Left &&
                   mouseX < rect.Right &&
                   mouseY >= rect.Top &&
                   mouseY < rect.Bottom;
        }


        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(
            IntPtr hWnd,
            out RECT lpRect
        );


        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
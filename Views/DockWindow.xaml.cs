using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Animation;

namespace HoldMyNotes.Views
{
    public partial class DockWindow : Window
    {
        private const int AnimationDuration = 160;

        private bool _isShown;

        public DockWindow()
        {
            InitializeComponent();

            PositionDock();
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

            AnimateOpacity(1);
        }

        public void HideDock()
        {
            if (!_isShown)
            {
                return;
            }

            _isShown = false;

            AnimateOpacity(0);
        }

        public bool IsMouseInside(int mouseX, int mouseY)
        {
            if (!IsVisible)
            {
                return false;
            }

            IntPtr handle =
                new System.Windows.Interop.WindowInteropHelper(this).Handle;

            if (!GetWindowRect(handle, out RECT rect))
            {
                return false;
            }

            return mouseX >= rect.Left &&
                   mouseX < rect.Right &&
                   mouseY >= rect.Top &&
                   mouseY < rect.Bottom;
        }

        private void AnimateOpacity(double targetOpacity)
        {
            BeginAnimation(OpacityProperty, null);

            DoubleAnimation animation = new DoubleAnimation
            {
                From = Opacity,
                To = targetOpacity,

                Duration = TimeSpan.FromMilliseconds(
                    AnimationDuration
                ),

                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            if (targetOpacity == 0)
            {
                animation.Completed += HideAnimationCompleted;
            }

            BeginAnimation(
                OpacityProperty,
                animation
            );
        }

        private void HideAnimationCompleted(
            object? sender,
            EventArgs e)
        {
            if (!_isShown)
            {
                Hide();
            }
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
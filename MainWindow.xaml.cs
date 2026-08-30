using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using HoldMyNotes.Views;

namespace HoldMyNotes
{
    public partial class MainWindow : Window
    {
        private readonly DockWindow _dockWindow;
        private readonly DispatcherTimer _mouseTimer;

        public MainWindow()
        {
            InitializeComponent();

            PositionTrigger();

            _dockWindow = new DockWindow();

            _mouseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            _mouseTimer.Tick += MouseTimer_Tick;
            _mouseTimer.Start();
        }

        private void PositionTrigger()
        {
            Left = SystemParameters.WorkArea.Right - Width;

            Top = (SystemParameters.WorkArea.Height - Height) / 2;
        }

        private void MouseTimer_Tick(object? sender, EventArgs e)
        {
            POINT cursorPosition;

            if (!GetCursorPos(out cursorPosition))
            {
                return;
            }

            RECT triggerRect;

            if (!GetWindowRect(
                    new System.Windows.Interop.WindowInteropHelper(this).Handle,
                    out triggerRect))
            {
                return;
            }

            bool mouseInsideTrigger =
                triggerRect.Contains(
                    cursorPosition.X,
                    cursorPosition.Y
                );

            bool mouseInsideDock =
                _dockWindow.IsMouseInside(
                    cursorPosition.X,
                    cursorPosition.Y
                );

            if (mouseInsideTrigger || mouseInsideDock)
            {
                _dockWindow.ShowDock();
            }
            else
            {
                _dockWindow.HideDock();
            }
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(
            out POINT lpPoint
        );

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(
            IntPtr hWnd,
            out RECT lpRect
        );

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public bool Contains(int x, int y)
            {
                return x >= Left &&
                       x < Right &&
                       y >= Top &&
                       y < Bottom;
            }
        }
    }
}
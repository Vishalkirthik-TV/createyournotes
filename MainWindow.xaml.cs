using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using HoldMyNotes.Views;

namespace HoldMyNotes
{
    public partial class MainWindow : Window
    {
        private readonly EdgeRailWindow _edgeRail;
        private readonly DispatcherTimer _mouseTimer;

        private bool _railOpen;

        // Only the final 8 pixels of the screen trigger opening.
        private const int EdgeTriggerWidth = 8;

        public MainWindow()
        {
            InitializeComponent();

            /*
             * MainWindow is only the invisible mouse trigger.
             */
            ConfigureTrigger();

            _edgeRail = new EdgeRailWindow();

            _edgeRail.Show();

            _mouseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            _mouseTimer.Tick += MouseTimer_Tick;
            _mouseTimer.Start();
        }

        private void ConfigureTrigger()
        {
            /*
             * Make this window only 8 pixels wide.
             *
             * We deliberately don't use WorkArea.Right here.
             */

            Width = EdgeTriggerWidth;

            Height = GetPrimaryScreenHeight();

            Left =
                GetPrimaryScreenLeft() +
                GetPrimaryScreenWidth() -
                EdgeTriggerWidth;

            Top =
                GetPrimaryScreenTop();
        }

        private void MouseTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (!GetCursorPos(out POINT cursor))
            {
                return;
            }

            /*
             * Get the REAL Windows screen coordinates.
             */
            int screenLeft =
                GetSystemMetrics(SM_XVIRTUALSCREEN);

            int screenWidth =
                GetSystemMetrics(SM_CXVIRTUALSCREEN);

            int screenRight =
                screenLeft + screenWidth;


            /*
             * ------------------------------------------------
             * EDGE TRIGGER
             * ------------------------------------------------
             *
             * Only the final 8 physical pixels of the
             * virtual desktop can trigger the rail.
             */

            bool mouseAtEdge =
                cursor.X >=
                screenRight - EdgeTriggerWidth;


            /*
             * ------------------------------------------------
             * RAIL HIT TEST
             * ------------------------------------------------
             *
             * Only check the rail if it is already open.
             *
             * This is important because transparent areas
             * of the EdgeRailWindow must NOT be able to
             * trigger it from the other side of the screen.
             */

            bool mouseInsideRail = false;

            if (_railOpen)
            {
                mouseInsideRail =
                    _edgeRail.IsMouseInsideVisibleArea(
                        cursor.X,
                        cursor.Y
                    );
            }


            bool shouldOpen =
                mouseAtEdge ||
                mouseInsideRail;


            if (shouldOpen)
            {
                OpenRail();
            }
            else
            {
                CloseRail();
            }
        }

        private void OpenRail()
        {
            if (_railOpen)
            {
                return;
            }

            _railOpen = true;

            _edgeRail.Open();
        }

        private void CloseRail()
        {
            if (!_railOpen)
            {
                return;
            }

            _railOpen = false;

            _edgeRail.CloseRail();
        }


        private static int GetPrimaryScreenLeft()
        {
            return GetSystemMetrics(SM_XVIRTUALSCREEN);
        }

        private static int GetPrimaryScreenTop()
        {
            return GetSystemMetrics(SM_YVIRTUALSCREEN);
        }

        private static int GetPrimaryScreenWidth()
        {
            return GetSystemMetrics(SM_CXVIRTUALSCREEN);
        }

        private static int GetPrimaryScreenHeight()
        {
            return GetSystemMetrics(SM_CYVIRTUALSCREEN);
        }


        // ----------------------------------------------------
        // Windows API
        // ----------------------------------------------------

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(
            out POINT lpPoint
        );

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(
            int nIndex
        );


        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;


        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
using System;
using System.IO;
using System.Windows;

using Forms = System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;

namespace HoldMyNotes
{
    public partial class App : System.Windows.Application
    {
        private Forms.NotifyIcon? _trayIcon;

        private DrawingIcon? _trayIconImage;

        protected override void OnStartup(
            StartupEventArgs e)
        {
            base.OnStartup(e);

            CreateTrayIcon();
        }

        private void CreateTrayIcon()
        {
            Uri resourceUri =
                new Uri(
                    "/HoldMyNotes;component/Assets/HoldMyNotes.ico",
                    UriKind.Relative
                );

            System.Windows.Resources.StreamResourceInfo? resource =
                GetResourceStream(resourceUri);

            if (resource == null)
            {
                return;
            }

            using Stream stream =
                resource.Stream;

            _trayIconImage =
                new DrawingIcon(stream);


            _trayIcon =
                new Forms.NotifyIcon
                {
                    Icon =
                        _trayIconImage,

                    Text =
                        "HoldMyNotes",

                    Visible =
                        true
                };


            Forms.ContextMenuStrip menu =
                new Forms.ContextMenuStrip();


            Forms.ToolStripItem openItem =
                menu.Items.Add(
                    "Open"
                );

            openItem.Click +=
                (_, _) =>
                {
                    OpenAllNotesWindow();
                };


            Forms.ToolStripItem newNoteItem =
                menu.Items.Add(
                    "New Note"
                );

            newNoteItem.Click +=
                (_, _) =>
                {
                    OpenNewNote();
                };


            menu.Items.Add(
                new Forms.ToolStripSeparator()
            );


            Forms.ToolStripItem exitItem =
                menu.Items.Add(
                    "Exit"
                );

            exitItem.Click +=
                (_, _) =>
                {
                    ShutdownApplication();
                };


            _trayIcon.ContextMenuStrip =
                menu;


            _trayIcon.DoubleClick +=
                (_, _) =>
                {
                    OpenAllNotesWindow();
                };
        }


        private void OpenAllNotesWindow()
        {
            Views.AllNotesWindow window =
                new Views.AllNotesWindow();

            window.Show();
            window.Activate();

            if (window.WindowState ==
                WindowState.Minimized)
            {
                window.WindowState =
                    WindowState.Normal;
            }
        }


        private void OpenNewNote()
        {
            Views.NoteWindow window =
                new Views.NoteWindow();

            window.Show();

            window.Activate();
        }


        private void ShutdownApplication()
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible =
                    false;

                _trayIcon.Dispose();

                _trayIcon = null;
            }


            _trayIconImage?.Dispose();

            _trayIconImage = null;


            Shutdown();
        }


        protected override void OnExit(
            ExitEventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible =
                    false;

                _trayIcon.Dispose();

                _trayIcon = null;
            }


            _trayIconImage?.Dispose();

            _trayIconImage = null;


            base.OnExit(e);
        }
    }
}
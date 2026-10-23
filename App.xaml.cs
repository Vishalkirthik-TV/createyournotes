using System;
using System.IO;

using Forms = System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;

using HoldMyNotes.Data;
using HoldMyNotes.Services;

namespace HoldMyNotes
{
    public partial class App : System.Windows.Application
    {
        private Forms.NotifyIcon? _trayIcon;

        private DrawingIcon? _trayIconImage;

        private Views.AllNotesWindow? _allNotesWindow;

        protected override void OnStartup(
            System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                Database.Initialize();

                /*
                 * Load persisted settings, push the configured fonts into
                 * the application resources, and register the global
                 * hotkey that opens the All Notes view.
                 */
                AppSettings.Load();

                AppSettings.ApplyFonts();

                HotkeyService.Initialize();

                HotkeyService.Pressed += OpenAllNotesWindow;

                HotkeyService.Apply(
                    AppSettings.GlobalHotkey
                );

                CreateTrayIcon();

                MainWindow mainWindow =
                    new MainWindow();

                MainWindow =
                    mainWindow;

                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    ex.ToString(),
                    "HoldMyNotes startup error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );

                Shutdown();
            }
        }


        private void CreateTrayIcon()
        {
            Uri resourceUri =
                new Uri(
                    "pack://application:,,,/HoldMyNotes;component/Assets/HoldMyNotes.ico",
                    UriKind.Absolute
                );

            System.Windows.Resources.StreamResourceInfo? resource =
                GetResourceStream(resourceUri);

            if (resource == null)
            {
                throw new InvalidOperationException(
                    "HoldMyNotes.ico could not be loaded as a WPF resource."
                );
            }

            using MemoryStream memoryStream =
                new MemoryStream();

            resource.Stream.CopyTo(
                memoryStream
            );

            memoryStream.Position = 0;

            _trayIconImage =
                new DrawingIcon(
                    memoryStream
                );


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
                    "Open All Notes"
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


            _trayIcon.MouseClick +=
                (_, args) =>
                {
                    if (args.Button ==
                        Forms.MouseButtons.Left)
                    {
                        OpenAllNotesWindow();
                    }
                };
        }


        private void OpenAllNotesWindow()
        {
            /*
             * Reuse a single All Notes window so the tray action and the
             * global hotkey focus it instead of stacking duplicates. A
             * closed window cannot be re-shown, so we null the reference
             * in the Closed handler and build a fresh one when needed.
             */
            if (_allNotesWindow == null)
            {
                _allNotesWindow =
                    new Views.AllNotesWindow();

                _allNotesWindow.Closed +=
                    (_, _) =>
                    {
                        _allNotesWindow = null;
                    };
            }

            _allNotesWindow.Show();

            _allNotesWindow.WindowState =
                System.Windows.WindowState.Normal;

            _allNotesWindow.Activate();

            _allNotesWindow.Topmost = true;
            _allNotesWindow.Topmost = false;
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
            System.Windows.ExitEventArgs e)
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
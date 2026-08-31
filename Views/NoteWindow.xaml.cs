using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using HoldMyNotes.Data;
using HoldMyNotes.Models;

namespace HoldMyNotes.Views
{
    public partial class NoteWindow : Window
    {
        private readonly NoteRepository _noteRepository;

        private readonly DispatcherTimer _autoSaveTimer;

        private Note _note;

        private bool _isNewNote;

        private string _selectedColor;

        private bool _isPinned;

        private bool _isComplete;

        private bool _autoSaveEnabled;

        private bool _hasUnsavedChanges;

        private bool _isClosing;


        public NoteWindow()
        {
            InitializeComponent();

            _noteRepository =
                new NoteRepository();


            DateTime now =
                DateTime.Now;


            _note =
                new Note
                {
                    Title = "New Note",
                    Content = "",
                    Color = "#FFFFD75A",
                    IsPinned = false,
                    IsCompleted = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };


            _isNewNote =
                true;


            _selectedColor =
                _note.Color;


            _isPinned =
                _note.IsPinned;


            _isComplete =
                _note.IsCompleted;


            _autoSaveTimer =
                CreateAutoSaveTimer();


            ConfigureForNewNote();

            UpdateNoteSurfaceColor(
                _selectedColor
            );

            SelectCurrentColor();

            AutoSaveStatusText.Text =
                "Not saved";

            TitleTextBox.Focus();

            TitleTextBox.SelectAll();


            EnableAutoSave();
        }


        public NoteWindow(Note note)
        {
            InitializeComponent();

            _noteRepository =
                new NoteRepository();


            _note =
                note;


            _isNewNote =
                false;


            _selectedColor =
                note.Color;


            _isPinned =
                note.IsPinned;


            _isComplete =
                note.IsCompleted;


            _autoSaveTimer =
                CreateAutoSaveTimer();


            LoadExistingNote();

            UpdateNoteSurfaceColor(
                _selectedColor
            );

            SelectCurrentColor();

            UpdateCompleteVisual();

            AutoSaveStatusText.Text =
                "Saved";


            EnableAutoSave();
        }


        private DispatcherTimer CreateAutoSaveTimer()
        {
            DispatcherTimer timer =
                new DispatcherTimer
                {
                    Interval =
                        TimeSpan.FromMilliseconds(
                            250
                        )
                };


            timer.Tick +=
                AutoSaveTimer_Tick;


            return timer;
        }


        private void EnableAutoSave()
        {
            _autoSaveEnabled =
                true;


            TitleTextBox.TextChanged +=
                NoteTextBox_TextChanged;


            ContentTextBox.TextChanged +=
                NoteTextBox_TextChanged;
        }


        private void DisableAutoSave()
        {
            _autoSaveEnabled =
                false;


            TitleTextBox.TextChanged -=
                NoteTextBox_TextChanged;


            ContentTextBox.TextChanged -=
                NoteTextBox_TextChanged;
        }


        private void NoteTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (!_autoSaveEnabled ||
                _isClosing)
            {
                return;
            }


            _hasUnsavedChanges =
                true;


            AutoSaveStatusText.Text =
                "Saving...";


            RestartAutoSaveTimer();
        }


        private void RestartAutoSaveTimer()
        {
            _autoSaveTimer.Stop();

            _autoSaveTimer.Start();
        }


        private void AutoSaveTimer_Tick(
            object? sender,
            EventArgs e)
        {
            _autoSaveTimer.Stop();


            if (_isClosing)
            {
                return;
            }


            SaveToDatabase(
                false
            );
        }


        private void ConfigureForNewNote()
        {
            DeleteButton.IsEnabled =
                false;


            CompleteButton.Content =
                "Mark complete";
        }


        private void LoadExistingNote()
        {
            TitleTextBox.Text =
                _note.Title;


            ContentTextBox.Text =
                _note.Content;


            DeleteButton.IsEnabled =
                true;


            CompleteButton.Content =
                _note.IsCompleted
                    ? "Completed"
                    : "Mark complete";
        }


        private bool SaveToDatabase(
            bool force)
        {
            string title =
                TitleTextBox.Text.Trim();


            string content =
                ContentTextBox.Text;


            if (string.IsNullOrWhiteSpace(title))
            {
                AutoSaveStatusText.Text =
                    "Enter a title";

                return false;
            }


            if (!force &&
                !_hasUnsavedChanges)
            {
                return true;
            }


            _note.Title =
                title;


            _note.Content =
                content;


            _note.Color =
                _selectedColor;


            _note.IsPinned =
                _isPinned;


            _note.IsCompleted =
                _isComplete;


            _note.UpdatedAt =
                DateTime.Now;


            try
            {
                if (_isNewNote)
                {
                    int id =
                        _noteRepository.AddNote(
                            _note
                        );


                    _note.Id =
                        id;


                    _isNewNote =
                        false;


                    DeleteButton.IsEnabled =
                        true;
                }
                else
                {
                    _noteRepository.UpdateNote(
                        _note
                    );
                }


                _hasUnsavedChanges =
                    false;


                AutoSaveStatusText.Text =
                    "Saved";


                return true;
            }
            catch (Exception ex)
            {
                AutoSaveStatusText.Text =
                    "Save failed";


                if (force)
                {
                    MessageBox.Show(
                        $"Could not save the note.\n\n{ex.Message}",
                        "HoldMyNotes",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }


                return false;
            }
        }


        private void DeleteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isNewNote)
            {
                return;
            }


            DeleteConfirmWindow dialog =
                new DeleteConfirmWindow(
                    _note.Title
                );


            dialog.Owner =
                this;


            bool? result =
                dialog.ShowDialog();


            if (result != true)
            {
                return;
            }


            try
            {
                _autoSaveTimer.Stop();

                _noteRepository.DeleteNote(
                    _note.Id
                );


                _isClosing =
                    true;


                DialogResult =
                    true;


                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not delete the note.\n\n{ex.Message}",
                    "HoldMyNotes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        private void CompleteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _isComplete =
                !_isComplete;


            UpdateCompleteVisual();


            _hasUnsavedChanges =
                true;


            AutoSaveStatusText.Text =
                "Saving...";


            RestartAutoSaveTimer();
        }


        private void UpdateCompleteVisual()
        {
            if (_isComplete)
            {
                CompleteButton.Content =
                    "Completed";


                CompleteButton.Background =
                    new SolidColorBrush(
                        Color.FromArgb(
                            55,
                            100,
                            170,
                            110
                        )
                    );
            }
            else
            {
                CompleteButton.Content =
                    "Mark complete";


                CompleteButton.Background =
                    new SolidColorBrush(
                        Color.FromArgb(
                            24,
                            255,
                            255,
                            255
                        )
                    );
            }
        }


        private void ColorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not Button button)
            {
                return;
            }


            if (button.Tag is not string color)
            {
                return;
            }


            _selectedColor =
                color;


            UpdateNoteSurfaceColor(
                color
            );


            SelectCurrentColor();


            _hasUnsavedChanges =
                true;


            AutoSaveStatusText.Text =
                "Saving...";


            RestartAutoSaveTimer();
        }


        private void SelectCurrentColor()
        {
            Button[] buttons =
            {
                YellowButton,
                CoralButton,
                GreenButton,
                BlueButton,
                PurpleButton
            };


            foreach (
                Button button
                in buttons)
            {
                button.BorderThickness =
                    new Thickness(0);


                button.BorderBrush =
                    null;
            }


            foreach (
                Button button
                in buttons)
            {
                if (button.Tag is string color &&
                    color == _selectedColor)
                {
                    button.BorderThickness =
                        new Thickness(2);


                    button.BorderBrush =
                        new SolidColorBrush(
                            Color.FromRgb(
                                90,
                                82,
                                100
                            )
                        );


                    break;
                }
            }
        }


        private void UpdateNoteSurfaceColor(
            string color)
        {
            object? converted =
                ColorConverter.ConvertFromString(
                    color
                );


            if (converted is not Color parsedColor)
            {
                return;
            }


            Color surfaceColor =
                Color.FromRgb(
                    (byte)(
                        parsedColor.R * 0.82 +
                        255 * 0.18
                    ),

                    (byte)(
                        parsedColor.G * 0.82 +
                        255 * 0.18
                    ),

                    (byte)(
                        parsedColor.B * 0.82 +
                        255 * 0.18
                    )
                );


            NoteSurface.Background =
                new SolidColorBrush(
                    surfaceColor
                );
        }


        private void UpdatePinVisual()
        {
        }


        private void CloseWindowButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _isClosing =
                true;


            _autoSaveTimer.Stop();


            DisableAutoSave();


            if (_hasUnsavedChanges)
            {
                SaveToDatabase(
                    true
                );
            }
            else if (_isNewNote)
            {
                string title =
                    TitleTextBox.Text.Trim();


                if (!string.Equals(
                        title,
                        "New Note",
                        StringComparison.OrdinalIgnoreCase
                    ) ||
                    !string.IsNullOrWhiteSpace(
                        ContentTextBox.Text
                    ))
                {
                    SaveToDatabase(
                        true
                    );
                }
            }


            DialogResult =
                _hasUnsavedChanges
                    ? true
                    : true;


            Close();
        }


        protected override void OnClosed(
            EventArgs e)
        {
            _autoSaveTimer.Stop();

            DisableAutoSave();

            base.OnClosed(e);
        }


        private void WindowHeader_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.LeftButton ==
                MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
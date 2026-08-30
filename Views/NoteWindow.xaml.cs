using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using HoldMyNotes.Data;
using HoldMyNotes.Models;

namespace HoldMyNotes.Views
{
    public partial class NoteWindow : Window
    {
        private readonly NoteRepository _noteRepository;

        private Note _note;

        private readonly bool _isNewNote;

        private string _selectedColor;

        private bool _isPinned;

        private bool _isComplete;


        // ============================================================
        // NEW NOTE
        // ============================================================

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


            _isNewNote = true;

            _selectedColor =
                _note.Color;

            _isPinned =
                _note.IsPinned;

            _isComplete =
                _note.IsCompleted;


            ConfigureForNewNote();

            UpdateNoteSurfaceColor(
                _selectedColor
            );

            SelectCurrentColor();


            TitleTextBox.Focus();

            TitleTextBox.SelectAll();
        }


        // ============================================================
        // EXISTING NOTE
        // ============================================================

        public NoteWindow(Note note)
        {
            InitializeComponent();

            _noteRepository =
                new NoteRepository();


            /*
             * Work with the actual note object.
             */

            _note =
                note;


            _isNewNote = false;


            _selectedColor =
                note.Color;


            _isPinned =
                note.IsPinned;


            _isComplete =
                note.IsCompleted;


            LoadExistingNote();

            UpdateNoteSurfaceColor(
                _selectedColor
            );

            SelectCurrentColor();

            UpdatePinVisual();

            UpdateCompleteVisual();
        }


        // ============================================================
        // NEW NOTE SETUP
        // ============================================================

        private void ConfigureForNewNote()
        {
            DeleteButton.IsEnabled =
                false;


            SavedStatusText_NotUsed();


            CompleteButton.Content =
                "Mark complete";


            PinButton_NotUsed();
        }


        /*
         * These are intentionally empty helpers because
         * the current UI no longer displays a "Saved" label
         * or pin button.
         *
         * They keep the constructor readable and make it easy
         * to add those features visually later.
         */

        private void SavedStatusText_NotUsed()
        {
        }


        private void PinButton_NotUsed()
        {
        }


        // ============================================================
        // LOAD EXISTING
        // ============================================================

        private void LoadExistingNote()
        {
            TitleTextBox.Text =
                _note.Title;


            ContentTextBox.Text =
                _note.Content;


            DeleteButton.IsEnabled =
                true;
        }


        // ============================================================
        // SAVE
        // ============================================================

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            SaveNote();
        }


        private void SaveNote()
        {
            string title =
                TitleTextBox.Text.Trim();


            string content =
                ContentTextBox.Text;


            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "Please enter a title.",
                    "HoldMyNotes",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                TitleTextBox.Focus();

                return;
            }


            // --------------------------------------------------------
            // Update object
            // --------------------------------------------------------

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


            // --------------------------------------------------------
            // INSERT or UPDATE
            // --------------------------------------------------------

            if (_isNewNote)
            {
                int id =
                    _noteRepository.AddNote(
                        _note
                    );


                _note.Id =
                    id;
            }
            else
            {
                _noteRepository.UpdateNote(
                    _note
                );
            }


            /*
             * true tells EdgeRailWindow:
             *
             * "The note changed."
             */

            DialogResult = true;

            Close();
        }


        // ============================================================
        // DELETE
        // ============================================================

        private void DeleteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isNewNote)
            {
                return;
            }


            MessageBoxResult result =
                MessageBox.Show(
                    $"Delete \"{_note.Title}\"?",
                    "Delete Note",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );


            if (result !=
                MessageBoxResult.Yes)
            {
                return;
            }


            try
            {
                _noteRepository.DeleteNote(
                    _note.Id
                );


                /*
                 * true means the parent should reload
                 * because the database changed.
                 */

                DialogResult = true;

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


        // ============================================================
        // COMPLETE
        // ============================================================

        private void CompleteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _isComplete =
                !_isComplete;


            UpdateCompleteVisual();
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


        // ============================================================
        // COLOR
        // ============================================================

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


            foreach (Button button
                     in buttons)
            {
                button.BorderThickness =
                    new Thickness(0);
            }


            foreach (Button button
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


        // ============================================================
        // PIN
        // ============================================================

        /*
         * Pin will be added back to the visual header later.
         *
         * The database/model support is already present.
         */

        private void UpdatePinVisual()
        {
            // Reserved for the pin UI.
        }


        // ============================================================
        // CLOSE
        // ============================================================

        private void CloseWindowButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            /*
             * Closing the window without pressing Save
             * does NOT modify SQLite.
             */

            DialogResult = false;

            Close();
        }


        // ============================================================
        // DRAG WINDOW
        // ============================================================

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
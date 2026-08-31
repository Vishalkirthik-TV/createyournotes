using System.Windows;
using System.Windows.Input;

namespace HoldMyNotes.Views
{
    public partial class DeleteConfirmWindow : Window
    {
        public DeleteConfirmWindow(string noteTitle)
        {
            InitializeComponent();

            NoteNameText.Text =
                "\"" + noteTitle + "\" will be permanently deleted.";

            PreviewKeyDown +=
                DeleteConfirmWindow_PreviewKeyDown;
        }

        private void DeleteConfirmWindow_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void DeleteConfirmButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
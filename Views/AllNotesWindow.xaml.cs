using Microsoft.Data.Sqlite;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HoldMyNotes.Data;
using HoldMyNotes.Models;

namespace HoldMyNotes.Views
{
    public partial class AllNotesWindow : Window
    {
        private const string DefaultColor = "#FFD75A";

        private readonly NoteRepository _noteRepository;
        private readonly ObservableCollection<NoteListItem> _filteredNotes = new();

        private List<NoteListItem> _allNotes = new();
        private string _filter = "All";
        private int? _selectedId;

        public ObservableCollection<NoteListItem> FilteredNotes => _filteredNotes;

        public event EventHandler? NotesChanged;

        public AllNotesWindow()
        {
            InitializeComponent();

            _noteRepository = new NoteRepository();
            DataContext = this;

            LoadNotes();
            ApplyFilter();
        }

        // ============================================================
        // DATABASE
        // ============================================================

        private static SqliteConnection OpenConnection()
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "holdmynotes.db"
            );

            SqliteConnection connection = new SqliteConnection(
                $"Data Source={path}"
            );

            connection.Open();
            return connection;
        }

        private void LoadNotes()
        {
            _allNotes = new List<NoteListItem>();

            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                SELECT Id, Title, Content, Color, IsCompleted, CreatedAt, UpdatedAt
                FROM Notes
                ORDER BY IsPinned DESC, datetime(UpdatedAt) DESC, Id DESC;
                """;

            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string title = reader.IsDBNull(1) ? "Untitled note" : reader.GetString(1);
                string content = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                string color = reader.IsDBNull(3) ? DefaultColor : reader.GetString(3);
                bool completed = !reader.IsDBNull(4) && reader.GetInt32(4) != 0;
                string createdAt = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);
                string updatedAt = reader.IsDBNull(6) ? createdAt : reader.GetString(6);

                _allNotes.Add(new NoteListItem
                {
                    Id = id,
                    Title = title,
                    Content = content,
                    Color = GetSafeColor(color),
                    IsCompleted = completed,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                });
            }
        }

        private static string GetSafeColor(string color)
        {
            try
            {
                if (ColorConverter.ConvertFromString(color) is Color parsed)
                    return parsed.ToString();
            }
            catch
            {
                // Use the fallback below.
            }

            return DefaultColor;
        }

        // ============================================================
        // SEARCH / FILTER
        // ============================================================

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filter)
            {
                _filter = filter;
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            string search = SearchBox?.Text?.Trim() ?? string.Empty;

            IEnumerable<NoteListItem> query = _allNotes;

            if (_filter == "Active")
                query = query.Where(n => !n.IsCompleted);
            else if (_filter == "Archived")
                query = query.Where(n => n.IsCompleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(n =>
                    n.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    n.Content.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            _filteredNotes.Clear();

            foreach (NoteListItem item in query)
                _filteredNotes.Add(item);

            CountText.Text = $"{_filteredNotes.Count} notes";
            UpdateFilterVisuals();

            if (_selectedId.HasValue)
            {
                NoteListItem? selected = _filteredNotes.FirstOrDefault(
                    n => n.Id == _selectedId.Value
                );

                if (selected != null)
                    NotesList.SelectedItem = selected;
                else
                    ClearDetail();
            }
            else if (_filteredNotes.Count > 0)
            {
                NotesList.SelectedIndex = 0;
            }
            else
            {
                ClearDetail();
            }
        }

        private void UpdateFilterVisuals()
        {
            SetFilterButtonState(AllFilterButton, _filter == "All");
            SetFilterButtonState(ActiveFilterButton, _filter == "Active");
            SetFilterButtonState(ArchivedFilterButton, _filter == "Archived");
        }

        private static void SetFilterButtonState(Button button, bool active)
        {
            button.Background = active
                ? new SolidColorBrush(Color.FromRgb(225, 223, 215))
                : Brushes.Transparent;

            button.Foreground = active
                ? new SolidColorBrush(Color.FromRgb(55, 53, 48))
                : new SolidColorBrush(Color.FromRgb(119, 116, 109));
        }

        // ============================================================
        // SELECTION / DETAIL
        // ============================================================

        private void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (NotesList.SelectedItem is not NoteListItem item)
                return;

            _selectedId = item.Id;
            ShowDetail(item);
        }

        private void ShowDetail(NoteListItem item)
        {
            DetailTitle.Text = item.Title;
            DetailContent.Text = item.Content;
            DetailTime.Text = FormatDate(item.UpdatedAt);
            DetailMeta.Text =
                $"Created {FormatDate(item.CreatedAt)} · Updated {FormatDate(item.UpdatedAt)}";

            try
            {
                DetailCard.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(item.Color)
                );
            }
            catch
            {
                DetailCard.Background = new SolidColorBrush(Color.FromRgb(183, 217, 250));
            }

            MarkCompleteButton.Content = item.IsCompleted
                ? "Mark active"
                : "Mark complete";
        }

        private void ClearDetail()
        {
            _selectedId = null;
            DetailTitle.Text = "";
            DetailContent.Text = "";
            DetailTime.Text = "";
            DetailMeta.Text = "Select a note to view it.";
            DetailCard.Background = new SolidColorBrush(Color.FromRgb(230, 228, 220));
            MarkCompleteButton.Content = "Mark complete";
        }

        private static string FormatDate(string value)
        {
            if (DateTime.TryParse(value, out DateTime date))
                return date.ToString("dd MMM yyyy · HH:mm");

            return string.IsNullOrWhiteSpace(value) ? "" : value;
        }

        // ============================================================
        // EDIT
        // ============================================================

        private void NotesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NotesList.SelectedItem is not NoteListItem item)
                return;

            OpenEditor(item.Id);
            e.Handled = true;
        }

        private void OpenEditor(int id)
        {
            Note? note = _noteRepository.GetNote(id);
            if (note == null)
                return;

            NoteWindow window = new NoteWindow(note);
            bool? result = window.ShowDialog();

            if (result == true)
            {
                ReloadAfterChange();
            }
        }

        // ============================================================
        // MARK COMPLETE
        // ============================================================

        private void MarkCompleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedId.HasValue)
                return;

            NoteListItem? item = _allNotes.FirstOrDefault(
                n => n.Id == _selectedId.Value
            );

            if (item == null)
                return;

            SetCompleted(item.Id, !item.IsCompleted);
            ReloadAfterChange();
        }

        private static void SetCompleted(int id, bool completed)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                UPDATE Notes
                SET IsCompleted = $completed,
                    UpdatedAt = $updatedAt
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue("$completed", completed ? 1 : 0);
            command.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("O"));
            command.Parameters.AddWithValue("$id", id);
            command.ExecuteNonQuery();
        }

        // ============================================================
        // DELETE
        // ============================================================

        private void DeleteButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (!_selectedId.HasValue)
                return;

            NoteListItem? item =
                _allNotes.FirstOrDefault(
                    n => n.Id == _selectedId.Value
                );

            if (item == null)
                return;

            DeleteConfirmWindow dialog =
                new DeleteConfirmWindow(
                    item.Title
                );

            dialog.Owner =
                this;

            bool? result =
                dialog.ShowDialog();

            if (result != true)
                return;

            using SqliteConnection connection =
                OpenConnection();

            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText =
                "DELETE FROM Notes WHERE Id = $id;";

            command.Parameters.AddWithValue(
                "$id",
                item.Id
            );

            command.ExecuteNonQuery();

            _selectedId = null;

            ReloadAfterChange();
        }

        // ============================================================
        // EXPORT
        // ============================================================

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedId.HasValue)
                return;

            NoteListItem? item = _allNotes.FirstOrDefault(
                n => n.Id == _selectedId.Value
            );

            if (item == null)
                return;

            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Export note",
                Filter = "Text file (*.txt)|*.txt|JSON file (*.json)|*.json",
                FileName = SanitizeFileName(item.Title) + ".txt"
            };

            if (dialog.ShowDialog(this) != true)
                return;

            if (Path.GetExtension(dialog.FileName).Equals(
                    ".json",
                    StringComparison.OrdinalIgnoreCase))
            {
                string json = JsonSerializer.Serialize(
                    item,
                    new JsonSerializerOptions { WriteIndented = true }
                );
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
            }
            else
            {
                StringBuilder text = new StringBuilder();
                text.AppendLine(item.Title);
                text.AppendLine();
                text.AppendLine(item.Content);
                text.AppendLine();
                text.AppendLine($"Created: {item.CreatedAt}");
                text.AppendLine($"Updated: {item.UpdatedAt}");
                File.WriteAllText(dialog.FileName, text.ToString(), Encoding.UTF8);
            }
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');

            return string.IsNullOrWhiteSpace(value) ? "note" : value.Trim();
        }

        // ============================================================
        // IMPORT
        // ============================================================

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Import notes",
                Filter = "JSON files (*.json)|*.json",
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
                return;

            try
            {
                string json = File.ReadAllText(dialog.FileName, Encoding.UTF8);
                NoteImportDto? dto = JsonSerializer.Deserialize<NoteImportDto>(json);

                if (dto != null && !string.IsNullOrWhiteSpace(dto.Title))
                {
                    InsertImportedNote(dto);
                }
                else
                {
                    MessageBox.Show(
                        this,
                        "The selected JSON file does not contain a valid note.",
                        "Import failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                ReloadAfterChange();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "The note could not be imported.\n\n" + ex.Message,
                    "Import failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private static void InsertImportedNote(NoteImportDto dto)
        {
            DateTime now = DateTime.UtcNow;

            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO Notes
                (Title, Content, Color, IsPinned, IsCompleted, CreatedAt, UpdatedAt)
                VALUES
                ($title, $content, $color, $pinned, $completed, $createdAt, $updatedAt);
                """;

            command.Parameters.AddWithValue("$title", dto.Title.Trim());
            command.Parameters.AddWithValue("$content", dto.Content ?? string.Empty);
            command.Parameters.AddWithValue("$color", dto.Color ?? DefaultColor);
            command.Parameters.AddWithValue("$pinned", dto.IsPinned ? 1 : 0);
            command.Parameters.AddWithValue("$completed", dto.IsCompleted ? 1 : 0);
            command.Parameters.AddWithValue("$createdAt", now.ToString("O"));
            command.Parameters.AddWithValue("$updatedAt", now.ToString("O"));
            command.ExecuteNonQuery();
        }

        // ============================================================
        // RELOAD
        // ============================================================

        private void ReloadAfterChange()
        {
            LoadNotes();
            ApplyFilter();
            NotesChanged?.Invoke(this, EventArgs.Empty);
        }

        private sealed class NoteImportDto
        {
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string Color { get; set; } = DefaultColor;
            public bool IsPinned { get; set; }
            public bool IsCompleted { get; set; }
        }

        public sealed class NoteListItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string Color { get; set; } = DefaultColor;
            public bool IsCompleted { get; set; }
            public string CreatedAt { get; set; } = string.Empty;
            public string UpdatedAt { get; set; } = string.Empty;

            public string Preview
            {
                get
                {
                    string value = Content
                        .Replace("\r", " ")
                        .Replace("\n", " ")
                        .Trim();

                    return value.Length > 72
                        ? value[..72] + "…"
                        : value;
                }
            }

            public string StatusText => IsCompleted ? "ARCHIVED" : "ACTIVE";

            public string RelativeTime
            {
                get
                {
                    if (!DateTime.TryParse(UpdatedAt, out DateTime date))
                        return "";

                    TimeSpan age = DateTime.UtcNow - date.ToUniversalTime();

                    if (age.TotalMinutes < 1) return "now";
                    if (age.TotalHours < 1) return $"{Math.Max(1, (int)age.TotalMinutes)}m";
                    if (age.TotalDays < 1) return $"{Math.Max(1, (int)age.TotalHours)}h";
                    if (age.TotalDays < 30) return $"{Math.Max(1, (int)age.TotalDays)}d";

                    return date.ToString("dd MMM");
                }
            }
        }
    }
}

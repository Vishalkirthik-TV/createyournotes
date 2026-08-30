using System;
using System.Collections.Generic;
using HoldMyNotes.Models;
using Microsoft.Data.Sqlite;

namespace HoldMyNotes.Data
{
    public class NoteRepository
    {
        // ============================================================
        // GET ALL NOTES
        // ============================================================

        public List<Note> GetAllNotes()
        {
            List<Note> notes = new List<Note>();

            using SqliteConnection connection =
                Database.GetConnection();

            connection.Open();

            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText = """
                SELECT
                    Id,
                    Title,
                    Content,
                    Color,
                    IsPinned,
                    IsCompleted,
                    CreatedAt,
                    UpdatedAt
                FROM Notes
                ORDER BY Id;
                """;

            using SqliteDataReader reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                Note note = new Note
                {
                    Id = reader.GetInt32(0),

                    Title = reader.GetString(1),

                    Content = reader.GetString(2),

                    Color = reader.GetString(3),

                    IsPinned = reader.GetInt32(4) == 1,

                    IsCompleted = reader.GetInt32(5) == 1,

                    CreatedAt = DateTime.Parse(
                        reader.GetString(6)
                    ),

                    UpdatedAt = DateTime.Parse(
                        reader.GetString(7)
                    )
                };

                notes.Add(note);
            }

            return notes;
        }


        // ============================================================
        // GET ONE NOTE
        // ============================================================

        public Note? GetNote(int id)
        {
            using SqliteConnection connection =
                Database.GetConnection();

            connection.Open();

            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText = """
                SELECT
                    Id,
                    Title,
                    Content,
                    Color,
                    IsPinned,
                    IsCompleted,
                    CreatedAt,
                    UpdatedAt
                FROM Notes
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue(
                "$id",
                id
            );

            using SqliteDataReader reader =
                command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new Note
            {
                Id = reader.GetInt32(0),

                Title = reader.GetString(1),

                Content = reader.GetString(2),

                Color = reader.GetString(3),

                IsPinned = reader.GetInt32(4) == 1,

                IsCompleted = reader.GetInt32(5) == 1,

                CreatedAt = DateTime.Parse(
                    reader.GetString(6)
                ),

                UpdatedAt = DateTime.Parse(
                    reader.GetString(7)
                )
            };
        }


        // ============================================================
        // ADD NOTE
        // ============================================================

        public int AddNote(Note note)
        {
            using SqliteConnection connection =
                Database.GetConnection();

            connection.Open();

            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText = """
                INSERT INTO Notes
                (
                    Title,
                    Content,
                    Color,
                    IsPinned,
                    IsCompleted,
                    CreatedAt,
                    UpdatedAt
                )
                VALUES
                (
                    $title,
                    $content,
                    $color,
                    $isPinned,
                    $isCompleted,
                    $createdAt,
                    $updatedAt
                );

                SELECT last_insert_rowid();
                """;

            command.Parameters.AddWithValue(
                "$title",
                note.Title
            );

            command.Parameters.AddWithValue(
                "$content",
                note.Content
            );

            command.Parameters.AddWithValue(
                "$color",
                note.Color
            );

            command.Parameters.AddWithValue(
                "$isPinned",
                note.IsPinned ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "$isCompleted",
                note.IsCompleted ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "$createdAt",
                note.CreatedAt.ToString("O")
            );

            command.Parameters.AddWithValue(
                "$updatedAt",
                note.UpdatedAt.ToString("O")
            );

            long id =
                (long)command.ExecuteScalar()!;

            return (int)id;
        }


        // ============================================================
        // UPDATE NOTE
        // ============================================================

        public void UpdateNote(Note note)
        {
            using SqliteConnection connection =
                Database.GetConnection();

            connection.Open();

            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText = """
                UPDATE Notes
                SET
                    Title = $title,
                    Content = $content,
                    Color = $color,
                    IsPinned = $isPinned,
                    IsCompleted = $isCompleted,
                    UpdatedAt = $updatedAt
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue(
                "$id",
                note.Id
            );

            command.Parameters.AddWithValue(
                "$title",
                note.Title
            );

            command.Parameters.AddWithValue(
                "$content",
                note.Content
            );

            command.Parameters.AddWithValue(
                "$color",
                note.Color
            );

            command.Parameters.AddWithValue(
                "$isPinned",
                note.IsPinned ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "$isCompleted",
                note.IsCompleted ? 1 : 0
            );

            command.Parameters.AddWithValue(
                "$updatedAt",
                note.UpdatedAt.ToString("O")
            );

            int rowsAffected =
                command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException(
                    $"Note with ID {note.Id} was not found."
                );
            }
        }


        // ============================================================
        // DELETE NOTE
        // ============================================================

        public void DeleteNote(int id)
        {
            using SqliteConnection connection =
                Database.GetConnection();

            connection.Open();

            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText = """
                DELETE FROM Notes
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue(
                "$id",
                id
            );

            int rowsAffected =
                command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException(
                    $"Note with ID {id} was not found."
                );
            }
        }
    }
}
using System;
using Microsoft.Data.Sqlite;

namespace HoldMyNotes.Data
{
    /// <summary>
    /// Simple key/value persistence for application settings.
    /// </summary>
    public class SettingsRepository
    {
        public string? Get(string name)
        {
            using SqliteConnection connection =
                Database.GetConnection();

            connection.Open();

            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText =
                "SELECT Value FROM Settings WHERE Name = $name;";

            command.Parameters.AddWithValue(
                "$name",
                name
            );

            object? result =
                command.ExecuteScalar();

            return result as string;
        }

        public void Set(string name, string value)
        {
            using SqliteConnection connection =
                Database.GetConnection();

            connection.Open();

            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText = """
                INSERT INTO Settings (Name, Value)
                VALUES ($name, $value)
                ON CONFLICT(Name) DO UPDATE SET Value = $value;
                """;

            command.Parameters.AddWithValue(
                "$name",
                name
            );

            command.Parameters.AddWithValue(
                "$value",
                value
            );

            command.ExecuteNonQuery();
        }
    }
}

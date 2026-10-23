using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace HoldMyNotes.Data
{
    public static class Database
    {
        private static readonly string DatabasePath =
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "holdmynotes.db"
            );


        private static readonly string ConnectionString =
            $"Data Source={DatabasePath}";


        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(
                ConnectionString
            );
        }


        public static void Initialize()
        {
            using SqliteConnection connection =
                GetConnection();

            connection.Open();


            // ========================================================
            // CREATE TABLE
            // ========================================================

            using SqliteCommand createCommand =
                connection.CreateCommand();

            createCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS Notes
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                    Title TEXT NOT NULL,

                    Content TEXT NOT NULL DEFAULT '',

                    Color TEXT NOT NULL,

                    IsPinned INTEGER NOT NULL DEFAULT 0,

                    IsCompleted INTEGER NOT NULL DEFAULT 0,

                    CreatedAt TEXT NOT NULL,

                    UpdatedAt TEXT NOT NULL
                );
                """;

            createCommand.ExecuteNonQuery();


            // ========================================================
            // DATABASE MIGRATION
            // ========================================================

            AddColumnIfMissing(
                connection,
                "IsPinned",
                "INTEGER NOT NULL DEFAULT 0"
            );


            AddColumnIfMissing(
                connection,
                "IsCompleted",
                "INTEGER NOT NULL DEFAULT 0"
            );


            // ========================================================
            // SETTINGS TABLE (key/value)
            // ========================================================

            using SqliteCommand settingsCommand =
                connection.CreateCommand();

            settingsCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS Settings
                (
                    Name TEXT PRIMARY KEY,

                    Value TEXT NOT NULL DEFAULT ''
                );
                """;

            settingsCommand.ExecuteNonQuery();
        }


        private static void AddColumnIfMissing(
            SqliteConnection connection,
            string columnName,
            string columnDefinition)
        {
            using SqliteCommand command =
                connection.CreateCommand();

            command.CommandText =
                "PRAGMA table_info(Notes);";


            using SqliteDataReader reader =
                command.ExecuteReader();


            bool exists = false;


            while (reader.Read())
            {
                string existingColumn =
                    reader.GetString(1);


                if (string.Equals(
                        existingColumn,
                        columnName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;

                    break;
                }
            }


            reader.Close();


            if (exists)
            {
                return;
            }


            using SqliteCommand alterCommand =
                connection.CreateCommand();

            alterCommand.CommandText =
                $"ALTER TABLE Notes ADD COLUMN {columnName} {columnDefinition};";

            alterCommand.ExecuteNonQuery();
        }
    }
}
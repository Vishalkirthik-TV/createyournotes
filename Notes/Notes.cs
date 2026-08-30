using System;

namespace HoldMyNotes.Models
{
    public class Note
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public string Content { get; set; } = "";

        public string Color { get; set; } = "#FFFFD75A";

        public bool IsPinned { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
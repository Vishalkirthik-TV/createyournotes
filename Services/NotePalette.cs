using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace HoldMyNotes.Services
{
    /// <summary>
    /// The note colour palette: the five built-in colours plus any extra
    /// colours the user added in Settings.
    /// </summary>
    public static class NotePalette
    {
        public static readonly IReadOnlyList<string> DefaultColors =
            new List<string>
            {
                "#FFFFD75A", // yellow
                "#FFFFA98F", // coral
                "#FF9CDBA2", // green
                "#FF9BCFEA", // blue
                "#FFD0B9F2", // purple
            };

        private static List<string> _custom = new();

        public static IReadOnlyList<string> CustomColors => _custom;

        public static IEnumerable<string> AllColors =>
            DefaultColors.Concat(_custom);

        public static bool IsDefault(string color) =>
            DefaultColors.Any(c =>
                string.Equals(c, color, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Load custom colours from the stored JSON blob.
        /// </summary>
        public static void LoadCustom(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                _custom = new List<string>();
                return;
            }

            try
            {
                _custom =
                    JsonSerializer.Deserialize<List<string>>(json)
                    ?? new List<string>();
            }
            catch
            {
                _custom = new List<string>();
            }
        }

        public static string SerializeCustom()
        {
            return JsonSerializer.Serialize(_custom.ToList());
        }

        public static void AddCustom(string color)
        {
            string normalized = Normalize(color);

            if (string.IsNullOrEmpty(normalized))
            {
                return;
            }

            bool exists = AllColors.Any(c =>
                string.Equals(c, normalized, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return;
            }

            _custom.Add(normalized);
        }

        public static void RemoveCustom(string color)
        {
            _custom.RemoveAll(c =>
                string.Equals(c, color, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Normalise any "#RGB", "#RRGGBB" or "#AARRGGBB" string to the
        /// "#FFRRGGBB" form the rest of the app uses for note colours.
        /// Returns an empty string when the input cannot be parsed.
        /// </summary>
        public static string Normalize(string? color)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return string.Empty;
            }

            string text = color.Trim();

            if (!text.StartsWith("#", StringComparison.Ordinal))
            {
                text = "#" + text;
            }

            try
            {
                if (System.Windows.Media.ColorConverter
                        .ConvertFromString(text)
                    is System.Windows.Media.Color parsed)
                {
                    return string.Format(
                        "#{0:X2}{1:X2}{2:X2}{3:X2}",
                        parsed.A,
                        parsed.R,
                        parsed.G,
                        parsed.B);
                }
            }
            catch
            {
                // Fall through to the empty result below.
            }

            return string.Empty;
        }
    }
}

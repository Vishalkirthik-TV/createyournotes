using System.Collections.Generic;

namespace HoldMyNotes.Services
{
    /// <summary>
    /// A selectable font: a friendly name plus the FontFamily source
    /// used to construct a System.Windows.Media.FontFamily.
    /// </summary>
    public sealed class FontOption
    {
        public FontOption(string displayName, string source)
        {
            DisplayName = displayName;
            Source = source;
        }

        public string DisplayName { get; }

        public string Source { get; }
    }

    /// <summary>
    /// The fonts offered in Settings. The handwriting font drives note
    /// bodies and previews; the UI font drives headings and controls.
    /// </summary>
    public static class FontCatalog
    {
        /// <summary>
        /// The embedded Virgil handwriting font (converted from the WOFF2
        /// the project ships with). WPF needs the full pack URI when a
        /// FontFamily is constructed from code.
        /// </summary>
        public const string VirgilSource =
            "pack://application:,,,/HoldMyNotes;component/Assets/Fonts/#Virgil 3 YOFF";

        public const string DefaultUiSource = "Segoe UI";

        public static readonly IReadOnlyList<FontOption> HandwritingFonts =
            new List<FontOption>
            {
                new FontOption("Virgil (handwritten)", VirgilSource),
                new FontOption("Segoe Print", "Segoe Print"),
                new FontOption("Segoe Script", "Segoe Script"),
                new FontOption("Comic Sans MS", "Comic Sans MS"),
                new FontOption("Ink Free", "Ink Free"),
                new FontOption("Calibri", "Calibri"),
            };

        public static readonly IReadOnlyList<FontOption> UiFonts =
            new List<FontOption>
            {
                new FontOption("Segoe UI", "Segoe UI"),
                new FontOption("Segoe UI Light", "Segoe UI Light"),
                new FontOption("Calibri", "Calibri"),
                new FontOption("Arial", "Arial"),
                new FontOption("Tahoma", "Tahoma"),
                new FontOption("Verdana", "Verdana"),
            };

        /// <summary>
        /// Resolve a stored source string back to a display name for the
        /// font pickers. Falls back to the first entry when unknown.
        /// </summary>
        public static string DisplayNameFor(
            IReadOnlyList<FontOption> options,
            string? source)
        {
            foreach (FontOption option in options)
            {
                if (string.Equals(
                        option.Source,
                        source,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return option.DisplayName;
                }
            }

            return options[0].DisplayName;
        }

        public static string SourceFor(
            IReadOnlyList<FontOption> options,
            string? displayName)
        {
            foreach (FontOption option in options)
            {
                if (string.Equals(
                        option.DisplayName,
                        displayName,
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    return option.Source;
                }
            }

            return options[0].Source;
        }
    }
}

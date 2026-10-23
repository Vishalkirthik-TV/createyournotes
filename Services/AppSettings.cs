using System;
using System.Windows;
using System.Windows.Media;
using HoldMyNotes.Data;

namespace HoldMyNotes.Services
{
    /// <summary>
    /// Application-wide settings facade. Wraps the key/value store with
    /// typed accessors, caches the current values, and applies changes
    /// (fonts, hotkey) immediately.
    /// </summary>
    public static class AppSettings
    {
        private const string HotkeyKey = "GlobalHotkey";
        private const string NoteFontKey = "NoteFont";
        private const string UiFontKey = "UiFont";
        private const string NoteColorsKey = "NoteColors";

        public const string HotkeyNone = "None";

        private static readonly SettingsRepository _repo = new();

        public static string GlobalHotkey { get; private set; } = HotkeyNone;

        public static string NoteFontSource { get; private set; } =
            FontCatalog.VirgilSource;

        public static string UiFontSource { get; private set; } =
            FontCatalog.DefaultUiSource;

        /// <summary>
        /// Raised after any setting changes, so open windows can refresh.
        /// </summary>
        public static event Action? SettingsChanged;

        /// <summary>
        /// Load persisted values. Call once at startup after the database
        /// has been initialised.
        /// </summary>
        public static void Load()
        {
            GlobalHotkey =
                _repo.Get(HotkeyKey) ?? HotkeyNone;

            NoteFontSource =
                _repo.Get(NoteFontKey) ?? FontCatalog.VirgilSource;

            UiFontSource =
                _repo.Get(UiFontKey) ?? FontCatalog.DefaultUiSource;

            NotePalette.LoadCustom(
                _repo.Get(NoteColorsKey)
            );
        }

        /// <summary>
        /// Push the current fonts into the application resources so every
        /// window bound with DynamicResource updates live.
        /// </summary>
        public static void ApplyFonts()
        {
            Application? app = Application.Current;

            if (app == null)
            {
                return;
            }

            app.Resources["HandwritingFont"] =
                new FontFamily(NoteFontSource);

            app.Resources["UiFont"] =
                new FontFamily(UiFontSource);
        }

        public static void SetHotkey(string hotkey)
        {
            GlobalHotkey =
                string.IsNullOrWhiteSpace(hotkey) ? HotkeyNone : hotkey;

            _repo.Set(HotkeyKey, GlobalHotkey);

            SettingsChanged?.Invoke();
        }

        public static void SetNoteFont(string source)
        {
            NoteFontSource = source;

            _repo.Set(NoteFontKey, source);

            ApplyFonts();

            SettingsChanged?.Invoke();
        }

        public static void SetUiFont(string source)
        {
            UiFontSource = source;

            _repo.Set(UiFontKey, source);

            ApplyFonts();

            SettingsChanged?.Invoke();
        }

        public static void SaveCustomColors()
        {
            _repo.Set(
                NoteColorsKey,
                NotePalette.SerializeCustom()
            );

            SettingsChanged?.Invoke();
        }
    }
}

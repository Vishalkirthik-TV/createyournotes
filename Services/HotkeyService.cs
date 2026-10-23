using System;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace HoldMyNotes.Services
{
    /// <summary>
    /// Registers a single application-wide (global) hotkey using the Win32
    /// RegisterHotKey API. A message-only window receives WM_HOTKEY so the
    /// hotkey works regardless of which window has focus.
    /// </summary>
    public static class HotkeyService
    {
        private const int WmHotkey = 0x0312;

        private const int IdHotkey = 0x484E; // arbitrary unique id

        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;
        private const uint ModShift = 0x0004;
        private const uint ModWin = 0x0008;
        private const uint ModNoRepeat = 0x4000;

        private static HwndSource? _source;
        private static IntPtr _hwnd;
        private static bool _initialized;

        /// <summary>
        /// Fired when the registered hotkey is pressed anywhere.
        /// </summary>
        public static event Action? Pressed;

        public static string? Current { get; private set; }

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            HwndSourceParameters parameters =
                new HwndSourceParameters("HoldMyNotesHotkeySink")
                {
                    // HWND_MESSAGE: a message-only window, invisible and
                    // free of any sizing / z-order side effects.
                    ParentWindow = new IntPtr(-3),
                    Width = 0,
                    Height = 0,
                    WindowStyle = 0
                };

            _source = new HwndSource(parameters);

            _source.AddHook(WndProc);

            _hwnd = _source.Handle;

            _initialized = true;
        }

        /// <summary>
        /// Register the given hotkey string (e.g. "Ctrl+Shift+N"), replacing
        /// any previously registered one. Returns true when the hotkey is
        /// active — either successfully registered or deliberately cleared
        /// ("None"). Returns false when the string is invalid or the
        /// shortcut is already taken by another application.
        /// </summary>
        public static bool Apply(string? hotkey)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(hotkey) ||
                string.Equals(hotkey, "None", StringComparison.OrdinalIgnoreCase))
            {
                Unregister();
                Current = AppSettings.HotkeyNone;
                return true;
            }

            if (!TryParse(hotkey, out uint modifiers, out uint vk))
            {
                return false;
            }

            Unregister();

            bool ok = RegisterHotKey(
                _hwnd,
                IdHotkey,
                modifiers | ModNoRepeat,
                vk
            );

            if (ok)
            {
                Current = hotkey;
            }

            return ok;
        }

        public static void Unregister()
        {
            if (!_initialized)
            {
                return;
            }

            UnregisterHotKey(_hwnd, IdHotkey);

            Current = AppSettings.HotkeyNone;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private static IntPtr WndProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg == WmHotkey && wParam.ToInt32() == IdHotkey)
            {
                Pressed?.Invoke();

                handled = true;
            }

            return IntPtr.Zero;
        }

        // ------------------------------------------------------------
        // Parsing / formatting
        // ------------------------------------------------------------

        /// <summary>
        /// Parse a "Ctrl+Shift+N"-style string into Win32 modifier flags and
        /// a virtual-key code. Requires at least one modifier so a bare key
        /// never swallows normal typing system-wide.
        /// </summary>
        public static bool TryParse(
            string? text,
            out uint modifiers,
            out uint virtualKey)
        {
            modifiers = 0;
            virtualKey = 0;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string[] parts = text.Split(
                new[] { '+' },
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries
            );

            bool hasKey = false;

            foreach (string part in parts)
            {
                switch (part.ToLowerInvariant())
                {
                    case "ctrl":
                    case "control":
                        modifiers |= ModControl;
                        continue;

                    case "alt":
                        modifiers |= ModAlt;
                        continue;

                    case "shift":
                        modifiers |= ModShift;
                        continue;

                    case "win":
                    case "windows":
                        modifiers |= ModWin;
                        continue;
                }

                if (Enum.TryParse(part, true, out Key key) && key != Key.None)
                {
                    virtualKey =
                        (uint)KeyInterop.VirtualKeyFromKey(key);

                    hasKey = true;
                }
                else
                {
                    return false;
                }
            }

            return hasKey && modifiers != 0;
        }

        /// <summary>
        /// Build a display string such as "Ctrl+Shift+N" from a pressed key
        /// and the active modifiers. Returns null when the press is only a
        /// modifier key (so the UI can keep waiting for a real key).
        /// </summary>
        public static string? Describe(Key key, ModifierKeys modifiers)
        {
            if (IsModifierKey(key))
            {
                return null;
            }

            var parts = new System.Collections.Generic.List<string>();

            if (modifiers.HasFlag(ModifierKeys.Control))
            {
                parts.Add("Ctrl");
            }

            if (modifiers.HasFlag(ModifierKeys.Alt))
            {
                parts.Add("Alt");
            }

            if (modifiers.HasFlag(ModifierKeys.Shift))
            {
                parts.Add("Shift");
            }

            if (modifiers.HasFlag(ModifierKeys.Windows))
            {
                parts.Add("Win");
            }

            parts.Add(KeyToFriendlyName(key));

            return string.Join("+", parts);
        }

        private static bool IsModifierKey(Key key)
        {
            return key is
                Key.LeftShift or Key.RightShift or
                Key.LeftCtrl or Key.RightCtrl or
                Key.LeftAlt or Key.RightAlt or
                Key.LWin or Key.RWin or
                Key.System;
        }

        private static string KeyToFriendlyName(Key key)
        {
            string name = key.ToString();

            // D0..D9 -> 0..9
            if (name.Length == 2 && name[0] == 'D' &&
                char.IsDigit(name[1]))
            {
                return name[1].ToString();
            }

            // NumPad0.. -> Num 0
            if (name.StartsWith("NumPad", StringComparison.Ordinal))
            {
                return "Num " + name.Substring(6);
            }

            return name;
        }

        // ------------------------------------------------------------
        // Win32
        // ------------------------------------------------------------

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(
            IntPtr hWnd,
            int id,
            uint fsModifiers,
            uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(
            IntPtr hWnd,
            int id);
    }
}

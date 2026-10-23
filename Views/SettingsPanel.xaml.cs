using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HoldMyNotes.Services;

namespace HoldMyNotes.Views
{
    public partial class SettingsPanel : UserControl
    {
        // ============================================================
        // STATE
        // ============================================================

        private bool _isLoaded;

        private string _pendingHotkey;


        public SettingsPanel()
        {
            InitializeComponent();

            _pendingHotkey =
                AppSettings.GlobalHotkey;

            Loaded +=
                (_, _) =>
                {
                    if (!_isLoaded)
                    {
                        _isLoaded = true;

                        PopulateFonts();

                        RefreshHotkeyBox();

                        RebuildPalette();
                    }
                };
        }


        // ============================================================
        // GLOBAL HOTKEY
        // ============================================================

        private void RefreshHotkeyBox()
        {
            HotkeyBox.Text =
                _pendingHotkey;

            HotkeyStatus.Text =
                string.Equals(
                        _pendingHotkey,
                        AppSettings.HotkeyNone,
                        StringComparison.OrdinalIgnoreCase)
                    ? "Hotkey is off. The All Notes view can still be opened from the tray icon."
                    : "Current hotkey: " + _pendingHotkey;
        }


        private void HotkeyBox_GotKeyboardFocus(
            object sender,
            KeyboardFocusChangedEventArgs e)
        {
            HotkeyBox.Text =
                "Press shortcut...";

            HotkeyStatus.Text =
                "Now press your desired key combination.";
        }


        private void HotkeyBox_PreviewMouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            /*
             * Keep the box focusable even though it is read-only, so the
             * user can click in and start recording a shortcut.
             */
            Keyboard.Focus(HotkeyBox);
        }


        private void HotkeyBox_PreviewKeyDown(
            object sender,
            KeyEventArgs e)
        {
            e.Handled = true;

            Key key =
                e.Key == Key.System ? e.SystemKey : e.Key;

            /*
             * Escape cancels recording without changing anything.
             */
            if (key == Key.Escape)
            {
                RefreshHotkeyBox();

                Keyboard.ClearFocus();

                return;
            }


            string? described =
                HotkeyService.Describe(
                    key,
                    Keyboard.Modifiers
                );

            if (described == null)
            {
                // Still holding a modifier; keep waiting for a real key.
                return;
            }


            _pendingHotkey =
                described;

            HotkeyBox.Text =
                described;

            HotkeyStatus.Text =
                "Click Save to apply this shortcut.";
        }


        private void HotkeyNone_Click(
            object sender,
            RoutedEventArgs e)
        {
            _pendingHotkey =
                AppSettings.HotkeyNone;

            SaveHotkey();

            Keyboard.ClearFocus();
        }


        private void HotkeySave_Click(
            object sender,
            RoutedEventArgs e)
        {
            SaveHotkey();

            Keyboard.ClearFocus();
        }


        private void SaveHotkey()
        {
            AppSettings.SetHotkey(
                _pendingHotkey
            );

            bool ok =
                HotkeyService.Apply(
                    AppSettings.GlobalHotkey
                );

            RefreshHotkeyBox();

            if (string.Equals(
                    AppSettings.GlobalHotkey,
                    AppSettings.HotkeyNone,
                    StringComparison.OrdinalIgnoreCase))
            {
                HotkeyStatus.Text =
                    "Hotkey cleared. Open All Notes from the tray icon instead.";
            }
            else if (ok)
            {
                HotkeyStatus.Text =
                    "Saved. Press " + AppSettings.GlobalHotkey + " from anywhere to open All Notes.";
            }
            else
            {
                HotkeyStatus.Text =
                    "That shortcut could not be registered — it may be used by another app. Try a different combination.";

                HotkeyStatus.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(179, 59, 50)
                    );
            }
        }


        // ============================================================
        // FONTS
        // ============================================================

        private void PopulateFonts()
        {
            HandwritingCombo.ItemsSource =
                FontCatalog.HandwritingFonts;

            HandwritingCombo.DisplayMemberPath =
                nameof(FontOption.DisplayName);

            HandwritingCombo.SelectedItem =
                FontCatalog.HandwritingFonts.FirstOrDefault(f =>
                    string.Equals(
                        f.Source,
                        AppSettings.NoteFontSource,
                        StringComparison.OrdinalIgnoreCase))
                ?? FontCatalog.HandwritingFonts[0];


            UiCombo.ItemsSource =
                FontCatalog.UiFonts;

            UiCombo.DisplayMemberPath =
                nameof(FontOption.DisplayName);

            UiCombo.SelectedItem =
                FontCatalog.UiFonts.FirstOrDefault(f =>
                    string.Equals(
                        f.Source,
                        AppSettings.UiFontSource,
                        StringComparison.OrdinalIgnoreCase))
                ?? FontCatalog.UiFonts[0];


            UpdatePreview();
        }


        private void HandwritingCombo_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (HandwritingCombo.SelectedItem is not FontOption option)
            {
                return;
            }

            AppSettings.SetNoteFont(
                option.Source
            );

            UpdatePreview();
        }


        private void UiCombo_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (UiCombo.SelectedItem is not FontOption option)
            {
                return;
            }

            AppSettings.SetUiFont(
                option.Source
            );

            UpdatePreview();
        }


        private void UpdatePreview()
        {
            if (PreviewHandwriting == null ||
                PreviewUi == null)
            {
                return;
            }

            PreviewHandwriting.FontFamily =
                new FontFamily(
                    AppSettings.NoteFontSource
                );

            PreviewUi.FontFamily =
                new FontFamily(
                    AppSettings.UiFontSource
                );
        }


        // ============================================================
        // NOTE COLORS
        // ============================================================

        private void RebuildPalette()
        {
            PalettePanel.Children.Clear();

            foreach (string color in NotePalette.AllColors)
            {
                PalettePanel.Children.Add(
                    CreateSwatch(color)
                );
            }
        }


        private UIElement CreateSwatch(string color)
        {
            bool isDefault =
                NotePalette.IsDefault(color);

            Border swatch =
                new Border
                {
                    Width = 40,
                    Height = 40,
                    CornerRadius = new CornerRadius(10),
                    Background =
                        (SolidColorBrush)new BrushConverter().ConvertFrom(color)!,
                    Margin = new Thickness(0, 0, 10, 10),
                    BorderBrush =
                        new SolidColorBrush(Color.FromRgb(224, 221, 212)),
                    BorderThickness = new Thickness(1),
                    ToolTip =
                        isDefault
                            ? color + " (default)"
                            : color + " — click to remove"
                };

            if (!isDefault)
            {
                swatch.Cursor = Cursors.Hand;

                swatch.MouseLeftButtonDown +=
                    (_, _) =>
                    {
                        NotePalette.RemoveCustom(color);

                        AppSettings.SaveCustomColors();

                        ColorStatus.Text =
                            "Removed " + color + " from the palette.";

                        RebuildPalette();
                    };
            }

            return swatch;
        }


        private void HexBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            // Not wired in XAML; placeholder to keep parity.
        }


        private void PickColor_Click(
            object sender,
            RoutedEventArgs e)
        {
            using System.Windows.Forms.ColorDialog dialog =
                new System.Windows.Forms.ColorDialog
                {
                    FullOpen = true,
                    AnyColor = true
                };

            if (dialog.ShowDialog() ==
                System.Windows.Forms.DialogResult.OK)
            {
                string hex =
                    string.Format(
                        "#{0:X2}{1:X2}{2:X2}{3:X2}",
                        255,
                        dialog.Color.R,
                        dialog.Color.G,
                        dialog.Color.B
                    );

                HexBox.Text =
                    hex;

                NewColorPreview.Background =
                    (SolidColorBrush)new BrushConverter().ConvertFrom(hex)!;
            }
        }


        private void AddColor_Click(
            object sender,
            RoutedEventArgs e)
        {
            string normalized =
                NotePalette.Normalize(HexBox.Text);

            if (string.IsNullOrEmpty(normalized))
            {
                ColorStatus.Text =
                    "That isn't a valid color. Use a hex value like #FFD75A or #FFFFD75A.";

                ColorStatus.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(179, 59, 50)
                    );

                return;
            }


            bool alreadyExists =
                NotePalette.AllColors.Any(c =>
                    string.Equals(
                        c,
                        normalized,
                        StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
            {
                ColorStatus.Text =
                    normalized + " is already in the palette.";

                ColorStatus.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(179, 59, 50)
                    );

                return;
            }


            NotePalette.AddCustom(normalized);

            AppSettings.SaveCustomColors();

            ColorStatus.Foreground =
                new SolidColorBrush(
                    Color.FromRgb(116, 113, 106)
                );

            ColorStatus.Text =
                "Added " + normalized + ". Open a note to use it.";

            NewColorPreview.Background =
                (SolidColorBrush)new BrushConverter().ConvertFrom(normalized)!;

            RebuildPalette();
        }
    }
}

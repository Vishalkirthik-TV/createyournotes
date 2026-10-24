# CreateYourNotes

**Your thoughts. Your space. Your notes.**

CreateYourNotes is a lightweight sticky-notes app for Windows. Keep notes close at the edge of your screen, open the full notes list when you need it, and get straight to writing.

[Download for Windows](https://github.com/Vishalkirthik-TV/createyournotes/releases/latest) · [Report an issue](https://github.com/Vishalkirthik-TV/createyournotes/issues)

## Features

- **Notes always within reach** — open the notes rail from the edge of your screen.
- **A clear view of everything** — search notes, filter active or archived notes, and preview them in one window.
- **A focused writing space** — notes save automatically as you work.
- **Make it yours** — choose note colours and fonts, and set a global keyboard shortcut.
- **Local and offline** — notes are stored on your PC; no account or internet connection is required.
- **Take notes with you** — export notes as `.txt` or `.json`.

## Get started

1. Visit the [latest releases](https://github.com/Vishalkirthik-TV/createyournotes/releases/latest) and check the available Windows download.
2. Install or run the app.
3. Open **All Notes** from the system tray icon, or create a note from its tray menu.
4. Open **Settings** to customize your colours, fonts, and keyboard shortcut.

> The app is designed for Windows 10 / 11 and later. Check the Releases page for the latest download options.

## Build from source

You'll need the **.NET 8 SDK** and Windows.

From the repository root, run:

```powershell
dotnet restore .\HoldMyNotes.sln
dotnet build .\HoldMyNotes.sln --configuration Release
```

## Your notes and privacy

Notes and preferences are stored locally in the app's SQLite database (`holdmynotes.db`). The app does not require an account or cloud sync. Export important notes from **All Notes** if you want a separate copy.

## Help and feedback

Found a problem or have an idea? [Open an issue](https://github.com/Vishalkirthik-TV/createyournotes/issues). Include your Windows version and a short description of what happened.
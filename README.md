# WildcardSingularity

A WPF desktop application (.NET 10, Windows) that randomly samples lines from text files on a timer and writes them to corresponding output files.

## What it does

1. Point it at a folder containing `.txt` files.
2. Check the files you want to process.
3. Set an interval (seconds) and press **Start**.
4. On each tick, one random non-empty line is read from each selected file and written to a `<basename>_ws.txt` file in the same folder.
5. Uncheck a file at any time — it will be skipped on the next tick without stopping the timer.

Settings (folder path, interval, and per-file selections) are saved automatically and restored on next launch.

## Tech stack

| Concern | Choice |
|---|---|
| UI | WPF (.NET 10, Windows) |
| Pattern | MVVM via `CommunityToolkit.Mvvm` |
| Folder dialog | `Ookii.Dialogs.Wpf` |
| Settings | `System.Text.Json` → `%AppData%\WildcardSingularity\settings.json` |

## Requirements

- Windows
- [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## Building

```
dotnet build
dotnet run
```

## Notes

- `_ws.txt` files in the watched folder are automatically excluded from the file list.
- Files with only blank lines are silently skipped (no output written).
- Per-file errors are reported in the status bar without stopping the timer.

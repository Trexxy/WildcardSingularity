# WildcardSingularity

A WPF desktop application (.NET 10, Windows) that randomly samples lines from text files on a timer and writes them to corresponding output files.

## What it does

**Files tab**

1. Point it at a folder containing `.txt` files.
2. Set a line count (≥ 1) next to each file you want to process — files with a count of 0 are inactive. Active files are highlighted for easy scanning.
3. Set an interval (seconds) and press **Start**.
4. On each tick, that many distinct random lines are read from each active file and written to `ws/<filename>` in the watched folder.
5. Change line counts at any time — the new value takes effect on the next tick without stopping the timer.

**Search & Replace tab**

- Add as many search/replace rule pairs as needed with **+ Add Rule**. Rules apply globally to every line selected from every active file, in order, before being written to the output. Source files are never modified.
- Save the current rule list as a named preset using the editable combo box and **Save**. **Load** replaces the current rules with the preset; **Delete** removes it. Presets do not affect line counts.

Settings (folder, interval, line counts, search/replace rules) and presets are saved automatically and restored on next launch.

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

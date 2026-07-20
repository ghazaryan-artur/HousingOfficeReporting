# CLAUDE.md

Context for Claude Code. A short overview of the project, where things live, how to build, what's done, and what still needs verification.

## About the project

Desktop application (WPF, .NET 8) for a housing office (ЖЭК) — a replacement for the manual Excel file `hamatirutyun2020.xlsm` that keeps the familiar look and carries no risk of breaking formulas. The UI is in Armenian (the user is an adult, non-technical). The original xlsm sits in the repo root and its data is imported on first launch.

**User:** communicates in Russian, but the application and all UI text are in Armenian. Do not translate UI strings without an explicit request.

## Stack and dependencies

- **.NET 8** (`net8.0-windows`), **WPF**
- **ClosedXML 0.104** — reads/writes xlsm/xlsx (the source file has no VBA, so exporting to xlsx is lossless)
- **Microsoft.Data.Sqlite 8** — working storage (one database per year)
- **CommunityToolkit.Mvvm 8.3** — `[ObservableProperty]`, `[RelayCommand]`

## Project layout

```
src/HousingOffice.App/
  Models/            House.cs, Resident.cs, YearSettings.cs
  Services/          DatabaseService.cs   ← SQLite CRUD + BulkImport
                     XlsmService.cs       ← Import/Export via ClosedXML
                     AutoSaveService.cs   ← DispatcherTimer debounce + heartbeat
                     HistoryService.cs    ← .db snapshots, 2-day rotation
                     AppPaths.cs          ← %LocalAppData%\HousingOffice
  ViewModels/        MainViewModel.cs, ResidentRowViewModel.cs, HouseListItem.cs
  Views/             AddHouseDialog, AddResidentDialog, NewYearDialog (all — Window)
  MainWindow.xaml    ← toolbar + house list on the left + DataGrid + status bar
  App.xaml           ← button styles, DataGrid, font
  HousingOffice.App.csproj
publish.ps1
hamatirutyun2020.xlsm   ← source file for the initial import
```

User data: `%LocalAppData%\HousingOffice\data_YYYY.db` + `history\`.

## Build / run (Windows)

```powershell
# debug
dotnet run --project src/HousingOffice.App

# release single-file exe
./publish.ps1
# → publish\HousingOffice.exe (~70 MB, self-contained win-x64)
```

## Key project decisions (don't change without a reason)

- **Formulas are not stored, they're computed in C#.** Read-only DataGrid columns: `PaidTotal` (=SUM of payments) and `FinalBalance` (=E−F+G·currentMonth−T−U). This is a literal implementation of requirement #1 — the user can't break a formula because there is no formula in the UI to break.
- **One SQLite file per year** (`data_2020.db`, `data_2021.db`, ...). Switching years means reopening the database. Simpler than a single file with a year column.
- **Autosave:** `AutoSaveService` — 2-second debounce after the last edit, plus a heartbeat every 5 minutes. `_dirtyResidents` is a HashSet of IDs, flushed in `FlushDirty()`.
- **History:** after every flush, `HistoryService.Snapshot()` copies the .db to `history/data_YYYY__stamp.db`; anything older than 2 days is deleted.
- **Year picker:** a small `📅 2020 ▾` button in the top-right corner — not hover-triggered (older users, accidental triggers), an explicit click opens a Popup instead.
- **Importing an xlsm wipes the whole year** (`BulkImport` runs `DELETE FROM ...` inside a transaction). Before importing, show a MessageBox warning and take a history snapshot.

## What hasn't been tested (this was assembled on macOS without dotnet)

The first thing to do in a new session on Windows is run the build and launch the app, since WPF couldn't be visually verified here:

1. `dotnet restore src/HousingOffice.App` — check that all packages resolve.
2. `dotnet build` — catch compiler errors (may be in XAML bindings).
3. `dotnet run` — open the app, click 📥 Ներմուծել Excel, select `hamatirutyun2020.xlsm`, verify that 60 houses appear in the sidebar.
4. Edit the `Մուտք 5` cell for any resident — after 2 seconds the status bar should show `Ավտոպահպանված HH:MM`, and the `Ընդանուր` and `Վերջն. մնացորդ` columns should recalculate.
5. Check the year popup (top-right corner) — opens on click, closes on click outside.
6. Export → open in Excel → confirm the SUM/total formulas were inserted.

Possible pitfalls to watch for:
- `Popup` with `PlacementTarget="{Binding ElementName=YearButton}"` and internal bindings via `ElementName=YearPopup` — WPF can be finicky about Popup namescopes; if commands inside the popup don't fire, check here first.
- `ComboBox SelectedItem="{Binding CurrentMonth}"` bound to an `x:Array` of Int32 elements — usually works, but if not, switch to `SelectedValue`/`SelectedValuePath`.
- DataGrid columns are narrow (60px for monthly payments) — Armenian headers are long and may not fit; check visually.

## The `verify` skill

After non-trivial code changes, run `verify` (launch the app and check the scenario visually) rather than relying only on build/typecheck. WPF bugs live in bindings and aren't caught by the compiler.

## How to proceed

If the user asks for a new feature, first check whether it violates requirement #1 (formulas must remain immutable). Anything that changes a computed value must go through editing the source fields, not through new "formulas in cells."

If the user asks for a UI change, stay as close as possible to the original Excel look (same column headers in Armenian, same order, large font, explicit buttons). The target audience is non-technical.

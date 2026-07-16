# HousingOffice — Համատիրության հաշվառում

A WPF application (.NET 8) for housing office (ЖЭК / համատիրություն) bookkeeping. A replacement for the manual Excel file `hamatirutyun2020.xlsm` that preserves the familiar look of the spreadsheet while removing the risk of accidentally breaking a formula.

The UI is in Armenian; this README is in English.

## Features

- List of houses on the left, residents table on the right — the layout mirrors the original Excel as closely as possible
- The **Ընդանուր** and **Վերջնական մնացորդ** columns are read-only and computed by the app; there is no formula the user can break
- **➕ Ավելացնել տուն** and **👤 Ավելացնել բնակիչ** buttons with confirmation dialogs
- Search over houses (left panel)
- Auto-save (2 s debounce plus a 5-minute heartbeat)
- History: a SQLite snapshot is written after every save to `%LocalAppData%\HousingOffice\history\`, auto-cleaned after 2 days
- Import from `.xlsm` / `.xlsx` (**📥 Ներմուծել Excel** button) — you can feed the original housing-office file in directly
- Export to `.xlsx` (**📤 Արտահանել Excel** button) — restores the SUM / total formulas
- Year navigation: a small `📅 2020 ▾` button in the top-right corner; each year is a separate database file `data_YYYY.db`
- Single `.exe`, no console commands required to launch

## Where data is stored

`%LocalAppData%\HousingOffice\`
```
data_2020.db       ← main database for the year
data_2021.db
history\           ← snapshots from the last 2 days
    data_2020__2026-07-15_10-42-11.db
    ...
```

## Build (on Windows)

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone <repo>
cd HousingOfficeReporting
./publish.ps1
```

The resulting `publish\HousingOffice.exe` (~70 MB) can be copied to the target machine as-is — no .NET Runtime install is needed (self-contained).

## First run

1. Launch `HousingOffice.exe` by double-clicking it.
2. An empty window appears with the hint «Դատարկ բազա — սեղմեք 📥 Ներմուծել Excel».
3. Click **📥 Ներմուծել Excel**, pick `hamatirutyun2020.xlsm` — all houses are imported into SQLite.
4. From then on, work inside the app. Excel is no longer needed.

## Project layout

```
src/HousingOffice.App/
    Models/                     ← House, Resident, YearSettings
    Services/                   ← DatabaseService, XlsmService, AutoSaveService, HistoryService, AppPaths
    ViewModels/                 ← MainViewModel, ResidentRowViewModel, HouseListItem
    Views/                      ← AddHouseDialog, AddResidentDialog, NewYearDialog
    MainWindow.xaml             ← main window (toolbar, side list, DataGrid)
    App.xaml                    ← button / grid styles
    HousingOffice.App.csproj
publish.ps1                     ← builds the single-file exe
hamatirutyun2020.xlsm           ← source file (for the initial import)
```

## Development on Windows

```powershell
dotnet restore src/HousingOffice.App
dotnet run --project src/HousingOffice.App
```

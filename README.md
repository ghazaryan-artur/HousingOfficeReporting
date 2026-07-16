# HousingOffice — Համատիրության հաշվառում

WPF-приложение (.NET 8) для ведения отчётности ЖЭК-а. Замена ручного Excel-файла `hamatirutyun2020.xlsm` с сохранением привычного вида таблиц и без риска случайно испортить формулы.

## Что умеет

- Список домов слева, таблица жильцов справа — вид максимально повторяет исходный Excel
- Колонки **Ընդանուր** и **Վերջնական մնացորդ** — read-only, считаются программой; изменить формулу невозможно
- Кнопки «➕ Ավելացնել տուն» и «👤 Ավելացնել բնակիչ» с подтверждением
- Поиск по домам (левая панель)
- Автосохранение (debounce 2 сек + heartbeat каждые 5 мин)
- История: снапшот SQLite после каждого сохранения в `%LocalAppData%\HousingOffice\history\`, авточистка через 2 дня
- Импорт из `.xlsm`/`.xlsx` (кнопка **📥 Ներմուծել Excel**) — можно взять исходный файл ЖЭК-а
- Экспорт в `.xlsx` (кнопка **📤 Արտահանել Excel**) — восстанавливает формулы SUM/итогов
- Навигация по годам: маленькая кнопка `📅 2020 ▾` в правом верхнем углу, каждый год — отдельная БД `data_YYYY.db`
- Один `.exe`, никаких консольных команд для запуска

## Где что хранится

`%LocalAppData%\HousingOffice\`
```
data_2020.db       ← основная БД года
data_2021.db
history\           ← снапшоты за последние 2 дня
    data_2020__2026-07-15_10-42-11.db
    ...
```

## Сборка (на Windows)

Требуется [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
git clone <repo>
cd HousingOfficeReporting
./publish.ps1
```

Готовый `publish\HousingOffice.exe` (~70 МБ) можно скопировать на целевой компьютер — .NET Runtime ставить не нужно (self-contained).

## Первый запуск

1. Запустить `HousingOffice.exe` двойным кликом.
2. Появится пустое окно с подсказкой «Դատարկ բազա — սեղմեք 📥 Ներմուծել Excel».
3. Нажать **📥 Ներմուծել Excel**, выбрать `hamatirutyun2020.xlsm` — все дома импортируются в SQLite.
4. Дальше работать в приложении. Excel больше не нужен.

## Структура проекта

```
src/HousingOffice.App/
    Models/                     ← House, Resident, YearSettings
    Services/                   ← DatabaseService, XlsmService, AutoSaveService, HistoryService, AppPaths
    ViewModels/                 ← MainViewModel, ResidentRowViewModel, HouseListItem
    Views/                      ← AddHouseDialog, AddResidentDialog, NewYearDialog
    MainWindow.xaml             ← главное окно (тулбар, боковой список, DataGrid)
    App.xaml                    ← стили кнопок/грида
    HousingOffice.App.csproj
publish.ps1                     ← сборка single-file exe
hamatirutyun2020.xlsm           ← исходный файл (для первого импорта)
```

## Разработка на Windows

```powershell
dotnet restore src/HousingOffice.App
dotnet run --project src/HousingOffice.App
```

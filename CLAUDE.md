# CLAUDE.md

Контекст для Claude Code. Кратко о проекте, где что лежит, как собирать, что уже сделано и что осталось проверить.

## О проекте

Десктоп-приложение (WPF, .NET 8) для ЖЭК-а — замена ручного Excel-файла `hamatirutyun2020.xlsm` с сохранением привычного вида и без риска испортить формулы. Интерфейс на армянском (пользователь — взрослый, не технарь). Оригинальный xlsm лежит в корне, из него импортируются данные при первом запуске.

**Пользователь:** пишет по-русски, но приложение и все тексты в UI — на армянском. Не переводить UI-строки без явной просьбы.

## Стек и зависимости

- **.NET 8** (`net8.0-windows`), **WPF**
- **ClosedXML 0.104** — чтение/запись xlsm/xlsx (в исходнике VBA нет, поэтому экспорт в xlsx — без потерь)
- **Microsoft.Data.Sqlite 8** — рабочее хранилище (одна БД на год)
- **CommunityToolkit.Mvvm 8.3** — `[ObservableProperty]`, `[RelayCommand]`

## Раскладка проекта

```
src/HousingOffice.App/
  Models/            House.cs, Resident.cs, YearSettings.cs
  Services/          DatabaseService.cs   ← SQLite CRUD + BulkImport
                     XlsmService.cs       ← Import/Export ClosedXML
                     AutoSaveService.cs   ← DispatcherTimer debounce + heartbeat
                     HistoryService.cs    ← снапшоты .db, ротация 2 дня
                     AppPaths.cs          ← %LocalAppData%\HousingOffice
  ViewModels/        MainViewModel.cs, ResidentRowViewModel.cs, HouseListItem.cs
  Views/             AddHouseDialog, AddResidentDialog, NewYearDialog (все — Window)
  MainWindow.xaml    ← тулбар + список домов слева + DataGrid + status bar
  App.xaml           ← стили кнопок, DataGrid, шрифт
  HousingOffice.App.csproj
publish.ps1
hamatirutyun2020.xlsm   ← исходник для первого импорта
```

Данные пользователя: `%LocalAppData%\HousingOffice\data_YYYY.db` + `history\`.

## Сборка / запуск (Windows)

```powershell
# отладка
dotnet run --project src/HousingOffice.App

# релиз single-file exe
./publish.ps1
# → publish\HousingOffice.exe (~70 MB, self-contained win-x64)
```

## Ключевые проектные решения (не менять без причины)

- **Формулы не хранятся, а вычисляются в C#.** Read-only колонки в DataGrid: `PaidTotal` (=SUM платежей) и `FinalBalance` (=E−F+G·currentMonth−T−U). Это буквальное выполнение п.1 требований — пользователь не может испортить формулу, потому что её просто нет в UI.
- **Один SQLite-файл на год** (`data_2020.db`, `data_2021.db`, ...). Переключение года = переоткрытие БД. Так проще, чем один файл с колонкой year.
- **Автосейв:** `AutoSaveService` — debounce 2 сек после последнего изменения, плюс heartbeat каждые 5 мин. `_dirtyResidents` — HashSet ID, флашится в `FlushDirty()`.
- **История:** после каждого флаша `HistoryService.Snapshot()` копирует .db в `history/data_YYYY__stamp.db`, старше 2 дней удаляется.
- **Год-пикер:** маленькая кнопка `📅 2020 ▾` в правом верхнем углу — не hover (пожилые пользователи, случайные срабатывания), а явный клик → Popup.
- **Импорт xlsm затирает год целиком** (`BulkImport` делает `DELETE FROM ...` в транзакции). Перед импортом — MessageBox с предупреждением и снапшот в history.

## Что НЕ протестировано (собрано на macOS без dotnet)

Первое, что стоит сделать в новой сессии на Windows — прогнать сборку и запуск, потому что WPF я тут не мог проверить визуально:

1. `dotnet restore src/HousingOffice.App` — проверить, что все пакеты подтягиваются.
2. `dotnet build` — поймать компиляторные ошибки (могут быть в bindings XAML).
3. `dotnet run` — открыть, нажать 📥 Ներմուծել Excel, выбрать `hamatirutyun2020.xlsm`, проверить что 60 домов появились в сайдбаре.
4. Отредактировать ячейку `Մուտք 5` у любого жильца — через 2 сек в статус-баре должно появиться `Ավտոպահպանված HH:MM`, колонки `Ընդանուր` и `Վերջն. մնացորդ` должны пересчитаться.
5. Проверить попап года (правый верхний угол) — открывается по клику, закрывается по клику вне.
6. Экспорт → открыть в Excel → убедиться, что формулы SUM/итога подставились.

Возможные подводные камни, за которыми стоит следить:
- `Popup` с `PlacementTarget="{Binding ElementName=YearButton}"` и внутренние привязки `ElementName=YearPopup` — WPF иногда капризничает с namescope у Popup; если команды в попапе не срабатывают, это первое место.
- `ComboBox SelectedItem="{Binding CurrentMonth}"` с `x:Array` Int32-элементов — обычно работает, но если нет — заменить на `SelectedValue`/`SelectedValuePath`.
- Колонки DataGrid узкие (60px для месячных платежей) — армянские заголовки длинные, могут не влезать; проверить визуально.

## Скилл `verify`

После нетривиальных правок кода — прогонять `verify` (запуск приложения и проверка сценария глазами), а не только build/typecheck. WPF-баги живут в биндингах и не ловятся компилятором.

## Как продолжать

Если пользователь просит добавить фичу — сначала посмотреть, не нарушает ли она п.1 требований (формулы неизменяемы). Всё, что меняет вычисляемое значение — только через изменение исходных полей, не через новые «формулы в клетках».

Если пользователь просит поменять UI — держаться максимально близко к виду Excel (те же заголовки колонок армянскими буквами, тот же порядок, крупный шрифт, явные кнопки). Целевая аудитория — не технари.

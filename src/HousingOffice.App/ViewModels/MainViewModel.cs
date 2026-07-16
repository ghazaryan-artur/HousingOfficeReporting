using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HousingOffice.Models;
using HousingOffice.Services;
using HousingOffice.Views;

namespace HousingOffice.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private DatabaseService _db = null!;
    private AutoSaveService _autoSave = null!;
    private readonly HistoryService _history;
    private readonly HashSet<long> _dirtyResidents = new();
    private YearSettings _settings = new();

    public ObservableCollection<int> AvailableYears { get; } = new();
    public ObservableCollection<HouseListItem> Houses { get; } = new();
    public ICollectionView HousesView { get; }
    public ObservableCollection<ResidentRowViewModel> Residents { get; } = new();

    [ObservableProperty] private int currentYear;
    [ObservableProperty] private HouseListItem? selectedHouse;
    [ObservableProperty] private string housesFilter = "";
    [ObservableProperty] private int currentMonth = 12;
    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private bool isYearPickerOpen;

    public MainViewModel()
    {
        HousesView = CollectionViewSource.GetDefaultView(Houses);
        HousesView.Filter = FilterHouse;

        DiscoverYears();
        var year = AvailableYears.Count > 0 ? AvailableYears.Max() : DateTime.Now.Year;
        _history = new HistoryService(AppPaths.DataDir);
        SwitchYear(year);
    }

    private bool FilterHouse(object obj)
    {
        if (obj is not HouseListItem h) return false;
        if (string.IsNullOrWhiteSpace(HousesFilter)) return true;
        return h.DisplayName.Contains(HousesFilter, StringComparison.OrdinalIgnoreCase);
    }

    partial void OnHousesFilterChanged(string value) => HousesView.Refresh();

    partial void OnSelectedHouseChanged(HouseListItem? value)
    {
        LoadResidents();
    }

    partial void OnCurrentMonthChanged(int value)
    {
        _db.SaveCurrentMonth(value);
        foreach (var r in Residents) r.RaiseComputed();
        _autoSave?.MarkDirty();
    }

    private void DiscoverYears()
    {
        AvailableYears.Clear();
        var years = Directory.EnumerateFiles(AppPaths.DataDir, "data_*.db")
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .Select(n => n.StartsWith("data_") && int.TryParse(n[5..], out var y) ? y : -1)
            .Where(y => y > 0)
            .Distinct()
            .OrderBy(y => y);
        foreach (var y in years) AvailableYears.Add(y);
        if (AvailableYears.Count == 0) AvailableYears.Add(DateTime.Now.Year);
    }

    public void SwitchYear(int year)
    {
        _autoSave?.FlushNow();
        _autoSave?.Dispose();
        _db = new DatabaseService(year, AppPaths.DbPathForYear(year));
        _settings = _db.LoadSettings();
        CurrentYear = year;
        CurrentMonth = _settings.CurrentMonth;

        if (!AvailableYears.Contains(year))
        {
            var list = AvailableYears.Append(year).OrderBy(y => y).ToList();
            AvailableYears.Clear();
            foreach (var y in list) AvailableYears.Add(y);
        }

        _autoSave = new AutoSaveService(FlushDirty);
        _autoSave.Saved += t => StatusText = $"Ավտոպահպանված {t:HH:mm}";

        ReloadHouses();
        StatusText = Houses.Count == 0
            ? $"Տարի {year} • Դատարկ բազա — սեղմեք 📥 Ներմուծել Excel"
            : $"Տարի {year} • {Houses.Count} տուն";
    }

    public void ReloadHouses()
    {
        long? prev = SelectedHouse?.Id;
        Houses.Clear();
        foreach (var h in _db.ListHouses())
            Houses.Add(new HouseListItem(h));
        SelectedHouse = prev.HasValue
            ? Houses.FirstOrDefault(x => x.Id == prev.Value) ?? Houses.FirstOrDefault()
            : Houses.FirstOrDefault();
    }

    private void LoadResidents()
    {
        Residents.Clear();
        if (SelectedHouse == null) return;
        var currentMonthProvider = new Func<int>(() => CurrentMonth);
        foreach (var r in _db.ListResidents(SelectedHouse.Id))
            Residents.Add(new ResidentRowViewModel(r, currentMonthProvider, MarkDirty));
    }

    private void MarkDirty(ResidentRowViewModel r)
    {
        _dirtyResidents.Add(r.Id);
        _autoSave.MarkDirty();
    }

    private void FlushDirty()
    {
        if (_dirtyResidents.Count == 0) return;
        var toSave = Residents.Where(r => _dirtyResidents.Contains(r.Id)).ToList();
        foreach (var vm in toSave) _db.UpdateResident(vm.Model);
        _dirtyResidents.Clear();
        _history.Snapshot(_db.DbPath);
    }

    [RelayCommand]
    private void AddHouse()
    {
        var dlg = new AddHouseDialog();
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.HouseName))
        {
            var id = _db.AddHouse(dlg.HouseName.Trim(), dlg.Street, dlg.Number);
            ReloadHouses();
            SelectedHouse = Houses.FirstOrDefault(h => h.Id == id);
            _history.Snapshot(_db.DbPath);
        }
    }

    [RelayCommand]
    private void AddResident()
    {
        if (SelectedHouse == null) return;
        var dlg = new AddResidentDialog();
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.FullName))
        {
            _db.AddResident(SelectedHouse.Id, dlg.FullName.Trim(), dlg.SquareMeters, dlg.DebitDebt, dlg.MonthlyCharge);
            LoadResidents();
            _history.Snapshot(_db.DbPath);
        }
    }

    [RelayCommand]
    private void DeleteResident(ResidentRowViewModel? r)
    {
        if (r == null) return;
        var res = System.Windows.MessageBox.Show(
            $"Հեռացնե՞լ բնակիչ «{r.FullName}»?",
            "Հաստատում", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (res != System.Windows.MessageBoxResult.Yes) return;
        _db.DeleteResident(r.Id);
        LoadResidents();
        _history.Snapshot(_db.DbPath);
    }

    [RelayCommand]
    private void DeleteHouse()
    {
        if (SelectedHouse == null) return;
        var res = System.Windows.MessageBox.Show(
            $"Հեռացնե՞լ ամբողջ տունը «{SelectedHouse.DisplayName}» բոլոր բնակիչներով?",
            "Հաստատում", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (res != System.Windows.MessageBoxResult.Yes) return;
        _db.DeleteHouse(SelectedHouse.Id);
        ReloadHouses();
        _history.Snapshot(_db.DbPath);
    }

    [RelayCommand]
    private void ImportXlsm()
    {
        var ofd = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel workbook (*.xlsm;*.xlsx)|*.xlsm;*.xlsx",
            Title = "Ընտրել ֆայլ ներմուծելու համար",
        };
        if (ofd.ShowDialog() != true) return;

        var res = System.Windows.MessageBox.Show(
            $"Ներմուծել {Path.GetFileName(ofd.FileName)}?\n" +
            $"ԱՄԲՈՂՋ {CurrentYear}թ. տվյալները կփոխարինվեն նոր ֆայլի պարունակությամբ.",
            "Հաստատել ներմուծումը", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
        if (res != System.Windows.MessageBoxResult.Yes) return;

        _autoSave.FlushNow();
        _history.Snapshot(_db.DbPath);

        var xlsm = new XlsmService();
        var result = xlsm.Import(ofd.FileName);
        _db.BulkImport(result.Houses, result.CurrentMonth);
        _settings = _db.LoadSettings();
        CurrentMonth = _settings.CurrentMonth;
        ReloadHouses();
        StatusText = $"Ներմուծված է {result.Houses.Count} տուն";
    }

    [RelayCommand]
    private void ExportXlsm()
    {
        var sfd = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            FileName = $"hamatirutyun{CurrentYear}.xlsx",
            Title = "Արտահանել որպես Excel",
        };
        if (sfd.ShowDialog() != true) return;

        _autoSave.FlushNow();
        var xlsm = new XlsmService();
        var data = _db.ListHouses()
            .Select(h => (h, _db.ListResidents(h.Id)))
            .ToList();
        xlsm.Export(sfd.FileName, data, CurrentMonth, CurrentYear);
        StatusText = $"Արտահանված է՝ {Path.GetFileName(sfd.FileName)}";
    }

    [RelayCommand]
    private void OpenYearPicker() => IsYearPickerOpen = true;

    [RelayCommand]
    private void PickYear(int year)
    {
        IsYearPickerOpen = false;
        if (year == CurrentYear) return;
        SwitchYear(year);
    }

    [RelayCommand]
    private void CreateNewYear()
    {
        IsYearPickerOpen = false;
        var dlg = new NewYearDialog(AvailableYears.Max() + 1);
        if (dlg.ShowDialog() == true && dlg.Year > 0)
            SwitchYear(dlg.Year);
    }

    public void Dispose()
    {
        _autoSave?.FlushNow();
        _autoSave?.Dispose();
    }
}

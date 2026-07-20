using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HousingOffice.Services;

namespace HousingOffice.ViewModels;

public partial class ChangeHistoryViewModel : ObservableObject
{
    private readonly DatabaseService _db;

    public ObservableCollection<ChangeLogRowViewModel> Changes { get; } = new();

    [ObservableProperty] private bool hasChanges;

    public ChangeHistoryViewModel(DatabaseService db)
    {
        _db = db;
        Reload();
    }

    private void Reload()
    {
        Changes.Clear();
        foreach (var e in _db.ListRecentChanges(100))
            Changes.Add(new ChangeLogRowViewModel(e));
        HasChanges = Changes.Count > 0;
    }

    [RelayCommand]
    private void Undo(ChangeLogRowViewModel? row)
    {
        if (row == null || row.Undone) return;
        var result = _db.UndoChange(row.Id);
        if (result == null)
        {
            MessageBox.Show(
                "Այս փոփոխությունն այլևս հնարավոր չէ հետարկել (միգուցե բնակիչը հեռացվել է, կամ արդեն հետարկված է)։",
                "Հնարավոր չէ հետարկել", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        Reload();
    }
}

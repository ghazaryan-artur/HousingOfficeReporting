using CommunityToolkit.Mvvm.ComponentModel;
using HousingOffice.Models;

namespace HousingOffice.ViewModels;

public partial class HouseListItem : ObservableObject
{
    public House Model { get; }
    public HouseListItem(House model) { Model = model; }

    public long Id => Model.Id;
    public string Name => Model.Name;
    public string DisplayName => Model.Name.Replace('_', ' ');
}

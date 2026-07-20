using System.Windows;
using HousingOffice.ViewModels;

namespace HousingOffice.Views;

public partial class EditModeDialog : Window
{
    public EditModeDialog(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

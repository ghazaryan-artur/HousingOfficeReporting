using System.Windows;
using HousingOffice.Services;
using HousingOffice.ViewModels;

namespace HousingOffice.Views;

public partial class ChangeHistoryDialog : Window
{
    public ChangeHistoryDialog(DatabaseService db)
    {
        InitializeComponent();
        DataContext = new ChangeHistoryViewModel(db);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

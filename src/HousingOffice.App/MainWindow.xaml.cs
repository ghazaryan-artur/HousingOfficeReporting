using System.Windows;
using HousingOffice.ViewModels;

namespace HousingOffice;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, _) => (DataContext as MainViewModel)?.Dispose();
    }
}

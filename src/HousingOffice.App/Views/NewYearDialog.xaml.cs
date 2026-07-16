using System.Windows;

namespace HousingOffice.Views;

public partial class NewYearDialog : Window
{
    public int Year { get; private set; }

    public NewYearDialog(int suggested)
    {
        InitializeComponent();
        YearBox.Text = suggested.ToString();
        YearBox.SelectAll();
        YearBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(YearBox.Text.Trim(), out var y) || y < 1900 || y > 2200)
        {
            MessageBox.Show("Մուտքագրեք ճիշտ տարի (օր. 2025)", "Սխալ",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Year = y;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

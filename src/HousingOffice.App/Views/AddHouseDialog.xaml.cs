using System.Windows;

namespace HousingOffice.Views;

public partial class AddHouseDialog : Window
{
    public string? Street { get; private set; }
    public string? Number { get; private set; }
    public string HouseName { get; private set; } = "";

    public AddHouseDialog()
    {
        InitializeComponent();
        StreetBox.TextChanged += (_, _) => Recompose();
        NumberBox.TextChanged += (_, _) => Recompose();
    }

    private void Recompose()
    {
        var s = StreetBox.Text.Trim().Replace(' ', '_');
        var n = NumberBox.Text.Trim();
        NameBox.Text = string.IsNullOrEmpty(n) ? s : $"{s}_{n}";
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Անվանումը դատարկ չի կարող լինել", "Սխալ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var confirm = MessageBox.Show($"Ավելացնե՞լ նոր տուն «{name}»?", "Հաստատում",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        Street = string.IsNullOrWhiteSpace(StreetBox.Text) ? null : StreetBox.Text.Trim();
        Number = string.IsNullOrWhiteSpace(NumberBox.Text) ? null : NumberBox.Text.Trim();
        HouseName = name;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

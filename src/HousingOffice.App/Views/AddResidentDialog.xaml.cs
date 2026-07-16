using System.Globalization;
using System.Windows;

namespace HousingOffice.Views;

public partial class AddResidentDialog : Window
{
    public string FullName { get; private set; } = "";
    public double? SquareMeters { get; private set; }
    public double DebitDebt { get; private set; }
    public double MonthlyCharge { get; private set; }

    public AddResidentDialog()
    {
        InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Անունը դատարկ չի կարող լինել", "Սխալ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseOptional(SquareBox.Text, out var sq) ||
            !TryParseRequired(DebitBox.Text, out var d) ||
            !TryParseRequired(ChargeBox.Text, out var m))
        {
            MessageBox.Show("Թվային դաշտում ոչ ճիշտ արժեք է", "Սխալ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show($"Ավելացնե՞լ բնակիչ «{name}»?", "Հաստատում",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        FullName = name;
        SquareMeters = sq;
        DebitDebt = d;
        MonthlyCharge = m;
        DialogResult = true;
        Close();
    }

    private static bool TryParseOptional(string s, out double? v)
    {
        if (string.IsNullOrWhiteSpace(s)) { v = null; return true; }
        if (TryParseRequired(s, out var x)) { v = x; return true; }
        v = null; return false;
    }

    private static bool TryParseRequired(string s, out double v)
    {
        s = s.Trim().Replace(',', '.');
        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out v);
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

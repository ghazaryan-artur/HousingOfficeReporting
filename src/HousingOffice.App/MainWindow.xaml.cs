using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HousingOffice.ViewModels;

namespace HousingOffice;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, _) => (DataContext as MainViewModel)?.Dispose();

        // Popups use StaysOpen="True" and the buttons toggle IsOpen directly, so opening/closing
        // is fully under our control here instead of racing WPF's built-in StaysOpen="False"
        // outside-click dismissal (whose timing relative to the button's own Click isn't guaranteed).
        PreviewMouseDown += (_, e) => CloseIfOutside(HouseButton, HousePopup, e);
        PreviewMouseDown += (_, e) => CloseIfOutside(YearButton, YearPopup, e);
    }

    private static void CloseIfOutside(UIElement button, System.Windows.Controls.Primitives.Popup popup, MouseButtonEventArgs e)
    {
        if (!popup.IsOpen) return;
        var source = e.OriginalSource as DependencyObject;
        if (source == null) return;
        if (button == source || IsAncestor(button, source)) return;
        if (popup.Child != null && (popup.Child == source || IsAncestor(popup.Child, source))) return;
        popup.IsOpen = false;
    }

    private static bool IsAncestor(DependencyObject ancestor, DependencyObject node)
        => ancestor is Visual visual && node is Visual descendant && visual.IsAncestorOf(descendant);
}

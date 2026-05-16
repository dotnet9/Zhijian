using Avalonia.Interactivity;
using Avalonia.Input;
using AtomUI.Desktop.Controls;

namespace Zhijian.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void HandleFileMenuClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
        {
            return;
        }

        menuItem.IsSubMenuOpen = true;
        e.Handled = true;
    }

    private void HandleFileMenuPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
        {
            return;
        }

        menuItem.IsSubMenuOpen = true;
        e.Handled = true;
    }
}

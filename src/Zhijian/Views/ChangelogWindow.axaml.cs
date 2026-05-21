using AtomUI.Controls;
using AtomUI.Theme;
using Avalonia;
using Zhijian.ViewModels;
using AtomWindow = AtomUI.Desktop.Controls.Window;

namespace Zhijian.Views;

public partial class ChangelogWindow : AtomWindow
{
    private const string DarkClass = "dark";

    public ChangelogWindow()
    {
        InitializeComponent();
        DataContext = new ChangelogWindowViewModel();
        SetDarkThemeClass(Application.Current?.IsDarkThemeMode() ?? false);

        if (Application.Current?.GetThemeManager() is { } themeManager)
        {
            themeManager.BindingSource.PropertyChanged += HandleThemeManagerPropertyChanged;
            Closed += (_, _) => themeManager.BindingSource.PropertyChanged -= HandleThemeManagerPropertyChanged;
        }
    }

    private void HandleThemeManagerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IThemeManager.IsDarkThemeModeProperty)
        {
            SetDarkThemeClass(e.GetNewValue<bool>());
        }
    }

    private void SetDarkThemeClass(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            if (!Classes.Contains(DarkClass))
            {
                Classes.Add(DarkClass);
            }

            return;
        }

        Classes.Remove(DarkClass);
    }
}

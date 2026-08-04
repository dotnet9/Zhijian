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

        if (Application.Current?.GetThemeManager() is { } themeManager)
        {
            SetDarkThemeClass(themeManager.CurrentTheme?.Appearance == ThemeAppearance.Dark);
            themeManager.ThemeChanged += HandleThemeChanged;
            Closed += (_, _) => themeManager.ThemeChanged -= HandleThemeChanged;
        }
    }

    private void HandleThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        SetDarkThemeClass(e.State.Appearance == ThemeAppearance.Dark);
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

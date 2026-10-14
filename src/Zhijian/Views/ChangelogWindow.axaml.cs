using Avalonia;
using Zhijian.ViewModels;
using AppWindow = CodeWF.Avalonia.Controls.Controls.CodeWFWindow;

namespace Zhijian.Views;

public partial class ChangelogWindow : AppWindow
{
    private const string DarkClass = "dark";

    public ChangelogWindow()
    {
        InitializeComponent();
        DataContext = new ChangelogWindowViewModel();

        ActualThemeVariantChanged += (_, _) => RefreshTheme();
        RefreshTheme();
    }

    private void RefreshTheme() => SetDarkThemeClass(ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark || ActualThemeVariant.InheritVariant == global::Avalonia.Styling.ThemeVariant.Dark);

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

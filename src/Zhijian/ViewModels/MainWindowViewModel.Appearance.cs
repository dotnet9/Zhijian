using CodeWF.Avalonia.Controls.Themes;
using global::Avalonia.Media;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel
{
    private bool _updatingHostTheme;
    public Color AccentColor => global::Avalonia.Application.Current is { } app
        && app.TryGetResource("StudioAccentColor", app.ActualThemeVariant, out var value) && value is Color color
        ? color : Color.Parse(IsDarkTheme ? "#AE96FF" : "#6745D6");
    public void SelectPaletteByKey(string key)
    {
        if (App.ThemeController is { } controller && Enum.TryParse<StudioPalette>(key, out var palette))
        {
            controller.Palette = palette;
            OnPropertyChanged(nameof(AccentColor));
            App.SaveThemePreferences();
        }
    }
    public void SetSystemTheme()
    {
        if (App.ThemeController is { } controller) controller.Mode = StudioThemeMode.System;
        App.SaveThemePreferences();
    }
    public void UpdateThemeFromHost(bool dark)
    {
        _updatingHostTheme = true;
        try { IsDarkTheme = dark; OnPropertyChanged(nameof(AccentColor)); }
        finally { _updatingHostTheme = false; }
    }
}

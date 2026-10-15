using CodeWF.Avalonia.Controls.Themes;
using global::Avalonia.Media;
using Avalonia.Controls;
using CodeWF.Toolkit.Lang;
using Zhijian.Services;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel
{
    public IReadOnlyList<TitleBarPalette> TitleBarPalettes { get; } =
        new[] { "DotNet", "Aurora", "Ember", "Default", "Aquatic", "Desert", "Dusk", "NightSky" }
            .Select(key => new TitleBarPalette(key)).ToArray();
    public IReadOnlyList<LocalizationLanguage> TitleBarLanguages { get; } = I18nManager.Instance.GetLanguages() ?? [];
    public TitleBarPalette SelectedTitleBarPalette
    {
        get => TitleBarPalettes.Single(item => item.Key == (App.ThemeController?.Palette ?? StudioPalette.Aurora).ToString());
        set { if (value is not null) SelectPaletteByKey(value.Key); }
    }
    public StudioThemeMode ThemeMode
    {
        get => App.ThemeController?.Mode ?? StudioThemeMode.Light;
        set
        {
            if (App.ThemeController is not { } controller || controller.Mode == value) return;
            controller.Mode = value;
            RefreshThemeSelection();
            App.SaveThemePreferences();
        }
    }
    public LocalizationLanguage? SelectedTitleBarLanguage
    {
        get => TitleBarLanguages.FirstOrDefault(item => item.CultureName == I18nManager.Instance.Culture?.Name);
        set { if (value is not null) SetLanguage(value.CultureName); }
    }
    public void UpdateLanguageFromHost()
    {
        _selectedCultureName = I18nManager.Instance.Culture?.Name ?? ApplicationSettings.DefaultCultureName;
        RefreshLocalizedProperties();
        OnPropertyChanged(nameof(SelectedTitleBarLanguage));
    }
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
            OnPropertyChanged(nameof(SelectedTitleBarPalette));
            App.SaveThemePreferences();
        }
    }
    public void SetSystemTheme()
    {
        if (App.ThemeController is { } controller) controller.Mode = StudioThemeMode.System;
        RefreshThemeSelection();
        App.SaveThemePreferences();
    }
    private void RefreshThemeSelection()
    {
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsExplicitDarkTheme));
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(ThemeMode));
        OnPropertyChanged(nameof(SelectedTitleBarPalette));
    }

    public void UpdateThemeFromHost(bool dark)
    {
        _updatingHostTheme = true;
        try { IsDarkTheme = dark; OnPropertyChanged(nameof(AccentColor)); }
        finally { _updatingHostTheme = false; }
        RefreshThemeSelection();
    }
}

public sealed record TitleBarPalette(string Key)
{
    public string ResourceKey => "Zhijian.ZhijianL.Palette" + Key;
    public IBrush Swatch => global::Avalonia.Application.Current is { } app && app.TryFindResource("StudioSwatch" + Key, out var value) && value is IBrush brush
        ? brush : Brushes.Transparent;
}

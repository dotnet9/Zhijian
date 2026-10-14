using Avalonia;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Markup.Xaml;
using CodeWF.Avalonia.Lang;
using CodeWF.Avalonia.Lang.Json;
using System.Globalization;
using Zhijian.Services;
using Zhijian.ViewModels;
using Zhijian.Views;

namespace Zhijian;

public partial class App : Application
{
    public override void Initialize()
    {
        base.Initialize();
        AvaloniaXamlLoader.Load(this);

        var langPlugin = new JsonLangPlugin
        {
            ResourceFolder = ResolveI18nFolder()
        };
        I18nManager.Instance.Register(langPlugin, new CultureInfo("zh-CN"), out _);
        I18nManager.Instance.CultureChanged += (_, _) => ApplyControlCulture();
        ApplyControlCulture();

        ThemeController = new CodeWF.Avalonia.Controls.Themes.StudioThemeController(this)
        {
            Palette = CodeWF.Avalonia.Controls.Themes.StudioPalette.Aurora,
            Mode = CodeWF.Avalonia.Controls.Themes.StudioThemeMode.Light
        };
    }

    public static CodeWF.Avalonia.Controls.Themes.StudioThemeController? ThemeController { get; private set; }

    private void ApplyControlCulture()
    {
        var culture = I18nManager.Instance.Culture?.Name;
        global::Avalonia.Controls.ResourceDictionary semi = culture switch
        {
            "en-US" => new Semi.Avalonia.Locale.en_us(),
            "ja-JP" => new Semi.Avalonia.Locale.ja_jp(),
            _ => new Semi.Avalonia.Locale.zh_cn()
        };
        global::Avalonia.Controls.ResourceDictionary ursa = culture is "en-US" or "ja-JP"
            ? new Ursa.Themes.Semi.Locale.en_us()
            : new Ursa.Themes.Semi.Locale.zh_cn();
        foreach (var resource in semi) Resources[resource.Key] = resource.Value;
        foreach (var resource in ursa) Resources[resource.Key] = resource.Value;
    }

    private static void LoadThemePreferences()
    {
        try
        {
            var path = ApplicationSettings.GetUserDataPath("appearance.txt");
            if (!File.Exists(path) || ThemeController is null) return;
            var values = File.ReadAllLines(path);
            if (values.Length > 0 && Enum.TryParse<CodeWF.Avalonia.Controls.Themes.StudioPalette>(values[0], out var palette)) ThemeController.Palette = palette;
            if (values.Length > 1 && Enum.TryParse<CodeWF.Avalonia.Controls.Themes.StudioThemeMode>(values[1], out var mode)) ThemeController.Mode = mode;
        }
        catch (IOException) { }
    }
    public static void SaveThemePreferences()
    {
        if (ThemeController is null) return;
        try { File.WriteAllLines(ApplicationSettings.GetUserDataPath("appearance.txt"), new[] { ThemeController.Palette.ToString(), ThemeController.Mode.ToString() }); }
        catch (IOException) { }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ApplicationSettings.InitializeAsync().GetAwaiter().GetResult();
        DefaultFileOpeningService.Configure();
        LoadThemePreferences();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            var startupFilePath = desktop.Args?.FirstOrDefault(static arg => !string.IsNullOrWhiteSpace(arg));
            mainWindow.DataContext = new MainWindowViewModel(
                new AvaloniaMindMapFileService(mainWindow),
                new AvaloniaApplicationActionService(mainWindow),
                startupFilePath);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 语言包目录：常规部署在 BaseDirectory/I18n；macOS .app 打包会把
    /// 可执行文件以外的内容挪进 Contents/Resources，这里按存在性探测。
    /// </summary>
    private static string ResolveI18nFolder()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "I18n"),
            Path.Combine(baseDirectory, "..", "Resources", "I18n"),
        };
        return candidates.FirstOrDefault(Directory.Exists)
            ?? Path.Combine(baseDirectory, "I18n");

}
}

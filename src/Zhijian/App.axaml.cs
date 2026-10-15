using Avalonia;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Markup.Xaml;
using CodeWF.Toolkit.Lang;
using CodeWF.Toolkit.Lang.Json;
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
        if (!global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(ApplyControlCulture);
            return;
        }
        if (I18nManager.Instance.Culture is not { } culture) return;
        var semiCulture = culture.Name == "zh-Hant" ? CultureInfo.GetCultureInfo("zh-TW") : culture;
        // 当前 Ursa 2.2.0 未提供繁体和日语资源，明确回退到简体和英语。
        var ursaCulture = culture.Name switch
        {
            "zh-Hant" => CultureInfo.GetCultureInfo("zh-CN"),
            "ja-JP" => CultureInfo.GetCultureInfo("en-US"),
            _ => culture
        };
        foreach (var theme in Styles.OfType<Semi.Avalonia.SemiTheme>()) theme.Locale = semiCulture;
        foreach (var theme in Styles.OfType<Ursa.Themes.Semi.UrsaSemiTheme>()) theme.Locale = ursaCulture;
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

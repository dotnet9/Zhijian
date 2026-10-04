using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AtomUI;
using AtomUI.Desktop.Controls;
using AtomUI.Theme;
using AtomUI.Localization;
using Lang.Avalonia;
using Lang.Avalonia.Json;
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
        NativeAotCompatibility.PreserveAtomUiLanguageResourceArrays();

        var langPlugin = new JsonLangPlugin
        {
            ResourceFolder = ResolveI18nFolder()
        };
        I18nManager.Instance.Register(langPlugin, new CultureInfo("zh-CN"), out _);

        this.UseAtomUI(builder =>
        {
            // AtomUI 6.2 起语言系统迁移到 AtomUI.Localization（LanguageTag，BCP-47）。
            // AtomUI 仅内置 zh-CN / zh-TW / en-US 的完整翻译，声明过多会在启动时校验失败。
            builder.UseLanguages(
                LanguageTag.Parse("zh-CN"),
                new[]
                {
                    LanguageTag.Parse("zh-CN"),
                    LanguageTag.Parse("zh-TW"),
                    LanguageTag.Parse("en-US")
                });
            builder.WithInitialTheme(IThemeManager.DEFAULT_THEME_ID);
            builder.UseAlibabaSansFont();
            builder.UseDesktopControls();
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ApplicationSettings.InitializeAsync().GetAwaiter().GetResult();
        DefaultFileOpeningService.Configure();

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
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
            ResourceFolder = Path.Combine(AppContext.BaseDirectory, "I18n")
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
}

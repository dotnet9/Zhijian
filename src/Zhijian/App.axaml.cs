using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AtomUI;
using AtomUI.Desktop.Controls;
using AtomUI.Theme;
using AtomUI.Theme.Language;
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
            builder.WithDefaultLanguageVariant(LanguageVariant.zh_CN);
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

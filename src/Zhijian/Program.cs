using Avalonia;
using global::Avalonia.Media;
using global::Avalonia.Controls;
using Zhijian.Services;

namespace Zhijian;

sealed class Program
{
    // 框架初始化完成前，只初始化配置和日志。
    [STAThread]
    public static void Main(string[] args)
    {
        if (args is ["--verify-runtime", var directory])
        {
            Diagnostics.RuntimeVerification.Run(directory);
            return;
        }
        ApplicationSettings.InitializeAsync().GetAwaiter().GetResult();
        ApplicationLogger.Configure(ApplicationSettings.UserDataDirectory);

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose);
        }
        catch (Exception exception)
        {
            ApplicationLogger.Error("Application terminated unexpectedly.", exception);
            throw;
        }
        finally
        {
            ApplicationLogger.Shutdown();
        }
    }

    // 供应用入口和设计器共用。
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new FontManagerOptions
            {
                FontFallbacks =
                [
                    new FontFallback { FontFamily = new FontFamily("Microsoft YaHei UI") },
                    new FontFallback { FontFamily = new FontFamily("PingFang SC") },
                    new FontFallback { FontFamily = new FontFamily("Noto Sans CJK SC") }
                ]
            })
            .LogToTrace();
}

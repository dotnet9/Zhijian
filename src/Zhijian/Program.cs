using Avalonia;
using Avalonia.Controls;
using Zhijian.Services;

namespace Zhijian;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
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

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

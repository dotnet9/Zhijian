using CodeWF.Log.Core;
using System.Diagnostics;

namespace Zhijian.Services;

internal static class ApplicationLogger
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(2);
    private static int _configured;

    public static void Configure()
    {
        if (Interlocked.Exchange(ref _configured, 1) == 1)
        {
            return;
        }

        Logger.Level = LogType.Warn;
        Logger.EnableConsoleOutput = false;
        Logger.MaxLogFileSizeMB = 20;
        Logger.TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";
        ConfigureLogDirectory(GetDefaultLogDirectory());
        Logger.RecordToFile();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                Error("Unhandled application exception.", exception);
            }
            else
            {
                Error($"Unhandled non-exception application failure: {args.ExceptionObject}");
            }

            Flush();
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Error("Unobserved task exception.", args.Exception);
        };
    }

    public static void ConfigureLogDirectory(string? directory)
    {
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Logger.LogDir = directory;
        }
    }

    public static void Warning(string message)
    {
        Logger.Warn(message, log2UI: false, log2File: true, log2Console: false);
    }

    public static void Warning(string message, Exception exception)
    {
        Warning($"{message}{Environment.NewLine}{exception}");
    }

    public static void Error(string message, Exception? exception = null)
    {
        Logger.Error(message, exception, log2UI: false, log2File: true, log2Console: false);
    }

    public static void Flush()
    {
        try
        {
            var flushTask = Logger.FlushAsync();
            if (flushTask.Wait(FlushTimeout))
            {
                flushTask.GetAwaiter().GetResult();
                return;
            }

            Debug.WriteLine("Application log flush timed out.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Application log flush failed: {exception}");
        }
    }

    private static string GetDefaultLogDirectory()
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return string.IsNullOrWhiteSpace(baseDirectory)
            ? AppContext.BaseDirectory
            : Path.Combine(baseDirectory, "Zhijian");
    }
}

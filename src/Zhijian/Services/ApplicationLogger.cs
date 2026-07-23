using CodeWF.Log.Core;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Zhijian.Services;

internal static class ApplicationLogger
{
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(2);
    private static readonly object ConfigureLock = new();
    private static readonly ConcurrentQueue<(string Message, Exception? Exception)> PendingWarnings = new();
    private static int _configured;

    public static void Configure(string? logDirectory = null)
    {
        if (Volatile.Read(ref _configured) == 1)
        {
            return;
        }

        lock (ConfigureLock)
        {
            if (_configured == 1)
            {
                return;
            }

            Logger.Initialize(new LoggerOptions
            {
                MinimumLevel = LogType.Warn,
                EnableConsole = false,
                File = new FileLogOptions
                {
                    DirectoryPath = string.IsNullOrWhiteSpace(logDirectory)
                        ? GetDefaultLogDirectory()
                        : logDirectory,
                    MaxFileSizeBytes = 20L * 1024 * 1024,
                    TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff"
                }
            });
            Volatile.Write(ref _configured, 1);
        }

        while (PendingWarnings.TryDequeue(out var warning))
        {
            Logger.WarnToFile(warning.Message, warning.Exception);
        }

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

    public static void Warning(string message)
    {
        Warning(message, null);
    }

    public static void Warning(string message, Exception? exception)
    {
        if (Volatile.Read(ref _configured) == 0)
        {
            PendingWarnings.Enqueue((message, exception));
            return;
        }

        Logger.WarnToFile(message, exception);
    }

    public static void Error(string message, Exception? exception = null)
    {
        Logger.ErrorToFile(message, exception);
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

    public static void Shutdown()
    {
        try
        {
            var shutdownTask = Logger.ShutdownAsync();
            if (shutdownTask.Wait(FlushTimeout))
            {
                shutdownTask.GetAwaiter().GetResult();
                return;
            }

            Debug.WriteLine("Application log shutdown timed out.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Application log shutdown failed: {exception}");
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

using System.Xml.Linq;

namespace Zhijian.Services;

public static class ApplicationSettings
{
    private const string ShowNewUserTourKey = "ShowNewUserTour";
    private const string DefaultCultureNameKey = "DefaultCultureName";
    private const string RecentFilesFileNameKey = "RecentFilesFileName";
    private const string TourSeenFileNameKey = "TourSeenFileName";
    private const string UserDataDirectoryKey = "UserDataDirectory";
    private const string MaxRecentFilesKey = "MaxRecentFiles";
    private const string MaxHistoryStepsKey = "MaxHistorySteps";
    private static readonly Dictionary<string, string> Settings = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim LoadLock = new(1, 1);
    private static bool _isLoaded;

    public static bool ShowNewUserTour => GetBoolean(ShowNewUserTourKey, defaultValue: true);

    public static string DefaultCultureName => GetString(DefaultCultureNameKey, "zh-CN");

    public static string RecentFilesFileName => GetString(RecentFilesFileNameKey, "recent-files.json");

    public static string TourSeenFileName => GetString(TourSeenFileNameKey, "new-user-tour.seen");

    public static string UserDataDirectory => GetString(UserDataDirectoryKey, GetDefaultUserDataDirectory());

    public static int MaxRecentFiles => GetInt32(MaxRecentFilesKey, defaultValue: 12, minValue: 1);

    public static int MaxHistorySteps => GetInt32(MaxHistoryStepsKey, defaultValue: 80, minValue: 1);

    public static string GetUserDataPath(string fileName)
    {
        return Path.Combine(UserDataDirectory, fileName);
    }

    private static string GetString(string key, string defaultValue)
    {
        var value = GetAppSetting(key);
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    private static bool GetBoolean(string key, bool defaultValue)
    {
        var value = GetAppSetting(key);
        return bool.TryParse(value, out var parsedValue) ? parsedValue : defaultValue;
    }

    private static int GetInt32(string key, int defaultValue, int minValue)
    {
        var value = GetAppSetting(key);
        return int.TryParse(value, out var parsedValue) && parsedValue >= minValue
            ? parsedValue
            : defaultValue;
    }

    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded)
        {
            return;
        }

        await LoadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isLoaded)
            {
                return;
            }

            foreach (var configPath in GetCompiledConfigPaths().Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!await FileExistsAsync(configPath, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                try
                {
                    await using var stream = new FileStream(
                        configPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite,
                        bufferSize: 4096,
                        FileOptions.Asynchronous | FileOptions.SequentialScan);
                    var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
                    foreach (var element in document.Root?
                                 .Element("appSettings")?
                                 .Elements("add")
                             ?? [])
                    {
                        var key = (string?)element.Attribute("key");
                        var value = (string?)element.Attribute("value");
                        if (!string.IsNullOrWhiteSpace(key) && value is not null)
                        {
                            Settings[key] = value;
                        }
                    }
                }
                catch (Exception exception)
                {
                    // 配置文件损坏不应影响应用启动，读取失败时统一回退到代码中的默认值。
                    ApplicationLogger.Warning($"Loading application config failed. file=\"{configPath}\"", exception);
                }
            }

            _isLoaded = true;
        }
        finally
        {
            LoadLock.Release();
        }
    }

    private static string? GetAppSetting(string key)
    {
        return Settings.GetValueOrDefault(key);
    }

    private static async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(() => File.Exists(filePath), cancellationToken).ConfigureAwait(false);
    }

    private static IEnumerable<string> GetCompiledConfigPaths()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            yield return $"{Environment.ProcessPath}.config";
        }

        var appName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? "Zhijian";
        if (!string.IsNullOrWhiteSpace(appName))
        {
            // 单文件/AOT 下 Assembly.Location 为空，配置文件路径统一按应用目录推导。
            yield return Path.Combine(AppContext.BaseDirectory, $"{appName}.dll.config");
            yield return Path.Combine(AppContext.BaseDirectory, $"{appName}.exe.config");
        }
    }

    private static string GetDefaultUserDataDirectory()
    {
        var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = AppContext.BaseDirectory;
        }

        return Path.Combine(baseDirectory, "Zhijian");
    }
}

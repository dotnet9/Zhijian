using System.Reflection;
using System.Xml.Linq;

namespace Zhijian.Services;

public static class ApplicationSettings
{
    private const string ShowNewUserTourKey = "ShowNewUserTour";
    private const string DefaultCultureNameKey = "DefaultCultureName";
    private const string RecentFilesFileNameKey = "RecentFilesFileName";
    private const string TourSeenFileNameKey = "TourSeenFileName";
    private const string MaxRecentFilesKey = "MaxRecentFiles";
    private const string MaxHistoryStepsKey = "MaxHistorySteps";

    public static bool ShowNewUserTour => GetBoolean(ShowNewUserTourKey, defaultValue: true);

    public static string DefaultCultureName => GetString(DefaultCultureNameKey, "zh-CN");

    public static string RecentFilesFileName => GetString(RecentFilesFileNameKey, "recent-files.json");

    public static string TourSeenFileName => GetString(TourSeenFileNameKey, "new-user-tour.seen");

    public static int MaxRecentFiles => GetInt32(MaxRecentFilesKey, defaultValue: 12, minValue: 1);

    public static int MaxHistorySteps => GetInt32(MaxHistoryStepsKey, defaultValue: 80, minValue: 1);

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

    private static string? GetAppSetting(string key)
    {
        foreach (var configPath in GetCompiledConfigPaths().Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!File.Exists(configPath))
            {
                continue;
            }

            try
            {
                var document = XDocument.Load(configPath);
                var value = document.Root?
                    .Element("appSettings")?
                    .Elements("add")
                    .FirstOrDefault(element => string.Equals(
                        (string?)element.Attribute("key"),
                        key,
                        StringComparison.OrdinalIgnoreCase))
                    ?.Attribute("value")
                    ?.Value;
                if (value is not null)
                {
                    return value;
                }
            }
            catch
            {
                // 配置文件损坏不应影响应用启动，读取失败时统一回退到代码中的默认值。
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCompiledConfigPaths()
    {
        var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(entryAssemblyPath))
        {
            // .NET SDK 会把 App.config 编译为 <入口程序集>.dll.config，
            // 运行时读取的是输出目录中的编译产物，而不是源文件 App.config。
            yield return $"{entryAssemblyPath}.config";
        }

        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            yield return $"{Environment.ProcessPath}.config";
        }
    }
}

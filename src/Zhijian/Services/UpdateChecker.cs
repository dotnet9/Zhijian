using System.Net.Http;
using System.Text.Json;

namespace Zhijian.Services;

/// <summary>
/// 更新检查结果，区分无更新和网络/API 失败。
/// </summary>
public sealed record UpdateCheckResult(UpdateInfo? Update, bool Succeeded, string? Error)
{
    public static UpdateCheckResult Latest() => new(null, true, null);

    public static UpdateCheckResult Failed(string error) => new(null, false, error);
}

/// <summary>开机自检更新的结果。</summary>
public sealed record UpdateInfo(
    Version Version,
    string Tag,
    string Title,
    string? Notes,
    string PageUrl,
    string? AssetUrl,
    string? AssetName,
    string? ChecksumUrl = null);

public sealed record UpdateDownloadProgress(long BytesReceived, long? TotalBytes)
{
    public double? Percentage => TotalBytes is > 0
        ? BytesReceived * 100d / TotalBytes.Value
        : null;
}

public sealed record UpdateDownloadResult(string FilePath, string FileName, long BytesReceived);

/// <summary>检查 GitHub Releases 更新。</summary>
public interface IUpdateChecker
{
    Task<UpdateCheckResult> CheckAsync(Version current, CancellationToken cancellationToken = default);
}

/// <summary>下载更新资产并报告进度。</summary>
public interface IUpdateDownloader
{
    Task<UpdateDownloadResult> DownloadAsync(
        UpdateInfo update,
        IProgress<UpdateDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 通过 GitHub Releases API 检查更新：够用、无额外依赖、AOT 友好（源生成 JSON）。
/// 任何网络/解析异常都吞掉返回失败结果，不打扰用户。
/// </summary>
public sealed class UpdateChecker : IUpdateChecker
{
    private const string DefaultApiBase = "https://api.github.com";

    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _apiBase;
    private readonly Action<string, Exception?>? _log;

    public UpdateChecker(HttpClient http, string owner, string repo, string? apiBase = null, Action<string, Exception?>? log = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _owner = owner;
        _repo = repo;
        _apiBase = (apiBase ?? DefaultApiBase).TrimEnd('/');
        _log = log;
    }

    public async Task<UpdateCheckResult> CheckAsync(Version current, CancellationToken cancellationToken = default)
    {
        try
        {
            string url = $"{_apiBase}/repos/{_owner}/{_repo}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            request.Headers.TryAddWithoutValidation("User-Agent", "Zhijian-UpdateChecker");

            using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _log?.Invoke($"检查更新失败：HTTP {(int)response.StatusCode}", null);
                return UpdateCheckResult.Failed($"HTTP {(int)response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            GitHubRelease? release = JsonSerializer.Deserialize(json, UpdateJsonContext.Default.GitHubRelease);
            if (release is null || release.Draft || release.Prerelease)
            {
                return UpdateCheckResult.Latest();
            }

            Version? candidate = VersionUtil.Parse(release.TagName);
            if (!VersionUtil.IsNewer(candidate, current))
            {
                return UpdateCheckResult.Latest();
            }

            (string? assetUrl, string? assetName, string? checksumUrl) = PickAsset(release);
            string title = string.IsNullOrWhiteSpace(release.Name) ? release.TagName ?? string.Empty : release.Name!;

            return new UpdateCheckResult(new UpdateInfo(
                candidate!,
                release.TagName ?? string.Empty,
                title,
                release.Body,
                release.HtmlUrl ?? $"https://github.com/{_owner}/{_repo}/releases",
                assetUrl,
                assetName,
                checksumUrl), true, null);
        }
        catch (OperationCanceledException)
        {
            return UpdateCheckResult.Failed("操作已取消");
        }
        catch (Exception ex)
        {
            _log?.Invoke($"检查更新异常：{ex.Message}", ex);
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// 选择当前系统/架构的安装包资产：win 用 setup.exe，linux 用 deb，
    /// osx 优先 dmg；没有匹配包时交给用户打开 Release 页面。
    /// </summary>
    private static (string? Url, string? Name, string? ChecksumUrl) PickAsset(GitHubRelease release)
    {
        if (release.Assets is null || release.Assets.Length == 0)
        {
            return (null, null, null);
        }

        string? runtimeIdentifier = CurrentRuntimeIdentifier();
        if (string.IsNullOrWhiteSpace(runtimeIdentifier))
        {
            return (null, null, null);
        }

        GitHubAsset? preferred = null;
        if (runtimeIdentifier.StartsWith("win-", StringComparison.OrdinalIgnoreCase))
        {
            preferred = release.Assets.FirstOrDefault(asset =>
                IsAssetForRuntime(asset, runtimeIdentifier) &&
                asset.Name!.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            string nativeExtension = runtimeIdentifier.StartsWith("linux-", StringComparison.Ordinal)
                ? ".deb"
                : ".dmg";

            preferred = release.Assets.FirstOrDefault(asset =>
                IsAssetForRuntime(asset, runtimeIdentifier) &&
                asset.Name!.EndsWith(nativeExtension, StringComparison.OrdinalIgnoreCase));
        }

        if (preferred is null)
        {
            return (null, null, null);
        }

        GitHubAsset? checksum = release.Assets.FirstOrDefault(asset =>
            string.Equals(asset.Name, preferred.Name + ".sha256", StringComparison.OrdinalIgnoreCase));
        return (preferred.BrowserDownloadUrl, preferred.Name, checksum?.BrowserDownloadUrl);
    }

    private static bool IsAssetForRuntime(GitHubAsset asset, string runtimeIdentifier)
    {
        string marker = "-" + runtimeIdentifier;
        return !string.IsNullOrWhiteSpace(asset.Name) &&
            !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl) &&
            (asset.Name.Contains(marker + ".", StringComparison.OrdinalIgnoreCase) ||
             asset.Name.Contains(marker + "-", StringComparison.OrdinalIgnoreCase));
    }

    private static string? CurrentRuntimeIdentifier()
    {
        string os = OperatingSystem.IsWindows()
            ? "win"
            : OperatingSystem.IsLinux()
                ? "linux"
                : OperatingSystem.IsMacOS()
                    ? "osx"
                    : string.Empty;

        string architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.X64 => "x64",
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            _ => string.Empty
        };

        return os.Length == 0 || architecture.Length == 0 ? null : os + "-" + architecture;
    }
}

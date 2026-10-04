using System.Globalization;
using System.Net;
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
/// 检查 GitHub Releases 更新，默认路径不碰 api.github.com 的每小时配额（未认证 60 次/小时/IP，
/// 走代理时被同出口用户共享、极易耗尽）：releases/latest 的 302 落点就是最新发布 tag（只读响应头），
/// 有新版本时再经 expanded_assets/{tag}（发布页懒加载资产列表的接口）解析安装包直链。
/// 网页端点拿不到（改版/超时/网络）才退回 Releases API，收到 403/429 按指示退避，
/// 恢复前不再发请求。任何网络/解析异常都吞掉返回失败结果，不打扰用户。
/// </summary>
public sealed class UpdateChecker : IUpdateChecker
{
    private const string DefaultApiBase = "https://api.github.com";
    private const string DefaultWebBase = "https://github.com";

    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _apiBase;
    private readonly string _webBase;
    private readonly Action<string, Exception?>? _log;
    private DateTime _blockedUntilUtc = DateTime.MinValue;

    public UpdateChecker(HttpClient http, string owner, string repo, string? apiBase = null, Action<string, Exception?>? log = null, string? webBase = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _owner = owner;
        _repo = repo;
        _apiBase = (apiBase ?? DefaultApiBase).TrimEnd('/');
        _webBase = (webBase ?? DefaultWebBase).TrimEnd('/');
        _log = log;
    }

    public async Task<UpdateCheckResult> CheckAsync(Version current, CancellationToken cancellationToken = default)
    {
        try
        {
            GitHubRelease? release = await FindLatestReleaseViaWebAsync(cancellationToken).ConfigureAwait(false)
                ?? await FindLatestReleaseViaApiAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>网页端点取最新 release；拿不到返回 null，由调用方退回 API。</summary>
    private async Task<GitHubRelease?> FindLatestReleaseViaWebAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? tag = await FetchLatestTagViaRedirectAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }

            return await FetchWebReleaseAsync(tag, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient 超时也抛 TaskCanceledException：网页路径超时不算失败，退回 API
            _log?.Invoke("网页检查超时，退回 API", null);
            return null;
        }
        catch (Exception ex)
        {
            _log?.Invoke($"网页检查失败，退回 API：{ex.Message}", ex);
            return null;
        }
    }

    /// <summary>releases/latest 的 302 落点就是 /releases/tag/{tag}；只取响应头，不下载页面。</summary>
    private async Task<string?> FetchLatestTagViaRedirectAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_webBase}/{_owner}/{_repo}/releases/latest");
        request.Headers.TryAddWithoutValidation("User-Agent", "Zhijian-UpdateChecker");
        using HttpResponseMessage response = await _http
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        // HttpClient 自动跟随重定向时最终地址写在 RequestMessage 上；跟随被禁用时读 Location 头
        Uri? finalUri = IsRedirect(response.StatusCode)
            ? response.Headers.Location
            : response.RequestMessage?.RequestUri;
        string? tag = ExtractTag(finalUri);
        if (tag is null)
        {
            _log?.Invoke($"网页检查未解析到最新 tag（HTTP {(int)response.StatusCode}）", null);
        }

        return tag;
    }

    /// <summary>expanded_assets 是发布页懒加载资产列表的接口；从中拼出 release 模型（发布说明等字段拿不到）。</summary>
    private async Task<GitHubRelease?> FetchWebReleaseAsync(string tag, CancellationToken cancellationToken)
    {
        string url = $"{_webBase}/{_owner}/{_repo}/releases/expanded_assets/{tag}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "Zhijian-UpdateChecker");
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _log?.Invoke($"资产列表拉取失败：HTTP {(int)response.StatusCode}", null);
            return null;
        }

        string html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        List<GitHubAsset> assets = ParseDownloadAssets(html, tag);
        return new GitHubRelease
        {
            TagName = tag,
            HtmlUrl = $"{_webBase}/{_owner}/{_repo}/releases/tag/{tag}",
            Assets = assets.ToArray()
        };
    }

    /// <summary>回退：Releases API。403/429 按指示退避，恢复前不再发请求。</summary>
    private async Task<GitHubRelease?> FindLatestReleaseViaApiAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow < _blockedUntilUtc)
        {
            _log?.Invoke($"API 限流中，约 {_blockedUntilUtc.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture)} 后恢复", null);
            return null;
        }

        string url = $"{_apiBase}/repos/{_owner}/{_repo}/releases/latest";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
        request.Headers.TryAddWithoutValidation("User-Agent", "Zhijian-UpdateChecker");
        using HttpResponseMessage response = await _http.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
        {
            _blockedUntilUtc = ParseBlockedUntil(response);
            _log?.Invoke($"检查更新被限流（HTTP {(int)response.StatusCode}），恢复前不再请求 API", null);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _log?.Invoke($"检查更新失败：HTTP {(int)response.StatusCode}", null);
            throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
        }

        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize(json, UpdateJsonContext.Default.GitHubRelease);
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Found
            or HttpStatusCode.SeeOther
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    /// <summary>从 …/releases/tag/{tag} 的落点地址抠出 tag；跟随与未跟随两种响应都要顾及。</summary>
    private static string? ExtractTag(Uri? uri)
    {
        if (uri is null)
        {
            return null;
        }

        const string marker = "/releases/tag/";
        string path = uri.IsAbsoluteUri ? uri.AbsolutePath : uri.OriginalString;
        int start = path.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        string tag = Uri.UnescapeDataString(path[(start + marker.Length)..]).TrimEnd('/');
        return tag.Length > 0 ? tag : null;
    }

    /// <summary>从 expanded_assets 的 HTML 里抠出 /releases/download/{tag}/{文件名} 资产；
    /// 简单字符串扫描而不引 HTML 解析器，保持 AOT 友好。</summary>
    private List<GitHubAsset> ParseDownloadAssets(string html, string tag)
    {
        var assets = new List<GitHubAsset>();
        const string marker = "/releases/download/";
        int index = 0;
        while ((index = html.IndexOf("href=\"", index, StringComparison.Ordinal)) >= 0)
        {
            int start = index + "href=\"".Length;
            int end = html.IndexOf('"', start);
            if (end < 0)
            {
                break;
            }

            index = end + 1;
            string href = html[start..end];
            int pathStart = href.IndexOf(marker, StringComparison.Ordinal);
            if (pathStart < 0)
            {
                continue;
            }

            string remainder = href[(pathStart + marker.Length)..]; // {tag}/{文件名}
            int separator = remainder.IndexOf('/');
            if (separator <= 0)
            {
                continue;
            }

            string hrefTag = Uri.UnescapeDataString(remainder[..separator]);
            string name = Uri.UnescapeDataString(remainder[(separator + 1)..]);
            if (name.Length == 0 || !string.Equals(hrefTag, tag, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 同一资产在页面里可能以根相对与绝对两种形式各出现一次
            if (assets.Any(a => string.Equals(a.Name, name, StringComparison.Ordinal)))
            {
                continue;
            }

            bool absolute = href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
            assets.Add(new GitHubAsset
            {
                Name = name,
                BrowserDownloadUrl = absolute ? href : _webBase + href
            });
        }

        return assets;
    }

    /// <summary>从 Retry-After 或 X-RateLimit-Reset 推算限流恢复时间，兜底 10 分钟，最长封顶 1 小时。</summary>
    private static DateTime ParseBlockedUntil(HttpResponseMessage response)
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan? retryAfter = response.Headers.RetryAfter?.Delta;
        if (retryAfter is null && response.Headers.RetryAfter?.Date is { } retryDate)
        {
            retryAfter = retryDate - DateTimeOffset.UtcNow;
        }

        DateTime blocked = retryAfter is { } delay && delay > TimeSpan.Zero
            ? now + delay
            : response.Headers.TryGetValues("X-RateLimit-Reset", out var values)
              && long.TryParse(values.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long resetSeconds)
                ? DateTimeOffset.FromUnixTimeSeconds(resetSeconds).UtcDateTime
                : now + TimeSpan.FromMinutes(10);

        blocked += TimeSpan.FromSeconds(30);
        return blocked > now + TimeSpan.FromHours(1) ? now + TimeSpan.FromHours(1) : blocked;
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

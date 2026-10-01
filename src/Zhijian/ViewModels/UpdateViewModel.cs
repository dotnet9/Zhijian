using System.Collections.Generic;
using System.Diagnostics;
using Zhijian.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using ReactiveUI;

namespace Zhijian.ViewModels;

/// <summary>
/// 更新状态机：检查 GitHub 最新版 → 下载（带进度）→ 运行安装器。
/// 任何失败都转成可直接展示的中文提示，不抛出。
/// </summary>
public sealed class UpdateViewModel : ViewModelBase
{
    private readonly IUpdateChecker _checker;
    private readonly IUpdateDownloader _downloader;
    private readonly HttpClient _http;
    private readonly Action<string, string, Exception?> _log;
    private CancellationTokenSource? _downloadCancellation;
    private double _downloadProgress;
    private bool _isChecking;
    private bool _isDownloading;
    private string? _downloadedPath;
    private UpdateInfo? _pendingUpdate;
    private string _resultText = string.Empty;

    public UpdateViewModel(Action<string, string, Exception?> log)
    {
        _log = log;
        _http = new HttpClient();
        _checker = new UpdateChecker(_http, "dotnet9", "ClearC", log: (message, exception) => _log("WARN", message, exception));
        _downloader = new UpdateDownloader(_http);

        CheckCommand = ReactiveCommand.CreateFromTask(() => CheckAsync());
        DownloadCommand = ReactiveCommand.CreateFromTask(() => DownloadAsync());
        InstallCommand = ReactiveCommand.Create(InstallDownloaded);
        OpenPageCommand = ReactiveCommand.Create(OpenReleasePage);
    }

    public ICommand CheckCommand { get; }
    public ICommand DownloadCommand { get; }
    public ICommand InstallCommand { get; }
    public ICommand OpenPageCommand { get; }

    public bool IsChecking
    {
        get => _isChecking;
        private set => SetProperty(ref _isChecking, value);
    }

    public bool IsDownloading
    {
        get => _isDownloading;
        private set
        {
            if (SetProperty(ref _isDownloading, value))
            {
                OnPropertyChanged(nameof(CanDownload));
            }
        }
    }

    public bool IsReady => !string.IsNullOrWhiteSpace(_downloadedPath) && File.Exists(_downloadedPath);

    public bool UpdateAvailable => _pendingUpdate is not null;

    /// <summary>状态栏常驻按钮的文案：平时是「检查更新」，发现新版后变成版本徽标。</summary>
    public string ButtonText => UpdateAvailable ? $"新版本 {_pendingUpdate!.Tag}" : "检查更新";

    public string ResultText => _resultText;

    public double DownloadProgress => _downloadProgress;

    public string ProgressText => IsDownloading
        ? _downloadProgress > 0 ? $"下载中 {_downloadProgress:0}%" : "正在准备下载…"
        : IsReady ? "下载完成" : string.Empty;

    public bool CanDownload => UpdateAvailable && !IsDownloading && !IsReady &&
        !string.IsNullOrWhiteSpace(_pendingUpdate?.AssetUrl) &&
        !string.IsNullOrWhiteSpace(_pendingUpdate?.ChecksumUrl);

    /// <summary>没有匹配资产或缺少校验文件时，只能引导用户去 Release 页面。</summary>
    public bool NeedsReleasePage => UpdateAvailable && !IsDownloading && !IsReady && !CanDownload;

    public async Task CheckAsync()
    {
        if (IsChecking || IsDownloading)
        {
            return;
        }

        IsChecking = true;
        try
        {
            var current = ParseCurrentVersion();
            UpdateCheckResult result = await _checker.CheckAsync(current).ConfigureAwait(true);

            if (!result.Succeeded)
            {
                SetResult("检查更新失败，请稍后重试");
                return;
            }

            if (result.Update is null)
            {
                _pendingUpdate = null;
                SetResult($"已是最新版本 v{current}");
                return;
            }

            _pendingUpdate = result.Update;
            SetResult(NeedsReleasePage
                ? "发现新版本，但没有匹配的安装包，请到发布页下载"
                : $"发现新版本 {result.Update.Tag}，可下载安装");
        }
        finally
        {
            IsChecking = false;
        }
    }

    public async Task DownloadAsync()
    {
        if (IsDownloading || _pendingUpdate?.AssetUrl is null)
        {
            if (NeedsReleasePage)
            {
                OpenReleasePage();
            }
            return;
        }

        _downloadCancellation?.Dispose();
        _downloadCancellation = new CancellationTokenSource();
        _downloadProgress = 0;
        IsDownloading = true;

        try
        {
            var progress = new Progress<UpdateDownloadProgress>(value =>
            {
                _downloadProgress = value.Percentage ?? _downloadProgress;
                this.RaisePropertyChanged(nameof(DownloadProgress));
                this.RaisePropertyChanged(nameof(ProgressText));
            });

            UpdateDownloadResult download = await _downloader.DownloadAsync(
                _pendingUpdate, progress, _downloadCancellation.Token).ConfigureAwait(true);
            _downloadedPath = download.FilePath;
            SetResult($"已下载 {download.FileName}，可安装");
        }
        catch (OperationCanceledException)
        {
            SetResult("已取消下载");
        }
        catch (Exception exception)
        {
            SetResult($"下载失败：{exception.Message}");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    /// <summary>运行下载好的安装器（Inno Setup），主程序退出让安装器接管。</summary>
    public void InstallDownloaded()
    {
        string? path = _downloadedPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            SetResult("安装包不存在，请重新下载");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
        catch (Exception exception)
        {
            SetResult($"启动安装器失败：{exception.Message}");
        }
    }

    public void OpenReleasePage()
    {
        if (_pendingUpdate?.PageUrl is string url)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }

    private static Version ParseCurrentVersion()
    {
        var informational = typeof(UpdateViewModel).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return VersionUtil.Parse(informational) ?? new Version(0, 1, 0);
    }

    private void SetResult(string text)
    {
        _resultText = text;
        this.RaisePropertyChanged(nameof(ResultText));
        this.RaisePropertyChanged(nameof(UpdateAvailable));
        this.RaisePropertyChanged(nameof(ButtonText));
        this.RaisePropertyChanged(nameof(IsReady));
        this.RaisePropertyChanged(nameof(CanDownload));
        this.RaisePropertyChanged(nameof(NeedsReleasePage));
        this.RaisePropertyChanged(nameof(ProgressText));
        _log("INFO", text, null);
    }
}

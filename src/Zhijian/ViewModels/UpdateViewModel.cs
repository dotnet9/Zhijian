using System.Reactive;
using System.Reactive.Subjects;
using CodeWF.Toolkit.Core.UpdateChecking;
using ReactiveUI;
using CodeWF.Toolkit.Lang;
using System.Globalization;
using Avalonia.Controls.ApplicationLifetimes;
using System.Diagnostics;

namespace Zhijian.ViewModels;

public sealed class UpdateViewModel : ReactiveObject, IDisposable
{
    private readonly IUpdateService _updates;
    private readonly bool _ownsService;
    private readonly I18nManager _localizer;
    private readonly Action<string> _openUrl;
    private readonly Action _exit;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly BehaviorSubject<bool> _canUpdate = new(true);
    private CancellationTokenSource? _operation;
    private UpdateInfo? _update;
    private UpdateDownloadResult? _download;
    private UpdateState _state;
    private string? _error;
    private UpdateInstallResult? _install;
    private double? _progress;
    private bool _disposed;

    /// <summary>应用只保留状态机；网络、选包、校验和安装全部由 Toolkit 提供。</summary>
    public UpdateViewModel(Action<string, string, Exception?> log, IUpdateService? updates = null,
        Action<string>? openUrl = null, Action? exit = null)
    {
        _updates = updates ?? new UpdateService(new UpdateServiceOptions
        {
            Repository = "Zhijian", ProductName = "Zhijian",
            Log = message => log("WARN", message, null)
        });
        _ownsService = updates is null;
        _localizer = I18nManager.Instance;
        _openUrl = openUrl ?? (url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));
        _exit = exit ?? (() => (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown());
        UpdateCommand = ReactiveCommand.CreateFromTask(AdvanceAsync, _canUpdate);
        CheckCommand = ReactiveCommand.CreateFromTask(CheckAsync, _canUpdate);
        CancelCommand = ReactiveCommand.Create(() => _operation?.Cancel());
        RepositoryCommand = ReactiveCommand.Create(() => OpenUrl("https://github.com/dotnet9/Zhijian"));
        WebsiteCommand = ReactiveCommand.Create(() => OpenUrl("https://codewf.com"));
        _localizer.CultureChanged += OnCultureChanged;
    }

    public ReactiveCommand<Unit, Unit> UpdateCommand { get; }
    public ReactiveCommand<Unit, Unit> CheckCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> RepositoryCommand { get; }
    public ReactiveCommand<Unit, Unit> WebsiteCommand { get; }
    public UpdateState State => _state;
    public bool IsBusy => _state is UpdateState.Checking or UpdateState.Downloading or UpdateState.Installing;
    public bool IsDownloading => _state == UpdateState.Downloading;
    public bool CanRecheck => _state is UpdateState.Available or UpdateState.Downloaded;
    public bool IsIndeterminate => !_progress.HasValue;
    public double Progress => _progress ?? 0;
    public string ProgressText => _progress.HasValue ? (_progress.Value / 100).ToString("P0", (_localizer.Culture ?? CultureInfo.CurrentUICulture)) : string.Empty;
    public string ActionText => _localizer.GetResource(_state switch
    {
        UpdateState.Checking => ZhijianL.AboutChecking,
        UpdateState.Downloading => ZhijianL.AboutDownloading,
        UpdateState.Installing => ZhijianL.AboutInstalling,
        UpdateState.Available when !CanDownload => ZhijianL.UpdateReleasePage,
        UpdateState.Available => ZhijianL.UpdateDownload,
        UpdateState.Downloaded when _download?.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true => ZhijianL.AboutOpenFolder,
        UpdateState.Downloaded => ZhijianL.UpdateInstall,
        _ => ZhijianL.UpdateCheck
    });
    public string StatusText => _state switch
    {
        UpdateState.Checking => _localizer.GetResource(ZhijianL.AboutChecking),
        UpdateState.Current => _localizer.GetResource(ZhijianL.AboutCurrent),
        UpdateState.Available => string.Format(_localizer.Culture, _localizer.GetResource(ZhijianL.AboutAvailable), _update!.Tag),
        UpdateState.Downloading => string.Format(_localizer.Culture, _localizer.GetResource(ZhijianL.AboutDownloadProgress), ProgressText),
        UpdateState.Downloaded => _localizer.GetResource(ZhijianL.AboutDownloaded),
        UpdateState.Failed => string.Format(_localizer.Culture, _localizer.GetResource(ZhijianL.AboutFailed), _error),
        UpdateState.Canceled => _localizer.GetResource(ZhijianL.AboutCanceled),
        UpdateState.Installed => _install?.Message ?? string.Empty,
        _ => string.Empty
    };
    public void Cancel() => _operation?.Cancel();

    public string ReleaseNotes => _update?.Notes ?? string.Empty;
    private bool CanDownload => !string.IsNullOrWhiteSpace(_update?.AssetUrl) && !string.IsNullOrWhiteSpace(_update.ChecksumUrl);

    public async Task AdvanceAsync()
    {
        if (IsBusy || _disposed) return;
        if (_state == UpdateState.Available)
        {
            if (CanDownload) await DownloadAsync();
            else OpenUrl(_update!.PageUrl);
        }
        else if (_state == UpdateState.Downloaded) await InstallAsync();
        else await CheckAsync();
    }

    public Task CheckAsync() => RunAsync(UpdateState.Checking, async token =>
    {
        _update = null;
        _download = null;
        _install = null;
        var result = await _updates.CheckAsync(cancellationToken: token);
        token.ThrowIfCancellationRequested();
        if (!result.Succeeded) { _error = result.Error; SetState(UpdateState.Failed); }
        else { _update = result.Update; SetState(_update is null ? UpdateState.Current : UpdateState.Available); }
    });

    private Task DownloadAsync() => RunAsync(UpdateState.Downloading, async token =>
    {
        _progress = null;
        var progress = new Progress<UpdateDownloadProgress>(value =>
        {
            if (_disposed || !IsDownloading) return;
            _progress = value.Percentage;
            Refresh();
        });
        _download = await _updates.DownloadAsync(_update!, progress, token);
        token.ThrowIfCancellationRequested();
        SetState(UpdateState.Downloaded);
    });

    private Task InstallAsync() => RunAsync(UpdateState.Installing, async token =>
    {
        _install = await _updates.InstallAsync(_download!, token);
        SetState(UpdateState.Installed);
        if (_install.ShouldExitApplication) _exit();
    });

    private async Task RunAsync(UpdateState state, Func<CancellationToken, Task> action)
    {
        if (IsBusy || _disposed) return;
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _operation = operation;
        SetState(state);
        try { await action(operation.Token); }
        catch (OperationCanceledException) when (operation.IsCancellationRequested) { SetState(UpdateState.Canceled); }
        catch (Exception error) { _error = error.Message; SetState(UpdateState.Failed); }
        finally { _operation = null; }
    }

    private void OpenUrl(string url)
    {
        try { _openUrl(url); }
        catch (Exception error) { _error = error.Message; SetState(UpdateState.Failed); }
    }

    private void SetState(UpdateState state)
    {
        if (_disposed) return;
        _state = state;
        _canUpdate.OnNext(!IsBusy);
        Refresh();
    }
    private void OnCultureChanged(object? sender, EventArgs args)
    {
        if (_disposed) return;
        if (!global::Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => OnCultureChanged(sender, args));
            return;
        }
        Refresh();
    }
    private void Refresh()
    {
        foreach (var property in new[] { nameof(State), nameof(IsBusy), nameof(IsDownloading), nameof(CanRecheck), nameof(IsIndeterminate),
                     nameof(Progress), nameof(ProgressText), nameof(ActionText), nameof(StatusText), nameof(ReleaseNotes) })
            this.RaisePropertyChanged(property);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _localizer.CultureChanged -= OnCultureChanged;
        _lifetime.Dispose();
        UpdateCommand.Dispose();
        CheckCommand.Dispose();
        CancelCommand.Dispose();
        RepositoryCommand.Dispose();
        WebsiteCommand.Dispose();
        _canUpdate.Dispose();
        if (_ownsService && _updates is IDisposable service) service.Dispose();
    }
}

public enum UpdateState { Idle, Checking, Current, Available, Downloading, Downloaded, Installing, Installed, Failed, Canceled }

using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using AtomUI.Controls;
using AtomUI.Desktop.Controls;
using Zhijian.Views;

namespace Zhijian.Services;

public sealed class AvaloniaApplicationActionService : IApplicationActionService
{
    private const string WebsiteUrl = "https://codewf.com";
    private const string RepositoryUrl = "https://github.com/dotnet9/zhijian";
    private const string FeedbackUrl = "https://github.com/dotnet9/zhijian/issues/new";
    private const string FeatureRequestUrl = "https://github.com/dotnet9/zhijian/issues/new?labels=enhancement";
    private const string PullRequestsUrl = "https://github.com/dotnet9/zhijian/pulls";

    private ChangelogWindow? _changelogWindow;
    private ThanksWindow? _thanksWindow;
    private AboutWindow? _aboutWindow;
    private WindowMessageManager? _messageManager;
    private readonly Avalonia.Controls.Window _owner;

    public AvaloniaApplicationActionService(Avalonia.Controls.Window owner)
    {
        _owner = owner;
        _owner.Opened += (_, _) => EnsureMessageManager();
        _owner.Closed += (_, _) =>
        {
            _messageManager?.Dispose();
            _messageManager = null;
        };
    }

    public void OpenWebsite()
    {
        OpenUrl(WebsiteUrl);
    }

    public void OpenRepository()
    {
        OpenUrl(RepositoryUrl);
    }

    public void OpenFeedback()
    {
        OpenUrl(FeedbackUrl);
    }

    public void OpenFeatureRequest()
    {
        OpenUrl(FeatureRequestUrl);
    }

    public void OpenPullRequests()
    {
        OpenUrl(PullRequestsUrl);
    }

    public void OpenNewWindow()
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(executable)
        {
            UseShellExecute = true,
            WorkingDirectory = AppContext.BaseDirectory
        });
    }

    public void OpenFileLocation(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{filePath}\"")
        {
            UseShellExecute = true
        });
    }

    public async Task SetClipboardTextAsync(string text)
    {
        if (_owner.Clipboard is not null)
        {
            await _owner.Clipboard.SetTextAsync(text);
        }
    }

    public void ShowSuccessMessage(string message)
    {
        EnsureMessageManager();
        if (_owner.Dispatcher.CheckAccess())
        {
            ShowSuccessMessageCore(message);
            return;
        }

        Dispatcher.UIThread.Post(() => ShowSuccessMessageCore(message), DispatcherPriority.Send);
    }

    private void ShowSuccessMessageCore(string message)
    {
        _messageManager?.Show(new Message(
            type: MessageType.Success,
            content: message));
    }

    private void EnsureMessageManager()
    {
        if (_messageManager is not null)
        {
            return;
        }

        _messageManager = new WindowMessageManager(TopLevel.GetTopLevel(_owner) ?? _owner)
        {
            MaxItems = 4
        };
    }

    public void CloseMainWindow()
    {
        _owner.Close();
    }

    public void ShowChangelog()
    {
        if (_changelogWindow is { IsVisible: true })
        {
            _changelogWindow.Activate();
            return;
        }

        _changelogWindow = new ChangelogWindow();
        _changelogWindow.Closed += (_, _) => _changelogWindow = null;
        _changelogWindow.Show(_owner);
    }

    public void ShowAbout()
    {
        if (_aboutWindow is { IsVisible: true })
        {
            _aboutWindow.Activate();
            return;
        }

        _aboutWindow = new AboutWindow();
        _aboutWindow.Closed += (_, _) => _aboutWindow = null;
        _aboutWindow.Show(_owner);
    }

    public void ShowThanks()
    {
        if (_thanksWindow is { IsVisible: true })
        {
            _thanksWindow.Activate();
            return;
        }

        _thanksWindow = new ThanksWindow();
        _thanksWindow.Closed += (_, _) => _thanksWindow = null;
        _thanksWindow.Show(_owner);
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }
}

using System.Diagnostics;
using Avalonia.Controls;
using Zhijian.Views;

namespace Zhijian.Services;

public sealed class AvaloniaApplicationActionService(Window owner) : IApplicationActionService
{
    private const string WebsiteUrl = "https://codewf.com";
    private const string RepositoryUrl = "https://github.com/dotnet9/Zhijian";

    private ChangelogWindow? _changelogWindow;
    private ThanksWindow? _thanksWindow;
    private AboutWindow? _aboutWindow;

    public void OpenWebsite()
    {
        OpenUrl(WebsiteUrl);
    }

    public void OpenRepository()
    {
        OpenUrl(RepositoryUrl);
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
        _changelogWindow.Show(owner);
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
        _aboutWindow.Show(owner);
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
        _thanksWindow.Show(owner);
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }
}

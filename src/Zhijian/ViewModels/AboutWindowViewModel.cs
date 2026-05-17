using System.Reflection;

namespace Zhijian.ViewModels;

public sealed class AboutWindowViewModel : ViewModelBase
{
    public string ProductName => "枝见 Zhijian";

    public string Description => "本地优先、Markdown-first 的 Avalonia / AtomUI 脑图编辑器。";

    public string Version => GetAppVersion();

    public string UpdatedAt => "2026-05-17";

    public string Author => "沙漠尽头的狼";

    public string ContactUrl => "https://codewf.com";

    public string RepositoryUrl => "https://github.com/dotnet9/Zhijian";

    public string MindViewNuGetUrl => "https://www.nuget.org/packages/CodeWF.MindView";

    public string MindViewThemesNuGetUrl => "https://www.nuget.org/packages/CodeWF.MindView.Themes";

    public Uri ContactUri => new(ContactUrl);

    public Uri RepositoryUri => new(RepositoryUrl);

    public Uri MindViewNuGetUri => new(MindViewNuGetUrl);

    public Uri MindViewThemesNuGetUri => new(MindViewThemesNuGetUrl);

    private static string GetAppVersion()
    {
        var assembly = typeof(AboutWindowViewModel).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
               ?? assembly.GetName().Version?.ToString()
               ?? "12.0.3.5";
    }
}

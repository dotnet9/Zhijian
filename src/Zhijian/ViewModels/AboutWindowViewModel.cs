using System.Globalization;
using System.Reflection;
using CodeWF.Tools.Extensions;
using Zhijian.Services;

namespace Zhijian.ViewModels;

public sealed class AboutWindowViewModel : ViewModelBase
{
    private static readonly Assembly AppAssembly = typeof(AboutWindowViewModel).Assembly;

    public string Version => AppAssembly.Version() ?? string.Empty;

    public string CompileTime => GetCompileTime();

    public string Author => "沙漠尽头的狼";

    public string ContactUrl => "https://codewf.com";

    public string RepositoryUrl => "https://github.com/dotnet9/Zhijian";

    public string MindViewNuGetUrl => "https://www.nuget.org/packages/CodeWF.MindView";

    public string MindViewThemesNuGetUrl => "https://www.nuget.org/packages/CodeWF.MindView.Themes";

    public Uri ContactUri => new(ContactUrl);

    public Uri RepositoryUri => new(RepositoryUrl);

    public Uri MindViewNuGetUri => new(MindViewNuGetUrl);

    public Uri MindViewThemesNuGetUri => new(MindViewThemesNuGetUrl);

    public UpdateViewModel Update { get; } = new((_, _, _) => { });

    private static string GetCompileTime()
    {
        return AppAssembly.CompileTime()?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
               ?? string.Empty;
    }
}

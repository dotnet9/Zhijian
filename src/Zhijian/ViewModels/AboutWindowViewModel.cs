using System.Globalization;
using System.Reflection;
using CodeWF.Toolkit.Core.Extensions;
using CodeWF.Avalonia.Lang;
using CodeWF.Toolkit.Core.UpdateChecking;
using ReactiveUI;
using System.Runtime.InteropServices;

namespace Zhijian.ViewModels;

public sealed class AboutWindowViewModel : ViewModelBase, IDisposable
{
    private static readonly Assembly AppAssembly = typeof(AboutWindowViewModel).Assembly;

    public string Version => UpdateVersion.Display(AppAssembly);

    public string Platform => $"{RuntimeInformation.FrameworkDescription} · Avalonia {typeof(Avalonia.Application).Assembly.GetName().Version}";

    public string Copyright => AppAssembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? string.Empty;

    public string CompileTime => GetCompileTime();

    public string Author => I18nManager.Instance.GetResource(ZhijianL.AboutAuthorName);

    public AboutWindowViewModel() => I18nManager.Instance.CultureChanged += OnCultureChanged;

    private void OnCultureChanged(object? sender, EventArgs args) => this.RaisePropertyChanged(nameof(Author));

    public void Dispose()
    {
        I18nManager.Instance.CultureChanged -= OnCultureChanged;
        Update.Dispose();
    }

    public string ContactUrl => "https://codewf.com";

    public string RepositoryUrl => "https://github.com/dotnet9/Zhijian";

    public string MindViewNuGetUrl => "https://www.nuget.org/packages/CodeWF.Avalonia.MindView";

    public Uri ContactUri => new(ContactUrl);

    public Uri RepositoryUri => new(RepositoryUrl);

    public Uri MindViewNuGetUri => new(MindViewNuGetUrl);

    public UpdateViewModel Update { get; } = new((_, _, _) => { });

    private static string GetCompileTime()
    {
        return AppAssembly.CompileTime()?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
               ?? string.Empty;
    }
}

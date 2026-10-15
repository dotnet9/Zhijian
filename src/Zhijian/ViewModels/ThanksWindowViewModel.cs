using CodeWF.Toolkit.Lang;

namespace Zhijian.ViewModels;

public sealed class ThanksWindowViewModel : ViewModelBase, IDisposable
{
    public ThanksWindowViewModel() => I18nManager.Instance.CultureChanged += OnCultureChanged;
    public string Title => I18nManager.Instance.GetResource(ZhijianL.Thanks);

    public string Description => I18nManager.Instance.GetResource(ZhijianL.ThanksDescription);
    private void OnCultureChanged(object? sender, EventArgs args)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Description));
    }
    public void Dispose() => I18nManager.Instance.CultureChanged -= OnCultureChanged;

    public IReadOnlyList<OpenSourceProjectInfo> Projects { get; } =
    [
        new("Dotnet", "https://dotnet.microsoft.com/zh-cn/"),
        new("Avalonia UI", "https://avaloniaui.net/"),
        new("Semi.Avalonia", "https://github.com/irihitech/Semi.Avalonia"),
        new("Ursa.Avalonia", "https://github.com/irihitech/Ursa.Avalonia"),
        new("CodeWF.Avalonia", "https://github.com/dotnet9/CodeWF.Avalonia")
    ];
}

public sealed record OpenSourceProjectInfo(string Name, string Url)
{
    public Uri Uri => new(Url);
}

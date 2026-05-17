namespace Zhijian.ViewModels;

public sealed class ThanksWindowViewModel : ViewModelBase
{
    public string Title => "感谢";

    public string Description => "枝见感谢这些优秀开源项目提供的基础能力与设计参考。";

    public IReadOnlyList<OpenSourceProjectInfo> Projects { get; } =
    [
        new("Dotnet", "https://dotnet.microsoft.com/zh-cn/"),
        new("Avalonia UI", "https://avaloniaui.net/"),
        new("Semi.Avalonia", "https://github.com/irihitech/Semi.Avalonia"),
        new("Ursa.Avalonia", "https://github.com/irihitech/Ursa.Avalonia"),
        new("AtomUI", "https://github.com/AtomUI/AtomUI")
    ];
}

public sealed record OpenSourceProjectInfo(string Name, string Url)
{
    public Uri Uri => new(Url);
}

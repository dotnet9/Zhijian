using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CodeWF.Markdown.Lite.Controls;
using CodeWF.Markdown.Lite.Themes;
using AtomButton = AtomUI.Desktop.Controls.Button;
using AtomHyperLinkButton = AtomUI.Desktop.Controls.HyperLinkButton;
using AtomWindow = AtomUI.Desktop.Controls.Window;

namespace Zhijian.Views;

public sealed class ChangelogWindow : AtomWindow
{
    private const string ChineseChangelogFileName = "CHANGELOG.zh-CN.md";
    private const string EnglishChangelogFileName = "CHANGELOG.md";

    private readonly MarkdownViewer _viewer = new()
    {
        TypographyTheme = MarkdownTypographyThemes.Simple,
        TypographySize = MarkdownTypographySizes.Normal
    };

    public ChangelogWindow()
    {
        Title = "更新日志";
        Width = 840;
        Height = 680;
        MinWidth = 640;
        MinHeight = 520;

        var title = new TextBlock
        {
            Text = "更新日志",
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var languageButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        languageButtons.Children.Add(CreateLanguageButton("中文", ChineseChangelogFileName));
        languageButtons.Children.Add(CreateLanguageButton("English", EnglishChangelogFileName));

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Children.Add(title);
        Grid.SetColumn(languageButtons, 1);
        header.Children.Add(languageButtons);

        var scrollViewer = new ScrollViewer
        {
            Content = _viewer,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        Content = new Border
        {
            Padding = new Thickness(24, 20),
            Child = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(new GridLength(1, GridUnitType.Star))
                },
                RowSpacing = 14,
                Children =
                {
                    header,
                    scrollViewer
                }
            }
        };
        Grid.SetRow(scrollViewer, 1);

        LoadChangelog(ChineseChangelogFileName);
    }

    private AtomButton CreateLanguageButton(string text, string fileName)
    {
        var button = new AtomButton
        {
            Content = text,
            MinWidth = 74,
            Height = 30,
            Padding = new Thickness(10, 4)
        };
        button.Click += (_, _) => LoadChangelog(fileName);
        return button;
    }

    private void LoadChangelog(string fileName)
    {
        var path = FindBundledFile(fileName);
        _viewer.Markdown = path is null
            ? $"# 更新日志\n\n未找到随程序复制的 `{fileName}` 文件。"
            : File.ReadAllText(path);
    }

    private static string? FindBundledFile(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(Directory.GetCurrentDirectory(), fileName),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", fileName)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", fileName))
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}

public sealed class AboutWindow : AtomWindow
{
    private const string WebsiteUrl = "https://codewf.com";
    private const string RepositoryUrl = "https://github.com/dotnet9/Zhijian";
    private const string MindViewNuGetUrl = "https://www.nuget.org/packages/CodeWF.MindView";
    private const string MindViewThemesNuGetUrl = "https://www.nuget.org/packages/CodeWF.MindView.Themes";

    public AboutWindow()
    {
        Title = "关于枝见";
        Width = 700;
        Height = 520;
        MinWidth = 560;
        MinHeight = 460;

        var title = new TextBlock
        {
            Text = "枝见 Zhijian",
            FontSize = 26,
            FontWeight = FontWeight.SemiBold
        };

        var description = new TextBlock
        {
            Text = "本地优先、Markdown-first 的 Avalonia / AtomUI 脑图编辑器。",
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brush.Parse("#667085")
        };

        var rows = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(0, 18, 0, 0)
        };
        rows.Children.Add(CreateTextRow("版本号", GetAppVersion()));
        rows.Children.Add(CreateTextRow("更新时间", "2026-05-17"));
        rows.Children.Add(CreateTextRow("作者", "沙漠尽头的狼"));
        rows.Children.Add(CreateLinkRow("联系方式", WebsiteUrl));
        rows.Children.Add(CreateLinkRow("仓库地址", RepositoryUrl));
        rows.Children.Add(CreateLinkRow("NuGet 包", MindViewNuGetUrl));
        rows.Children.Add(CreateLinkRow("主题包", MindViewThemesNuGetUrl));

        Content = new Border
        {
            Padding = new Thickness(28, 24),
            Child = new StackPanel
            {
                Spacing = 0,
                Children =
                {
                    title,
                    description,
                    rows
                }
            }
        };
    }

    private static Grid CreateTextRow(string label, string value)
    {
        var row = CreateRowGrid();
        row.Children.Add(CreateLabel(label));
        var text = new SelectableTextBlock
        {
            Text = value,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(text, 1);
        row.Children.Add(text);
        return row;
    }

    private static Grid CreateLinkRow(string label, string url)
    {
        var row = CreateRowGrid();
        row.Children.Add(CreateLabel(label));

        var link = new AtomHyperLinkButton
        {
            Content = new TextBlock
            {
                Text = url,
                TextWrapping = TextWrapping.Wrap
            },
            NavigateUri = new Uri(url),
            Padding = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(link, 1);
        row.Children.Add(link);
        return row;
    }

    private static Grid CreateRowGrid()
    {
        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(new GridLength(82)),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star))
            },
            MinHeight = 32
        };
    }

    private static TextBlock CreateLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Brush.Parse("#667085"),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static string GetAppVersion()
    {
        var assembly = typeof(AboutWindow).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
               ?? assembly.GetName().Version?.ToString()
               ?? "12.0.3.4";
    }
}

using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Zhijian.ViewModels;

public partial class ChangelogWindowViewModel : ViewModelBase
{
    private const string ChineseChangelogFileName = "CHANGELOG.zh-CN.md";
    private const string EnglishChangelogFileName = "CHANGELOG.md";

    [ObservableProperty]
    private string _markdown = string.Empty;

    public ChangelogWindowViewModel()
    {
        LoadChangelog(ChineseChangelogFileName);
    }

    [RelayCommand]
    private void LoadChinese()
    {
        LoadChangelog(ChineseChangelogFileName);
    }

    [RelayCommand]
    private void LoadEnglish()
    {
        LoadChangelog(EnglishChangelogFileName);
    }

    private void LoadChangelog(string fileName)
    {
        var path = FindBundledFile(fileName);
        Markdown = path is null
            ? $"# 更新日志\n\n未找到随程序复制的 `{fileName}` 文件。"
            : File.ReadAllText(path, Encoding.UTF8);
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

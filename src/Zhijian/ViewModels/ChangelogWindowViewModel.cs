using System.Text;
using System.Globalization;
using CodeWF.Avalonia.Lang;

namespace Zhijian.ViewModels;

public sealed class ChangelogWindowViewModel : ViewModelBase
{
    private const string ChineseChangelogFileName = "UpdateLog.md";
    private const string EnglishChangelogFileName = "UpdateLog.md";

    private string _markdown = string.Empty;

    public ChangelogWindowViewModel()
    {
        _ = LoadChangelogAsync(ChineseChangelogFileName);
    }

    public string Markdown
    {
        get => _markdown;
        private set => SetProperty(ref _markdown, value);
    }

    public async Task LoadChineseAsync()
    {
        await LoadChangelogAsync(ChineseChangelogFileName);
    }

    public async Task LoadEnglishAsync()
    {
        await LoadChangelogAsync(EnglishChangelogFileName);
    }

    private async Task LoadChangelogAsync(string fileName)
    {
        var path = await FindBundledFileAsync(fileName);
        Markdown = path is null
            ? string.Format(CultureInfo.CurrentCulture, I18nManager.Instance.GetResource(ZhijianL.ChangelogMissing), fileName)
            : await File.ReadAllTextAsync(path, Encoding.UTF8);
    }

    private static async Task<string?> FindBundledFileAsync(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, fileName),
            Path.Combine(Directory.GetCurrentDirectory(), fileName),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", fileName)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", fileName))
        };

        foreach (var candidate in candidates)
        {
            if (await Task.Run(() => File.Exists(candidate)))
            {
                return candidate;
            }
        }

        return null;
    }
}

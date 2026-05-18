namespace Zhijian.ViewModels;

public sealed record MindMapFileItem(
    string FilePath,
    string Name,
    string Extension,
    string Preview,
    DateTime ModifiedAt)
{
    public string DisplayName => Name;

    public string ExtensionText => Extension.TrimStart('.');
}

public sealed record RecentFileItem(string FilePath)
{
    public string DisplayName => Path.GetFileName(FilePath);

    public string FolderName => Path.GetDirectoryName(FilePath) ?? string.Empty;
}

using CodeWF.MindView;
using System.Text;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IMindMapEditorController
{
    public async Task<bool> ConfirmCloseAsync()
    {
        return await EnsureCanChangeDocumentAsync();
    }

    public async Task NewDocumentAsync()
    {
        if (!await EnsureCanChangeDocumentAsync())
        {
            return;
        }

        SetBlankDocument();
        StatusText = "已新建空白脑图";
    }

    public async Task OpenDocumentAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            if (!await EnsureCanChangeDocumentAsync())
            {
                return;
            }

            StatusText = "正在打开脑图文件...";
            var result = await _fileService.OpenAsync();
            if (result is null)
            {
                return;
            }

            await LoadOpenResultAsync(result);
        });
    }

    public async Task OpenFolderAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            StatusText = "正在打开文件夹...";
            var folderPath = await _fileService.PickFolderAsync();
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            await LoadFolderFilesAsync(folderPath);
            WorkspaceTabIndex = 0;
            StatusText = $"已打开文件夹：{folderPath}";
        });
    }

    public async Task OpenRecentFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        await RunFileOperationAsync(async () =>
        {
            if (!await EnsureCanChangeDocumentAsync())
            {
                return;
            }

            await LoadFilePathAsync(filePath);
        });
    }

    public async Task SaveAsync()
    {
        await RunFileOperationAsync(async () => await SaveDocumentAsync(forceSaveAs: false));
    }

    public async Task SaveAsAsync()
    {
        await RunFileOperationAsync(async () => await SaveDocumentAsync(forceSaveAs: true));
    }

    public void OpenFileLocation()
    {
        if (CurrentFilePath is null)
        {
            return;
        }

        _applicationActionService.OpenFileLocation(CurrentFilePath);
        StatusText = "已打开文件位置";
    }

    public async Task CopyAsMarkdownAsync()
    {
        ApplyMarkdownEditsIfNeeded();
        var content = MindMapDocumentCodec.ToMarkdown(Root);
        await _applicationActionService.SetClipboardTextAsync(content);
        var message = T(ZhijianL.StatusCopiedMarkdown);
        StatusText = message;
        _applicationActionService.ShowSuccessMessage(message);
    }

    public async Task ImportMarkdownAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenTextAsync(MindMapFileFormat.Markdown);
            if (content is null)
            {
                return;
            }

            var document = await DecodeDocumentAsync(MindMapFileFormat.Markdown, content, null);
            ReplaceTree(document.Root, "导入 Markdown", document.MarkdownSnapshot);
            StatusText = "已导入 Markdown";
        });
    }

    public async Task ImportOpmlAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenTextAsync(MindMapFileFormat.Opml);
            if (content is null)
            {
                return;
            }

            var document = await DecodeDocumentAsync(MindMapFileFormat.Opml, content, null);
            ReplaceTree(document.Root, "导入 OPML", document.MarkdownSnapshot);
            StatusText = "已导入 OPML";
        });
    }

    public async Task ImportXMindAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenBinaryAsync(MindMapFileFormat.XMind);
            if (content is null)
            {
                return;
            }

            var document = await DecodeDocumentAsync(MindMapFileFormat.XMind, null, content);
            ReplaceTree(document.Root, "导入 XMind", document.MarkdownSnapshot);
            StatusText = "已导入 XMind";
        });
    }

    public async Task ExportMarkdownAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = IsMarkdownMode ? MarkdownText : MindMapDocumentCodec.ToMarkdown(Root);
            await _fileService.SaveTextAsync(MindMapFileFormat.Markdown, content);
            StatusText = "已导出 Markdown";
        });
    }

    public async Task ExportOpmlAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            ApplyMarkdownEditsIfNeeded();
            await _fileService.SaveTextAsync(MindMapFileFormat.Opml, MindMapDocumentCodec.ToOpml(Root));
            StatusText = "已导出 OPML";
        });
    }

    public async Task ExportXMindAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            ApplyMarkdownEditsIfNeeded();
            await _fileService.SaveBinaryAsync(MindMapFileFormat.XMind, MindMapDocumentCodec.ToXMind(Root));
            StatusText = "已导出 XMind";
        });
    }

    private async Task<bool> EnsureCanChangeDocumentAsync()
    {
        if (!IsDirty)
        {
            return true;
        }

        var decision = await _fileService.ConfirmSaveChangesAsync(CurrentDocumentName);
        if (decision == MindMapSaveChangesDecision.Cancel)
        {
            return false;
        }

        if (decision == MindMapSaveChangesDecision.Discard)
        {
            return true;
        }

        await SaveDocumentAsync(forceSaveAs: false);
        return !IsDirty;
    }

    private void SetBlankDocument()
    {
        try
        {
            _isLoadingDocument = true;
            ReplaceTree(CreateBlankRoot(), "空白脑图", recordHistory: false);
            ResetHistory("空白脑图");
            CurrentFilePath = null;
            _currentFileFormat = MindMapFileFormat.Markdown;
            WorkspaceTabIndex = 1;
            MarkDocumentClean();
        }
        finally
        {
            _isLoadingDocument = false;
        }
    }

    private async Task OpenFolderFileAsync(MindMapFileItem file)
    {
        await RunFileOperationAsync(async () =>
        {
            if (!await EnsureCanChangeDocumentAsync())
            {
                SelectedFolderFile = FolderFiles.FirstOrDefault(item => item.FilePath == CurrentFilePath);
                return;
            }

            await LoadFilePathAsync(file.FilePath);
            WorkspaceTabIndex = 1;
        });
    }

    private async Task LoadOpenResultAsync(MindMapFileOpenResult result)
    {
        StatusText = $"正在解析：{Path.GetFileName(result.FilePath)}";
        var document = await DecodeDocumentAsync(result.Format, result.TextContent, result.BinaryContent, result.FilePath);
        await LoadDocumentAsync(document, result.FilePath, $"打开 {Path.GetFileName(result.FilePath)}");
    }

    private async Task LoadStartupDocumentAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)
            || !await FileExistsAsync(filePath)
            || !MindMapFileFormatRegistry.IsSupportedFile(filePath))
        {
            return;
        }

        try
        {
            await LoadFilePathAsync(filePath);
        }
        catch (Exception exception)
        {
            StatusText = $"默认文件加载失败：{exception.Message}";
        }
    }

    private async Task LoadFilePathAsync(string filePath)
    {
        if (!await FileExistsAsync(filePath))
        {
            await RemoveRecentFileAsync(filePath);
            StatusText = "文件不存在，已从最近文件中移除";
            return;
        }

        var format = MindMapFileFormatRegistry.GetFormatFromPath(filePath);
        StatusText = $"正在读取：{Path.GetFileName(filePath)}";
        var document = await ReadDocumentAsync(filePath, format);

        await LoadDocumentAsync(document, filePath, $"打开 {Path.GetFileName(filePath)}");
    }

    private static async Task<LoadedMindMapDocument> ReadDocumentAsync(string filePath, MindMapFileFormat format)
    {
        if (MindMapFileFormatRegistry.RequiresBinaryContent(format))
        {
            var binaryContent = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
            return await DecodeDocumentAsync(format, null, binaryContent, filePath).ConfigureAwait(false);
        }

        if (!MindMapFileFormatRegistry.IsTextFormat(format))
        {
            return await DecodeDocumentAsync(format, null, null, filePath).ConfigureAwait(false);
        }

        var textContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8).ConfigureAwait(false);
        return await DecodeDocumentAsync(format, textContent, null, filePath).ConfigureAwait(false);
    }

    private static Task<LoadedMindMapDocument> DecodeDocumentAsync(
        MindMapFileFormat format,
        string? textContent,
        byte[]? binaryContent,
        string? filePath = null)
    {
        return Task.Run(() =>
        {
            var root = MindMapDocumentCodec.FromDocument(format, textContent, binaryContent, filePath);

            return new LoadedMindMapDocument(root, format, MindMapDocumentCodec.ToMarkdown(root));
        });
    }

    private async Task LoadDocumentAsync(LoadedMindMapDocument document, string filePath, string historyLabel)
    {
        try
        {
            _isLoadingDocument = true;
            _currentFileFormat = document.Format;
            CurrentFilePath = filePath;
            ReplaceTree(document.Root, historyLabel, document.MarkdownSnapshot, recordHistory: false);
            ResetHistory(historyLabel, document.MarkdownSnapshot);
            MarkDocumentClean();
            await AddRecentFileAsync(filePath);
            await AddFileToFileListAsync(filePath);
            WorkspaceTabIndex = 1;
            StatusText = $"已打开：{Path.GetFileName(filePath)}";
        }
        finally
        {
            _isLoadingDocument = false;
        }
    }

    private async Task SaveDocumentAsync(bool forceSaveAs)
    {
        ApplyMarkdownEditsIfNeeded();

        var targetPath = CurrentFilePath;
        var targetFormat = MindMapFileFormatRegistry.CanWriteFormat(_currentFileFormat)
            ? _currentFileFormat
            : MindMapFileFormat.Markdown;
        if (forceSaveAs
            || string.IsNullOrWhiteSpace(targetPath)
            || !MindMapFileFormatRegistry.CanWriteFormat(_currentFileFormat))
        {
            var saveTarget = await _fileService.PickSaveTargetAsync(targetFormat, GetSuggestedFileName(targetFormat));
            if (saveTarget is null)
            {
                return;
            }

            targetPath = saveTarget.FilePath;
            targetFormat = saveTarget.Format;
        }

        await WriteDocumentAsync(targetPath, targetFormat);
        _currentFileFormat = targetFormat;
        CurrentFilePath = targetPath;
        await AddRecentFileAsync(targetPath);
        await AddFileToFileListAsync(targetPath);
        MarkDocumentClean();
        StatusText = $"已保存：{Path.GetFileName(targetPath)}";
    }

    private async Task WriteDocumentAsync(string filePath, MindMapFileFormat format)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            await Task.Run(() => Directory.CreateDirectory(directory));
        }

        switch (format)
        {
            case MindMapFileFormat.Markdown:
                await File.WriteAllTextAsync(filePath, MindMapDocumentCodec.ToMarkdown(Root), Encoding.UTF8);
                break;
            case MindMapFileFormat.Opml:
                await File.WriteAllTextAsync(filePath, MindMapDocumentCodec.ToOpml(Root), Encoding.UTF8);
                break;
            case MindMapFileFormat.XMind:
                var xmind = await Task.Run(() => MindMapDocumentCodec.ToXMind(Root));
                await File.WriteAllBytesAsync(filePath, xmind);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private string GetSuggestedFileName(MindMapFileFormat format)
    {
        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            return $"{Path.GetFileNameWithoutExtension(CurrentFilePath)}.{MindMapFileFormatRegistry.GetDefaultExtension(format)}";
        }

        var title = string.IsNullOrWhiteSpace(Root.Title) ? "未命名脑图" : Root.Title.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            title = title.Replace(invalid, '-');
        }

        return $"{title}.{MindMapFileFormatRegistry.GetDefaultExtension(format)}";
    }

    private async Task LoadFolderFilesAsync(string folderPath)
    {
        var filePaths = await Task.Run(() => Directory
            .EnumerateFiles(folderPath)
            .Where(MindMapFileFormatRegistry.IsSupportedFile)
            .OrderByDescending(File.GetLastWriteTime)
            .ToList());

        var fileItems = new List<MindMapFileItem>();
        foreach (var filePath in filePaths)
        {
            fileItems.Add(await CreateFileItemAsync(filePath));
        }

        FolderFiles.Clear();
        foreach (var fileItem in fileItems)
        {
            FolderFiles.Add(fileItem);
        }

        SelectFolderFile(CurrentFilePath);
        OnPropertyChanged(nameof(HasFolderFiles));
        OnPropertyChanged(nameof(IsFolderEmpty));
        OnPropertyChanged(nameof(FolderSummary));
    }

    private async Task AddFileToFileListAsync(string filePath)
    {
        if (!MindMapFileFormatRegistry.IsSupportedFile(filePath))
        {
            return;
        }

        var fileItem = await CreateFileItemAsync(filePath);
        var existing = FolderFiles.FirstOrDefault(item =>
            string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            var index = FolderFiles.IndexOf(existing);
            FolderFiles[index] = fileItem;
            SelectFolderFile(filePath);
            return;
        }

        FolderFiles.Insert(0, fileItem);
        SelectFolderFile(filePath);
        OnPropertyChanged(nameof(HasFolderFiles));
        OnPropertyChanged(nameof(IsFolderEmpty));
        OnPropertyChanged(nameof(FolderSummary));
    }

    private void SelectFolderFile(string? filePath)
    {
        SelectedFolderFile = filePath is null
            ? null
            : FolderFiles.FirstOrDefault(item => string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<MindMapFileItem> CreateFileItemAsync(string filePath)
    {
        var modifiedAt = await Task.Run(() => File.GetLastWriteTime(filePath));
        return new MindMapFileItem(
            filePath,
            Path.GetFileNameWithoutExtension(filePath),
            Path.GetExtension(filePath),
            await CreateFilePreviewAsync(filePath),
            modifiedAt);
    }

    private static async Task<string> CreateFilePreviewAsync(string filePath)
    {
        try
        {
            var format = MindMapFileFormatRegistry.GetFormatFromPath(filePath);
            if (format == MindMapFileFormat.XMind)
            {
                var bytes = await File.ReadAllBytesAsync(filePath);
                var root = await Task.Run(() => MindMapDocumentCodec.FromXMind(bytes));
                return string.IsNullOrWhiteSpace(root.Title) ? "XMind 脑图" : root.Title.Trim();
            }

            if (!MindMapFileFormatRegistry.IsTextFormat(format))
            {
                var fileInfo = await Task.Run(() => new FileInfo(filePath));
                return $"{MindMapFileFormatRegistry.GetDisplayName(format)} · {FormatFileSize(fileInfo.Length)}";
            }

            var prefix = await ReadTextPrefixAsync(filePath);
            var preview = string.Join(
                Environment.NewLine,
                SplitLines(prefix)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(2));
            return string.IsNullOrWhiteSpace(preview)
                ? MindMapFileFormatRegistry.GetDisplayName(format)
                : preview;
        }
        catch
        {
            return "无法预览";
        }
    }

    private async Task LoadRecentFilesAsync()
    {
        RecentFiles.Clear();
        var paths = await _recentFileStore.LoadAsync(MindMapFileFormatRegistry.IsSupportedFile);
        foreach (var path in paths)
        {
            RecentFiles.Add(new RecentFileItem(path));
        }
    }

    private async Task AddRecentFileAsync(string filePath)
    {
        RemoveRecentFileCore(filePath);
        RecentFiles.Insert(0, new RecentFileItem(filePath));
        while (RecentFiles.Count > MaxRecentFiles)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        await SaveRecentFilesAsync();
    }

    private async Task RemoveRecentFileAsync(string filePath, bool save = true)
    {
        RemoveRecentFileCore(filePath);
        if (save)
        {
            await SaveRecentFilesAsync();
        }
    }

    private void RemoveRecentFileCore(string filePath)
    {
        var existing = RecentFiles.FirstOrDefault(item =>
            string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            RecentFiles.Remove(existing);
        }
    }

    private async Task SaveRecentFilesAsync()
    {
        await _recentFileStore.SaveAsync(RecentFiles.Select(item => item.FilePath));
    }

    private static async Task<bool> FileExistsAsync(string filePath)
    {
        return await Task.Run(() => File.Exists(filePath));
    }

    private static async Task<string> ReadTextPrefixAsync(string filePath, int maxChars = 4096)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var buffer = new char[maxChars];
        var read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length));
        return new string(buffer, 0, read);
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / 1024d / 1024d:0.#} MB";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024d:0.#} KB";
        }

        return $"{bytes} B";
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
    }

}

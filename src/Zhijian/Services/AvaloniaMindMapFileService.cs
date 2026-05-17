using System.Text;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CodeWF.MindView;
using Zhijian.Views;

namespace Zhijian.Services;

public sealed class AvaloniaMindMapFileService(Window owner) : IMindMapFileService
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public async Task<MindMapFileOpenResult?> OpenAsync(CancellationToken cancellationToken = default)
    {
        var file = await OpenFileAsync();
        if (file is null)
        {
            return null;
        }

        var filePath = GetLocalPath(file);
        var format = GetFormatFromPath(filePath);
        if (format == MindMapFileFormat.XMind)
        {
            await using var stream = await file.OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            return new MindMapFileOpenResult(filePath, format, null, memory.ToArray());
        }

        await using var textStream = await file.OpenReadAsync();
        using var reader = new StreamReader(textStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return new MindMapFileOpenResult(
            filePath,
            format,
            await reader.ReadToEndAsync(cancellationToken),
            null);
    }

    public async Task<string?> OpenTextAsync(
        MindMapFileFormat format,
        CancellationToken cancellationToken = default)
    {
        var file = await OpenFileAsync(format);
        if (file is null)
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    public async Task<byte[]?> OpenBinaryAsync(
        MindMapFileFormat format,
        CancellationToken cancellationToken = default)
    {
        var file = await OpenFileAsync(format);
        if (file is null)
        {
            return null;
        }

        await using var stream = await file.OpenReadAsync();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "打开文件夹",
            AllowMultiple = false
        });

        return folders.Count == 0 ? null : GetLocalPath(folders[0]);
    }

    public async Task<MindMapFileSaveTarget?> PickSaveTargetAsync(
        MindMapFileFormat suggestedFormat,
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        var fileType = GetFileType(suggestedFormat);
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存脑图",
            DefaultExtension = GetDefaultExtension(suggestedFormat),
            FileTypeChoices = GetAllFileTypes(),
            SuggestedFileType = fileType,
            SuggestedFileName = suggestedFileName,
            ShowOverwritePrompt = true
        });

        if (file is null)
        {
            return null;
        }

        var filePath = GetLocalPath(file);
        return new MindMapFileSaveTarget(filePath, GetFormatFromPath(filePath, suggestedFormat));
    }

    public async Task SaveTextAsync(
        MindMapFileFormat format,
        string content,
        CancellationToken cancellationToken = default)
    {
        var file = await SaveFileAsync(format);
        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Utf8NoBom);
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
    }

    public async Task SaveBinaryAsync(
        MindMapFileFormat format,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        var file = await SaveFileAsync(format);
        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync();
        await stream.WriteAsync(content, cancellationToken);
    }

    public async Task<MindMapSaveChangesDecision> ConfirmSaveChangesAsync(
        string documentName,
        CancellationToken cancellationToken = default)
    {
        var dialog = new SaveChangesWindow(documentName);
        return await dialog.ShowDialog<MindMapSaveChangesDecision>(owner);
    }

    private async Task<IStorageFile?> OpenFileAsync(MindMapFileFormat format)
    {
        var fileType = GetFileType(format);
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = $"导入 {GetDisplayName(format)}",
            AllowMultiple = false,
            FileTypeFilter = [fileType],
            SuggestedFileType = fileType
        });

        return files.Count == 0 ? null : files[0];
    }

    private async Task<IStorageFile?> OpenFileAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "打开脑图文件",
            AllowMultiple = false,
            FileTypeFilter = GetAllFileTypes()
        });

        return files.Count == 0 ? null : files[0];
    }

    private async Task<IStorageFile?> SaveFileAsync(MindMapFileFormat format)
    {
        var fileType = GetFileType(format);
        return await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"导出 {GetDisplayName(format)}",
            DefaultExtension = GetDefaultExtension(format),
            FileTypeChoices = [fileType],
            SuggestedFileType = fileType,
            SuggestedFileName = $"枝见.{GetDefaultExtension(format)}",
            ShowOverwritePrompt = true
        });
    }

    private static FilePickerFileType GetFileType(MindMapFileFormat format)
    {
        return format switch
        {
            MindMapFileFormat.Markdown => new FilePickerFileType("Markdown")
            {
                Patterns = ["*.md", "*.markdown"],
                MimeTypes = ["text/markdown", "text/plain"]
            },
            MindMapFileFormat.Opml => new FilePickerFileType("OPML")
            {
                Patterns = ["*.opml", "*.xml"],
                MimeTypes = ["text/x-opml", "application/xml", "text/xml"]
            },
            MindMapFileFormat.XMind => new FilePickerFileType("XMind")
            {
                Patterns = ["*.xmind"],
                MimeTypes = ["application/octet-stream"]
            },
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private static IReadOnlyList<FilePickerFileType> GetAllFileTypes()
    {
        return
        [
            new FilePickerFileType("支持的脑图文件")
            {
                Patterns = ["*.md", "*.markdown", "*.opml", "*.xml", "*.xmind"],
                MimeTypes =
                [
                    "text/markdown",
                    "text/plain",
                    "text/x-opml",
                    "application/xml",
                    "text/xml",
                    "application/octet-stream"
                ]
            },
            GetFileType(MindMapFileFormat.Markdown),
            GetFileType(MindMapFileFormat.Opml),
            GetFileType(MindMapFileFormat.XMind)
        ];
    }

    private static string GetDefaultExtension(MindMapFileFormat format)
    {
        return format switch
        {
            MindMapFileFormat.Markdown => "md",
            MindMapFileFormat.Opml => "opml",
            MindMapFileFormat.XMind => "xmind",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private static string GetDisplayName(MindMapFileFormat format)
    {
        return format switch
        {
            MindMapFileFormat.Markdown => "Markdown",
            MindMapFileFormat.Opml => "OPML",
            MindMapFileFormat.XMind => "XMind",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    private static string GetLocalPath(IStorageItem item)
    {
        return item.Path.LocalPath;
    }

    private static MindMapFileFormat GetFormatFromPath(
        string filePath,
        MindMapFileFormat fallback = MindMapFileFormat.Markdown)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".md" or ".markdown" => MindMapFileFormat.Markdown,
            ".opml" or ".xml" => MindMapFileFormat.Opml,
            ".xmind" => MindMapFileFormat.XMind,
            _ => fallback
        };
    }
}

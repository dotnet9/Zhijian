using System.Text;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace Zhijian.Services;

public sealed class AvaloniaMindMapFileService(Window owner) : IMindMapFileService
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

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
}

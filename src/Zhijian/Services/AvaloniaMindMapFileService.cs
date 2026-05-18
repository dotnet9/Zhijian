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
        var file = await OpenFileAsync("打开可编辑脑图文件", GetEditableFileTypes());
        return await ReadOpenResultAsync(file, cancellationToken);
    }

    public async Task<MindMapFileOpenResult?> ImportAsync(CancellationToken cancellationToken = default)
    {
        var file = await OpenFileAsync("导入文件", GetImportFileTypes());
        return await ReadOpenResultAsync(file, cancellationToken);
    }

    private static async Task<MindMapFileOpenResult?> ReadOpenResultAsync(
        IStorageFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return null;
        }

        var filePath = GetLocalPath(file);
        var format = MindMapFileFormatRegistry.GetFormatFromPath(filePath);
        if (MindMapFileFormatRegistry.RequiresBinaryContent(format))
        {
            await using var stream = await file.OpenReadAsync();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, cancellationToken);
            return new MindMapFileOpenResult(filePath, format, null, memory.ToArray());
        }

        if (!MindMapFileFormatRegistry.IsTextFormat(format))
        {
            return new MindMapFileOpenResult(filePath, format, null, null);
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
        var writableFormat = MindMapFileFormatRegistry.CanWriteFormat(suggestedFormat)
            ? suggestedFormat
            : MindMapFileFormat.Markdown;
        var fileType = GetFileType(writableFormat);
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "另存为可编辑脑图",
            DefaultExtension = MindMapFileFormatRegistry.GetDefaultExtension(writableFormat),
            FileTypeChoices = GetWritableFileTypes(),
            SuggestedFileType = fileType,
            SuggestedFileName = suggestedFileName,
            ShowOverwritePrompt = true
        });

        if (file is null)
        {
            return null;
        }

        var filePath = GetLocalPath(file);
        var targetFormat = MindMapFileFormatRegistry.GetFormatFromPath(filePath, writableFormat);
        if (!MindMapFileFormatRegistry.CanWriteFormat(targetFormat))
        {
            targetFormat = writableFormat;
            filePath = Path.ChangeExtension(filePath, MindMapFileFormatRegistry.GetDefaultExtension(targetFormat));
        }

        return new MindMapFileSaveTarget(filePath, targetFormat);
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
            Title = $"导入 {MindMapFileFormatRegistry.GetDisplayName(format)}",
            AllowMultiple = false,
            FileTypeFilter = [fileType],
            SuggestedFileType = fileType
        });

        return files.Count == 0 ? null : files[0];
    }

    private async Task<IStorageFile?> OpenFileAsync(string title, IReadOnlyList<FilePickerFileType> fileTypeFilter)
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypeFilter
        });

        return files.Count == 0 ? null : files[0];
    }

    private async Task<IStorageFile?> SaveFileAsync(MindMapFileFormat format)
    {
        var fileType = GetFileType(format);
        return await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = $"导出 {MindMapFileFormatRegistry.GetDisplayName(format)}",
            DefaultExtension = MindMapFileFormatRegistry.GetDefaultExtension(format),
            FileTypeChoices = [fileType],
            SuggestedFileType = fileType,
            SuggestedFileName = $"枝见.{MindMapFileFormatRegistry.GetDefaultExtension(format)}",
            ShowOverwritePrompt = true
        });
    }

    private static FilePickerFileType GetFileType(MindMapFileFormat format)
    {
        var descriptor = MindMapFileFormatRegistry.GetDescriptor(format);
        var fileType = new FilePickerFileType(descriptor.DisplayName)
        {
            Patterns = descriptor.Patterns
        };
        if (descriptor.MimeTypes.Count > 0)
        {
            fileType.MimeTypes = descriptor.MimeTypes;
        }

        return fileType;
    }

    private static IReadOnlyList<FilePickerFileType> GetImportFileTypes()
    {
        return
        [
            new FilePickerFileType("支持的导入格式")
            {
                Patterns = MindMapFileFormatRegistry.ReadablePatterns,
                MimeTypes = MindMapFileFormatRegistry.ReadableMimeTypes
            },
            ..MindMapFileFormatRegistry.ReadableFormats.Select(descriptor => GetFileType(descriptor.Format))
        ];
    }

    private static IReadOnlyList<FilePickerFileType> GetEditableFileTypes()
    {
        return
        [
            new FilePickerFileType("可编辑脑图文件")
            {
                Patterns = MindMapFileFormatRegistry.WritablePatterns,
                MimeTypes = MindMapFileFormatRegistry.WritableMimeTypes
            },
            ..MindMapFileFormatRegistry.WritableFormats.Select(descriptor => GetFileType(descriptor.Format))
        ];
    }

    private static IReadOnlyList<FilePickerFileType> GetWritableFileTypes()
    {
        return MindMapFileFormatRegistry.WritableFormats
            .Select(descriptor => GetFileType(descriptor.Format))
            .ToArray();
    }

    private static string GetLocalPath(IStorageItem item)
    {
        return item.Path.LocalPath;
    }

}

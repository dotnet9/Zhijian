using CodeWF.MindView;

namespace Zhijian.Services;

public sealed class DisabledMindMapFileService : IMindMapFileService
{
    public Task<MindMapFileOpenResult?> OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<MindMapFileOpenResult?>(null);
    }

    public Task<MindMapFileOpenResult?> ImportAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<MindMapFileOpenResult?>(null);
    }

    public Task<string?> OpenTextAsync(MindMapFileFormat format, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<byte[]?> OpenBinaryAsync(MindMapFileFormat format, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<byte[]?>(null);
    }

    public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<MindMapFileSaveTarget?> PickSaveTargetAsync(
        MindMapFileFormat suggestedFormat,
        string suggestedFileName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<MindMapFileSaveTarget?>(null);
    }

    public Task SaveTextAsync(MindMapFileFormat format, string content, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveBinaryAsync(MindMapFileFormat format, byte[] content, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<MindMapSaveChangesDecision> ConfirmSaveChangesAsync(
        string documentName,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MindMapSaveChangesDecision.Discard);
    }
}

using CodeWF.MindView;

namespace Zhijian.Services;

public sealed class DisabledMindMapFileService : IMindMapFileService
{
    public Task<string?> OpenTextAsync(MindMapFileFormat format, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<byte[]?> OpenBinaryAsync(MindMapFileFormat format, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<byte[]?>(null);
    }

    public Task SaveTextAsync(MindMapFileFormat format, string content, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SaveBinaryAsync(MindMapFileFormat format, byte[] content, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

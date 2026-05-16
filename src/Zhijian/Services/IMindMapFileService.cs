namespace Zhijian.Services;

public interface IMindMapFileService
{
    Task<string?> OpenTextAsync(MindMapFileFormat format, CancellationToken cancellationToken = default);

    Task<byte[]?> OpenBinaryAsync(MindMapFileFormat format, CancellationToken cancellationToken = default);

    Task SaveTextAsync(MindMapFileFormat format, string content, CancellationToken cancellationToken = default);

    Task SaveBinaryAsync(MindMapFileFormat format, byte[] content, CancellationToken cancellationToken = default);
}

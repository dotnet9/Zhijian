using System.Text;
using System.Text.Json;

namespace Zhijian.Services;

public sealed class RecentFileStore(string filePath, int maxFiles)
{
    public async Task<IReadOnlyList<string>> LoadAsync(
        Func<string, bool> isSupportedFile,
        CancellationToken cancellationToken = default)
    {
        if (!await FileExistsAsync(filePath, cancellationToken))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken);
            var paths = Parse(json);
            return await Task.Run(
                () => paths
                    .Where(File.Exists)
                    .Where(isSupportedFile)
                    .Take(maxFiles)
                    .ToArray(),
                cancellationToken);
        }
        catch (Exception exception)
        {
            ApplicationLogger.Warning($"Loading recent files failed. file=\"{filePath}\"", exception);
            return [];
        }
    }

    public async Task SaveAsync(
        IEnumerable<string> paths,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            await Task.Run(() => Directory.CreateDirectory(directory), cancellationToken);
        }

        await File.WriteAllTextAsync(filePath, CreateJson(paths.Take(maxFiles)), Encoding.UTF8, cancellationToken);
    }

    private static async Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() => File.Exists(path), cancellationToken);
    }

    private static IReadOnlyList<string> Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return document.RootElement
            .EnumerateArray()
            .Where(element => element.ValueKind == JsonValueKind.String)
            .Select(element => element.GetString())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToArray();
    }

    private static string CreateJson(IEnumerable<string> paths)
    {
        using var memory = new MemoryStream();
        using (var writer = new Utf8JsonWriter(memory, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartArray();
            foreach (var path in paths)
            {
                writer.WriteStringValue(path);
            }

            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(memory.ToArray());
    }
}

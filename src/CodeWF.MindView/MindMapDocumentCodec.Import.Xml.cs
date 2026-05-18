using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CodeWF.MindView;

public static partial class MindMapDocumentCodec
{
    private static MindMapNode FromXml(string xml, string? filePath)
    {
        var document = XDocument.Parse(xml);
        var rootName = document.Root?.Name.LocalName;
        if (string.Equals(rootName, "opml", StringComparison.OrdinalIgnoreCase))
        {
            return FromOpml(xml);
        }

        if (string.Equals(rootName, "map", StringComparison.OrdinalIgnoreCase))
        {
            return FromFreeMind(xml);
        }

        if (IsDrawIoDocument(document))
        {
            return FromDrawIoXml(document, filePath);
        }

        return FromGenericXml(document, filePath);
    }

    private static MindMapNode FromFreeMind(string xml)
    {
        var document = XDocument.Parse(xml);
        var rootElement = document.Root?
            .Elements()
            .FirstOrDefault(element => element.Name.LocalName == "node");
        if (rootElement is null)
        {
            return FromGenericXml(document, null);
        }

        return FromFreeMindNode(rootElement);
    }

    private static MindMapNode FromFreeMindNode(XElement element)
    {
        var title = element.Attribute("TEXT")?.Value
            ?? element.Attribute("text")?.Value
            ?? element.Elements().FirstOrDefault(child => child.Name.LocalName == "richcontent")?.Value;
        var node = new MindMapNode(CleanText(title, UntitledTopic))
        {
            Note = CleanText(element
                .Elements()
                .FirstOrDefault(child => child.Name.LocalName == "richcontent"
                    && string.Equals((string?)child.Attribute("TYPE"), "NOTE", StringComparison.OrdinalIgnoreCase))
                ?.Value)
        };

        foreach (var child in element.Elements().Where(child => child.Name.LocalName == "node"))
        {
            node.Children.Add(FromFreeMindNode(child));
        }

        return node;
    }

    private static MindMapNode FromDrawIo(string xml, string? filePath)
    {
        var document = XDocument.Parse(xml);
        return FromDrawIoXml(document, filePath);
    }

    private static MindMapNode FromDrawIoXml(XDocument document, string? filePath)
    {
        var root = new MindMapNode(GetFileTitle(filePath, "draw.io 图表"));
        var diagrams = document.Descendants().Where(element => element.Name.LocalName == "diagram").ToList();
        if (diagrams.Count > 0)
        {
            foreach (var diagram in diagrams)
            {
                var diagramNode = new MindMapNode(CleanText(diagram.Attribute("name")?.Value, "Diagram"));
                if (!AddDrawIoDiagramContent(diagram, diagramNode))
                {
                    AddDrawIoCells(document, diagramNode);
                }

                root.Children.Add(diagramNode);
            }
        }
        else
        {
            AddDrawIoCells(document, root);
        }

        return root.Children.Count == 1 ? root.Children[0] : root;
    }

    private static bool AddDrawIoDiagramContent(XElement diagram, MindMapNode parent)
    {
        if (AddDrawIoCells(diagram, parent) > 0)
        {
            return true;
        }

        var content = diagram.Value;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        if (TryParseDrawIoXml(content, out var uncompressedDocument))
        {
            return AddDrawIoCells(uncompressedDocument, parent) > 0;
        }

        if (!TryDecodeDrawIoDiagram(content, out var decodedXml))
        {
            return false;
        }

        return TryParseDrawIoXml(decodedXml, out var decodedDocument)
            && AddDrawIoCells(decodedDocument, parent) > 0;
    }

    private static int AddDrawIoCells(XContainer container, MindMapNode parent)
    {
        var values = container
                     .Descendants()
                     .Select(element => element.Attribute("value")?.Value ?? element.Attribute("label")?.Value)
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Select(value => CleanText(value))
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Take(120)
                     .ToList();

        foreach (var value in values)
        {
            parent.Children.Add(new MindMapNode(value));
        }

        return values.Count;
    }

    private static bool TryParseDrawIoXml(string xml, out XDocument document)
    {
        document = new XDocument();
        var decoded = DecodeUriComponent(xml).Trim();
        if (!decoded.StartsWith('<'))
        {
            return false;
        }

        try
        {
            document = XDocument.Parse(decoded);
            return IsDrawIoDocument(document);
        }
        catch
        {
            return false;
        }
    }

    private static bool TryDecodeDrawIoDiagram(string encoded, out string xml)
    {
        xml = string.Empty;
        var bytes = TryDecodeBase64(encoded);
        if (bytes is null)
        {
            return false;
        }

        foreach (var useZLib in new[] { false, true })
        {
            if (!TryInflate(bytes, useZLib, out var inflated))
            {
                continue;
            }

            var decoded = DecodeUriComponent(inflated).Trim();
            if (decoded.StartsWith('<') || decoded.Contains("mxGraphModel", StringComparison.OrdinalIgnoreCase))
            {
                xml = decoded;
                return true;
            }
        }

        return false;
    }

    private static byte[]? TryDecodeBase64(string value)
    {
        var normalized = Regex.Replace(value.Trim(), @"\s+", string.Empty)
            .Replace('-', '+')
            .Replace('_', '/');
        if (normalized.Length == 0)
        {
            return null;
        }

        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + 4 - padding, '=');
        }

        try
        {
            return Convert.FromBase64String(normalized);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryInflate(byte[] bytes, bool useZLib, out string text)
    {
        text = string.Empty;
        try
        {
            using var input = new MemoryStream(bytes);
            using Stream inflater = useZLib
                ? new ZLibStream(input, CompressionMode.Decompress)
                : new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            inflater.CopyTo(output);
            text = Encoding.UTF8.GetString(output.ToArray());
            return !string.IsNullOrWhiteSpace(text);
        }
        catch
        {
            return false;
        }
    }

    private static string DecodeUriComponent(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }

    private static MindMapNode FromGenericXml(XDocument document, string? filePath)
    {
        var root = new MindMapNode(GetFileTitle(filePath, document.Root?.Name.LocalName ?? "XML"));
        foreach (var element in document
                     .Descendants()
                     .Where(element => element != document.Root)
                     .Select(ReadElementTitle)
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Take(120))
        {
            root.Children.Add(new MindMapNode(element));
        }

        return root.Children.Count == 0 ? CreateMetadataNode(MindMapFileFormat.Xml, filePath) : root;
    }

    private static string ReadElementTitle(XElement element)
    {
        var attribute = element.Attributes().FirstOrDefault(attr =>
            attr.Name.LocalName is "TEXT" or "text" or "title" or "name" or "label" or "value");
        if (attribute is not null)
        {
            return CleanText(attribute.Value);
        }

        return element.HasElements ? element.Name.LocalName : CleanText(element.Value);
    }

    private static bool IsDrawIoDocument(XDocument document)
    {
        var rootName = document.Root?.Name.LocalName;
        return string.Equals(rootName, "mxfile", StringComparison.OrdinalIgnoreCase)
            || document.Descendants().Any(element => element.Name.LocalName is "mxGraphModel" or "mxCell");
    }
}

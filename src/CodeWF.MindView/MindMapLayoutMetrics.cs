using Avalonia;

namespace CodeWF.MindView;

public enum MindMapNodeVisualKind
{
    Root,
    Branch,
    Leaf
}

public readonly record struct MindMapNodeSizeMetrics(
    double MinWidth,
    double MaxWidth,
    double MinHeight,
    Thickness Padding,
    double FontSize);

public static class MindMapLayoutMetrics
{
    public const double DefaultHorizontalSpacing = 104;
    public const double DefaultVerticalSpacing = 34;
    public const double RootMinWidth = 88;
    public const double RootMaxWidth = 260;
    public const double RootMinHeight = 46;
    public const double BranchMinWidth = 112;
    public const double BranchMaxWidth = 240;
    public const double BranchMinHeight = 42;
    public const double LeafMinWidth = 48;
    public const double LeafMaxWidth = 260;
    public const double LeafMinHeight = 30;
    public const double DragHandleHitWidth = 16;
    public const double NoteFontSize = 13;
    public const double NoteVerticalSpacing = 4;
    public const double NoteMinHeight = 28;
    public const double NoteMaxHeight = 96;

    public static MindMapNodeVisualKind GetVisualKind(int level)
    {
        if (level <= 1)
        {
            return MindMapNodeVisualKind.Root;
        }

        return level == 2 ? MindMapNodeVisualKind.Branch : MindMapNodeVisualKind.Leaf;
    }

    public static MindMapNodeSizeMetrics GetSizeMetrics(int level)
    {
        return GetVisualKind(level) switch
        {
            MindMapNodeVisualKind.Root => new MindMapNodeSizeMetrics(
                RootMinWidth,
                RootMaxWidth,
                RootMinHeight,
                new Thickness(10, 5),
                18),
            MindMapNodeVisualKind.Branch => new MindMapNodeSizeMetrics(
                BranchMinWidth,
                BranchMaxWidth,
                BranchMinHeight,
                new Thickness(12, 5),
                17),
            _ => new MindMapNodeSizeMetrics(
                LeafMinWidth,
                LeafMaxWidth,
                LeafMinHeight,
                new Thickness(0, 2),
                16)
        };
    }

    public static Size EstimateNodeSize(MindMapNode node, int level, string placeholder = "Topic")
    {
        var metrics = GetSizeMetrics(level);
        var text = string.IsNullOrWhiteSpace(node.Title) ? placeholder : node.Title.Trim();
        var paddingWidth = metrics.Padding.Left + metrics.Padding.Right;
        var paddingHeight = metrics.Padding.Top + metrics.Padding.Bottom;
        var minContentWidth = Math.Max(12, metrics.MinWidth - paddingWidth);
        var maxContentWidth = Math.Max(minContentWidth, metrics.MaxWidth - paddingWidth);
        var textWidth = Math.Max(minContentWidth, EstimateMaxLineWidth(text, metrics.FontSize));
        var contentWidth = Math.Clamp(textWidth, minContentWidth, maxContentWidth);
        var lineCount = EstimateWrappedLineCount(text, metrics.FontSize, maxContentWidth);
        var lineHeight = Math.Ceiling(metrics.FontSize * 1.35);
        var height = Math.Max(metrics.MinHeight, lineCount * lineHeight + paddingHeight);

        if (!string.IsNullOrWhiteSpace(node.Note))
        {
            var noteText = node.Note.Trim();
            // 备注参与布局估算，按显式换行逐段计算，避免多行备注压到后续兄弟节点。
            var noteWidth = Math.Max(minContentWidth, EstimateMaxLineWidth(noteText, NoteFontSize));
            var noteLines = EstimateWrappedLineCount(noteText, NoteFontSize, maxContentWidth);
            var noteHeight = Math.Clamp(
                noteLines * Math.Ceiling(NoteFontSize * 1.45),
                NoteMinHeight,
                NoteMaxHeight);
            height += NoteVerticalSpacing + noteHeight;
            contentWidth = Math.Max(contentWidth, Math.Clamp(noteWidth, minContentWidth, maxContentWidth));
        }

        return new Size(contentWidth + paddingWidth, height);
    }

    private static int EstimateWrappedLineCount(string text, double fontSize, double maxLineWidth)
    {
        var lines = 0;
        foreach (var paragraph in SplitLines(text))
        {
            var width = EstimateTextWidth(paragraph, fontSize);
            lines += Math.Max(1, (int)Math.Ceiling(width / maxLineWidth));
        }

        return Math.Max(1, lines);
    }

    private static double EstimateMaxLineWidth(string text, double fontSize)
    {
        var width = 0d;
        foreach (var line in SplitLines(text))
        {
            width = Math.Max(width, EstimateTextWidth(line, fontSize));
        }

        return Math.Max(fontSize, width);
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private static double EstimateTextWidth(string text, double fontSize)
    {
        var width = 0d;
        foreach (var character in text)
        {
            width += character <= '\u007f' ? fontSize * 0.56 : fontSize;
        }

        return Math.Max(fontSize, width);
    }
}

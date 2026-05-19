using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace CodeWF.MindView.Controls;

public partial class MindMapEditor
{
    private void UpdateNodePosition(MindMapNode node)
    {
        if (!_nodeFrames.TryGetValue(node, out var control))
        {
            return;
        }

        Canvas.SetLeft(control, node.X);
        Canvas.SetTop(control, node.Y);
        PositionNodeToolbar();
    }

    private void UpdateConnectors()
    {
        foreach (var connector in _connectors)
        {
            var parentSize = GetRenderedNodeSize(connector.Parent);
            var childSize = GetRenderedNodeSize(connector.Child);
            var start = GetConnectorStart(connector.Parent, parentSize);
            var end = GetConnectorEnd(connector.Child, childSize);

            connector.Path.Data = CreateConnectorGeometry(start, end);
        }
    }

    private Point GetConnectorStart(MindMapNode node, Size size)
    {
        return new Point(node.X + size.Width, node.Y + size.Height / 2);
    }

    private Point GetConnectorEnd(MindMapNode node, Size size)
    {
        return new Point(node.X, node.Y + size.Height / 2);
    }

    private void EnsureCanvasSize()
    {
        var nodes = _nodeFrames.Keys.ToList();
        var horizontalPadding = GetCanvasHorizontalPadding();
        var verticalPadding = GetCanvasVerticalPadding();
        var width = nodes.Count == 0
            ? MinCanvasWidth + horizontalPadding
            : nodes.Max(node => node.X + GetRenderedNodeSize(node).Width + horizontalPadding);
        var height = nodes.Count == 0
            ? MinCanvasHeight + verticalPadding
            : nodes.Max(node => node.Y + GetRenderedNodeSize(node).Height + verticalPadding);

        _canvas.Width = Math.Max(MinCanvasWidth + horizontalPadding, width);
        _canvas.Height = Math.Max(MinCanvasHeight + verticalPadding, height);
    }

    private double GetCanvasHorizontalPadding()
    {
        var viewportWidth = _scrollViewer.Viewport.Width;
        if (viewportWidth <= 0 || _zoomScale <= 0)
        {
            return MinCanvasHorizontalPadding;
        }

        return Math.Max(MinCanvasHorizontalPadding, viewportWidth / _zoomScale);
    }

    private double GetCanvasVerticalPadding()
    {
        var viewportHeight = _scrollViewer.Viewport.Height;
        if (viewportHeight <= 0 || _zoomScale <= 0)
        {
            return MinCanvasVerticalPadding;
        }

        return Math.Max(MinCanvasVerticalPadding, viewportHeight / _zoomScale);
    }

    private Size GetRenderedNodeSize(MindMapNode node)
    {
        if (_nodeFrames.TryGetValue(node, out var frame)
            && frame.Bounds.Width > 0
            && frame.Bounds.Height > 0)
        {
            return frame.Bounds.Size;
        }

        var metrics = GetNodeMetrics(node);
        return MindMapLayoutMetrics.EstimateNodeSize(node, GetNodeLevel(node), metrics.Placeholder);
    }

    private NodeMetrics GetNodeMetrics(MindMapNode node)
    {
        var level = GetNodeLevel(node);
        if (level <= 1)
        {
            return new NodeMetrics(
                MindMapLayoutMetrics.RootMinWidth,
                MindMapLayoutMetrics.RootMaxWidth,
                MindMapLayoutMetrics.RootMinHeight,
                new CornerRadius(8),
                GetResourceBrush(MindViewStyleKeys.RootBackgroundBrushResource, "#148BFF", "#148BFF"),
                Brushes.Transparent,
                new Thickness(0),
                new Thickness(10, 5),
                BoxShadows.Parse("0 6 18 0 #16000000"),
                GetResourceBrush(MindViewStyleKeys.RootForegroundBrushResource, "#FFFFFF", "#FFFFFF"),
                18,
                FontWeight.SemiBold,
                HorizontalAlignment.Stretch,
                CenterTopicPlaceholder,
                IsTextOnly: false);
        }

        if (level == 2)
        {
            var accent = GetNodeAccentColor(node, "#2563EB");
            return new NodeMetrics(
                MindMapLayoutMetrics.BranchMinWidth,
                MindMapLayoutMetrics.BranchMaxWidth,
                MindMapLayoutMetrics.BranchMinHeight,
                new CornerRadius(8),
                CreateBranchBackgroundBrush(accent),
                CreateAccentBrush(accent, IsDarkTheme ? 0.5 : 0.34),
                new Thickness(1),
                new Thickness(12, 5),
                BoxShadows.Parse(IsDarkTheme ? "0 4 12 0 #30000000" : "0 3 10 0 #10000000"),
                GetPrimaryTextBrush(),
                17,
                FontWeight.SemiBold,
                HorizontalAlignment.Stretch,
                TopicPlaceholder,
                IsTextOnly: false,
                HoverBackground: CreateBranchBackgroundBrush(accent, isHover: true),
                HoverBorderBrush: CreateAccentBrush(accent, IsDarkTheme ? 0.72 : 0.48),
                HoverBoxShadow: BoxShadows.Parse(IsDarkTheme ? "0 7 18 0 #42000000" : "0 7 18 0 #16000000"),
                SelectedBackground: CreateBranchBackgroundBrush(accent, isSelected: true),
                SelectedBorderBrush: CreateAccentBrush(accent, IsDarkTheme ? 0.95 : 0.72),
                SelectedBorderThickness: new Thickness(1),
                SelectedBoxShadow: BoxShadows.Parse(IsDarkTheme ? "0 8 22 0 #4A000000" : "0 8 22 0 #1C000000"),
                DragBoxShadow: BoxShadows.Parse(IsDarkTheme ? "0 12 28 0 #58000000" : "0 12 28 0 #22000000"));
        }

        return new NodeMetrics(
            MindMapLayoutMetrics.LeafMinWidth,
            MindMapLayoutMetrics.LeafMaxWidth,
            MindMapLayoutMetrics.LeafMinHeight,
            new CornerRadius(0),
            Brushes.Transparent,
            Brushes.Transparent,
            new Thickness(0),
            new Thickness(0, 2),
            default,
            GetPrimaryTextBrush(),
            16,
            FontWeight.Regular,
            HorizontalAlignment.Stretch,
            TopicPlaceholder,
            IsTextOnly: true,
            HoverBackground: Brush.Parse(IsDarkTheme ? "#162235" : "#EEF6FF"),
            SelectedBackground: Brush.Parse(IsDarkTheme ? "#1E2F46" : "#E6F2FF"));
    }

    private static Geometry CreateConnectorGeometry(Point start, Point end)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        var distance = Math.Max(32, end.X - start.X);
        var controlOffset = distance * 0.55;
        context.BeginFigure(start, isFilled: false);
        context.CubicBezierTo(
            new Point(start.X + controlOffset, start.Y),
            new Point(end.X - controlOffset, end.Y),
            end);
        return geometry;
    }

    private sealed record Connector(MindMapNode Parent, MindMapNode Child, Avalonia.Controls.Shapes.Path Path);

    private sealed record ConnectorWorkItem(MindMapNode Parent, MindMapNode Child);

    private sealed record NodeMetrics(
        double MinWidth,
        double MaxWidth,
        double MinHeight,
        CornerRadius CornerRadius,
        IBrush Background,
        IBrush BorderBrush,
        Thickness BorderThickness,
        Thickness Padding,
        BoxShadows BoxShadow,
        IBrush Foreground,
        double FontSize,
        FontWeight FontWeight,
        HorizontalAlignment ContentAlignment,
        string Placeholder,
        bool IsTextOnly,
        IBrush? HoverBackground = null,
        IBrush? HoverBorderBrush = null,
        BoxShadows? HoverBoxShadow = null,
        IBrush? SelectedBackground = null,
        IBrush? SelectedBorderBrush = null,
        Thickness? SelectedBorderThickness = null,
        BoxShadows? SelectedBoxShadow = null,
        BoxShadows? DragBoxShadow = null);

    private void ApplyTheme()
    {
        var canvasBrush = GetCanvasBackgroundBrush();
        _canvas.Background = canvasBrush;
        _scrollViewer.Background = canvasBrush;
    }

    private IBrush GetCanvasBackgroundBrush()
    {
        return GetResourceBrush(MindViewStyleKeys.CanvasBackgroundBrushResource, "#F8FAFC", "#111827");
    }

    private IBrush GetConnectorBrush()
    {
        return GetResourceBrush(MindViewStyleKeys.ConnectorBrushResource, "#148BFF", "#60A5FA");
    }

    private IBrush GetSelectionBrush()
    {
        return GetResourceBrush(MindViewStyleKeys.SelectionBrushResource, "#148BFF", "#60A5FA");
    }

    private IBrush GetPanelBackgroundBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#1F2937" : "#F8FAFC");
    }

    private IBrush GetPanelBorderBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#374151" : "#CBD5E1");
    }

    private IBrush GetPrimaryTextBrush()
    {
        return GetResourceBrush(MindViewStyleKeys.PrimaryTextBrushResource, "#111827", "#F9FAFB");
    }

    private IBrush GetSecondaryTextBrush()
    {
        return GetResourceBrush(MindViewStyleKeys.SecondaryTextBrushResource, "#6B7280", "#9CA3AF");
    }

    private IBrush CreateBranchBackgroundBrush(Color accent, bool isHover = false, bool isSelected = false)
    {
        var target = Color.Parse(IsDarkTheme ? "#111827" : "#FFFFFF");
        var targetWeight = IsDarkTheme
            ? isSelected ? 0.66 : isHover ? 0.7 : 0.76
            : isSelected ? 0.82 : isHover ? 0.86 : 0.91;
        return new SolidColorBrush(MixColor(accent, target, targetWeight));
    }

    private static IBrush CreateAccentBrush(Color accent, double opacity)
    {
        return new SolidColorBrush(accent, opacity);
    }

    private static Color GetNodeAccentColor(MindMapNode node, string fallback)
    {
        try
        {
            return Color.Parse(string.IsNullOrWhiteSpace(node.AccentColor) ? fallback : node.AccentColor);
        }
        catch (FormatException)
        {
            return Color.Parse(fallback);
        }
    }

    private static Color MixColor(Color source, Color target, double targetWeight)
    {
        targetWeight = Math.Clamp(targetWeight, 0, 1);
        var sourceWeight = 1 - targetWeight;
        return Color.FromArgb(
            MixByte(source.A, target.A, sourceWeight, targetWeight),
            MixByte(source.R, target.R, sourceWeight, targetWeight),
            MixByte(source.G, target.G, sourceWeight, targetWeight),
            MixByte(source.B, target.B, sourceWeight, targetWeight));
    }

    private static byte MixByte(byte source, byte target, double sourceWeight, double targetWeight)
    {
        return (byte)Math.Round(source * sourceWeight + target * targetWeight);
    }

    private static TextAlignment ToTextAlignment(HorizontalAlignment alignment)
    {
        return alignment switch
        {
            HorizontalAlignment.Center => TextAlignment.Center,
            HorizontalAlignment.Right => TextAlignment.Right,
            _ => TextAlignment.Left
        };
    }

    private IBrush GetResourceBrush(string key, string lightFallback, string darkFallback)
    {
        if (TryGetResource(key, ActualThemeVariant, out var value) && value is IBrush brush)
        {
            return brush;
        }

        return Brush.Parse(IsDarkTheme ? darkFallback : lightFallback);
    }
}

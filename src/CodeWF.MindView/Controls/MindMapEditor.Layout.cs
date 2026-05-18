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
                "中心主题",
                IsTextOnly: false);
        }

        if (level == 2)
        {
            return new NodeMetrics(
                MindMapLayoutMetrics.BranchMinWidth,
                MindMapLayoutMetrics.BranchMaxWidth,
                MindMapLayoutMetrics.BranchMinHeight,
                new CornerRadius(8),
                GetNodeAccentBrush(node, "#2563EB"),
                Brushes.Transparent,
                new Thickness(0),
                new Thickness(12, 5),
                BoxShadows.Parse(IsDarkTheme ? "0 4 14 0 #2A000000" : "0 5 16 0 #18000000"),
                Brushes.White,
                17,
                FontWeight.Medium,
                HorizontalAlignment.Stretch,
                "主题",
                IsTextOnly: false);
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
            "主题",
            IsTextOnly: true);
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
        bool IsTextOnly);

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

    private static IBrush GetNodeAccentBrush(MindMapNode node, string fallback)
    {
        try
        {
            return Brush.Parse(string.IsNullOrWhiteSpace(node.AccentColor) ? fallback : node.AccentColor);
        }
        catch (FormatException)
        {
            return Brush.Parse(fallback);
        }
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

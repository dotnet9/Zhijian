using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Zhijian.Models;

namespace Zhijian.Views;

public class MindMapMiniMap : Control
{
    public static readonly StyledProperty<ObservableCollection<MindMapNode>?> RootsProperty =
        AvaloniaProperty.Register<MindMapMiniMap, ObservableCollection<MindMapNode>?>(nameof(Roots));

    public static readonly StyledProperty<bool> IsDarkThemeProperty =
        AvaloniaProperty.Register<MindMapMiniMap, bool>(nameof(IsDarkTheme));

    public static readonly StyledProperty<Rect> ViewportBoundsProperty =
        AvaloniaProperty.Register<MindMapMiniMap, Rect>(nameof(ViewportBounds));

    private const double PreviewPadding = 14;
    private const double NodeWidth = 54;
    private const double NodeHeight = 15;

    private readonly List<MindMapNode> _observedNodes = [];
    private readonly List<INotifyCollectionChanged> _observedCollections = [];
    private Func<Point, Point>? _toCanvasPoint;

    public event EventHandler<Point>? MapPointRequested;

    public MindMapMiniMap()
    {
        ClipToBounds = true;
    }

    public ObservableCollection<MindMapNode>? Roots
    {
        get => GetValue(RootsProperty);
        set => SetValue(RootsProperty, value);
    }

    public bool IsDarkTheme
    {
        get => GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
    }

    public Rect ViewportBounds
    {
        get => GetValue(ViewportBoundsProperty);
        set => SetValue(ViewportBoundsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == RootsProperty)
        {
            RewireTree();
        }

        if (change.Property == RootsProperty
            || change.Property == IsDarkThemeProperty
            || change.Property == ViewportBoundsProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || _toCanvasPoint is null)
        {
            return;
        }

        MapPointRequested?.Invoke(this, _toCanvasPoint(e.GetPosition(this)));
        e.Handled = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var background = Brush.Parse(IsDarkTheme ? "#0F172A" : "#F8FAFC");
        var border = new Pen(Brush.Parse(IsDarkTheme ? "#334155" : "#D8E0EA"), 1);
        context.DrawRectangle(background, border, new RoundedRect(Bounds, 6));

        var nodes = FlattenNodes();
        if (nodes.Count == 0)
        {
            _toCanvasPoint = null;
            return;
        }

        var mapBounds = GetMapBounds(nodes);
        var scale = Math.Min(
            Math.Max(0.01, (Bounds.Width - PreviewPadding * 2) / mapBounds.Width),
            Math.Max(0.01, (Bounds.Height - PreviewPadding * 2) / mapBounds.Height));

        var contentWidth = mapBounds.Width * scale;
        var contentHeight = mapBounds.Height * scale;
        var offset = new Vector(
            (Bounds.Width - contentWidth) / 2 - mapBounds.X * scale,
            (Bounds.Height - contentHeight) / 2 - mapBounds.Y * scale);

        Point ToPreview(Point point) => new(point.X * scale + offset.X, point.Y * scale + offset.Y);
        _toCanvasPoint = point => new Point((point.X - offset.X) / scale, (point.Y - offset.Y) / scale);

        if (Roots is not null)
        {
            foreach (var root in Roots)
            {
                DrawConnectors(context, root, ToPreview);
            }
        }

        foreach (var node in nodes)
        {
            DrawNode(context, node, ToPreview);
        }

        if (ViewportBounds.Width > 0 && ViewportBounds.Height > 0)
        {
            var topLeft = ToPreview(ViewportBounds.Position);
            var bottomRight = ToPreview(ViewportBounds.BottomRight);
            var rect = new Rect(topLeft, bottomRight).Intersect(Bounds.Deflate(4));
            if (rect.Width > 2 && rect.Height > 2)
            {
                context.DrawRectangle(
                    Brush.Parse(IsDarkTheme ? "#1D4ED826" : "#148BFF22"),
                    new Pen(Brush.Parse("#148BFF"), 1.2),
                    rect);
            }
        }
    }

    private void RewireTree()
    {
        foreach (var node in _observedNodes)
        {
            node.PropertyChanged -= HandleNodeChanged;
        }

        foreach (var collection in _observedCollections)
        {
            collection.CollectionChanged -= HandleTreeChanged;
        }

        _observedNodes.Clear();
        _observedCollections.Clear();

        if (Roots is null)
        {
            return;
        }

        Roots.CollectionChanged += HandleTreeChanged;
        _observedCollections.Add(Roots);

        foreach (var root in Roots)
        {
            WatchNode(root);
        }
    }

    private void WatchNode(MindMapNode node)
    {
        _observedNodes.Add(node);
        node.PropertyChanged += HandleNodeChanged;
        node.Children.CollectionChanged += HandleTreeChanged;
        _observedCollections.Add(node.Children);

        foreach (var child in node.Children)
        {
            WatchNode(child);
        }
    }

    private void HandleNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MindMapNode.Title)
            or nameof(MindMapNode.X)
            or nameof(MindMapNode.Y)
            or nameof(MindMapNode.AccentColor))
        {
            InvalidateVisual();
        }
    }

    private void HandleTreeChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RewireTree();
        InvalidateVisual();
    }

    private List<MindMapNode> FlattenNodes()
    {
        var nodes = new List<MindMapNode>();
        if (Roots is null)
        {
            return nodes;
        }

        foreach (var root in Roots)
        {
            AppendNode(root, nodes);
        }

        return nodes;
    }

    private static void AppendNode(MindMapNode node, List<MindMapNode> nodes)
    {
        nodes.Add(node);
        foreach (var child in node.Children)
        {
            AppendNode(child, nodes);
        }
    }

    private static Rect GetMapBounds(IReadOnlyCollection<MindMapNode> nodes)
    {
        var minX = nodes.Min(node => node.X);
        var minY = nodes.Min(node => node.Y);
        var maxX = nodes.Max(node => node.X + NodeWidth);
        var maxY = nodes.Max(node => node.Y + NodeHeight);
        return new Rect(minX - 40, minY - 32, maxX - minX + 80, maxY - minY + 64);
    }

    private void DrawConnectors(DrawingContext context, MindMapNode parent, Func<Point, Point> toPreview)
    {
        var pen = new Pen(Brush.Parse(IsDarkTheme ? "#60A5FA" : "#148BFF"), 1);
        foreach (var child in parent.Children)
        {
            var start = toPreview(new Point(parent.X + NodeWidth, parent.Y + NodeHeight / 2));
            var end = toPreview(new Point(child.X, child.Y + NodeHeight / 2));
            var geometry = new StreamGeometry();
            using (var stream = geometry.Open())
            {
                var distance = Math.Max(14, end.X - start.X);
                var controlOffset = distance * 0.55;
                stream.BeginFigure(start, isFilled: false);
                stream.CubicBezierTo(
                    new Point(start.X + controlOffset, start.Y),
                    new Point(end.X - controlOffset, end.Y),
                    end);
            }

            context.DrawGeometry(null, pen, geometry);
            DrawConnectors(context, child, toPreview);
        }
    }

    private void DrawNode(DrawingContext context, MindMapNode node, Func<Point, Point> toPreview)
    {
        var point = toPreview(new Point(node.X, node.Y));
        var rect = new Rect(point.X, point.Y, NodeWidth, NodeHeight);
        var fill = Brush.Parse(string.IsNullOrWhiteSpace(node.AccentColor)
            ? "#148BFF"
            : node.AccentColor);
        var opacityBrush = new SolidColorBrush(((ISolidColorBrush)fill).Color, IsDarkTheme ? 0.75 : 0.9);
        context.DrawRectangle(opacityBrush, null, new RoundedRect(rect, 4));
    }
}

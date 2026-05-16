using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Zhijian.Desktop.Models;
using Zhijian.Desktop.ViewModels;
using AtomButton = AtomUI.Desktop.Controls.Button;

namespace Zhijian.Desktop.Views;

public class MindMapEditor : UserControl
{
    public static readonly StyledProperty<ObservableCollection<MindMapNode>?> RootsProperty =
        AvaloniaProperty.Register<MindMapEditor, ObservableCollection<MindMapNode>?>(nameof(Roots));

    public static readonly StyledProperty<MindMapNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<MindMapEditor, MindMapNode?>(
            nameof(SelectedNode),
            defaultBindingMode: BindingMode.TwoWay);

    private const double RootWidth = 72;
    private const double RootMinHeight = 46;
    private const double BranchWidth = 112;
    private const double BranchMinHeight = 42;
    private const double LeafWidth = 190;
    private const double LeafMinHeight = 30;
    private const double MinCanvasWidth = 920;
    private const double MinCanvasHeight = 620;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 2.0;
    private const double ZoomFactor = 1.1;

    private readonly Canvas _canvas = new()
    {
        Background = Brush.Parse("#F8FAFC"),
        Cursor = new Cursor(StandardCursorType.Hand),
        MinWidth = MinCanvasWidth,
        MinHeight = MinCanvasHeight
    };
    private readonly LayoutTransformControl _zoomHost;
    private readonly ScrollViewer _scrollViewer;
    private readonly TextBlock _zoomText = new()
    {
        Width = 48,
        TextAlignment = TextAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 12,
        Foreground = Brush.Parse("#334155")
    };

    private readonly Dictionary<MindMapNode, Border> _nodeFrames = [];
    private readonly Dictionary<MindMapNode, TextBox> _titleEditors = [];
    private readonly List<Connector> _connectors = [];
    private readonly List<MindMapNode> _observedNodes = [];
    private readonly List<INotifyCollectionChanged> _observedCollections = [];

    private MindMapNode? _dragNode;
    private Point _dragStartPointer;
    private Point _dragStartNode;
    private bool _isPanningCanvas;
    private bool _isSpacePressed;
    private Point _panStartPointer;
    private Vector _panStartOffset;
    private double _zoomScale = 1;

    public MindMapEditor()
    {
        Focusable = true;
        _zoomHost = new LayoutTransformControl
        {
            Child = _canvas,
            LayoutTransform = CreateZoomTransform(_zoomScale)
        };

        _scrollViewer = new ScrollViewer
        {
            Content = _zoomHost,
            Background = _canvas.Background,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var viewport = new Grid();
        viewport.Children.Add(_scrollViewer);

        var zoomControls = CreateZoomControls();
        viewport.Children.Add(zoomControls);

        Content = viewport;
        UpdateZoomText();
        AddHandler(PointerPressedEvent, HandleCanvasPanStarted, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, HandleCanvasPanned, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, HandleCanvasPanCompleted, RoutingStrategies.Tunnel, handledEventsToo: true);
        _scrollViewer.AddHandler(PointerPressedEvent, HandleCanvasPanStarted, RoutingStrategies.Tunnel, handledEventsToo: true);
        _scrollViewer.AddHandler(PointerMovedEvent, HandleCanvasPanned, RoutingStrategies.Tunnel, handledEventsToo: true);
        _scrollViewer.AddHandler(PointerReleasedEvent, HandleCanvasPanCompleted, RoutingStrategies.Tunnel, handledEventsToo: true);
        _scrollViewer.PointerCaptureLost += (_, _) => StopCanvasPan();
        AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyUpEvent, HandleKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerWheelChangedEvent, HandlePointerWheelChanged, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    public ObservableCollection<MindMapNode>? Roots
    {
        get => GetValue(RootsProperty);
        set => SetValue(RootsProperty, value);
    }

    public MindMapNode? SelectedNode
    {
        get => GetValue(SelectedNodeProperty);
        set => SetValue(SelectedNodeProperty, value);
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == RootsProperty)
        {
            Rebuild();
        }
        else if (change.Property == SelectedNodeProperty)
        {
            ApplySelectionState();
        }
    }

    private void Rebuild()
    {
        DetachTreeSubscriptions();

        _canvas.Children.Clear();
        _nodeFrames.Clear();
        _titleEditors.Clear();
        _connectors.Clear();

        if (Roots is null)
        {
            return;
        }

        Roots.CollectionChanged += HandleTreeStructureChanged;
        _observedCollections.Add(Roots);

        foreach (var root in Roots)
        {
            WatchNode(root);
            AddConnectors(root);
        }

        foreach (var root in Roots)
        {
            AddNodeVisuals(root);
        }

        UpdateConnectors();
        ApplySelectionState();
        EnsureCanvasSize();
    }

    private void DetachTreeSubscriptions()
    {
        foreach (var node in _observedNodes)
        {
            node.PropertyChanged -= HandleNodePropertyChanged;
        }

        foreach (var collection in _observedCollections)
        {
            collection.CollectionChanged -= HandleTreeStructureChanged;
        }

        _observedNodes.Clear();
        _observedCollections.Clear();
    }

    private void WatchNode(MindMapNode node)
    {
        _observedNodes.Add(node);
        node.PropertyChanged += HandleNodePropertyChanged;
        node.Children.CollectionChanged += HandleTreeStructureChanged;
        _observedCollections.Add(node.Children);

        foreach (var child in node.Children)
        {
            WatchNode(child);
        }
    }

    private void AddConnectors(MindMapNode parent)
    {
        foreach (var child in parent.Children)
        {
            var path = new Avalonia.Controls.Shapes.Path
            {
                Stroke = Brush.Parse("#148BFF"),
                StrokeThickness = 2,
                IsHitTestVisible = false
            };

            _canvas.Children.Add(path);
            _connectors.Add(new Connector(parent, child, path));
            AddConnectors(child);
        }
    }

    private void AddNodeVisuals(MindMapNode node)
    {
        var nodeVisual = CreateNodeVisual(node);
        _nodeFrames[node] = nodeVisual;
        _canvas.Children.Add(nodeVisual);
        UpdateNodePosition(node);

        foreach (var child in node.Children)
        {
            AddNodeVisuals(child);
        }
    }

    private Border CreateNodeVisual(MindMapNode node)
    {
        var metrics = GetNodeMetrics(node);
        var root = new Border
        {
            Width = metrics.Width,
            MinHeight = metrics.MinHeight,
            CornerRadius = metrics.CornerRadius,
            Background = metrics.Background,
            BorderBrush = metrics.BorderBrush,
            BorderThickness = metrics.BorderThickness,
            Padding = metrics.Padding,
            BoxShadow = metrics.BoxShadow,
            DataContext = node,
            Focusable = true,
            Transitions = new Transitions
            {
                new BrushTransition { Property = Border.BorderBrushProperty, Duration = TimeSpan.FromMilliseconds(160) },
                new ThicknessTransition { Property = Border.BorderThicknessProperty, Duration = TimeSpan.FromMilliseconds(120) },
                new BoxShadowsTransition { Property = Border.BoxShadowProperty, Duration = TimeSpan.FromMilliseconds(180) }
            }
        };

        var titleBox = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = metrics.Foreground,
            FontSize = metrics.FontSize,
            FontWeight = metrics.FontWeight,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = false,
            PlaceholderText = metrics.Placeholder,
            HorizontalContentAlignment = metrics.TextAlignment,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinHeight = metrics.MinHeight - metrics.Padding.Top - metrics.Padding.Bottom,
            Padding = new Thickness(0)
        };
        titleBox.Bind(TextBox.TextProperty, new Binding(nameof(MindMapNode.Title))
        {
            Source = node,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        titleBox.GotFocus += (_, _) => SelectNode(node);
        titleBox.AddHandler(
            KeyDownEvent,
            (sender, e) => HandleTitleKeyDown(node, sender as TextBox, e),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        root.Child = titleBox;
        root.SizeChanged += (_, _) =>
        {
            UpdateConnectors();
            EnsureCanvasSize();
        };

        root.PointerPressed += (_, e) =>
        {
            if (e.Source is TextBox)
            {
                return;
            }

            SelectNode(node);
            root.Focus();
            e.Handled = true;
        };
        root.KeyDown += (_, e) => HandleFrameKeyDown(node, e);

        return root;
    }

    private void HandleTitleKeyDown(MindMapNode node, TextBox? editor, KeyEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            var nextNode = viewModel.HandleMapEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            var nextNode = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? node
                : viewModel.HandleMapTab(node);

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                viewModel.PromoteNode(node);
            }

            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if ((e.Key == Key.Delete || e.Key == Key.Back)
            && string.IsNullOrWhiteSpace(editor?.Text)
            && !viewModel.IsRoot(node))
        {
            var focusTarget = viewModel.DeleteNode(node);
            FocusNode(focusTarget);
            e.Handled = true;
        }
    }

    private void HandleFrameKeyDown(MindMapNode node, KeyEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            var focusTarget = viewModel.DeleteNode(node);
            FocusFrame(focusTarget);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            var nextNode = viewModel.HandleMapEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            var nextNode = viewModel.HandleMapTab(node);
            FocusNode(nextNode);
            e.Handled = true;
        }
    }

    private void HandleNodeDragStarted(MindMapNode node, Control? control, PointerPressedEventArgs e)
    {
        if (control is null || !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        SelectNode(node);
        _dragNode = node;
        _dragStartPointer = e.GetPosition(_canvas);
        _dragStartNode = new Point(node.X, node.Y);
        e.Pointer.Capture(control);
        e.Handled = true;
    }

    private void HandleNodeDragged(object? sender, PointerEventArgs e)
    {
        if (_dragNode is null)
        {
            return;
        }

        var current = e.GetPosition(_canvas);
        var delta = current - _dragStartPointer;

        _dragNode.X = Math.Max(12, _dragStartNode.X + delta.X);
        _dragNode.Y = Math.Max(12, _dragStartNode.Y + delta.Y);

        UpdateNodePosition(_dragNode);
        UpdateConnectors();
        EnsureCanvasSize();
        e.Handled = true;
    }

    private void HandleNodeDragCompleted(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragNode is null)
        {
            return;
        }

        _dragNode = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void HandleCanvasPanStarted(object? sender, PointerPressedEventArgs e)
    {
        if (_isPanningCanvas
            || _dragNode is not null
            || !_isSpacePressed
            || !IsCanvasPanSource(e.Source)
            || !e.GetCurrentPoint(_scrollViewer).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isPanningCanvas = true;
        _panStartPointer = e.GetPosition(_scrollViewer);
        _panStartOffset = _scrollViewer.Offset;
        _canvas.Cursor = new Cursor(StandardCursorType.SizeAll);
        _scrollViewer.Cursor = new Cursor(StandardCursorType.SizeAll);
        e.Pointer.Capture(_scrollViewer);
        e.Handled = true;
    }

    private void HandleCanvasPanned(object? sender, PointerEventArgs e)
    {
        if (!_isPanningCanvas)
        {
            return;
        }

        var current = e.GetPosition(_scrollViewer);
        var delta = current - _panStartPointer;
        _scrollViewer.Offset = ClampScrollOffset(new Vector(
            _panStartOffset.X - delta.X,
            _panStartOffset.Y - delta.Y));
        e.Handled = true;
    }

    private void HandleCanvasPanCompleted(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPanningCanvas)
        {
            return;
        }

        StopCanvasPan();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void StopCanvasPan()
    {
        _isPanningCanvas = false;
        _canvas.Cursor = new Cursor(StandardCursorType.Hand);
        _scrollViewer.Cursor = Cursor.Default;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space || e.Source is TextBox)
        {
            return;
        }

        _isSpacePressed = true;
    }

    private void HandleKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePressed = false;
        }
    }

    private void HandlePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }

        if (Math.Abs(e.Delta.Y) < double.Epsilon)
        {
            return;
        }

        var factor = e.Delta.Y > 0 ? ZoomFactor : 1 / ZoomFactor;
        SetZoom(_zoomScale * factor);
        e.Handled = true;
    }

    private void SetZoom(double zoom)
    {
        _zoomScale = Math.Clamp(zoom, MinZoom, MaxZoom);
        _zoomHost.LayoutTransform = CreateZoomTransform(_zoomScale);
        _zoomHost.InvalidateMeasure();
        UpdateZoomText();
    }

    private static ScaleTransform CreateZoomTransform(double zoom)
    {
        return new ScaleTransform(zoom, zoom);
    }

    private Border CreateZoomControls()
    {
        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };
        controls.Children.Add(CreateZoomButton("-", () => SetZoom(_zoomScale / ZoomFactor), "缩小"));
        controls.Children.Add(_zoomText);
        controls.Children.Add(CreateZoomButton("100%", () => SetZoom(1), "重置缩放"));
        controls.Children.Add(CreateZoomButton("+", () => SetZoom(_zoomScale * ZoomFactor), "放大"));

        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 14, 14),
            Padding = new Thickness(8, 6),
            Background = Brush.Parse("#F8FAFC"),
            BorderBrush = Brush.Parse("#CBD5E1"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            BoxShadow = BoxShadows.Parse("0 6 18 0 #16000000"),
            Child = controls
        };
    }

    private Vector ClampScrollOffset(Vector offset)
    {
        var maxX = Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);
        return new Vector(
            Math.Clamp(offset.X, 0, maxX),
            Math.Clamp(offset.Y, 0, maxY));
    }

    private bool IsCanvasPanSource(object? source)
    {
        if (source is not Visual visual)
        {
            return false;
        }

        for (var current = visual; current is not null; current = current.GetVisualParent())
        {
            if (current is TextBox or Button or ScrollBar or Thumb)
            {
                return false;
            }

            if (_nodeFrames.Values.Contains(current))
            {
                return false;
            }

            if (ReferenceEquals(current, _scrollViewer))
            {
                return true;
            }
        }

        return true;
    }

    private static AtomButton CreateZoomButton(string content, Action action, string tooltip)
    {
        var button = new AtomButton
        {
            Content = content,
            MinWidth = 34,
            Height = 28,
            Padding = new Thickness(8, 2),
            FontSize = 12
        };
        ToolTip.SetTip(button, tooltip);
        AutomationProperties.SetName(button, tooltip);
        button.Click += (_, _) => action();
        return button;
    }

    private void UpdateZoomText()
    {
        _zoomText.Text = $"{_zoomScale:P0}";
    }

    private void HandleNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MindMapNode node)
        {
            return;
        }

        if (e.PropertyName is nameof(MindMapNode.X) or nameof(MindMapNode.Y))
        {
            UpdateNodePosition(node);
            UpdateConnectors();
            EnsureCanvasSize();
        }
        else if (e.PropertyName == nameof(MindMapNode.AccentColor))
        {
            Rebuild();
        }
    }

    private void HandleTreeStructureChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Rebuild();
    }

    private void SelectNode(MindMapNode node)
    {
        SetCurrentValue(SelectedNodeProperty, node);
        ApplySelectionState();
    }

    private void FocusNode(MindMapNode? node)
    {
        if (node is null)
        {
            return;
        }

        SetCurrentValue(SelectedNodeProperty, node);
        Dispatcher.UIThread.Post(() =>
        {
            if (_titleEditors.TryGetValue(node, out var editor))
            {
                editor.Focus();
                editor.CaretIndex = editor.Text?.Length ?? 0;
            }
        });
    }

    private void FocusFrame(MindMapNode? node)
    {
        if (node is null)
        {
            return;
        }

        SetCurrentValue(SelectedNodeProperty, node);
        Dispatcher.UIThread.Post(() =>
        {
            if (_nodeFrames.TryGetValue(node, out var frame))
            {
                frame.Focus();
            }
        });
    }

    private void ApplySelectionState()
    {
        foreach (var (node, frame) in _nodeFrames)
        {
            var metrics = GetNodeMetrics(node);
            var selected = ReferenceEquals(node, SelectedNode);
            frame.BorderBrush = selected ? Brush.Parse("#148BFF") : metrics.BorderBrush;
            frame.BorderThickness = selected
                ? new Thickness(metrics.IsTextOnly ? 0 : 2)
                : metrics.BorderThickness;
            frame.BoxShadow = selected && !metrics.IsTextOnly
                ? BoxShadows.Parse("0 6 18 0 #18000000")
                : metrics.BoxShadow;
        }
    }

    private void UpdateNodePosition(MindMapNode node)
    {
        if (!_nodeFrames.TryGetValue(node, out var control))
        {
            return;
        }

        Canvas.SetLeft(control, node.X);
        Canvas.SetTop(control, node.Y);
    }

    private void UpdateConnectors()
    {
        foreach (var connector in _connectors)
        {
            var parentSize = GetRenderedNodeSize(connector.Parent);
            var childSize = GetRenderedNodeSize(connector.Child);
            var start = new Point(
                connector.Parent.X + parentSize.Width,
                connector.Parent.Y + parentSize.Height / 2);
            var end = new Point(
                connector.Child.X,
                connector.Child.Y + childSize.Height / 2);

            connector.Path.Data = CreateConnectorGeometry(start, end);
        }
    }

    private void EnsureCanvasSize()
    {
        var nodes = _nodeFrames.Keys.ToList();
        var width = nodes.Count == 0
            ? MinCanvasWidth
            : nodes.Max(node => node.X + GetRenderedNodeSize(node).Width + 120);
        var height = nodes.Count == 0
            ? MinCanvasHeight
            : nodes.Max(node => node.Y + GetRenderedNodeSize(node).Height + 120);

        _canvas.Width = Math.Max(MinCanvasWidth, width);
        _canvas.Height = Math.Max(MinCanvasHeight, height);
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
        return new Size(metrics.Width, metrics.MinHeight);
    }

    private NodeMetrics GetNodeMetrics(MindMapNode node)
    {
        var level = ViewModel?.GetLevel(node) ?? 1;
        if (level <= 1)
        {
            return new NodeMetrics(
                RootWidth,
                RootMinHeight,
                new CornerRadius(8),
                Brush.Parse("#148BFF"),
                Brushes.Transparent,
                new Thickness(0),
                new Thickness(10, 5),
                BoxShadows.Parse("0 6 18 0 #16000000"),
                Brushes.White,
                18,
                FontWeight.SemiBold,
                HorizontalAlignment.Center,
                "中心主题",
                IsTextOnly: false);
        }

        if (level == 2)
        {
            return new NodeMetrics(
                BranchWidth,
                BranchMinHeight,
                new CornerRadius(8),
                Brush.Parse("#EEF0F3"),
                Brushes.Transparent,
                new Thickness(0),
                new Thickness(12, 5),
                BoxShadows.Parse("0 3 10 0 #0C000000"),
                Brush.Parse("#111827"),
                17,
                FontWeight.Medium,
                HorizontalAlignment.Center,
                "主题",
                IsTextOnly: false);
        }

        return new NodeMetrics(
            LeafWidth,
            LeafMinHeight,
            new CornerRadius(0),
            Brushes.Transparent,
            Brushes.Transparent,
            new Thickness(0),
            new Thickness(0, 2),
            default,
            Brush.Parse("#111827"),
            16,
            FontWeight.Regular,
            HorizontalAlignment.Left,
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

    private sealed record NodeMetrics(
        double Width,
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
        HorizontalAlignment TextAlignment,
        string Placeholder,
        bool IsTextOnly);
}

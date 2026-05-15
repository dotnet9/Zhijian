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
using Zhijian.Desktop.Models;
using Zhijian.Desktop.ViewModels;

namespace Zhijian.Desktop.Views;

public class MindMapEditor : UserControl
{
    public static readonly StyledProperty<ObservableCollection<MindMapNode>?> RootsProperty =
        AvaloniaProperty.Register<MindMapEditor, ObservableCollection<MindMapNode>?>(nameof(Roots));

    public static readonly StyledProperty<MindMapNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<MindMapEditor, MindMapNode?>(
            nameof(SelectedNode),
            defaultBindingMode: BindingMode.TwoWay);

    private const double NodeWidth = 220;
    private const double NodeHeight = 96;
    private const double RootWidth = 240;
    private const double RootHeight = 108;
    private const double MinCanvasWidth = 1160;
    private const double MinCanvasHeight = 720;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 2.0;
    private const double ZoomFactor = 1.1;

    private readonly Canvas _canvas = new()
    {
        Background = Brush.Parse("#F8FAFC"),
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
    private double _zoomScale = 1;

    public MindMapEditor()
    {
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
            var line = new Line
            {
                Stroke = Brush.Parse("#9CB7D3"),
                StrokeThickness = 2
            };

            _canvas.Children.Add(line);
            _connectors.Add(new Connector(parent, child, line));
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
        var isRoot = ViewModel?.IsRoot(node) == true;
        var root = new Border
        {
            Width = isRoot ? RootWidth : NodeWidth,
            Height = isRoot ? RootHeight : NodeHeight,
            CornerRadius = new CornerRadius(8),
            Background = Brush.Parse("#FFFFFF"),
            BorderBrush = Brush.Parse(node.AccentColor),
            BorderThickness = new Thickness(isRoot ? 2 : 1),
            BoxShadow = BoxShadows.Parse(isRoot ? "0 8 24 0 #22000000" : "0 4 16 0 #16000000"),
            DataContext = node,
            Focusable = true,
            Transitions = new Transitions
            {
                new BrushTransition { Property = Border.BorderBrushProperty, Duration = TimeSpan.FromMilliseconds(160) },
                new ThicknessTransition { Property = Border.BorderThicknessProperty, Duration = TimeSpan.FromMilliseconds(120) },
                new BoxShadowsTransition { Property = Border.BoxShadowProperty, Duration = TimeSpan.FromMilliseconds(180) }
            }
        };

        var layout = new Grid
        {
            RowDefinitions = new RowDefinitions("10,*,24")
        };

        var dragHandle = new Border
        {
            Background = Brush.Parse(node.AccentColor),
            Cursor = new Cursor(StandardCursorType.SizeAll),
            CornerRadius = new CornerRadius(8, 8, 0, 0)
        };
        ToolTip.SetTip(dragHandle, "拖拽移动节点");
        dragHandle.PointerPressed += (sender, e) => HandleNodeDragStarted(node, sender as Control, e);
        dragHandle.PointerMoved += HandleNodeDragged;
        dragHandle.PointerReleased += HandleNodeDragCompleted;

        var titleBox = new TextBox
        {
            Margin = new Thickness(12, 4, 12, 0),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            FontSize = isRoot ? 18 : 14,
            FontWeight = isRoot ? FontWeight.SemiBold : FontWeight.Medium,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = false,
            PlaceholderText = isRoot ? "中心主题" : "主题",
            VerticalContentAlignment = VerticalAlignment.Center
        };
        titleBox.Bind(TextBox.TextProperty, new Binding(nameof(MindMapNode.Title))
        {
            Source = node,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        titleBox.GotFocus += (_, _) => SelectNode(node);
        titleBox.KeyDown += (sender, e) => HandleTitleKeyDown(node, sender as TextBox, e);

        var notePreview = new TextBlock
        {
            Margin = new Thickness(12, 0, 12, 8),
            FontSize = 11,
            Foreground = Brush.Parse("#64748B"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        notePreview.Bind(TextBlock.TextProperty, new Binding(nameof(MindMapNode.Note))
        {
            Source = node,
            Mode = BindingMode.OneWay
        });

        Grid.SetRow(titleBox, 1);
        Grid.SetRow(notePreview, 2);
        layout.Children.Add(dragHandle);
        layout.Children.Add(titleBox);
        layout.Children.Add(notePreview);
        root.Child = layout;

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
            && string.IsNullOrEmpty(editor?.Text)
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
        if (control is null)
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

    private static Button CreateZoomButton(string content, Action action, string tooltip)
    {
        var button = new Button
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
            var selected = ReferenceEquals(node, SelectedNode);
            frame.BorderBrush = Brush.Parse(selected ? node.AccentColor : "#DDE3EA");
            frame.BorderThickness = new Thickness(selected ? 2 : 1);
            frame.BoxShadow = BoxShadows.Parse(selected ? "0 8 24 0 #26000000" : "0 4 16 0 #16000000");
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
            var parentWidth = ViewModel?.IsRoot(connector.Parent) == true ? RootWidth : NodeWidth;
            var parentHeight = ViewModel?.IsRoot(connector.Parent) == true ? RootHeight : NodeHeight;
            var childHeight = ViewModel?.IsRoot(connector.Child) == true ? RootHeight : NodeHeight;

            connector.Line.StartPoint = new Point(
                connector.Parent.X + parentWidth,
                connector.Parent.Y + parentHeight / 2);
            connector.Line.EndPoint = new Point(
                connector.Child.X,
                connector.Child.Y + childHeight / 2);
        }
    }

    private void EnsureCanvasSize()
    {
        var nodes = _nodeFrames.Keys.ToList();
        var width = nodes.Count == 0
            ? MinCanvasWidth
            : nodes.Max(node => node.X + (ViewModel?.IsRoot(node) == true ? RootWidth : NodeWidth) + 180);
        var height = nodes.Count == 0
            ? MinCanvasHeight
            : nodes.Max(node => node.Y + (ViewModel?.IsRoot(node) == true ? RootHeight : NodeHeight) + 160);

        _canvas.Width = Math.Max(MinCanvasWidth, width);
        _canvas.Height = Math.Max(MinCanvasHeight, height);
    }

    private sealed record Connector(MindMapNode Parent, MindMapNode Child, Line Line);
}

using CodeWF.MindView;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CodeWF.MindView.Controls;

public class MindMapEditor : UserControl
{
    public static readonly StyledProperty<ObservableCollection<MindMapNode>?> RootsProperty =
        AvaloniaProperty.Register<MindMapEditor, ObservableCollection<MindMapNode>?>(nameof(Roots));

    public static readonly StyledProperty<MindMapNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<MindMapEditor, MindMapNode?>(
            nameof(SelectedNode),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsDarkThemeProperty =
        AvaloniaProperty.Register<MindMapEditor, bool>(nameof(IsDarkTheme));

    public static readonly StyledProperty<IMindMapEditorController?> ControllerProperty =
        AvaloniaProperty.Register<MindMapEditor, IMindMapEditorController?>(nameof(Controller));

    public static readonly DirectProperty<MindMapEditor, string> ZoomTextProperty =
        AvaloniaProperty.RegisterDirect<MindMapEditor, string>(
            nameof(ZoomText),
            editor => editor.ZoomText);

    public static readonly DirectProperty<MindMapEditor, Rect> ViewportBoundsProperty =
        AvaloniaProperty.RegisterDirect<MindMapEditor, Rect>(
            nameof(ViewportBounds),
            editor => editor.ViewportBounds);

    private const double MinCanvasWidth = 920;
    private const double MinCanvasHeight = 620;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 2.0;
    private const double ZoomFactor = 1.1;
    private const double DragStartDistance = 6;
    private const double DropEdgeRatio = 0.28;
    private const double NodeMenuWidth = 156;

    private readonly Canvas _canvas = new()
    {
        Background = Brush.Parse("#F8FAFC"),
        Cursor = new Cursor(StandardCursorType.Hand),
        MinWidth = MinCanvasWidth,
        MinHeight = MinCanvasHeight
    };
    private readonly LayoutTransformControl _zoomHost;
    private readonly ScrollViewer _scrollViewer;
    private readonly Avalonia.Controls.Shapes.Path _dropPreviewPath = new()
    {
        StrokeThickness = 2,
        StrokeDashArray = new AvaloniaList<double> { 5, 4 },
        IsHitTestVisible = false,
        IsVisible = false
    };

    private readonly Dictionary<MindMapNode, Border> _nodeFrames = [];
    private readonly Dictionary<MindMapNode, TextBox> _titleEditors = [];
    private readonly Dictionary<MindMapNode, TextBox> _noteEditors = [];
    private readonly Dictionary<MindMapNode, Border> _noteFrames = [];
    private readonly HashSet<MindMapNode> _editingNoteNodes = [];
    private readonly List<Connector> _connectors = [];
    private readonly List<MindMapNode> _observedNodes = [];
    private readonly List<INotifyCollectionChanged> _observedCollections = [];

    private MindMapNode? _dragNode;
    private Point _dragStartPointer;
    private bool _isDraggingNode;
    private MindMapNode? _dropTarget;
    private MindMapDropPlacement _dropPlacement = MindMapDropPlacement.Child;
    private bool _isPanningCanvas;
    private bool _isSpacePressed;
    private MindMapNode? _toolbarNode;
    private Point _panStartPointer;
    private Vector _panStartOffset;
    private double _zoomScale = 1;
    private string _zoomText = "100%";
    private Rect _viewportBounds;
    private TopLevel? _topLevel;
    private readonly Border _nodeToolbar;
    private readonly StackPanel _nodeMenuPanel = new()
    {
        Spacing = 2
    };
    private readonly Border _nodeMenu;

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
        _nodeToolbar = CreateNodeToolbar();
        _nodeMenu = CreateNodeMenu();
        LostFocus += (_, _) => HideNodeMenu();

        var viewport = new Grid();
        viewport.Children.Add(_scrollViewer);

        Content = viewport;
        ApplyTheme();
        UpdateZoomText();
        _scrollViewer.PropertyChanged += HandleScrollViewerPropertyChanged;
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

    public bool IsDarkTheme
    {
        get => GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
    }

    public IMindMapEditorController? Controller
    {
        get => GetValue(ControllerProperty);
        set => SetValue(ControllerProperty, value);
    }

    public string ZoomText
    {
        get => _zoomText;
        private set => SetAndRaise(ZoomTextProperty, ref _zoomText, value);
    }

    public Rect ViewportBounds
    {
        get => _viewportBounds;
        private set => SetAndRaise(ViewportBoundsProperty, ref _viewportBounds, value);
    }

    private IMindMapEditorController? ControllerContext => Controller ?? DataContext as IMindMapEditorController;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == RootsProperty)
        {
            Rebuild();
        }
        else if (change.Property == SelectedNodeProperty)
        {
            CollapseEmptyNoteEditorsExcept(SelectedNode);
            HideNodeMenu();
            if (!ReferenceEquals(_toolbarNode, SelectedNode))
            {
                _toolbarNode = null;
            }

            ApplySelectionState();
            UpdateToolbarVisibility();
        }
        else if (change.Property == IsDarkThemeProperty)
        {
            ApplyTheme();
            Rebuild();
        }
        else if (change.Property == ControllerProperty)
        {
            Rebuild();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachTopLevelKeyTracking();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachTopLevelKeyTracking();
        base.OnDetachedFromVisualTree(e);
    }

    private void AttachTopLevelKeyTracking()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (ReferenceEquals(_topLevel, topLevel))
        {
            return;
        }

        DetachTopLevelKeyTracking();
        _topLevel = topLevel;
        _topLevel?.AddHandler(KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        _topLevel?.AddHandler(KeyUpEvent, HandleKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void DetachTopLevelKeyTracking()
    {
        if (_topLevel is null)
        {
            return;
        }

        _topLevel.RemoveHandler(KeyDownEvent, HandleKeyDown);
        _topLevel.RemoveHandler(KeyUpEvent, HandleKeyUp);
        _topLevel = null;
        _isSpacePressed = false;
        StopCanvasPan();
    }

    private void Rebuild()
    {
        DetachTreeSubscriptions();

        _canvas.Children.Clear();
        _nodeFrames.Clear();
        _titleEditors.Clear();
        _noteEditors.Clear();
        _noteFrames.Clear();
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

        _canvas.Children.Add(_dropPreviewPath);
        _canvas.Children.Add(_nodeToolbar);
        _canvas.Children.Add(_nodeMenu);
        HideDropPreview();
        HideNodeMenu();
        UpdateToolbarVisibility();
        UpdateConnectors();
        ApplySelectionState();
        EnsureCanvasSize();
        UpdateViewportBounds();
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
                Stroke = GetConnectorBrush(),
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
            MinWidth = metrics.MinWidth,
            MaxWidth = metrics.MaxWidth,
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

        var isRoot = ControllerContext?.IsRoot(node) == true;
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
            PlaceholderForeground = GetSecondaryTextBrush(),
            FocusAdorner = null,
            HorizontalContentAlignment = metrics.TextAlignment,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinWidth = Math.Max(12, metrics.MinWidth - metrics.Padding.Left - metrics.Padding.Right),
            MaxWidth = Math.Max(12, metrics.MaxWidth - metrics.Padding.Left - metrics.Padding.Right),
            MinHeight = metrics.MinHeight - metrics.Padding.Top - metrics.Padding.Bottom,
            Padding = new Thickness(0)
        };
        titleBox.Classes.Add("codewfMindMapTitleEditor");
        titleBox.Bind(TextBox.TextProperty, new Binding(nameof(MindMapNode.Title))
        {
            Source = node,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        titleBox.GotFocus += (_, _) =>
        {
            SelectNode(node);
            ShowNodeToolbar(node);
        };
        titleBox.AddHandler(
            KeyDownEvent,
            (sender, e) => HandleTitleKeyDown(node, sender as TextBox, e),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        var titleHost = CreateTitleHost(node, titleBox, isRoot);
        var noteBox = CreateNoteEditor(node, metrics);
        var noteFrame = new Border
        {
            Margin = new Thickness(0, MindMapLayoutMetrics.NoteVerticalSpacing, 0, 0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Child = noteBox
        };

        var content = new StackPanel
        {
            Spacing = 0
        };
        content.Children.Add(titleHost);
        content.Children.Add(noteFrame);
        root.Child = content;
        _noteEditors[node] = noteBox;
        _noteFrames[node] = noteFrame;
        UpdateNoteEditorVisibility(node);

        root.SizeChanged += (_, _) =>
        {
            UpdateConnectors();
            EnsureCanvasSize();
        };

        root.AddHandler(
            PointerPressedEvent,
            (_, e) =>
            {
                var point = e.GetCurrentPoint(root);
                if (IsRightPointerPressed(point.Properties))
                {
                    SelectNode(node);
                    ShowNodeToolbar(node);
                    ShowNodeMenu(node, e.GetPosition(_canvas));
                    e.Handled = true;
                    return;
                }

                if (HasVisualAncestor<TextBox>(e.Source))
                {
                    HideNodeMenu();
                    return;
                }

                HideNodeMenu();
                SelectNode(node);
                ShowNodeToolbar(node);
                root.Focus();
                HandleNodeDragStarted(node, root, e);
                e.Handled = true;
            },
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        root.PointerMoved += HandleNodeDragged;
        root.PointerReleased += HandleNodeDragCompleted;
        root.KeyDown += (_, e) => HandleFrameKeyDown(node, e);

        return root;
    }

    private Control CreateTitleHost(MindMapNode node, TextBox titleBox, bool isRoot)
    {
        if (isRoot)
        {
            return titleBox;
        }

        var titleHost = new Grid();
        titleHost.Children.Add(titleBox);
        titleHost.Children.Add(CreateDragHandle(node));
        return titleHost;
    }

    private Control CreateDragHandle(MindMapNode node)
    {
        // 只保留透明命中区，不画竖线，避免拖拽入口干扰脑图视觉样式。
        var handle = new Border
        {
            Width = MindMapLayoutMetrics.DragHandleHitWidth,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.SizeAll),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        ToolTip.SetTip(handle, "拖到节点中部成为子节点，拖到上下边缘调整同级顺序");
        handle.PointerPressed += (sender, e) => HandleNodeDragStarted(node, sender as Control, e);
        handle.PointerMoved += HandleNodeDragged;
        handle.PointerReleased += HandleNodeDragCompleted;
        return handle;
    }

    private TextBox CreateNoteEditor(MindMapNode node, NodeMetrics metrics)
    {
        var noteBox = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = GetSecondaryTextBrush(),
            FontSize = MindMapLayoutMetrics.NoteFontSize,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            PlaceholderText = "备注",
            PlaceholderForeground = GetSecondaryTextBrush(),
            FocusAdorner = null,
            MinWidth = Math.Max(12, metrics.MinWidth - metrics.Padding.Left - metrics.Padding.Right),
            MaxWidth = Math.Max(12, metrics.MaxWidth - metrics.Padding.Left - metrics.Padding.Right),
            MinHeight = MindMapLayoutMetrics.NoteMinHeight,
            MaxHeight = 96,
            Padding = new Thickness(0, 2, 0, 0),
            VerticalContentAlignment = VerticalAlignment.Top
        };
        noteBox.Classes.Add("codewfMindMapTitleEditor");
        noteBox.Bind(TextBox.TextProperty, new Binding(nameof(MindMapNode.Note))
        {
            Source = node,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        noteBox.GotFocus += (_, _) =>
        {
            SelectNode(node);
            ShowNodeToolbar(node);
        };
        noteBox.LostFocus += (_, _) => CollapseEmptyNoteEditor(node);
        noteBox.AddHandler(
            KeyDownEvent,
            (sender, e) => HandleNoteKeyDown(node, sender as TextBox, e),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        return noteBox;
    }

    private Border CreateNodeToolbar()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2
        };
        panel.Children.Add(CreateToolbarButton(
            "添加同级主题",
            Geometry.Parse("M4 7h6v6H4zM14 7h6v6h-6zM10 10h4M7 13v4h10v-4"),
            AddToolbarSiblingNode));
        panel.Children.Add(CreateToolbarButton(
            "添加子主题",
            Geometry.Parse("M5 5h7v7H5zM12 8h5v4M17 12h2v7h-7v-7h5"),
            AddToolbarChildNode));
        panel.Children.Add(CreateToolbarButton(
            "提升为父节点",
            Geometry.Parse("M5 7h14M5 12h9M5 17h5M15 13l4-4-4-4"),
            PromoteToolbarNode));
        panel.Children.Add(CreateToolbarButton(
            "降级为子节点",
            Geometry.Parse("M5 7h14M10 12h9M14 17h5M9 13l-4-4 4-4"),
            DemoteToolbarNode));
        panel.Children.Add(CreateToolbarButton(
            "备注",
            Geometry.Parse("M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"),
            () =>
            {
                if (_toolbarNode is not null)
                {
                    ShowNoteEditor(_toolbarNode);
                }
            }));
        panel.Children.Add(CreateToolbarButton(
            "删除",
            Geometry.Parse("M3 6h18M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6M10 11v6M14 11v6"),
            DeleteToolbarNode));

        return new Border
        {
            Width = 188,
            Height = 32,
            Padding = new Thickness(4, 3),
            CornerRadius = new CornerRadius(6),
            Background = Brush.Parse(IsDarkTheme ? "#111827" : "#FFFFFF"),
            BorderBrush = Brush.Parse(IsDarkTheme ? "#334155" : "#D8E0EA"),
            BorderThickness = new Thickness(1),
            BoxShadow = BoxShadows.Parse("0 6 18 0 #22000000"),
            Child = panel,
            IsVisible = false
        };
    }

    private Border CreateToolbarButton(string tooltip, Geometry icon, Action action)
    {
        var path = new Avalonia.Controls.Shapes.Path
        {
            Data = icon,
            Stroke = GetSecondaryTextBrush(),
            StrokeThickness = 2,
            Fill = Brushes.Transparent,
            Stretch = Stretch.Uniform,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round
        };

        var button = new Border
        {
            Width = 30,
            Height = 24,
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Child = new Viewbox
            {
                Width = 15,
                Height = 15,
                Child = path
            }
        };
        ToolTip.SetTip(button, tooltip);
        button.PointerPressed += (_, e) =>
        {
            action();
            e.Handled = true;
        };
        return button;
    }

    private Border CreateNodeMenu()
    {
        var menu = new Border
        {
            Width = NodeMenuWidth,
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(6),
            Background = Brush.Parse(IsDarkTheme ? "#111827" : "#FFFFFF"),
            BorderBrush = Brush.Parse(IsDarkTheme ? "#334155" : "#D8E0EA"),
            BorderThickness = new Thickness(1),
            BoxShadow = BoxShadows.Parse("0 8 22 0 #24000000"),
            Child = _nodeMenuPanel,
            IsVisible = false
        };
        menu.PointerPressed += (_, e) => e.Handled = true;
        return menu;
    }

    private void ShowNodeMenu(MindMapNode node, Point canvasPoint)
    {
        var controller = ControllerContext;
        _nodeMenuPanel.Children.Clear();
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("添加子级", true, () => AddChildFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("添加同级", controller?.IsRoot(node) != true, () => AddSiblingFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("提升为父节点", controller?.CanPromoteNode(node) == true, () => PromoteNodeFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("降级为子节点", controller?.CanDemoteNode(node) == true, () => DemoteNodeFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("上移", controller?.CanMoveNodeUp(node) == true, () => MoveNodeUpFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("下移", controller?.CanMoveNodeDown(node) == true, () => MoveNodeDownFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem(string.IsNullOrWhiteSpace(node.Note) ? "添加备注" : "编辑备注", true, () => ShowNoteEditor(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("删除", controller?.IsRoot(node) != true, () => DeleteNodeFromMenu(node)));

        _nodeMenu.Background = Brush.Parse(IsDarkTheme ? "#111827" : "#FFFFFF");
        _nodeMenu.BorderBrush = Brush.Parse(IsDarkTheme ? "#334155" : "#D8E0EA");
        _nodeMenu.IsVisible = true;

        var x = Math.Clamp(canvasPoint.X, 8, Math.Max(8, _canvas.Width - NodeMenuWidth - 8));
        var y = Math.Clamp(canvasPoint.Y, 8, Math.Max(8, _canvas.Height - 270));
        Canvas.SetLeft(_nodeMenu, x);
        Canvas.SetTop(_nodeMenu, y);
    }

    private Border CreateNodeMenuItem(string header, bool isEnabled, Action action)
    {
        var text = new TextBlock
        {
            Text = header,
            FontSize = 13,
            Foreground = isEnabled
                ? GetPrimaryTextBrush()
                : Brush.Parse(IsDarkTheme ? "#64748B" : "#A0A7B1"),
            VerticalAlignment = VerticalAlignment.Center
        };

        var row = new Border
        {
            Height = 28,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 0),
            Background = Brushes.Transparent,
            Cursor = isEnabled ? new Cursor(StandardCursorType.Hand) : Cursor.Default,
            Child = text
        };
        if (isEnabled)
        {
            row.PointerEntered += (_, _) => row.Background = Brush.Parse(IsDarkTheme ? "#1F2937" : "#F1F5F9");
            row.PointerExited += (_, _) => row.Background = Brushes.Transparent;
            row.PointerPressed += (_, e) =>
            {
                HideNodeMenu();
                action();
                e.Handled = true;
            };
        }

        return row;
    }

    private void HideNodeMenu()
    {
        _nodeMenu.IsVisible = false;
    }

    private void AddToolbarChildNode()
    {
        if (_toolbarNode is null)
        {
            return;
        }

        AddChildFromMenu(_toolbarNode);
    }

    private void AddToolbarSiblingNode()
    {
        if (_toolbarNode is null || ControllerContext?.IsRoot(_toolbarNode) == true)
        {
            return;
        }

        AddSiblingFromMenu(_toolbarNode);
    }

    private void PromoteToolbarNode()
    {
        if (_toolbarNode is not null)
        {
            PromoteNodeFromMenu(_toolbarNode);
        }
    }

    private void DemoteToolbarNode()
    {
        if (_toolbarNode is not null)
        {
            DemoteNodeFromMenu(_toolbarNode);
        }
    }

    private void AddChildFromMenu(MindMapNode node)
    {
        if (ControllerContext is null)
        {
            return;
        }

        var child = ControllerContext.AddChild(node);
        _toolbarNode = child;
        FocusNode(child);
    }

    private void AddSiblingFromMenu(MindMapNode node)
    {
        var controller = ControllerContext;
        if (controller is null || controller.IsRoot(node))
        {
            return;
        }

        var sibling = controller.AddSibling(node);
        _toolbarNode = sibling;
        FocusNode(sibling);
    }

    private void PromoteNodeFromMenu(MindMapNode node)
    {
        if (ControllerContext?.PromoteNode(node) == true)
        {
            _toolbarNode = node;
            FocusNode(node);
        }
    }

    private void DemoteNodeFromMenu(MindMapNode node)
    {
        if (ControllerContext?.DemoteNode(node) == true)
        {
            _toolbarNode = node;
            FocusNode(node);
        }
    }

    private void MoveNodeUpFromMenu(MindMapNode node)
    {
        if (ControllerContext?.MoveNodeUp(node) == true)
        {
            _toolbarNode = node;
            FocusNode(node);
        }
    }

    private void MoveNodeDownFromMenu(MindMapNode node)
    {
        if (ControllerContext?.MoveNodeDown(node) == true)
        {
            _toolbarNode = node;
            FocusNode(node);
        }
    }

    private void DeleteNodeFromMenu(MindMapNode node)
    {
        var controller = ControllerContext;
        if (controller is null || controller.IsRoot(node))
        {
            return;
        }

        var focusTarget = controller.DeleteNode(node);
        _toolbarNode = null;
        UpdateToolbarVisibility();
        FocusNode(focusTarget);
    }

    private void DeleteToolbarNode()
    {
        if (_toolbarNode is null)
        {
            return;
        }

        DeleteNodeFromMenu(_toolbarNode);
    }

    private void HandleTitleKeyDown(MindMapNode node, TextBox? editor, KeyEventArgs e)
    {
        var controller = ControllerContext;
        if (controller is null)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            var nextNode = controller.HandleMapEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            var nextNode = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? node
                : controller.HandleMapTab(node);

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                controller.PromoteNode(node);
            }

            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if ((e.Key == Key.Delete || e.Key == Key.Back)
            && string.IsNullOrWhiteSpace(editor?.Text)
            && !controller.IsRoot(node))
        {
            var focusTarget = controller.DeleteNode(node);
            FocusNode(focusTarget);
            e.Handled = true;
        }
    }

    private void HandleNoteKeyDown(MindMapNode node, TextBox? editor, KeyEventArgs e)
    {
        if (e.Key is not (Key.Back or Key.Delete)
            || !string.IsNullOrWhiteSpace(editor?.Text))
        {
            return;
        }

        node.Note = string.Empty;
        _editingNoteNodes.Remove(node);
        UpdateNoteEditorVisibility(node);
        FocusNode(node);
        e.Handled = true;
    }

    private void HandleFrameKeyDown(MindMapNode node, KeyEventArgs e)
    {
        var controller = ControllerContext;
        if (controller is null)
        {
            return;
        }

        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            var focusTarget = controller.DeleteNode(node);
            FocusFrame(focusTarget);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            var nextNode = controller.HandleMapEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (controller.PromoteNode(node))
                {
                    FocusNode(node);
                }
            }
            else
            {
                var nextNode = controller.HandleMapTab(node);
                FocusNode(nextNode);
            }

            e.Handled = true;
        }
    }

    private void HandleNodeDragStarted(MindMapNode node, Control? control, PointerPressedEventArgs e)
    {
        var controller = ControllerContext;
        if (control is null
            || controller is null
            || controller.IsRoot(node)
            || !IsLeftPointerPressed(e.GetCurrentPoint(control).Properties))
        {
            return;
        }

        SelectNode(node);
        _dragNode = node;
        _dragStartPointer = e.GetPosition(_canvas);
        _isDraggingNode = false;
        _dropTarget = null;
        _dropPlacement = MindMapDropPlacement.Child;
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
        if (!_isDraggingNode && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) < DragStartDistance)
        {
            return;
        }

        _isDraggingNode = true;
        UpdateDropTarget(_dragNode, current);

        ApplySelectionState();
        e.Handled = true;
    }

    private void HandleNodeDragCompleted(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragNode is null)
        {
            return;
        }

        var dragNode = _dragNode;
        var dropTarget = _dropTarget;
        var dropPlacement = _dropPlacement;

        _dragNode = null;
        _dropTarget = null;
        _dropPlacement = MindMapDropPlacement.Child;
        _isDraggingNode = false;
        HideDropPreview();
        e.Pointer.Capture(null);

        if (dropTarget is not null
            && ControllerContext?.MoveNode(dragNode, dropTarget, dropPlacement) == true)
        {
            FocusNode(dragNode);
        }

        ApplySelectionState();
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
        UpdateViewportBounds();
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
        if (e.Key != Key.Space || HasVisualAncestor<TextBox>(e.Source))
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

    public void ZoomOut()
    {
        SetZoom(_zoomScale / ZoomFactor);
    }

    public void ZoomIn()
    {
        SetZoom(_zoomScale * ZoomFactor);
    }

    public void ResetZoom()
    {
        SetZoom(1);
    }

    public void CenterRoot()
    {
        var root = Roots?.FirstOrDefault();
        if (root is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => CenterNode(root), DispatcherPriority.Loaded);
    }

    public void CenterViewportAt(Point canvasPoint)
    {
        var offset = new Vector(
            canvasPoint.X * _zoomScale - _scrollViewer.Viewport.Width / 2,
            canvasPoint.Y * _zoomScale - _scrollViewer.Viewport.Height / 2);
        _scrollViewer.Offset = ClampScrollOffset(offset);
        UpdateViewportBounds();
    }

    private void SetZoom(double zoom)
    {
        var center = new Point(
            (_scrollViewer.Offset.X + _scrollViewer.Viewport.Width / 2) / _zoomScale,
            (_scrollViewer.Offset.Y + _scrollViewer.Viewport.Height / 2) / _zoomScale);

        _zoomScale = Math.Clamp(zoom, MinZoom, MaxZoom);
        _zoomHost.LayoutTransform = CreateZoomTransform(_zoomScale);
        _zoomHost.InvalidateMeasure();
        UpdateZoomText();
        CenterViewportAt(center);
    }

    private static ScaleTransform CreateZoomTransform(double zoom)
    {
        return new ScaleTransform(zoom, zoom);
    }

    private Vector ClampScrollOffset(Vector offset)
    {
        var maxX = Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);
        return new Vector(
            Math.Clamp(offset.X, 0, maxX),
            Math.Clamp(offset.Y, 0, maxY));
    }

    private void CenterNode(MindMapNode node)
    {
        var nodeSize = GetRenderedNodeSize(node);
        var center = new Point(node.X + nodeSize.Width / 2, node.Y + nodeSize.Height / 2);
        CenterViewportAt(center);
    }

    private void HandleScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.OffsetProperty
            || e.Property == ScrollViewer.ViewportProperty
            || e.Property == ScrollViewer.ExtentProperty)
        {
            UpdateViewportBounds();
        }
    }

    private void UpdateViewportBounds()
    {
        if (_zoomScale <= 0 || _scrollViewer.Viewport.Width <= 0 || _scrollViewer.Viewport.Height <= 0)
        {
            ViewportBounds = default;
            return;
        }

        ViewportBounds = new Rect(
            _scrollViewer.Offset.X / _zoomScale,
            _scrollViewer.Offset.Y / _zoomScale,
            _scrollViewer.Viewport.Width / _zoomScale,
            _scrollViewer.Viewport.Height / _zoomScale);
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

            if (ReferenceEquals(current, _nodeToolbar))
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

    private static bool HasVisualAncestor<T>(object? source)
        where T : Visual
    {
        if (source is not Visual visual)
        {
            return false;
        }

        for (var current = visual; current is not null; current = current.GetVisualParent())
        {
            if (current is T)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateZoomText()
    {
        ZoomText = $"{_zoomScale:P0}";
        UpdateViewportBounds();
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
        else if (e.PropertyName == nameof(MindMapNode.Note))
        {
            UpdateNoteEditorVisibility(node);
            UpdateConnectors();
            EnsureCanvasSize();
            PositionNodeToolbar();
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

    private void ShowNoteEditor(MindMapNode node)
    {
        // 备注输入框只有在用户显式添加或已有内容时显示，空备注失焦后会回收。
        _editingNoteNodes.Add(node);
        SelectNode(node);
        UpdateNoteEditorVisibility(node);
        PositionNodeToolbar();
        Dispatcher.UIThread.Post(() =>
        {
            if (_noteEditors.TryGetValue(node, out var editor))
            {
                editor.Focus();
                editor.CaretIndex = editor.Text?.Length ?? 0;
            }
        });
    }

    private void CollapseEmptyNoteEditor(MindMapNode node)
    {
        if (!string.IsNullOrWhiteSpace(node.Note))
        {
            return;
        }

        _editingNoteNodes.Remove(node);
        UpdateNoteEditorVisibility(node);
        UpdateConnectors();
        EnsureCanvasSize();
    }

    private void CollapseEmptyNoteEditorsExcept(MindMapNode? nodeToKeep)
    {
        foreach (var node in _editingNoteNodes.ToList())
        {
            if (!ReferenceEquals(node, nodeToKeep) && string.IsNullOrWhiteSpace(node.Note))
            {
                _editingNoteNodes.Remove(node);
                UpdateNoteEditorVisibility(node);
            }
        }
    }

    private void UpdateNoteEditorVisibility(MindMapNode node)
    {
        if (!_noteFrames.TryGetValue(node, out var frame))
        {
            return;
        }

        var visible = _editingNoteNodes.Contains(node) || !string.IsNullOrWhiteSpace(node.Note);
        frame.IsVisible = visible;
    }

    private void ShowNodeToolbar(MindMapNode node)
    {
        _toolbarNode = node;
        UpdateToolbarVisibility();
    }

    private void UpdateToolbarVisibility()
    {
        if (_toolbarNode is null || !_nodeFrames.ContainsKey(_toolbarNode))
        {
            _nodeToolbar.IsVisible = false;
            return;
        }

        _nodeToolbar.Background = Brush.Parse(IsDarkTheme ? "#111827" : "#FFFFFF");
        _nodeToolbar.BorderBrush = Brush.Parse(IsDarkTheme ? "#334155" : "#D8E0EA");
        _nodeToolbar.IsVisible = true;
        PositionNodeToolbar();
    }

    private void PositionNodeToolbar()
    {
        if (_toolbarNode is null || !_nodeFrames.TryGetValue(_toolbarNode, out _))
        {
            return;
        }

        var size = GetRenderedNodeSize(_toolbarNode);
        var x = _toolbarNode.X + size.Width / 2 - _nodeToolbar.Width / 2;
        var y = _toolbarNode.Y - _nodeToolbar.Height - 8;
        if (y < 8)
        {
            y = _toolbarNode.Y + size.Height + 8;
        }

        Canvas.SetLeft(_nodeToolbar, Math.Max(8, x));
        Canvas.SetTop(_nodeToolbar, Math.Max(8, y));
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
            var dragging = _isDraggingNode && ReferenceEquals(node, _dragNode);
            frame.BorderBrush = selected ? GetSelectionBrush() : metrics.BorderBrush;
            frame.BorderThickness = selected
                ? new Thickness(metrics.IsTextOnly ? 0 : 2)
                : metrics.BorderThickness;
            frame.BoxShadow = selected && !metrics.IsTextOnly
                ? BoxShadows.Parse("0 6 18 0 #18000000")
                : metrics.BoxShadow;
            frame.Opacity = dragging ? 0.55 : 1;
            UpdateNoteEditorVisibility(node);
        }

        PositionNodeToolbar();
    }

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
        return MindMapLayoutMetrics.EstimateNodeSize(node, ControllerContext?.GetLevel(node) ?? 1, metrics.Placeholder);
    }

    private void UpdateDropTarget(MindMapNode dragNode, Point canvasPoint)
    {
        var controller = ControllerContext;
        if (controller is null)
        {
            ClearDropTarget();
            return;
        }

        MindMapNode? nextTarget = null;
        var nextPlacement = MindMapDropPlacement.Child;

        foreach (var (node, frame) in _nodeFrames)
        {
            if (!controller.CanMoveNode(dragNode, node))
            {
                continue;
            }

            var bounds = GetNodeBounds(node, frame);
            if (!bounds.Contains(canvasPoint))
            {
                continue;
            }

            nextTarget = node;
            nextPlacement = GetDropPlacement(bounds, canvasPoint);
            if (controller.IsRoot(node) && nextPlacement is MindMapDropPlacement.Before or MindMapDropPlacement.After)
            {
                nextPlacement = MindMapDropPlacement.Child;
            }

            break;
        }

        if (nextTarget is null)
        {
            ClearDropTarget();
            return;
        }

        _dropTarget = nextTarget;
        _dropPlacement = nextPlacement;
        ShowDropPreview(nextTarget, nextPlacement);
    }

    private void ClearDropTarget()
    {
        _dropTarget = null;
        _dropPlacement = MindMapDropPlacement.Child;
        HideDropPreview();
    }

    private static MindMapDropPlacement GetDropPlacement(Rect targetBounds, Point pointer)
    {
        var offsetY = pointer.Y - targetBounds.Top;
        if (offsetY < targetBounds.Height * DropEdgeRatio)
        {
            return MindMapDropPlacement.Before;
        }

        if (offsetY > targetBounds.Height * (1 - DropEdgeRatio))
        {
            return MindMapDropPlacement.After;
        }

        return MindMapDropPlacement.Child;
    }

    private static bool IsRightPointerPressed(PointerPointProperties properties)
    {
        return properties.IsRightButtonPressed
            || properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed;
    }

    private static bool IsLeftPointerPressed(PointerPointProperties properties)
    {
        return properties.IsLeftButtonPressed
            || properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;
    }

    private Rect GetNodeBounds(MindMapNode node, Control frame)
    {
        var size = GetRenderedNodeSize(node);
        return new Rect(node.X, node.Y, size.Width, size.Height);
    }

    private void ShowDropPreview(MindMapNode target, MindMapDropPlacement placement)
    {
        if (!_nodeFrames.TryGetValue(target, out var frame))
        {
            HideDropPreview();
            return;
        }

        var bounds = GetNodeBounds(target, frame);
        _dropPreviewPath.Stroke = placement == MindMapDropPlacement.Child
            ? Brush.Parse("#22C55E")
            : GetSelectionBrush();
        _dropPreviewPath.Data = CreateDropPreviewGeometry(bounds, placement);
        _dropPreviewPath.IsVisible = true;
    }

    private void HideDropPreview()
    {
        _dropPreviewPath.IsVisible = false;
        _dropPreviewPath.Data = null;
    }

    private static Geometry CreateDropPreviewGeometry(Rect bounds, MindMapDropPlacement placement)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();

        if (placement == MindMapDropPlacement.Child)
        {
            // 拖到节点中部时用虚线框提示“成为子节点”的最终结果。
            var rect = bounds.Inflate(7);
            context.BeginFigure(rect.TopLeft, isFilled: false);
            context.LineTo(rect.TopRight);
            context.LineTo(rect.BottomRight);
            context.LineTo(rect.BottomLeft);
            context.LineTo(rect.TopLeft);
            context.EndFigure(isClosed: true);
            return geometry;
        }

        var y = placement == MindMapDropPlacement.Before
            ? bounds.Top - 8
            : bounds.Bottom + 8;
        // 拖到上下边缘时只画一条插入线，表示调整同级顺序。
        var start = new Point(bounds.Left - 16, y);
        var end = new Point(bounds.Right + 16, y);
        context.BeginFigure(start, isFilled: false);
        context.LineTo(end);
        return geometry;
    }

    private NodeMetrics GetNodeMetrics(MindMapNode node)
    {
        var level = ControllerContext?.GetLevel(node) ?? 1;
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
                HorizontalAlignment.Center,
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
                GetResourceBrush(MindViewStyleKeys.BranchBackgroundBrushResource, "#EEF0F3", "#1F2937"),
                Brushes.Transparent,
                new Thickness(0),
                new Thickness(12, 5),
                BoxShadows.Parse(IsDarkTheme ? "0 3 10 0 #26000000" : "0 3 10 0 #0C000000"),
                GetPrimaryTextBrush(),
                17,
                FontWeight.Medium,
                HorizontalAlignment.Center,
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
        HorizontalAlignment TextAlignment,
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
        return GetResourceBrush(MindViewStyleKeys.SecondaryTextBrushResource, "#334155", "#CBD5E1");
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

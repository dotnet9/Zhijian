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
    private const double NodeMenuWidth = 224;
    private const double LayoutRootX = 72;
    private const double LayoutRootY = 72;
    private const double LayoutMinNodeY = 24;

    private static readonly string[] DefaultPalette =
    [
        "#2563EB",
        "#16A34A",
        "#F97316",
        "#DB2777",
        "#7C3AED",
        "#0891B2",
        "#DC2626",
        "#CA8A04",
        "#0D9488",
        "#4F46E5"
    ];

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
    private int _nextPaletteIndex = Random.Shared.Next(DefaultPalette.Length);
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

    /// <summary>
    /// 按当前树结构重新计算节点坐标。未提供 Controller 时，控件的内置结构操作会自动调用它。
    /// </summary>
    public void ArrangeNodes()
    {
        if (Roots is null || Roots.Count == 0)
        {
            return;
        }

        foreach (var root in Roots)
        {
            AssignMissingColors(root);
        }

        var nextTop = LayoutRootY;
        foreach (var root in Roots)
        {
            var columnPositions = CalculateColumnPositions(root);
            LayoutNode(root, 0, columnPositions, ref nextTop);
            nextTop += MindMapLayoutMetrics.DefaultVerticalSpacing;
        }
    }

    /// <summary>
    /// 添加子主题。宿主提供 Controller 时委托给宿主，否则使用控件内置树操作。
    /// </summary>
    public MindMapNode AddChild(MindMapNode? parent = null, string title = "新主题")
    {
        if (ControllerContext is { } controller)
        {
            return controller.AddChild(parent, title);
        }

        parent = ResolveEditableNode(parent);
        var child = CreateNode(title);
        parent.Children.Add(child);
        SelectedNode = child;
        ArrangeNodes();
        return child;
    }

    /// <summary>
    /// 添加同级主题；根节点会转为添加子主题，避免产生多个中心主题。
    /// </summary>
    public MindMapNode AddSibling(MindMapNode? node = null, string title = "新主题")
    {
        if (ControllerContext is { } controller)
        {
            return controller.AddSibling(node, title);
        }

        node = ResolveEditableNode(node);
        if (IsRootNode(node))
        {
            return AddChild(node, title);
        }

        var parent = FindParent(node) ?? EnsureRoot();
        var sibling = CreateNode(title);
        var index = parent.Children.IndexOf(node);
        parent.Children.Insert(index + 1, sibling);
        SelectedNode = sibling;
        ArrangeNodes();
        return sibling;
    }

    public bool CanPromoteNode(MindMapNode? node = null)
    {
        node ??= SelectedNode;
        if (ControllerContext is { } controller)
        {
            return controller.CanPromoteNode(node);
        }

        if (node is null || IsRootNode(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && !IsRootNode(parent) && FindParent(parent) is not null;
    }

    public bool PromoteNode(MindMapNode? node = null)
    {
        node ??= SelectedNode;
        if (ControllerContext is { } controller)
        {
            return controller.PromoteNode(node);
        }

        if (!CanPromoteNode(node) || node is null)
        {
            return false;
        }

        var parent = FindParent(node)!;
        var grandParent = FindParent(parent)!;
        parent.Children.Remove(node);
        var parentIndex = grandParent.Children.IndexOf(parent);
        grandParent.Children.Insert(parentIndex + 1, node);
        SelectedNode = node;
        ArrangeNodes();
        return true;
    }

    public bool CanDemoteNode(MindMapNode? node = null)
    {
        node ??= SelectedNode;
        if (ControllerContext is { } controller)
        {
            return controller.CanDemoteNode(node);
        }

        if (node is null || IsRootNode(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && parent.Children.IndexOf(node) > 0;
    }

    public bool DemoteNode(MindMapNode? node = null)
    {
        node ??= SelectedNode;
        if (ControllerContext is { } controller)
        {
            return controller.DemoteNode(node);
        }

        if (!CanDemoteNode(node) || node is null)
        {
            return false;
        }

        var parent = FindParent(node)!;
        var index = parent.Children.IndexOf(node);
        var newParent = parent.Children[index - 1];
        parent.Children.RemoveAt(index);
        newParent.Children.Add(node);
        SelectedNode = node;
        ArrangeNodes();
        return true;
    }

    public bool CanMoveNodeUp(MindMapNode? node = null)
    {
        node ??= SelectedNode;
        if (ControllerContext is { } controller)
        {
            return controller.CanMoveNodeUp(node);
        }

        if (node is null || IsRootNode(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && parent.Children.IndexOf(node) > 0;
    }

    public bool MoveNodeUp(MindMapNode? node = null)
    {
        node ??= SelectedNode;
        if (ControllerContext is { } controller)
        {
            return controller.MoveNodeUp(node);
        }

        return MoveNodeWithinSiblings(node, -1);
    }

    public bool CanMoveNodeDown(MindMapNode? node = null)
    {
        node ??= SelectedNode;
        if (ControllerContext is { } controller)
        {
            return controller.CanMoveNodeDown(node);
        }

        if (node is null || IsRootNode(node))
        {
            return false;
        }

        var parent = FindParent(node);
        if (parent is null)
        {
            return false;
        }

        var index = parent.Children.IndexOf(node);
        return index >= 0 && index < parent.Children.Count - 1;
    }

    public bool MoveNodeDown(MindMapNode? node = null)
    {
        node ??= SelectedNode;
        if (ControllerContext is { } controller)
        {
            return controller.MoveNodeDown(node);
        }

        return MoveNodeWithinSiblings(node, 1);
    }

    public MindMapNode DeleteNode(MindMapNode? node = null)
    {
        if (ControllerContext is { } controller)
        {
            return controller.DeleteNode(node);
        }

        node = ResolveEditableNode(node);
        if (IsRootNode(node))
        {
            SelectedNode = node;
            return node;
        }

        var parent = FindParent(node) ?? EnsureRoot();
        var index = parent.Children.IndexOf(node);
        var focusTarget = index > 0 ? parent.Children[index - 1] : parent;
        parent.Children.Remove(node);
        SelectedNode = focusTarget;
        ArrangeNodes();
        return focusTarget;
    }

    public bool CanMoveNode(MindMapNode? node, MindMapNode? target)
    {
        if (ControllerContext is { } controller)
        {
            return controller.CanMoveNode(node, target);
        }

        return node is not null
            && target is not null
            && !IsRootNode(node)
            && !ReferenceEquals(node, target)
            && !IsDescendant(node, target);
    }

    public bool MoveNode(MindMapNode? node, MindMapNode? target, MindMapDropPlacement placement)
    {
        if (ControllerContext is { } controller)
        {
            return controller.MoveNode(node, target, placement);
        }

        if (!CanMoveNode(node, target) || node is null || target is null)
        {
            return false;
        }

        var oldParent = FindParent(node);
        if (oldParent is null)
        {
            return false;
        }

        if (IsRootNode(target) && placement is MindMapDropPlacement.Before or MindMapDropPlacement.After)
        {
            placement = MindMapDropPlacement.Child;
        }

        var newParent = placement == MindMapDropPlacement.Child
            ? target
            : FindParent(target) ?? EnsureRoot();
        var insertionIndex = placement switch
        {
            MindMapDropPlacement.Before => newParent.Children.IndexOf(target),
            MindMapDropPlacement.After => newParent.Children.IndexOf(target) + 1,
            _ => newParent.Children.Count
        };

        var oldIndex = oldParent.Children.IndexOf(node);
        if (oldIndex < 0)
        {
            return false;
        }

        oldParent.Children.RemoveAt(oldIndex);
        if (ReferenceEquals(oldParent, newParent) && oldIndex < insertionIndex)
        {
            insertionIndex--;
        }

        insertionIndex = Math.Clamp(insertionIndex, 0, newParent.Children.Count);
        newParent.Children.Insert(insertionIndex, node);
        SelectedNode = node;
        ArrangeNodes();
        return true;
    }

    private IMindMapEditorController? ControllerContext => Controller ?? DataContext as IMindMapEditorController;

    private MindMapNode ResolveEditableNode(MindMapNode? node)
    {
        if (node is not null && ContainsNode(node))
        {
            return node;
        }

        if (SelectedNode is not null && ContainsNode(SelectedNode))
        {
            return SelectedNode;
        }

        return EnsureRoot();
    }

    private MindMapNode EnsureRoot()
    {
        var roots = Roots;
        if (roots is null)
        {
            roots = [];
            Roots = roots;
        }

        if (roots.Count == 0)
        {
            roots.Add(CreateNode(string.Empty));
        }

        return roots[0];
    }

    private MindMapNode CreateNode(string title)
    {
        return new MindMapNode(title)
        {
            AccentColor = NextColor()
        };
    }

    private void AssignMissingColors(MindMapNode node)
    {
        if (string.IsNullOrWhiteSpace(node.AccentColor))
        {
            node.AccentColor = NextColor();
        }

        foreach (var child in node.Children)
        {
            AssignMissingColors(child);
        }
    }

    private string NextColor()
    {
        return DefaultPalette[_nextPaletteIndex++ % DefaultPalette.Length];
    }

    private bool IsRootNode(MindMapNode? node)
    {
        if (ControllerContext is { } controller)
        {
            return controller.IsRoot(node);
        }

        return node is not null && Roots?.Contains(node) == true;
    }

    private int GetNodeLevel(MindMapNode node)
    {
        if (ControllerContext is { } controller)
        {
            return controller.GetLevel(node);
        }

        return FindLevel(node) ?? 1;
    }

    private int? FindLevel(MindMapNode node)
    {
        if (Roots is null)
        {
            return null;
        }

        foreach (var root in Roots)
        {
            var level = FindLevel(root, node, 1);
            if (level is not null)
            {
                return level;
            }
        }

        return null;
    }

    private static int? FindLevel(MindMapNode current, MindMapNode node, int level)
    {
        if (ReferenceEquals(current, node))
        {
            return level;
        }

        foreach (var child in current.Children)
        {
            var childLevel = FindLevel(child, node, level + 1);
            if (childLevel is not null)
            {
                return childLevel;
            }
        }

        return null;
    }

    private bool ContainsNode(MindMapNode node)
    {
        return FindLevel(node) is not null;
    }

    private MindMapNode? FindParent(MindMapNode node)
    {
        if (Roots is null)
        {
            return null;
        }

        foreach (var root in Roots)
        {
            var parent = FindParent(root, node);
            if (parent is not null)
            {
                return parent;
            }
        }

        return null;
    }

    private static MindMapNode? FindParent(MindMapNode candidateParent, MindMapNode node)
    {
        if (candidateParent.Children.Contains(node))
        {
            return candidateParent;
        }

        foreach (var child in candidateParent.Children)
        {
            var parent = FindParent(child, node);
            if (parent is not null)
            {
                return parent;
            }
        }

        return null;
    }

    private static bool IsDescendant(MindMapNode candidateAncestor, MindMapNode candidateDescendant)
    {
        foreach (var child in candidateAncestor.Children)
        {
            if (ReferenceEquals(child, candidateDescendant) || IsDescendant(child, candidateDescendant))
            {
                return true;
            }
        }

        return false;
    }

    private MindMapNode HandleMapEnter(MindMapNode node)
    {
        if (ControllerContext is { } controller)
        {
            return controller.HandleMapEnter(node);
        }

        return IsRootNode(node) ? AddChild(node, string.Empty) : AddSibling(node, string.Empty);
    }

    private MindMapNode HandleMapTab(MindMapNode node)
    {
        if (ControllerContext is { } controller)
        {
            return controller.HandleMapTab(node);
        }

        return AddChild(node, string.Empty);
    }

    private bool MoveNodeWithinSiblings(MindMapNode? node, int offset)
    {
        if (node is null || IsRootNode(node))
        {
            return false;
        }

        var parent = FindParent(node);
        if (parent is null)
        {
            return false;
        }

        var oldIndex = parent.Children.IndexOf(node);
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= parent.Children.Count)
        {
            return false;
        }

        parent.Children.Move(oldIndex, newIndex);
        SelectedNode = node;
        ArrangeNodes();
        return true;
    }

    private static double[] CalculateColumnPositions(MindMapNode root)
    {
        var columnWidths = new List<double>();
        CollectColumnWidths(root, 0, columnWidths);

        var columnPositions = new double[columnWidths.Count];
        var x = LayoutRootX;
        for (var i = 0; i < columnPositions.Length; i++)
        {
            columnPositions[i] = x;
            x += columnWidths[i] + MindMapLayoutMetrics.DefaultHorizontalSpacing;
        }

        return columnPositions;
    }

    private static void CollectColumnWidths(MindMapNode node, int depth, List<double> columnWidths)
    {
        var level = depth + 1;
        var size = MindMapLayoutMetrics.EstimateNodeSize(node, level);
        while (columnWidths.Count <= depth)
        {
            columnWidths.Add(0);
        }

        columnWidths[depth] = Math.Max(columnWidths[depth], size.Width);
        foreach (var child in node.Children)
        {
            CollectColumnWidths(child, depth + 1, columnWidths);
        }
    }

    private static LayoutResult LayoutNode(MindMapNode node, int depth, IReadOnlyList<double> columnPositions, ref double nextTop)
    {
        var level = depth + 1;
        var size = MindMapLayoutMetrics.EstimateNodeSize(node, level);
        node.X = depth < columnPositions.Count
            ? columnPositions[depth]
            : columnPositions[^1] + (depth - columnPositions.Count + 1) * (MindMapLayoutMetrics.LeafMaxWidth + MindMapLayoutMetrics.DefaultHorizontalSpacing);

        if (node.Children.Count == 0)
        {
            node.Y = nextTop;
            nextTop = node.Y + size.Height + MindMapLayoutMetrics.DefaultVerticalSpacing;
            return new LayoutResult(node.Y + size.Height / 2, node.Y, node.Y + size.Height);
        }

        var firstCenter = 0d;
        var lastCenter = 0d;
        var subtreeTop = double.MaxValue;
        var subtreeBottom = double.MinValue;
        for (var i = 0; i < node.Children.Count; i++)
        {
            var childLayout = LayoutNode(node.Children[i], depth + 1, columnPositions, ref nextTop);
            if (i == 0)
            {
                firstCenter = childLayout.CenterY;
            }

            lastCenter = childLayout.CenterY;
            subtreeTop = Math.Min(subtreeTop, childLayout.Top);
            subtreeBottom = Math.Max(subtreeBottom, childLayout.Bottom);
        }

        var nodeCenter = (firstCenter + lastCenter) / 2;
        node.Y = Math.Max(LayoutMinNodeY, nodeCenter - size.Height / 2);
        subtreeTop = Math.Min(subtreeTop, node.Y);
        subtreeBottom = Math.Max(subtreeBottom, node.Y + size.Height);
        nextTop = Math.Max(nextTop, subtreeBottom + MindMapLayoutMetrics.DefaultVerticalSpacing);
        return new LayoutResult(node.Y + size.Height / 2, subtreeTop, subtreeBottom);
    }

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
            AssignMissingColors(root);
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

        var isRoot = IsRootNode(node);
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
            PlaceholderForeground = isRoot ? Brushes.White : GetSecondaryTextBrush(),
            FocusAdorner = null,
            TextAlignment = ToTextAlignment(metrics.ContentAlignment),
            HorizontalContentAlignment = metrics.ContentAlignment,
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
                if (isRoot)
                {
                    FocusNode(node);
                }
                else
                {
                    root.Focus();
                    HandleNodeDragStarted(node, root, e);
                }

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
        var noteForeground = metrics.IsTextOnly
            ? GetSecondaryTextBrush()
            : Brush.Parse(IsDarkTheme ? "#CBD5E1" : "#D1D5DB");
        var noteBox = new TextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = noteForeground,
            FontSize = MindMapLayoutMetrics.NoteFontSize,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            PlaceholderText = "备注",
            PlaceholderForeground = noteForeground,
            FocusAdorner = null,
            TextAlignment = ToTextAlignment(metrics.ContentAlignment),
            MinWidth = Math.Max(12, metrics.MinWidth - metrics.Padding.Left - metrics.Padding.Right),
            MaxWidth = Math.Max(12, metrics.MaxWidth - metrics.Padding.Left - metrics.Padding.Right),
            MinHeight = MindMapLayoutMetrics.NoteMinHeight,
            MaxHeight = 96,
            Padding = new Thickness(0, 2, 0, 0),
            HorizontalContentAlignment = metrics.ContentAlignment,
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
        _nodeMenuPanel.Children.Clear();
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("+", "添加子级", "Tab", true, () => AddChildFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("+", "添加同级", "Enter", !IsRootNode(node), () => AddSiblingFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("<", "提升为父节点", "Shift+Tab", CanPromoteNode(node), () => PromoteNodeFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem(">", "降级为子节点", "Tab", CanDemoteNode(node), () => DemoteNodeFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("^", "上移", "Alt+Up", CanMoveNodeUp(node), () => MoveNodeUpFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("v", "下移", "Alt+Down", CanMoveNodeDown(node), () => MoveNodeDownFromMenu(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("i", string.IsNullOrWhiteSpace(node.Note) ? "添加备注" : "编辑备注", null, true, () => ShowNoteEditor(node)));
        _nodeMenuPanel.Children.Add(CreateNodeMenuItem("x", "删除", "Delete", !IsRootNode(node), () => DeleteNodeFromMenu(node)));

        _nodeMenu.Background = Brush.Parse(IsDarkTheme ? "#111827" : "#FFFFFF");
        _nodeMenu.BorderBrush = Brush.Parse(IsDarkTheme ? "#334155" : "#D8E0EA");
        _nodeMenu.IsVisible = true;

        var x = Math.Clamp(canvasPoint.X, 8, Math.Max(8, _canvas.Width - NodeMenuWidth - 8));
        var y = Math.Clamp(canvasPoint.Y, 8, Math.Max(8, _canvas.Height - 270));
        Canvas.SetLeft(_nodeMenu, x);
        Canvas.SetTop(_nodeMenu, y);
    }

    private Border CreateNodeMenuItem(string iconText, string header, string? shortcut, bool isEnabled, Action action)
    {
        var foreground = isEnabled
            ? GetPrimaryTextBrush()
            : Brush.Parse(IsDarkTheme ? "#64748B" : "#A0A7B1");
        var shortcutBrush = isEnabled
            ? Brush.Parse(IsDarkTheme ? "#94A3B8" : "#667085")
            : Brush.Parse(IsDarkTheme ? "#475569" : "#A0A7B1");
        var icon = new TextBlock
        {
            Text = iconText,
            Width = 18,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = shortcutBrush,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var text = new TextBlock
        {
            Text = header,
            FontSize = 13,
            Foreground = foreground,
            VerticalAlignment = VerticalAlignment.Center
        };
        var shortcutText = new TextBlock
        {
            Text = shortcut ?? string.Empty,
            FontSize = 12,
            Foreground = shortcutBrush,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("22,*,Auto"),
            ColumnSpacing = 8
        };
        Grid.SetColumn(text, 1);
        Grid.SetColumn(shortcutText, 2);
        content.Children.Add(icon);
        content.Children.Add(text);
        content.Children.Add(shortcutText);

        var row = new Border
        {
            Height = 30,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 0),
            Background = Brushes.Transparent,
            Cursor = isEnabled ? new Cursor(StandardCursorType.Hand) : Cursor.Default,
            Child = content
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
        if (_toolbarNode is null || IsRootNode(_toolbarNode))
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
        var child = AddChild(node);
        _toolbarNode = child;
        FocusNode(child);
    }

    private void AddSiblingFromMenu(MindMapNode node)
    {
        if (IsRootNode(node))
        {
            return;
        }

        var sibling = AddSibling(node);
        _toolbarNode = sibling;
        FocusNode(sibling);
    }

    private void PromoteNodeFromMenu(MindMapNode node)
    {
        if (PromoteNode(node))
        {
            _toolbarNode = node;
            FocusNode(node);
        }
    }

    private void DemoteNodeFromMenu(MindMapNode node)
    {
        if (DemoteNode(node))
        {
            _toolbarNode = node;
            FocusNode(node);
        }
    }

    private void MoveNodeUpFromMenu(MindMapNode node)
    {
        if (MoveNodeUp(node))
        {
            _toolbarNode = node;
            FocusNode(node);
        }
    }

    private void MoveNodeDownFromMenu(MindMapNode node)
    {
        if (MoveNodeDown(node))
        {
            _toolbarNode = node;
            FocusNode(node);
        }
    }

    private void DeleteNodeFromMenu(MindMapNode node)
    {
        if (IsRootNode(node))
        {
            return;
        }

        var focusTarget = DeleteNode(node);
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
        if (e.Key == Key.Enter)
        {
            var nextNode = HandleMapEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            var nextNode = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? node
                : HandleMapTab(node);

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                PromoteNode(node);
            }

            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if ((e.Key == Key.Delete || e.Key == Key.Back)
            && string.IsNullOrWhiteSpace(editor?.Text)
            && !IsRootNode(node))
        {
            var focusTarget = DeleteNode(node);
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
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            var focusTarget = DeleteNode(node);
            FocusFrame(focusTarget);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            var nextNode = HandleMapEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (PromoteNode(node))
                {
                    FocusNode(node);
                }
            }
            else
            {
                var nextNode = HandleMapTab(node);
                FocusNode(nextNode);
            }

            e.Handled = true;
        }
    }

    private void HandleNodeDragStarted(MindMapNode node, Control? control, PointerPressedEventArgs e)
    {
        if (control is null
            || IsRootNode(node)
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
        var wasDragging = _isDraggingNode;

        _dragNode = null;
        _dropTarget = null;
        _dropPlacement = MindMapDropPlacement.Child;
        _isDraggingNode = false;
        HideDropPreview();
        e.Pointer.Capture(null);

        if (dropTarget is not null
            && MoveNode(dragNode, dropTarget, dropPlacement))
        {
            FocusNode(dragNode);
        }
        else if (!wasDragging)
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
        if (!HasZoomModifier(e.KeyModifiers))
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

    private static bool HasZoomModifier(KeyModifiers modifiers)
    {
        var commandModifier = OperatingSystem.IsMacOS()
            ? KeyModifiers.Meta
            : KeyModifiers.Control;
        return modifiers.HasFlag(commandModifier);
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
        return MindMapLayoutMetrics.EstimateNodeSize(node, GetNodeLevel(node), metrics.Placeholder);
    }

    private void UpdateDropTarget(MindMapNode dragNode, Point canvasPoint)
    {
        MindMapNode? nextTarget = null;
        var nextPlacement = MindMapDropPlacement.Child;

        foreach (var (node, frame) in _nodeFrames)
        {
            if (!CanMoveNode(dragNode, node))
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
            if (IsRootNode(node) && nextPlacement is MindMapDropPlacement.Before or MindMapDropPlacement.After)
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

    private readonly record struct LayoutResult(double CenterY, double Top, double Bottom);

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

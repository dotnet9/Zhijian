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

public partial class MindMapEditor : UserControl
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
    private const double MinCanvasHorizontalPadding = 240;
    private const double MinCanvasVerticalPadding = 180;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 2.0;
    private const double ZoomFactor = 1.1;
    private const double DragStartDistance = 6;
    private const double DropEdgeRatio = 0.28;
    private const double NodeMenuWidth = 224;
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

        MindMapTreeLayout.Arrange(Roots);
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

}

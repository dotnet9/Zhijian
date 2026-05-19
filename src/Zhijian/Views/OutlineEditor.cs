using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodeWF.MindView;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Zhijian.ViewModels;
using AtomTextBox = AtomUI.Desktop.Controls.TextBox;
using AtomToolTip = AtomUI.Desktop.Controls.ToolTip;

namespace Zhijian.Views;

public partial class OutlineEditor : UserControl
{
    public static readonly StyledProperty<ObservableCollection<MindMapNode>?> RootsProperty =
        AvaloniaProperty.Register<OutlineEditor, ObservableCollection<MindMapNode>?>(nameof(Roots));

    public static readonly StyledProperty<MindMapNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<OutlineEditor, MindMapNode?>(
            nameof(SelectedNode),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IMindMapEditorController?> ControllerProperty =
        AvaloniaProperty.Register<OutlineEditor, IMindMapEditorController?>(nameof(Controller));

    public static readonly StyledProperty<bool> IsDarkThemeProperty =
        AvaloniaProperty.Register<OutlineEditor, bool>(nameof(IsDarkTheme));

    public static readonly StyledProperty<string> CenterTopicPlaceholderProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(CenterTopicPlaceholder), "中心主题");

    public static readonly StyledProperty<string> TopicPlaceholderProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(TopicPlaceholder), "主题");

    public static readonly StyledProperty<string> NotePlaceholderProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(NotePlaceholder), "备注");

    public static readonly StyledProperty<string> AddChildTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(AddChildText), "添加子级");

    public static readonly StyledProperty<string> AddSiblingTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(AddSiblingText), "添加同级");

    public static readonly StyledProperty<string> PromoteTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(PromoteText), "提升为父节点");

    public static readonly StyledProperty<string> DemoteTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DemoteText), "降级为子节点");

    public static readonly StyledProperty<string> MoveUpTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(MoveUpText), "上移");

    public static readonly StyledProperty<string> MoveDownTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(MoveDownText), "下移");

    public static readonly StyledProperty<string> AddNoteTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(AddNoteText), "添加备注");

    public static readonly StyledProperty<string> EditNoteTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(EditNoteText), "编辑备注");

    public static readonly StyledProperty<string> DeleteNodeTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DeleteNodeText), "删除节点");

    public static readonly StyledProperty<string> DragNodeTipProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DragNodeTip), "拖到节点中部成为子节点，拖到上下边缘成为同级节点");

    private const double IndentSize = 24;
    private const double DragHandleColumnWidth = 28;
    private const double OutlineDotSize = 8;
    private const double OutlineDotGlowSize = 20;
    private const double DropEdgeRatio = 0.28;
    private const double DragStartDistance = 6;
    private const int RebuildRowBatchSize = 80;

    private readonly StackPanel _itemsPanel = new()
    {
        Spacing = 2,
        Margin = new Thickness(10, 8)
    };

    private readonly Dictionary<MindMapNode, Border> _rowFrames = [];
    private readonly Dictionary<MindMapNode, AtomTextBox> _titleEditors = [];
    private readonly Dictionary<MindMapNode, AtomTextBox> _noteEditors = [];
    private readonly Dictionary<MindMapNode, Border> _noteFrames = [];
    private readonly HashSet<MindMapNode> _editingNoteNodes = [];
    private readonly Dictionary<MindMapNode, Border> _dragHandles = [];
    private readonly Dictionary<MindMapNode, Border> _dragHandleGlows = [];
    private readonly List<MindMapNode> _observedNodes = [];
    private readonly List<INotifyCollectionChanged> _observedCollections = [];

    private int _rebuildVersion;
    private bool _isRebuildQueued;
    private KeyEventArgs? _lastHandledEditorKeyEvent;
    private MindMapNode? _dragNode;
    private MindMapNode? _dropTarget;
    private MindMapNode? _pendingFocusNode;
    private Control? _dragAnchor;
    private MindMapNode? _hoverDragHandleNode;
    private Point _dragStartPointer;
    private bool _isDraggingNode;
    private MindMapDropPlacement _dropPlacement = MindMapDropPlacement.Child;

    public OutlineEditor()
    {
        _itemsPanel.PointerMoved += HandleDragMoved;
        _itemsPanel.PointerReleased += HandleDragReleased;

        Content = new ScrollViewer
        {
            Content = _itemsPanel,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
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

    public IMindMapEditorController? Controller
    {
        get => GetValue(ControllerProperty);
        set => SetValue(ControllerProperty, value);
    }

    public bool IsDarkTheme
    {
        get => GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
    }

    public string CenterTopicPlaceholder
    {
        get => GetValue(CenterTopicPlaceholderProperty);
        set => SetValue(CenterTopicPlaceholderProperty, value);
    }

    public string TopicPlaceholder
    {
        get => GetValue(TopicPlaceholderProperty);
        set => SetValue(TopicPlaceholderProperty, value);
    }

    public string NotePlaceholder
    {
        get => GetValue(NotePlaceholderProperty);
        set => SetValue(NotePlaceholderProperty, value);
    }

    public string AddChildText
    {
        get => GetValue(AddChildTextProperty);
        set => SetValue(AddChildTextProperty, value);
    }

    public string AddSiblingText
    {
        get => GetValue(AddSiblingTextProperty);
        set => SetValue(AddSiblingTextProperty, value);
    }

    public string PromoteText
    {
        get => GetValue(PromoteTextProperty);
        set => SetValue(PromoteTextProperty, value);
    }

    public string DemoteText
    {
        get => GetValue(DemoteTextProperty);
        set => SetValue(DemoteTextProperty, value);
    }

    public string MoveUpText
    {
        get => GetValue(MoveUpTextProperty);
        set => SetValue(MoveUpTextProperty, value);
    }

    public string MoveDownText
    {
        get => GetValue(MoveDownTextProperty);
        set => SetValue(MoveDownTextProperty, value);
    }

    public string AddNoteText
    {
        get => GetValue(AddNoteTextProperty);
        set => SetValue(AddNoteTextProperty, value);
    }

    public string EditNoteText
    {
        get => GetValue(EditNoteTextProperty);
        set => SetValue(EditNoteTextProperty, value);
    }

    public string DeleteNodeText
    {
        get => GetValue(DeleteNodeTextProperty);
        set => SetValue(DeleteNodeTextProperty, value);
    }

    public string DragNodeTip
    {
        get => GetValue(DragNodeTipProperty);
        set => SetValue(DragNodeTipProperty, value);
    }

    private IMindMapEditorController? ViewModel =>
        Controller
        ?? DataContext as IMindMapEditorController
        ?? this.FindAncestorOfType<Window>()?.DataContext as IMindMapEditorController;

    private sealed record OutlineRowWorkItem(MindMapNode Node, int Level);

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
            ApplySelectionState();
        }
        else if (change.Property == IsDarkThemeProperty)
        {
            Rebuild();
        }
        else if (IsTextResourceProperty(change.Property))
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        _isRebuildQueued = false;
        var version = ++_rebuildVersion;
        DetachSubscriptions();
        _itemsPanel.Children.Clear();
        _rowFrames.Clear();
        _titleEditors.Clear();
        _noteEditors.Clear();
        _noteFrames.Clear();
        _dragHandles.Clear();
        _dragHandleGlows.Clear();
        _hoverDragHandleNode = null;

        if (Roots is null)
        {
            return;
        }

        Roots.CollectionChanged += HandleTreeChanged;
        _observedCollections.Add(Roots);

        var rowWork = new List<OutlineRowWorkItem>();
        foreach (var root in Roots)
        {
            WatchNode(root);
            CollectRowWork(root, 1, rowWork);
        }

        RenderRowBatch(version, rowWork, rowIndex: 0);
    }

    private static void CollectRowWork(MindMapNode node, int level, ICollection<OutlineRowWorkItem> rowWork)
    {
        rowWork.Add(new OutlineRowWorkItem(node, level));
        foreach (var child in node.Children)
        {
            CollectRowWork(child, level + 1, rowWork);
        }
    }

    private void RenderRowBatch(int version, IReadOnlyList<OutlineRowWorkItem> rowWork, int rowIndex)
    {
        if (version != _rebuildVersion)
        {
            return;
        }

        var rowLimit = Math.Min(rowWork.Count, rowIndex + RebuildRowBatchSize);
        for (var i = rowIndex; i < rowLimit; i++)
        {
            AddNodeRow(rowWork[i].Node, rowWork[i].Level);
        }

        TryFocusPendingNode();

        if (rowLimit < rowWork.Count)
        {
            Dispatcher.UIThread.Post(
                () => RenderRowBatch(version, rowWork, rowLimit),
                DispatcherPriority.Background);
            return;
        }

        ApplySelectionState();
        TryFocusPendingNode();
    }

    private void DetachSubscriptions()
    {
        foreach (var node in _observedNodes)
        {
            node.PropertyChanged -= HandleNodePropertyChanged;
        }

        foreach (var collection in _observedCollections)
        {
            collection.CollectionChanged -= HandleTreeChanged;
        }

        _observedNodes.Clear();
        _observedCollections.Clear();
    }

    private void WatchNode(MindMapNode node)
    {
        _observedNodes.Add(node);
        node.PropertyChanged += HandleNodePropertyChanged;
        node.Children.CollectionChanged += HandleTreeChanged;
        _observedCollections.Add(node.Children);

        foreach (var child in node.Children)
        {
            WatchNode(child);
        }
    }

    private void AddNodeRow(MindMapNode node, int level)
    {
        var isRoot = ViewModel?.IsRoot(node) == true;
        var frame = CreateRowFrame(node, level);
        var grid = CreateRowGrid();
        var dragHandle = CreateDragHandle(node, isRoot);
        var titleBox = CreateTitleEditor(node, isRoot);
        var noteBox = CreateNoteEditor(node);
        var noteFrame = CreateNoteFrame(noteBox);
        var contentPanel = new StackPanel { Spacing = 3 };

        contentPanel.Children.Add(titleBox);
        contentPanel.Children.Add(noteFrame);
        Grid.SetColumn(contentPanel, 1);

        grid.Children.Add(dragHandle);
        grid.Children.Add(contentPanel);
        frame.Child = grid;

        frame.AddHandler(
            PointerPressedEvent,
            (_, e) => HandleRowPointerPressed(node, frame, e),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        _rowFrames[node] = frame;
        _titleEditors[node] = titleBox;
        _noteEditors[node] = noteBox;
        _noteFrames[node] = noteFrame;
        _dragHandles[node] = dragHandle;
        _itemsPanel.Children.Add(frame);
    }

    private Border CreateRowFrame(MindMapNode node, int level)
    {
        return new Border
        {
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Margin = new Thickness((level - 1) * IndentSize, 0, 0, 0),
            Padding = new Thickness(2, 1),
            MinHeight = 32,
            DataContext = node
        };
    }

    private static Grid CreateRowGrid()
    {
        return new Grid
        {
            ColumnDefinitions = new ColumnDefinitions($"{DragHandleColumnWidth},*")
        };
    }

    private Border CreateDragHandle(MindMapNode node, bool isRoot)
    {
        var handle = new Border
        {
            Width = DragHandleColumnWidth,
            MinHeight = 30,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Cursor = isRoot ? Cursor.Default : new Cursor(StandardCursorType.SizeAll),
            IsHitTestVisible = !isRoot,
            Child = isRoot ? null : CreateDragDot(node)
        };

        if (isRoot)
        {
            return handle;
        }

        AtomToolTip.SetTip(handle, DragNodeTip);
        handle.PointerPressed += (sender, e) => HandleDragHandlePointerPressed(node, sender as Control, e);
        handle.PointerEntered += (_, _) => SetHoveredDragHandleNode(node);
        handle.PointerExited += (_, _) =>
        {
            if (ReferenceEquals(_hoverDragHandleNode, node))
            {
                SetHoveredDragHandleNode(null);
            }
        };
        return handle;
    }

    private Control CreateDragDot(MindMapNode node)
    {
        var glow = new Border
        {
            Width = OutlineDotGlowSize,
            Height = OutlineDotGlowSize,
            CornerRadius = new CornerRadius(OutlineDotGlowSize / 2),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var dot = new Border
        {
            Width = OutlineDotSize,
            Height = OutlineDotSize,
            CornerRadius = new CornerRadius(OutlineDotSize / 2),
            Background = GetDragDotBrush(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var dotHost = new Grid
        {
            Width = DragHandleColumnWidth,
            MinHeight = 30
        };
        dotHost.Children.Add(glow);
        dotHost.Children.Add(dot);
        _dragHandleGlows[node] = glow;

        return dotHost;
    }

    private AtomTextBox CreateTitleEditor(MindMapNode node, bool isRoot)
    {
        var editor = new AtomTextBox
        {
            Margin = new Thickness(0, 1, 4, 1),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = GetPrimaryTextBrush(),
            PlaceholderForeground = GetPlaceholderTextBrush(),
            FontSize = isRoot ? 16 : 15,
            FontWeight = isRoot ? FontWeight.SemiBold : FontWeight.Regular,
            PlaceholderText = isRoot ? CenterTopicPlaceholder : TopicPlaceholder,
            AcceptsReturn = false,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinHeight = 28
        };
        editor.Text = node.Title;
        editor.PropertyChanged += (_, e) =>
        {
            if (e.Property == AtomTextBox.TextProperty && !string.Equals(node.Title, editor.Text, StringComparison.Ordinal))
            {
                node.Title = editor.Text ?? string.Empty;
            }
        };
        editor.GotFocus += (_, _) => SelectNode(node);
        editor.AddHandler(
            KeyDownEvent,
            (sender, e) => HandleTitleKeyDown(node, sender as AtomTextBox, e),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        return editor;
    }

    private AtomTextBox CreateNoteEditor(MindMapNode node)
    {
        var editor = new AtomTextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = GetSecondaryTextBrush(),
            PlaceholderForeground = GetPlaceholderTextBrush(),
            FontSize = MindMapLayoutMetrics.NoteFontSize,
            PlaceholderText = NotePlaceholder,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 26,
            MaxHeight = 94,
            Padding = new Thickness(0, 1, 0, 3),
            VerticalContentAlignment = VerticalAlignment.Top
        };
        editor.Text = node.Note;
        editor.PropertyChanged += (_, e) =>
        {
            if (e.Property == AtomTextBox.TextProperty && !string.Equals(node.Note, editor.Text, StringComparison.Ordinal))
            {
                node.Note = editor.Text ?? string.Empty;
            }
        };
        editor.GotFocus += (_, _) => SelectNode(node);
        editor.LostFocus += (_, _) => CollapseEmptyNoteEditor(node);
        editor.AddHandler(
            KeyDownEvent,
            (sender, e) => HandleNoteKeyDown(node, sender as AtomTextBox, e),
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        return editor;
    }

    private static Border CreateNoteFrame(AtomTextBox noteBox)
    {
        return new Border
        {
            Margin = new Thickness(0, 0, 4, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Child = noteBox
        };
    }

    private IBrush GetPrimaryTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#F9FAFB" : "#111827");
    }

    private IBrush GetSecondaryTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#9CA3AF" : "#6B7280");
    }

    private IBrush GetPlaceholderTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#94A3B8" : "#667085");
    }

    private IBrush GetDragDotBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#F8FAFC" : "#111111");
    }

    private void HandleNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MindMapNode node)
        {
            return;
        }

        if (e.PropertyName == nameof(MindMapNode.AccentColor))
        {
            Rebuild();
            FocusNode(node);
            return;
        }

        if (e.PropertyName == nameof(MindMapNode.Title))
        {
            UpdateEditorText(_titleEditors, node, node.Title);
        }
        else if (e.PropertyName == nameof(MindMapNode.Note))
        {
            UpdateEditorText(_noteEditors, node, node.Note);
            UpdateNoteVisibility(node);
        }
    }

    private static void UpdateEditorText(Dictionary<MindMapNode, AtomTextBox> editors, MindMapNode node, string text)
    {
        if (editors.TryGetValue(node, out var editor)
            && !string.Equals(editor.Text, text, StringComparison.Ordinal))
        {
            editor.Text = text;
        }
    }

    private void ScheduleRebuild()
    {
        if (_isRebuildQueued)
        {
            return;
        }

        _isRebuildQueued = true;
        Dispatcher.UIThread.Post(Rebuild, DispatcherPriority.Background);
    }

    private void TryFocusPendingNode()
    {
        if (_pendingFocusNode is null)
        {
            return;
        }

        if (_isRebuildQueued)
        {
            return;
        }

        if (!_titleEditors.TryGetValue(_pendingFocusNode, out var editor))
        {
            return;
        }

        _pendingFocusNode = null;
        editor.Focus();
        editor.CaretIndex = editor.Text?.Length ?? 0;
    }

    private void HandleTreeChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ScheduleRebuild();
    }

    private static bool IsTextResourceProperty(AvaloniaProperty property)
    {
        return property == CenterTopicPlaceholderProperty
               || property == TopicPlaceholderProperty
               || property == NotePlaceholderProperty
               || property == AddChildTextProperty
               || property == AddSiblingTextProperty
               || property == PromoteTextProperty
               || property == DemoteTextProperty
               || property == MoveUpTextProperty
               || property == MoveDownTextProperty
               || property == AddNoteTextProperty
               || property == EditNoteTextProperty
               || property == DeleteNodeTextProperty
               || property == DragNodeTipProperty;
    }

}

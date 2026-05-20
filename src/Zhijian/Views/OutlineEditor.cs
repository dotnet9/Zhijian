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
    private const string DefaultCenterTopicPlaceholder = "Center topic";
    private const string DefaultTopicPlaceholder = "Topic";
    private const string DefaultNotePlaceholder = "Note";
    private const string DefaultAddChildText = "Add child";
    private const string DefaultAddSiblingText = "Add sibling";
    private const string DefaultPromoteText = "Promote";
    private const string DefaultDemoteText = "Demote";
    private const string DefaultMoveUpText = "Move up";
    private const string DefaultMoveDownText = "Move down";
    private const string DefaultAddNoteText = "Add note";
    private const string DefaultEditNoteText = "Edit note";
    private const string DefaultDeleteNodeText = "Delete node";
    private const string DefaultDragNodeTip = "Drop in the middle to make it a child; drop near the edges to reorder siblings";
    private const string DefaultDropAsChildText = "Set as child";
    private const string DefaultDropBeforeText = "Insert above";
    private const string DefaultDropAfterText = "Insert below";

    private const double IndentSize = 24;
    private const double DragHandleColumnWidth = 28;
    private const double OutlineDotSize = 8;
    private const double OutlineDotGlowSize = 20;
    private const double DropEdgeRatio = 0.28;
    private const double DragStartDistance = 6;
    private const double DropPreviewLineThickness = 3;
    private const double DropPreviewLabelSpacing = 6;
    private const double DropPreviewLabelMinWidth = 72;
    private const double DropPreviewLabelMinHeight = 26;
    private const double DropPreviewLabelPaddingX = 8;
    private const double DropPreviewLabelPaddingY = 4;
    private const double DropPreviewLabelCornerRadius = 4;
    private const double DropPreviewLabelBorderThickness = 1;
    private const double DropPreviewLabelFontSize = 12;
    private const double DropPreviewLineLeftPadding = 4;
    private const double DropPreviewLineRightPadding = 8;
    private const double DropPreviewLineMinWidth = 48;
    private const double DropPreviewChildLabelOffsetX = 10;
    private const double DropPreviewOverlayPadding = 8;
    private const double DropPreviewLabelFallbackWidth = 96;
    private const double DropPreviewLabelFallbackHeight = 26;
    private const double OutlinePanelSpacing = 2;
    private const double OutlinePanelMarginX = 10;
    private const double OutlinePanelMarginY = 8;
    private const double RowFrameCornerRadius = 4;
    private const double RowFrameBorderThickness = 1;
    private const double RowFrameDropChildBorderThickness = 2;
    private const double RowFramePaddingX = 2;
    private const double RowFramePaddingY = 1;
    private const double RowFrameMinHeight = 32;
    private const double DragHandleMinHeight = 30;
    private const double TitleEditorMarginRight = 4;
    private const double TitleEditorMarginY = 1;
    private const double TitleEditorMinHeight = 28;
    private const double NoteEditorMinHeight = 26;
    private const double NoteEditorMaxHeight = 94;
    private const double NoteEditorPaddingBottom = 3;
    private const double NoteFrameMarginRight = 4;
    private const double NoteFrameMarginBottom = 4;
    private const int RebuildRowBatchSize = 80;

    private const string DropPreviewBoxShadow = "0 6 18 0 #22000000";
    private const string DropChildAccentBrush = "#22C55E";
    private const string DropSiblingAccentBrush = "#2563EB";
    private const string DropChildLabelBackgroundDark = "#064E3B";
    private const string DropChildLabelBackgroundLight = "#ECFDF5";
    private const string DropSiblingLabelBackgroundDark = "#172554";
    private const string DropSiblingLabelBackgroundLight = "#EFF6FF";
    private const string DropChildLabelForegroundDark = "#DCFCE7";
    private const string DropChildLabelForegroundLight = "#166534";
    private const string DropSiblingLabelForegroundDark = "#DBEAFE";
    private const string DropSiblingLabelForegroundLight = "#1D4ED8";
    private const string DropChildRowBackgroundDark = "#064E3B33";
    private const string DropChildRowBackgroundLight = "#ECFDF5";
    private const string DropSiblingRowBackgroundDark = "#1D4ED833";
    private const string DropSiblingRowBackgroundLight = "#EFF6FF";
    private const string SelectedRowBorderDark = "#64748B";
    private const string SelectedRowBorderLight = "#CBD5E1";
    private const string TransparentBrushText = "#00000000";
    private const string DragHandleGlowBackgroundDark = "#FFFFFF24";
    private const string DragHandleGlowBackgroundLight = "#00000018";
    private const string DragHandleActiveBorderDark = "#93C5FD";
    private const string DragHandleActiveBorderLight = "#2563EB";
    private const string DragHandleHoverBorderDark = "#FFFFFF33";
    private const string DragHandleHoverBorderLight = "#00000022";
    private const string PrimaryTextBrushDark = "#F9FAFB";
    private const string PrimaryTextBrushLight = "#111827";
    private const string SecondaryTextBrushDark = "#9CA3AF";
    private const string SecondaryTextBrushLight = "#6B7280";
    private const string PlaceholderTextBrushDark = "#94A3B8";
    private const string PlaceholderTextBrushLight = "#667085";
    private const string DragDotBrushDark = "#F8FAFC";
    private const string DragDotBrushLight = "#111111";

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
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(CenterTopicPlaceholder), DefaultCenterTopicPlaceholder);

    public static readonly StyledProperty<string> TopicPlaceholderProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(TopicPlaceholder), DefaultTopicPlaceholder);

    public static readonly StyledProperty<string> NotePlaceholderProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(NotePlaceholder), DefaultNotePlaceholder);

    public static readonly StyledProperty<string> AddChildTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(AddChildText), DefaultAddChildText);

    public static readonly StyledProperty<string> AddSiblingTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(AddSiblingText), DefaultAddSiblingText);

    public static readonly StyledProperty<string> PromoteTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(PromoteText), DefaultPromoteText);

    public static readonly StyledProperty<string> DemoteTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DemoteText), DefaultDemoteText);

    public static readonly StyledProperty<string> MoveUpTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(MoveUpText), DefaultMoveUpText);

    public static readonly StyledProperty<string> MoveDownTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(MoveDownText), DefaultMoveDownText);

    public static readonly StyledProperty<string> AddNoteTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(AddNoteText), DefaultAddNoteText);

    public static readonly StyledProperty<string> EditNoteTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(EditNoteText), DefaultEditNoteText);

    public static readonly StyledProperty<string> DeleteNodeTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DeleteNodeText), DefaultDeleteNodeText);

    public static readonly StyledProperty<string> DragNodeTipProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DragNodeTip), DefaultDragNodeTip);

    public static readonly StyledProperty<string> DropAsChildTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DropAsChildText), DefaultDropAsChildText);

    public static readonly StyledProperty<string> DropBeforeTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DropBeforeText), DefaultDropBeforeText);

    public static readonly StyledProperty<string> DropAfterTextProperty =
        AvaloniaProperty.Register<OutlineEditor, string>(nameof(DropAfterText), DefaultDropAfterText);

    private readonly StackPanel _itemsPanel = new()
    {
        Spacing = OutlinePanelSpacing,
        Margin = new Thickness(OutlinePanelMarginX, OutlinePanelMarginY)
    };

    private readonly ScrollViewer _scrollViewer;
    private readonly Canvas _dropPreviewOverlay = new()
    {
        IsHitTestVisible = false
    };
    private readonly Border _dropPreviewLine = new()
    {
        Height = DropPreviewLineThickness,
        CornerRadius = new CornerRadius(DropPreviewLineThickness / 2),
        IsHitTestVisible = false,
        IsVisible = false
    };
    private readonly TextBlock _dropPreviewText = new()
    {
        FontSize = DropPreviewLabelFontSize,
        FontWeight = FontWeight.SemiBold,
        TextWrapping = TextWrapping.NoWrap
    };
    private readonly Border _dropPreviewLabel;

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
        _scrollViewer = new ScrollViewer
        {
            Content = _itemsPanel,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        _dropPreviewLabel = CreateDropPreviewLabel();
        _dropPreviewOverlay.Children.Add(_dropPreviewLine);
        _dropPreviewOverlay.Children.Add(_dropPreviewLabel);
        _itemsPanel.PointerMoved += HandleDragMoved;
        _itemsPanel.PointerReleased += HandleDragReleased;

        Content = new Grid
        {
            Children =
            {
                _scrollViewer,
                _dropPreviewOverlay
            }
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

    public string DropAsChildText
    {
        get => GetValue(DropAsChildTextProperty);
        set => SetValue(DropAsChildTextProperty, value);
    }

    public string DropBeforeText
    {
        get => GetValue(DropBeforeTextProperty);
        set => SetValue(DropBeforeTextProperty, value);
    }

    public string DropAfterText
    {
        get => GetValue(DropAfterTextProperty);
        set => SetValue(DropAfterTextProperty, value);
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
        HideDropPreview();

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
            BorderThickness = new Thickness(RowFrameBorderThickness),
            CornerRadius = new CornerRadius(RowFrameCornerRadius),
            Background = Brushes.Transparent,
            Margin = new Thickness((level - 1) * IndentSize, 0, 0, 0),
            Padding = new Thickness(RowFramePaddingX, RowFramePaddingY),
            MinHeight = RowFrameMinHeight,
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
            MinHeight = DragHandleMinHeight,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(RowFrameBorderThickness),
            CornerRadius = new CornerRadius(RowFrameCornerRadius),
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
            BorderThickness = new Thickness(RowFrameBorderThickness),
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
            MinHeight = DragHandleMinHeight
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
            Margin = new Thickness(0, TitleEditorMarginY, TitleEditorMarginRight, TitleEditorMarginY),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = GetPrimaryTextBrush(),
            PlaceholderForeground = GetPlaceholderTextBrush(),
            FontSize = isRoot ? 16 : 15,
            FontWeight = isRoot ? FontWeight.SemiBold : FontWeight.Regular,
            PlaceholderText = isRoot ? CenterTopicPlaceholder : TopicPlaceholder,
            AcceptsReturn = false,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinHeight = TitleEditorMinHeight
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
            MinHeight = NoteEditorMinHeight,
            MaxHeight = NoteEditorMaxHeight,
            Padding = new Thickness(0, TitleEditorMarginY, 0, NoteEditorPaddingBottom),
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
            Margin = new Thickness(0, 0, NoteFrameMarginRight, NoteFrameMarginBottom),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Child = noteBox
        };
    }

    private IBrush GetPrimaryTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? PrimaryTextBrushDark : PrimaryTextBrushLight);
    }

    private IBrush GetSecondaryTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? SecondaryTextBrushDark : SecondaryTextBrushLight);
    }

    private IBrush GetPlaceholderTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? PlaceholderTextBrushDark : PlaceholderTextBrushLight);
    }

    private IBrush GetDragDotBrush()
    {
        return Brush.Parse(IsDarkTheme ? DragDotBrushDark : DragDotBrushLight);
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
               || property == DragNodeTipProperty
               || property == DropAsChildTextProperty
               || property == DropBeforeTextProperty
               || property == DropAfterTextProperty;
    }

}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CodeWF.MindView;
using AtomUI.Icons.AntDesign;
using Zhijian.ViewModels;
using AtomMenuFlyout = AtomUI.Desktop.Controls.MenuFlyout;
using AtomMenuItem = AtomUI.Desktop.Controls.MenuItem;
using AtomTextBox = AtomUI.Desktop.Controls.TextBox;

namespace Zhijian.Views;

public class OutlineEditor : UserControl
{
    public static readonly StyledProperty<ObservableCollection<MindMapNode>?> RootsProperty =
        AvaloniaProperty.Register<OutlineEditor, ObservableCollection<MindMapNode>?>(nameof(Roots));

    public static readonly StyledProperty<MindMapNode?> SelectedNodeProperty =
        AvaloniaProperty.Register<OutlineEditor, MindMapNode?>(
            nameof(SelectedNode),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsDarkThemeProperty =
        AvaloniaProperty.Register<OutlineEditor, bool>(nameof(IsDarkTheme));

    private const double IndentSize = 24;
    private const double DropEdgeRatio = 0.28;
    private const double DragStartDistance = 6;

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
    private readonly Dictionary<MindMapNode, Ellipse> _dragDots = [];
    private readonly List<MindMapNode> _observedNodes = [];
    private readonly List<INotifyCollectionChanged> _observedCollections = [];

    private MindMapNode? _dragNode;
    private MindMapNode? _dropTarget;
    private Control? _dragAnchor;
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

    public bool IsDarkTheme
    {
        get => GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
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
            CollapseEmptyNoteEditorsExcept(SelectedNode);
            ApplySelectionState();
        }
        else if (change.Property == IsDarkThemeProperty)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        DetachSubscriptions();
        _itemsPanel.Children.Clear();
        _rowFrames.Clear();
        _titleEditors.Clear();
        _noteEditors.Clear();
        _noteFrames.Clear();
        _dragDots.Clear();

        if (Roots is null)
        {
            return;
        }

        Roots.CollectionChanged += HandleTreeChanged;
        _observedCollections.Add(Roots);

        foreach (var root in Roots)
        {
            WatchNode(root);
            AddNodeRow(root, 1);
        }

        ApplySelectionState();
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
        var frame = new Border
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

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("18,*")
        };

        var dot = new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = isRoot ? Brushes.Transparent : Brush.Parse("#111111"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = isRoot ? Cursor.Default : new Cursor(StandardCursorType.SizeAll),
            IsHitTestVisible = !isRoot
        };

        if (!isRoot)
        {
            ToolTip.SetTip(dot, "拖到节点中部成为子节点，拖到上下边缘成为同级节点");
            dot.PointerPressed += (sender, e) => HandleDotPointerPressed(node, sender as Control, e);
        }

        var contentPanel = new StackPanel
        {
            Spacing = 3
        };

        var titleBox = new AtomTextBox
        {
            Margin = new Thickness(0, 1, 4, 1),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = GetPrimaryTextBrush(),
            PlaceholderForeground = GetPlaceholderTextBrush(),
            FontSize = isRoot ? 16 : 15,
            FontWeight = isRoot ? FontWeight.SemiBold : FontWeight.Regular,
            PlaceholderText = isRoot ? "中心主题" : "主题",
            AcceptsReturn = false,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinHeight = 28
        };
        titleBox.Bind(AtomTextBox.TextProperty, new Binding(nameof(MindMapNode.Title))
        {
            Source = node,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        titleBox.GotFocus += (_, _) => SelectNode(node);
        titleBox.AddHandler(
            KeyDownEvent,
            (sender, e) => HandleTitleKeyDown(node, sender as AtomTextBox, e),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        var noteBox = new AtomTextBox
        {
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = GetSecondaryTextBrush(),
            PlaceholderForeground = GetPlaceholderTextBrush(),
            FontSize = 12,
            PlaceholderText = "备注",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 26,
            MaxHeight = 94,
            Padding = new Thickness(8, 4),
            VerticalContentAlignment = VerticalAlignment.Top
        };
        noteBox.Bind(AtomTextBox.TextProperty, new Binding(nameof(MindMapNode.Note))
        {
            Source = node,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        noteBox.GotFocus += (_, _) => SelectNode(node);
        noteBox.LostFocus += (_, _) => CollapseEmptyNoteEditor(node);
        noteBox.AddHandler(
            KeyDownEvent,
            (sender, e) => HandleNoteKeyDown(node, sender as AtomTextBox, e),
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        var noteFrame = new Border
        {
            Margin = new Thickness(0, 0, 4, 4),
            BorderThickness = new Thickness(2, 0, 0, 0),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(0),
            Child = noteBox
        };

        contentPanel.Children.Add(titleBox);
        contentPanel.Children.Add(noteFrame);
        Grid.SetColumn(contentPanel, 1);

        grid.Children.Add(dot);
        grid.Children.Add(contentPanel);
        frame.Child = grid;

        frame.PointerPressed += (_, e) =>
        {
            if (e.Source is AtomTextBox or Ellipse)
            {
                return;
            }

            SelectNode(node);
        };

        _rowFrames[node] = frame;
        _titleEditors[node] = titleBox;
        _noteEditors[node] = noteBox;
        _noteFrames[node] = noteFrame;
        _dragDots[node] = dot;
        _itemsPanel.Children.Add(frame);

        foreach (var child in node.Children)
        {
            AddNodeRow(child, level + 1);
        }
    }

    private void HandleTitleKeyDown(MindMapNode node, AtomTextBox? editor, KeyEventArgs e)
    {
        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            var nextNode = viewModel.HandleOutlineEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            var changed = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? viewModel.PromoteNode(node)
                : viewModel.DemoteNode(node);

            if (changed)
            {
                FocusNode(node);
            }

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

    private void HandleDotPointerPressed(MindMapNode node, Control? control, PointerPressedEventArgs e)
    {
        var viewModel = ViewModel;
        if (control is null
            || viewModel is null
            || viewModel.IsRoot(node))
        {
            return;
        }

        var point = e.GetCurrentPoint(control);
        SelectNode(node);
        if (point.Properties.IsRightButtonPressed)
        {
            ShowNodeMenu(node, control);
            e.Handled = true;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _dragNode = node;
        _dropTarget = null;
        _dragAnchor = control;
        _dragStartPointer = e.GetPosition(_itemsPanel);
        // 短按圆点打开菜单，移动超过阈值后才进入拖拽，避免菜单和拖拽互相抢事件。
        _isDraggingNode = false;
        e.Pointer.Capture(_itemsPanel);
        e.Handled = true;
        ApplySelectionState();
    }

    private void HandleDragMoved(object? sender, PointerEventArgs e)
    {
        if (_dragNode is null)
        {
            return;
        }

        var point = e.GetPosition(_itemsPanel);
        var delta = point - _dragStartPointer;
        if (!_isDraggingNode && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) < DragStartDistance)
        {
            return;
        }

        _isDraggingNode = true;
        var viewModel = ViewModel;
        MindMapNode? nextTarget = null;
        var nextPlacement = MindMapDropPlacement.Child;

        foreach (var (node, frame) in _rowFrames)
        {
            if (viewModel?.CanMoveNode(_dragNode, node) != true)
            {
                continue;
            }

            var bounds = frame.Bounds;
            if (point.Y < bounds.Top || point.Y > bounds.Bottom)
            {
                continue;
            }

            nextTarget = node;
            nextPlacement = GetDropPlacement(bounds, point);
            break;
        }

        if (!ReferenceEquals(nextTarget, _dropTarget) || nextPlacement != _dropPlacement)
        {
            _dropTarget = nextTarget;
            _dropPlacement = nextPlacement;
            ApplySelectionState();
        }

        e.Handled = true;
    }

    private void HandleDragReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragNode is null)
        {
            return;
        }

        var dragNode = _dragNode;
        var dropTarget = _dropTarget;
        var dropPlacement = _dropPlacement;
        var dragAnchor = _dragAnchor;
        var wasDragging = _isDraggingNode;

        _dragNode = null;
        _dropTarget = null;
        _dragAnchor = null;
        _isDraggingNode = false;
        e.Pointer.Capture(null);

        if (!wasDragging)
        {
            ShowNodeMenu(dragNode, dragAnchor);
        }
        else if (dropTarget is not null && ViewModel?.MoveNode(dragNode, dropTarget, dropPlacement) == true)
        {
            FocusNode(dragNode);
        }

        ApplySelectionState();
        e.Handled = true;
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

    private void ShowNodeMenu(MindMapNode node, Control? anchor)
    {
        anchor ??= _rowFrames.TryGetValue(node, out var frame) ? frame : null;
        if (anchor is null)
        {
            return;
        }

        var menu = new AtomMenuFlyout();
        var noteItem = new AtomMenuItem
        {
            Header = "备注",
            Icon = new CommentOutlined { Width = 14, Height = 14 }
        };
        noteItem.Click += (_, _) => ShowNoteEditor(node);
        menu.Items.Add(noteItem);

        var deleteItem = new AtomMenuItem
        {
            Header = "删除",
            Icon = new DeleteOutlined { Width = 14, Height = 14 },
            IsEnabled = ViewModel?.IsRoot(node) != true
        };
        deleteItem.Click += (_, _) => DeleteNodeFromMenu(node);
        menu.Items.Add(deleteItem);
        menu.ShowAt(anchor);
    }

    private void ShowNoteEditor(MindMapNode node)
    {
        // 备注与标题共用 MindMapNode，显示策略由编辑状态和实际内容共同决定。
        _editingNoteNodes.Add(node);
        SelectNode(node);
        UpdateNoteVisibility(node);
        Dispatcher.UIThread.Post(() =>
        {
            if (_noteEditors.TryGetValue(node, out var editor))
            {
                editor.Focus();
                editor.CaretIndex = editor.Text?.Length ?? 0;
            }
        });
    }

    private void DeleteNodeFromMenu(MindMapNode node)
    {
        var focusTarget = ViewModel?.DeleteNode(node);
        FocusNode(focusTarget);
    }

    private void CollapseEmptyNoteEditor(MindMapNode node)
    {
        if (!string.IsNullOrWhiteSpace(node.Note))
        {
            return;
        }

        _editingNoteNodes.Remove(node);
        UpdateNoteVisibility(node);
    }

    private void CollapseEmptyNoteEditorsExcept(MindMapNode? nodeToKeep)
    {
        foreach (var node in _editingNoteNodes.ToList())
        {
            if (!ReferenceEquals(node, nodeToKeep) && string.IsNullOrWhiteSpace(node.Note))
            {
                _editingNoteNodes.Remove(node);
                UpdateNoteVisibility(node);
            }
        }
    }

    private void UpdateNoteVisibility(MindMapNode node)
    {
        if (!_noteFrames.TryGetValue(node, out var frame))
        {
            return;
        }

        frame.IsVisible = _editingNoteNodes.Contains(node) || !string.IsNullOrWhiteSpace(node.Note);
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

    private void ApplySelectionState()
    {
        foreach (var (node, frame) in _rowFrames)
        {
            var selected = ReferenceEquals(node, SelectedNode);
            var isDropTarget = ReferenceEquals(node, _dropTarget);

            frame.Background = Brushes.Transparent;
            frame.BorderBrush = Brush.Parse(isDropTarget
                ? _dropPlacement == MindMapDropPlacement.Child ? "#22C55E" : "#2563EB"
                : selected ? IsDarkTheme ? "#64748B" : "#CBD5E1" : "#00000000");

            if (_noteFrames.TryGetValue(node, out var noteFrame))
            {
                UpdateNoteVisibility(node);
                noteFrame.Background = Brush.Parse(IsDarkTheme ? "#111827" : "#F8FAFC");
                noteFrame.BorderBrush = Brush.Parse(selected ? "#148BFF" : IsDarkTheme ? "#475569" : "#CBD5E1");
            }
        }

        foreach (var (node, dot) in _dragDots)
        {
            if (ViewModel?.IsRoot(node) == true)
            {
                continue;
            }

            dot.Fill = Brush.Parse(ReferenceEquals(node, _dragNode) ? "#2563EB" : IsDarkTheme ? "#CBD5E1" : "#111111");
        }
    }

    private void HandleNoteKeyDown(MindMapNode node, AtomTextBox? editor, KeyEventArgs e)
    {
        if (e.Key is not (Key.Back or Key.Delete)
            || !string.IsNullOrWhiteSpace(editor?.Text))
        {
            return;
        }

        node.Note = string.Empty;
        _editingNoteNodes.Remove(node);
        UpdateNoteVisibility(node);
        FocusNode(node);
        e.Handled = true;
    }

    private IBrush GetPrimaryTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#F9FAFB" : "#111827");
    }

    private IBrush GetSecondaryTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#CBD5E1" : "#475467");
    }

    private IBrush GetPlaceholderTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#94A3B8" : "#667085");
    }

    private void HandleNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is MindMapNode node && e.PropertyName == nameof(MindMapNode.AccentColor))
        {
            Rebuild();
            FocusNode(node);
            return;
        }

        if (e.PropertyName == nameof(MindMapNode.Note))
        {
            if (sender is MindMapNode noteNode)
            {
                UpdateNoteVisibility(noteNode);
            }
        }
    }

    private void HandleTreeChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Rebuild();
    }
}

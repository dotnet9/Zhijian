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
using Zhijian.Desktop.Models;
using Zhijian.Desktop.ViewModels;
using AtomTextBox = AtomUI.Desktop.Controls.TextBox;

namespace Zhijian.Desktop.Views;

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

    private readonly StackPanel _itemsPanel = new()
    {
        Spacing = 2,
        Margin = new Thickness(10, 8)
    };

    private readonly Dictionary<MindMapNode, Border> _rowFrames = [];
    private readonly Dictionary<MindMapNode, AtomTextBox> _titleEditors = [];
    private readonly Dictionary<MindMapNode, Ellipse> _dragDots = [];
    private readonly List<MindMapNode> _observedNodes = [];
    private readonly List<INotifyCollectionChanged> _observedCollections = [];

    private MindMapNode? _dragNode;
    private MindMapNode? _dropTarget;
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
        Grid.SetColumn(titleBox, 1);

        grid.Children.Add(dot);
        grid.Children.Add(titleBox);
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
            || viewModel.IsRoot(node)
            || !e.GetCurrentPoint(control).Properties.IsLeftButtonPressed)
        {
            return;
        }

        SelectNode(node);
        _dragNode = node;
        _dropTarget = null;
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

        _dragNode = null;
        _dropTarget = null;
        e.Pointer.Capture(null);

        if (dropTarget is not null && ViewModel?.MoveNode(dragNode, dropTarget, dropPlacement) == true)
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

    private IBrush GetPrimaryTextBrush()
    {
        return Brush.Parse(IsDarkTheme ? "#F9FAFB" : "#111827");
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
        }
    }

    private void HandleTreeChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Rebuild();
    }
}

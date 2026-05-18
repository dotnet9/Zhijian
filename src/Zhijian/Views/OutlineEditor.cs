using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
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
            ColumnDefinitions = new ColumnDefinitions("20,*")
        };

        var dot = new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = isRoot ? Brushes.Transparent : Brush.Parse("#111111"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 14, 0, 0),
            Cursor = isRoot ? Cursor.Default : new Cursor(StandardCursorType.SizeAll),
            IsHitTestVisible = !isRoot
        };

        if (!isRoot)
        {
            AtomToolTip.SetTip(dot, "拖到节点中部成为子节点，拖到上下边缘成为同级节点");
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
        titleBox.Text = node.Title;
        titleBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == AtomTextBox.TextProperty && !string.Equals(node.Title, titleBox.Text, StringComparison.Ordinal))
            {
                node.Title = titleBox.Text ?? string.Empty;
            }
        };
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
            FontSize = MindMapLayoutMetrics.NoteFontSize,
            PlaceholderText = "备注",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 26,
            MaxHeight = 94,
            Padding = new Thickness(0, 1, 0, 3),
            VerticalContentAlignment = VerticalAlignment.Top
        };
        noteBox.Text = node.Note;
        noteBox.PropertyChanged += (_, e) =>
        {
            if (e.Property == AtomTextBox.TextProperty && !string.Equals(node.Note, noteBox.Text, StringComparison.Ordinal))
            {
                node.Note = noteBox.Text ?? string.Empty;
            }
        };
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
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
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

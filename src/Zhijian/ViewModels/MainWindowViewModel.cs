using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using AtomUI.Controls;
using Avalonia;
using CodeWF.MindView;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zhijian.Services;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IMindMapEditorController
{
    private const double HorizontalGap = MindMapLayoutMetrics.DefaultHorizontalSpacing;
    private const double VerticalSpacing = MindMapLayoutMetrics.DefaultVerticalSpacing;
    private const double RootX = 72;
    private const double RootY = 72;
    private const double MinNodeY = 24;
    private const int MaxHistorySteps = 80;

    private static readonly string[] Palette =
    [
        "#235C9B",
        "#47A878",
        "#E69B38",
        "#7A5AF8",
        "#D94D68",
        "#1F8A9B",
        "#9B6A23",
        "#4A6FD9"
    ];

    private readonly IMindMapFileService _fileService;
    private readonly IApplicationActionService _applicationActionService;
    private readonly HashSet<MindMapNode> _observedNodes = [];
    private readonly List<HistoryEntry> _history = [];
    private int _nextPaletteIndex;
    private int _historyIndex = -1;
    private bool _isApplyingMarkdown;
    private bool _isSyncingMarkdownFromTree;
    private bool _isRestoringHistory;

    [ObservableProperty]
    private MindMapNode? _selectedNode;

    [ObservableProperty]
    private bool _isMarkdownMode;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _markdownText = string.Empty;

    [ObservableProperty]
    private string _statusText = "就绪";

    public MainWindowViewModel()
        : this(new DisabledMindMapFileService(), new DisabledApplicationActionService())
    {
    }

    public MainWindowViewModel(IMindMapFileService fileService)
        : this(fileService, new DisabledApplicationActionService())
    {
    }

    public MainWindowViewModel(
        IMindMapFileService fileService,
        IApplicationActionService applicationActionService)
    {
        _fileService = fileService;
        _applicationActionService = applicationActionService;
        Roots = new ObservableCollection<MindMapNode>
        {
            CreateNode(
                "枝见",
                new MindMapNode(
                    "产品愿景",
                    new MindMapNode("Markdown 默认存储"),
                    new MindMapNode("三种编辑视图")),
                new MindMapNode(
                    "编辑体验",
                    new MindMapNode("树形编辑"),
                    new MindMapNode("图形编辑"),
                    new MindMapNode("双视图")),
                new MindMapNode(
                    "导入导出",
                    new MindMapNode("XMind"),
                    new MindMapNode("OPML"),
                    new MindMapNode("Markdown")))
        };

        WatchTree();
        AssignMissingColors(Root);
        IsDarkTheme = Application.Current?.IsDarkThemeMode() ?? false;
        SelectedNode = Root;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("初始内容");
    }

    public ObservableCollection<MindMapNode> Roots { get; }

    public MindMapNode Root => Roots[0];

    public bool IsOutlineMode => !IsMarkdownMode;

    public string EditorPaneTitle => IsMarkdownMode ? "Markdown" : "大纲";

    public string ToggleEditorToolTip => IsMarkdownMode ? "切换到大纲视图" : "切换到 Markdown 视图";

    public string SelectedNodeSummary => SelectedNode is null
        ? $"{NodeCount} 个节点"
        : $"{NodeCount} 个节点 · 第 {GetLevel(SelectedNode)} 层 · {SelectedNode.Children.Count} 个子节点";

    public int NodeCount => FlattenNodes().Count;

    public bool CanUndo => _historyIndex > 0;

    public bool CanRedo => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

    public string HistorySummary => _history.Count == 0 ? "步骤 0/0" : $"步骤 {_historyIndex + 1}/{_history.Count}";

    public string ShellBackground => IsDarkTheme ? "#111827" : "#F3F6FA";

    public string PanelBackground => IsDarkTheme ? "#1F2937" : "#FFFFFF";

    public string PanelFooterBackground => IsDarkTheme ? "#111827" : "#F8FAFC";

    public string PanelBorderBrush => IsDarkTheme ? "#374151" : "#DDE3EA";

    public string TitleBarBackground => IsDarkTheme ? "#111827" : "#FFFFFF";

    public string PrimaryTextBrush => IsDarkTheme ? "#F9FAFB" : "#0F172A";

    public string SecondaryTextBrush => IsDarkTheme ? "#CBD5E1" : "#667085";

    [RelayCommand]
    public void ToggleEditorMode()
    {
        if (IsMarkdownMode)
        {
            ApplyMarkdownToTree(refreshMarkdownText: false);
            IsMarkdownMode = false;
            StatusText = "已应用 Markdown 并切换到大纲视图";
            return;
        }

        SyncMarkdownFromTree();
        IsMarkdownMode = true;
        StatusText = "已切换到 Markdown 视图";
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        _historyIndex--;
        RestoreHistoryEntry(_history[_historyIndex]);
        StatusText = $"已后退：{_history[_historyIndex].Label}";
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        _historyIndex++;
        RestoreHistoryEntry(_history[_historyIndex]);
        StatusText = $"已前进：{_history[_historyIndex].Label}";
    }

    public bool IsRoot(MindMapNode? node)
    {
        return Roots.Count > 0 && ReferenceEquals(node, Root);
    }

    public int GetLevel(MindMapNode node)
    {
        return Roots.Count == 0 ? 0 : GetLevel(Root, node, 1);
    }

    public IReadOnlyList<MindMapNode> FlattenNodes()
    {
        var nodes = new List<MindMapNode>();
        foreach (var root in Roots)
        {
            AppendFlattened(root, nodes);
        }

        return nodes;
    }

    public MindMapNode HandleOutlineEnter(MindMapNode node)
    {
        if (IsRoot(node) || node.Children.Count > 0)
        {
            return AddChild(node, string.Empty);
        }

        return AddSibling(node, string.Empty);
    }

    public MindMapNode HandleMapEnter(MindMapNode node)
    {
        return IsRoot(node) ? AddChild(node, string.Empty) : AddSibling(node, string.Empty);
    }

    public MindMapNode HandleMapTab(MindMapNode node)
    {
        return AddChild(node, string.Empty);
    }

    public MindMapNode AddChild(MindMapNode? parent, string title = "新主题")
    {
        parent ??= SelectedNode ?? Root;

        var child = CreateNode(title);
        parent.Children.Add(child);
        SelectedNode = child;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("添加子主题");
        StatusText = "已添加子主题";
        return child;
    }

    public MindMapNode AddSibling(MindMapNode? node, string title = "新主题")
    {
        node ??= SelectedNode ?? Root;
        if (IsRoot(node))
        {
            return AddChild(node, title);
        }

        var parent = FindParent(node) ?? Root;
        var sibling = CreateNode(title);
        var index = parent.Children.IndexOf(node);
        parent.Children.Insert(index + 1, sibling);
        SelectedNode = sibling;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("添加同级主题");
        StatusText = "已添加同级主题";
        return sibling;
    }

    public MindMapNode DeleteNode(MindMapNode? node)
    {
        node ??= SelectedNode ?? Root;
        if (IsRoot(node))
        {
            SelectedNode = Root;
            return Root;
        }

        var parent = FindParent(node) ?? Root;
        var index = parent.Children.IndexOf(node);
        var focusTarget = index > 0 ? parent.Children[index - 1] : parent;

        parent.Children.Remove(node);
        SelectedNode = focusTarget;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("删除主题");
        StatusText = "已删除主题";
        return focusTarget;
    }

    public bool DemoteNode(MindMapNode? node)
    {
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
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("降级主题");
        StatusText = "已降级为子主题";
        return true;
    }

    public bool CanDemoteNode(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && parent.Children.IndexOf(node) > 0;
    }

    public bool PromoteNode(MindMapNode? node)
    {
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
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("升级主题");
        StatusText = "已提升为父级主题";
        return true;
    }

    public bool CanMoveNodeUp(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && parent.Children.IndexOf(node) > 0;
    }

    public bool MoveNodeUp(MindMapNode? node)
    {
        return MoveNodeWithinSiblings(node, -1, "上移主题", "已上移主题");
    }

    public bool CanMoveNodeDown(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
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

    public bool MoveNodeDown(MindMapNode? node)
    {
        return MoveNodeWithinSiblings(node, 1, "下移主题", "已下移主题");
    }

    public bool CanPromoteNode(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && !IsRoot(parent) && FindParent(parent) is not null;
    }

    public bool CanMoveNode(MindMapNode? node, MindMapNode? target)
    {
        return node is not null
            && target is not null
            && !IsRoot(node)
            && !ReferenceEquals(node, target)
            && !IsDescendant(node, target);
    }

    public bool MoveNode(MindMapNode? node, MindMapNode? target, MindMapDropPlacement placement)
    {
        if (!CanMoveNode(node, target) || node is null || target is null)
        {
            return false;
        }

        var oldParent = FindParent(node);
        if (oldParent is null)
        {
            return false;
        }

        if (IsRoot(target) && placement is MindMapDropPlacement.Before or MindMapDropPlacement.After)
        {
            placement = MindMapDropPlacement.Child;
        }

        var newParent = placement == MindMapDropPlacement.Child
            ? target
            : FindParent(target) ?? Root;
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
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("移动主题");
        StatusText = "已移动主题";
        return true;
    }

    private bool MoveNodeWithinSiblings(MindMapNode? node, int offset, string historyLabel, string statusText)
    {
        if (node is null || IsRoot(node))
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
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep(historyLabel);
        StatusText = statusText;
        return true;
    }

    [RelayCommand]
    private void OpenWebsite()
    {
        _applicationActionService.OpenWebsite();
        StatusText = "已打开网站";
    }

    [RelayCommand]
    private void ShowChangelog()
    {
        _applicationActionService.ShowChangelog();
        StatusText = "已打开更新日志";
    }

    [RelayCommand]
    private void ShowAbout()
    {
        _applicationActionService.ShowAbout();
        StatusText = "已打开关于窗口";
    }

    [RelayCommand]
    private void ShowThanks()
    {
        _applicationActionService.ShowThanks();
        StatusText = "已打开感谢窗口";
    }

    [RelayCommand]
    private void OpenRepository()
    {
        _applicationActionService.OpenRepository();
        StatusText = "已打开 GitHub 仓库";
    }

    [RelayCommand]
    private async Task ImportMarkdownAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenTextAsync(MindMapFileFormat.Markdown);
            if (content is null)
            {
                return;
            }

            ReplaceTree(MindMapDocumentCodec.FromMarkdown(content), "导入 Markdown");
            StatusText = "已导入 Markdown";
        });
    }

    [RelayCommand]
    private async Task ImportOpmlAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenTextAsync(MindMapFileFormat.Opml);
            if (content is null)
            {
                return;
            }

            ReplaceTree(MindMapDocumentCodec.FromOpml(content), "导入 OPML");
            StatusText = "已导入 OPML";
        });
    }

    [RelayCommand]
    private async Task ImportXMindAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenBinaryAsync(MindMapFileFormat.XMind);
            if (content is null)
            {
                return;
            }

            ReplaceTree(MindMapDocumentCodec.FromXMind(content), "导入 XMind");
            StatusText = "已导入 XMind";
        });
    }

    [RelayCommand]
    private async Task ExportMarkdownAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = IsMarkdownMode ? MarkdownText : MindMapDocumentCodec.ToMarkdown(Root);
            await _fileService.SaveTextAsync(MindMapFileFormat.Markdown, content);
            StatusText = "已导出 Markdown";
        });
    }

    [RelayCommand]
    private async Task ExportOpmlAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            ApplyMarkdownEditsIfNeeded();
            await _fileService.SaveTextAsync(MindMapFileFormat.Opml, MindMapDocumentCodec.ToOpml(Root));
            StatusText = "已导出 OPML";
        });
    }

    [RelayCommand]
    private async Task ExportXMindAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            ApplyMarkdownEditsIfNeeded();
            await _fileService.SaveBinaryAsync(MindMapFileFormat.XMind, MindMapDocumentCodec.ToXMind(Root));
            StatusText = "已导出 XMind";
        });
    }

    private void AutoLayout()
    {
        if (Roots.Count == 0)
        {
            return;
        }

        var columnPositions = CalculateColumnPositions(Root);
        var nextTop = RootY;
        LayoutNode(Root, 0, columnPositions, ref nextTop);
    }

    partial void OnIsMarkdownModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsOutlineMode));
        OnPropertyChanged(nameof(EditorPaneTitle));
        OnPropertyChanged(nameof(ToggleEditorToolTip));
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        Application.Current?.SetDarkThemeMode(value);
        OnPropertyChanged(nameof(ShellBackground));
        OnPropertyChanged(nameof(PanelBackground));
        OnPropertyChanged(nameof(PanelFooterBackground));
        OnPropertyChanged(nameof(PanelBorderBrush));
        OnPropertyChanged(nameof(TitleBarBackground));
        OnPropertyChanged(nameof(PrimaryTextBrush));
        OnPropertyChanged(nameof(SecondaryTextBrush));
    }

    partial void OnSelectedNodeChanged(MindMapNode? value)
    {
        OnPropertyChanged(nameof(SelectedNodeSummary));
    }

    partial void OnMarkdownTextChanged(string value)
    {
        if (_isSyncingMarkdownFromTree || _isApplyingMarkdown || !IsMarkdownMode)
        {
            return;
        }

        ApplyMarkdownToTree(refreshMarkdownText: false);
    }

    private async Task RunFileOperationAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            StatusText = exception.Message;
        }
    }

    private void ApplyMarkdownEditsIfNeeded()
    {
        if (IsMarkdownMode)
        {
            ApplyMarkdownToTree(refreshMarkdownText: false);
        }
    }

    private void ReplaceTree(MindMapNode root, string historyLabel)
    {
        try
        {
            _isApplyingMarkdown = true;
            Roots.Clear();
            Roots.Add(root);
            AssignMissingColors(root);
            SelectedNode = root;
            AutoLayout();
            RefreshTreeSummary();
        }
        finally
        {
            _isApplyingMarkdown = false;
            SyncMarkdownFromTree();
        }

        RecordHistoryStep(historyLabel);
    }

    private static void AppendFlattened(MindMapNode node, List<MindMapNode> nodes)
    {
        nodes.Add(node);
        foreach (var child in node.Children)
        {
            AppendFlattened(child, nodes);
        }
    }

    private static int GetLevel(MindMapNode current, MindMapNode target, int level)
    {
        if (ReferenceEquals(current, target))
        {
            return level;
        }

        foreach (var child in current.Children)
        {
            var childLevel = GetLevel(child, target, level + 1);
            if (childLevel > 0)
            {
                return childLevel;
            }
        }

        return 0;
    }

    private void WatchTree()
    {
        Roots.CollectionChanged += HandleChildrenChanged;
        foreach (var root in Roots)
        {
            WatchNode(root);
        }
    }

    private void WatchNode(MindMapNode node)
    {
        if (!_observedNodes.Add(node))
        {
            return;
        }

        node.PropertyChanged += HandleNodePropertyChanged;
        node.Children.CollectionChanged += HandleChildrenChanged;

        foreach (var child in node.Children)
        {
            WatchNode(child);
        }
    }

    private void UnwatchNode(MindMapNode node)
    {
        if (!_observedNodes.Remove(node))
        {
            return;
        }

        node.PropertyChanged -= HandleNodePropertyChanged;
        node.Children.CollectionChanged -= HandleChildrenChanged;

        foreach (var child in node.Children)
        {
            UnwatchNode(child);
        }
    }

    private void HandleNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MindMapNode.Title) or nameof(MindMapNode.Note))
        {
            AutoLayout();
            RefreshTreeSummary();
        }

        if (e.PropertyName is nameof(MindMapNode.Title) or nameof(MindMapNode.Note))
        {
            SyncMarkdownFromTree();
            RecordHistoryStep("编辑主题");
        }
    }

    private void HandleChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (MindMapNode node in e.OldItems)
            {
                UnwatchNode(node);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (MindMapNode node in e.NewItems)
            {
                AssignMissingColors(node);
                WatchNode(node);
            }
        }

        SyncMarkdownFromTree();
        RefreshTreeSummary();
    }

    private MindMapNode? FindParent(MindMapNode node)
    {
        return Roots.Count == 0 ? null : FindParent(Root, node);
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

    private void RefreshTreeSummary()
    {
        OnPropertyChanged(nameof(NodeCount));
        OnPropertyChanged(nameof(SelectedNodeSummary));
    }

    private static double[] CalculateColumnPositions(MindMapNode root)
    {
        // 每列先按真实节点宽度估算，避免第 4 级以后长标题与连线挤在一起。
        var columnWidths = new List<double>();
        CollectColumnWidths(root, 0, columnWidths);

        var columnPositions = new double[columnWidths.Count];
        var x = RootX;
        for (var i = 0; i < columnPositions.Length; i++)
        {
            columnPositions[i] = x;
            x += columnWidths[i] + HorizontalGap;
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
            : columnPositions[^1] + (depth - columnPositions.Count + 1) * (MindMapLayoutMetrics.LeafMaxWidth + HorizontalGap);

        if (node.Children.Count == 0)
        {
            node.Y = nextTop;
            nextTop = node.Y + size.Height + VerticalSpacing;
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
        node.Y = Math.Max(MinNodeY, nodeCenter - size.Height / 2);
        subtreeTop = Math.Min(subtreeTop, node.Y);
        subtreeBottom = Math.Max(subtreeBottom, node.Y + size.Height);
        nextTop = Math.Max(nextTop, subtreeBottom + VerticalSpacing);
        return new LayoutResult(node.Y + size.Height / 2, subtreeTop, subtreeBottom);
    }

    private MindMapNode CreateNode(string title, params MindMapNode[] children)
    {
        var node = new MindMapNode(title, children)
        {
            AccentColor = NextColor()
        };

        AssignMissingColors(node);
        return node;
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
        return Palette[_nextPaletteIndex++ % Palette.Length];
    }

    private void SyncMarkdownFromTree()
    {
        if (_isSyncingMarkdownFromTree || _isApplyingMarkdown || Roots.Count == 0)
        {
            return;
        }

        try
        {
            _isSyncingMarkdownFromTree = true;
            MarkdownText = MindMapDocumentCodec.ToMarkdown(Root);
        }
        finally
        {
            _isSyncingMarkdownFromTree = false;
        }
    }

    private void ApplyMarkdownToTree(bool refreshMarkdownText = true)
    {
        try
        {
            _isApplyingMarkdown = true;
            var root = MindMapDocumentCodec.FromMarkdown(MarkdownText);

            Roots.Clear();
            Roots.Add(root);
            AssignMissingColors(root);
            SelectedNode = root;
            AutoLayout();
            RefreshTreeSummary();
        }
        finally
        {
            _isApplyingMarkdown = false;
            if (refreshMarkdownText)
            {
                SyncMarkdownFromTree();
            }
        }

        RecordHistoryStep("编辑 Markdown");
    }

    private void RecordHistoryStep(string label)
    {
        if (_isRestoringHistory || Roots.Count == 0)
        {
            return;
        }

        var snapshot = MindMapDocumentCodec.ToMarkdown(Root);
        if (_historyIndex >= 0 && _history[_historyIndex].Snapshot == snapshot)
        {
            return;
        }

        if (_historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        }

        _history.Add(new HistoryEntry(label, snapshot));
        if (_history.Count > MaxHistorySteps)
        {
            _history.RemoveAt(0);
        }

        _historyIndex = _history.Count - 1;
        RefreshHistoryState();
    }

    private void RestoreHistoryEntry(HistoryEntry entry)
    {
        try
        {
            _isRestoringHistory = true;
            _isApplyingMarkdown = true;
            var root = MindMapDocumentCodec.FromMarkdown(entry.Snapshot);

            Roots.Clear();
            Roots.Add(root);
            AssignMissingColors(root);
            SelectedNode = root;
            AutoLayout();
            RefreshTreeSummary();
        }
        finally
        {
            _isApplyingMarkdown = false;
            _isRestoringHistory = false;
            SyncMarkdownFromTree();
            RefreshHistoryState();
        }
    }

    private void RefreshHistoryState()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(HistorySummary));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private readonly record struct LayoutResult(double CenterY, double Top, double Bottom);

    private sealed record HistoryEntry(string Label, string Snapshot);
}

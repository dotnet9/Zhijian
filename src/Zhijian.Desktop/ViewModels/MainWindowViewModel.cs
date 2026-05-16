using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zhijian.Desktop.Models;
using Zhijian.Desktop.Services;

namespace Zhijian.Desktop.ViewModels;

public enum MindMapDropPlacement
{
    Before,
    After,
    Child
}

public partial class MainWindowViewModel : ViewModelBase
{
    private const double HorizontalSpacing = 160;
    private const double VerticalSpacing = 66;
    private const double RootX = 72;

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
    private readonly HashSet<MindMapNode> _observedNodes = [];
    private int _nextPaletteIndex;
    private bool _isApplyingMarkdown;
    private bool _isSyncingMarkdownFromTree;

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
        : this(new DisabledMindMapFileService())
    {
    }

    public MainWindowViewModel(IMindMapFileService fileService)
    {
        _fileService = fileService;
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
        IsDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        SelectedNode = Root;
        AutoLayout();
        SyncMarkdownFromTree();
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

    [RelayCommand]
    public void ToggleEditorMode()
    {
        if (IsMarkdownMode)
        {
            ApplyMarkdownToTree(refreshMarkdownText: false);
            IsMarkdownMode = false;
            return;
        }

        SyncMarkdownFromTree();
        IsMarkdownMode = true;
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
        return focusTarget;
    }

    public bool DemoteNode(MindMapNode? node)
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
        if (index <= 0)
        {
            return false;
        }

        var newParent = parent.Children[index - 1];
        parent.Children.RemoveAt(index);
        newParent.Children.Add(node);
        SelectedNode = node;
        AutoLayout();
        SyncMarkdownFromTree();
        return true;
    }

    public bool PromoteNode(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        if (parent is null || IsRoot(parent))
        {
            return false;
        }

        var grandParent = FindParent(parent);
        if (grandParent is null)
        {
            return false;
        }

        parent.Children.Remove(node);
        var parentIndex = grandParent.Children.IndexOf(parent);
        grandParent.Children.Insert(parentIndex + 1, node);
        SelectedNode = node;
        AutoLayout();
        SyncMarkdownFromTree();
        return true;
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
        return true;
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

            ReplaceTree(MindMapDocumentCodec.FromMarkdown(content));
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

            ReplaceTree(MindMapDocumentCodec.FromOpml(content));
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

            ReplaceTree(MindMapDocumentCodec.FromXMind(content));
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

        var leafIndex = 0;
        LayoutNode(Root, 0, ref leafIndex);
    }

    partial void OnIsMarkdownModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsOutlineMode));
        OnPropertyChanged(nameof(EditorPaneTitle));
        OnPropertyChanged(nameof(ToggleEditorToolTip));
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = value ? ThemeVariant.Dark : ThemeVariant.Light;
        }
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

    private void ReplaceTree(MindMapNode root)
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
            SyncMarkdownFromTree();
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

    private static double LayoutNode(MindMapNode node, int depth, ref int leafIndex)
    {
        node.X = RootX + depth * HorizontalSpacing;

        if (node.Children.Count == 0)
        {
            node.Y = 72 + leafIndex * VerticalSpacing;
            leafIndex++;
            return node.Y;
        }

        var first = 0d;
        var last = 0d;
        for (var i = 0; i < node.Children.Count; i++)
        {
            var childY = LayoutNode(node.Children[i], depth + 1, ref leafIndex);
            if (i == 0)
            {
                first = childY;
            }

            last = childY;
        }

        node.Y = (first + last) / 2;
        return node.Y;
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
    }
}

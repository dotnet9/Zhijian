using System.Collections.Specialized;
using System.ComponentModel;
using CodeWF.MindView;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IMindMapEditorController
{
    private void MarkDocumentDirty()
    {
        if (_isLoadingDocument || _isRestoringHistory)
        {
            return;
        }

        IsDirty = true;
    }

    private void MarkDocumentClean()
    {
        IsDirty = false;
    }

    private void ResetHistory(string label, string? snapshot = null)
    {
        _history.Clear();
        _historyIndex = -1;
        RecordHistoryStep(label, snapshot);
        RefreshHistoryState();
    }

    private void ApplyMarkdownEditsIfNeeded()
    {
        if (IsMarkdownMode)
        {
            ApplyMarkdownToTree(refreshMarkdownText: false);
        }
    }

    private void ReplaceTree(
        MindMapNode root,
        string historyLabel,
        string? markdownSnapshot = null,
        bool recordHistory = true)
    {
        try
        {
            _isApplyingMarkdown = true;
            PrepareRootForDisplay(root);
            Roots.Clear();
            Roots.Add(root);
            SelectedNode = root;
            RefreshTreeSummary();
            if (markdownSnapshot is not null)
            {
                MarkdownText = markdownSnapshot;
            }
        }
        finally
        {
            _isApplyingMarkdown = false;
            if (markdownSnapshot is null)
            {
                SyncMarkdownFromTree();
            }
        }

        if (recordHistory)
        {
            RecordHistoryStep(historyLabel, markdownSnapshot);
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
            AutoLayout();
            RefreshTreeSummary();
        }

        if (e.PropertyName is nameof(MindMapNode.Title) or nameof(MindMapNode.Note))
        {
            SyncMarkdownFromTree();
            RecordHistoryStep(T(ZhijianL.HistoryEditTopic));
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
        RefreshSelectedNodeCommands();
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

    private MindMapNode CreateBlankRoot()
    {
        return CreateNode(string.Empty);
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
            PrepareRootForDisplay(root);

            Roots.Clear();
            Roots.Add(root);
            SelectedNode = root;
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

        RecordHistoryStep(T(ZhijianL.HistoryEditMarkdown));
    }

    private void RecordHistoryStep(string label, string? snapshot = null)
    {
        if (_isRestoringHistory || Roots.Count == 0)
        {
            return;
        }

        snapshot ??= MindMapDocumentCodec.ToMarkdown(Root);
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
        MarkDocumentDirty();
    }

    private void RestoreHistoryEntry(HistoryEntry entry)
    {
        try
        {
            _isRestoringHistory = true;
            _isApplyingMarkdown = true;
            var root = MindMapDocumentCodec.FromMarkdown(entry.Snapshot);
            PrepareRootForDisplay(root);

            Roots.Clear();
            Roots.Add(root);
            SelectedNode = root;
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
    }

    private void PrepareRootForDisplay(MindMapNode root)
    {
        AssignMissingColors(root);
        MindMapTreeLayout.Arrange(new[] { root });
    }

    private void RefreshSelectedNodeCommands()
    {
        OnPropertyChanged(nameof(CanPromoteSelectedNode));
        OnPropertyChanged(nameof(CanDemoteSelectedNode));
        OnPropertyChanged(nameof(CanMoveSelectedNodeUp));
        OnPropertyChanged(nameof(CanMoveSelectedNodeDown));
        OnPropertyChanged(nameof(CanDeleteSelectedNode));
        OnPropertyChanged(nameof(CanAddSiblingToSelectedNode));
    }

    private sealed record LoadedMindMapDocument(
        MindMapNode Root,
        MindMapFileFormat Format,
        string MarkdownSnapshot);

    private sealed record HistoryEntry(string Label, string Snapshot);
}

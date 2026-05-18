using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using AtomUI.Theme.Language;
using Avalonia;
using CodeWF.MindView;
using Lang.Avalonia;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IMindMapEditorController
{
    private void AutoLayout()
    {
        MindMapTreeLayout.Arrange(Roots);
    }

    public void AddSiblingToSelected()
    {
        AddSibling(SelectedNode);
    }

    public void AddChildToSelected()
    {
        AddChild(SelectedNode);
    }

    public void PromoteSelected()
    {
        PromoteNode(SelectedNode);
    }

    public void DemoteSelected()
    {
        DemoteNode(SelectedNode);
    }

    public void MoveSelectedUp()
    {
        MoveNodeUp(SelectedNode);
    }

    public void MoveSelectedDown()
    {
        MoveNodeDown(SelectedNode);
    }

    public void DeleteSelected()
    {
        DeleteNode(SelectedNode);
    }

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

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        _historyIndex--;
        RestoreHistoryEntry(_history[_historyIndex]);
        StatusText = $"已后退：{_history[_historyIndex].Label}";
    }

    public void Redo()
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

}

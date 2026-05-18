using AtomUI.Icons.AntDesign;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using CodeWF.MindView;
using AtomMenuFlyout = AtomUI.Desktop.Controls.MenuFlyout;
using AtomMenuItem = AtomUI.Desktop.Controls.MenuItem;
using AtomTextBox = AtomUI.Desktop.Controls.TextBox;

namespace Zhijian.Views;

public partial class OutlineEditor
{
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
        if (IsRightPointerPressed(point.Properties))
        {
            ShowNodeMenu(node, control);
            e.Handled = true;
            return;
        }

        if (!IsLeftPointerPressed(point.Properties))
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

    private static bool IsRightPointerPressed(PointerPointProperties properties)
    {
        return properties.IsRightButtonPressed
            || properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed;
    }

    private static bool IsLeftPointerPressed(PointerPointProperties properties)
    {
        return properties.IsLeftButtonPressed
            || properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;
    }

    private void ShowNodeMenu(MindMapNode node, Control? anchor)
    {
        anchor ??= _rowFrames.TryGetValue(node, out var frame) ? frame : null;
        if (anchor is null)
        {
            return;
        }

        var viewModel = ViewModel;
        var menu = new AtomMenuFlyout();
        menu.Items.Add(CreateNodeMenuItem(
            "添加子级",
            new SubnodeOutlined { Width = 14, Height = 14 },
            "Tab",
            true,
            () => AddChildFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            "添加同级",
            new SisternodeOutlined { Width = 14, Height = 14 },
            "Enter",
            viewModel?.IsRoot(node) != true,
            () => AddSiblingFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            "提升为父节点",
            new MenuFoldOutlined { Width = 14, Height = 14 },
            "Shift+Tab",
            viewModel?.CanPromoteNode(node) == true,
            () => PromoteNodeFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            "降级为子节点",
            new MenuUnfoldOutlined { Width = 14, Height = 14 },
            "Tab",
            viewModel?.CanDemoteNode(node) == true,
            () => DemoteNodeFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            "上移",
            new ArrowUpOutlined { Width = 14, Height = 14 },
            "Alt+Up",
            viewModel?.CanMoveNodeUp(node) == true,
            () => MoveNodeUpFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            "下移",
            new ArrowDownOutlined { Width = 14, Height = 14 },
            "Alt+Down",
            viewModel?.CanMoveNodeDown(node) == true,
            () => MoveNodeDownFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            string.IsNullOrWhiteSpace(node.Note) ? "添加备注" : "编辑备注",
            new CommentOutlined { Width = 14, Height = 14 },
            null,
            true,
            () => ShowNoteEditor(node)));
        menu.Items.Add(CreateNodeMenuItem(
            "删除",
            new DeleteOutlined { Width = 14, Height = 14 },
            "Delete",
            viewModel?.IsRoot(node) != true,
            () => DeleteNodeFromMenu(node)));
        menu.ShowAt(anchor);
    }

    private static AtomMenuItem CreateNodeMenuItem(string header, PathIcon? icon, string? inputGesture, bool isEnabled, Action action)
    {
        var item = new AtomMenuItem
        {
            Header = header,
            IsEnabled = isEnabled
        };
        if (icon is not null)
        {
            item.Icon = icon;
        }

        if (!string.IsNullOrWhiteSpace(inputGesture))
        {
            item.InputGesture = KeyGesture.Parse(inputGesture);
        }

        item.Click += (_, _) => action();
        return item;
    }

    private void AddChildFromMenu(MindMapNode node)
    {
        var child = ViewModel?.AddChild(node);
        FocusNode(child);
    }

    private void AddSiblingFromMenu(MindMapNode node)
    {
        var sibling = ViewModel?.AddSibling(node);
        FocusNode(sibling);
    }

    private void PromoteNodeFromMenu(MindMapNode node)
    {
        if (ViewModel?.PromoteNode(node) == true)
        {
            FocusNode(node);
        }
    }

    private void DemoteNodeFromMenu(MindMapNode node)
    {
        if (ViewModel?.DemoteNode(node) == true)
        {
            FocusNode(node);
        }
    }

    private void MoveNodeUpFromMenu(MindMapNode node)
    {
        if (ViewModel?.MoveNodeUp(node) == true)
        {
            FocusNode(node);
        }
    }

    private void MoveNodeDownFromMenu(MindMapNode node)
    {
        if (ViewModel?.MoveNodeDown(node) == true)
        {
            FocusNode(node);
        }
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
                noteFrame.Background = Brushes.Transparent;
                noteFrame.BorderBrush = Brushes.Transparent;
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
}

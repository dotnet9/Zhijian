using AtomUI.Icons.AntDesign;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodeWF.MindView;
using AtomMenuFlyout = AtomUI.Desktop.Controls.MenuFlyout;
using AtomMenuItem = AtomUI.Desktop.Controls.MenuItem;
using AtomTextBox = AtomUI.Desktop.Controls.TextBox;

namespace Zhijian.Views;

public partial class OutlineEditor
{
    private void HandleTitleKeyDown(MindMapNode node, AtomTextBox? editor, KeyEventArgs e)
    {
        if (ReferenceEquals(_lastHandledEditorKeyEvent, e))
        {
            return;
        }

        var viewModel = ViewModel;
        if (viewModel is null)
        {
            return;
        }

        if (TryHandleTitleNavigation(node, editor, e))
        {
            return;
        }

        var action = MindMapKeyboardGestureRouter.ResolveTitleAction(
            e.Key,
            e.KeyModifiers,
            string.IsNullOrWhiteSpace(editor?.Text));

        switch (action)
        {
            case MindMapKeyboardAction.AddFromEnter:
                FocusNode(viewModel.HandleMapEnter(node));
                MarkEditorKeyEventHandled(e);
                return;

            case MindMapKeyboardAction.Promote:
                if (viewModel.PromoteNode(node))
                {
                    FocusNode(node);
                }
                MarkEditorKeyEventHandled(e);
                return;

            case MindMapKeyboardAction.Demote:
                FocusNode(viewModel.HandleMapTab(node));
                MarkEditorKeyEventHandled(e);
                return;

            case MindMapKeyboardAction.MoveUp:
                if (viewModel.MoveNodeUp(node))
                {
                    FocusNode(node);
                }
                MarkEditorKeyEventHandled(e);
                return;

            case MindMapKeyboardAction.MoveDown:
                if (viewModel.MoveNodeDown(node))
                {
                    FocusNode(node);
                }
                MarkEditorKeyEventHandled(e);
                return;

            case MindMapKeyboardAction.DeleteEmptyTitle:
                if (!viewModel.IsRoot(node))
                {
                    FocusNode(viewModel.DeleteNode(node));
                    MarkEditorKeyEventHandled(e);
                }
                return;
        }
    }

    private void HandleDragHandlePointerPressed(MindMapNode node, Control? control, PointerPressedEventArgs e)
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
        HideDropPreview();
        e.Pointer.Capture(_itemsPanel);
        e.Handled = true;
        ApplySelectionState();
    }

    private void HandleRowPointerPressed(MindMapNode node, Control anchor, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(anchor);
        if (IsRightPointerPressed(point.Properties))
        {
            SelectNode(node);
            ShowNodeMenu(node, anchor);
            e.Handled = true;
            return;
        }

        if (e.Source is AtomTextBox || HasVisualAncestor<AtomTextBox>(e.Source))
        {
            return;
        }

        if (IsLeftPointerPressed(point.Properties))
        {
            SelectNode(node);
        }
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
            if (viewModel?.IsRoot(node) == true
                && nextPlacement is MindMapDropPlacement.Before or MindMapDropPlacement.After)
            {
                nextPlacement = MindMapDropPlacement.Child;
            }
            break;
        }

        if (!ReferenceEquals(nextTarget, _dropTarget) || nextPlacement != _dropPlacement)
        {
            _dropTarget = nextTarget;
            _dropPlacement = nextPlacement;
            if (nextTarget is null)
            {
                HideDropPreview();
            }
            else
            {
                ShowDropPreview(nextTarget, nextPlacement);
            }
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
        _dropPlacement = MindMapDropPlacement.Child;
        _isDraggingNode = false;
        HideDropPreview();
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

    private Border CreateDropPreviewLabel()
    {
        return new Border
        {
            MinWidth = 72,
            MinHeight = 26,
            Padding = new Thickness(8, 4),
            CornerRadius = new CornerRadius(4),
            BorderThickness = new Thickness(1),
            BoxShadow = BoxShadows.Parse("0 6 18 0 #22000000"),
            Child = _dropPreviewText,
            IsHitTestVisible = false,
            IsVisible = false
        };
    }

    private void ShowDropPreview(MindMapNode target, MindMapDropPlacement placement)
    {
        if (!_rowFrames.TryGetValue(target, out var frame))
        {
            HideDropPreview();
            return;
        }

        var translated = frame.TranslatePoint(new Point(0, 0), _dropPreviewOverlay);
        if (translated is not { } topLeft)
        {
            HideDropPreview();
            return;
        }

        var bounds = new Rect(topLeft, frame.Bounds.Size);
        ApplyDropPreviewStyle(placement);
        ShowDropPreviewLine(bounds, placement);
        ShowDropPreviewLabel(GetDropPlacementText(placement), bounds, placement);
    }

    private void HideDropPreview()
    {
        _dropPreviewLine.IsVisible = false;
        _dropPreviewLabel.IsVisible = false;
        _dropPreviewText.Text = string.Empty;
    }

    private void ShowDropPreviewLine(Rect bounds, MindMapDropPlacement placement)
    {
        if (placement == MindMapDropPlacement.Child)
        {
            _dropPreviewLine.IsVisible = false;
            return;
        }

        var y = placement == MindMapDropPlacement.Before
            ? bounds.Top
            : bounds.Bottom;
        var lineLeft = bounds.Left + DragHandleColumnWidth + 4;
        var lineWidth = Math.Max(48, bounds.Right - lineLeft - 8);

        _dropPreviewLine.Width = lineWidth;
        _dropPreviewLine.Height = DropPreviewLineThickness;
        Canvas.SetLeft(_dropPreviewLine, lineLeft);
        Canvas.SetTop(_dropPreviewLine, y - DropPreviewLineThickness / 2);
        _dropPreviewLine.IsVisible = true;
    }

    private void ShowDropPreviewLabel(string text, Rect bounds, MindMapDropPlacement placement)
    {
        _dropPreviewText.Text = text;
        _dropPreviewLabel.IsVisible = true;
        PositionDropPreviewLabel(bounds, placement);
    }

    private void ApplyDropPreviewStyle(MindMapDropPlacement placement)
    {
        var isChild = placement == MindMapDropPlacement.Child;
        var accent = Brush.Parse(isChild ? "#22C55E" : "#2563EB");
        _dropPreviewLine.Background = accent;
        _dropPreviewLabel.BorderBrush = accent;
        _dropPreviewLabel.Background = Brush.Parse(isChild
            ? IsDarkTheme ? "#064E3B" : "#ECFDF5"
            : IsDarkTheme ? "#172554" : "#EFF6FF");
        _dropPreviewText.Foreground = Brush.Parse(isChild
            ? IsDarkTheme ? "#DCFCE7" : "#166534"
            : IsDarkTheme ? "#DBEAFE" : "#1D4ED8");
    }

    private string GetDropPlacementText(MindMapDropPlacement placement)
    {
        return placement switch
        {
            MindMapDropPlacement.Before => DropBeforeText,
            MindMapDropPlacement.After => DropAfterText,
            _ => DropAsChildText
        };
    }

    private void PositionDropPreviewLabel(Rect bounds, MindMapDropPlacement placement)
    {
        var labelSize = MeasureDropPreviewLabel();
        var x = placement == MindMapDropPlacement.Child
            ? bounds.Left + DragHandleColumnWidth + 10
            : bounds.Left + bounds.Width / 2 - labelSize.Width / 2;
        var y = placement switch
        {
            MindMapDropPlacement.Before => bounds.Top - labelSize.Height - DropPreviewLabelSpacing,
            MindMapDropPlacement.After => bounds.Bottom + DropPreviewLabelSpacing,
            _ => bounds.Top + bounds.Height / 2 - labelSize.Height / 2
        };

        var overlayWidth = GetFiniteSize(_dropPreviewOverlay.Bounds.Width, _itemsPanel.Bounds.Width);
        var overlayHeight = GetFiniteSize(_dropPreviewOverlay.Bounds.Height, _itemsPanel.Bounds.Height);
        x = Math.Clamp(x, 8, Math.Max(8, overlayWidth - labelSize.Width - 8));
        y = Math.Clamp(y, 8, Math.Max(8, overlayHeight - labelSize.Height - 8));
        Canvas.SetLeft(_dropPreviewLabel, x);
        Canvas.SetTop(_dropPreviewLabel, y);
    }

    private Size MeasureDropPreviewLabel()
    {
        _dropPreviewLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = _dropPreviewLabel.DesiredSize;
        return new Size(
            size.Width > 0 ? size.Width : 96,
            size.Height > 0 ? size.Height : 26);
    }

    private static double GetFiniteSize(double preferred, double fallback)
    {
        if (!double.IsNaN(preferred) && !double.IsInfinity(preferred) && preferred > 0)
        {
            return preferred;
        }

        return !double.IsNaN(fallback) && !double.IsInfinity(fallback) && fallback > 0 ? fallback : 1000;
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
            AddChildText,
            new SubnodeOutlined { Width = 14, Height = 14 },
            null,
            true,
            () => AddChildFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            AddSiblingText,
            new SisternodeOutlined { Width = 14, Height = 14 },
            "Enter",
            viewModel?.IsRoot(node) != true,
            () => AddSiblingFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            PromoteText,
            new MenuFoldOutlined { Width = 14, Height = 14 },
            "Shift+Tab",
            viewModel?.CanPromoteNode(node) == true,
            () => PromoteNodeFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            DemoteText,
            new MenuUnfoldOutlined { Width = 14, Height = 14 },
            "Tab",
            viewModel?.CanDemoteNode(node) == true,
            () => DemoteNodeFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            MoveUpText,
            new ArrowUpOutlined { Width = 14, Height = 14 },
            "Alt+Up",
            viewModel?.CanMoveNodeUp(node) == true,
            () => MoveNodeUpFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            MoveDownText,
            new ArrowDownOutlined { Width = 14, Height = 14 },
            "Alt+Down",
            viewModel?.CanMoveNodeDown(node) == true,
            () => MoveNodeDownFromMenu(node)));
        menu.Items.Add(CreateNodeMenuItem(
            string.IsNullOrWhiteSpace(node.Note) ? AddNoteText : EditNoteText,
            new CommentOutlined { Width = 14, Height = 14 },
            null,
            true,
            () => ShowNoteEditor(node)));
        menu.Items.Add(CreateNodeMenuItem(
            DeleteNodeText,
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

    private void SetHoveredDragHandleNode(MindMapNode? node)
    {
        if (ReferenceEquals(_hoverDragHandleNode, node))
        {
            return;
        }

        _hoverDragHandleNode = node;
        ApplySelectionState();
    }

    private void FocusNode(MindMapNode? node)
    {
        if (node is null)
        {
            return;
        }

        _pendingFocusNode = node;
        SetCurrentValue(SelectedNodeProperty, node);
        Dispatcher.UIThread.Post(TryFocusPendingNode);
    }

    private void ApplySelectionState()
    {
        foreach (var (node, frame) in _rowFrames)
        {
            var selected = ReferenceEquals(node, SelectedNode);
            var isDropTarget = ReferenceEquals(node, _dropTarget);
            var dropAsChild = isDropTarget && _dropPlacement == MindMapDropPlacement.Child;

            frame.Background = isDropTarget
                ? Brush.Parse(dropAsChild
                    ? IsDarkTheme ? "#064E3B33" : "#ECFDF5"
                    : IsDarkTheme ? "#1D4ED833" : "#EFF6FF")
                : Brushes.Transparent;
            frame.BorderThickness = dropAsChild ? new Thickness(2) : new Thickness(1);
            frame.BorderBrush = Brush.Parse(isDropTarget
                ? dropAsChild ? "#22C55E" : "#2563EB"
                : selected ? IsDarkTheme ? "#64748B" : "#CBD5E1" : "#00000000");

            if (_noteFrames.TryGetValue(node, out var noteFrame))
            {
                UpdateNoteVisibility(node);
                noteFrame.Background = Brushes.Transparent;
                noteFrame.BorderBrush = Brushes.Transparent;
            }
        }

        foreach (var (node, handle) in _dragHandles)
        {
            if (ViewModel?.IsRoot(node) == true)
            {
                continue;
            }

            var active = ReferenceEquals(node, _dragNode);
            var hovered = ReferenceEquals(node, _hoverDragHandleNode);
            handle.Background = Brushes.Transparent;
            handle.BorderBrush = Brushes.Transparent;

            if (_dragHandleGlows.TryGetValue(node, out var glow))
            {
                glow.Background = active || hovered
                    ? Brush.Parse(IsDarkTheme ? "#FFFFFF24" : "#00000018")
                    : Brushes.Transparent;
                glow.BorderBrush = active
                    ? Brush.Parse(IsDarkTheme ? "#93C5FD" : "#2563EB")
                    : hovered
                        ? Brush.Parse(IsDarkTheme ? "#FFFFFF33" : "#00000022")
                        : Brushes.Transparent;
            }
        }
    }

    private static bool HasVisualAncestor<T>(object? source)
        where T : Visual
    {
        return FindVisualAncestor<T>(source) is not null;
    }

    private static T? FindVisualAncestor<T>(object? source)
        where T : Visual
    {
        if (source is T sourceMatch)
        {
            return sourceMatch;
        }

        if (source is not Visual visual)
        {
            return null;
        }

        for (var current = visual; current is not null; current = current.GetVisualParent())
        {
            if (current is T ancestorMatch)
            {
                return ancestorMatch;
            }
        }

        return null;
    }

    private void HandleNoteKeyDown(MindMapNode node, AtomTextBox? editor, KeyEventArgs e)
    {
        if (ReferenceEquals(_lastHandledEditorKeyEvent, e))
        {
            return;
        }

        var action = MindMapKeyboardGestureRouter.ResolveNoteAction(
            e.Key,
            string.IsNullOrWhiteSpace(editor?.Text));
        if (action != MindMapKeyboardAction.DeleteEmptyNote)
        {
            return;
        }

        node.Note = string.Empty;
        _editingNoteNodes.Remove(node);
        UpdateNoteVisibility(node);
        FocusNode(node);
        MarkEditorKeyEventHandled(e);
    }

    private bool TryHandleTitleNavigation(MindMapNode node, AtomTextBox? editor, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None)
        {
            return false;
        }

        var target = e.Key switch
        {
            Key.Up => FindAdjacentOutlineNode(node, -1),
            Key.Down => FindAdjacentOutlineNode(node, 1),
            Key.Left when IsCaretAtStart(editor) => FindOutlineParent(node),
            Key.Right when IsCaretAtEnd(editor) => node.Children.FirstOrDefault(),
            _ => null
        };

        if (target is null)
        {
            return false;
        }

        FocusNode(target);
        MarkEditorKeyEventHandled(e);
        return true;
    }

    private MindMapNode? FindAdjacentOutlineNode(MindMapNode node, int offset)
    {
        var nodes = GetOutlineNodes();
        var index = nodes.IndexOf(node);
        var targetIndex = index + offset;
        return index >= 0 && targetIndex >= 0 && targetIndex < nodes.Count
            ? nodes[targetIndex]
            : null;
    }

    private List<MindMapNode> GetOutlineNodes()
    {
        var nodes = new List<MindMapNode>();
        if (Roots is null)
        {
            return nodes;
        }

        foreach (var root in Roots)
        {
            CollectOutlineNodes(root, nodes);
        }

        return nodes;
    }

    private static void CollectOutlineNodes(MindMapNode node, ICollection<MindMapNode> nodes)
    {
        nodes.Add(node);
        foreach (var child in node.Children)
        {
            CollectOutlineNodes(child, nodes);
        }
    }

    private MindMapNode? FindOutlineParent(MindMapNode node)
    {
        if (Roots is null)
        {
            return null;
        }

        foreach (var root in Roots)
        {
            var parent = FindOutlineParent(root, node);
            if (parent is not null)
            {
                return parent;
            }
        }

        return null;
    }

    private static MindMapNode? FindOutlineParent(MindMapNode parent, MindMapNode node)
    {
        if (parent.Children.Contains(node))
        {
            return parent;
        }

        foreach (var child in parent.Children)
        {
            var match = FindOutlineParent(child, node);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static bool IsCaretAtStart(AtomTextBox? editor)
    {
        return editor is null || editor.CaretIndex <= 0;
    }

    private static bool IsCaretAtEnd(AtomTextBox? editor)
    {
        return editor is null || editor.CaretIndex >= (editor.Text?.Length ?? 0);
    }

    private void MarkEditorKeyEventHandled(KeyEventArgs e)
    {
        _lastHandledEditorKeyEvent = e;
        e.Handled = true;
    }
}

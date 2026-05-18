using CodeWF.MindView;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CodeWF.MindView.Controls;

public partial class MindMapEditor
{
    private void HandleTitleKeyDown(MindMapNode node, TextBox? editor, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var nextNode = HandleMapEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            var nextNode = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? node
                : HandleMapTab(node);

            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                PromoteNode(node);
            }

            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if ((e.Key == Key.Delete || e.Key == Key.Back)
            && string.IsNullOrWhiteSpace(editor?.Text)
            && !IsRootNode(node))
        {
            var focusTarget = DeleteNode(node);
            FocusNode(focusTarget);
            e.Handled = true;
        }
    }

    private void HandleNoteKeyDown(MindMapNode node, TextBox? editor, KeyEventArgs e)
    {
        if (e.Key is not (Key.Back or Key.Delete)
            || !string.IsNullOrWhiteSpace(editor?.Text))
        {
            return;
        }

        node.Note = string.Empty;
        _editingNoteNodes.Remove(node);
        UpdateNoteEditorVisibility(node);
        FocusNode(node);
        e.Handled = true;
    }

    private void HandleFrameKeyDown(MindMapNode node, KeyEventArgs e)
    {
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            var focusTarget = DeleteNode(node);
            FocusFrame(focusTarget);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            var nextNode = HandleMapEnter(node);
            FocusNode(nextNode);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (PromoteNode(node))
                {
                    FocusNode(node);
                }
            }
            else
            {
                var nextNode = HandleMapTab(node);
                FocusNode(nextNode);
            }

            e.Handled = true;
        }
    }

    private void HandleNodeDragStarted(MindMapNode node, Control? control, PointerPressedEventArgs e)
    {
        if (control is null
            || IsRootNode(node)
            || !IsLeftPointerPressed(e.GetCurrentPoint(control).Properties))
        {
            return;
        }

        SelectNode(node);
        _dragNode = node;
        _dragStartPointer = e.GetPosition(_canvas);
        _isDraggingNode = false;
        _dropTarget = null;
        _dropPlacement = MindMapDropPlacement.Child;
        e.Pointer.Capture(control);
        e.Handled = true;
    }

    private void HandleNodeDragged(object? sender, PointerEventArgs e)
    {
        if (_dragNode is null)
        {
            return;
        }

        var current = e.GetPosition(_canvas);
        var delta = current - _dragStartPointer;
        if (!_isDraggingNode && Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y) < DragStartDistance)
        {
            return;
        }

        _isDraggingNode = true;
        UpdateDropTarget(_dragNode, current);

        ApplySelectionState();
        e.Handled = true;
    }

    private void HandleNodeDragCompleted(object? sender, PointerReleasedEventArgs e)
    {
        if (_dragNode is null)
        {
            return;
        }

        var dragNode = _dragNode;
        var dropTarget = _dropTarget;
        var dropPlacement = _dropPlacement;
        var wasDragging = _isDraggingNode;

        _dragNode = null;
        _dropTarget = null;
        _dropPlacement = MindMapDropPlacement.Child;
        _isDraggingNode = false;
        HideDropPreview();
        e.Pointer.Capture(null);

        if (dropTarget is not null
            && MoveNode(dragNode, dropTarget, dropPlacement))
        {
            FocusNode(dragNode);
        }
        else if (!wasDragging)
        {
            FocusNode(dragNode);
        }

        ApplySelectionState();
        e.Handled = true;
    }

    private void HandleCanvasPanStarted(object? sender, PointerPressedEventArgs e)
    {
        if (_isPanningCanvas
            || _dragNode is not null
            || !_isSpacePressed
            || !IsCanvasPanSource(e.Source)
            || !e.GetCurrentPoint(_scrollViewer).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isPanningCanvas = true;
        _panStartPointer = e.GetPosition(_scrollViewer);
        _panStartOffset = _scrollViewer.Offset;
        _canvas.Cursor = new Cursor(StandardCursorType.SizeAll);
        _scrollViewer.Cursor = new Cursor(StandardCursorType.SizeAll);
        e.Pointer.Capture(_scrollViewer);
        e.Handled = true;
    }

    private void HandleCanvasPanned(object? sender, PointerEventArgs e)
    {
        if (!_isPanningCanvas)
        {
            return;
        }

        var current = e.GetPosition(_scrollViewer);
        var delta = current - _panStartPointer;
        _scrollViewer.Offset = ClampScrollOffset(new Vector(
            _panStartOffset.X - delta.X,
            _panStartOffset.Y - delta.Y));
        UpdateViewportBounds();
        e.Handled = true;
    }

    private void HandleCanvasPanCompleted(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPanningCanvas)
        {
            return;
        }

        StopCanvasPan();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void StopCanvasPan()
    {
        _isPanningCanvas = false;
        _canvas.Cursor = new Cursor(StandardCursorType.Hand);
        _scrollViewer.Cursor = Cursor.Default;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Space || HasVisualAncestor<TextBox>(e.Source))
        {
            return;
        }

        _isSpacePressed = true;
    }

    private void HandleKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _isSpacePressed = false;
        }
    }

    private void HandlePointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!HasZoomModifier(e.KeyModifiers))
        {
            return;
        }

        if (Math.Abs(e.Delta.Y) < double.Epsilon)
        {
            return;
        }

        var factor = e.Delta.Y > 0 ? ZoomFactor : 1 / ZoomFactor;
        SetZoom(_zoomScale * factor);
        e.Handled = true;
    }

    private static bool HasZoomModifier(KeyModifiers modifiers)
    {
        var commandModifier = OperatingSystem.IsMacOS()
            ? KeyModifiers.Meta
            : KeyModifiers.Control;
        return modifiers.HasFlag(commandModifier);
    }

    public void ZoomOut()
    {
        SetZoom(_zoomScale / ZoomFactor);
    }

    public void ZoomIn()
    {
        SetZoom(_zoomScale * ZoomFactor);
    }

    public void ResetZoom()
    {
        SetZoom(1);
    }

    public void CenterRoot()
    {
        var root = Roots?.FirstOrDefault();
        if (root is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() => CenterNode(root), DispatcherPriority.Loaded);
    }

    public void CenterViewportAt(Point canvasPoint)
    {
        var offset = new Vector(
            canvasPoint.X * _zoomScale - _scrollViewer.Viewport.Width / 2,
            canvasPoint.Y * _zoomScale - _scrollViewer.Viewport.Height / 2);
        _scrollViewer.Offset = ClampScrollOffset(offset);
        UpdateViewportBounds();
    }

    private void SetZoom(double zoom)
    {
        var center = new Point(
            (_scrollViewer.Offset.X + _scrollViewer.Viewport.Width / 2) / _zoomScale,
            (_scrollViewer.Offset.Y + _scrollViewer.Viewport.Height / 2) / _zoomScale);

        _zoomScale = Math.Clamp(zoom, MinZoom, MaxZoom);
        _zoomHost.LayoutTransform = CreateZoomTransform(_zoomScale);
        EnsureCanvasSize();
        _zoomHost.InvalidateMeasure();
        UpdateZoomText();
        CenterViewportAt(center);
    }

    private static ScaleTransform CreateZoomTransform(double zoom)
    {
        return new ScaleTransform(zoom, zoom);
    }

    private Vector ClampScrollOffset(Vector offset)
    {
        var maxX = Math.Max(0, _scrollViewer.Extent.Width - _scrollViewer.Viewport.Width);
        var maxY = Math.Max(0, _scrollViewer.Extent.Height - _scrollViewer.Viewport.Height);
        return new Vector(
            Math.Clamp(offset.X, 0, maxX),
            Math.Clamp(offset.Y, 0, maxY));
    }

    private void CenterNode(MindMapNode node)
    {
        var nodeSize = GetRenderedNodeSize(node);
        var center = new Point(node.X + nodeSize.Width / 2, node.Y + nodeSize.Height / 2);
        CenterViewportAt(center);
    }

    private void HandleScrollViewerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.ViewportProperty)
        {
            EnsureCanvasSize();
        }

        if (e.Property == ScrollViewer.OffsetProperty
            || e.Property == ScrollViewer.ViewportProperty
            || e.Property == ScrollViewer.ExtentProperty)
        {
            UpdateViewportBounds();
        }
    }

    private void UpdateViewportBounds()
    {
        if (_zoomScale <= 0 || _scrollViewer.Viewport.Width <= 0 || _scrollViewer.Viewport.Height <= 0)
        {
            ViewportBounds = default;
            return;
        }

        ViewportBounds = new Rect(
            _scrollViewer.Offset.X / _zoomScale,
            _scrollViewer.Offset.Y / _zoomScale,
            _scrollViewer.Viewport.Width / _zoomScale,
            _scrollViewer.Viewport.Height / _zoomScale);
    }

    private bool IsCanvasPanSource(object? source)
    {
        if (source is not Visual visual)
        {
            return false;
        }

        for (var current = visual; current is not null; current = current.GetVisualParent())
        {
            if (current is TextBox or Button or ScrollBar or Thumb)
            {
                return false;
            }

            if (_nodeFrames.Values.Contains(current))
            {
                return false;
            }

            if (ReferenceEquals(current, _nodeToolbar))
            {
                return false;
            }

            if (ReferenceEquals(current, _scrollViewer))
            {
                return true;
            }
        }

        return true;
    }

    private static bool HasVisualAncestor<T>(object? source)
        where T : Visual
    {
        if (source is not Visual visual)
        {
            return false;
        }

        for (var current = visual; current is not null; current = current.GetVisualParent())
        {
            if (current is T)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateZoomText()
    {
        ZoomText = $"{_zoomScale:P0}";
        UpdateViewportBounds();
    }

    private void UpdateDropTarget(MindMapNode dragNode, Point canvasPoint)
    {
        MindMapNode? nextTarget = null;
        var nextPlacement = MindMapDropPlacement.Child;

        foreach (var (node, frame) in _nodeFrames)
        {
            if (!CanMoveNode(dragNode, node))
            {
                continue;
            }

            var bounds = GetNodeBounds(node, frame);
            if (!bounds.Contains(canvasPoint))
            {
                continue;
            }

            nextTarget = node;
            nextPlacement = GetDropPlacement(bounds, canvasPoint);
            if (IsRootNode(node) && nextPlacement is MindMapDropPlacement.Before or MindMapDropPlacement.After)
            {
                nextPlacement = MindMapDropPlacement.Child;
            }

            break;
        }

        if (nextTarget is null)
        {
            ClearDropTarget();
            return;
        }

        _dropTarget = nextTarget;
        _dropPlacement = nextPlacement;
        ShowDropPreview(nextTarget, nextPlacement);
    }

    private void ClearDropTarget()
    {
        _dropTarget = null;
        _dropPlacement = MindMapDropPlacement.Child;
        HideDropPreview();
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

    private Rect GetNodeBounds(MindMapNode node, Control frame)
    {
        var size = GetRenderedNodeSize(node);
        return new Rect(node.X, node.Y, size.Width, size.Height);
    }

    private void ShowDropPreview(MindMapNode target, MindMapDropPlacement placement)
    {
        if (!_nodeFrames.TryGetValue(target, out var frame))
        {
            HideDropPreview();
            return;
        }

        var bounds = GetNodeBounds(target, frame);
        _dropPreviewPath.Stroke = placement == MindMapDropPlacement.Child
            ? Brush.Parse("#22C55E")
            : GetSelectionBrush();
        _dropPreviewPath.Data = CreateDropPreviewGeometry(bounds, placement);
        _dropPreviewPath.IsVisible = true;
    }

    private void HideDropPreview()
    {
        _dropPreviewPath.IsVisible = false;
        _dropPreviewPath.Data = null;
    }

    private static Geometry CreateDropPreviewGeometry(Rect bounds, MindMapDropPlacement placement)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();

        if (placement == MindMapDropPlacement.Child)
        {
            // 拖到节点中部时用虚线框提示“成为子节点”的最终结果。
            var rect = bounds.Inflate(7);
            context.BeginFigure(rect.TopLeft, isFilled: false);
            context.LineTo(rect.TopRight);
            context.LineTo(rect.BottomRight);
            context.LineTo(rect.BottomLeft);
            context.LineTo(rect.TopLeft);
            context.EndFigure(isClosed: true);
            return geometry;
        }

        var y = placement == MindMapDropPlacement.Before
            ? bounds.Top - 8
            : bounds.Bottom + 8;
        // 拖到上下边缘时只画一条插入线，表示调整同级顺序。
        var start = new Point(bounds.Left - 16, y);
        var end = new Point(bounds.Right + 16, y);
        context.BeginFigure(start, isFilled: false);
        context.LineTo(end);
        return geometry;
    }

}

using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AtomUI.Desktop.Controls;
using Zhijian.ViewModels;

namespace Zhijian.Views;

public partial class MainWindow : Window
{
    private const double DefaultOutlinePaneWidth = 420;
    private const double MinOutlinePaneWidth = 320;
    private const double MaxOutlinePaneWidth = 640;
    private const double SplitterWidth = 14;
    private const double MinMindMapPaneWidth = 420;

    private TitleBarLeftAddOn? _titleBarLeftAddOn;
    private TitleBarRightAddOn? _titleBarRightAddOn;
    private bool _isPaneSplitterDragging;
    private Point _paneSplitterStartPoint;
    private double _paneSplitterStartWidth;

    public MainWindow()
    {
        InitializeComponent();
        MiniMapPopup.PlacementTarget = MiniMapButton;
        MiniMap.MapPointRequested += (_, point) =>
        {
            MiniMapPopup.IsOpen = false;
            MindMap.CenterViewportAt(point);
            SetStatus("已定位到小图位置");
        };
        AddHandler(PointerPressedEvent, HandleTitleBarDragPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyDownEvent, HandleWindowKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        DataContextChanged += (_, _) => ApplyTitleBarDataContext();
        PaneSplitter.PointerPressed += HandlePaneSplitterPointerPressed;
        PaneSplitter.PointerMoved += HandlePaneSplitterPointerMoved;
        PaneSplitter.PointerReleased += HandlePaneSplitterPointerReleased;
        PaneSplitter.PointerCaptureLost += (_, _) => StopPaneSplitterDrag();
    }

    protected override WindowTitleBar? NotifyCreateTitleBar(WindowTitleBar? oldTitleBar)
    {
        return oldTitleBar ?? new WindowTitleBar
        {
            Name = "PART_TitleBar"
        };
    }

    protected override void NotifyConfigureTitleBar(WindowTitleBar titleBar)
    {
        base.NotifyConfigureTitleBar(titleBar);
        titleBar.SetCurrentValue(WindowTitleBar.TitleProperty, null);
        _titleBarLeftAddOn = new TitleBarLeftAddOn();
        _titleBarRightAddOn = new TitleBarRightAddOn();
        ApplyTitleBarDataContext();
        titleBar.SetCurrentValue(WindowTitleBar.LeftAddOnProperty, _titleBarLeftAddOn);
        titleBar.SetCurrentValue(WindowTitleBar.RightAddOnProperty, _titleBarRightAddOn);
    }

    private void ApplyTitleBarDataContext()
    {
        if (_titleBarLeftAddOn is not null)
        {
            _titleBarLeftAddOn.DataContext = DataContext;
        }

        if (_titleBarRightAddOn is not null)
        {
            _titleBarRightAddOn.DataContext = DataContext;
        }
    }

    private void ToggleMiniMapClicked(object? sender, RoutedEventArgs e)
    {
        MiniMapPopup.IsOpen = !MiniMapPopup.IsOpen;
    }

    private void CenterRootClicked(object? sender, RoutedEventArgs e)
    {
        CenterRootTopic();
    }

    private void ZoomOutClicked(object? sender, RoutedEventArgs e)
    {
        MindMap.ZoomOut();
    }

    private void ZoomInClicked(object? sender, RoutedEventArgs e)
    {
        MindMap.ZoomIn();
    }

    private void ResetZoomClicked(object? sender, RoutedEventArgs e)
    {
        MindMap.ResetZoom();
    }

    private void HandleWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.L && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            CenterRootTopic();
            e.Handled = true;
        }
    }

    private void HandleTitleBarDragPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || WindowState == Avalonia.Controls.WindowState.FullScreen
            || !IsTitleBarDragSource(e))
        {
            return;
        }

        BeginMoveDrag(e);
        e.Handled = true;
    }

    private bool IsTitleBarDragSource(PointerPressedEventArgs e)
    {
        var point = e.GetPosition(this);
        if (point.Y > 40)
        {
            return false;
        }

        if (point.X < 320)
        {
            return false;
        }

        if (e.Source is not Visual source)
        {
            return true;
        }

        for (var current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is TitleBarLeftAddOn or TitleBarRightAddOn
                || current is Avalonia.Controls.Button
                || current is Button or DropdownButton or ToggleSwitch or MenuItem or Avalonia.Controls.MenuItem)
            {
                return false;
            }

            if (current.GetType().Name.Contains("CaptionButton", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private void HandlePaneSplitterPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(PaneSplitter);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        _isPaneSplitterDragging = true;
        _paneSplitterStartPoint = e.GetPosition(PaneGrid);
        _paneSplitterStartWidth = OutlinePaneHost.Bounds.Width > 0
            ? OutlinePaneHost.Bounds.Width
            : DefaultOutlinePaneWidth;
        e.Pointer.Capture(PaneSplitter);
        e.Handled = true;
    }

    private void HandlePaneSplitterPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPaneSplitterDragging)
        {
            return;
        }

        var currentPoint = e.GetPosition(PaneGrid);
        SetOutlinePaneWidth(_paneSplitterStartWidth + currentPoint.X - _paneSplitterStartPoint.X);
        e.Handled = true;
    }

    private void HandlePaneSplitterPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPaneSplitterDragging)
        {
            return;
        }

        StopPaneSplitterDrag();
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void StopPaneSplitterDrag()
    {
        _isPaneSplitterDragging = false;
    }

    private void SetOutlinePaneWidth(double width)
    {
        var maxWidth = MaxOutlinePaneWidth;
        var availableWidth = PaneGrid.Bounds.Width - SplitterWidth - MinMindMapPaneWidth;
        if (availableWidth > 0)
        {
            maxWidth = Math.Min(maxWidth, Math.Max(MinOutlinePaneWidth, availableWidth));
        }

        var targetWidth = Math.Clamp(width, MinOutlinePaneWidth, maxWidth);
        PaneGrid.ColumnDefinitions[0].Width = new Avalonia.Controls.GridLength(targetWidth, Avalonia.Controls.GridUnitType.Pixel);
    }

    private void CenterRootTopic()
    {
        MindMap.CenterRoot();
        SetStatus("已定位到中心主题");
    }

    private void SetStatus(string status)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.StatusText = status;
        }
    }
}

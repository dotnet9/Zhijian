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
    private TitleBarLeftAddOn? _titleBarLeftAddOn;
    private TitleBarRightAddOn? _titleBarRightAddOn;
    private bool _isCloseConfirmed;

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
        Closing += HandleWindowClosing;
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
        if (DataContext is MainWindowViewModel viewModel
            && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                Execute(viewModel.NewWindowCommand);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.N)
            {
                Execute(viewModel.NewDocumentCommand);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.O)
            {
                Execute(viewModel.OpenDocumentCommand);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.K)
            {
                Execute(viewModel.OpenFolderCommand);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                Execute(viewModel.SaveAsCommand);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.S)
            {
                Execute(viewModel.SaveCommand);
                e.Handled = true;
                return;
            }
        }

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

    private void CenterRootTopic()
    {
        MindMap.CenterRoot();
        SetStatus("已定位到中心主题");
    }

    private async void HandleWindowClosing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        if (_isCloseConfirmed || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        e.Cancel = true;
        if (!await viewModel.ConfirmCloseAsync())
        {
            return;
        }

        _isCloseConfirmed = true;
        Close();
    }

    private void SetStatus(string status)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.StatusText = status;
        }
    }

    private static void Execute(System.Windows.Input.ICommand command)
    {
        if (command.CanExecute(null))
        {
            command.Execute(null);
        }
    }
}

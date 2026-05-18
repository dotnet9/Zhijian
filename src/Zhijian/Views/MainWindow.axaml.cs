using AtomUI.Desktop.Controls;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Lang.Avalonia;
using Zhijian.ViewModels;

namespace Zhijian.Views;

public partial class MainWindow : Window
{
    private TitleBarLeftAddOn? _titleBarLeftAddOn;
    private bool _isCloseConfirmed;

    public MainWindow()
    {
        InitializeComponent();
        MiniMapPopup.PlacementTarget = MiniMapButton;
        MiniMap.MapPointRequested += (_, point) =>
        {
            MiniMapPopup.IsOpen = false;
            MindMap.CenterViewportAt(point);
            SetStatus(T(ZhijianL.StatusCenteredMiniMap));
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
        _titleBarLeftAddOn = new TitleBarLeftAddOn();
        FileMenuTourStep.Target = _titleBarLeftAddOn.FileMenuTourTarget;
        ApplyTitleBarDataContext();
        titleBar.SetCurrentValue(WindowTitleBar.TitleProperty, string.Empty);
        titleBar.SetCurrentValue(WindowTitleBar.LeftAddOnProperty, _titleBarLeftAddOn);
        titleBar.SetCurrentValue(WindowTitleBar.RightAddOnProperty, null);
    }

    private void ApplyTitleBarDataContext()
    {
        if (_titleBarLeftAddOn is not null)
        {
            _titleBarLeftAddOn.DataContext = DataContext;
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

    private async void HandleWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel
            && HasCommandModifier(e.KeyModifiers))
        {
            if (e.Key == Key.N && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                viewModel.NewWindow();
                return;
            }

            if (e.Key == Key.N)
            {
                e.Handled = true;
                await viewModel.NewDocumentAsync();
                return;
            }

            if (e.Key == Key.O)
            {
                e.Handled = true;
                await viewModel.OpenDocumentAsync();
                return;
            }

            if (e.Key == Key.I && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                await viewModel.ImportDocumentAsync();
                return;
            }

            if (e.Key == Key.K)
            {
                e.Handled = true;
                await viewModel.OpenFolderAsync();
                return;
            }

            if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                await viewModel.SaveAsAsync();
                return;
            }

            if (e.Key == Key.S)
            {
                e.Handled = true;
                await viewModel.SaveAsync();
                return;
            }

            if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                viewModel.Redo();
                return;
            }

            if (e.Key == Key.Z)
            {
                e.Handled = true;
                viewModel.Undo();
                return;
            }

            if (e.Key == Key.Y)
            {
                e.Handled = true;
                viewModel.Redo();
                return;
            }

            if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                e.Handled = true;
                await viewModel.CopyAsMarkdownAsync();
                return;
            }

            if (e.Key == Key.W)
            {
                e.Handled = true;
                viewModel.Close();
                return;
            }
        }

        if (e.Key == Key.L && HasCommandModifier(e.KeyModifiers))
        {
            CenterRootTopic();
            e.Handled = true;
            return;
        }

        if (DataContext is MainWindowViewModel vm
            && e.Key == Key.Delete
            && !IsTextInputSource(e))
        {
            vm.DeleteSelected();
            e.Handled = true;
        }
    }

    private static bool HasCommandModifier(KeyModifiers modifiers)
    {
        if (OperatingSystem.IsMacOS())
        {
            return modifiers.HasFlag(KeyModifiers.Meta);
        }

        return modifiers.HasFlag(KeyModifiers.Control);
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
            if (current is TitleBarLeftAddOn
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
        SetStatus(T(ZhijianL.StatusCenteredRoot));
    }

    private static bool IsTextInputSource(KeyEventArgs e)
    {
        if (e.Source is not Visual source)
        {
            return false;
        }

        for (var current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is Avalonia.Controls.TextBox)
            {
                return true;
            }
        }

        return false;
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

    private static string T(string key)
    {
        return I18nManager.Instance.GetResource(key) ?? key;
    }

}

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AtomUI.Desktop.Controls;
using System.Diagnostics;
using Zhijian.ViewModels;

namespace Zhijian.Views;

public partial class MainWindow : Window
{
    private const string RepositoryUrl = "https://github.com/dotnet9/Zhijian";
    private const string WebsiteUrl = "https://codewf.com";

    private ChangelogWindow? _changelogWindow;
    private AboutWindow? _aboutWindow;

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
        titleBar.SetCurrentValue(WindowTitleBar.LeftAddOnProperty, CreateTitleBarLeftAddOn());
        titleBar.SetCurrentValue(WindowTitleBar.RightAddOnProperty, CreateTitleBarRightAddOn());
    }

    private Avalonia.Controls.StackPanel CreateTitleBarLeftAddOn()
    {
        var title = new Avalonia.Controls.TextBlock
        {
            Text = "枝见 Zhijian",
            FontSize = 13,
            FontWeight = Avalonia.Media.FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        title.Bind(Avalonia.Controls.TextBlock.ForegroundProperty, new Binding("PrimaryTextBrush"));

        return new Avalonia.Controls.StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 14,
            Children =
            {
                title,
                CreateFileMenuButton(),
                CreateAboutMenuButton()
            }
        };
    }

    private static Avalonia.Controls.StackPanel CreateTitleBarRightAddOn()
    {
        var label = new Avalonia.Controls.TextBlock
        {
            Text = "深色",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center
        };
        label.Bind(Avalonia.Controls.TextBlock.ForegroundProperty, new Binding("SecondaryTextBrush"));

        var toggle = new ToggleSwitch
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        toggle.Bind(ToggleButton.IsCheckedProperty, new Binding("IsDarkTheme")
        {
            Mode = BindingMode.TwoWay
        });
        AutomationProperties.SetName(toggle, "Toggle dark theme");
        Avalonia.Controls.ToolTip.SetTip(toggle, "切换深色主题");

        return new Avalonia.Controls.StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 8,
            Margin = new Thickness(0, 0, 12, 0),
            Children =
            {
                label,
                toggle
            }
        };
    }

    private static Avalonia.Controls.Button CreateFileMenuButton()
    {
        var flyout = CreateFileMenuFlyout();
        var button = new Avalonia.Controls.Button
        {
            Content = "文件",
            Background = Avalonia.Media.Brushes.Transparent,
            BorderBrush = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 2),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        button.Click += (_, _) => flyout.ShowAt(button);
        AutomationProperties.SetName(button, "File menu");
        return button;
    }

    private static MenuFlyout CreateFileMenuFlyout()
    {
        var flyout = new MenuFlyout();
        flyout.Items.Add(CreateCommandMenuItem("导入 Markdown", "ImportMarkdownCommand"));
        flyout.Items.Add(CreateCommandMenuItem("导入 OPML", "ImportOpmlCommand"));
        flyout.Items.Add(CreateCommandMenuItem("导入 XMind", "ImportXMindCommand"));
        flyout.Items.Add(CreateCommandMenuItem("导出 Markdown", "ExportMarkdownCommand"));
        flyout.Items.Add(CreateCommandMenuItem("导出 OPML", "ExportOpmlCommand"));
        flyout.Items.Add(CreateCommandMenuItem("导出 XMind", "ExportXMindCommand"));
        return flyout;
    }

    private static MenuItem CreateCommandMenuItem(string header, string commandPath)
    {
        var item = new MenuItem
        {
            Header = header
        };
        item.Bind(MenuItem.CommandProperty, new Binding(commandPath));
        return item;
    }

    private Avalonia.Controls.Button CreateAboutMenuButton()
    {
        var flyout = CreateAboutMenuFlyout();
        var button = new Avalonia.Controls.Button
        {
            Content = "关于",
            Background = Avalonia.Media.Brushes.Transparent,
            BorderBrush = Avalonia.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 2),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        button.Click += (_, _) => flyout.ShowAt(button);
        AutomationProperties.SetName(button, "About menu");
        return button;
    }

    private MenuFlyout CreateAboutMenuFlyout()
    {
        var website = new MenuItem { Header = "打开网站" };
        website.Click += (_, _) =>
        {
            OpenUrl(WebsiteUrl);
            SetStatus("已打开网站");
        };

        var changelog = new MenuItem { Header = "更新日志" };
        changelog.Click += (_, _) => ShowChangelogWindow();

        var about = new MenuItem { Header = "关于" };
        about.Click += (_, _) => ShowAboutWindow();

        var flyout = new MenuFlyout();
        flyout.Items.Add(website);
        flyout.Items.Add(changelog);
        flyout.Items.Add(about);
        return flyout;
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

    private void OpenRepositoryClicked(object? sender, RoutedEventArgs e)
    {
        OpenUrl(RepositoryUrl);
        SetStatus("已打开 GitHub 仓库");
    }

    private void ShowChangelogWindow()
    {
        if (_changelogWindow is { IsVisible: true })
        {
            _changelogWindow.Activate();
            return;
        }

        _changelogWindow = new ChangelogWindow();
        _changelogWindow.Closed += (_, _) => _changelogWindow = null;
        _changelogWindow.Show(this);
        SetStatus("已打开更新日志");
    }

    private void ShowAboutWindow()
    {
        if (_aboutWindow is { IsVisible: true })
        {
            _aboutWindow.Activate();
            return;
        }

        _aboutWindow = new AboutWindow();
        _aboutWindow.Closed += (_, _) => _aboutWindow = null;
        _aboutWindow.Show(this);
        SetStatus("已打开关于窗口");
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

        if (e.Source is not Visual source)
        {
            return true;
        }

        for (var current = source; current is not null; current = current.GetVisualParent())
        {
            if (current is Button or DropdownButton or ToggleSwitch or MenuItem)
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

    private void SetStatus(string status)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.StatusText = status;
        }
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }
}

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using AtomUI.Desktop.Controls;

namespace Zhijian.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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

    private static Avalonia.Controls.StackPanel CreateTitleBarLeftAddOn()
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
                CreateFileMenuButton()
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

    private static DropdownButton CreateFileMenuButton()
    {
        var button = new DropdownButton
        {
            Content = "文件",
            ButtonType = ButtonType.Text,
            TriggerType = FlyoutTriggerType.Click,
            IsArrowVisible = true,
            Padding = new Thickness(6, 2),
            VerticalAlignment = VerticalAlignment.Center,
            DropdownFlyout = CreateFileMenuFlyout()
        };
        AutomationProperties.SetName(button, "File menu");
        return button;
    }

    private static MenuFlyout CreateFileMenuFlyout()
    {
        var import = new MenuItem { Header = "导入" };
        import.Items.Add(CreateCommandMenuItem("Markdown", "ImportMarkdownCommand"));
        import.Items.Add(CreateCommandMenuItem("OPML", "ImportOpmlCommand"));
        import.Items.Add(CreateCommandMenuItem("XMind", "ImportXMindCommand"));

        var export = new MenuItem { Header = "导出" };
        export.Items.Add(CreateCommandMenuItem("Markdown", "ExportMarkdownCommand"));
        export.Items.Add(CreateCommandMenuItem("OPML", "ExportOpmlCommand"));
        export.Items.Add(CreateCommandMenuItem("XMind", "ExportXMindCommand"));

        var flyout = new MenuFlyout();
        flyout.Items.Add(import);
        flyout.Items.Add(export);
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
}

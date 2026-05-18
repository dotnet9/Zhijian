using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AtomUI;
using AtomUI.Controls.Localization;
using AtomUI.Desktop.Controls;
using AtomUI.Desktop.Controls.Localization;
using AtomUI.Theme;
using AtomUI.Theme.Language;
using Lang.Avalonia;
using Lang.Avalonia.Json;
using System.Globalization;
using Zhijian.Services;
using Zhijian.ViewModels;
using Zhijian.Views;

namespace Zhijian;

public partial class App : Application
{
    public override void Initialize()
    {
        base.Initialize();
        AvaloniaXamlLoader.Load(this);
        PreserveNativeAotEnumArrays();

        var langPlugin = new JsonLangPlugin
        {
            ResourceFolder = Path.Combine(AppContext.BaseDirectory, "I18n")
        };
        I18nManager.Instance.Register(langPlugin, new CultureInfo("zh-CN"), out _);

        this.UseAtomUI(builder =>
        {
            builder.WithDefaultLanguageVariant(LanguageVariant.zh_CN);
            builder.WithDefaultTheme(IThemeManager.DEFAULT_THEME_ID);
            builder.UseAlibabaSansFont();
            builder.UseDesktopControls();
        });
    }

    private static void PreserveNativeAotEnumArrays()
    {
        GC.KeepAlive(Enum.GetValues<AtomUI.Theme.Styling.SharedTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Theme.TokenSystem.DesignTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Controls.DesignTokens.IconTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.AddOnDecoratedBoxTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.AdornerLayerTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.AlertTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ArrowDecoratedBoxTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.AutoCompleteTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.AvatarTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.BadgeTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.BreadcrumbTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ButtonSpinnerTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ButtonTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.CalendarTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.CardTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.CarouselTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.CascaderTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.CheckBoxTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.CollapseTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ComboBoxTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.DatePickerTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.DescriptionsTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.DialogTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.DrawerTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.EmptyTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ExpanderTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.FloatButtonTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.FlyoutHostTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.FormTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.GroupBoxTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ImagePreviewerTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.Primitives.DesignTokens.InfoPickerInputTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.LineEditTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ListBoxTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ListViewTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.MarqueeLabelTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.MentionsTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.MenuTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.MessageBoxTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.MessageTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.NavMenuTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.NotificationTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.NumericUpDownTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.OptionButtonTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.PaginationTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.PopupConfirmTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.PopupHostTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ProgressBarTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.QRCodeTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.RadioButtonTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.RateTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ResultTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.Primitives.DesignTokens.IndicatorScrollViewerTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ScrollViewerTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SegmentedTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SelectTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SeparatorTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SkeletonTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SliderTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SpaceTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SpinTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SplitterTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.SplitViewTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.StatisticTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.StepsTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TabControlTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TagTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TextAreaTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TimelineTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TimePickerTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ToggleSwitchTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.ToolTipTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TourTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TransferTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TreeFlyoutTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TreeSelectTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.TreeViewTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.UploadTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.WindowTitleBarTokenKind>());
        GC.KeepAlive(Enum.GetValues<AtomUI.Desktop.Controls.DesignTokens.WindowTokenKind>());
        GC.KeepAlive(Enum.GetValues<CommonLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<DatePickerLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<DialogLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<ImagePreviewerLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<PaginationLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<QRCodeLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<TimePickerLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<TourLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<TransferLangResourceKind>());
        GC.KeepAlive(Enum.GetValues<UploadLangResourceKind>());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainWindowViewModel(
                new AvaloniaMindMapFileService(mainWindow),
                new AvaloniaApplicationActionService(mainWindow),
                GetStartupDocumentPath());
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string? GetStartupDocumentPath()
    {
        var manualPath = Path.Combine(AppContext.BaseDirectory, "使用手册.md");
        return File.Exists(manualPath) ? manualPath : null;
    }
}

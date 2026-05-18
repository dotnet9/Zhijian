using AtomUI.Controls.Localization;
using AtomUI.Controls.DesignTokens;
using AtomUI.Desktop.Controls.Localization;
using AtomUI.Desktop.Controls.DesignTokens;
using AtomUI.Desktop.Controls.Primitives.DesignTokens;
using AtomUI.Theme.Styling;
using AtomUI.Theme.TokenSystem;

namespace Zhijian;

internal static class NativeAotCompatibility
{
    public static void PreserveAtomUiLanguageResourceArrays()
    {
        PreserveAtomUiThemeTokenArrays();

        PreserveEnumArray<CommonLangResourceKind>();
        PreserveEnumArray<DatePickerLangResourceKind>();
        PreserveEnumArray<DialogLangResourceKind>();
        PreserveEnumArray<ImagePreviewerLangResourceKind>();
        PreserveEnumArray<PaginationLangResourceKind>();
        PreserveEnumArray<QRCodeLangResourceKind>();
        PreserveEnumArray<TimePickerLangResourceKind>();
        PreserveEnumArray<TourLangResourceKind>();
        PreserveEnumArray<TransferLangResourceKind>();
        PreserveEnumArray<UploadLangResourceKind>();
    }

    private static void PreserveAtomUiThemeTokenArrays()
    {
        PreserveEnumArray<SharedTokenKind>();
        PreserveEnumArray<DesignTokenKind>();
        PreserveEnumArray<IconTokenKind>();
        PreserveEnumArray<AddOnDecoratedBoxTokenKind>();
        PreserveEnumArray<AdornerLayerTokenKind>();
        PreserveEnumArray<AlertTokenKind>();
        PreserveEnumArray<ArrowDecoratedBoxTokenKind>();
        PreserveEnumArray<AutoCompleteTokenKind>();
        PreserveEnumArray<AvatarTokenKind>();
        PreserveEnumArray<BadgeTokenKind>();
        PreserveEnumArray<BreadcrumbTokenKind>();
        PreserveEnumArray<ButtonSpinnerTokenKind>();
        PreserveEnumArray<ButtonTokenKind>();
        PreserveEnumArray<CalendarTokenKind>();
        PreserveEnumArray<CardTokenKind>();
        PreserveEnumArray<CarouselTokenKind>();
        PreserveEnumArray<CascaderTokenKind>();
        PreserveEnumArray<CheckBoxTokenKind>();
        PreserveEnumArray<CollapseTokenKind>();
        PreserveEnumArray<ComboBoxTokenKind>();
        PreserveEnumArray<DatePickerTokenKind>();
        PreserveEnumArray<DescriptionsTokenKind>();
        PreserveEnumArray<DialogTokenKind>();
        PreserveEnumArray<DrawerTokenKind>();
        PreserveEnumArray<EmptyTokenKind>();
        PreserveEnumArray<ExpanderTokenKind>();
        PreserveEnumArray<FloatButtonTokenKind>();
        PreserveEnumArray<FlyoutHostTokenKind>();
        PreserveEnumArray<FormTokenKind>();
        PreserveEnumArray<GroupBoxTokenKind>();
        PreserveEnumArray<ImagePreviewerTokenKind>();
        PreserveEnumArray<InfoPickerInputTokenKind>();
        PreserveEnumArray<LineEditTokenKind>();
        PreserveEnumArray<ListBoxTokenKind>();
        PreserveEnumArray<ListViewTokenKind>();
        PreserveEnumArray<MarqueeLabelTokenKind>();
        PreserveEnumArray<MentionsTokenKind>();
        PreserveEnumArray<MenuTokenKind>();
        PreserveEnumArray<MessageBoxTokenKind>();
        PreserveEnumArray<MessageTokenKind>();
        PreserveEnumArray<NavMenuTokenKind>();
        PreserveEnumArray<NotificationTokenKind>();
        PreserveEnumArray<NumericUpDownTokenKind>();
        PreserveEnumArray<OptionButtonTokenKind>();
        PreserveEnumArray<PaginationTokenKind>();
        PreserveEnumArray<PopupConfirmTokenKind>();
        PreserveEnumArray<PopupHostTokenKind>();
        PreserveEnumArray<ProgressBarTokenKind>();
        PreserveEnumArray<QRCodeTokenKind>();
        PreserveEnumArray<RadioButtonTokenKind>();
        PreserveEnumArray<RateTokenKind>();
        PreserveEnumArray<ResultTokenKind>();
        PreserveEnumArray<IndicatorScrollViewerTokenKind>();
        PreserveEnumArray<ScrollViewerTokenKind>();
        PreserveEnumArray<SegmentedTokenKind>();
        PreserveEnumArray<SelectTokenKind>();
        PreserveEnumArray<SeparatorTokenKind>();
        PreserveEnumArray<SkeletonTokenKind>();
        PreserveEnumArray<SliderTokenKind>();
        PreserveEnumArray<SpaceTokenKind>();
        PreserveEnumArray<SpinTokenKind>();
        PreserveEnumArray<SplitterTokenKind>();
        PreserveEnumArray<SplitViewTokenKind>();
        PreserveEnumArray<StatisticTokenKind>();
        PreserveEnumArray<StepsTokenKind>();
        PreserveEnumArray<TabControlTokenKind>();
        PreserveEnumArray<TagTokenKind>();
        PreserveEnumArray<TextAreaTokenKind>();
        PreserveEnumArray<TimelineTokenKind>();
        PreserveEnumArray<TimePickerTokenKind>();
        PreserveEnumArray<ToggleSwitchTokenKind>();
        PreserveEnumArray<ToolTipTokenKind>();
        PreserveEnumArray<TourTokenKind>();
        PreserveEnumArray<TransferTokenKind>();
        PreserveEnumArray<TreeFlyoutTokenKind>();
        PreserveEnumArray<TreeSelectTokenKind>();
        PreserveEnumArray<TreeViewTokenKind>();
        PreserveEnumArray<UploadTokenKind>();
        PreserveEnumArray<WindowTitleBarTokenKind>();
        PreserveEnumArray<WindowTokenKind>();
    }

    private static void PreserveEnumArray<TEnum>()
        where TEnum : struct, Enum
    {
        GC.KeepAlive(Enum.GetValues<TEnum>());
    }
}

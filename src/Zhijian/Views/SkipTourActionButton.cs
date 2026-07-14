using AtomUI;
using AtomUI.Desktop.Controls;
using Avalonia.Interactivity;

namespace Zhijian.Views;

public class SkipTourActionButton : Button, ITourAction
{
    private Tour? _tour;

    static SkipTourActionButton()
    {
        Tour.StyleTypeProperty.AddOwner<SkipTourActionButton>();
        SizeTypeProperty.OverrideDefaultValue<SkipTourActionButton>(CustomizableSizeType.Small);
        ButtonTypeProperty.OverrideDefaultValue<SkipTourActionButton>(ButtonType.Default);
    }

    public int StepCount { get; set; }

    public int ActiveIndex { get; set; }

    public TourStyleType StyleType { get; set; }

    public void NotifyAttached(Tour tour)
    {
        if (ReferenceEquals(_tour, tour))
        {
            return;
        }

        if (_tour is not null)
        {
            Click -= HandleClick;
        }

        _tour = tour;
        Click += HandleClick;
    }

    private void HandleClick(object? sender, RoutedEventArgs e)
    {
        // “跳过”应立即结束整段新手引导，并触发 ViewModel 记录已看过状态。
        _tour?.HideTour();
        e.Handled = true;
    }
}

using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Zhijian.ViewModels;
using AtomMenuItem = AtomUI.Desktop.Controls.MenuItem;
using AtomToolTip = AtomUI.Desktop.Controls.ToolTip;

namespace Zhijian.Views;

public partial class TitleBarLeftAddOn : UserControl
{
    private MainWindowViewModel? _viewModel;

    public TitleBarLeftAddOn()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => WireViewModel(DataContext as MainWindowViewModel);
        RegisterMenuActions();
    }

    private void WireViewModel(MainWindowViewModel? viewModel)
    {
        if (_viewModel is not null)
        {
            _viewModel.RecentFiles.CollectionChanged -= HandleRecentFilesChanged;
        }

        _viewModel = viewModel;
        if (_viewModel is not null)
        {
            _viewModel.RecentFiles.CollectionChanged += HandleRecentFilesChanged;
        }

        RebuildRecentMenu();
    }

    private void HandleRecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildRecentMenu();
    }

    private void RebuildRecentMenu()
    {
        RecentFilesMenu.Items.Clear();
        if (_viewModel is null || _viewModel.RecentFiles.Count == 0)
        {
            RecentFilesMenu.Items.Add(new AtomMenuItem
            {
                Header = "暂无最近文件",
                IsEnabled = false
            });
            return;
        }

        foreach (var file in _viewModel.RecentFiles)
        {
            var item = new AtomMenuItem
            {
                Header = file.DisplayName,
                DataContext = file
            };
            item.AddHandler(
                PointerReleasedEvent,
                RecentFilePointerReleased,
                Avalonia.Interactivity.RoutingStrategies.Bubble,
                handledEventsToo: true);
            AtomToolTip.SetTip(item, file.FilePath);
            RecentFilesMenu.Items.Add(item);
        }
    }

    private void RegisterMenuActions()
    {
        RegisterMenuAction(NewDocumentItem, () => Execute(_viewModel?.NewDocumentCommand));
        RegisterMenuAction(NewWindowItem, () => Execute(_viewModel?.NewWindowCommand));
        RegisterMenuAction(OpenDocumentItem, () => Execute(_viewModel?.OpenDocumentCommand));
        RegisterMenuAction(OpenFolderItem, () => Execute(_viewModel?.OpenFolderCommand));
        RegisterMenuAction(SaveItem, () => Execute(_viewModel?.SaveCommand));
        RegisterMenuAction(SaveAsItem, () => Execute(_viewModel?.SaveAsCommand));
        RegisterMenuAction(OpenFileLocationItem, () => Execute(_viewModel?.OpenFileLocationCommand));
        RegisterMenuAction(CloseItem, () => Execute(_viewModel?.CloseCommand));
        RegisterMenuAction(OpenWebsiteItem, () => Execute(_viewModel?.OpenWebsiteCommand));
        RegisterMenuAction(ShowChangelogItem, () => Execute(_viewModel?.ShowChangelogCommand));
        RegisterMenuAction(ShowThanksItem, () => Execute(_viewModel?.ShowThanksCommand));
        RegisterMenuAction(ShowAboutItem, () => Execute(_viewModel?.ShowAboutCommand));
    }

    private static void RegisterMenuAction(AtomMenuItem item, Action action)
    {
        item.AddHandler(
            PointerReleasedEvent,
            (_, e) =>
            {
                if (e.GetCurrentPoint(item).Properties.PointerUpdateKind is not PointerUpdateKind.LeftButtonReleased)
                {
                    return;
                }

                action();
                e.Handled = true;
            },
            Avalonia.Interactivity.RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private void RecentFilePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.GetCurrentPoint(sender as Control).Properties.PointerUpdateKind is not PointerUpdateKind.LeftButtonReleased)
        {
            return;
        }

        if (sender is AtomMenuItem { DataContext: RecentFileItem recentFile }
            && _viewModel?.OpenRecentFileCommand.CanExecute(recentFile.FilePath) == true)
        {
            _viewModel.OpenRecentFileCommand.Execute(recentFile.FilePath);
            e.Handled = true;
        }
    }

    private static void Execute(System.Windows.Input.ICommand? command)
    {
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }
}

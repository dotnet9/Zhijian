using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Lang.Avalonia;
using Zhijian.ViewModels;
using AtomMenuItem = AtomUI.Desktop.Controls.MenuItem;
using AtomToolTip = AtomUI.Desktop.Controls.ToolTip;

namespace Zhijian.Views;

public partial class TitleBarLeftAddOn : UserControl
{
    private MainWindowViewModel? _viewModel;

    public Control FileMenuTourTarget => FileMenuItem;

    public TitleBarLeftAddOn()
    {
        InitializeComponent();
        ApplyPlatformInputGestures();
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
                Header = I18nManager.Instance.GetResource(ZhijianL.NoRecentFiles) ?? "No recent files",
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
        RegisterMenuAction(UndoItem, () => Execute(_viewModel?.UndoCommand));
        RegisterMenuAction(RedoItem, () => Execute(_viewModel?.RedoCommand));
        RegisterMenuAction(AddSiblingItem, () => Execute(_viewModel?.AddSiblingToSelectedCommand));
        RegisterMenuAction(AddChildItem, () => Execute(_viewModel?.AddChildToSelectedCommand));
        RegisterMenuAction(PromoteItem, () => Execute(_viewModel?.PromoteSelectedCommand));
        RegisterMenuAction(DemoteItem, () => Execute(_viewModel?.DemoteSelectedCommand));
        RegisterMenuAction(MoveUpItem, () => Execute(_viewModel?.MoveSelectedUpCommand));
        RegisterMenuAction(MoveDownItem, () => Execute(_viewModel?.MoveSelectedDownCommand));
        RegisterMenuAction(CopyMarkdownItem, () => Execute(_viewModel?.CopyAsMarkdownCommand));
        RegisterMenuAction(DeleteNodeItem, () => Execute(_viewModel?.DeleteSelectedCommand));
        RegisterMenuAction(LightThemeItem, () => Execute(_viewModel?.SetLightThemeCommand));
        RegisterMenuAction(DarkThemeItem, () => Execute(_viewModel?.SetDarkThemeCommand));
        RegisterMenuAction(SimplifiedChineseItem, () => Execute(_viewModel?.SelectSimplifiedChineseCommand));
        RegisterMenuAction(TraditionalChineseItem, () => Execute(_viewModel?.SelectTraditionalChineseCommand));
        RegisterMenuAction(EnglishItem, () => Execute(_viewModel?.SelectEnglishCommand));
        RegisterMenuAction(JapaneseItem, () => Execute(_viewModel?.SelectJapaneseCommand));
        RegisterMenuAction(FeedbackItem, () => Execute(_viewModel?.OpenFeedbackCommand));
        RegisterMenuAction(FeatureRequestItem, () => Execute(_viewModel?.OpenFeatureRequestCommand));
        RegisterMenuAction(PullRequestsItem, () => Execute(_viewModel?.OpenPullRequestsCommand));
        RegisterMenuAction(RepositoryItem, () => Execute(_viewModel?.OpenRepositoryCommand));
        RegisterMenuAction(OpenWebsiteItem, () => Execute(_viewModel?.OpenWebsiteCommand));
        RegisterMenuAction(ShowChangelogItem, () => Execute(_viewModel?.ShowChangelogCommand));
        RegisterMenuAction(ShowThanksItem, () => Execute(_viewModel?.ShowThanksCommand));
        RegisterMenuAction(ShowAboutItem, () => Execute(_viewModel?.ShowAboutCommand));
    }

    private void ApplyPlatformInputGestures()
    {
        NewDocumentItem.InputGesture = CreateCommandGesture(Key.N);
        NewWindowItem.InputGesture = CreateCommandGesture(Key.N, KeyModifiers.Shift);
        OpenDocumentItem.InputGesture = CreateCommandGesture(Key.O);
        OpenFolderItem.InputGesture = CreateCommandGesture(Key.K);
        SaveItem.InputGesture = CreateCommandGesture(Key.S);
        SaveAsItem.InputGesture = CreateCommandGesture(Key.S, KeyModifiers.Shift);
        CloseItem.InputGesture = OperatingSystem.IsMacOS()
            ? CreateCommandGesture(Key.W)
            : KeyGesture.Parse("Alt+F4");
        UndoItem.InputGesture = CreateCommandGesture(Key.Z);
        RedoItem.InputGesture = OperatingSystem.IsMacOS()
            ? CreateCommandGesture(Key.Z, KeyModifiers.Shift)
            : CreateCommandGesture(Key.Y);
        CopyMarkdownItem.InputGesture = CreateCommandGesture(Key.C, KeyModifiers.Shift);
    }

    private static KeyGesture CreateCommandGesture(Key key, KeyModifiers extraModifiers = KeyModifiers.None)
    {
        var commandModifier = OperatingSystem.IsMacOS()
            ? KeyModifiers.Meta
            : KeyModifiers.Control;
        return new KeyGesture(key, commandModifier | extraModifiers);
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

using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
            item.Click += RecentFileClicked;
            AtomToolTip.SetTip(item, file.FilePath);
            RecentFilesMenu.Items.Add(item);
        }
    }

    private void ApplyPlatformInputGestures()
    {
        NewDocumentItem.InputGesture = CreateCommandGesture(Key.N);
        NewWindowItem.InputGesture = CreateCommandGesture(Key.N, KeyModifiers.Shift);
        OpenDocumentItem.InputGesture = CreateCommandGesture(Key.O);
        ImportDocumentItem.InputGesture = CreateCommandGesture(Key.I, KeyModifiers.Shift);
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

    private async void RecentFileClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not AtomMenuItem { DataContext: RecentFileItem recentFile }
            || _viewModel is null)
        {
            return;
        }

        await _viewModel.OpenRecentFileAsync(recentFile.FilePath);
        e.Handled = true;
    }
}

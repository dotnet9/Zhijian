using CodeWF.MindView;
using Zhijian.ViewModels;
using AtomWindow = AtomUI.Desktop.Controls.Window;

namespace Zhijian.Views;

public partial class SaveChangesWindow : AtomWindow
{
    public SaveChangesWindow()
        : this("未命名")
    {
    }

    public SaveChangesWindow(string documentName)
    {
        InitializeComponent();
        DataContext = new SaveChangesWindowViewModel(documentName);
    }

    private void SaveClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(MindMapSaveChangesDecision.Save);
    }

    private void DiscardClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(MindMapSaveChangesDecision.Discard);
    }

    private void CancelClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(MindMapSaveChangesDecision.Cancel);
    }
}

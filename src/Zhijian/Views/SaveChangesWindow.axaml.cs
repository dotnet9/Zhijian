using CodeWF.Avalonia.MindView;
using Zhijian.ViewModels;
using AppWindow = CodeWF.Avalonia.Controls.Controls.CodeWFWindow;

namespace Zhijian.Views;

public partial class SaveChangesWindow : AppWindow
{
    public SaveChangesWindow()
        : this(null)
    {
    }

    public SaveChangesWindow(string? documentName)
    {
        InitializeComponent();
        DataContext = new SaveChangesWindowViewModel(documentName);
        Closed += (_, _) => (DataContext as SaveChangesWindowViewModel)?.Dispose();
    }

    private void SaveClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(MindMapSaveChangesDecision.Save);
    }

    private void DiscardClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(MindMapSaveChangesDecision.Discard);
    }

    private void CancelClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(MindMapSaveChangesDecision.Cancel);
    }
}

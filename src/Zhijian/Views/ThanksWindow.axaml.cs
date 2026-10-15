using Zhijian.ViewModels;
using AppWindow = CodeWF.Avalonia.Controls.Controls.CodeWFWindow;

namespace Zhijian.Views;

public partial class ThanksWindow : AppWindow
{
    public ThanksWindow()
    {
        InitializeComponent();
        DataContext = new ThanksWindowViewModel();
        Closed += (_, _) => (DataContext as ThanksWindowViewModel)?.Dispose();
    }
    private void OpenLinkClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var url = (sender as global::Avalonia.Controls.Button)?.Tag?.ToString();
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "mailto")
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}

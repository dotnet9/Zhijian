using Zhijian.ViewModels;
using AppWindow = CodeWF.Avalonia.Controls.Controls.CodeWFWindow;

namespace Zhijian.Views;

public partial class AboutWindow : AppWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new AboutWindowViewModel();
    }
    private void OpenLinkClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var url = (sender as global::Avalonia.Controls.Button)?.Tag?.ToString();
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "mailto")
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}

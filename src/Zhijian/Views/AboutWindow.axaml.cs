using Zhijian.ViewModels;
using AtomWindow = AtomUI.Desktop.Controls.Window;

namespace Zhijian.Views;

public partial class AboutWindow : AtomWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new AboutWindowViewModel();
    }
}

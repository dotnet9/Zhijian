using Zhijian.ViewModels;
using AtomWindow = AtomUI.Desktop.Controls.Window;

namespace Zhijian.Views;

public partial class ThanksWindow : AtomWindow
{
    public ThanksWindow()
    {
        InitializeComponent();
        DataContext = new ThanksWindowViewModel();
    }
}

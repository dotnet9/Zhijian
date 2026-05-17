using Zhijian.ViewModels;
using AtomWindow = AtomUI.Desktop.Controls.Window;

namespace Zhijian.Views;

public partial class ChangelogWindow : AtomWindow
{
    public ChangelogWindow()
    {
        InitializeComponent();
        DataContext = new ChangelogWindowViewModel();
    }
}

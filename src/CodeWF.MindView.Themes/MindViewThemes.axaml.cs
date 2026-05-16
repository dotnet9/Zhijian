using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace CodeWF.MindView.Themes;

public class MindViewThemes : Styles
{
    public MindViewThemes()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

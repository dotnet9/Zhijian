using global::Avalonia.Markup.Xaml;
using global::Avalonia.Styling;

namespace CodeWF.MindView.Themes;

public class MindViewThemes : Styles
{
    public MindViewThemes()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

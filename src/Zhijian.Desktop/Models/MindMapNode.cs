using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Zhijian.Desktop.Models;

public partial class MindMapNode : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private string _accentColor = string.Empty;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    public MindMapNode(string title, params MindMapNode[] children)
    {
        _title = title;
        Children = new ObservableCollection<MindMapNode>(children);
    }

    public ObservableCollection<MindMapNode> Children { get; }
}

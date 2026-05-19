using Avalonia.Controls;
using CodeWF.MindView;

namespace Zhijian.Views;

public partial class WorkspaceOutlineView : UserControl
{
    public WorkspaceOutlineView()
    {
        InitializeComponent();
    }

    public Control EditorHost => OutlineEditorHost;

    public Control EditorModeToggleTarget => EditorModeToggleButton;

    public IMindMapEditorController? Controller
    {
        get => OutlineEditorControl.Controller;
        set => OutlineEditorControl.Controller = value;
    }
}

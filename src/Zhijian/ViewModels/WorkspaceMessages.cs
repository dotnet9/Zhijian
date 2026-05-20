using CodeWF.EventBus;
using CodeWF.MindView;

namespace Zhijian.ViewModels;

public sealed class WorkspaceFilesState
{
    public IReadOnlyList<MindMapFileItem> FolderFiles { get; init; } = [];

    public MindMapFileItem? SelectedFolderFile { get; init; }

    public bool HasFolderFiles { get; init; }

    public bool IsFolderEmpty { get; init; }

    public string FolderSummary { get; init; } = string.Empty;

    public string PanelBorderBrush { get; init; } = string.Empty;

    public string PrimaryTextBrush { get; init; } = string.Empty;

    public string SecondaryTextBrush { get; init; } = string.Empty;
}

public sealed class WorkspaceOutlineState
{
    public IReadOnlyList<MindMapNode> Roots { get; init; } = [];

    public MindMapNode? SelectedNode { get; init; }

    public bool IsOutlineMode { get; init; }

    public bool IsMarkdownMode { get; init; }

    public bool IsDarkTheme { get; init; }

    public string MarkdownText { get; init; } = string.Empty;

    public string EditorPaneTitle { get; init; } = string.Empty;

    public string ToggleEditorToolTip { get; init; } = string.Empty;

    public string PanelBackground { get; init; } = string.Empty;

    public string PanelBorderBrush { get; init; } = string.Empty;

    public string PrimaryTextBrush { get; init; } = string.Empty;

    public string SecondaryTextBrush { get; init; } = string.Empty;

    public bool CanAddSiblingToSelectedNode { get; init; }
}

public sealed class WorkspaceFilesStateChangedCommand(WorkspaceFilesState state) : Command
{
    public WorkspaceFilesState State { get; } = state;
}

public sealed class WorkspaceOutlineStateChangedCommand(WorkspaceOutlineState state) : Command
{
    public WorkspaceOutlineState State { get; } = state;
}

public sealed class WorkspaceOpenDocumentRequestedCommand : Command;

public sealed class WorkspaceImportDocumentRequestedCommand : Command;

public sealed class WorkspaceOpenFolderRequestedCommand : Command;

public sealed class WorkspaceOpenUserManualRequestedCommand : Command;

public sealed class WorkspaceFolderFileSelectedCommand(MindMapFileItem? file) : Command
{
    public MindMapFileItem? File { get; } = file;
}

public sealed class WorkspaceSelectedNodeChangedCommand(MindMapNode? node) : Command
{
    public MindMapNode? Node { get; } = node;
}

public sealed class WorkspaceMarkdownTextChangedCommand(string text) : Command
{
    public string Text { get; } = text;
}

public sealed class WorkspaceAddChildRequestedCommand : Command;

public sealed class WorkspaceAddSiblingRequestedCommand : Command;

public sealed class WorkspaceCopyMarkdownRequestedCommand : Command;

public sealed class WorkspaceToggleEditorModeRequestedCommand : Command;

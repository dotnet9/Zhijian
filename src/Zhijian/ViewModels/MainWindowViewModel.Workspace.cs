using System.Collections.Specialized;
using System.ComponentModel;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel
{
    private void SubscribeWorkspaceEvents()
    {
        _workspaceEventBus.Subscribe<WorkspaceOpenDocumentRequestedCommand>(_ => OpenDocumentAsync());
        _workspaceEventBus.Subscribe<WorkspaceImportDocumentRequestedCommand>(_ => ImportDocumentAsync());
        _workspaceEventBus.Subscribe<WorkspaceOpenFolderRequestedCommand>(_ => OpenFolderAsync());
        _workspaceEventBus.Subscribe<WorkspaceOpenUserManualRequestedCommand>(_ => OpenUserManualAsync());
        _workspaceEventBus.Subscribe<WorkspaceFolderFileSelectedCommand>(HandleWorkspaceFolderFileSelected);
        _workspaceEventBus.Subscribe<WorkspaceSelectedNodeChangedCommand>(HandleWorkspaceSelectedNodeChanged);
        _workspaceEventBus.Subscribe<WorkspaceMarkdownTextChangedCommand>(HandleWorkspaceMarkdownTextChanged);
        _workspaceEventBus.Subscribe<WorkspaceAddChildRequestedCommand>(_ => AddChildToSelected());
        _workspaceEventBus.Subscribe<WorkspaceAddSiblingRequestedCommand>(_ => AddSiblingToSelected());
        _workspaceEventBus.Subscribe<WorkspaceCopyMarkdownRequestedCommand>(_ => CopyAsMarkdownAsync());
        _workspaceEventBus.Subscribe<WorkspaceToggleEditorModeRequestedCommand>(_ => ToggleEditorMode());
    }

    private void HandleWorkspaceFolderFileSelected(WorkspaceFolderFileSelectedCommand command)
    {
        if (!Equals(SelectedFolderFile, command.File))
        {
            SelectedFolderFile = command.File;
        }
    }

    private void HandleWorkspaceSelectedNodeChanged(WorkspaceSelectedNodeChangedCommand command)
    {
        if (!ReferenceEquals(SelectedNode, command.Node))
        {
            SelectedNode = command.Node;
        }
    }

    private void HandleWorkspaceMarkdownTextChanged(WorkspaceMarkdownTextChangedCommand command)
    {
        if (!string.Equals(MarkdownText, command.Text, StringComparison.Ordinal))
        {
            MarkdownText = command.Text;
        }
    }

    private void HandleWorkspaceStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SelectedFolderFile):
            case nameof(HasFolderFiles):
            case nameof(IsFolderEmpty):
            case nameof(FolderSummary):
                PublishWorkspaceFilesState();
                break;
            case nameof(PanelBackground):
            case nameof(PanelBorderBrush):
            case nameof(PrimaryTextBrush):
            case nameof(SecondaryTextBrush):
                PublishWorkspaceFilesState();
                PublishWorkspaceOutlineState();
                break;
            case nameof(SelectedNode):
            case nameof(IsOutlineMode):
            case nameof(IsMarkdownMode):
            case nameof(IsDarkTheme):
            case nameof(MarkdownText):
            case nameof(EditorPaneTitle):
            case nameof(ToggleEditorToolTip):
            case nameof(CanAddSiblingToSelectedNode):
                PublishWorkspaceOutlineState();
                break;
        }
    }

    private void HandleWorkspaceRootsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PublishWorkspaceOutlineState();
    }

    private void HandleWorkspaceFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        PublishWorkspaceFilesState();
    }

    private void PublishWorkspaceFilesState()
    {
        _workspaceEventBus.Publish(new WorkspaceFilesStateChangedCommand(new WorkspaceFilesState
        {
            FolderFiles = FolderFiles.ToArray(),
            SelectedFolderFile = SelectedFolderFile,
            HasFolderFiles = HasFolderFiles,
            IsFolderEmpty = IsFolderEmpty,
            FolderSummary = FolderSummary,
            PanelBorderBrush = PanelBorderBrush,
            PrimaryTextBrush = PrimaryTextBrush,
            SecondaryTextBrush = SecondaryTextBrush
        }));
    }

    private void PublishWorkspaceOutlineState()
    {
        _workspaceEventBus.Publish(new WorkspaceOutlineStateChangedCommand(new WorkspaceOutlineState
        {
            Roots = Roots.ToArray(),
            SelectedNode = SelectedNode,
            IsOutlineMode = IsOutlineMode,
            IsMarkdownMode = IsMarkdownMode,
            IsDarkTheme = IsDarkTheme,
            MarkdownText = MarkdownText,
            EditorPaneTitle = EditorPaneTitle,
            ToggleEditorToolTip = ToggleEditorToolTip,
            PanelBackground = PanelBackground,
            PanelBorderBrush = PanelBorderBrush,
            PrimaryTextBrush = PrimaryTextBrush,
            SecondaryTextBrush = SecondaryTextBrush,
            CanAddSiblingToSelectedNode = CanAddSiblingToSelectedNode
        }));
    }
}

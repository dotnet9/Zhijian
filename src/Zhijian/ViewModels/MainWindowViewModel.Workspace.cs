using System.Collections.Specialized;
using System.ComponentModel;
using CodeWF.EventBus;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel
{
    [EventHandler]
    private Task HandleWorkspaceOpenDocumentRequested(WorkspaceOpenDocumentRequestedCommand command)
    {
        return OpenDocumentAsync();
    }

    [EventHandler]
    private Task HandleWorkspaceImportDocumentRequested(WorkspaceImportDocumentRequestedCommand command)
    {
        return ImportDocumentAsync();
    }

    [EventHandler]
    private Task HandleWorkspaceOpenFolderRequested(WorkspaceOpenFolderRequestedCommand command)
    {
        return OpenFolderAsync();
    }

    [EventHandler]
    private Task HandleWorkspaceOpenUserManualRequested(WorkspaceOpenUserManualRequestedCommand command)
    {
        return OpenUserManualAsync();
    }

    [EventHandler]
    private void HandleWorkspaceFolderFileSelected(WorkspaceFolderFileSelectedCommand command)
    {
        if (!Equals(SelectedFolderFile, command.File))
        {
            SelectedFolderFile = command.File;
        }
    }

    [EventHandler]
    private void HandleWorkspaceSelectedNodeChanged(WorkspaceSelectedNodeChangedCommand command)
    {
        if (!ReferenceEquals(SelectedNode, command.Node))
        {
            SelectedNode = command.Node;
        }
    }

    [EventHandler]
    private void HandleWorkspaceMarkdownTextChanged(WorkspaceMarkdownTextChangedCommand command)
    {
        if (!string.Equals(MarkdownText, command.Text, StringComparison.Ordinal))
        {
            MarkdownText = command.Text;
        }
    }

    [EventHandler]
    private void HandleWorkspaceAddChildRequested(WorkspaceAddChildRequestedCommand command)
    {
        AddChildToSelected();
    }

    [EventHandler]
    private void HandleWorkspaceAddSiblingRequested(WorkspaceAddSiblingRequestedCommand command)
    {
        AddSiblingToSelected();
    }

    [EventHandler]
    private Task HandleWorkspaceCopyMarkdownRequested(WorkspaceCopyMarkdownRequestedCommand command)
    {
        return CopyAsMarkdownAsync();
    }

    [EventHandler]
    private void HandleWorkspaceToggleEditorModeRequested(WorkspaceToggleEditorModeRequestedCommand command)
    {
        ToggleEditorMode();
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

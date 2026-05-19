using System.Collections.ObjectModel;
using CodeWF.EventBus;

namespace Zhijian.ViewModels;

public sealed class WorkspaceFilesViewModel : ViewModelBase
{
    private readonly IEventBus _eventBus = EventBus.Default;
    private bool _isApplyingState;
    private MindMapFileItem? _selectedFolderFile;
    private bool _hasFolderFiles;
    private bool _isFolderEmpty = true;
    private string _folderSummary = string.Empty;
    private string _panelBorderBrush = string.Empty;
    private string _primaryTextBrush = string.Empty;
    private string _secondaryTextBrush = string.Empty;

    public WorkspaceFilesViewModel()
    {
        _eventBus.Subscribe(this);
    }

    public ObservableCollection<MindMapFileItem> FolderFiles { get; } = [];

    public MindMapFileItem? SelectedFolderFile
    {
        get => _selectedFolderFile;
        set
        {
            if (SetProperty(ref _selectedFolderFile, value) && !_isApplyingState)
            {
                _eventBus.Publish(new WorkspaceFolderFileSelectedCommand(value));
            }
        }
    }

    public bool HasFolderFiles
    {
        get => _hasFolderFiles;
        private set => SetProperty(ref _hasFolderFiles, value);
    }

    public bool IsFolderEmpty
    {
        get => _isFolderEmpty;
        private set => SetProperty(ref _isFolderEmpty, value);
    }

    public string FolderSummary
    {
        get => _folderSummary;
        private set => SetProperty(ref _folderSummary, value);
    }

    public string PanelBorderBrush
    {
        get => _panelBorderBrush;
        private set => SetProperty(ref _panelBorderBrush, value);
    }

    public string PrimaryTextBrush
    {
        get => _primaryTextBrush;
        private set => SetProperty(ref _primaryTextBrush, value);
    }

    public string SecondaryTextBrush
    {
        get => _secondaryTextBrush;
        private set => SetProperty(ref _secondaryTextBrush, value);
    }

    public Task OpenDocumentAsync()
    {
        return _eventBus.PublishAsync(new WorkspaceOpenDocumentRequestedCommand());
    }

    public Task ImportDocumentAsync()
    {
        return _eventBus.PublishAsync(new WorkspaceImportDocumentRequestedCommand());
    }

    public Task OpenFolderAsync()
    {
        return _eventBus.PublishAsync(new WorkspaceOpenFolderRequestedCommand());
    }

    public Task OpenUserManualAsync()
    {
        return _eventBus.PublishAsync(new WorkspaceOpenUserManualRequestedCommand());
    }

    [EventHandler]
    private void ApplyState(WorkspaceFilesStateChangedCommand command)
    {
        _isApplyingState = true;
        try
        {
            SyncFolderFiles(command.State.FolderFiles);
            SelectedFolderFile = command.State.SelectedFolderFile;
            HasFolderFiles = command.State.HasFolderFiles;
            IsFolderEmpty = command.State.IsFolderEmpty;
            FolderSummary = command.State.FolderSummary;
            PanelBorderBrush = command.State.PanelBorderBrush;
            PrimaryTextBrush = command.State.PrimaryTextBrush;
            SecondaryTextBrush = command.State.SecondaryTextBrush;
        }
        finally
        {
            _isApplyingState = false;
        }
    }

    private void SyncFolderFiles(IReadOnlyList<MindMapFileItem> files)
    {
        if (FolderFiles.Count == files.Count && FolderFiles.SequenceEqual(files))
        {
            return;
        }

        FolderFiles.Clear();
        foreach (var file in files)
        {
            FolderFiles.Add(file);
        }
    }
}

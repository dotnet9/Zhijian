using System.Collections.ObjectModel;
using CodeWF.EventBus;
using CodeWF.MindView;

namespace Zhijian.ViewModels;

public sealed class WorkspaceOutlineViewModel : ViewModelBase
{
    private readonly IEventBus _eventBus = EventBus.Default;
    private bool _isApplyingState;
    private MindMapNode? _selectedNode;
    private bool _isOutlineMode = true;
    private bool _isMarkdownMode;
    private bool _isDarkTheme;
    private string _markdownText = string.Empty;
    private string _editorPaneTitle = string.Empty;
    private string _toggleEditorToolTip = string.Empty;
    private string _panelBackground = string.Empty;
    private string _panelBorderBrush = string.Empty;
    private string _primaryTextBrush = string.Empty;
    private string _secondaryTextBrush = string.Empty;
    private bool _canAddSiblingToSelectedNode;

    public WorkspaceOutlineViewModel()
    {
        _eventBus.Subscribe(this);
    }

    public ObservableCollection<MindMapNode> Roots { get; } = [];

    public MindMapNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value) && !_isApplyingState)
            {
                _eventBus.Publish(new WorkspaceSelectedNodeChangedCommand(value));
            }
        }
    }

    public bool IsOutlineMode
    {
        get => _isOutlineMode;
        private set => SetProperty(ref _isOutlineMode, value);
    }

    public bool IsMarkdownMode
    {
        get => _isMarkdownMode;
        private set => SetProperty(ref _isMarkdownMode, value);
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        private set => SetProperty(ref _isDarkTheme, value);
    }

    public string MarkdownText
    {
        get => _markdownText;
        set
        {
            if (SetProperty(ref _markdownText, value) && !_isApplyingState)
            {
                _eventBus.Publish(new WorkspaceMarkdownTextChangedCommand(value));
            }
        }
    }

    public string EditorPaneTitle
    {
        get => _editorPaneTitle;
        private set => SetProperty(ref _editorPaneTitle, value);
    }

    public string ToggleEditorToolTip
    {
        get => _toggleEditorToolTip;
        private set => SetProperty(ref _toggleEditorToolTip, value);
    }

    public string PanelBackground
    {
        get => _panelBackground;
        private set => SetProperty(ref _panelBackground, value);
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

    public bool CanAddSiblingToSelectedNode
    {
        get => _canAddSiblingToSelectedNode;
        private set => SetProperty(ref _canAddSiblingToSelectedNode, value);
    }

    public void AddChildToSelected()
    {
        _eventBus.Publish(new WorkspaceAddChildRequestedCommand());
    }

    public void AddSiblingToSelected()
    {
        _eventBus.Publish(new WorkspaceAddSiblingRequestedCommand());
    }

    public Task CopyAsMarkdownAsync()
    {
        return _eventBus.PublishAsync(new WorkspaceCopyMarkdownRequestedCommand());
    }

    public void ToggleEditorMode()
    {
        _eventBus.Publish(new WorkspaceToggleEditorModeRequestedCommand());
    }

    [EventHandler]
    private void ApplyState(WorkspaceOutlineStateChangedCommand command)
    {
        _isApplyingState = true;
        try
        {
            SyncRoots(command.State.Roots);
            SelectedNode = command.State.SelectedNode;
            IsOutlineMode = command.State.IsOutlineMode;
            IsMarkdownMode = command.State.IsMarkdownMode;
            IsDarkTheme = command.State.IsDarkTheme;
            MarkdownText = command.State.MarkdownText;
            EditorPaneTitle = command.State.EditorPaneTitle;
            ToggleEditorToolTip = command.State.ToggleEditorToolTip;
            PanelBackground = command.State.PanelBackground;
            PanelBorderBrush = command.State.PanelBorderBrush;
            PrimaryTextBrush = command.State.PrimaryTextBrush;
            SecondaryTextBrush = command.State.SecondaryTextBrush;
            CanAddSiblingToSelectedNode = command.State.CanAddSiblingToSelectedNode;
        }
        finally
        {
            _isApplyingState = false;
        }
    }

    private void SyncRoots(IReadOnlyList<MindMapNode> roots)
    {
        if (Roots.Count == roots.Count && Roots.SequenceEqual(roots))
        {
            return;
        }

        Roots.Clear();
        foreach (var root in roots)
        {
            Roots.Add(root);
        }
    }
}

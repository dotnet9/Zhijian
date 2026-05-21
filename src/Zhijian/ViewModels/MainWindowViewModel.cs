using AtomUI.Controls;
using Avalonia;
using CodeWF.EventBus;
using CodeWF.MindView;
using Lang.Avalonia;
using System.Collections.ObjectModel;
using Zhijian.Services;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IMindMapEditorController
{
    private static readonly int MaxHistorySteps = ApplicationSettings.MaxHistorySteps;
    private static readonly int MaxRecentFiles = ApplicationSettings.MaxRecentFiles;
    private static readonly string RecentFilesName = ApplicationSettings.RecentFilesFileName;
    private static readonly string TourSeenName = ApplicationSettings.TourSeenFileName;
    private const string MarkdownStatsSummaryKey = "Zhijian.ZhijianL.MarkdownStatsSummary";
    private const string MarkdownStatsSavedKey = "Zhijian.ZhijianL.MarkdownStatsSaved";
    private const string MarkdownStatsUnsavedKey = "Zhijian.ZhijianL.MarkdownStatsUnsaved";
    private const string QuickStartTitleKey = "Zhijian.ZhijianL.QuickStartTitle";
    private const string QuickStartDescriptionKey = "Zhijian.ZhijianL.QuickStartDescription";
    private const string NewFromTemplateKey = "Zhijian.ZhijianL.NewFromTemplate";
    private const string ProductPlanTemplateKey = "Zhijian.ZhijianL.ProductPlanTemplate";
    private const string ProductPlanTemplateDescriptionKey = "Zhijian.ZhijianL.ProductPlanTemplateDescription";
    private const string ProductPlanTemplateMarkdownKey = "Zhijian.ZhijianL.ProductPlanTemplateMarkdown";
    private const string MeetingNotesTemplateKey = "Zhijian.ZhijianL.MeetingNotesTemplate";
    private const string MeetingNotesTemplateDescriptionKey = "Zhijian.ZhijianL.MeetingNotesTemplateDescription";
    private const string MeetingNotesTemplateMarkdownKey = "Zhijian.ZhijianL.MeetingNotesTemplateMarkdown";
    private const string StudyNotesTemplateKey = "Zhijian.ZhijianL.StudyNotesTemplate";
    private const string StudyNotesTemplateDescriptionKey = "Zhijian.ZhijianL.StudyNotesTemplateDescription";
    private const string StudyNotesTemplateMarkdownKey = "Zhijian.ZhijianL.StudyNotesTemplateMarkdown";
    private const string StatusTemplateAppliedKey = "Zhijian.ZhijianL.StatusTemplateApplied";
    private const string HistoryTemplateAppliedKey = "Zhijian.ZhijianL.HistoryTemplateApplied";

    private static readonly string[] Palette =
    [
        "#2563EB",
        "#16A34A",
        "#F97316",
        "#DB2777",
        "#7C3AED",
        "#0891B2",
        "#DC2626",
        "#CA8A04",
        "#0D9488",
        "#4F46E5"
    ];

    private readonly IMindMapFileService _fileService;
    private readonly IApplicationActionService _applicationActionService;
    private readonly RecentFileStore _recentFileStore;
    private readonly IEventBus _workspaceEventBus;
    private readonly HashSet<MindMapNode> _observedNodes = [];
    private readonly List<HistoryEntry> _history = [];
    private int _nextPaletteIndex = Random.Shared.Next(Palette.Length);
    private int _historyIndex = -1;
    private bool _isApplyingMarkdown;
    private bool _isSyncingMarkdownFromTree;
    private bool _isRestoringHistory;
    private bool _isLoadingDocument;
    private bool _isDocumentBusy;
    private int _documentBusyDepth;
    private bool _hasOpenedNewUserTour;
    private string _selectedCultureName = ApplicationSettings.DefaultCultureName;
    private MindMapFileFormat _currentFileFormat = MindMapFileFormat.Markdown;

    private MindMapNode? _selectedNode;
    private bool _isMarkdownMode;
    private bool _isDarkTheme;
    private string _markdownText = string.Empty;
    private string _statusText = string.Empty;
    private string _documentBusyText = string.Empty;
    private string? _currentFilePath;
    private bool _isDirty;
    private int _workspaceTabIndex = 1;
    private MindMapFileItem? _selectedFolderFile;
    private bool _isNewUserTourOpen;
    private bool _isWorkspacePaneVisible = true;
    private int _mindMapViewportResetRequestId;

    public MainWindowViewModel()
        : this(new DisabledMindMapFileService(), new DisabledApplicationActionService())
    {
    }

    public MainWindowViewModel(IMindMapFileService fileService)
        : this(fileService, new DisabledApplicationActionService())
    {
    }

    public MainWindowViewModel(
        IMindMapFileService fileService,
        IApplicationActionService applicationActionService,
        string? startupFilePath = null)
    {
        _fileService = fileService;
        _applicationActionService = applicationActionService;
        _recentFileStore = new RecentFileStore(ApplicationSettings.GetUserDataPath(RecentFilesName), MaxRecentFiles);
        _workspaceEventBus = EventBus.Default;
        _workspaceEventBus.Subscribe(this);
        Roots = new ObservableCollection<MindMapNode> { CreateBlankRoot() };
        FolderFiles = [];
        RecentFiles = [];
        FilesPane = new WorkspaceFilesViewModel();
        OutlinePane = new WorkspaceOutlineViewModel();
        PropertyChanged += HandleWorkspaceStatePropertyChanged;
        Roots.CollectionChanged += HandleWorkspaceRootsChanged;
        FolderFiles.CollectionChanged += HandleWorkspaceFilesChanged;

        WatchTree();
        AssignMissingColors(Root);
        IsDarkTheme = Application.Current?.IsDarkThemeMode() ?? false;
        _selectedCultureName = I18nManager.Instance.Culture?.Name ?? ApplicationSettings.DefaultCultureName;
        StatusText = T(ZhijianL.Ready);
        SelectedNode = Root;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep(T(ZhijianL.EmptyMindMap));
        MarkDocumentClean();
        PublishWorkspaceFilesState();
        PublishWorkspaceOutlineState();
        _ = LoadRecentFilesAsync();
        _ = LoadStartupDocumentAsync(startupFilePath);
        _ = InitializeNewUserTourAsync();
    }

    public ObservableCollection<MindMapNode> Roots { get; }

    public ObservableCollection<MindMapFileItem> FolderFiles { get; }

    public ObservableCollection<RecentFileItem> RecentFiles { get; }

    public WorkspaceFilesViewModel FilesPane { get; }

    public WorkspaceOutlineViewModel OutlinePane { get; }

    public MindMapNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (SetProperty(ref _selectedNode, value))
            {
                OnSelectedNodeChanged(value);
            }
        }
    }

    public bool IsMarkdownMode
    {
        get => _isMarkdownMode;
        set
        {
            if (SetProperty(ref _isMarkdownMode, value))
            {
                OnIsMarkdownModeChanged(value);
            }
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                OnIsDarkThemeChanged(value);
            }
        }
    }

    public string MarkdownText
    {
        get => _markdownText;
        set
        {
            if (SetProperty(ref _markdownText, value))
            {
                OnMarkdownTextChanged(value);
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public bool IsDocumentBusy
    {
        get => _isDocumentBusy;
        private set => SetProperty(ref _isDocumentBusy, value);
    }

    public string DocumentBusyText
    {
        get => _documentBusyText;
        private set => SetProperty(ref _documentBusyText, value);
    }

    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set
        {
            if (SetProperty(ref _currentFilePath, value))
            {
                OnCurrentFilePathChanged(value);
            }
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (SetProperty(ref _isDirty, value))
            {
                OnIsDirtyChanged(value);
            }
        }
    }

    public int WorkspaceTabIndex
    {
        get => _workspaceTabIndex;
        set => SetProperty(ref _workspaceTabIndex, value);
    }

    public MindMapFileItem? SelectedFolderFile
    {
        get => _selectedFolderFile;
        set
        {
            if (SetProperty(ref _selectedFolderFile, value))
            {
                OnSelectedFolderFileChanged(value);
            }
        }
    }

    public bool IsNewUserTourOpen
    {
        get => _isNewUserTourOpen;
        set
        {
            if (SetProperty(ref _isNewUserTourOpen, value))
            {
                OnIsNewUserTourOpenChanged(value);
            }
        }
    }

    public bool IsWorkspacePaneVisible
    {
        get => _isWorkspacePaneVisible;
        set
        {
            if (SetProperty(ref _isWorkspacePaneVisible, value))
            {
                OnIsWorkspacePaneVisibleChanged(value);
            }
        }
    }

    public bool IsWorkspacePaneHidden => !IsWorkspacePaneVisible;

    public int MindMapViewportResetRequestId
    {
        get => _mindMapViewportResetRequestId;
        private set => SetProperty(ref _mindMapViewportResetRequestId, value);
    }

    public MindMapNode Root => Roots[0];

    public bool IsOutlineMode => !IsMarkdownMode;

    public string EditorPaneTitle => IsMarkdownMode ? MarkdownStatsSummary : T(ZhijianL.OutlineTab);

    public string MarkdownStatsSummary => FormatText(
        MarkdownStatsSummaryKey,
        CountMarkdownLines(MarkdownText),
        CountMarkdownCharacters(MarkdownText),
        NodeCount,
        T(IsDirty ? MarkdownStatsUnsavedKey : MarkdownStatsSavedKey));

    public string QuickStartTitleText => T(QuickStartTitleKey);

    public string QuickStartDescriptionText => T(QuickStartDescriptionKey);

    public string NewFromTemplateText => T(NewFromTemplateKey);

    public string ProductPlanTemplateText => T(ProductPlanTemplateKey);

    public string ProductPlanTemplateDescriptionText => T(ProductPlanTemplateDescriptionKey);

    public string MeetingNotesTemplateText => T(MeetingNotesTemplateKey);

    public string MeetingNotesTemplateDescriptionText => T(MeetingNotesTemplateDescriptionKey);

    public string StudyNotesTemplateText => T(StudyNotesTemplateKey);

    public string StudyNotesTemplateDescriptionText => T(StudyNotesTemplateDescriptionKey);

    public string ToggleEditorToolTip => IsMarkdownMode ? T(ZhijianL.ToggleToOutline) : T(ZhijianL.ToggleToMarkdown);

    public string CenterRootToolTip => $"{T(ZhijianL.CenterRoot)}  {PrimaryCommandText} + L";

    public string ZoomOutToolTip => $"{T(ZhijianL.ZoomOut)}  {PrimaryCommandText} + -";

    public string ZoomInToolTip => $"{T(ZhijianL.ZoomIn)}  {PrimaryCommandText} + +";

    public string ResetZoomToolTip => $"{T(ZhijianL.ResetZoom)}  {PrimaryCommandText} + 0";

    public string ToggleWorkspacePaneText => IsWorkspacePaneVisible ? T(ZhijianL.HideWorkspacePane) : T(ZhijianL.ShowWorkspacePane);

    public string ToggleWorkspacePaneToolTip => $"{(IsWorkspacePaneVisible ? T(ZhijianL.HideWorkspacePane) : T(ZhijianL.ShowWorkspacePane))}  {PrimaryCommandText} + B";

    public string SelectedNodeSummary => SelectedNode is null
        ? FormatText(ZhijianL.NodeSummary, NodeCount)
        : FormatText(ZhijianL.NodeSummarySelected, NodeCount, GetLevel(SelectedNode), SelectedNode.Children.Count);

    public int NodeCount => FlattenNodes().Count;

    public bool CanUndo => _historyIndex > 0;

    public bool CanRedo => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

    public string HistorySummary => _history.Count == 0
        ? FormatText(ZhijianL.HistorySummary, 0, 0)
        : FormatText(ZhijianL.HistorySummary, _historyIndex + 1, _history.Count);

    public string WindowTitle => $"{DocumentTitle} - {T(ZhijianL.AppName)}";

    public string DocumentTitle => $"{(IsDirty ? "*" : string.Empty)}{CurrentDocumentName}";

    public string CurrentDocumentName => string.IsNullOrWhiteSpace(CurrentFilePath)
        ? T(ZhijianL.Untitled)
        : Path.GetFileName(CurrentFilePath);

    public bool HasFolderFiles => FolderFiles.Count > 0;

    public bool IsFolderEmpty => !HasFolderFiles;

    public string FolderSummary => HasFolderFiles
        ? FormatText(ZhijianL.FolderSummaryOpen, FolderFiles.Count)
        : T(ZhijianL.FolderSummaryClosed);

    public bool IsLightTheme => !IsDarkTheme;

    public bool IsSimplifiedChinese => string.Equals(_selectedCultureName, "zh-CN", StringComparison.OrdinalIgnoreCase);

    public bool IsTraditionalChinese => string.Equals(_selectedCultureName, "zh-Hant", StringComparison.OrdinalIgnoreCase);

    public bool IsEnglish => string.Equals(_selectedCultureName, "en-US", StringComparison.OrdinalIgnoreCase);

    public bool IsJapanese => string.Equals(_selectedCultureName, "ja-JP", StringComparison.OrdinalIgnoreCase);

    public string ShellBackground => IsDarkTheme ? "#111827" : "#F3F6FA";

    public string PanelBackground => IsDarkTheme ? "#1F2937" : "#FFFFFF";

    public string PanelFooterBackground => IsDarkTheme ? "#111827" : "#F8FAFC";

    public string PanelBorderBrush => IsDarkTheme ? "#374151" : "#DDE3EA";

    public string TitleBarBackground => IsDarkTheme ? "#111827" : "#FFFFFF";

    public string PrimaryTextBrush => IsDarkTheme ? "#F9FAFB" : "#0F172A";

    public string SecondaryTextBrush => IsDarkTheme ? "#CBD5E1" : "#667085";

    public bool HasCurrentFile => !string.IsNullOrWhiteSpace(CurrentFilePath);

    public bool IsBlankDocument => !HasCurrentFile && !IsDirty && NodeCount == 1;

    public bool CanPromoteSelectedNode => CanPromoteNode(SelectedNode);

    public bool CanDemoteSelectedNode => CanDemoteNode(SelectedNode);

    public bool CanMoveSelectedNodeUp => CanMoveNodeUp(SelectedNode);

    public bool CanMoveSelectedNodeDown => CanMoveNodeDown(SelectedNode);

    public bool CanDeleteSelectedNode => SelectedNode is not null && !IsRoot(SelectedNode);

    public bool CanAddSiblingToSelectedNode => SelectedNode is not null && !IsRoot(SelectedNode);

}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using AtomUI.Controls;
using AtomUI.Theme.Language;
using Avalonia;
using CodeWF.MindView;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lang.Avalonia;
using Zhijian.Services;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IMindMapEditorController
{
    private const double HorizontalGap = MindMapLayoutMetrics.DefaultHorizontalSpacing;
    private const double VerticalSpacing = MindMapLayoutMetrics.DefaultVerticalSpacing;
    private const double RootX = 72;
    private const double RootY = 72;
    private const double MinNodeY = 24;
    private static readonly int MaxHistorySteps = ApplicationSettings.MaxHistorySteps;
    private static readonly int MaxRecentFiles = ApplicationSettings.MaxRecentFiles;
    private static readonly string RecentFilesName = ApplicationSettings.RecentFilesFileName;
    private static readonly string TourSeenName = ApplicationSettings.TourSeenFileName;

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
    private readonly HashSet<MindMapNode> _observedNodes = [];
    private readonly List<HistoryEntry> _history = [];
    private int _nextPaletteIndex = Random.Shared.Next(Palette.Length);
    private int _historyIndex = -1;
    private bool _isApplyingMarkdown;
    private bool _isSyncingMarkdownFromTree;
    private bool _isRestoringHistory;
    private bool _isLoadingDocument;
    private bool _hasOpenedNewUserTour;
    private string _selectedCultureName = ApplicationSettings.DefaultCultureName;
    private MindMapFileFormat _currentFileFormat = MindMapFileFormat.Markdown;

    [ObservableProperty]
    private MindMapNode? _selectedNode;

    [ObservableProperty]
    private bool _isMarkdownMode;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private string _markdownText = string.Empty;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private string? _currentFilePath;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private int _workspaceTabIndex = 1;

    [ObservableProperty]
    private MindMapFileItem? _selectedFolderFile;

    [ObservableProperty]
    private bool _isNewUserTourOpen;

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
        Roots = new ObservableCollection<MindMapNode> { CreateBlankRoot() };
        FolderFiles = [];
        RecentFiles = [];

        WatchTree();
        AssignMissingColors(Root);
        IsDarkTheme = Application.Current?.IsDarkThemeMode() ?? false;
        _selectedCultureName = I18nManager.Instance.Culture?.Name ?? ApplicationSettings.DefaultCultureName;
        StatusText = T(ZhijianL.Ready);
        SelectedNode = Root;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("空白脑图");
        MarkDocumentClean();
        LoadRecentFiles();
        LoadStartupDocument(startupFilePath);
        InitializeNewUserTour();
    }

    public ObservableCollection<MindMapNode> Roots { get; }

    public ObservableCollection<MindMapFileItem> FolderFiles { get; }

    public ObservableCollection<RecentFileItem> RecentFiles { get; }

    public MindMapNode Root => Roots[0];

    public bool IsOutlineMode => !IsMarkdownMode;

    public string EditorPaneTitle => IsMarkdownMode ? "Markdown" : T(ZhijianL.OutlineTab);

    public string ToggleEditorToolTip => IsMarkdownMode ? T(ZhijianL.ToggleToOutline) : T(ZhijianL.ToggleToMarkdown);

    public string CenterRootToolTip => $"{T(ZhijianL.CenterRoot)}  {PrimaryCommandText} + L";

    public string SelectedNodeSummary => SelectedNode is null
        ? FormatText(ZhijianL.NodeSummary, NodeCount)
        : FormatText(ZhijianL.NodeSummarySelected, NodeCount, GetLevel(SelectedNode), SelectedNode.Children.Count);

    public int NodeCount => FlattenNodes().Count;

    public bool CanUndo => _historyIndex > 0;

    public bool CanRedo => _historyIndex >= 0 && _historyIndex < _history.Count - 1;

    public string HistorySummary => _history.Count == 0
        ? FormatText(ZhijianL.HistorySummary, 0, 0)
        : FormatText(ZhijianL.HistorySummary, _historyIndex + 1, _history.Count);

    public string WindowTitle => T(ZhijianL.AppName);

    public string DocumentTitle => $"{(IsDirty ? "*" : string.Empty)}{CurrentDocumentName}";

    public string CurrentDocumentName => string.IsNullOrWhiteSpace(CurrentFilePath)
        ? T(ZhijianL.Untitled)
        : Path.GetFileName(CurrentFilePath);

    public bool HasFolderFiles => FolderFiles.Count > 0;

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

    public bool HasCurrentFile => !string.IsNullOrWhiteSpace(CurrentFilePath) && File.Exists(CurrentFilePath);

    public bool CanPromoteSelectedNode => CanPromoteNode(SelectedNode);

    public bool CanDemoteSelectedNode => CanDemoteNode(SelectedNode);

    public bool CanMoveSelectedNodeUp => CanMoveNodeUp(SelectedNode);

    public bool CanMoveSelectedNodeDown => CanMoveNodeDown(SelectedNode);

    public bool CanDeleteSelectedNode => SelectedNode is not null && !IsRoot(SelectedNode);

    [RelayCommand]
    private async Task NewDocumentAsync()
    {
        if (!await EnsureCanChangeDocumentAsync())
        {
            return;
        }

        SetBlankDocument();
        StatusText = "已新建空白脑图";
    }

    [RelayCommand]
    private void NewWindow()
    {
        _applicationActionService.OpenNewWindow();
        StatusText = "已打开新窗口";
    }

    [RelayCommand]
    private async Task OpenDocumentAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            if (!await EnsureCanChangeDocumentAsync())
            {
                return;
            }

            StatusText = "正在打开脑图文件...";
            var result = await _fileService.OpenAsync();
            if (result is null)
            {
                return;
            }

            LoadOpenResult(result);
        });
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            StatusText = "正在打开文件夹...";
            var folderPath = await _fileService.PickFolderAsync();
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return;
            }

            LoadFolderFiles(folderPath);
            WorkspaceTabIndex = 0;
            StatusText = $"已打开文件夹：{folderPath}";
        });
    }

    [RelayCommand]
    private async Task OpenRecentFileAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        await RunFileOperationAsync(async () =>
        {
            if (!await EnsureCanChangeDocumentAsync())
            {
                return;
            }

            LoadFilePath(filePath);
        });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunFileOperationAsync(async () => await SaveDocumentAsync(forceSaveAs: false));
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        await RunFileOperationAsync(async () => await SaveDocumentAsync(forceSaveAs: true));
    }

    [RelayCommand(CanExecute = nameof(HasCurrentFile))]
    private void OpenFileLocation()
    {
        if (CurrentFilePath is null)
        {
            return;
        }

        _applicationActionService.OpenFileLocation(CurrentFilePath);
        StatusText = "已打开文件位置";
    }

    [RelayCommand]
    private void Close()
    {
        _applicationActionService.CloseMainWindow();
    }

    [RelayCommand]
    private void SetDarkTheme()
    {
        SetTheme(isDark: true);
    }

    [RelayCommand]
    private void SetLightTheme()
    {
        SetTheme(isDark: false);
    }

    [RelayCommand]
    private void SelectSimplifiedChinese()
    {
        SetLanguage("zh-CN");
    }

    [RelayCommand]
    private void SelectTraditionalChinese()
    {
        SetLanguage("zh-Hant");
    }

    [RelayCommand]
    private void SelectEnglish()
    {
        SetLanguage("en-US");
    }

    [RelayCommand]
    private void SelectJapanese()
    {
        SetLanguage("ja-JP");
    }

    [RelayCommand]
    private void AddSiblingToSelected()
    {
        AddSibling(SelectedNode);
    }

    [RelayCommand]
    private void AddChildToSelected()
    {
        AddChild(SelectedNode);
    }

    [RelayCommand(CanExecute = nameof(CanPromoteSelectedNode))]
    private void PromoteSelected()
    {
        PromoteNode(SelectedNode);
    }

    [RelayCommand(CanExecute = nameof(CanDemoteSelectedNode))]
    private void DemoteSelected()
    {
        DemoteNode(SelectedNode);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectedNodeUp))]
    private void MoveSelectedUp()
    {
        MoveNodeUp(SelectedNode);
    }

    [RelayCommand(CanExecute = nameof(CanMoveSelectedNodeDown))]
    private void MoveSelectedDown()
    {
        MoveNodeDown(SelectedNode);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSelectedNode))]
    private void DeleteSelected()
    {
        DeleteNode(SelectedNode);
    }

    [RelayCommand]
    private async Task CopyAsMarkdownAsync()
    {
        ApplyMarkdownEditsIfNeeded();
        var content = MindMapDocumentCodec.ToMarkdown(Root);
        await _applicationActionService.SetClipboardTextAsync(content);
        var message = T(ZhijianL.StatusCopiedMarkdown);
        StatusText = message;
        _applicationActionService.ShowSuccessMessage(message);
    }

    [RelayCommand]
    private void OpenFeedback()
    {
        _applicationActionService.OpenFeedback();
        StatusText = T(ZhijianL.Feedback);
    }

    [RelayCommand]
    private void OpenFeatureRequest()
    {
        _applicationActionService.OpenFeatureRequest();
        StatusText = T(ZhijianL.SubmitFeature);
    }

    [RelayCommand]
    private void OpenPullRequests()
    {
        _applicationActionService.OpenPullRequests();
        StatusText = T(ZhijianL.SubmitPr);
    }

    [RelayCommand]
    public void ToggleEditorMode()
    {
        if (IsMarkdownMode)
        {
            ApplyMarkdownToTree(refreshMarkdownText: false);
            IsMarkdownMode = false;
            StatusText = "已应用 Markdown 并切换到大纲视图";
            return;
        }

        SyncMarkdownFromTree();
        IsMarkdownMode = true;
        StatusText = "已切换到 Markdown 视图";
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        _historyIndex--;
        RestoreHistoryEntry(_history[_historyIndex]);
        StatusText = $"已后退：{_history[_historyIndex].Label}";
    }

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        _historyIndex++;
        RestoreHistoryEntry(_history[_historyIndex]);
        StatusText = $"已前进：{_history[_historyIndex].Label}";
    }

    public bool IsRoot(MindMapNode? node)
    {
        return Roots.Count > 0 && ReferenceEquals(node, Root);
    }

    public int GetLevel(MindMapNode node)
    {
        return Roots.Count == 0 ? 0 : GetLevel(Root, node, 1);
    }

    public IReadOnlyList<MindMapNode> FlattenNodes()
    {
        var nodes = new List<MindMapNode>();
        foreach (var root in Roots)
        {
            AppendFlattened(root, nodes);
        }

        return nodes;
    }

    public MindMapNode HandleOutlineEnter(MindMapNode node)
    {
        if (IsRoot(node) || node.Children.Count > 0)
        {
            return AddChild(node, string.Empty);
        }

        return AddSibling(node, string.Empty);
    }

    public MindMapNode HandleMapEnter(MindMapNode node)
    {
        return IsRoot(node) ? AddChild(node, string.Empty) : AddSibling(node, string.Empty);
    }

    public MindMapNode HandleMapTab(MindMapNode node)
    {
        return AddChild(node, string.Empty);
    }

    public MindMapNode AddChild(MindMapNode? parent, string title = "新主题")
    {
        parent ??= SelectedNode ?? Root;

        var child = CreateNode(title);
        parent.Children.Add(child);
        SelectedNode = child;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("添加子主题");
        StatusText = "已添加子主题";
        return child;
    }

    public MindMapNode AddSibling(MindMapNode? node, string title = "新主题")
    {
        node ??= SelectedNode ?? Root;
        if (IsRoot(node))
        {
            return AddChild(node, title);
        }

        var parent = FindParent(node) ?? Root;
        var sibling = CreateNode(title);
        var index = parent.Children.IndexOf(node);
        parent.Children.Insert(index + 1, sibling);
        SelectedNode = sibling;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("添加同级主题");
        StatusText = "已添加同级主题";
        return sibling;
    }

    public MindMapNode DeleteNode(MindMapNode? node)
    {
        node ??= SelectedNode ?? Root;
        if (IsRoot(node))
        {
            SelectedNode = Root;
            return Root;
        }

        var parent = FindParent(node) ?? Root;
        var index = parent.Children.IndexOf(node);
        var focusTarget = index > 0 ? parent.Children[index - 1] : parent;

        parent.Children.Remove(node);
        SelectedNode = focusTarget;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("删除主题");
        StatusText = "已删除主题";
        return focusTarget;
    }

    public bool DemoteNode(MindMapNode? node)
    {
        if (!CanDemoteNode(node) || node is null)
        {
            return false;
        }

        var parent = FindParent(node)!;
        var index = parent.Children.IndexOf(node);
        var newParent = parent.Children[index - 1];
        parent.Children.RemoveAt(index);
        newParent.Children.Add(node);
        SelectedNode = node;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("降级主题");
        StatusText = "已降级为子主题";
        return true;
    }

    public bool CanDemoteNode(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && parent.Children.IndexOf(node) > 0;
    }

    public bool PromoteNode(MindMapNode? node)
    {
        if (!CanPromoteNode(node) || node is null)
        {
            return false;
        }

        var parent = FindParent(node)!;
        var grandParent = FindParent(parent)!;
        parent.Children.Remove(node);
        var parentIndex = grandParent.Children.IndexOf(parent);
        grandParent.Children.Insert(parentIndex + 1, node);
        SelectedNode = node;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("升级主题");
        StatusText = "已提升为父级主题";
        return true;
    }

    public bool CanMoveNodeUp(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && parent.Children.IndexOf(node) > 0;
    }

    public bool MoveNodeUp(MindMapNode? node)
    {
        return MoveNodeWithinSiblings(node, -1, "上移主题", "已上移主题");
    }

    public bool CanMoveNodeDown(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        if (parent is null)
        {
            return false;
        }

        var index = parent.Children.IndexOf(node);
        return index >= 0 && index < parent.Children.Count - 1;
    }

    public bool MoveNodeDown(MindMapNode? node)
    {
        return MoveNodeWithinSiblings(node, 1, "下移主题", "已下移主题");
    }

    public bool CanPromoteNode(MindMapNode? node)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        return parent is not null && !IsRoot(parent) && FindParent(parent) is not null;
    }

    public bool CanMoveNode(MindMapNode? node, MindMapNode? target)
    {
        return node is not null
            && target is not null
            && !IsRoot(node)
            && !ReferenceEquals(node, target)
            && !IsDescendant(node, target);
    }

    public bool MoveNode(MindMapNode? node, MindMapNode? target, MindMapDropPlacement placement)
    {
        if (!CanMoveNode(node, target) || node is null || target is null)
        {
            return false;
        }

        var oldParent = FindParent(node);
        if (oldParent is null)
        {
            return false;
        }

        if (IsRoot(target) && placement is MindMapDropPlacement.Before or MindMapDropPlacement.After)
        {
            placement = MindMapDropPlacement.Child;
        }

        var newParent = placement == MindMapDropPlacement.Child
            ? target
            : FindParent(target) ?? Root;
        var insertionIndex = placement switch
        {
            MindMapDropPlacement.Before => newParent.Children.IndexOf(target),
            MindMapDropPlacement.After => newParent.Children.IndexOf(target) + 1,
            _ => newParent.Children.Count
        };

        var oldIndex = oldParent.Children.IndexOf(node);
        if (oldIndex < 0)
        {
            return false;
        }

        oldParent.Children.RemoveAt(oldIndex);
        if (ReferenceEquals(oldParent, newParent) && oldIndex < insertionIndex)
        {
            insertionIndex--;
        }

        insertionIndex = Math.Clamp(insertionIndex, 0, newParent.Children.Count);
        newParent.Children.Insert(insertionIndex, node);
        SelectedNode = node;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep("移动主题");
        StatusText = "已移动主题";
        return true;
    }

    private bool MoveNodeWithinSiblings(MindMapNode? node, int offset, string historyLabel, string statusText)
    {
        if (node is null || IsRoot(node))
        {
            return false;
        }

        var parent = FindParent(node);
        if (parent is null)
        {
            return false;
        }

        var oldIndex = parent.Children.IndexOf(node);
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= parent.Children.Count)
        {
            return false;
        }

        parent.Children.Move(oldIndex, newIndex);
        SelectedNode = node;
        AutoLayout();
        SyncMarkdownFromTree();
        RecordHistoryStep(historyLabel);
        StatusText = statusText;
        return true;
    }

    [RelayCommand]
    private void OpenWebsite()
    {
        _applicationActionService.OpenWebsite();
        StatusText = "已打开网站";
    }

    [RelayCommand]
    private void ShowChangelog()
    {
        _applicationActionService.ShowChangelog();
        StatusText = "已打开更新日志";
    }

    [RelayCommand]
    private void ShowAbout()
    {
        _applicationActionService.ShowAbout();
        StatusText = "已打开关于窗口";
    }

    [RelayCommand]
    private void ShowThanks()
    {
        _applicationActionService.ShowThanks();
        StatusText = "已打开感谢窗口";
    }

    [RelayCommand]
    private void OpenRepository()
    {
        _applicationActionService.OpenRepository();
        StatusText = "已打开 GitHub 仓库";
    }

    [RelayCommand]
    private void ShowNewUserTour()
    {
        _hasOpenedNewUserTour = true;
        PrepareNewUserTour();
        if (IsNewUserTourOpen)
        {
            IsNewUserTourOpen = false;
        }

        IsNewUserTourOpen = true;
        StatusText = T(ZhijianL.StatusShowNewUserTour);
    }

    [RelayCommand]
    private async Task ImportMarkdownAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenTextAsync(MindMapFileFormat.Markdown);
            if (content is null)
            {
                return;
            }

            ReplaceTree(MindMapDocumentCodec.FromMarkdown(content), "导入 Markdown");
            StatusText = "已导入 Markdown";
        });
    }

    [RelayCommand]
    private async Task ImportOpmlAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenTextAsync(MindMapFileFormat.Opml);
            if (content is null)
            {
                return;
            }

            ReplaceTree(MindMapDocumentCodec.FromOpml(content), "导入 OPML");
            StatusText = "已导入 OPML";
        });
    }

    [RelayCommand]
    private async Task ImportXMindAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = await _fileService.OpenBinaryAsync(MindMapFileFormat.XMind);
            if (content is null)
            {
                return;
            }

            ReplaceTree(MindMapDocumentCodec.FromXMind(content), "导入 XMind");
            StatusText = "已导入 XMind";
        });
    }

    [RelayCommand]
    private async Task ExportMarkdownAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            var content = IsMarkdownMode ? MarkdownText : MindMapDocumentCodec.ToMarkdown(Root);
            await _fileService.SaveTextAsync(MindMapFileFormat.Markdown, content);
            StatusText = "已导出 Markdown";
        });
    }

    [RelayCommand]
    private async Task ExportOpmlAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            ApplyMarkdownEditsIfNeeded();
            await _fileService.SaveTextAsync(MindMapFileFormat.Opml, MindMapDocumentCodec.ToOpml(Root));
            StatusText = "已导出 OPML";
        });
    }

    [RelayCommand]
    private async Task ExportXMindAsync()
    {
        await RunFileOperationAsync(async () =>
        {
            ApplyMarkdownEditsIfNeeded();
            await _fileService.SaveBinaryAsync(MindMapFileFormat.XMind, MindMapDocumentCodec.ToXMind(Root));
            StatusText = "已导出 XMind";
        });
    }

    private void SetTheme(bool isDark)
    {
        IsDarkTheme = isDark;
        var themeName = isDark ? T(ZhijianL.DarkTheme) : T(ZhijianL.LightTheme);
        StatusText = FormatText(ZhijianL.StatusThemeChanged, themeName);
        _applicationActionService.ShowSuccessMessage(StatusText);
    }

    private void SetLanguage(string cultureName)
    {
        if (string.Equals(_selectedCultureName, cultureName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var culture = CultureInfo.GetCultureInfo(cultureName);
        _selectedCultureName = culture.Name;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        I18nManager.Instance.Culture = culture;
        Application.Current?.SetLanguageVariant(
            culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
                ? LanguageVariant.zh_CN
                : LanguageVariant.en_US);

        RefreshLocalizedProperties();
        StatusText = FormatText(ZhijianL.StatusLanguageChanged, GetLanguageDisplayName(culture.Name));
        _applicationActionService.ShowSuccessMessage(StatusText);
    }

    private void InitializeNewUserTour()
    {
        if (!ShouldShowNewUserTour())
        {
            return;
        }

        PrepareNewUserTour();
        _hasOpenedNewUserTour = true;
        IsNewUserTourOpen = true;
    }

    private void PrepareNewUserTour()
    {
        WorkspaceTabIndex = 1;
        SelectedNode ??= Root;
    }

    private static bool ShouldShowNewUserTour()
    {
        if (!IsTourEnabled())
        {
            return false;
        }

        return !File.Exists(GetTourSeenPath());
    }

    private static bool IsTourEnabled()
    {
        return ApplicationSettings.ShowNewUserTour;
    }

    private static string GetTourSeenPath()
    {
        return Path.Combine(AppContext.BaseDirectory, TourSeenName);
    }

    partial void OnIsNewUserTourOpenChanged(bool value)
    {
        if (value || !_hasOpenedNewUserTour)
        {
            return;
        }

        try
        {
            File.WriteAllText(GetTourSeenPath(), DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
        }
        catch
        {
        }
    }

    private static string GetLanguageDisplayName(string cultureName)
    {
        return cultureName switch
        {
            "zh-CN" => T(ZhijianL.SimplifiedChinese),
            "zh-Hant" => T(ZhijianL.TraditionalChinese),
            "en-US" => T(ZhijianL.English),
            "ja-JP" => T(ZhijianL.Japanese),
            _ => cultureName
        };
    }

    private void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(EditorPaneTitle));
        OnPropertyChanged(nameof(ToggleEditorToolTip));
        OnPropertyChanged(nameof(CenterRootToolTip));
        OnPropertyChanged(nameof(SelectedNodeSummary));
        OnPropertyChanged(nameof(HistorySummary));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DocumentTitle));
        OnPropertyChanged(nameof(CurrentDocumentName));
        OnPropertyChanged(nameof(FolderSummary));
        OnPropertyChanged(nameof(IsSimplifiedChinese));
        OnPropertyChanged(nameof(IsTraditionalChinese));
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(IsJapanese));
    }

    private static string T(string key)
    {
        return I18nManager.Instance.GetResource(key) ?? key;
    }

    private static string PrimaryCommandText => OperatingSystem.IsMacOS() ? "⌘" : "Ctrl";

    private static string FormatText(string key, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, T(key), args);
    }

    private void AutoLayout()
    {
        if (Roots.Count == 0)
        {
            return;
        }

        var columnPositions = CalculateColumnPositions(Root);
        var nextTop = RootY;
        LayoutNode(Root, 0, columnPositions, ref nextTop);
    }

    partial void OnIsMarkdownModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsOutlineMode));
        OnPropertyChanged(nameof(EditorPaneTitle));
        OnPropertyChanged(nameof(ToggleEditorToolTip));
        OnPropertyChanged(nameof(CenterRootToolTip));
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        Application.Current?.SetDarkThemeMode(value);
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(ShellBackground));
        OnPropertyChanged(nameof(PanelBackground));
        OnPropertyChanged(nameof(PanelFooterBackground));
        OnPropertyChanged(nameof(PanelBorderBrush));
        OnPropertyChanged(nameof(TitleBarBackground));
        OnPropertyChanged(nameof(PrimaryTextBrush));
        OnPropertyChanged(nameof(SecondaryTextBrush));
    }

    partial void OnSelectedNodeChanged(MindMapNode? value)
    {
        OnPropertyChanged(nameof(SelectedNodeSummary));
        RefreshSelectedNodeCommands();
    }

    partial void OnCurrentFilePathChanged(string? value)
    {
        OnPropertyChanged(nameof(CurrentDocumentName));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DocumentTitle));
        OnPropertyChanged(nameof(HasCurrentFile));
        OpenFileLocationCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsDirtyChanged(bool value)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DocumentTitle));
    }

    partial void OnSelectedFolderFileChanged(MindMapFileItem? value)
    {
        if (value is null
            || _isLoadingDocument
            || string.Equals(value.FilePath, CurrentFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = OpenFolderFileAsync(value);
    }

    partial void OnMarkdownTextChanged(string value)
    {
        if (_isSyncingMarkdownFromTree || _isApplyingMarkdown || !IsMarkdownMode)
        {
            return;
        }

        ApplyMarkdownToTree(refreshMarkdownText: false);
    }

    private async Task RunFileOperationAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            StatusText = exception.Message;
        }
    }

    public async Task<bool> ConfirmCloseAsync()
    {
        return await EnsureCanChangeDocumentAsync();
    }

    private async Task<bool> EnsureCanChangeDocumentAsync()
    {
        if (!IsDirty)
        {
            return true;
        }

        var decision = await _fileService.ConfirmSaveChangesAsync(CurrentDocumentName);
        if (decision == MindMapSaveChangesDecision.Cancel)
        {
            return false;
        }

        if (decision == MindMapSaveChangesDecision.Discard)
        {
            return true;
        }

        await SaveDocumentAsync(forceSaveAs: false);
        return !IsDirty;
    }

    private void SetBlankDocument()
    {
        try
        {
            _isLoadingDocument = true;
            ReplaceTree(CreateBlankRoot(), "空白脑图");
            ResetHistory("空白脑图");
            CurrentFilePath = null;
            _currentFileFormat = MindMapFileFormat.Markdown;
            WorkspaceTabIndex = 1;
            MarkDocumentClean();
        }
        finally
        {
            _isLoadingDocument = false;
        }
    }

    private async Task OpenFolderFileAsync(MindMapFileItem file)
    {
        await RunFileOperationAsync(async () =>
        {
            if (!await EnsureCanChangeDocumentAsync())
            {
                SelectedFolderFile = FolderFiles.FirstOrDefault(item => item.FilePath == CurrentFilePath);
                return;
            }

            LoadFilePath(file.FilePath);
            WorkspaceTabIndex = 1;
        });
    }

    private void LoadOpenResult(MindMapFileOpenResult result)
    {
        var root = result.Format == MindMapFileFormat.XMind
            ? MindMapDocumentCodec.FromXMind(result.BinaryContent ?? [])
            : result.Format == MindMapFileFormat.Opml
                ? MindMapDocumentCodec.FromOpml(result.TextContent ?? string.Empty)
                : MindMapDocumentCodec.FromMarkdown(result.TextContent ?? string.Empty);

        LoadDocument(root, result.FilePath, result.Format, $"打开 {Path.GetFileName(result.FilePath)}");
    }

    private void LoadStartupDocument(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)
            || !File.Exists(filePath)
            || !IsSupportedFile(filePath))
        {
            return;
        }

        try
        {
            LoadFilePath(filePath);
        }
        catch (Exception exception)
        {
            StatusText = $"默认文件加载失败：{exception.Message}";
        }
    }

    private void LoadFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            RemoveRecentFile(filePath);
            StatusText = "文件不存在，已从最近文件中移除";
            return;
        }

        var format = GetFormatFromPath(filePath);
        var root = format switch
        {
            MindMapFileFormat.Markdown => MindMapDocumentCodec.FromMarkdown(File.ReadAllText(filePath)),
            MindMapFileFormat.Opml => MindMapDocumentCodec.FromOpml(File.ReadAllText(filePath)),
            MindMapFileFormat.XMind => MindMapDocumentCodec.FromXMind(File.ReadAllBytes(filePath)),
            _ => CreateBlankRoot()
        };

        LoadDocument(root, filePath, format, $"打开 {Path.GetFileName(filePath)}");
    }

    private void LoadDocument(MindMapNode root, string filePath, MindMapFileFormat format, string historyLabel)
    {
        try
        {
            _isLoadingDocument = true;
            _currentFileFormat = format;
            CurrentFilePath = filePath;
            ReplaceTree(root, historyLabel);
            ResetHistory(historyLabel);
            MarkDocumentClean();
            AddRecentFile(filePath);
            AddFileToFileList(filePath);
            WorkspaceTabIndex = 1;
            StatusText = $"已打开：{Path.GetFileName(filePath)}";
        }
        finally
        {
            _isLoadingDocument = false;
        }
    }

    private async Task SaveDocumentAsync(bool forceSaveAs)
    {
        ApplyMarkdownEditsIfNeeded();

        var targetPath = CurrentFilePath;
        var targetFormat = _currentFileFormat;
        if (forceSaveAs || string.IsNullOrWhiteSpace(targetPath))
        {
            var saveTarget = await _fileService.PickSaveTargetAsync(targetFormat, GetSuggestedFileName(targetFormat));
            if (saveTarget is null)
            {
                return;
            }

            targetPath = saveTarget.FilePath;
            targetFormat = saveTarget.Format;
        }

        WriteDocument(targetPath, targetFormat);
        _currentFileFormat = targetFormat;
        CurrentFilePath = targetPath;
        AddRecentFile(targetPath);
        AddFileToFileList(targetPath);
        MarkDocumentClean();
        StatusText = $"已保存：{Path.GetFileName(targetPath)}";
    }

    private void WriteDocument(string filePath, MindMapFileFormat format)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        switch (format)
        {
            case MindMapFileFormat.Markdown:
                File.WriteAllText(filePath, MindMapDocumentCodec.ToMarkdown(Root));
                break;
            case MindMapFileFormat.Opml:
                File.WriteAllText(filePath, MindMapDocumentCodec.ToOpml(Root));
                break;
            case MindMapFileFormat.XMind:
                File.WriteAllBytes(filePath, MindMapDocumentCodec.ToXMind(Root));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }
    }

    private string GetSuggestedFileName(MindMapFileFormat format)
    {
        if (!string.IsNullOrWhiteSpace(CurrentFilePath))
        {
            return Path.GetFileName(CurrentFilePath);
        }

        var title = string.IsNullOrWhiteSpace(Root.Title) ? "未命名脑图" : Root.Title.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            title = title.Replace(invalid, '-');
        }

        return $"{title}.{GetDefaultExtension(format)}";
    }

    private void LoadFolderFiles(string folderPath)
    {
        FolderFiles.Clear();
        foreach (var filePath in Directory.EnumerateFiles(folderPath)
                     .Where(IsSupportedFile)
                     .OrderByDescending(File.GetLastWriteTime))
        {
            FolderFiles.Add(CreateFileItem(filePath));
        }

        SelectFolderFile(CurrentFilePath);
        OnPropertyChanged(nameof(HasFolderFiles));
        OnPropertyChanged(nameof(FolderSummary));
    }

    private void AddFileToFileList(string filePath)
    {
        var existing = FolderFiles.FirstOrDefault(item => item.FilePath == filePath);
        if (existing is not null)
        {
            var index = FolderFiles.IndexOf(existing);
            FolderFiles[index] = CreateFileItem(filePath);
            SelectFolderFile(filePath);
            return;
        }

        if (IsSupportedFile(filePath))
        {
            FolderFiles.Insert(0, CreateFileItem(filePath));
            SelectFolderFile(filePath);
            OnPropertyChanged(nameof(HasFolderFiles));
            OnPropertyChanged(nameof(FolderSummary));
        }
    }

    private void SelectFolderFile(string? filePath)
    {
        SelectedFolderFile = filePath is null
            ? null
            : FolderFiles.FirstOrDefault(item => string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
    }

    private MindMapFileItem CreateFileItem(string filePath)
    {
        return new MindMapFileItem(
            filePath,
            Path.GetFileNameWithoutExtension(filePath),
            Path.GetExtension(filePath),
            CreateFilePreview(filePath),
            File.GetLastWriteTime(filePath));
    }

    private static string CreateFilePreview(string filePath)
    {
        try
        {
            if (GetFormatFromPath(filePath) == MindMapFileFormat.XMind)
            {
                var root = MindMapDocumentCodec.FromXMind(File.ReadAllBytes(filePath));
                return string.IsNullOrWhiteSpace(root.Title) ? "XMind 脑图" : root.Title.Trim();
            }

            return string.Join(
                Environment.NewLine,
                File.ReadLines(filePath)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Take(2));
        }
        catch
        {
            return "无法预览";
        }
    }

    private void LoadRecentFiles()
    {
        RecentFiles.Clear();
        var filePath = GetRecentFilesPath();
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            var paths = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(filePath)) ?? [];
            foreach (var path in paths.Where(File.Exists).Where(IsSupportedFile).Take(MaxRecentFiles))
            {
                RecentFiles.Add(new RecentFileItem(path));
            }
        }
        catch
        {
            RecentFiles.Clear();
        }
    }

    private void AddRecentFile(string filePath)
    {
        RemoveRecentFile(filePath, save: false);
        RecentFiles.Insert(0, new RecentFileItem(filePath));
        while (RecentFiles.Count > MaxRecentFiles)
        {
            RecentFiles.RemoveAt(RecentFiles.Count - 1);
        }

        SaveRecentFiles();
    }

    private void RemoveRecentFile(string filePath, bool save = true)
    {
        var existing = RecentFiles.FirstOrDefault(item =>
            string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            RecentFiles.Remove(existing);
        }

        if (save)
        {
            SaveRecentFiles();
        }
    }

    private void SaveRecentFiles()
    {
        var filePath = GetRecentFilesPath();
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, JsonSerializer.Serialize(RecentFiles.Select(item => item.FilePath).ToList()));
    }

    private static string GetRecentFilesPath()
    {
        return Path.Combine(AppContext.BaseDirectory, RecentFilesName);
    }

    private static bool IsSupportedFile(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() is ".md" or ".markdown" or ".opml" or ".xml" or ".xmind";
    }

    private static MindMapFileFormat GetFormatFromPath(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".md" or ".markdown" => MindMapFileFormat.Markdown,
            ".opml" or ".xml" => MindMapFileFormat.Opml,
            ".xmind" => MindMapFileFormat.XMind,
            _ => MindMapFileFormat.Markdown
        };
    }

    private static string GetDefaultExtension(MindMapFileFormat format)
    {
        return format switch
        {
            MindMapFileFormat.Markdown => "md",
            MindMapFileFormat.Opml => "opml",
            MindMapFileFormat.XMind => "xmind",
            _ => "md"
        };
    }

    private void MarkDocumentDirty()
    {
        if (_isLoadingDocument || _isRestoringHistory)
        {
            return;
        }

        IsDirty = true;
    }

    private void MarkDocumentClean()
    {
        IsDirty = false;
    }

    private void ResetHistory(string label)
    {
        _history.Clear();
        _historyIndex = -1;
        RecordHistoryStep(label);
        RefreshHistoryState();
    }

    private void ApplyMarkdownEditsIfNeeded()
    {
        if (IsMarkdownMode)
        {
            ApplyMarkdownToTree(refreshMarkdownText: false);
        }
    }

    private void ReplaceTree(MindMapNode root, string historyLabel)
    {
        try
        {
            _isApplyingMarkdown = true;
            Roots.Clear();
            Roots.Add(root);
            AssignMissingColors(root);
            SelectedNode = root;
            AutoLayout();
            RefreshTreeSummary();
        }
        finally
        {
            _isApplyingMarkdown = false;
            SyncMarkdownFromTree();
        }

        RecordHistoryStep(historyLabel);
    }

    private static void AppendFlattened(MindMapNode node, List<MindMapNode> nodes)
    {
        nodes.Add(node);
        foreach (var child in node.Children)
        {
            AppendFlattened(child, nodes);
        }
    }

    private static int GetLevel(MindMapNode current, MindMapNode target, int level)
    {
        if (ReferenceEquals(current, target))
        {
            return level;
        }

        foreach (var child in current.Children)
        {
            var childLevel = GetLevel(child, target, level + 1);
            if (childLevel > 0)
            {
                return childLevel;
            }
        }

        return 0;
    }

    private void WatchTree()
    {
        Roots.CollectionChanged += HandleChildrenChanged;
        foreach (var root in Roots)
        {
            WatchNode(root);
        }
    }

    private void WatchNode(MindMapNode node)
    {
        if (!_observedNodes.Add(node))
        {
            return;
        }

        node.PropertyChanged += HandleNodePropertyChanged;
        node.Children.CollectionChanged += HandleChildrenChanged;

        foreach (var child in node.Children)
        {
            WatchNode(child);
        }
    }

    private void UnwatchNode(MindMapNode node)
    {
        if (!_observedNodes.Remove(node))
        {
            return;
        }

        node.PropertyChanged -= HandleNodePropertyChanged;
        node.Children.CollectionChanged -= HandleChildrenChanged;

        foreach (var child in node.Children)
        {
            UnwatchNode(child);
        }
    }

    private void HandleNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MindMapNode.Title) or nameof(MindMapNode.Note))
        {
            AutoLayout();
            RefreshTreeSummary();
        }

        if (e.PropertyName is nameof(MindMapNode.Title) or nameof(MindMapNode.Note))
        {
            SyncMarkdownFromTree();
            RecordHistoryStep("编辑主题");
        }
    }

    private void HandleChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (MindMapNode node in e.OldItems)
            {
                UnwatchNode(node);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (MindMapNode node in e.NewItems)
            {
                AssignMissingColors(node);
                WatchNode(node);
            }
        }

        SyncMarkdownFromTree();
        RefreshTreeSummary();
    }

    private MindMapNode? FindParent(MindMapNode node)
    {
        return Roots.Count == 0 ? null : FindParent(Root, node);
    }

    private static MindMapNode? FindParent(MindMapNode candidateParent, MindMapNode node)
    {
        if (candidateParent.Children.Contains(node))
        {
            return candidateParent;
        }

        foreach (var child in candidateParent.Children)
        {
            var parent = FindParent(child, node);
            if (parent is not null)
            {
                return parent;
            }
        }

        return null;
    }

    private static bool IsDescendant(MindMapNode candidateAncestor, MindMapNode candidateDescendant)
    {
        foreach (var child in candidateAncestor.Children)
        {
            if (ReferenceEquals(child, candidateDescendant) || IsDescendant(child, candidateDescendant))
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshTreeSummary()
    {
        OnPropertyChanged(nameof(NodeCount));
        OnPropertyChanged(nameof(SelectedNodeSummary));
        RefreshSelectedNodeCommands();
    }

    private static double[] CalculateColumnPositions(MindMapNode root)
    {
        // 每列先按真实节点宽度估算，避免第 4 级以后长标题与连线挤在一起。
        var columnWidths = new List<double>();
        CollectColumnWidths(root, 0, columnWidths);

        var columnPositions = new double[columnWidths.Count];
        var x = RootX;
        for (var i = 0; i < columnPositions.Length; i++)
        {
            columnPositions[i] = x;
            x += columnWidths[i] + HorizontalGap;
        }

        return columnPositions;
    }

    private static void CollectColumnWidths(MindMapNode node, int depth, List<double> columnWidths)
    {
        var level = depth + 1;
        var size = MindMapLayoutMetrics.EstimateNodeSize(node, level);
        while (columnWidths.Count <= depth)
        {
            columnWidths.Add(0);
        }

        columnWidths[depth] = Math.Max(columnWidths[depth], size.Width);
        foreach (var child in node.Children)
        {
            CollectColumnWidths(child, depth + 1, columnWidths);
        }
    }

    private static LayoutResult LayoutNode(MindMapNode node, int depth, IReadOnlyList<double> columnPositions, ref double nextTop)
    {
        var level = depth + 1;
        var size = MindMapLayoutMetrics.EstimateNodeSize(node, level);
        node.X = depth < columnPositions.Count
            ? columnPositions[depth]
            : columnPositions[^1] + (depth - columnPositions.Count + 1) * (MindMapLayoutMetrics.LeafMaxWidth + HorizontalGap);

        if (node.Children.Count == 0)
        {
            node.Y = nextTop;
            nextTop = node.Y + size.Height + VerticalSpacing;
            return new LayoutResult(node.Y + size.Height / 2, node.Y, node.Y + size.Height);
        }

        var firstCenter = 0d;
        var lastCenter = 0d;
        var subtreeTop = double.MaxValue;
        var subtreeBottom = double.MinValue;
        for (var i = 0; i < node.Children.Count; i++)
        {
            var childLayout = LayoutNode(node.Children[i], depth + 1, columnPositions, ref nextTop);
            if (i == 0)
            {
                firstCenter = childLayout.CenterY;
            }

            lastCenter = childLayout.CenterY;
            subtreeTop = Math.Min(subtreeTop, childLayout.Top);
            subtreeBottom = Math.Max(subtreeBottom, childLayout.Bottom);
        }

        var nodeCenter = (firstCenter + lastCenter) / 2;
        node.Y = Math.Max(MinNodeY, nodeCenter - size.Height / 2);
        subtreeTop = Math.Min(subtreeTop, node.Y);
        subtreeBottom = Math.Max(subtreeBottom, node.Y + size.Height);
        nextTop = Math.Max(nextTop, subtreeBottom + VerticalSpacing);
        return new LayoutResult(node.Y + size.Height / 2, subtreeTop, subtreeBottom);
    }

    private MindMapNode CreateNode(string title, params MindMapNode[] children)
    {
        var node = new MindMapNode(title, children)
        {
            AccentColor = NextColor()
        };

        AssignMissingColors(node);
        return node;
    }

    private MindMapNode CreateBlankRoot()
    {
        return CreateNode(string.Empty);
    }

    private void AssignMissingColors(MindMapNode node)
    {
        if (string.IsNullOrWhiteSpace(node.AccentColor))
        {
            node.AccentColor = NextColor();
        }

        foreach (var child in node.Children)
        {
            AssignMissingColors(child);
        }
    }

    private string NextColor()
    {
        return Palette[_nextPaletteIndex++ % Palette.Length];
    }

    private void SyncMarkdownFromTree()
    {
        if (_isSyncingMarkdownFromTree || _isApplyingMarkdown || Roots.Count == 0)
        {
            return;
        }

        try
        {
            _isSyncingMarkdownFromTree = true;
            MarkdownText = MindMapDocumentCodec.ToMarkdown(Root);
        }
        finally
        {
            _isSyncingMarkdownFromTree = false;
        }
    }

    private void ApplyMarkdownToTree(bool refreshMarkdownText = true)
    {
        try
        {
            _isApplyingMarkdown = true;
            var root = MindMapDocumentCodec.FromMarkdown(MarkdownText);

            Roots.Clear();
            Roots.Add(root);
            AssignMissingColors(root);
            SelectedNode = root;
            AutoLayout();
            RefreshTreeSummary();
        }
        finally
        {
            _isApplyingMarkdown = false;
            if (refreshMarkdownText)
            {
                SyncMarkdownFromTree();
            }
        }

        RecordHistoryStep("编辑 Markdown");
    }

    private void RecordHistoryStep(string label)
    {
        if (_isRestoringHistory || Roots.Count == 0)
        {
            return;
        }

        var snapshot = MindMapDocumentCodec.ToMarkdown(Root);
        if (_historyIndex >= 0 && _history[_historyIndex].Snapshot == snapshot)
        {
            return;
        }

        if (_historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        }

        _history.Add(new HistoryEntry(label, snapshot));
        if (_history.Count > MaxHistorySteps)
        {
            _history.RemoveAt(0);
        }

        _historyIndex = _history.Count - 1;
        RefreshHistoryState();
        MarkDocumentDirty();
    }

    private void RestoreHistoryEntry(HistoryEntry entry)
    {
        try
        {
            _isRestoringHistory = true;
            _isApplyingMarkdown = true;
            var root = MindMapDocumentCodec.FromMarkdown(entry.Snapshot);

            Roots.Clear();
            Roots.Add(root);
            AssignMissingColors(root);
            SelectedNode = root;
            AutoLayout();
            RefreshTreeSummary();
        }
        finally
        {
            _isApplyingMarkdown = false;
            _isRestoringHistory = false;
            SyncMarkdownFromTree();
            RefreshHistoryState();
        }
    }

    private void RefreshHistoryState()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(HistorySummary));
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void RefreshSelectedNodeCommands()
    {
        OnPropertyChanged(nameof(CanPromoteSelectedNode));
        OnPropertyChanged(nameof(CanDemoteSelectedNode));
        OnPropertyChanged(nameof(CanMoveSelectedNodeUp));
        OnPropertyChanged(nameof(CanMoveSelectedNodeDown));
        OnPropertyChanged(nameof(CanDeleteSelectedNode));
        PromoteSelectedCommand.NotifyCanExecuteChanged();
        DemoteSelectedCommand.NotifyCanExecuteChanged();
        MoveSelectedUpCommand.NotifyCanExecuteChanged();
        MoveSelectedDownCommand.NotifyCanExecuteChanged();
        DeleteSelectedCommand.NotifyCanExecuteChanged();
    }

    private readonly record struct LayoutResult(double CenterY, double Top, double Bottom);

    private sealed record HistoryEntry(string Label, string Snapshot);
}

public sealed record MindMapFileItem(
    string FilePath,
    string Name,
    string Extension,
    string Preview,
    DateTime ModifiedAt)
{
    public string DisplayName => Name;

    public string ExtensionText => Extension.TrimStart('.');
}

public sealed record RecentFileItem(string FilePath)
{
    public string DisplayName => Path.GetFileName(FilePath);

    public string FolderName => Path.GetDirectoryName(FilePath) ?? string.Empty;
}

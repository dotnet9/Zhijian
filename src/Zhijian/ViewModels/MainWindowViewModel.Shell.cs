using AtomUI.Controls;
using AtomUI.Theme.Language;
using Avalonia;
using CodeWF.MindView;
using Lang.Avalonia;
using System.Globalization;
using System.Text;
using Zhijian.Services;

namespace Zhijian.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IMindMapEditorController
{
    private void SetTheme(bool isDark)
    {
        IsDarkTheme = isDark;
        var themeName = isDark ? T(ZhijianL.DarkTheme) : T(ZhijianL.LightTheme);
        StatusText = FormatText(ZhijianL.StatusThemeChanged, themeName);
        _applicationActionService.ShowSuccessMessage(StatusText);
    }

    public void NewWindow()
    {
        _applicationActionService.OpenNewWindow();
        StatusText = T(ZhijianL.StatusNewWindow);
    }

    public void Close()
    {
        _applicationActionService.CloseMainWindow();
    }

    public void SetDarkTheme()
    {
        SetTheme(isDark: true);
    }

    public void SetLightTheme()
    {
        SetTheme(isDark: false);
    }

    public void SelectSimplifiedChinese()
    {
        SetLanguage("zh-CN");
    }

    public void SelectTraditionalChinese()
    {
        SetLanguage("zh-Hant");
    }

    public void SelectEnglish()
    {
        SetLanguage("en-US");
    }

    public void SelectJapanese()
    {
        SetLanguage("ja-JP");
    }

    public void OpenFeedback()
    {
        _applicationActionService.OpenFeedback();
        StatusText = T(ZhijianL.Feedback);
    }

    public void OpenFeatureRequest()
    {
        _applicationActionService.OpenFeatureRequest();
        StatusText = T(ZhijianL.SubmitFeature);
    }

    public void OpenPullRequests()
    {
        _applicationActionService.OpenPullRequests();
        StatusText = T(ZhijianL.SubmitPr);
    }

    public void OpenWebsite()
    {
        _applicationActionService.OpenWebsite();
        StatusText = T(ZhijianL.StatusOpenWebsite);
    }

    public void ShowChangelog()
    {
        _applicationActionService.ShowChangelog();
        StatusText = T(ZhijianL.StatusShowChangelog);
    }

    public void ShowAbout()
    {
        _applicationActionService.ShowAbout();
        StatusText = T(ZhijianL.StatusShowAbout);
    }

    public void ShowThanks()
    {
        _applicationActionService.ShowThanks();
        StatusText = T(ZhijianL.StatusShowThanks);
    }

    public void OpenRepository()
    {
        _applicationActionService.OpenRepository();
        StatusText = T(ZhijianL.StatusOpenRepository);
    }

    public void ShowNewUserTour()
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

    public void ToggleWorkspacePane()
    {
        IsWorkspacePaneVisible = !IsWorkspacePaneVisible;
        StatusText = T(IsWorkspacePaneVisible ? ZhijianL.StatusWorkspacePaneShown : ZhijianL.StatusWorkspacePaneHidden);
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

    private async Task InitializeNewUserTourAsync()
    {
        if (!await ShouldShowNewUserTourAsync())
        {
            return;
        }

        PrepareNewUserTour();
        _hasOpenedNewUserTour = true;
        IsNewUserTourOpen = true;
    }

    private void PrepareNewUserTour()
    {
        IsWorkspacePaneVisible = true;
        WorkspaceTabIndex = 1;
        SelectedNode ??= Root;
    }

    private static async Task<bool> ShouldShowNewUserTourAsync()
    {
        if (!IsTourEnabled())
        {
            return false;
        }

        return !await FileExistsAsync(GetTourSeenPath());
    }

    private static bool IsTourEnabled()
    {
        return ApplicationSettings.ShowNewUserTour;
    }

    private static string GetTourSeenPath()
    {
        return ApplicationSettings.GetUserDataPath(TourSeenName);
    }

    private void OnIsNewUserTourOpenChanged(bool value)
    {
        if (value || !_hasOpenedNewUserTour)
        {
            return;
        }

        _ = PersistTourSeenAsync();
    }

    private static async Task PersistTourSeenAsync()
    {
        try
        {
            await File.WriteAllTextAsync(
                GetTourSeenPath(),
                DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
                Encoding.UTF8);
        }
        catch (Exception exception)
        {
            ApplicationLogger.Warning($"Persisting the new user tour marker failed. file=\"{GetTourSeenPath()}\"", exception);
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
        OnPropertyChanged(nameof(ToggleWorkspacePaneToolTip));
        OnPropertyChanged(nameof(SelectedNodeSummary));
        OnPropertyChanged(nameof(HistorySummary));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DocumentTitle));
        OnPropertyChanged(nameof(CurrentDocumentName));
        OnPropertyChanged(nameof(FolderSummary));
        OnPropertyChanged(nameof(IsFolderEmpty));
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

    private void OnIsMarkdownModeChanged(bool value)
    {
        OnPropertyChanged(nameof(IsOutlineMode));
        OnPropertyChanged(nameof(EditorPaneTitle));
        OnPropertyChanged(nameof(ToggleEditorToolTip));
        OnPropertyChanged(nameof(CenterRootToolTip));
    }

    private void OnIsWorkspacePaneVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsWorkspacePaneHidden));
        OnPropertyChanged(nameof(ToggleWorkspacePaneToolTip));
    }

    private void OnIsDarkThemeChanged(bool value)
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

    private void OnSelectedNodeChanged(MindMapNode? value)
    {
        OnPropertyChanged(nameof(SelectedNodeSummary));
        RefreshSelectedNodeCommands();
    }

    private void OnCurrentFilePathChanged(string? value)
    {
        OnPropertyChanged(nameof(CurrentDocumentName));
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DocumentTitle));
        OnPropertyChanged(nameof(HasCurrentFile));
        OnPropertyChanged(nameof(IsBlankDocument));
    }

    private void OnIsDirtyChanged(bool value)
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DocumentTitle));
        OnPropertyChanged(nameof(IsBlankDocument));
    }

    private void OnSelectedFolderFileChanged(MindMapFileItem? value)
    {
        if (value is null
            || _isLoadingDocument
            || string.Equals(value.FilePath, CurrentFilePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = OpenFolderFileAsync(value);
    }

    private void OnMarkdownTextChanged(string value)
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
            ApplicationLogger.Error("File operation failed.", exception);
            StatusText = exception.Message;
        }
    }

}

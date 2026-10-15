using System.Diagnostics;
using System.Security;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.LogicalTree;
using CodeWF.Avalonia.Controls.Themes;
using CodeWF.Toolkit.Lang;
using CodeWF.Avalonia.MindView;
using CodeWF.Avalonia.MindView.Controls;
using SkiaSharp;
using Zhijian.ViewModels;
using CodeWF.Toolkit.Core.UpdateChecking;
using Avalonia.Media.Imaging;

var output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/ui-verification");
var assertionCount = 0;
Directory.CreateDirectory(output);
var testData = Path.Combine(output, "userdata"); Directory.CreateDirectory(testData);
var processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
File.WriteAllText(Path.Combine(AppContext.BaseDirectory, processName + ".dll.config"),
    "<configuration><appSettings><add key=\"ShowNewUserTour\" value=\"false\"/><add key=\"UserDataDirectory\" value=\""
    + SecurityElement.Escape(testData) + "\"/></appSettings></configuration>");
AppBuilder.Configure<Zhijian.App>().UseSkia().WithInterFont()
    .With(new FontManagerOptions
    {
        FontFallbacks =
        [
            new FontFallback { FontFamily = new FontFamily("Microsoft YaHei UI") },
            new FontFallback { FontFamily = new FontFamily("PingFang SC") },
            new FontFallback { FontFamily = new FontFamily("Noto Sans CJK SC") }
        ]
    })
    .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).SetupWithoutStarting();
var vm = new MainWindowViewModel();
var window = new Zhijian.Views.MainWindow { DataContext = vm };
window.Show(); Pump();
VerifyTitleBar();
VerifyControlCulture(window, ["zh-CN", "zh-Hant", "en-US", "ja-JP"], culture =>
    window.GetVisualDescendants().OfType<Zhijian.Views.TitleBarAppearance>().Single().FindControl<ComboBox>("TitleBarLanguageSelector")!.SelectedItem =
        vm.TitleBarLanguages.Single(item => item.CultureName == culture));
var editor = window.FindControl<MindMapEditor>("MindMap")!;
Check(editor.Bounds.Width > 500, "MindView has a real viewport");
vm.Roots[0].Title = "迁移验证";
var child = vm.AddChild(vm.Roots[0], "结构编辑"); Pump();
Check(vm.Roots[0].Children.Contains(child), "Structural editing uses the NuGet control");
vm.Undo(); Pump(); Check(vm.Roots[0].Children.Count == 0, "Undo restores the tree");
vm.Redo(); Pump(); Check(vm.Roots[0].Children.Count == 1, "Redo restores the branch");
var exported = MindMapDocumentCodec.ToMarkdown(vm.Roots[0]);
Check(MindMapDocumentCodec.FromMarkdown(exported).Children[0].Title == "结构编辑", "Markdown import and export preserve the tree");
vm.IsMarkdownMode = true;
vm.MarkdownText = "# Studio\n\n## Controls\n### Mind map\n### Outline\n## Documents\n### Markdown\n### OPML\n## Appearance\n### Eight palettes";
Pump();
Check(vm.Roots[0].Children.Count == 3, "Markdown edits update the real outline and mind map");
vm.IsMarkdownMode = false;
editor.PlaceRootNearLeftCenter();
Pump();
foreach (var palette in Enum.GetValues<StudioPalette>()) foreach (var dark in new[] { false, true })
{
    vm.SelectPaletteByKey(palette.ToString());
    if (dark) vm.SetDarkTheme(); else vm.SetLightTheme();
    Pump();
    Check(editor.IsDarkTheme == dark, palette + " mode follows the host");
    Check(editor.AccentColor == vm.AccentColor, palette + " uses the shared accent token");
    using var frame = window.CaptureRenderedFrame()!;
    Check(frame.PixelSize.Width > 900, "Native frame is rendered");
    using var stream = File.Create(Path.Combine(output, $"zhijian-{palette}-{(dark ? "dark" : "light")}.png"));
    frame.Save(stream, new PngBitmapEncoderOptions());
}
foreach (var culture in new[] { "zh-CN", "zh-Hant", "en-US", "ja-JP" })
{
    I18nManager.Instance.Culture = new System.Globalization.CultureInfo(culture); Pump();
    Check(I18nManager.Instance.Culture.Name == culture && !editor.CenterTopicPlaceholder.StartsWith("CodeWF."), "MindView keeps the requested embedded language: " + culture);
    ResourceDictionary controlCulture = culture switch
    {
        "en-US" => new Semi.Avalonia.Locale.en_us(),
        "ja-JP" => new Semi.Avalonia.Locale.ja_jp(),
        "zh-Hant" => new Semi.Avalonia.Locale.zh_tw(),
        _ => new Semi.Avalonia.Locale.zh_cn()
    };
    var localizedStrings = controlCulture.Where(item => item.Value is string).ToArray();
    Check(localizedStrings.Length > 0 && localizedStrings.All(item =>
        Application.Current!.TryGetResource(item.Key, Application.Current.ActualThemeVariant, out var value) && Equals(value, item.Value)),
        "Semi controls follow the application language: " + culture);
}
I18nManager.Instance.Culture = new System.Globalization.CultureInfo("zh-CN");
vm.SetSystemTheme(); Check(Zhijian.App.ThemeController!.Mode == StudioThemeMode.System && vm.IsSystemTheme && !vm.IsLightTheme && !vm.IsExplicitDarkTheme, "System mode stays selected independently of its effective color");
vm.ShowNewUserTour(); Pump(); Check(vm.IsNewUserTourOpen, "Guide can be replayed with CodeWF controls");
vm.IsNewUserTourOpen = false;
{
    var about = new Zhijian.Views.AboutWindow();
    about.Show(window); VerifyCaptionCapabilities(about);
    var aboutModel = (AboutWindowViewModel)about.DataContext!;
    Check(aboutModel.Version == UpdateVersion.Display(typeof(Zhijian.App).Assembly), "About version follows the application assembly");
    Check(DateTime.TryParse(aboutModel.CompileTime, out _), "About compile time is available after trimming");
    Check(aboutModel.Platform.Contains(".NET") && aboutModel.Platform.Contains("Avalonia"), "About platform comes from the runtime");
    foreach (var dark in new[] { false, true })
    {
        if (dark) vm.SetDarkTheme(); else vm.SetLightTheme();
        foreach (var culture in new[] { "zh-CN", "en-US", "zh-Hant", "ja-JP" })
        {
            SetBackgroundCulture(culture);
            Check(!aboutModel.Update.ActionText.StartsWith("Zhijian.") && !aboutModel.Author.StartsWith("Zhijian."), "About resources change dynamically: " + culture);
            using var frame = about.CaptureRenderedFrame()!;
            frame.Save(Path.Combine(output, $"about-{culture}-{(dark ? "dark" : "light")}.png"), new PngBitmapEncoderOptions());
        }
    }
    about.Close();
}
VerifyAuxiliaryWindows();
VerifyUpdates();
window.Close();
Console.WriteLine($"Zhijian UI verification passed: {assertionCount} assertions; NativeAOT={!System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported}; {output}");

void Check(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); assertionCount++; Console.WriteLine("PASS " + message); }
void VerifyCaptionCapabilities(Window target)
{
    Pump();
    var minimize = target.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_MinimizeButton");
    var maximize = target.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_MaximizeRestoreButton");
    var canMinimize = target.CanMinimize;
    var canMaximize = target.CanMaximize;
    var originalState = target.WindowState;
    target.WindowState = WindowState.Normal;
    target.CanMinimize = false; Pump();
    Check(!minimize.IsEnabled, "Actual caption disables minimization when the host forbids it");
    minimize.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(target.WindowState == WindowState.Normal, "Disabled caption also guards programmatic minimization");
    target.CanMinimize = true; Pump();
    Check(minimize.IsEnabled, "Runtime minimization capability updates the same caption button");
    minimize.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(target.WindowState == WindowState.Minimized, "Enabled caption minimizes the actual host window");
    target.WindowState = WindowState.Normal;
    target.CanMaximize = false; Pump();
    Check(!maximize.IsEnabled, "Actual caption disables forbidden maximization");
    maximize.RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
    Check(target.WindowState == WindowState.Normal, "Disabled caption guards programmatic maximization");
    target.CanMinimize = canMinimize; target.CanMaximize = canMaximize; target.WindowState = originalState; Pump();
    Check(minimize.IsEnabled == canMinimize && maximize.IsEnabled == (target.CanResize && canMaximize), "Caption capabilities restore without recreating the host window");
}
void VerifyControlCulture(Window owner, string[] cultures, Action<string> selectTitleBar)
{
    var originalCulture = I18nManager.Instance.Culture;
    var editor = new TextBox { Text = "Locale 中文 😀" };
    var host = new Window { Content = editor, Width = 340, Height = 110 };
    host.Show(owner); Pump();
    var menu = editor.ContextFlyout as MenuFlyout ?? throw new InvalidOperationException("Semi editor menu is missing.");
    var semiTheme = Application.Current!.Styles.OfType<Semi.Avalonia.SemiTheme>().Single();
    var ursaTheme = Application.Current.Styles.OfType<Ursa.Themes.Semi.UrsaSemiTheme>().Single();
    foreach (var source in new[] { "title selector", "direct manager", "background manager" })
    foreach (var culture in cultures)
    {
        if (source == "background manager") SetBackgroundCulture(culture);
        else if (source == "direct manager") I18nManager.Instance.Culture = System.Globalization.CultureInfo.GetCultureInfo(culture);
        else selectTitleBar(culture);
        Pump();
        ResourceDictionary semiText = culture switch
        {
            "en-US" => new Semi.Avalonia.Locale.en_us(),
            "zh-Hant" => new Semi.Avalonia.Locale.zh_tw(),
            "ja-JP" => new Semi.Avalonia.Locale.ja_jp(),
            _ => new Semi.Avalonia.Locale.zh_cn()
        };
        ResourceDictionary ursaText = culture switch
        {
            "en-US" => new Ursa.Themes.Semi.Locale.en_us(),
            "ja-JP" => new Ursa.Themes.Semi.Locale.en_us(),
            _ => new Ursa.Themes.Semi.Locale.zh_cn()
        };
        menu.ShowAt(editor); Pump();
        Check(((MenuItem)menu.Items[1]!).Header?.ToString() == semiText["STRING_MENU_COPY"]?.ToString() &&
              ((MenuItem)menu.Items[2]!).Header?.ToString() == semiText["STRING_MENU_PASTE"]?.ToString() &&
              editor.Text == "Locale 中文 😀" && semiTheme.Locale?.Name == (culture == "zh-Hant" ? "zh-TW" : culture),
            "Existing Semi editor copy/paste follows " + source + ": " + culture);
        menu.Hide();
        Check(ursaTheme.Resources["STRING_MENU_DIALOG_CANCEL"]?.ToString() == ursaText["STRING_MENU_DIALOG_CANCEL"]?.ToString() &&
              ursaTheme.Locale?.Name == (culture == "zh-Hant" ? "zh-CN" : culture == "ja-JP" ? "en-US" : culture),
            "Actual Ursa locale follows " + source + ": " + culture);
    }
    Application.Current.Resources["STRING_MENU_COPY"] = "宿主复制";
    Application.Current.Resources["STRING_MENU_DIALOG_CANCEL"] = "宿主取消";
    I18nManager.Instance.Culture = System.Globalization.CultureInfo.GetCultureInfo("zh-CN"); Pump();
    menu.ShowAt(editor); Pump();
    Check(((MenuItem)menu.Items[1]!).Header?.ToString() == "宿主复制" &&
          Application.Current.Resources["STRING_MENU_DIALOG_CANCEL"]?.ToString() == "宿主取消",
        "Third-party locale changes preserve host resource overrides");
    menu.Hide();
    Application.Current.Resources.Remove("STRING_MENU_COPY");
    Application.Current.Resources.Remove("STRING_MENU_DIALOG_CANCEL");
    I18nManager.Instance.Culture = originalCulture; Pump();
    host.Close();
}
void SetBackgroundCulture(string culture)
{
    Task.Run(() => I18nManager.Instance.Culture = System.Globalization.CultureInfo.GetCultureInfo(culture)).GetAwaiter().GetResult();
    Pump();
    Check(I18nManager.Instance.Culture.Name == culture, "Background language change completes with live views: " + culture);
}
void Pump()
{
    var until = Stopwatch.StartNew();
    do { Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Thread.Sleep(10); } while (until.ElapsedMilliseconds < 250);
}

void VerifyTitleBar()
{
    var controls = window.GetVisualDescendants().OfType<Zhijian.Views.TitleBarAppearance>().Single();
    var toggle = controls.FindControl<Ursa.Controls.ThemeToggleButton>("TitleBarThemeToggle")!;
    var palettes = controls.FindControl<ListBox>("TitleBarPalettes")!;
    var languages = controls.FindControl<ComboBox>("TitleBarLanguageSelector")!;
    var scope = controls.FindControl<ThemeVariantScope>("TitleBarThemeScope")!;
    var button = toggle.GetVisualDescendants().OfType<Button>().Single(item => item.Name == "PART_ThemeButton");
    Check(toggle.IsThreeState && palettes.Items.Count == 8, "Title bar uses one Ursa three-state button and eight flat palette dots");
    foreach (var palette in vm.TitleBarPalettes)
    {
        palettes.SelectedItem = palette; vm.ThemeMode = StudioThemeMode.System;
        foreach (var expected in new[] { StudioThemeMode.Light, StudioThemeMode.Dark, StudioThemeMode.System })
        {
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
            var saved = File.ReadAllLines(Zhijian.Services.ApplicationSettings.GetUserDataPath("appearance.txt"));
            Check(vm.ThemeMode == expected && saved[0] == palette.Key && saved[1] == expected.ToString(), "Actual Ursa clicks preserve and persist palette and mode: " + palette.Key + " " + expected);
            var dark = expected == StudioThemeMode.Dark || expected == StudioThemeMode.System && Application.Current!.PlatformSettings?.GetColorValues().ThemeVariant == Avalonia.Platform.PlatformThemeVariant.Dark;
            Check(Application.Current!.RequestedThemeVariant == StudioTheme.GetVariant(Enum.Parse<StudioPalette>(palette.Key), dark), "Ursa independent scope preserves custom Studio variant: " + palette.Key + " " + expected);
        }
    }
    foreach (var width in new[] { 980, 1280 })
    {
        window.Width = width; Pump(); window.UpdateLayout();
        languages.IsDropDownOpen = true; Pump();
        Check(languages.IsDropDownOpen && languages.Items.Count == vm.TitleBarLanguages.Count && languages.Items.Count == 4, "Language icon opens all four real languages at " + width);
        foreach (var language in vm.TitleBarLanguages)
        {
            languages.SelectedItem = language; Pump(); window.UpdateLayout();
            Check(I18nManager.Instance.Culture.Name == language.CultureName && ReferenceEquals(vm.SelectedTitleBarLanguage, language), "Language choice synchronizes global culture and selection: " + language.CultureName);
            Check(new[] { "SYSTEM", "LIGHT", "DARK" }.All(key => ((TextBlock)scope.Resources["STRING_THEME_TOGGLE_" + key]!).Text == I18nManager.Instance.GetResource("Zhijian.ZhijianL." + char.ToUpperInvariant(key[0]) + key[1..].ToLowerInvariant() + "Theme")), "Ursa internal tooltips follow language: " + language.CultureName);
            var left = (Control)window.LeftContent!;
            var caption = window.GetVisualDescendants().OfType<Button>().Single(item => item.Name == "PART_MinimizeButton");
            Check(controls.TranslatePoint(default, window)!.Value.X >= left.TranslatePoint(new Point(left.Bounds.Width, 0), window)!.Value.X &&
                  controls.TranslatePoint(new Point(controls.Bounds.Width, 0), window)!.Value.X <= caption.TranslatePoint(default, window)!.Value.X, "Title menu and right controls fit without overlap: " + width + " " + language.CultureName);
        }
        languages.IsDropDownOpen = false;
        Check(toggle.Bounds.Width == 30 && languages.Bounds.Height == 30 && palettes.GetVisualDescendants().OfType<ListBoxItem>().Count() == 8, "All title controls retain compact dimensions at " + width);
        using var frame = window.CaptureRenderedFrame()!;
        frame.Save(Path.Combine(output, "titlebar-" + width + ".png"), new PngBitmapEncoderOptions());
    }
    vm.SelectedTitleBarLanguage = vm.TitleBarLanguages.Single(item => item.CultureName == "zh-CN");
    vm.SelectedTitleBarPalette = vm.TitleBarPalettes.Single(item => item.Key == "Aurora"); vm.ThemeMode = StudioThemeMode.Light;
}

IEnumerable<MenuItem> DescendMenus(IEnumerable<MenuItem> roots)
{
    foreach (var menu in roots)
    {
        yield return menu;
        foreach (var child in DescendMenus(menu.Items.OfType<MenuItem>())) yield return child;
    }
}

void VerifyAuxiliaryWindows()
{
    Check(window.TitleBarHeight == 40, "Main title bar is 40 pixels: " + window.TitleBarHeight);
    Check(Application.Current!.TryGetResource("DefaultFontFamily", window.ActualThemeVariant, out var family) && Equals(window.FontFamily, family), "Main window uses the application font resource: " + window.FontFamily);
    var titleBar = (Control)window.LeftContent!;
    var titleMenu = titleBar.GetVisualDescendants().OfType<Menu>().Single();
    var titleMenus = DescendMenus(titleMenu.Items.OfType<MenuItem>()).ToArray();
    var paletteMenus = titleMenus.Where(item => item.CommandParameter is string value && Enum.TryParse<StudioPalette>(value, out _)).ToArray();
    Check(paletteMenus.Length == 0 && titleMenu.Items.Count == 5, "Title menu keeps five business groups without duplicate palette dropdowns");
    using var saveModel = new SaveChangesWindowViewModel("Studio.md");
    var save = new Zhijian.Views.SaveChangesWindow("Studio.md");
    var thanks = new Zhijian.Views.ThanksWindow();
    var about = new Zhijian.Views.AboutWindow();
    var changelog = new Zhijian.Views.ChangelogWindow();
    save.Show(window); thanks.Show(window); about.Show(window); changelog.Show(window); Pump();
    Complete(((ChangelogWindowViewModel)changelog.DataContext!).LoadChineseAsync()); Pump();
    var markdown = changelog.GetVisualDescendants().OfType<CodeWF.Avalonia.Markdown.Controls.MarkdownViewer>().Single();
    var originalMarkdown = markdown.Markdown;
    var originalDocument = vm.MarkdownText;
    var originalTree = MindMapDocumentCodec.ToMarkdown(vm.Roots[0]);
    Check(!string.IsNullOrWhiteSpace(originalMarkdown) && markdown.Bounds.Width > 0, "Actual changelog MarkdownViewer is loaded during background culture regression");
    foreach (var culture in new[] { "zh-CN", "en-US", "zh-Hant", "ja-JP" })
    {
        SetBackgroundCulture(culture);
        Check(markdown.Markdown == originalMarkdown && vm.MarkdownText == originalDocument && MindMapDocumentCodec.ToMarkdown(vm.Roots[0]) == originalTree,
            "Background language change preserves actual Markdown and mind-map content: " + culture);
        var selector = window.GetVisualDescendants().OfType<Zhijian.Views.TitleBarAppearance>().Single().FindControl<ComboBox>("TitleBarLanguageSelector")!;
        Check(vm.SelectedTitleBarLanguage?.CultureName == culture && (selector.SelectedItem as LocalizationLanguage)?.CultureName == culture,
            "Background language change synchronizes the actual title selector: " + culture);
        Check(save.Title == I18nManager.Instance.GetResource(Zhijian.ZhijianL.SaveChanges), "Save dialog title changes dynamically: " + culture);
        Check(saveModel.Message.Contains("Studio.md") && saveModel.Message != "Zhijian.ZhijianL.SaveChangesMessage", "Save message keeps the document name: " + culture);
        var thanksModel = (ThanksWindowViewModel)thanks.DataContext!;
        Check(thanks.Title == thanksModel.Title && thanksModel.Description == I18nManager.Instance.GetResource(Zhijian.ZhijianL.ThanksDescription), "Thanks window changes language: " + culture);
        foreach (var palette in vm.TitleBarPalettes)
        {
            Check(I18nManager.Instance.GetResource(palette.ResourceKey) is { } label && !label.StartsWith("Zhijian."), "Palette tooltip resource follows the culture: " + culture);
        }
        using var saveFrame = save.CaptureRenderedFrame()!;
        saveFrame.Save(Path.Combine(output, $"save-{culture}.png"), new PngBitmapEncoderOptions());
        using var thanksFrame = thanks.CaptureRenderedFrame()!;
        thanksFrame.Save(Path.Combine(output, $"thanks-{culture}.png"), new PngBitmapEncoderOptions());
    }
    save.Close(); thanks.Close(); about.Close(); changelog.Close();
}

void VerifyUpdates()
{
    I18nManager.Instance.Culture = new System.Globalization.CultureInfo("en-US");
    var service = new VerificationUpdates();
    var opened = new List<string>();
    var exits = 0;
    using var model = new UpdateViewModel((_, _, _) => { }, service, opened.Add, () => exits++);
    Complete(model.CheckAsync());
    Check(model.State == UpdateState.Current, "No release produces the current state");
    service.Result = UpdateCheckResult.Failed("offline");
    Complete(model.CheckAsync());
    Check(model.State == UpdateState.Failed && model.StatusText.Contains("offline"), "Update failures remain visible and retryable");
    service.Result = new UpdateCheckResult(new UpdateInfo(new Version(99, 0, 0), "v99.0.0", "Verification", null, "https://example.invalid/releases/v99.0.0", null, null), true, null);
    Complete(model.CheckAsync());
    Complete(model.AdvanceAsync());
    Check(opened.Count == 1 && service.Downloads == 0, "Missing assets open the release page without downloading");
    service.Result = service.Result with { Update = service.Result.Update! with { AssetUrl = "https://example.invalid/Zhijian-setup.exe", ChecksumUrl = "https://example.invalid/Zhijian-setup.exe.sha256", AssetName = "Zhijian-setup.exe" } };
    Complete(model.CheckAsync());
    Complete(model.AdvanceAsync());
    Check(model.State == UpdateState.Downloaded && service.Downloads == 1, "A verified download enables installation");
    Complete(model.AdvanceAsync());
    Check(model.State == UpdateState.Installed && service.Installs == 1 && exits == 0, "Manual installation does not close the application");
    service.DelayCheck = true;
    var checking = model.CheckAsync();
    Check(model.State == UpdateState.Checking && model.IsBusy, "Checking disables repeated actions");
    I18nManager.Instance.Culture = new System.Globalization.CultureInfo("zh-CN");
    Check(model.State == UpdateState.Checking && model.ActionText != "Checking…", "Language changes preserve an active operation");
    model.Cancel(); Complete(checking);
    Check(model.State == UpdateState.Canceled && !model.IsBusy, "Cancellation restores retry availability");
    var closing = model.CheckAsync();
    model.Dispose(); Complete(closing);
    Check(closing.IsCompletedSuccessfully, "Closing About cancels the operation without accessing disposed commands");
}

void Complete(Task task)
{
    var timer = Stopwatch.StartNew();
    while (!task.IsCompleted)
    {
        if (timer.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("Update state verification timed out.");
        Dispatcher.UIThread.RunJobs(); Thread.Sleep(10);
    }
    task.GetAwaiter().GetResult();
    Dispatcher.UIThread.RunJobs();
}

sealed class VerificationUpdates : IUpdateService
{
    public UpdateServiceOptions Options { get; } = new() { Repository = "Zhijian", ProductName = "Zhijian" };
    public Version CurrentVersion => new(12, 1, 0, 10);
    public string CurrentVersionText => CurrentVersion.ToString();
    public UpdateCheckResult Result { get; set; } = UpdateCheckResult.Latest();
    public bool DelayCheck { get; set; }
    public int Downloads { get; private set; }
    public int Installs { get; private set; }
    public async Task<UpdateCheckResult> CheckAsync(Version? current = null, CancellationToken cancellationToken = default)
    {
        if (DelayCheck) await Task.Delay(Timeout.Infinite, cancellationToken);
        return Result;
    }
    public Task<UpdateDownloadResult> DownloadAsync(UpdateInfo update, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Downloads++;
        progress?.Report(new UpdateDownloadProgress(100, 100));
        return Task.FromResult(new UpdateDownloadResult("verification-only", "Zhijian-setup.exe", 100));
    }
    public Task<UpdateInstallResult> InstallAsync(UpdateDownloadResult download, CancellationToken cancellationToken = default)
    {
        Installs++;
        return Task.FromResult(new UpdateInstallResult(UpdateInstallKind.Manual, "verification-only"));
    }
}

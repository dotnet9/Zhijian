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
    about.Show(window);
    var aboutModel = (AboutWindowViewModel)about.DataContext!;
    Check(aboutModel.Version == UpdateVersion.Display(typeof(Zhijian.App).Assembly), "About version follows the application assembly");
    Check(DateTime.TryParse(aboutModel.CompileTime, out _), "About compile time is available after trimming");
    Check(aboutModel.Platform.Contains(".NET") && aboutModel.Platform.Contains("Avalonia"), "About platform comes from the runtime");
    foreach (var dark in new[] { false, true })
    {
        if (dark) vm.SetDarkTheme(); else vm.SetLightTheme();
        foreach (var culture in new[] { "zh-CN", "en-US", "zh-Hant", "ja-JP" })
        {
            I18nManager.Instance.Culture = new System.Globalization.CultureInfo(culture);
            Pump();
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
Console.WriteLine("Zhijian UI verification passed: " + output);

void Check(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); Console.WriteLine("PASS " + message); }
void Pump()
{
    var until = Stopwatch.StartNew();
    do { Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Thread.Sleep(10); } while (until.ElapsedMilliseconds < 250);
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
    Check(paletteMenus.Length == 8, "Title menu exposes exactly eight palette commands: " + paletteMenus.Length);
    foreach (var menu in paletteMenus)
    {
        menu.Command!.Execute(menu.CommandParameter);
        Check(Zhijian.App.ThemeController!.Palette.ToString() == menu.CommandParameter!.ToString(), "Palette command changes the real controller");
    }
    using var saveModel = new SaveChangesWindowViewModel("Studio.md");
    var save = new Zhijian.Views.SaveChangesWindow("Studio.md");
    var thanks = new Zhijian.Views.ThanksWindow();
    save.Show(window);thanks.Show(window);
    foreach (var culture in new[] { "zh-CN", "en-US", "zh-Hant", "ja-JP" })
    {
        I18nManager.Instance.Culture = new System.Globalization.CultureInfo(culture);Pump();
        Check(save.Title == I18nManager.Instance.GetResource(Zhijian.ZhijianL.SaveChanges), "Save dialog title changes dynamically: " + culture);
        Check(saveModel.Message.Contains("Studio.md") && saveModel.Message != "Zhijian.ZhijianL.SaveChangesMessage", "Save message keeps the document name: " + culture);
        var thanksModel = (ThanksWindowViewModel)thanks.DataContext!;
        Check(thanks.Title == thanksModel.Title && thanksModel.Description == I18nManager.Instance.GetResource(Zhijian.ZhijianL.ThanksDescription), "Thanks window changes language: " + culture);
        foreach (var menu in paletteMenus)
        {
            var key = "Zhijian.ZhijianL.Palette" + menu.CommandParameter;
            Check(menu.Header?.ToString() == I18nManager.Instance.GetResource(key), "Palette menu label follows the culture: " + culture);
        }
        using var saveFrame = save.CaptureRenderedFrame()!;
        saveFrame.Save(Path.Combine(output, $"save-{culture}.png"), new PngBitmapEncoderOptions());
        using var thanksFrame = thanks.CaptureRenderedFrame()!;
        thanksFrame.Save(Path.Combine(output, $"thanks-{culture}.png"), new PngBitmapEncoderOptions());
    }
    save.Close();thanks.Close();
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

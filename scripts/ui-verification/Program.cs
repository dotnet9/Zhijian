using System.Diagnostics;
using System.Security;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CodeWF.Avalonia.Controls.Themes;
using CodeWF.Avalonia.Lang;
using CodeWF.Avalonia.MindView;
using CodeWF.Avalonia.MindView.Controls;
using SkiaSharp;
using Zhijian.ViewModels;

var output = Path.GetFullPath(args.FirstOrDefault() ?? "artifacts/ui-verification");
Directory.CreateDirectory(output);
var testData = Path.Combine(output, "userdata"); Directory.CreateDirectory(testData);
var processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath);
File.WriteAllText(Path.Combine(AppContext.BaseDirectory, processName + ".dll.config"),
    "<configuration><appSettings><add key=\"ShowNewUserTour\" value=\"false\"/><add key=\"UserDataDirectory\" value=\""
    + SecurityElement.Escape(testData) + "\"/></appSettings></configuration>");
AppBuilder.Configure<Zhijian.App>().UseSkia().WithInterFont()
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
    frame.Save(stream);
}
foreach (var culture in new[] { "zh-CN", "zh-Hant", "en-US", "ja-JP" })
{
    I18nManager.Instance.Culture = new System.Globalization.CultureInfo(culture); Pump();
    Check(!editor.CenterTopicPlaceholder.StartsWith("CodeWF."), "MindView language is embedded: " + culture);
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
vm.SetSystemTheme(); Check(Zhijian.App.ThemeController!.Mode == StudioThemeMode.System, "System mode stays selected");
vm.ShowNewUserTour(); Pump(); Check(vm.IsNewUserTourOpen, "Guide can be replayed with CodeWF controls");
vm.IsNewUserTourOpen = false;
window.Close();
Console.WriteLine("Zhijian UI verification passed: " + output);

void Check(bool passed, string message) { if (!passed) throw new InvalidOperationException(message); Console.WriteLine("PASS " + message); }
void Pump()
{
    var until = Stopwatch.StartNew();
    do { Dispatcher.UIThread.RunJobs(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Thread.Sleep(10); } while (until.ElapsedMilliseconds < 250);
}

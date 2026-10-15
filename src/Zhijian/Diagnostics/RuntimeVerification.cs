using System.Globalization;
using System.Runtime.CompilerServices;
using CodeWF.Avalonia.MindView;
using CodeWF.Toolkit.Core.UpdateChecking;
using CodeWF.Toolkit.Lang;
using CodeWF.Toolkit.Lang.Json;

namespace Zhijian.Diagnostics;

/// <summary>验证正式原生产物，不启动桌面或读取用户配置。</summary>
internal static class RuntimeVerification
{
    public static void Run(string directory)
    {
        if (RuntimeFeature.IsDynamicCodeSupported) throw new InvalidOperationException("NativeAOT executable required.");
        var output = Path.GetFullPath(directory);
        Directory.CreateDirectory(output);
        var resources = new[] { Path.Combine(AppContext.BaseDirectory, "I18n"), Path.Combine(AppContext.BaseDirectory, "..", "Resources", "I18n") }
            .FirstOrDefault(Directory.Exists) ?? throw new InvalidOperationException("Published I18n directory is unavailable.");
        if (!I18nManager.Instance.Register(new JsonLangPlugin { ResourceFolder = resources }, CultureInfo.GetCultureInfo("en-US"), out var error))
            throw new InvalidOperationException(error);
        var title = I18nManager.Instance.GetResource(ZhijianL.AppName);
        if (string.IsNullOrWhiteSpace(title) || title.StartsWith("Zhijian.", StringComparison.Ordinal))
            throw new InvalidOperationException("Published application language resources are unavailable.");
        const string markdown = "# NativeAOT 中文 😀\n\n## Shared tree\n### Markdown round trip";
        var root = MindMapDocumentCodec.FromMarkdown(markdown);
        var restored = MindMapDocumentCodec.FromMarkdown(MindMapDocumentCodec.ToMarkdown(root));
        if (restored.Title != "NativeAOT 中文 😀" || restored.Children.Single().Children.Single().Title != "Markdown round trip")
            throw new InvalidOperationException("Native Markdown Unicode/tree round trip failed.");
        var message = $"NativeAOT runtime verification passed; version={UpdateVersion.Display(typeof(Program).Assembly)}; I18n + Unicode Markdown/tree round trip";
        File.WriteAllText(Path.Combine(output, "runtime-verification.txt"), message);
        Console.WriteLine(message);
    }
}

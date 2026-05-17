namespace CodeWF.MindView;

/// <summary>
/// 枝见支持读写的脑图文档格式。
/// </summary>
public enum MindMapFileFormat
{
    /// <summary>
    /// 可读、可直接提交版本库的 Markdown 层级列表。
    /// </summary>
    Markdown,

    /// <summary>
    /// 便于和大纲类工具交换的 OPML。
    /// </summary>
    Opml,

    /// <summary>
    /// 常见脑图工具使用的 XMind 文件包。
    /// </summary>
    XMind
}

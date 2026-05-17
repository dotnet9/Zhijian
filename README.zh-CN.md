# 枝见 Zhijian

枝见是一个基于 C#、Avalonia 和 AtomUI 的本地 Markdown-first 脑图编辑器。它把大纲、Markdown 文本和图形脑图绑定到同一份文档模型上，适合写文章提纲、梳理功能设计和整理项目结构。

English documentation: [README.md](README.md)

仓库地址：<https://github.com/dotnet9/Zhijian>

![枝见主窗口](docs/media/zhijian-main-window.png)

## 功能亮点

- 左侧大纲或 Markdown 编辑，右侧实时脑图预览。
- Markdown-first 文档模型：标题转为节点，标题下正文转为节点备注。
- 大纲和脑图都支持标题、备注内联编辑。
- 大纲和脑图之间提供可见拖动分隔条，可左右调整宽度。
- 大纲和脑图菜单补齐添加同级、添加子级、提升、降级、上移、下移、备注和删除等高频操作。
- 脑图支持平移、缩放、回到中心主题，以及基于真实节点坐标的小图。
- 支持 Markdown、OPML、XMind 导入导出。
- 应用外壳使用 AtomUI 的窗口、标题栏菜单、对话框、ToolTip 和深色主题。
- 可复用的 `CodeWF.MindView` 控件只依赖 Avalonia，不强制依赖 AtomUI。

## 运行预览

下面的截图和 GIF 均来自本轮实际运行枝见桌面程序，并通过模拟用户操作截取。

![文件菜单](docs/media/zhijian-file-menu.png)

![关于菜单](docs/media/zhijian-about-menu.png)

![大纲菜单](docs/media/zhijian-outline-menu.png)

![脑图菜单](docs/media/zhijian-mind-menu.png)

![备注同步](docs/media/zhijian-note-sync.gif)

![拖动分隔条](docs/media/zhijian-splitter-resize.gif)

![Markdown 与深色主题](docs/media/zhijian-markdown-theme.gif)

![小图预览](docs/media/zhijian-minimap-popover.png)

## 编辑流程

左侧是主要输入区。大纲视图适合快速组织层级，Markdown 视图适合纯文本维护；右侧脑图会始终从同一棵 `MindMapNode` 树同步刷新。

节点标题编辑时常用快捷键：

- `Enter`：添加同级节点。如果当前大纲节点已有子节点，则添加子节点。
- `Tab`：添加子节点或降级为子节点。
- `Shift + Tab`：提升节点。
- `Delete` 或 `Backspace`：删除空的非根节点。
- `Ctrl + 鼠标滚轮`：缩放脑图。
- `Ctrl + L`：回到中心主题。

## 文件格式

枝见默认使用可读 Markdown 保存，也可以与常见脑图工具交换数据：

- Markdown (`.md`, `.markdown`)
- OPML (`.opml`, `.xml`)
- XMind (`.xmind`)

## 项目结构

```text
Zhijian/
|-- src/CodeWF.MindView/        可复用脑图控件和文档编解码
|-- src/CodeWF.MindView.Themes/ CodeWF.MindView 默认资源
|-- src/Zhijian/                Avalonia + AtomUI 桌面应用
|-- docs/                       架构和源码设计文档
|-- docs/media/                 实际运行截图和 GIF
|-- CHANGELOG.md                英文更新日志
|-- CHANGELOG.zh-CN.md          中文更新日志
`-- Zhijian.slnx                解决方案文件
```

## 复用 CodeWF.MindView

`src/CodeWF.MindView` 独立于 AtomUI。新的 Avalonia 应用可以引用 `CodeWF.MindView` 和 `CodeWF.MindView.Themes`，在 `App.axaml` 注册 `<mindThemes:MindViewThemes />`，然后在页面中直接使用 `MindMapEditor`：

```xml
<mind:MindMapEditor
    Roots="{Binding Roots}"
    SelectedNode="{Binding SelectedNode, Mode=TwoWay}"
    Controller="{Binding}" />
```

宿主 ViewModel 提供 `ObservableCollection<MindMapNode>`，并实现 `IMindMapEditorController`，用于处理层级判断、节点创建、删除、提升和拖拽移动。`src/Zhijian` 是围绕可复用控件构建 AtomUI 桌面应用的完整参考。

更完整的接入说明见 [docs/source-design.zh-CN.md](docs/source-design.zh-CN.md)。

## 开源项目感谢

枝见的开发离不开这些优秀开源平台和项目：

- [Dotnet](https://dotnet.microsoft.com/zh-cn/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia)
- [AtomUI](https://github.com/AtomUI/AtomUI)

## 开发

环境要求：

- .NET 10 SDK

常用命令：

```powershell
dotnet restore Zhijian.slnx
dotnet build Zhijian.slnx
dotnet run --project src/Zhijian/Zhijian.csproj
```

## 文档

- [架构说明](docs/architecture.zh-CN.md)
- [Architecture](docs/architecture.md)
- [源码设计](docs/source-design.zh-CN.md)
- [Source Design](docs/source-design.md)

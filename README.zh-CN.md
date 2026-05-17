# 枝见 Zhijian

枝见是一个基于 C#、Avalonia 和 AtomUI 的本地 Markdown-first 脑图编辑器。它把大纲、Markdown 文本和图形脑图绑定到同一份文档模型上，适合写文章提纲、梳理功能设计和整理项目结构。

English documentation: [README.md](README.md)

仓库地址：<https://github.com/dotnet9/Zhijian>

![枝见主窗口](docs/media/zhijian-main-window.png)

## 功能亮点

- 打开后是空白脑图：只有一个等待输入的中心主题。
- 文件菜单支持新建、新建窗口、打开、打开文件夹、最近文件、保存、另存为、打开文件位置和关闭。
- 文件夹模式提供“文件 / 大纲”两个 Tab：选择文件夹后列出支持的脑图文件，点击文件自动切换到大纲并加载。
- 大纲、Markdown 和脑图视图共享同一棵 `MindMapNode` 树。
- 大纲和脑图都支持标题、备注内联编辑。
- 大纲和脑图菜单提供添加同级、添加子级、提升、降级、上移、下移、备注和删除等高频操作。
- 脑图支持画布拖拽、缩放、回到中心主题，以及基于真实节点坐标的小图导航。
- 支持 Markdown、OPML、XMind 打开和保存。
- 应用外壳使用 AtomUI 的窗口、标题栏菜单、对话框、列表控件、ToolTip 和深色主题。
- 可复用的 `CodeWF.MindView` 控件只依赖 Avalonia，不强制依赖 AtomUI。

## 运行预览

下面的截图和 GIF 均来自真实运行的枝见桌面程序，并通过模拟用户操作截取。

![文件菜单](docs/media/zhijian-file-menu.png)

![打开文件夹流程](docs/media/zhijian-open-folder.gif)

![节点菜单](docs/media/zhijian-node-menus.gif)

![备注同步](docs/media/zhijian-note-sync.gif)

![小图导航](docs/media/zhijian-minimap.gif)

![缩放](docs/media/zhijian-zoom.gif)

![画布拖拽](docs/media/zhijian-canvas-pan.gif)

## 编辑流程

左侧是主要输入区。使用大纲模式快速组织层级；需要纯文本维护时切换到 Markdown；右侧脑图会从同一份数据实时刷新。

节点标题编辑时常用快捷键：

- `Enter`：添加同级节点。若当前大纲节点已有子节点，则添加子节点。
- `Tab`：添加子节点或降级为子节点。
- `Shift + Tab`：提升节点。
- `Delete` 或 `Backspace`：删除空的非根节点。
- `Ctrl + 鼠标滚轮`：缩放脑图。
- `Space + 左键拖拽`：拖拽脑图画布。
- `Ctrl + L`：回到中心主题。

## 文件格式

枝见默认使用可读 Markdown 保存，也可以与常见脑图工具交换数据：

- Markdown (`.md`, `.markdown`)
- OPML (`.opml`, `.xml`)
- XMind (`.xmind`)

## 复用 CodeWF.MindView

`src/CodeWF.MindView` 独立于 AtomUI。新的 Avalonia 应用可以引用 `CodeWF.MindView` 和 `CodeWF.MindView.Themes`，在 `App.axaml` 注册 `<mindThemes:MindViewThemes />`，然后在页面中使用 `MindMapEditor`：

```xml
<mind:MindMapEditor
    Roots="{Binding Roots}"
    SelectedNode="{Binding SelectedNode, Mode=TwoWay}"
    Controller="{Binding}" />
```

宿主 ViewModel 提供 `ObservableCollection<MindMapNode>`，并实现 `IMindMapEditorController`，用于处理层级判断、节点创建、删除、提升、降级和拖拽移动。`src/Zhijian` 是围绕可复用控件构建文件工作流、大纲编辑、Markdown 同步、标题栏菜单和 AtomUI 外壳的完整参考。

更完整的接入说明见 [docs/source-design.zh-CN.md](docs/source-design.zh-CN.md)。

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

# 枝见架构说明

English version: [architecture.md](architecture.md)

枝见是一个用于编辑 Markdown-first 脑图的 Avalonia 桌面应用。仓库把可复用脑图能力和应用外壳分开：

- `CodeWF.MindView` 包含共享节点模型、脑图编辑控件、小图控件，以及 Markdown/OPML/XMind 编解码。
- `CodeWF.MindView.Themes` 包含可复用控件的默认 Avalonia 资源。
- `Zhijian` 包含桌面外壳、标题栏菜单、大纲编辑器、Markdown 面板、对话框、文件服务、最近文件记录和应用 ViewModel。

![运行时架构视图](media/zhijian-main-window.gif)

## 运行证据

`docs/media` 中的截图和 GIF 已按当前界面重新制作。应用启动默认创建空白脑图；需要展示复杂层级时，可手动打开随程序输出的 `使用手册.md`。

![文件列表流程](media/zhijian-open-folder.gif)

![标题栏菜单流程](media/zhijian-title-menus.gif)

![首次启动引导](media/zhijian-onboarding.gif)

![主题和语言流程](media/zhijian-theme-language.gif)

![节点菜单](media/zhijian-node-menus.gif)

![创建节点](media/zhijian-create-node.gif)

![脑图拖拽调整层级](media/zhijian-mind-drag.gif)

![小图导航](media/zhijian-minimap.gif)

![画布拖拽](media/zhijian-canvas-pan.gif)

## 依赖方向

```text
Zhijian
  |-- 引用桌面 UI 依赖、CodeWF.MindView、CodeWF.MindView.Themes
  |-- 负责桌面外壳、菜单、对话框、文件选择器和 ViewModel
  |
  +--> CodeWF.MindView.Themes
       |-- 引用 CodeWF.MindView 资源
       |
       +--> CodeWF.MindView
            |-- 只引用 Avalonia
            |-- 负责模型、脑图编辑器、小图和编解码
```

`CodeWF.MindView` 刻意不依赖桌面外壳库。这样脑图编辑器可以被普通 Avalonia 应用复用，而枝见应用仍然可以按自己的产品工作流组织窗口、菜单、列表、按钮、文本框、ToolTip 和对话框。

## 产品范围

- 启动时创建只有中心主题的空白文档；随程序输出的 `使用手册.md` 可通过帮助菜单手动载入。
- 左侧文件 Tab 会显示单独打开或导入的文件；打开文件夹时则列出该目录下所有支持文件。
- 左侧提供文件/大纲 Tab 或 Markdown 编辑，右侧提供图形脑图编辑器。
- 文件流程支持新建、新建窗口、打开可编辑文件、导入、打开文件夹、最近文件、保存、另存为可编辑格式、打开文件位置和关闭。
- 编辑、主题、语言、帮助和关于流程都放在标题栏菜单中。
- 使用 `Lang.Avalonia.Json` 提供中文简体、中文繁体、英语和日语资源。
- 大纲编辑器支持标题、备注、Enter/Tab/Shift+Tab/Delete 规则、拖拽调整结构，以及高频结构菜单。
- 脑图编辑器支持标题和备注左对齐内联编辑、拖拽调整结构、触控板双指/滚轮平移、指针位置缩放、`Space + 左键` 或中键画布拖拽、小图导航和回到中心主题。
- 复制为 Markdown 会把当前文档 Markdown 写入剪贴板，并通过桌面全局消息提示成功。
- 首次启动引导精准高亮文件菜单、大纲编辑区、Markdown 切换、脑图画布和状态栏，并提供“跳过”按钮。
- 应用设置集中在 `src/Zhijian/App.config`，包含新手引导开关、默认语言、最近文件数、历史步数和运行状态文件名。
- 标题栏菜单、关于、更新日志、感谢和未保存确认窗口都属于应用外壳层。
- 支持 Markdown、OPML、XMind 打开和保存；更多外部格式走只读导入，另存为仍限制在可靠写出的可编辑格式。

## 数据流

`MainWindowViewModel` 持有 `ObservableCollection<MindMapNode> Roots` 和双向 `SelectedNode`。大纲、Markdown 文本、脑图编辑器和小图都观察同一棵树。

标题、备注、颜色和树结构变化会触发布局重算、Markdown 同步、统计刷新、历史快照和未保存状态跟踪。因为模型共享，所以任一视图中的编辑都会立即更新其他视图。

## 平台范围

桌面应用目标框架为 `net10.0`。可复用的 `CodeWF.MindView` 库多目标 `net8.0`、`net9.0` 和 `net10.0`，方便在枝见桌面外壳之外复用。macOS 下标题栏菜单、窗口快捷键和脑图缩放使用 `⌘` 作为主命令键，Windows/Linux 使用 `Ctrl`。

更深入的实现说明和复用接入方式见 [source-design.zh-CN.md](source-design.zh-CN.md)。

## 开源项目感谢

枝见的开发离不开这些优秀开源平台和项目：

- [Dotnet](https://dotnet.microsoft.com/zh-cn/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia)
- [AtomUI](https://github.com/AtomUI/AtomUI)

# 枝见架构说明

English version: [architecture.md](architecture.md)

枝见是一个基于 Avalonia 和 AtomUI 的 Markdown-first 脑图编辑器。仓库把可复用脑图能力和桌面应用外壳拆开维护：

- `CodeWF.MindView` 包含共享节点模型、脑图编辑控件、小图控件，以及 Markdown/OPML/XMind 编解码。
- `CodeWF.MindView.Themes` 包含可复用控件默认 Avalonia 资源。
- `Zhijian` 包含 AtomUI 桌面外壳、标题栏菜单、大纲编辑器、Markdown 面板、对话框、文件服务和应用 ViewModel。

![实际运行主界面](media/zhijian-main-window.png)

## 运行截图依据

`docs/media` 下的截图和 GIF 均来自本轮实际运行枝见桌面程序，并通过模拟用户操作截取。

![拖动分隔条](media/zhijian-splitter-resize.gif)

![Markdown 与深色主题](media/zhijian-markdown-theme.gif)

## 依赖方向

```text
Zhijian
  |-- 引用 AtomUI、CodeWF.MindView、CodeWF.MindView.Themes
  |-- 负责桌面外壳、菜单、对话框、文件选择器和 ViewModel
  |
  +--> CodeWF.MindView.Themes
       |-- 提供 CodeWF.MindView 默认资源
       |
       +--> CodeWF.MindView
            |-- 只依赖 Avalonia
            |-- 负责模型、脑图编辑器、小图和编解码
```

`CodeWF.MindView` 不依赖 AtomUI。这样新的 Avalonia 应用可以直接复用脑图控件，而枝见应用仍然可以用 AtomUI 统一窗口、菜单、按钮、文本框、ToolTip 和对话框体验。

## 产品范围

- 左侧大纲或 Markdown 编辑，右侧图形脑图编辑。
- 大纲和脑图之间提供可见分隔条，可拖动调整宽度。
- 大纲编辑支持标题、备注、Enter/Tab/Shift+Tab/Delete 规则和高频结构菜单。
- 脑图编辑支持标题内联编辑、备注编辑、拖拽调整结构、画布平移、缩放、小图导航和回到中心主题。
- 标题栏文件和关于菜单使用 AtomUI `Menu` 与 `MenuItem`。
- 关于窗口和更新日志窗口使用 AXML 与 ViewModel。
- 支持 Markdown、OPML、XMind 导入导出。

## 数据流

`MainWindowViewModel` 持有 `ObservableCollection<MindMapNode> Roots` 和双向绑定的 `SelectedNode`。大纲、Markdown 文本、脑图编辑器和小图都观察同一棵树。

标题、备注、颜色和树结构变化时，会触发布局重算、Markdown 同步、统计刷新和历史快照。由于模型只有一份，在任意视图编辑都会立即更新其他视图。

## 平台范围

桌面应用目标框架为 `net10.0`。可复用的 `CodeWF.MindView` 库多目标 `net8.0`、`net9.0` 和 `net10.0`，方便在枝见桌面外壳之外复用。

更深入的实现说明和新应用接入方式见 [source-design.zh-CN.md](source-design.zh-CN.md)。

# 枝见源码设计

English version: [source-design.md](source-design.md)

枝见拆分为可复用 Avalonia 脑图库和产品化桌面应用。核心设计规则很简单：可复用的文档和画布行为放在 `CodeWF.MindView`，桌面工作流放在 `Zhijian`。

![枝见运行界面](media/zhijian-main-window.png)

## 设计目标

- **单一模型**：大纲、Markdown、脑图、打开/保存和导出都围绕 `MindMapNode` 工作。
- **控件可复用**：`CodeWF.MindView` 只引用 Avalonia，不依赖产品应用外壳。
- **体验由应用层负责**：枝见的窗口、菜单、列表、文本框、按钮、对话框和 ToolTip 都在应用层组织。
- **外壳可本地化**：标题栏菜单和新手引导文字使用 `Lang.Avalonia.Json` 资源，覆盖中文、英语和日语用户。
- **即时同步**：大纲、Markdown 或脑图中的编辑都会通过同一棵树更新其他视图。
- **布局可预期**：节点标题和备注都会参与宽高估算，减少深层脑图重叠。
- **操作友好**：标题栏菜单、节点菜单、快捷键、新手引导、小图、缩放和画布拖拽都有可见入口。

## 真实交互素材

这些素材已按当前界面重新制作，并使用默认加载的使用手册，方便展示文件列表、小图、缩放、画布拖拽和层级调整。

![文件菜单](media/zhijian-file-menu.png)

![标题栏菜单](media/zhijian-title-menus.gif)

![首次启动引导](media/zhijian-onboarding.gif)

![主题和语言切换](media/zhijian-theme-language.gif)

![复制 Markdown 提示](media/zhijian-copy-markdown.gif)

![文件列表](media/zhijian-open-folder.gif)

![大纲和脑图菜单](media/zhijian-node-menus.gif)

![创建节点](media/zhijian-create-node.gif)

![大纲菜单](media/zhijian-outline-menu.gif)

![脑图拖拽调整层级](media/zhijian-mind-drag.gif)

![小图](media/zhijian-minimap.gif)

![缩放](media/zhijian-zoom.gif)

![画布拖拽](media/zhijian-canvas-pan.gif)

## 项目组织

```text
src/
  CodeWF.MindView/
    MindMapNode.cs              共享节点模型
    MindMapLayoutMetrics.cs     节点尺寸和布局估算
    MindMapDropPlacement.cs     前 / 后 / 子级拖拽语义
    MindMapDocumentCodec.cs     Markdown / OPML / XMind 编解码
    IMindMapEditorController.cs 编辑器宿主接口
    IMindMapFileService.cs      应用使用的文件服务抽象
    Controls/
      MindMapEditor.cs          主要脑图编辑控件
      MindMapMiniMap.cs         小图概览控件
  CodeWF.MindView.Themes/
    Themes/Common.axaml         默认脑图资源
  Zhijian/
    Views/MainWindow.axaml      主桌面布局
    Views/OutlineEditor.cs      应用层大纲编辑器
    Views/*Window.axaml         对话框、关于、更新日志、感谢窗口
    Services/                  Avalonia 文件和应用动作服务
    ViewModels/MainWindowViewModel.cs
```

## 数据模型

`MindMapNode` 是共享文档模型。它保存标题、备注、强调色、布局坐标和子节点。`MainWindowViewModel` 持有根集合和当前选择：

```csharp
public ObservableCollection<MindMapNode> Roots { get; }
public MindMapNode? SelectedNode { get; set; }
```

大纲编辑器、Markdown 编辑器、脑图编辑器、小图和文件编解码都读写同一个模型。结构变化后会重新订阅节点通知，确保新建节点继续参与同步。

## 桌面工作流

文件菜单属于应用层工作流。它负责创建空白文档、启动新编辑器进程、打开支持的文件、把文件夹加载到文件 Tab、把最近文件保存到 `recent-files.json`、保存当前文档、另存为其他格式、打开当前文件位置，以及关闭前询问是否保存未保存改动。应用启动时会加载随程序输出的 `使用手册.md`，单独打开的文件也会插入左侧文件列表，便于后续切换。

编辑、主题、语言、帮助和关于也都属于标题栏菜单。它们提供结构编辑命令、复制为 Markdown、深色/浅色主题切换、中文简体/中文繁体/英语/日语切换、问题反馈、需求提交、PR、仓库、更新日志、感谢和关于窗口。复制为 Markdown 会调用平台剪贴板，并显示桌面全局成功提示。

首次启动引导会精准命中标题栏文件菜单、左侧大纲编辑区、Markdown 切换按钮、右侧脑图画布、`Space + 左键` 画布平移、小图预览、缩放和状态栏导航。文件菜单步骤不再高亮整块左侧面板，避免新用户把文件入口和大纲区域混在一起。引导提供“跳过”按钮，关闭或跳过后会写入程序目录中的 `new-user-tour.seen`。

`src/Zhijian/App.config` 集中管理必要的应用配置：`ShowNewUserTour` 控制引导是否可显示，`DefaultCultureName` 控制默认语言，`RecentFilesFileName` 和 `TourSeenFileName` 控制运行状态文件名，`MaxRecentFiles` 和 `MaxHistorySteps` 控制最近文件与撤销历史容量。运行时通过 `ApplicationSettings` 读取 .NET 编译后的 `Zhijian.dll.config`，配置损坏时回退到代码默认值，避免阻断应用启动。

文件 Tab 使用应用层列表控件展示当前打开的单个文件，或打开文件夹后的 Markdown、OPML 和 XMind 文件，并在选择文件后自动回到大纲编辑。

## 脑图控件

`MindMapEditor` 在滚动视图中的 Canvas 上渲染节点和连线。它处理：

- 标题和备注内联编辑
- 标题和备注在同一内容宽度内左对齐，短文本和备注都能重新获得输入焦点
- 拖拽重排兄弟节点和调整父子关系
- 虚线落点预览
- 缩放和 `Space + 左键拖拽` 画布平移
- 给小图使用的视口跟踪
- 常用结构编辑、备注和删除的浮动节点操作

节点编辑器使用 Avalonia 控件，所以可复用库保持独立。

## 大纲编辑器

`OutlineEditor` 属于应用层代码，负责把大纲输入、圆点菜单和拖拽体验组合成桌面工作流。节点圆点菜单提供用户常用结构操作：

- 添加子级
- 添加同级
- 提升为父节点
- 降级为子节点
- 上移
- 下移
- 编辑备注
- 删除

同一个圆点区域支持点击/右键菜单和拖拽。只有移动距离超过阈值才进入拖拽，避免菜单点击和拖拽互相抢事件。

## 新应用接入

新的 Avalonia 应用可以不引用 `Zhijian` 桌面应用，只复用控件库。

添加项目引用：

```xml
<ItemGroup>
  <ProjectReference Include="..\CodeWF.MindView\CodeWF.MindView.csproj" />
  <ProjectReference Include="..\CodeWF.MindView.Themes\CodeWF.MindView.Themes.csproj" />
</ItemGroup>
```

在 `App.axaml` 注册默认资源：

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:mindThemes="using:CodeWF.MindView.Themes">
    <Application.Styles>
        <mindThemes:MindViewThemes />
    </Application.Styles>
</Application>
```

在视图中放置编辑器。基础场景只需要绑定节点集合和当前选中节点：

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:mind="https://codewf.com">
    <mind:MindMapEditor
        Roots="{Binding Roots}"
        SelectedNode="{Binding SelectedNode, Mode=TwoWay}" />
</UserControl>
```

`MindMapEditor` 内置添加子级、添加同级、升降级、同级上下移动、删除、拖拽移动和自动布局，普通接入者不需要先学习完整宿主接口。需要撤销历史、未保存状态、业务限制或自定义节点创建时，再实现 `IMindMapEditorController` 并绑定 `Controller`：

```csharp
public sealed class MindMapPageViewModel : IMindMapEditorController
{
    public ObservableCollection<MindMapNode> Roots { get; } =
    [
        new MindMapNode("Center topic")
    ];

    public MindMapNode? SelectedNode { get; set; }

    public int GetLevel(MindMapNode node) => ...;
    public bool IsRoot(MindMapNode? node) => ...;
    public MindMapNode AddChild(MindMapNode? parent, string title = "New topic") => ...;
    public MindMapNode AddSibling(MindMapNode? node, string title = "New topic") => ...;
    public bool CanPromoteNode(MindMapNode? node) => ...;
    public bool PromoteNode(MindMapNode? node) => ...;
    public bool CanDemoteNode(MindMapNode? node) => ...;
    public bool DemoteNode(MindMapNode? node) => ...;
    public MindMapNode DeleteNode(MindMapNode? node) => ...;
    public bool CanMoveNode(MindMapNode? node, MindMapNode? target) => ...;
    public bool MoveNode(MindMapNode? node, MindMapNode? target, MindMapDropPlacement placement) => ...;
}
```

如果新应用还需要大纲编辑器、标题栏菜单、文件打开/保存、文件夹浏览、最近文件或 Markdown 编辑，可以参考并复用 `src/Zhijian` 的应用层实现。需要区分的是：这些是应用外壳代码，而 `CodeWF.MindView` 是可复用的 Avalonia-only 控件库。

仓库地址：<https://github.com/dotnet9/Zhijian>

## 开源项目感谢

枝见的开发离不开这些优秀开源平台和项目：

- [Dotnet](https://dotnet.microsoft.com/zh-cn/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia)
- [AtomUI](https://github.com/AtomUI/AtomUI)

# 枝见源码设计

English version: [source-design.md](source-design.md)

枝见把可复用 Avalonia 脑图能力和 AtomUI 桌面应用外壳拆开维护。核心规则很清楚：通用文档模型、脑图画布和格式编解码放在 `CodeWF.MindView`；具体桌面体验、菜单、窗口和文件交互放在 `Zhijian`。

![枝见实际运行界面](media/zhijian-main-window.png)

## 设计目标

- **模型唯一**：大纲、Markdown、脑图、导入导出都围绕 `MindMapNode` 工作。
- **控件可复用**：`CodeWF.MindView` 只引用 Avalonia，不引用 AtomUI。
- **体验由应用负责**：枝见应用使用 AtomUI 的窗口、菜单、文本框、按钮、对话框和 ToolTip。
- **交互即时同步**：在大纲或脑图任一侧编辑，另一侧立即反映同一棵树。
- **布局可预测**：节点标题和备注都参与宽高估算，减少深层脑图重叠。
- **菜单更顺手**：大纲和脑图菜单直接提供常用结构操作，不把高频动作只藏在快捷键里。

## 真实交互截图

以下资源均来自本轮实际运行枝见桌面程序，并通过模拟用户操作截取。

![文件菜单](media/zhijian-file-menu.png)

![大纲结构菜单](media/zhijian-outline-menu.png)

![脑图结构菜单](media/zhijian-mind-menu.png)

![备注同步](media/zhijian-note-sync.gif)

![拖动分隔条](media/zhijian-splitter-resize.gif)

![小图预览](media/zhijian-minimap-popover.png)

## 项目组织

```text
src/
  CodeWF.MindView/
    MindMapNode.cs              共享节点模型
    MindMapLayoutMetrics.cs     节点尺寸与布局估算
    MindMapDropPlacement.cs     拖拽落点：前、后、子节点
    MindMapDocumentCodec.cs     Markdown / OPML / XMind 编解码
    Controls/
      MindMapEditor.cs          脑图主编辑控件
      MindMapMiniMap.cs         小图概览控件
  CodeWF.MindView.Themes/
    Themes/Common.axaml         脑图控件默认资源
  Zhijian/
    Views/MainWindow.axaml      主窗口布局
    Views/OutlineEditor.cs      AtomUI 大纲编辑器
    ViewModels/MainWindowViewModel.cs
```

## 数据模型

`MindMapNode` 是共享文档模型，保存标题、备注、颜色、布局坐标和子节点。`MainWindowViewModel` 持有根节点集合和当前选中节点：

```csharp
public ObservableCollection<MindMapNode> Roots { get; }
public MindMapNode? SelectedNode { get; set; }
```

大纲编辑器、Markdown 编辑器、脑图编辑器、小图和文件编解码都读写这一份模型。树结构变化时会重新订阅节点通知，保证新建节点也进入同步链路。

## 脑图控件

`MindMapEditor` 在 `ScrollViewer` 内部通过画布绘制节点和连接线，负责：

- 标题和备注内联编辑
- 拖拽改父子关系或调整同级顺序
- 虚线落点预览
- 缩放和画布平移
- 为小图提供视口信息
- 节点悬浮操作栏：备注和删除

节点编辑器使用 Avalonia 控件，而不是 AtomUI 控件，这样 `CodeWF.MindView` 保持独立可复用。

## 大纲编辑器

`OutlineEditor` 属于应用层，因为它使用 AtomUI 文本框、菜单和 AntDesign 图标。节点圆点菜单提供这些高频结构操作：

- 添加子级
- 添加同级
- 提升为父节点
- 降级为子节点
- 上移
- 下移
- 编辑备注
- 删除

同一个圆点区域同时支持点击/右键菜单和拖拽。只有移动距离超过阈值才进入拖拽，避免普通点击菜单时被拖拽逻辑抢占。

## 新应用接入

新的 Avalonia 应用可以只使用可复用控件，不需要引用 `Zhijian` 桌面应用。

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

在页面中使用脑图编辑器：

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:mind="https://codewf.com">
    <mind:MindMapEditor
        Roots="{Binding Roots}"
        SelectedNode="{Binding SelectedNode, Mode=TwoWay}"
        Controller="{Binding}" />
</UserControl>
```

宿主 ViewModel 提供 `ObservableCollection<MindMapNode>`，并实现 `IMindMapEditorController`：

```csharp
public sealed class MindMapPageViewModel : IMindMapEditorController
{
    public ObservableCollection<MindMapNode> Roots { get; } =
    [
        new MindMapNode("中心主题")
    ];

    public MindMapNode? SelectedNode { get; set; }

    public int GetLevel(MindMapNode node) => ...;
    public bool IsRoot(MindMapNode? node) => ...;
    public MindMapNode HandleMapEnter(MindMapNode node) => ...;
    public MindMapNode HandleMapTab(MindMapNode node) => ...;
    public bool PromoteNode(MindMapNode? node) => ...;
    public MindMapNode DeleteNode(MindMapNode? node) => ...;
    public bool CanMoveNode(MindMapNode? node, MindMapNode? target) => ...;
    public bool MoveNode(MindMapNode? node, MindMapNode? target, MindMapDropPlacement placement) => ...;
}
```

如果新应用还需要大纲视图、标题栏菜单、文件导入导出或 Markdown 面板，可以参考 `src/Zhijian` 的完整实现。注意这些属于应用外壳代码，而 `CodeWF.MindView` 是可复用的 Avalonia-only 控件库。

## 开源项目感谢

枝见的开发离不开这些优秀开源平台和项目：

- [Dotnet](https://dotnet.microsoft.com/zh-cn/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia)
- [AtomUI](https://github.com/AtomUI/AtomUI)

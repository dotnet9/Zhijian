# Zhijian Source Design

Chinese version: [source-design.zh-CN.md](source-design.zh-CN.md)

Zhijian is split into a reusable Avalonia mind-map library and an AtomUI desktop application. The main design rule is simple: keep reusable document and canvas behavior in `CodeWF.MindView`, and keep product-specific desktop workflow in `Zhijian`.

![Zhijian runtime](media/zhijian-main-window.png)

## Design Goals

- **Single model**: outline, Markdown, mind map, import, and export all work with `MindMapNode`.
- **Reusable controls**: `CodeWF.MindView` references Avalonia only, so it can be used without AtomUI.
- **Application-owned experience**: Zhijian uses AtomUI windows, menus, text boxes, buttons, dialogs, and tooltips.
- **Immediate synchronization**: edits in the outline or mind map update the other side through the same tree.
- **Predictable layout**: node titles and notes participate in width and height estimation, reducing overlap in deeper maps.
- **Friendly menus**: outline and mind-map menus expose common structure operations instead of hiding them behind keyboard-only workflows.

## Runtime Interaction Evidence

These assets were captured from a real running desktop session by simulating user operations.

![File menu](media/zhijian-file-menu.png)

![Outline structure menu](media/zhijian-outline-menu.png)

![Mind-map structure menu](media/zhijian-mind-menu.png)

![Note synchronization](media/zhijian-note-sync.gif)

![Splitter resizing](media/zhijian-splitter-resize.gif)

![Mini-map popover](media/zhijian-minimap-popover.png)

## Project Organization

```text
src/
  CodeWF.MindView/
    MindMapNode.cs              shared node model
    MindMapLayoutMetrics.cs     node size and layout estimation
    MindMapDropPlacement.cs     before / after / child drop semantics
    MindMapDocumentCodec.cs     Markdown / OPML / XMind codecs
    Controls/
      MindMapEditor.cs          main mind-map editing control
      MindMapMiniMap.cs         mini-map overview control
  CodeWF.MindView.Themes/
    Themes/Common.axaml         default mind-map resources
  Zhijian/
    Views/MainWindow.axaml      main desktop layout
    Views/OutlineEditor.cs      AtomUI outline editor
    ViewModels/MainWindowViewModel.cs
```

## Data Model

`MindMapNode` is the shared document model. It stores title, note, color, layout coordinates, and children. `MainWindowViewModel` owns the root collection and current selection:

```csharp
public ObservableCollection<MindMapNode> Roots { get; }
public MindMapNode? SelectedNode { get; set; }
```

The outline editor, Markdown editor, mind-map editor, mini-map, and file codecs all read and write this same model. Structure changes re-subscribe node notifications so newly created nodes stay part of the synchronization pipeline.

## Mind-Map Control

`MindMapEditor` renders nodes and connectors on a canvas inside a scroll viewer. It handles:

- inline title and note editing
- drag/drop reparenting and sibling reordering
- dashed drop previews
- zoom and canvas panning
- viewport tracking for the mini-map
- floating node actions for note editing and deletion

Node editors use Avalonia controls, not AtomUI controls, so the reusable library stays independent.

## Outline Editor

`OutlineEditor` is application-layer code because it uses AtomUI text boxes, menus, and AntDesign icons. It exposes user-friendly structure operations from the node dot menu:

- add child
- add sibling
- promote to parent
- demote to child
- move up
- move down
- edit note
- delete

The same dot area supports click/right-click menus and drag/drop. A drag starts only after movement passes a threshold, so normal menu clicks remain reliable.

## New App Integration

A new Avalonia application can use the reusable controls without referencing the `Zhijian` desktop app.

Add project references:

```xml
<ItemGroup>
  <ProjectReference Include="..\CodeWF.MindView\CodeWF.MindView.csproj" />
  <ProjectReference Include="..\CodeWF.MindView.Themes\CodeWF.MindView.Themes.csproj" />
</ItemGroup>
```

Register default resources in `App.axaml`:

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:mindThemes="using:CodeWF.MindView.Themes">
    <Application.Styles>
        <mindThemes:MindViewThemes />
    </Application.Styles>
</Application>
```

Place the editor in a view:

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

The host ViewModel provides `ObservableCollection<MindMapNode>` and implements `IMindMapEditorController`:

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
    public MindMapNode HandleMapEnter(MindMapNode node) => ...;
    public MindMapNode HandleMapTab(MindMapNode node) => ...;
    public bool PromoteNode(MindMapNode? node) => ...;
    public MindMapNode DeleteNode(MindMapNode? node) => ...;
    public bool CanMoveNode(MindMapNode? node, MindMapNode? target) => ...;
    public bool MoveNode(MindMapNode? node, MindMapNode? target, MindMapDropPlacement placement) => ...;
}
```

If the new app also needs an outline editor, title-bar menus, file import/export, or Markdown editing, use `src/Zhijian` as the reference implementation. Keep in mind that those pieces are application-shell code, while `CodeWF.MindView` is the reusable Avalonia-only control library.

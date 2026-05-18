# Zhijian Source Design

Chinese version: [source-design.zh-CN.md](source-design.zh-CN.md)

Zhijian is split into a reusable Avalonia mind-map library and a product desktop application. The main design rule is simple: keep reusable document and canvas behavior in `CodeWF.MindView`, and keep product-specific desktop workflow in `Zhijian`.

![Zhijian runtime](media/zhijian-main-window.png)

## Design Goals

- **Single model**: outline, Markdown, mind map, open/save, and export all work with `MindMapNode`.
- **Reusable controls**: `CodeWF.MindView` references Avalonia only, so it can be used without the product shell.
- **Application-owned experience**: windows, menus, list boxes, text boxes, buttons, dialogs, and tooltips are composed in the application layer.
- **Localized shell**: title-bar menus and onboarding text are backed by `Lang.Avalonia.Json` resources for Chinese, English, and Japanese users.
- **Immediate synchronization**: edits in the outline, Markdown, or mind map update the other views through the same tree.
- **Predictable layout**: node titles and notes participate in width and height estimation, reducing overlap in deeper maps.
- **Friendly operations**: title-bar menus, outline menus, mind-map menus, keyboard shortcuts, Tour onboarding, mini-map, zoom, and canvas panning are available from visible controls.

## Runtime Interaction Evidence

These assets were refreshed against the current UI with the bundled manual loaded by default, so the file list, mini-map, zoom, canvas panning, and hierarchy changes are easier to inspect.

![File menu](media/zhijian-file-menu.png)

![Title-bar menus](media/zhijian-title-menus.gif)

![First-run onboarding](media/zhijian-onboarding.gif)

![Theme and language switching](media/zhijian-theme-language.gif)

![Copy Markdown feedback](media/zhijian-copy-markdown.gif)

![File list](media/zhijian-open-folder.gif)

![Outline and mind-map menus](media/zhijian-node-menus.gif)

![Node creation](media/zhijian-create-node.gif)

![Outline menu](media/zhijian-outline-menu.gif)

![Mind-map drag hierarchy](media/zhijian-mind-drag.gif)

![Mini-map](media/zhijian-minimap.gif)

![Zoom](media/zhijian-zoom.gif)

![Canvas panning](media/zhijian-canvas-pan.gif)

## Project Organization

```text
src/
  CodeWF.MindView/
    MindMapNode.cs              shared node model
    MindMapLayoutMetrics.cs     node size and layout estimation
    MindMapDropPlacement.cs     before / after / child drop semantics
    MindMapDocumentCodec.cs     Markdown / OPML / XMind codecs
    IMindMapEditorController.cs editor host contract
    IMindMapFileService.cs      file service abstraction used by the app
    Controls/
      MindMapEditor.cs          main mind-map editing control
      MindMapMiniMap.cs         mini-map overview control
  CodeWF.MindView.Themes/
    Themes/Common.axaml         default mind-map resources
  Zhijian/
    Views/MainWindow.axaml      main desktop layout
    Views/OutlineEditor.cs      application-layer outline editor
    Views/*Window.axaml         dialogs and about/changelog/thanks windows
    Services/                  Avalonia file and app action services
    ViewModels/MainWindowViewModel.cs
```

## Data Model

`MindMapNode` is the shared document model. It stores title, note, accent color, layout coordinates, and children. `MainWindowViewModel` owns the root collection and current selection:

```csharp
public ObservableCollection<MindMapNode> Roots { get; }
public MindMapNode? SelectedNode { get; set; }
```

The outline editor, Markdown editor, mind-map editor, mini-map, and file codecs all read and write this same model. Structure changes re-subscribe node notifications so newly created nodes stay part of the synchronization pipeline.

## Desktop Workflow

The File menu is application-layer workflow. It creates blank documents, launches a new editor process, opens supported files, opens folders into the file tab, tracks recent files in `recent-files.json`, saves the current document, saves as another format, opens the current file location, and asks whether to save unsaved changes before closing. The app starts by loading the bundled `使用手册.md`, and any individually opened file is also inserted into the left file list for quick switching.

Edit, Theme, Language, Help, and About are also title-bar menus. They expose structural commands, copy-as-Markdown, dark/light theme switching, Simplified Chinese / Traditional Chinese / English / Japanese switching, feedback links, repository links, changelog, thanks, and about windows. Copy-as-Markdown uses the platform clipboard and then reports success through a desktop global message.

First-run onboarding now targets the title-bar File menu, the left outline editor, the Markdown switch button, the right mind-map canvas, `Space + left drag` canvas panning, mini-map preview, zoom, and status-bar navigation. The file step no longer highlights the whole left pane, so new users do not confuse file entry points with the outline editor. The tour includes a Skip button; closing or skipping writes `new-user-tour.seen` in the application directory.

`src/Zhijian/App.config` centralizes the necessary application settings: `ShowNewUserTour` controls whether onboarding can appear, `DefaultCultureName` sets the default UI culture, `RecentFilesFileName` and `TourSeenFileName` control runtime state file names, and `MaxRecentFiles` / `MaxHistorySteps` control recent-file and undo-history capacity. Runtime code reads the .NET-generated `Zhijian.dll.config` through `ApplicationSettings` and falls back to code defaults if the config is missing or malformed.

The file tab uses an application-layer list to show the current opened file, or Markdown, OPML, and XMind files from an opened folder, then switches back to the outline after a file is selected.

## Mind-Map Control

`MindMapEditor` renders nodes and connectors on a canvas inside a scroll viewer. It handles:

- inline title and note editing
- title and note editors left-align within the same content width, including short text and notes that need refocus
- drag/drop reparenting and sibling reordering
- dashed drop previews
- zoom and `Space + left drag` canvas panning
- viewport tracking for the mini-map
- floating node actions for common structure edits, note editing, and deletion

Node editors use Avalonia controls, so the reusable library stays independent.

## Outline Editor

`OutlineEditor` is application-layer code that combines outline input, the node-dot menu, and drag behavior into the desktop workflow. It exposes user-friendly structure operations from the node dot menu:

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

Place the editor in a view. Basic integration only binds the node collection and the current selection:

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:mind="https://codewf.com">
    <mind:MindMapEditor
        Roots="{Binding Roots}"
        SelectedNode="{Binding SelectedNode, Mode=TwoWay}" />
</UserControl>
```

`MindMapEditor` includes basic child/sibling creation, promotion, demotion, sibling reordering, deletion, drag/drop moves, and automatic layout, so a simple host does not need to learn the full controller contract first. Implement `IMindMapEditorController` and bind `Controller` only when the host needs undo history, dirty-state tracking, business rules, or custom node creation:

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

If the new app also needs an outline editor, title-bar menus, file open/save, folder browsing, recent files, or Markdown editing, use `src/Zhijian` as the reference implementation. Keep in mind that those pieces are application-shell code, while `CodeWF.MindView` is the reusable Avalonia-only control library.

Repository: <https://github.com/dotnet9/Zhijian>

## Open Source Thanks

Zhijian is built on excellent open source platforms and libraries:

- [Dotnet](https://dotnet.microsoft.com/zh-cn/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia)
- [AtomUI](https://github.com/AtomUI/AtomUI)

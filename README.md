# Zhijian

Zhijian is a local, Markdown-first mind-map editor built with C#, Avalonia, and AtomUI. It keeps the outline, Markdown text, and graphical mind map synchronized over the same document model, so users can write structure quickly and inspect the result visually.

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

Repository: <https://github.com/dotnet9/Zhijian>

![Zhijian main window](docs/media/zhijian-main-window.png)

## Highlights

- Starts with a blank mind map: one center topic waiting for input.
- File menu for New, New Window, Open, Open Folder, Recent Files, Save, Save As, Open File Location, and Close.
- Edit menu for Undo, Redo, Add Sibling, Add Child, Promote, Demote, Move, Delete, and Copy as Markdown.
- Theme, Language, Help, and About menus are grouped in the title bar with icons and shortcuts where useful.
- Language switching uses `Lang.Avalonia.Json` resources for Simplified Chinese, Traditional Chinese, English, and Japanese.
- First-run onboarding uses AtomUI Tour and can be controlled from `src/Zhijian/App.config`.
- Folder mode with `Files` and `Outline` tabs: choose a folder, browse supported mind-map files, then load one into the editor.
- Outline, Markdown, and mind-map views share the same `MindMapNode` tree.
- Inline title and note editing in both outline and mind-map views.
- User-friendly outline and mind-map menus for adding siblings or children, promoting or demoting nodes, moving nodes, editing notes, and deleting nodes.
- Mind-map panning, zooming, center-topic navigation, and a real mini-map based on current node coordinates.
- Copy as Markdown writes the current document Markdown to the clipboard and shows an AtomUI global message.
- Markdown, OPML, and XMind open/save support.
- AtomUI shell, title-bar menus, dialogs, list controls, tooltips, Tour, global messages, and dark theme.
- Reusable `CodeWF.MindView` controls depend only on Avalonia, not AtomUI.

## Runtime Preview

All screenshots and GIFs below were captured from a real running Zhijian desktop session with simulated user operations.

![File menu](docs/media/zhijian-file-menu.png)

![Title bar menus](docs/media/zhijian-title-menus.gif)

![Onboarding tour](docs/media/zhijian-onboarding.gif)

![Theme and language switching](docs/media/zhijian-theme-language.gif)

![Copy Markdown feedback](docs/media/zhijian-copy-markdown.gif)

![Open folder workflow](docs/media/zhijian-open-folder.gif)

![Node menus](docs/media/zhijian-node-menus.gif)

![Note synchronization](docs/media/zhijian-note-sync.gif)

![Mini-map navigation](docs/media/zhijian-minimap.gif)

![Zoom](docs/media/zhijian-zoom.gif)

![Canvas panning](docs/media/zhijian-canvas-pan.gif)

## Editing Workflow

The left pane is the main writing area. Use outline mode for structured editing, switch to Markdown when text-first editing is faster, and use the right mind-map canvas for visual inspection.

Useful keyboard behavior while editing a node title:

- `Enter`: add a sibling node. If the selected outline node has children, Zhijian adds a child node.
- `Tab`: add or demote to a child node.
- `Shift + Tab`: promote a node.
- `Delete` or `Backspace`: delete an empty non-root node.
- `Ctrl + mouse wheel`: zoom the mind-map canvas.
- `Space + left drag`: pan the mind-map canvas.
- `Ctrl + L`: return to the center topic.

## File Formats

Zhijian uses Markdown as the default readable format and can exchange data with common mind-map tools:

- Markdown (`.md`, `.markdown`)
- OPML (`.opml`, `.xml`)
- XMind (`.xmind`)

## Reusing CodeWF.MindView

`src/CodeWF.MindView` is independent from AtomUI. A new Avalonia app can reference `CodeWF.MindView` and `CodeWF.MindView.Themes`, register `<mindThemes:MindViewThemes />` in `App.axaml`, and place `MindMapEditor` in a view:

```xml
<mind:MindMapEditor
    Roots="{Binding Roots}"
    SelectedNode="{Binding SelectedNode, Mode=TwoWay}"
    Controller="{Binding}" />
```

The host ViewModel provides `ObservableCollection<MindMapNode>` and implements `IMindMapEditorController` for level lookup, node creation, deletion, promotion, demotion, and drag/drop moves. The `src/Zhijian` app is the complete reference for file workflow, outline editing, Markdown synchronization, title-bar menus, and AtomUI shell integration around the reusable control.

See [docs/source-design.md](docs/source-design.md) for the reusable-control integration details.

## Project Structure

```text
Zhijian/
|-- src/CodeWF.MindView/        reusable mind-map controls and document codecs
|-- src/CodeWF.MindView.Themes/ default resources for CodeWF.MindView
|-- src/Zhijian/                Avalonia and AtomUI desktop application
|-- docs/                       architecture and source-design documentation
|-- docs/media/                 runtime screenshots and GIFs
|-- CHANGELOG.md                English changelog
|-- CHANGELOG.zh-CN.md          Chinese changelog
`-- Zhijian.slnx                solution file
```

## Open Source Thanks

Zhijian is built on excellent open source platforms and libraries:

- [Dotnet](https://dotnet.microsoft.com/zh-cn/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia)
- [AtomUI](https://github.com/AtomUI/AtomUI)

## Development

Requirements:

- .NET 10 SDK

Common commands:

```powershell
dotnet restore Zhijian.slnx
dotnet build Zhijian.slnx
dotnet run --project src/Zhijian/Zhijian.csproj
```

## Documentation

- [Architecture](docs/architecture.md)
- [Architecture Chinese](docs/architecture.zh-CN.md)
- [Source Design](docs/source-design.md)
- [Source Design Chinese](docs/source-design.zh-CN.md)

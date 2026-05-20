# Zhijian

Zhijian is a local, Markdown-first mind-map editor built with C# and Avalonia. It keeps the outline, Markdown text, and graphical mind map synchronized over the same document model, so users can write structure quickly and inspect the result visually.

Chinese documentation: [README.zh-CN.md](README.zh-CN.md)

Repository: <https://github.com/dotnet9/Zhijian>

![Zhijian main window](docs/media/zhijian-main-window.gif)

## Highlights

- Starts with a blank mind map. The bundled `使用手册.md` can be opened from Help -> Open User Manual as a richer multi-level sample and help manual.
- File menu for New, New Window, Open Editable File, Import, Open Folder, Recent Files, Save, Save As Editable Format, Open File Location, and Close.
- Edit menu for Undo, Redo, Add Sibling, Add Child, Promote, Demote, Move, Delete, and Copy as Markdown.
- Theme, Language, Help, and About menus are grouped in the title bar with icons and shortcuts where useful.
- Language switching uses `Lang.Avalonia.Json` resources for Simplified Chinese, Traditional Chinese, English, and Japanese.
- First-run onboarding precisely highlights the title-bar File menu, outline editor, Markdown switch, mind-map canvas, and bottom navigation, and includes a Skip button.
- `src/Zhijian/App.config` centralizes onboarding, default language, recent-file count, history depth, and runtime state file names.
- `Files` and `Outline` tabs on the left: the empty file pane offers Open Editable File, Import, Open Folder, and Open User Manual; individually opened or imported files appear in the file list, and opening a folder lists every supported file in that folder.
- Outline, Markdown, and mind-map views share the same `MindMapNode` tree.
- Inline title and note editing in both outline and mind-map views.
- User-friendly outline and mind-map menus for adding siblings or children, promoting or demoting nodes, moving nodes, editing notes, and deleting nodes.
- Mind-map panning, zooming, center-topic navigation, and a real mini-map based on current node coordinates.
- Copy as Markdown writes the current document Markdown to the clipboard and shows a desktop global message.
- Open Editable File and Save As Editable Format focus on editable Markdown, OPML, and XMind files; Import covers broader read-only conversion formats without implying original-format save support.
- Desktop shell with title-bar menus, dialogs, list controls, tooltips, global messages, and dark theme.
- Reusable `CodeWF.MindView` controls depend only on Avalonia and stay separate from the desktop shell.

## Runtime Preview

The screenshots and GIFs below were refreshed against the current UI with the bundled manual loaded by default, so the file list, mini-map, zoom, and canvas-panning behavior are easier to inspect.

![File menu](docs/media/zhijian-file-menu.png)

![Title bar menus](docs/media/zhijian-title-menus.gif)

![Onboarding tour](docs/media/zhijian-onboarding.gif)

![Theme and language switching](docs/media/zhijian-theme-language.gif)

![Copy Markdown feedback](docs/media/zhijian-copy-markdown.gif)

![File list workflow](docs/media/zhijian-open-folder.gif)

![Node menus](docs/media/zhijian-node-menus.gif)

![Node creation](docs/media/zhijian-create-node.gif)

![Outline menu](docs/media/zhijian-outline-menu.gif)

![Note synchronization](docs/media/zhijian-note-sync.gif)

![Mind-map node toolbar](docs/media/zhijian-node-toolbar.png)

![Mind-map drag hierarchy](docs/media/zhijian-mind-drag.gif)

![Mini-map navigation](docs/media/zhijian-minimap.gif)

![Mini-map overview](docs/media/zhijian-minimap-overview.png)

![Zoom](docs/media/zhijian-zoom.gif)

![Canvas panning](docs/media/zhijian-canvas-pan.gif)

## Editing Workflow

The left pane is the main writing area. Use outline mode for structured editing, switch to Markdown when text-first editing is faster, and use the right mind-map canvas for visual inspection.

Useful keyboard behavior while editing a node title:

- `Enter`: add a sibling node. If the selected outline node has children, Zhijian adds a child node.
- `Tab`: add or demote to a child node.
- `Shift + Tab`: promote a node.
- `Delete` or `Backspace`: delete an empty non-root node.
- `Two-finger touchpad pinch`: zoom the mind-map canvas around the pointer.
- `Two-finger touchpad scroll` or `mouse wheel`: pan the mind-map canvas.
- `⌘ + mouse wheel` on macOS or `Ctrl + mouse wheel` on Windows/Linux: zoom the mind-map canvas around the pointer.
- `Shift + mouse wheel`: pan the mind-map canvas horizontally.
- `Space + left drag` or `middle-button drag`: pan the mind-map canvas.
- `⌘ + L` on macOS or `Ctrl + L` on Windows/Linux: return to the center topic.

## File Formats

Zhijian uses Markdown as the default readable format. To avoid data loss, Save As only exposes stable editable formats:

- Markdown (`.md`, `.markdown`)
- OPML (`.opml`)
- XMind (`.xmind`)

Other mind-map, draw.io, image, Office, document, and data files can be converted through Import, then saved as one of the editable formats above.

## Reusing CodeWF.MindView

`src/CodeWF.MindView` is independent from the application shell. A new Avalonia app can reference `CodeWF.MindView` and `CodeWF.MindView.Themes`, register `<mindThemes:MindViewThemes />` in `App.axaml`, and place `MindMapEditor` in a view. Basic integration only needs roots and the current selection:

```xml
<mind:MindMapEditor
    Roots="{Binding Roots}"
    SelectedNode="{Binding SelectedNode, Mode=TwoWay}" />
```

`MindMapEditor` includes basic child/sibling creation, promotion, demotion, sibling reordering, deletion, drag/drop moves, and automatic layout. Add `Controller="{Binding}"` only when the host needs to connect undo history, dirty state, or custom business rules through `IMindMapEditorController`. The `src/Zhijian` app is the complete reference for file workflow, outline editing, Markdown synchronization, title-bar menus, and desktop shell integration around the reusable control.

See [docs/source-design.md](docs/source-design.md) for the reusable-control integration details.

## Project Structure

```text
Zhijian/
|-- src/CodeWF.MindView/        reusable mind-map controls and document codecs
|-- src/CodeWF.MindView.Themes/ default resources for CodeWF.MindView
|-- src/Zhijian/                Zhijian desktop application
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
dotnet run --project src/Zhijian/Zhijian.csproj -f net10.0
```

### macOS Packaging

For distribution to other macOS users, package Zhijian as `.app` inside a `.dmg` instead of sending the raw `dotnet publish` folder. The script builds Intel and Apple Silicon DMGs by default:

```bash
./package_macos.sh
```

Build only Apple Silicon:

```bash
./package_macos.sh osx-arm64
```

Artifacts are written to `artifacts/macos/`. Without a certificate, the script uses ad-hoc signing for local testing. For real external distribution, sign with a Developer ID certificate and notarize the DMGs:

```bash
CODESIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)" \
NOTARIZE=1 \
NOTARY_KEYCHAIN_PROFILE=zhijian-notary \
./package_macos.sh all
```

The script requires a .NET 10 SDK and automatically checks `dotnet` and `$HOME/.dotnet/dotnet`. Set `DOTNET_CMD=/path/to/dotnet` if the SDK is installed somewhere else.

## Documentation

- [Architecture](docs/architecture.md)
- [Architecture Chinese](docs/architecture.zh-CN.md)
- [Source Design](docs/source-design.md)
- [Source Design Chinese](docs/source-design.zh-CN.md)

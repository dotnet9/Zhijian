# Zhijian

Zhijian is a lightweight mind-map editor built with Avalonia and AtomUI. It focuses on a fast dual-pane workflow: edit the outline or Markdown on the left, and keep a live mind-map preview on the right.

## Features

- Dual-pane editing with an outline/Markdown editor and a synchronized mind-map canvas.
- One-click switch between outline and Markdown views.
- Markdown-first document model using heading levels:
  - `#` for the root topic
  - `##` for second-level nodes
  - `###` and deeper headings for child nodes
  - plain text under a heading as that node's note
- Focused left-pane editing with a simplified outline tree and Markdown editor.
- Keyboard-first outline editing for adding, moving, promoting, demoting, and deleting nodes.
- Mind-map node editing with inline titles, synchronized notes, floating note/delete actions, drag/drop reparenting, sibling reordering, and selection sync.
- Outline node dot menus for adding sibling/child nodes, promoting/demoting, moving up/down, adding notes, or deleting nodes while preserving dot drag/drop behavior.
- Real mini-map overview rendered from the current mind-map coordinates and viewport.
- Mind-map canvas panning with `Space + left mouse` on empty canvas space.
- Unified status bar with node statistics, undo/redo step history, mini-map preview, center-topic navigation, zoom, and help actions.
- Zoom controls in the status bar plus `Ctrl + mouse wheel` zoom from 10% to 200%.
- Draggable splitter between the outline and mind-map panes.
- Plain-text title-bar File/About buttons with arrowless menus and direct Markdown, OPML, and XMind import/export actions.
- Title-bar theme switch between light and dark modes.
- Import and export support for Markdown, OPML, and XMind.

## Editing Workflow

The left pane is the main workspace. Edit directly in the outline tree for structured work, or switch to Markdown when writing and rearranging headings is faster. Notes are written as plain text below a Markdown heading and are previewed on mind-map nodes.

Keyboard shortcuts are available when editing a node title:

- `Enter`: add a sibling node. If the selected outline node has children, it adds a child node.
- `Tab`: add or demote to a child node.
- `Shift + Tab`: promote a node.
- `Delete` or `Backspace`: delete an empty non-root node. Pressing `Backspace` once clears the last character; pressing it again on the empty title removes the node.

The right pane is the mind-map canvas. Select or edit nodes directly, use the floating node toolbar for notes and deletion, drag non-root nodes onto another node to reparent them, or drag to a node edge to reorder siblings. Hold `Space` and drag empty canvas space with the left mouse button to pan, and use the status bar controls for zoom, real mini-map navigation, and centering the root topic. `Ctrl + L` also returns the canvas to the center topic.

File and about actions are available from the title bar.

## File Formats

Zhijian uses Markdown as the default readable storage format and can also exchange data with common mind-map tools:

- Markdown (`.md`, `.markdown`)
- OPML (`.opml`, `.xml`)
- XMind (`.xmind`)

## Project Structure

```text
Zhijian/
|-- src/CodeWF.MindView/        reusable mind-map controls and document codecs
|-- src/CodeWF.MindView.Themes/ default resources for CodeWF.MindView
|-- src/Zhijian/                Avalonia and AtomUI application
|-- docs/                     Design notes, source-design documentation, and reference images
|-- Directory.Build.props     Shared MSBuild settings
|-- Directory.Packages.props  Central package versions
|-- global.json               .NET SDK pin
`-- Zhijian.slnx              Solution file
```

## Development

Requirements:

- .NET 10 SDK

Common commands:

```bash
dotnet restore Zhijian.slnx
dotnet build Zhijian.slnx
dotnet format Zhijian.slnx --verify-no-changes
dotnet run --project src/Zhijian/Zhijian.csproj
```

## Reusing CodeWF.MindView

`src/CodeWF.MindView` is intentionally independent from AtomUI. A new Avalonia app can reference `CodeWF.MindView` and `CodeWF.MindView.Themes`, register `<mindThemes:MindViewThemes />` in `App.axaml`, and place `<mind:MindMapEditor Roots="{Binding Roots}" SelectedNode="{Binding SelectedNode, Mode=TwoWay}" Controller="{Binding}" />` in a view.

The host ViewModel provides an `ObservableCollection<MindMapNode>` and implements `IMindMapEditorController` for level lookup, node creation, deletion, promotion, and drag/drop moves. The `src/Zhijian` app is a complete reference for wiring the reusable control into an AtomUI desktop shell.

## Verification

Recent verification covered:

- building the solution with zero warnings and zero errors
- formatting verification with `dotnet format --verify-no-changes`
- running the app and screenshot-checking title editing, note sync, outline menus, drag/drop previews, mini-map navigation, Markdown view, and dark theme

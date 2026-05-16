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
- Mind-map node editing with inline titles, note previews, drag movement, and selection sync.
- Mind-map canvas panning with `Space + left mouse` on empty canvas space.
- Unified status bar with node statistics, undo/redo step history, mini-map preview, center-topic navigation, zoom, and help actions.
- Zoom controls in the status bar plus `Ctrl + mouse wheel` zoom from 10% to 200%.
- Draggable splitter between the outline and mind-map panes.
- Title-bar file menu for Markdown, OPML, and XMind import/export.
- Title-bar theme switch between light and dark modes.
- Import and export support for Markdown, OPML, and XMind.

## Editing Workflow

The left pane is the main workspace. Edit directly in the outline tree for structured work, or switch to Markdown when writing and rearranging headings is faster. Notes are written as plain text below a Markdown heading and are previewed on mind-map nodes.

Keyboard shortcuts are available when editing a node title:

- `Enter`: add a sibling node. If the selected outline node has children, it adds a child node.
- `Tab`: add or demote to a child node.
- `Shift + Tab`: promote a node.
- `Delete` or `Backspace`: delete an empty non-root node. Pressing `Backspace` once clears the last character; pressing it again on the empty title removes the node.

The right pane is the mind-map canvas. Select or edit nodes directly, drag node color handles to reposition nodes, hold `Space` and drag empty canvas space with the left mouse button to pan, and use the status bar controls for zoom, mini-map navigation, and centering the root topic. `Ctrl + L` also returns the canvas to the center topic.

File actions are available from the title bar through the `File` menu.

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
|-- docs/                     Design notes and reference images
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

## Verification

Recent verification covered:

- building the solution with zero warnings and zero errors
- formatting verification with `dotnet format --verify-no-changes`
- running the app and screenshot-checking the title bar, draggable split view, unified status bar, mini-map, center-topic action, Space-drag canvas panning, and dark theme

# Zhijian

Zhijian is a lightweight desktop mind-map editor built with Avalonia. It focuses on a fast dual-pane workflow: edit the outline or Markdown on the left, and keep a live mind-map preview on the right.

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
- Zoom controls on the mind-map canvas plus `Ctrl + mouse wheel` zoom from 10% to 200%.
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

The right pane is the mind-map canvas. Select or edit nodes directly, drag node color handles to reposition nodes, hold `Space` and drag empty canvas space with the left mouse button to pan, and use the zoom control in the lower-right corner for a comfortable canvas scale.

File actions are available from the title bar through the `File` menu.

## File Formats

Zhijian uses Markdown as the default readable storage format and can also exchange data with common mind-map tools:

- Markdown (`.md`, `.markdown`)
- OPML (`.opml`, `.xml`)
- XMind (`.xmind`)

## Project Structure

```text
Zhijian/
|-- src/Zhijian.Desktop/      Avalonia desktop application
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
dotnet run --project src/Zhijian.Desktop/Zhijian.Desktop.csproj
```

## Verification

Recent verification covered:

- building the solution with zero warnings and zero errors
- formatting verification with `dotnet format --verify-no-changes`
- running the desktop app and screenshot-checking the outline, Markdown editor, file menu, and zoom controls

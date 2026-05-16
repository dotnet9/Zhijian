# Zhijian Architecture

Zhijian is an Avalonia and AtomUI application for editing mind maps stored as Markdown.

## Repository layout

- `src/CodeWF.MindView`: reusable mind-map controls, shared node model, and Markdown/OPML/XMind import/export contracts.
- `src/CodeWF.MindView.Themes`: default resources for the mind-map controls.
- `src/Zhijian`: the desktop application project. It owns the outline view, Markdown pane, shell, and file-picker implementation.
- `docs`: design notes and project documentation.
- `Directory.Packages.props`: centrally managed NuGet package versions.
- `Directory.Build.props`: shared MSBuild settings.

## Initial product scope

- Split view with an outline editor on the left and a graphical mind map editor on the right.
- Outline editor keeps all nodes expanded and supports title editing, notes, Enter/Tab/Shift+Tab/Delete editing rules, and Markdown editing mode.
- Graph view supports direct title editing, node selection, dragging, keyboard node creation/deletion, per-node accent colors, and auto layout.
- The shared status bar owns document statistics, undo/redo step history, mini-map preview, center-topic navigation, zoom, and help actions.
- The document model enforces a single level-1 center node.

## Platform scope

The application targets `net10.0`. The reusable CodeWF.MindView libraries multi-target `net8.0`, `net9.0`, and `net10.0` so the control can be reused outside the Zhijian desktop shell.

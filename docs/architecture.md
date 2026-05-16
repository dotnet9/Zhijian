# Zhijian Architecture

Zhijian is an Avalonia and AtomUI application for editing mind maps stored as Markdown.

## Repository layout

- `src/Zhijian`: the single application project.
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

The project targets `net10.0` as one AtomUI desktop application. It does not keep platform-suffixed project names because there is no parallel web, mobile, or other platform project in this repository.

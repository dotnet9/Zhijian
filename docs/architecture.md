# Zhijian Architecture

Zhijian is an Avalonia desktop application for editing mind maps stored as Markdown.

## Repository layout

- `src/Zhijian.Desktop`: the single desktop application project.
- `docs`: design notes and project documentation.
- `Directory.Packages.props`: centrally managed NuGet package versions.
- `Directory.Build.props`: shared MSBuild settings.

## Initial product scope

- Fixed split view with an outline editor on the left and a graphical mind map editor on the right.
- Outline editor keeps all nodes expanded and supports title editing, notes, Enter/Tab/Shift+Tab/Delete editing rules, and Markdown editing mode.
- Graph view supports direct title editing, node selection, dragging, keyboard node creation/deletion, per-node accent colors, and auto layout.
- The document model enforces a single level-1 center node.

## Platform scope

The desktop project targets `net10.0` and uses Avalonia desktop APIs for Windows, Linux, and macOS.

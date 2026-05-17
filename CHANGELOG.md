# Changelog

## 12.0.3.5（2026-05-17）

- 🔨[Fix]-Fixed title-bar File/About buttons being intercepted by window-drag hit testing.
- 🔨[Fix]-Fixed mind-map node title and note editing by treating inner TextBox visuals as editor input sources.
- 🔨[Optimize]-Changed outline and mind-map notes to use only smaller text and muted foreground color, without note backgrounds, left borders, or block padding.
- 🔨[Optimize]-Replaced splitter resizing with explicit column-width dragging while keeping the outline pane constrained to 320-640 px.
- 🔨[Optimize]-Unified tooltips with a compact dark floating style closer to AtomUI.
- 🧪[Test]-Reran the desktop app with screenshots for menus, splitter drag, mind-map title/note input, zoom, canvas scrolling, and maximized layout.

## 12.0.3.4（2026-05-17）

- 🔨[优化]-Removed the chevrons from the title-bar File/About buttons and flattened the File menu into direct import/export actions for a lighter menu surface.
- 🔨[优化]-Expanded the outline and mind-map menus with the common add-sibling, promote/demote, move-up/move-down actions users expect during structural editing.
- 🧪[测试]-Reran the 30-minute automated desktop loop against the final UI, covering splitter drag, view switching, theme switching, mini-map, zoom, node create/delete, note editing, and both menus.
- 📝[文档]-Updated the article, source design, and README with the reusable `CodeWF.MindView` integration story.

## 12.0.3.3（2026-05-17）

- 🔨[优化]-Made the outline/mind-map splitter visible again with a dedicated drag slot and center handle while keeping `GridSplitter` resizing.
- 🧪[测试]-Added a 30-minute automated desktop run covering splitter drag, Markdown toggles, theme toggles, mini-map, zoom, node create/delete, note add/delete, and mind-map drag operations.
- 📝[文档]-Expanded the source design and README with guidance for using `CodeWF.MindView`, `MindMapEditor`, `MindMapNode`, and `IMindMapEditorController` from a new Avalonia app.

## 12.0.3.2（2026-05-17）

- 🔨[优化]-Removed the visible mind-map node drag line while keeping a transparent drag hit area for non-root nodes.
- ✨[新增]-Added synchronized note editing in both outline and mind-map views, including empty-note collapse and Backspace/Delete removal behavior.
- ✨[新增]-Added AtomUI outline node dot menus with note and delete actions, while preserving drag/drop from the same dot.
- ✨[新增]-Added a floating mind-map node toolbar with note and delete actions.
- ✨[新增]-Added mind-map drag/drop reparenting and sibling reordering with dashed drop previews.
- 🔨[优化]-Changed the mini-map to render a true overview from current node coordinates and viewport bounds.
- 🔨[优化]-Improved auto layout so deeper nodes and notes participate in width/height estimation.
- 📝[文档]-Added Chinese source-design documentation under `docs/源码设计.md`.

## 12.0.3.1（2026-05-16）

- 🔨[优化]-Extracted the reusable mind-map editor, mini-map, node model, and Markdown/OPML/XMind codecs into `CodeWF.MindView`.
- 🔨[优化]-Added `CodeWF.MindView.Themes` with default Avalonia resources and wired Zhijian to consume the extracted controls while keeping the outline view in the app.
- 🔨[优化]-Added CodeWF-style package metadata and updated project structure documentation for the new reusable libraries.
- 🔨[优化]-Renamed the application project from `Zhijian.Desktop` to `Zhijian`, including the project path, namespace, manifest identity, and run command.
- 🔨[优化]-Restored title-bar dragging while keeping the title-bar file menu and theme switch interactive.
- 🔨[优化]-Replaced pane-local status text with a unified bottom status bar for node statistics, undo/redo step history, mini-map, center-topic navigation, zoom, and GitHub help.
- 🔨[优化]-Added a draggable `GridSplitter` between the outline and mind-map panes.
- 🔨[优化]-Improved mind-map navigation with a status-bar mini-map preview, root-topic centering, `Ctrl + L`, and more reliable `Space + left mouse` canvas panning.
- ❌[删除]-Removed `Avalonia.Themes.Fluent` so the desktop app now runs on AtomUI styling only.
- 🔨[优化]-Replaced remaining native text editors with AtomUI text controls, fixing invisible editor text after removing Fluent.
- 🔨[优化]-Rebuilt the title-bar file menu as a real AtomUI `WindowTitleBar` add-on so it stays out of the work area while remaining clickable.
- 🔨[优化]-Restored the title-bar light/dark theme switch and connected the custom outline and mind-map surfaces to the same theme state.
- 🔨[优化]-Polished the title bar branding, outline/Markdown switch, and mind-map zoom controls, then verified the main workflow with running-app screenshots.

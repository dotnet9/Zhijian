# Changelog

## 12.0.3.13（2026-05-19）

- 🔧[Optimize]-Added a compact outline shortcut help button in the bottom toolbar so users can discover `Enter`, `Tab`, `Shift+Tab`, and `Alt+Up/Alt+Down` without reading the README.
- 🧪[Test]-Built `Zhijian.slnx`, launched the desktop app, hovered the new outline shortcut help button with UI Automation, and verified the tooltip screenshot.
- 📝[Docs]-Updated README, the user manual, and controller comments so outline shortcuts match the current behavior: center-topic `Enter` adds a child, normal-node `Enter` adds a sibling, `Tab` demotes, and `Alt+Up`/`Alt+Down` reorder siblings.
- 🔧[Optimize]-Made `Alt+Up`/`Alt+Down` work while editing topic titles, matching the shortcuts already shown in the node context menu and title-bar edit menu.
- 🧪[Test]-Built `Zhijian.slnx`, launched the desktop app, created `Root/A/B` with UI Automation, and verified `Alt+Up`/`Alt+Down` reorder both the outline and mind-map views with screenshots.
- ✨[Add]-Added `osx-x64` and `osx-arm64` publish profiles and included them in the default publish scripts so release packaging covers Windows, Linux, and macOS.
- 🧪[Test]-Published `osx-x64` and `osx-arm64` from `src/Zhijian/Zhijian.csproj` and confirmed the macOS executables are generated under `publish/osx-x64/Zhijian/Zhijian` and `publish/osx-arm64/Zhijian/Zhijian`.
- 🔧[Fix]-Restored the outline editor's connection to the main mind-map controller when hosted inside `WorkspaceOutlineView`, bringing back `Enter`, `Tab`, `Shift+Tab`, empty-title deletion, and synchronized visual updates.
- 🔧[Optimize]-Centralized outline/mind-map title keyboard routing in `MindMapKeyboardGestureRouter` and coalesced outline rebuild/focus restoration to reduce repeated redraws during fast editing.
- 🧪[Test]-Built `Zhijian.slnx`, launched the desktop app, and used UI Automation screenshots to verify root `Enter`, node `Enter`, `Tab` demote, and `Shift+Tab` promote across the outline and mind-map views.

- ✨[Add]-Added an immersive mind-map mode with a left-pane hide/show toggle near the outline/mind-map boundary and a `Ctrl/⌘ + B` shortcut.
- 🔨[Optimize]-Added visible outline quick actions for adding child nodes, adding sibling nodes, copying Markdown, and switching between outline and Markdown editing.
- 🔨[Optimize]-When users open a folder or start the onboarding tour, the left pane is restored automatically so file navigation and guided targets remain visible.
- 🔨[Optimize]-Moved `CodeWF.MindView` editor chrome, placeholders, messages, and selected format names into its own `Lang.Avalonia.Json` resources with T4-generated keys.
- 🔨[Optimize]-Kept blank and fallback node titles empty through Markdown, OPML, XMind, JSON, XML, HTML, CSV, and text import paths, so localized placeholders do not become user content.
- 🔨[Optimize]-Softened second-level mind-map branch nodes with tinted backgrounds, accent borders, dark readable text, and matching mini-map previews.
- 🔨[Optimize]-Added lightweight hover, selected, and drag motion feedback for mind-map nodes using brush and shadow transitions without changing node layout.
- 🔨[Optimize]-Tightened horizontal mind-map spacing so common four-level maps fit better in a normal desktop window.
- 🔨[Optimize]-Moved `CodeWF.MindView` node metrics, shadows, colors, menus, and mini-map styling into `CodeWF.MindView.Themes` Shared/Light/Dark resources, following the resource organization style used by Ursa.Avalonia.
- 🔨[Optimize]-Split the left Files and Outline/Markdown panes into dedicated `UserControl` views and ViewModels, using `CodeWF.EventBus` for request/state decoupling from the main window ViewModel.
- 🔨[Optimize]-Switched workspace messaging to `EventBus.Default` with attribute-based handlers and preserved `CodeWF.EventBus` for Native AOT trimming.
- 🔨[Optimize]-Kept the app icon and product name only in the left title-bar area while showing the current document name after the menus, avoiding repeated app names in the window title.
- 🔨[Optimize]-Applied AtomUI buttons to the `CodeWF.MindView` floating toolbar and context menu for smoother motion and click feedback.
- 🔨[Optimize]-Refined floating mind-map toolbar spacing so the delete action no longer sits tight against the toolbar border.
- 🔨[Optimize]-Let the mind-map canvas, toolbar, menu, node shadows, and placeholder colors follow the bound light/dark theme state consistently.
- 🔨[Optimize]-Softened mind-map placeholder foregrounds and rounded third-level selected node surfaces so empty hints read as secondary content.
- 🔨[Optimize]-Added clearer spacing between the bottom history-step text and undo/redo controls.
- 🔨[Fix]-Fixed the empty Files pane action buttons being too short and clipping localized text descenders across Chinese, English, and Japanese layouts.
- 🔨[Fix]-Restored double-click maximize/restore behavior for the custom title bar to match standard desktop window interaction.
- 🔨[Optimize]-Localized outline editor text and wired startup file arguments into the main window flow.
- 🧪[Test]-Built `Zhijian.slnx`, ran the desktop app, and verified visible, immersive, restored pane, multi-level node color, language switching, empty Files buttons, title-bar double click, and custom node titles not being translated with screenshots.

## 12.0.3.12（2026-05-18）

- 🔨[Optimize]-Hid the mind-map canvas scrollbars while keeping wheel, touchpad, mini-map, and drag navigation active, reducing visual noise on laptop-sized windows.
- 🧪[Test]-Ran the desktop app and checked light/dark theme screenshots at 1366×768, 1100×720, and the minimum window size.

## 12.0.3.11（2026-05-18）

- 🔨[Fix]-Added Avalonia `PinchGestureRecognizer` handling to the mind-map canvas and strengthened native touchpad magnify handling so laptop touchpad two-finger pinch can zoom the canvas.
- 🔨[Optimize]-Touchpad pinch zoom keeps the gesture position anchored while preserving `Ctrl/⌘ + wheel`, two-finger panning, and middle/`Space + left` canvas drag.
- 📝[Docs]-Updated README, the user manual, onboarding copy, and design docs for touchpad pinch zoom.

## 12.0.3.10（2026-05-18）

- 🔨[Optimize]-Added Open Editable File, Import, Open Folder, and Open User Manual actions to the empty Files pane so users do not need to discover the title-bar File menu first.
- 🔨[Optimize]-Changed Files pane summary and empty-state wording from mind-map/folder-specific copy to file-oriented copy, matching the fact that importable reference files also appear in the list.
- 🔨[Optimize]-Changed import history labels to "Import {0}" so imported files are not mixed with editable-file open actions.
- 🧪[Test]-Built `src\Zhijian\Zhijian.csproj` and checked the File menu and empty Files pane with UI Automation.

## 12.0.3.9（2026-05-18）

- 🔨[Optimize]-Split the product meaning of Open Editable File and Import: Open now shows only reliably writable Markdown, OPML, and XMind files, while Import keeps the broader read-only conversion formats.
- 🔨[Optimize]-Renamed Save As to Save As Editable Format so readable import formats do not imply original-format write-back support.
- 📝[Docs]-Updated README, the user manual, architecture docs, and source-design docs for the Open, Import, and Save As behavior.
- 🧪[Test]-Built `src\Zhijian\Zhijian.csproj` successfully.

## 12.0.3.8（2026-05-18）

- ✨[Add]-Added an "Open User Manual" entry to the empty file pane and title-bar Help menu for loading the bundled `使用手册.md`.
- ✨[Add]-Added an AtomUI centered loading overlay for large mind-map files and batched outline/mind-map rebuilding to reduce frozen-window feedback while reading, parsing, and rendering.
- 🔨[Optimize]-Adapted canvas navigation for laptop touchpads with two-finger/wheel panning, `Ctrl/⌘ + scroll` pointer-centered zoom, `Shift + wheel` horizontal panning, and middle-button drag panning.
- 🔨[Optimize]-Completed localized strings for opening, importing, exporting, history, node actions, and loading states across Simplified Chinese, Traditional Chinese, English, and Japanese.
- 📝[Docs]-Updated README, the user manual, architecture docs, and source-design docs with the new canvas pan/zoom behavior.
- 🧪[Test]-Built `src\Zhijian\Zhijian.csproj` and smoke-started the desktop app to verify the main window appears.

## 12.0.3.7（2026-05-18）

- ✨[Add]-Added application icon metadata from `logo.ico` and NuGet package icon metadata from `logo.png`, with author and project website information.
- ✨[Add]-Expanded readable import coverage across mainstream mind-map, draw.io, image, office, document, and data formats while keeping native Markdown, OPML, and XMind export paths explicit.
- 🔨[Optimize]-Changed startup back to an empty document while keeping the bundled `使用手册.md` available as a richer help/manual file.
- 🔨[Optimize]-Moved document loading, saving, recent files, folder scanning, and import decoding onto async paths to reduce visible pauses on large documents.
- 🔨[Optimize]-Replaced CommunityToolkit.Mvvm with ReactiveUI/Avalonia-compatible view models and direct async public-method command binding.
- 🔨[Optimize]-Refactored the main view model, mind-map editor, outline editor, document codec, file-format metadata, recent-file storage, and tree layout into clearer responsibility-focused components.
- 🔨[Optimize]-Centralized file-format metadata in `MindMapFileFormatRegistry` and document import strategies in a registry-style codec pipeline.
- 🔨[Optimize]-Simplified Native AOT trimming setup and added a focused AtomUI enum-array compatibility guard so the win-x64 Native AOT build starts correctly.
- 🔨[Fix]-Improved mind-map canvas padding and note-height layout so zooming, panning, and long node notes no longer squeeze content into unusable overlap.
- 🔨[Fix]-Kept Avalonia desktop startup synchronous while using non-capturing async configuration reads, restoring the main window for `dotnet run` and published builds.
- 🔨[Fix]-Removed duplicate title-bar filename rendering so the current document name appears only after the title-bar menus.
- 🧪[Test]-Built `Zhijian.slnx`, published win-x64 Native AOT, and verified the published `Zhijian.exe` stays running after startup.

## 12.0.3.6（2026-05-18）

- 🔨[Optimize]-Changed onboarding to precisely highlight the title-bar File menu, outline editor, Markdown switch, mind-map canvas, and bottom navigation instead of using the whole left pane as the file entry.
- 🔨[Optimize]-Show individually opened files in the left file list, while keeping folder-open behavior scoped to files inside the chosen folder.
- 🔨[Optimize]-Load the bundled `使用手册.md` on startup so the first screen demonstrates the full editing workflow.
- 🔨[Optimize]-Added built-in basic node creation, deletion, promotion, demotion, sibling reordering, drag/drop moves, and auto layout to `MindMapEditor`, so simple hosts can bind only `Roots` / `SelectedNode`.
- 📝[Docs]-Reduced repeated UI-framework mentions in product docs and focused README, docs, website documentation, and the article on Zhijian features.
- 📝[Docs]-Regenerated key screenshots/GIFs with the bundled manual loaded by default for clearer file-list, mini-map, zoom, canvas-panning, and hierarchy demos.
- ✨[Add]-Expanded onboarding to cover file/outline tabs, Markdown switching, outline shortcuts, mind-map dragging, canvas panning, mini-map preview, zoom, and status-bar navigation, with a Skip button.
- 🔨[Optimize]-Use `⌘` as the primary command modifier on macOS for title-bar menus, window shortcuts, and mind-map wheel zoom while keeping `Ctrl` on Windows/Linux.
- 🔨[Optimize]-Left-align mind-map titles and notes inside the same content width, including short text and note editors that need to regain focus.
- 🔨[Optimize]-Centralized onboarding, default culture, recent-file count, history depth, and runtime state file names in `src/Zhijian/App.config` through `ApplicationSettings`.
- 📝[Docs]-Updated README, architecture, and source-design docs for macOS shortcuts, onboarding, centralized configuration, node creation, node dragging, and mini-map media.
- 🧪[Test]-Built `Zhijian.slnx` with .NET 10 and ran a ViewModel workflow covering create, add, delete, promote, demote, notes, and Markdown synchronization.

## 12.0.3.5（2026-05-17）

- ✨[Add]-Expanded the File menu into a product workflow: New, New Window, Open, Open Folder, Recent Files, Save, Save As, Open File Location, and Close.
- ✨[Add]-Added folder mode with `Files` and `Outline` tabs so a selected folder can list supported mind-map files before loading one into the editor.
- ✨[Add]-Added recent-file persistence in the application directory and unsaved-change prompts for Save, Save As, and Close.
- 🔨[Optimize]-Changed startup to an empty document with one editable center topic instead of preloaded sample content.
- 🔨[Optimize]-Made level-2 mind-map nodes use stronger generated accent backgrounds and adjusted note text to a gray, slightly larger style for clearer title/note separation.
- 🔨[Fix]-Fixed title-bar File/About buttons being intercepted by window-drag hit testing.
- 🔨[Fix]-Fixed mind-map node title and note editing by treating inner TextBox visuals as editor input sources.
- 🔨[Fix]-Aligned mind-map notes with their node titles and let short-text nodes refocus from the empty hit area.
- 🔨[Optimize]-Changed outline and mind-map notes to use only smaller text and muted foreground color, without note backgrounds, left borders, or block padding.
- 🔨[Optimize]-Replaced splitter resizing with explicit column-width dragging while keeping the outline pane constrained to 320-640 px.
- 🔨[Optimize]-Unified tooltips with a compact dark floating style closer to AtomUI.
- 🔨[Optimize]-Moved title-bar add-ons, the About window, and the changelog window into AXML plus view models, using AtomUI `Menu/MenuItem` for title-bar menus to reduce C# UI composition.
- ✨[Add]-Added a title-bar About > Thanks action with an AtomUI thanks window listing Dotnet, Avalonia UI, Semi.Avalonia, Ursa.Avalonia, and AtomUI links.
- ✨[Add]-Added Edit, Theme, Language, and Help title-bar menus with icons, shortcuts, feedback links, and copy-as-Markdown.
- ✨[Add]-Added `Lang.Avalonia.Json` localization resources for Simplified Chinese, Traditional Chinese, English, and Japanese.
- ✨[Add]-Added a first-run AtomUI Tour that introduces title-bar menus, the outline input area, Markdown switching, the mind-map canvas, and status-bar navigation.
- 🔨[Optimize]-Moved the current document name into the visual center of the title bar instead of placing it directly after the menus.
- 🔨[Optimize]-Changed important command feedback to AtomUI global messages, including Copy as Markdown and theme/language changes.
- 📝[Docs]-Replaced splitter-focused documentation media with actual workflow media for opening folders, node menus, note sync, mini-map navigation, zoom, and canvas panning.
- 📝[Docs]-Added real running-app screenshots and GIFs for title-bar menus, onboarding, theme/language switching, and copy-as-Markdown feedback.
- 📝[Docs]-Added the open source thanks list to the README and design documentation.
- 📝[Docs]-Expanded the repository README and docs with paired English/Chinese versions plus runtime screenshots and GIFs captured from the actual desktop app.
- 🧪[Test]-Reran the desktop app with screenshots for menus, splitter drag, mind-map title/note input, zoom, canvas scrolling, and maximized layout.
- 🧪[Test]-Verified the centered title, AtomUI menus, global message, Tour onboarding, theme readability, language switching, and splitter drag with real window screenshots.

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

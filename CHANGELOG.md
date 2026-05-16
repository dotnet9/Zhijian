# Changelog

## 2026-05-16

- Renamed the application project from `Zhijian.Desktop` to `Zhijian`, including the project path, namespace, manifest identity, and run command.
- Restored title-bar dragging while keeping the title-bar file menu and theme switch interactive.
- Replaced pane-local status text with a unified bottom status bar for node statistics, undo/redo step history, mini-map, center-topic navigation, zoom, and GitHub help.
- Added a draggable `GridSplitter` between the outline and mind-map panes.
- Improved mind-map navigation with a status-bar mini-map preview, root-topic centering, `Ctrl + L`, and more reliable `Space + left mouse` canvas panning.
- Removed `Avalonia.Themes.Fluent` so the desktop app now runs on AtomUI styling only.
- Replaced remaining native text editors with AtomUI text controls, fixing invisible editor text after removing Fluent.
- Rebuilt the title-bar file menu as a real AtomUI `WindowTitleBar` add-on so it stays out of the work area while remaining clickable.
- Restored the title-bar light/dark theme switch and connected the custom outline and mind-map surfaces to the same theme state.
- Polished the title bar branding, outline/Markdown switch, and mind-map zoom controls, then verified the main workflow with running-app screenshots.

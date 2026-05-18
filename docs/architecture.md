# Zhijian Architecture

Chinese version: [architecture.zh-CN.md](architecture.zh-CN.md)

Zhijian is an Avalonia desktop application for editing Markdown-first mind maps. The repository separates reusable mind-map functionality from the application shell:

- `CodeWF.MindView` contains the shared node model, editor control, mini-map control, and Markdown/OPML/XMind codecs.
- `CodeWF.MindView.Themes` contains default Avalonia resources for the reusable controls.
- `Zhijian` contains the desktop shell, title-bar menus, outline editor, Markdown pane, dialogs, file services, recent-file storage, and application ViewModels.

![Runtime architecture view](media/zhijian-main-window.gif)

## Runtime Evidence

The screenshots and GIFs in `docs/media` were refreshed against the current UI. The app now starts with a blank mind map; the bundled `使用手册.md` can still be opened manually when a complex sample is useful.

![File list workflow](media/zhijian-open-folder.gif)

![Title-bar menu workflow](media/zhijian-title-menus.gif)

![First-run onboarding](media/zhijian-onboarding.gif)

![Theme and language workflow](media/zhijian-theme-language.gif)

![Node menus](media/zhijian-node-menus.gif)

![Node creation](media/zhijian-create-node.gif)

![Mind-map drag hierarchy](media/zhijian-mind-drag.gif)

![Mini-map navigation](media/zhijian-minimap.gif)

![Canvas panning](media/zhijian-canvas-pan.gif)

## Dependency Direction

```text
Zhijian
  |-- references desktop UI dependencies, CodeWF.MindView, CodeWF.MindView.Themes
  |-- owns desktop shell, menus, dialogs, file pickers, and ViewModels
  |
  +--> CodeWF.MindView.Themes
       |-- references CodeWF.MindView resources
       |
       +--> CodeWF.MindView
            |-- references Avalonia only
            |-- owns model, mind-map editor, mini-map, and codecs
```

`CodeWF.MindView` intentionally has no desktop-shell dependency. This keeps the mind-map editor reusable in a plain Avalonia application, while Zhijian can still compose windows, menus, list controls, buttons, text boxes, tooltips, and dialogs around its product workflow.

## Product Scope

- Startup creates a blank editable center topic; the bundled `使用手册.md` can be opened manually from the file workflow.
- Split view with file/outline tabs or Markdown editing on the left and a graphical mind-map editor on the right.
- The file tab lists an individually opened file, or every supported mind-map file when a folder is opened.
- File workflow for New, New Window, Open, Open Folder, Recent Files, Save, Save As, Open File Location, and Close.
- Edit, Theme, Language, Help, and About workflows exposed from title-bar menus.
- `Lang.Avalonia.Json` i18n/l10n resources for Simplified Chinese, Traditional Chinese, English, and Japanese.
- Outline editor with title editing, notes, Enter/Tab/Shift+Tab/Delete rules, drag/drop structure changes, and high-frequency structure menus.
- Mind-map editor with left-aligned inline title/note editing, drag/drop structure changes, two-finger/wheel panning, pointer-centered zooming, `Space + left drag` or middle-button panning, mini-map navigation, and center-topic navigation.
- Copy as Markdown writes the current Markdown to the clipboard and reports success with a desktop global message.
- First-run onboarding precisely highlights the File menu, outline editor, Markdown switch, mind-map canvas, and status bar, with a Skip button.
- Application settings are centralized in `src/Zhijian/App.config`, including onboarding, default culture, recent-file count, history depth, and runtime state file names.
- Title-bar menus, about, changelog, thanks, and unsaved-changes dialogs belong to the application shell layer.
- Markdown, OPML, and XMind open/save support.

## Data Flow

`MainWindowViewModel` owns an `ObservableCollection<MindMapNode> Roots` and a two-way `SelectedNode`. The outline, Markdown text, mind-map editor, and mini-map all observe the same tree model.

Title, note, color, and tree-structure changes trigger layout recalculation, Markdown synchronization, statistics updates, history snapshots, and dirty-state tracking. Because the model is shared, editing in one view immediately updates the other views.

## Platform Scope

The desktop application targets `net10.0`. The reusable `CodeWF.MindView` libraries multi-target `net8.0`, `net9.0`, and `net10.0` so the control can be reused outside the Zhijian desktop shell. On macOS, title-bar menus, window shortcuts, and mind-map zoom use `⌘` as the command modifier; Windows/Linux use `Ctrl`.

See [source-design.md](source-design.md) for deeper implementation notes and reusable-control integration details.

## Open Source Thanks

Zhijian is built on excellent open source platforms and libraries:

- [Dotnet](https://dotnet.microsoft.com/zh-cn/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia)
- [AtomUI](https://github.com/AtomUI/AtomUI)

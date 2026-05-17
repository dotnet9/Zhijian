# Zhijian Architecture

Chinese version: [architecture.zh-CN.md](architecture.zh-CN.md)

Zhijian is an Avalonia and AtomUI desktop application for editing Markdown-first mind maps. The repository separates reusable mind-map functionality from the application shell:

- `CodeWF.MindView` contains the shared node model, editor control, mini-map control, and Markdown/OPML/XMind codecs.
- `CodeWF.MindView.Themes` contains default Avalonia resources for the reusable controls.
- `Zhijian` contains the AtomUI desktop shell, title-bar menus, outline editor, Markdown pane, dialogs, file services, recent-file storage, and application ViewModels.

![Runtime architecture view](media/zhijian-main-window.png)

## Runtime Evidence

The screenshots and GIFs in `docs/media` were captured from a real running Zhijian desktop session with simulated user operations.

![Open folder workflow](media/zhijian-open-folder.gif)

![Title-bar menu workflow](media/zhijian-title-menus.gif)

![First-run onboarding](media/zhijian-onboarding.gif)

![Theme and language workflow](media/zhijian-theme-language.gif)

![Node menus](media/zhijian-node-menus.gif)

![Mini-map navigation](media/zhijian-minimap.gif)

![Canvas panning](media/zhijian-canvas-pan.gif)

## Dependency Direction

```text
Zhijian
  |-- references AtomUI, CodeWF.MindView, CodeWF.MindView.Themes
  |-- owns desktop shell, menus, dialogs, file pickers, and ViewModels
  |
  +--> CodeWF.MindView.Themes
       |-- references CodeWF.MindView resources
       |
       +--> CodeWF.MindView
            |-- references Avalonia only
            |-- owns model, mind-map editor, mini-map, and codecs
```

`CodeWF.MindView` intentionally has no AtomUI dependency. This keeps the mind-map editor reusable in a plain Avalonia application, while Zhijian can still use AtomUI for its window, menus, list controls, buttons, text boxes, tooltips, and dialogs.

## Product Scope

- Blank startup document with one editable center topic.
- Split view with file/outline tabs or Markdown editing on the left and a graphical mind-map editor on the right.
- File workflow for New, New Window, Open, Open Folder, Recent Files, Save, Save As, Open File Location, and Close.
- Edit, Theme, Language, Help, and About workflows exposed from AtomUI title-bar menus.
- `Lang.Avalonia.Json` i18n/l10n resources for Simplified Chinese, Traditional Chinese, English, and Japanese.
- Outline editor with title editing, notes, Enter/Tab/Shift+Tab/Delete rules, drag/drop structure changes, and high-frequency structure menus.
- Mind-map editor with inline title editing, note editing, drag/drop structure changes, panning, zooming, mini-map navigation, and center-topic navigation.
- Copy as Markdown writes the current Markdown to the clipboard and reports success with an AtomUI global message.
- First-run onboarding implemented with AtomUI Tour and controlled by `src/Zhijian/App.config`.
- Title-bar menus implemented with AtomUI `Menu` and `MenuItem`.
- About, changelog, thanks, and unsaved-changes dialogs implemented with AtomUI windows and ViewModels.
- Markdown, OPML, and XMind open/save support.

## Data Flow

`MainWindowViewModel` owns an `ObservableCollection<MindMapNode> Roots` and a two-way `SelectedNode`. The outline, Markdown text, mind-map editor, and mini-map all observe the same tree model.

Title, note, color, and tree-structure changes trigger layout recalculation, Markdown synchronization, statistics updates, history snapshots, and dirty-state tracking. Because the model is shared, editing in one view immediately updates the other views.

## Platform Scope

The desktop application targets `net10.0`. The reusable `CodeWF.MindView` libraries multi-target `net8.0`, `net9.0`, and `net10.0` so the control can be reused outside the Zhijian desktop shell.

See [source-design.md](source-design.md) for deeper implementation notes and reusable-control integration details.

## Open Source Thanks

Zhijian is built on excellent open source platforms and libraries:

- [Dotnet](https://dotnet.microsoft.com/zh-cn/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [Ursa.Avalonia](https://github.com/irihitech/Ursa.Avalonia)
- [AtomUI](https://github.com/AtomUI/AtomUI)

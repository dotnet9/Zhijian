# GitHub Releases

This document keeps copy-ready GitHub Release notes for Zhijian.
本文档记录 Zhijian 每个发布版本可直接复制使用的 GitHub Release 文案。

## v12.0.3.15 - 2026-05-27

### Release Title

Zhijian 12.0.3.15 - Release Packaging And Dependency Updates

### Release Notes

#### English

##### Added

- Added `package_all.bat` for one-command release packaging.
- Added `scripts/package_zhijian_artifacts.ps1` to generate GitHub Release-ready zip archives.
- Release archives now use the `Zhijian-v<Version>-<RID>.zip` naming pattern.
- Packaging now writes `.sha256` checksum files and a JSON release manifest.

##### Improved

- `package_all.bat` runs `publish.bat` first, then writes release zips to `artifacts/release/`.
- Release zips exclude `.pdb` debug symbols and nested `.zip` files.
- The publish flow removes stale zip files from `publish/` before packaging.
- Project version metadata was updated to `12.0.3.15`.
- Updated AtomUI, `CodeWF.Markdown.Lite.Themes`, `Lang.Avalonia.Json`, and related package versions.

##### Verification

- `dotnet build Zhijian.slnx`
- `package_all.bat`

#### 简体中文

##### 新增

- 新增 `package_all.bat` 一键 release 打包入口。
- 新增 `scripts/package_zhijian_artifacts.ps1`，生成 GitHub Release 可直接上传的 zip 包。
- Release 产物统一命名为 `Zhijian-v<Version>-<RID>.zip`。
- 打包时生成 `.sha256` 校验文件和 JSON release manifest。

##### 优化

- `package_all.bat` 会先执行 `publish.bat`，再输出 release zip 到 `artifacts/release/`。
- Release zip 会排除 `.pdb` 调试符号文件和嵌套 `.zip` 文件。
- 发布流程会清理 `publish/` 下残留的旧 zip 文件。
- 项目版本更新为 `12.0.3.15`。
- 更新 AtomUI、`CodeWF.Markdown.Lite.Themes`、`Lang.Avalonia.Json` 等依赖版本。

##### 验证

- `dotnet build Zhijian.slnx`
- `package_all.bat`

## v12.0.3.13 - 2026-05-19

### Release Title

Zhijian 12.0.3.13 - Mind Map Editing And Cross-Platform Publishing

### Release Notes

#### English

##### Added

- Center-topic dragging now pans the whole mind-map canvas.
- Blank documents now show a quick-start strip for adding a child node, opening the user manual, importing files, or switching to Markdown.
- Added shortcut help entries for the mind-map and outline views.
- Added `osx-x64` and `osx-arm64` publish profiles.

##### Improved

- Opening, importing, or applying a built-in template now places the center topic in a better starting position.
- Increased default invisible canvas space to reduce early pan boundaries.
- The empty Files pane now uses AtomUI `Empty` and icon buttons.
- Status-bar tool buttons stay stable at minimum window size.
- The About window now reads assembly version and compile time metadata and uses localized resources.
- Mind-map and outline shortcut help now clarifies `Tab`, `Shift+Tab`, `Enter`, and `Alt+Up/Alt+Down` behavior.
- Outline title editing keeps focus stable after structural shortcuts.

##### Fixed

- Fixed `WorkspaceOutlineView` losing access to the main mind-map controller.
- Fixed outline `Enter`, `Tab`, `Shift+Tab`, empty-title deletion, and visual mind-map synchronization issues.
- Fixed unstable `Alt+Up` / `Alt+Down` sibling reordering while editing titles.

##### Verification

- `dotnet build Zhijian.slnx`
- `package_all.bat`

#### 简体中文

##### 新增

- 脑图中心主题支持左键拖拽平移整张画布。
- 空白文档新增快速开始操作条，可添加子节点、打开使用手册、导入文件或切换到 Markdown。
- 新增脑图和大纲快捷键帮助入口。
- 新增 `osx-x64` 和 `osx-arm64` 发布配置。

##### 优化

- 打开、导入或应用内置模板后，中心主题会自动放到更容易开始编辑的位置。
- 加大脑图默认画布留白，减少拖拽平移时过早撞到边界。
- 文件页空状态改用 AtomUI `Empty` 和图标按钮。
- 状态栏工具按钮在最小窗口尺寸下保持稳定。
- About 窗口改为读取程序集版本和编译时间，并迁入本地化资源。
- 统一脑图和大纲快捷键说明，明确 `Tab`、`Shift+Tab`、`Enter`、`Alt+Up/Alt+Down` 行为。
- 大纲标题编辑在结构快捷键后保持焦点稳定。

##### 修复

- 修复大纲视图被 `WorkspaceOutlineView` 包裹后无法拿到主窗口脑图控制器的问题。
- 修复大纲 `Enter`、`Tab`、`Shift+Tab`、空标题删除与右侧脑图不同步的问题。
- 修复标题编辑状态下 `Alt+Up` / `Alt+Down` 无法稳定调整同级顺序的问题。

##### 验证

- `dotnet build Zhijian.slnx`
- `package_all.bat`

# Zhijian 离屏回归验证

加载实际 App、主窗口和 NuGet 脑图控件，通过 Avalonia.Headless 与 Skia 验证，不操作桌面。用户数据隔离到指定输出目录。

```powershell
dotnet run --project scripts/ui-verification/UiVerification.csproj -c Release -- ./artifacts/ui-verification
if ($LASTEXITCODE -ne 0) { throw 'Zhijian UI verification failed' }
```

覆盖节点增删与撤销重做、Markdown 导入导出、八套配色 × 浅深模式、共享主题色、四语言内嵌资源、系统模式及引导重播。失败返回非零退出码，PNG 写入输出目录。

联调时通过仓库外 NuGet 配置安装 `temp_nugets` 临时包；仓库不保留本地包源。正式包发布后，用只含 nuget.org 的配置和新缓存验证恢复。

Windows 发布验收必须运行 `scripts/publish.ps1 -RuntimeIdentifier win-x64`，再启动 `artifacts/publish/win-x64/Zhijian/Zhijian.exe`。发布时保留 portable 编译符号以完成 Avalonia XAML 编译，最终发布件不带 PDB。离屏回归不能替代实际窗口、NativeAOT、IME 和多 DPI 验证。

2026-10-15 验证记录：使用仅含 nuget.org 的仓库外配置及独立缓存恢复 CodeWF.Avalonia 12.1.4.15，以上回归全部通过；解析后的依赖图不含 AtomUI。Windows x64 本地脚本发布完成，原生 EXE 已启动并保持主窗口响应，PE 的 CLR 目录为空。构建仍有依赖库的裁剪 / AOT 分析警告；真实键鼠操作、中文输入法、多 DPI 和其他系统尚未完成验收。

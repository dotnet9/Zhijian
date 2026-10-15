# Zhijian 离屏回归验证

加载实际 App、主窗口和 NuGet 脑图控件，通过 Avalonia.Headless 与 Skia 验证，不操作桌面。用户数据隔离到指定输出目录。

```powershell
dotnet run --project scripts/ui-verification/UiVerification.csproj -c Release -- ./artifacts/ui-verification
if ($LASTEXITCODE -ne 0) { throw 'Zhijian UI verification failed' }
```

覆盖节点增删与撤销重做、Markdown 导入导出、八套配色 × 浅深模式、共享主题色、四语言内嵌资源、系统模式及引导重播。失败返回非零退出码，PNG 写入输出目录。

联调时通过仓库外 NuGet 配置安装 `temp_nugets` 临时包；仓库不保留本地包源。正式包发布后，用只含 nuget.org 的配置和新缓存验证恢复。

Windows 发布验收必须运行 `scripts/publish.ps1 -RuntimeIdentifier win-x64`，再启动 `artifacts/publish/win-x64/Zhijian/Zhijian.exe`。发布时保留 portable 编译符号以完成 Avalonia XAML 编译，最终发布件不带 PDB。离屏回归不能替代实际窗口、NativeAOT、IME 和多 DPI 验证。

2026-10-15 复核：CodeWF.Avalonia 12.1.4.18 正式依赖，NativeAOT 验证器实际运行通过 139 项断言；覆盖关于窗口实际元数据、四语动态切换、八个配色菜单命令、保存 / 感谢窗口及受控更新完整状态。详见 [验收记录](../../docs/UI-VERIFICATION.md)。

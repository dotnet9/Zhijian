# Zhijian 离屏回归验证

加载实际 App、主窗口和 NuGet 脑图控件，通过 Avalonia.Headless 与 Skia 验证，不操作桌面。用户数据隔离到指定输出目录。

```powershell
dotnet run --project scripts/ui-verification/UiVerification.csproj -c Release -- ./artifacts/ui-verification
if ($LASTEXITCODE -ne 0) { throw 'Zhijian UI verification failed' }
```

覆盖节点增删与撤销重做、Markdown 导入导出、八套配色 × 浅深模式、共享主题色、四语言内嵌资源、系统模式及引导重播。标题栏验证直接点击 Ursa 内部按钮，检查三态持久化和 Studio 配色保留、八个色点、真实四语言列表、模式提示动态切语及 980/1280 四语言边界。失败返回非零退出码，PNG 写入输出目录。

联调时通过仓库外 NuGet 配置安装 `temp_nugets` 临时包；仓库不保留本地包源。正式包发布后，用只含 nuget.org 的配置和新缓存验证恢复。

Windows 发布验收先运行 `scripts/publish.ps1 -RuntimeIdentifier win-x64`，并将本验证器以同样的 NativeAOT 元数据设置发布后离屏执行。锁屏环境不启动普通桌面程序或安装器。发布时保留 portable 编译符号以完成 Avalonia XAML 编译，最终发布件不带 PDB。离屏回归不能替代真实桌面、IME 和多 DPI 验证。

2026-10-15 标题栏复核：CodeWF.Avalonia 12.1.4.21，托管离屏验证通过 208 项断言。此前 12.1.0.12 的 NativeAOT 验证通过 139 项；新版本原生结果记录见 [验收记录](../../docs/UI-VERIFICATION.md)。

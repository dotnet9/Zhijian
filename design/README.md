# 枝见界面原型

`index.html` 为零服务依赖的可点击原型，直接打开即可。此目录不参与 App 编译。

本轮按已确认的 Semi、Ursa、CodeWF 外壳增补八色配色、独立三态、四语言及关于更新流程，替代旧原型只有暖绿色与固定更新提示的状态。应用继续保留“大纲、Markdown、脑图共享同一模型”的功能：文件与大纲位于左侧，底部切换大纲/Markdown，右侧脑图持续显示，方便边写边观察结构；不再把三种视图设计成互斥的顶层页签。

| 区域 | 设计与实现合同 |
| --- | --- |
| 标题栏 | 40px 高，紧凑菜单；三态与八色独立，四语言实时刷新 |
| 左侧 | 默认 360px，范围 280—560px；文件、大纲及 Markdown 输入 |
| 中间 | 14px 分隔区，拖拽调整宽度；右侧保留真实脑图视口 |
| 底部 | 状态、撤销、定位、缩放与小图入口 |
| 关于 | 实际版本、编译时间、平台与作者；检查、下载、取消、重试与安装状态 |

主题数据按共享 StudioTheme 的颜色令牌固化于 `assets/palettes.json`，不需要其他仓库。原型文案由宿主 JSON 生成，运行：

```powershell
pwsh scripts/sync_design_resources.ps1
pwsh scripts/sync_design_resources.ps1 -Check
npm ci --prefix design/tests
# 首次安装后执行 npx playwright install chromium
npm test --prefix design/tests
```

门禁操作真实 DOM 入口，覆盖 16 套配色、系统变化、四语言、1240/980 两种宽度、编辑切换、标题同步、小图及四种更新场景，要求无脚本异常。它不执行 .NET 文件转换、下载或安装；这些由原生程序和 UI 验证器分别检查。原型中的文档与编译时间是展示数据。

锁屏期间仅执行无头浏览器与离屏检查；字体度量、IME、多 DPI 和系统窗口行为仍需对应桌面环境补验。两库 Demo 的固定原型不受本次 App 原型增补影响。

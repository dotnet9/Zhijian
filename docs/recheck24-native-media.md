# 当前原生截图与历史媒体来源

2026-10-16，版本 12.1.0.16。当前 PNG 从正式 Windows x64 NativeAOT UI 验证器的原始捕获逐字节复制，绘制真实应用控件，不是安装器运行截图。桌面保持锁定，样例数据与运行目录隔离。

正式原生源提交：`c6487be43d4c7c55cfc1b2f959b172d0152dd8ff`，不可变标签：`v12.1.0.16`。原生构建、运行时自检和 UI 日志由本轮独立交付收据保存。此文档与图片在标签之后独立提交，不改变正式构建源或版本标签。

| 仓库图片 | 原生捕获文件 | SHA-256（复制前后相同） |
| --- | --- | --- |
| `docs/media/zhijian-v12.1.0.16-main.png` | `zhijian-Aurora-light.png` | `c7e63ee19a220f4f1fa66ff939b9719bbf3988df9d7d9a4581e75b4aaf122e0b` |
| `docs/media/zhijian-v12.1.0.16-dark.png` | `zhijian-DotNet-dark.png` | `49739acfd45be2112e1d0b66763c186ba127f62a1a373123fc65b76239c4fc91` |

## 保留的历史媒体

下表版本仅指媒体文件最后一次仓库内容更新时的项目版本，不能证明实际录制构建版本。以下 18 项媒体本轮均未重新录制或验收为当前版本。SHA-256 仅绑定保留的历史文件。

| 历史文件 | 最后内容更新提交 | 当时项目版本 | 当前保留文件 SHA-256 |
| --- | --- | --- | --- |
| `docs/media/zhijian-main-window.gif` | `2589c495feae8a1405bde70e0333474e4e40c6f2` | `12.0.3.6` | `c9055b2c679034d5ad300eb1e48d03e9f8a21f0bd57e46c390ff6bf41742be24` |
| `docs/media/zhijian-file-menu.png` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `88eee1613a6074c0dde07aa909f05eb4bdc013c5e89bd29ecfd886a72ff61291` |
| `docs/media/zhijian-title-menus.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `ec2221721a0eee464aad7766b7b805b042e9ed095b83b8fc2400207c3fbb5f48` |
| `docs/media/zhijian-onboarding.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `24d0468222242dff57f9306694aa3a0fb4fa3df22ab1357bb3c61286ebe9c237` |
| `docs/media/zhijian-theme-language.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `4615a453b91de05b2cb7bd2ef16d6963e0a6c19366d72bcd917fb093c8eb2f91` |
| `docs/media/zhijian-copy-markdown.gif` | `f2d2d41f49a63c9ab5cd50ccdf5c35e753c03844` | `12.0.3.6` | `cac908a1c924a014140315cb868ea2743da15577a0ab968ecaa86e0bfd014b1d` |
| `docs/media/zhijian-open-folder.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `7a265aaeb729b136e293b7702228f463f995f13a0a984282cba0b917066ce15a` |
| `docs/media/zhijian-node-menus.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `0a911a66a97a5dfe7c811e2998f2380736baa6c1003dd5c891b9fac7f5877742` |
| `docs/media/zhijian-create-node.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `e168168094a5c83082172d72245692a6b86284661c49089b427af893c06ffa16` |
| `docs/media/zhijian-outline-menu.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `414842f027a30d7708e65475cd0e01e14a8a38915e68001e32cd368de26b1833` |
| `docs/media/zhijian-note-sync.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `b117dd0b38ad71249210b9d6edfdd23f652dea0f902cbe7aa7ca476b9c451925` |
| `docs/media/zhijian-node-toolbar.png` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `d1acfc8f9d8e2d0d2dfb263de9e593649978cd931920dffc1472811905a7a983` |
| `docs/media/zhijian-mind-drag.gif` | `eda1505082d1ed00563f490ebe7c8a25a0b9a245` | `12.0.3.6` | `2fa671683440215c85a3f6695166bcd0a70b5848e2fc37b290cc4dabd70356f9` |
| `docs/media/zhijian-minimap.gif` | `eda1505082d1ed00563f490ebe7c8a25a0b9a245` | `12.0.3.6` | `5d37d372434127668e9f1dd85d963df2876c30a5c496b8e88cad5cc95ee782a1` |
| `docs/media/zhijian-minimap-overview.png` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `ba5c1a3cdf820440b66e98b691681c6be189ea070884845ea227ca3d63cefd3f` |
| `docs/media/zhijian-zoom.gif` | `eda1505082d1ed00563f490ebe7c8a25a0b9a245` | `12.0.3.6` | `1c7f30484d333602bbe2ada062d1ab6874c60d4e8d7ce8f138f5311466d816d7` |
| `docs/media/zhijian-canvas-pan.gif` | `ea2b078a921c7b98b2f43acba41f91e5d85f25de` | `12.0.3.6` | `bf441ef997c5f0c7909ae49d76717a98e1153a7eef4c0aad04bc798a7bad1f31` |
| `docs/media/zhijian-native-palettes.gif` | `ae524b5ea52ca00c857b946380c642bc15a5f187` | `12.1.0.11` | `d9bb395bb30917bc8f9730d40508335e020d3b48707a83cc15ffb31c0c0fab38` |

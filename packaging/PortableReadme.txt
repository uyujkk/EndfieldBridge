ENDFIELDBRIDGE 0.3.2 / 终末地启动桥接

运行 EndfieldBridge.exe / Run EndfieldBridge.exe.
本程序自带 .NET 运行时，无需另行安装 / Includes the .NET runtime.

EndfieldBridge.exe        主程序 / Application
StartFromModManager.cmd   管理器启动入口 / Mod Manager entry
docs/                    安装、风险、来源与源码信息 / Guides, risks, credits, source
licenses/                AGPL 与第三方许可证 / AGPL and third-party licenses
data/                    首次运行后生成：配置、日志与恢复信息 / Created on first run

首次设置请阅读 docs/INSTALL.md 和 docs/RISK.zh-CN.md。
Read docs/INSTALL.md and docs/RISK.md before setup.
管理器中选择 StartFromModManager.cmd；游戏目录仍须填写实际游戏目录。
Select StartFromModManager.cmd in your manager; keep the actual game folder in XXMI.

旧版同目录升级后首次运行会将已知配置与日志迁入 data/，保留内容，不覆盖已有新版数据。
同目录升级不改变桥接 EXE 路径；换目录前先恢复 EFMI，再重新适配与授权。
旧版 ZIP 用户建议在全新目录解压新版；覆盖解压不会自动移除旧 DLL。
Setup 会清理旧版 0.3.1 随包安装的已知文件；不清理私人文件。

Known legacy settings migrate into data/ on first run without overwriting newer data.
Use Setup for an in-place installed upgrade. Extract portable ZIPs into a fresh folder;
overlay extraction cannot remove old DLLs. Restore EFMI before moving the bridge.

运行时原生组件由 .NET 自动解包到临时缓存，配置与游戏路径始终以此 EXE 所在目录为准。
Native runtime components extract to a temporary .NET cache; the executable path
and data/ remain beside this EXE. Do not modify or point XXMI at the cache.

账号风险：第三方启动与 Mod 可能导致账号处罚或封禁，不保证账号安全。
ACCOUNT RISK: Third-party launching and Mods may lead to penalties or bans.

AGPL-3.0-only / WITHOUT ANY WARRANTY / 不提供任何担保
Copyright (c) 2026 EndfieldBridge contributors; upstream credits: docs/NOTICE.md.
License: licenses/LICENSE. You may copy, modify and redistribute under the license.
对应源码 / Corresponding source:
https://github.com/uyujkk/EndfieldBridge/releases/tag/v0.3.2

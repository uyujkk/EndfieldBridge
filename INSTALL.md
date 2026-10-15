# Install / 安装 — EndfieldBridge 0.3.2

**Unofficial software: third-party launching and DLL Mods may lead to account
penalties or bans. No successful test guarantees account safety.**

**非官方工具：第三方启动与 DLL Mod 可能导致账号处罚或封禁，技术测试不能保证账号安全。**

## Choose a package / 选择发布包

- `EndfieldBridge-0.3.2-win-x64-Setup.exe`: current-user installer. Installs to a
  writable user folder, adds Start Menu shortcuts and an optional desktop shortcut.
  Installation itself does not require administrator rights. Game launching may
  request UAC separately. No game is launched at the end of installation.
- `EndfieldBridge-0.3.2-win-x64-portable.zip`: extract the entire folder to a
  writable location, then run `EndfieldBridge.exe`. Do not run inside the ZIP or
  copy only the EXE.
- Both contain .NET 9.0.21 for Windows x64; no separate .NET installation is needed.
  Supported target: Windows 10/11 x64. Neither includes game files, loaders or Mods.
- `EndfieldBridge-0.3.2-source.zip` contains the corresponding source and build
  scripts. Compare downloaded artifacts with `SHA256SUMS.txt` using PowerShell
  `Get-FileHash -Algorithm SHA256 <file>`.

安装版默认只为当前用户安装，可选择桌面快捷方式；安装本身不请求管理员权限。
便携版需完整解压到可写目录。两种形式均自带运行时，不需要另装 .NET。
程序启动游戏时可能单独请求 UAC；安装结束不会自动启动游戏。


## Folder layout / 文件结构

```text
EndfieldBridge-0.3.2-win-x64/
  EndfieldBridge.exe
  StartFromModManager.cmd
  README.txt
  docs/
  licenses/
  data/                   # created on first run / 首次运行生成
```

The .NET runtime and required Chinese system-control resources are bundled into
one compressed EXE. Native runtime components extract automatically into .NET's
per-user temporary cache. Do not use that cache as a launcher or settings path.
The actual bridge EXE path, XXMI callback and manager entry remain in this folder.

Profiles, logs, session locks and restore metadata live in data/. First launch
moves known legacy files from this same folder into data/ without changing their
bytes. A pre-existing data/ file wins; conflicting legacy files are left intact.
Installer upgrades remove only exact filenames shipped by version 0.3.1; they
preserve user data and unrelated files. ZIP extraction cannot remove old files:
use a fresh folder for portable upgrades, restoring EFMI before moving and then
setting it up at the new location, or use the installer in the existing location.

运行时已合并到单个 EXE，原生组件由 .NET 自动解包到用户临时缓存，启动入口不变。
配置、日志和恢复信息写入 data/；首次运行迁移同目录旧数据，不覆盖已有新版配置。
安装版升级清理 0.3.1 随包提供的确切文件，保留私人数据与其他文件。便携 ZIP 请完整
解压到新目录；覆盖解压无法自动删除旧 DLL。换目录前先恢复 EFMI，再重新适配。

## First use / 首次使用

1. Read `RISK.md` / `RISK.zh-CN.md`, `LICENSE` and `NOTICE.md`.
2. Run the bridge and select your own `Endfield.exe`. Leave the official parent
   empty for discovery, or choose the actual official signed `Games.exe`.
3. For Mods, choose your separately installed `XXMI Launcher.exe`, close the
   game and XXMI, then use **Import EFMI**, **Back up + set up** and enable Mod mode.
4. At XXMI's protected-settings prompt, verify the complete bridge path and choose
   **Keep** only if it is correct. **Reset** clears the custom command.
5. For Integrated Mod Manager, use **Copy manager entry** and select the generated
   `StartFromModManager.cmd` as the launch file for the Endfield repository.

中文完整步骤见 README.zh-CN.md。XXMI 的游戏目录须包含 Endfield.exe 和 Endfield_Data，
不能填桥接 EXE 或快捷方式。默认不启用 Mod；需要先由用户明确配置和适配。

## Upgrade, move and uninstall / 更新、移动与卸载

Exit the game, bridge and XXMI normally before updating. Keep the same install
folder when upgrading. The installer never ships or overwrites `data/profile.json`,
data logs or backup metadata. Back up the application folder before an upgrade.

Existing users of the lab/portable build: restore EFMI from the old bridge before
moving to a different folder. Then set up EFMI again from the new location; the
custom command contains an absolute executable path and needs new XXMI approval.
Do not assume installing elsewhere updates an existing loader command.

Before uninstalling, use **Restore EFMI** while the bridge is still installed.
Uninstall removes installed application files and shortcuts but preserves
user-created profiles/logs/backups. It does not restore XXMI automatically,
remove game/loader files, or undo game-operator records. Portable users restore
EFMI first and may then remove their extracted folder.

更新前正常退出游戏、桥接和 XXMI，同目录升级保留用户配置。旧实验版换到新位置时，
先用旧版“恢复 EFMI”，再在新位置适配并重新授权。卸载前也先恢复 EFMI；卸载器不自动
修改加载器配置，保留用户生成的配置、日志与备份，不处理游戏文件或运营方记录。

## Licensing and source / 许可与源码

AGPL-3.0-only, WITHOUT ANY WARRANTY. Copyright and upstream credits: `NOTICE.md`.
License, notices and matching source are available from **Risks / license** in
the app and https://github.com/uyujkk/EndfieldBridge/releases/tag/v0.3.2 .

The project installer and application are not Authenticode-signed by this
project; Windows may show an unknown-publisher/SmartScreen prompt. Check the
source and hashes and make your own trust decision. Do not disable protection
or grant exclusions as an installation step. Hashes establish integrity against
the published files, not account safety or official approval.

# Copyright, provenance and modifications / 版权、来源与修改说明

EndfieldBridge 0.3.2 — modified on 2026-10-15.

Copyright (c) 2026 EndfieldBridge contributors. This notice claims only the
contributors' own additions. All upstream rights remain with their respective
authors and contributors. The complete bridge is distributed under
**GNU Affero General Public License version 3 only (AGPL-3.0-only)**; see LICENSE.
It is provided WITHOUT ANY WARRANTY. You may copy, modify and redistribute it
under that license. No additional account-risk acknowledgment restricts those rights.

## Launch-core provenance

- **BlockyDeer/endfield-nolauncher**, by BlockyDeer and contributors, AGPL-3.0:
  https://github.com/BlockyDeer/endfield-nolauncher/tree/fcda770937cc58711801c2c3a58d93ff2d4b16a8
  Reviewed files: `src/launch.cpp`, `src/launch.h`, `src/main.cpp`,
  `tests/options_tests.cpp`. The downloaded `src/launch.cpp` matches that commit
  (SHA-256 `A4796A61D28D6727A0F52B5B9E9E3F2170AE6E28480CD25E76BF0A090BD69D1E`).
- **Dr-hydra/Better-Endfield**, by Dr-hydra and contributors, AGPL-3.0:
  https://github.com/Dr-hydra/Better-Endfield/tree/514f6d631088a9d4d52eb68a40914674c52ff79b
  Upstream file: `native/loaders/shared/official_launcher.cpp`. The intermediate
  project declares that it copied its launch functions from this source. During
  the 2026-10-15 provenance review, this file and its license were inspected;
  its argument-quoting function was verified to match the intermediate project.

The launch approach and the structure of the argument-quoting helper in
`src/Native.cs` were adapted after reading the intermediate source. This is not
a clean-room implementation. Different programming languages and the absence
of imported C++ source files do not establish independence. The project is now
distributed under AGPL-3.0 to preserve the applicable copyleft permissions.

Changes relative to those sources include a C#/.NET Windows Forms implementation,
.NET certificate and safe-handle handling, different launcher discovery and
argument forwarding, a bilingual GUI, persisted profiles, isolated self-process
tests, XXMI configuration backup/field-level restore, and manager entry scripts.
Version 0.3.1 adds AGPL notices, in-application legal/source links, self-contained
Windows x64 packaging, and an installer. This does not relicense upstream code
or purport to revoke permissions already validly granted for original additions.

## Interoperability reference

**SpectrumQT/XXMI-Launcher**, by SpectrumQT and contributors, declares GPL-3.0:
https://github.com/SpectrumQT/XXMI-Launcher

Its public configuration and signature-validation code was reviewed to understand
Custom Launch fields, process detection, UTF-8 command bytes, Base64 DER public
keys, and ECDSA/SHA-256 signatures. EndfieldBridge uses .NET cryptography and an
external XXMI process; it does not bundle XXMI source, binaries, private keys,
injectors, game files, or Mods. Users obtain and license those components separately.

## Distribution components

- Self-contained builds include Microsoft .NET, licensed separately. Runtime
  license and third-party notices accompany the application. .NET source:
  https://github.com/dotnet/runtime and https://github.com/dotnet/winforms
- Setup executables are built with Inno Setup, by Jordan Russell, Martijn Laan
  and contributors: https://jrsoftware.org/ . The compiler is not bundled.
  Its license notice accompanies the installer payload. The bundled installer
  translation `packaging/ChineseSimplified.isl` credits Zhenghan Yang (Kira),
  obtained through the official translation directory from `jrsoftware/issrc`
  commit `6ef32198ef1f7b7b375cd4b6b90896c2a58eb4c2`. Its original header is retained;
  see `packaging/INNO-SETUP-LICENSE.txt` for the upstream Inno Setup license.
- Windows API documentation informed API usage; this notice does not imply
  endorsement by Microsoft or by any referenced project.

## Corresponding source

For each release, the same GitHub release page provides the installer, portable
binary package, SHA-256 checksums, and an explicit source archive containing the
bridge source and packaging/build scripts at the tagged commit:
https://github.com/uyujkk/EndfieldBridge/releases/tag/v0.3.2

Build instructions are in CONTRIBUTING.md and `scripts/Build-Release.ps1`.
The general-purpose .NET SDK and Inno Setup compiler are obtained separately.

## 中文说明

本项目保留所有上游作者与贡献者的权利；以上作者名为来源致谢，不冒称拥有其代码版权。
启动核心在阅读 endfield-nolauncher 后实现，其中参数转义结构与上游接近；该项目进一步
来源于 Better-Endfield。现以 AGPL-3.0-only 整理整个独立桥接项目，不再声称属于
clean-room 或已确认纯 MIT 的实现。2026-10-15 的审查也直接核对了 Better-Endfield。

修改包括 C# 图形界面、双语、配置、加载器适配、备份恢复、测试和安装发布脚本。
安装包与便携包不含官方游戏、XXMI、Mod 或私人配置；对应源码在同一发布页提供。
程序无担保，允许依许可证复制、修改与再分发。风险说明不是附加许可限制，也不代表
游戏官方授权。独立 Mod 管理器不在本项目许可证变更范围内。

Version 0.3.2 (2026-10-15) bundles the runtime into one compressed executable, groups
documents and licenses, migrates known runtime state to data/, and provides an exact
0.3.1 package-file cleanup list for installer upgrades. No game-launch algorithm changes.

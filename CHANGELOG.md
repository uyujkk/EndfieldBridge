# Changelog

## 0.3.2 — 2026-10-15

- Bundle the self-contained runtime into one compressed EXE; retain English and
  simplified-Chinese controls without loose language/runtime folders.
- Keep only the app, manager entry, quick guide, docs/ and licenses/ in the package
  root; use data/ for profiles, logs, tests, locks and restore metadata.
- Migrate known legacy state without overwriting newer data; preserve the bridge
  EXE path and XXMI callback for same-folder upgrades.
- Clean exact 0.3.1 package filenames during installer upgrades. User data and
  unrelated files are preserved. Portable ZIPs should be extracted to fresh folders.

## 0.3.1 — 2026-10-15

- Distribute the standalone bridge under AGPL-3.0-only, with full license text,
  upstream provenance, explicit modification notices, and matching source archives.
- Add bilingual in-app license, credits and source links alongside account risks.
- Add Windows x64 self-contained portable and current-user installer builds,
  with .NET 9.0.21, installation/upgrade instructions and SHA-256 checksums.
- Preserve existing launch, loader integration and backup/restore behavior.
- This change applies to EndfieldBridge, not to the separate Mod Manager project.

AGPL 整理仅涉及独立桥接项目；增加程序内许可入口、自带运行时的安装/便携包及对应源码，
不改动独立 Mod 管理器的许可证或发布版本。

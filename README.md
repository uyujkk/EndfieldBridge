# Endfield Bridge

[简体中文](README.zh-CN.md) | English

> **ACCOUNT RISK: This is unofficial software. Third-party launching and DLL-based Mods may lead to account penalties, including bans. There is no ban-free guarantee. Read [RISK.md](RISK.md) before using it.**

A Windows desktop companion for starting Arknights: Endfield with an official signed parent process, with optional XXMI / EFMI integration and an entry point for Integrated Mod Manager.

![English interface preview](docs/images/en-US-activity.png)

## Features

- Chinese / English interface with a saved language preference.
- Separate launch settings, activity, loader setup, and risk information.
- Official `Games.exe` discovery, Authenticode trust checks and signer-organization checks.
- A suspended official parent process selected using the Windows process-creation API.
- Optional XXMI / EFMI Custom Launch integration; Mod loading stays with the installed loader.
- Configuration backup and field-level restore that preserves unrelated settings.
- Read-only verification of XXMI command authorization using its public key; no private-key access.
- `StartFromModManager.cmd` for managers that accept a launcher file but cannot pass arguments.

Signature checks, process creation, and injection logs are **not evidence of official approval or account safety**. The project is not affiliated with or endorsed by Hypergryph, GRYPHLINE, or the XXMI project.

## Requirements

- Windows 10/11 x64 and .NET 9 Desktop Runtime.
- Your own installed game and official mainland-China Hypergryph launcher with a valid signed `Games.exe`.
- XXMI / EFMI installed separately if you choose Mod mode.
- Administrator consent when launching requires it. Opening the settings window does not require elevation.

Other regions, Wine/Proton, and different loader versions have not been validated by this project. No official game files or loader libraries are included.

## Build and run

```powershell
dotnet build src/EndfieldBridge.csproj -c Release -o app
.\app\EndfieldBridge.exe
```

The output is framework-dependent. Keep the generated DLL, runtime configuration, and executable together.

1. Select the actual `Endfield.exe` and, optionally, the actual `XXMI Launcher.exe`.
2. Leave **Official Games.exe** empty to discover the official launcher, or select it manually.
3. Use **Import EFMI** to read the configured game path and arguments, then **Validate**.
4. To enable Mods, exit the game and XXMI. Open **Loader setup**, select **Back up & set up**, and enable Mod mode.
5. On XXMI's first protected-settings prompt, verify the exact bridge executable and arguments, then choose **Keep**. **Reset** clears the command. XXMI creates the authorization signature itself.

The **game folder** in XXMI must be the folder containing `Endfield.exe` and `Endfield_Data`. Do not enter a shortcut, the bridge executable, or the loader executable there.

### Integrated Mod Manager

Select **Copy manager entry**, then choose the copied `StartFromModManager.cmd` as the launcher path for the Endfield repository in your manager. The bridge does not directly edit manager repositories or change other game profiles.

The intended chain is: manager → bridge → XXMI / EFMI → bridge Custom Launch → game. Without Mod mode, the bridge starts the game directly. Do not put a waiting bridge command in XXMI's pre-launch command field.

### Settings and restore

`profile.json`, `bridge.log`, and backup metadata are written beside the application. This folder must be writable. Logs and backups can contain local paths; review them before sharing.

Setup changes seven EFMI importer fields: `game_launch`, `start_method`, `game_folder`, `game_process_exe_enabled`, `game_process_exe`, `custom_launch`, and, only when the command changes, `custom_launch_signature`. Other importer settings and game profiles are preserved.

Use **Restore EFMI** before moving the application folder. Restore returns the saved launch fields, including any originally invalid values; choose a valid game folder afterward if necessary. Local restore cannot undo any game-operator records.

### Command line

| Option | Purpose |
| --- | --- |
| No arguments | Open the settings window |
| `--launch` | Launch using the saved mode; may request UAC |
| `--game-only` | Launch without invoking XXMI |
| `--game-only --from-xxmi` | Loader callback; read current EFMI arguments |
| `--self-test` | Run isolated fixtures and a harmless self-process parent probe |
| `--render-preview <png> --language=en-US --page=risk` | Render a UI preview without launching the game |

## Validation and limits

Self-tests cover argument quoting, configuration backup/restore, unrelated-field preservation, malformed game-folder handling, public-key command verification, language switching, and actual Windows parent-process IDs using this program as both probe processes. They never launch the game or perform Mod injection. The official-signature integration check is skipped when no local game profile is configured.

A local user-run session produced an XXMI DLL-injection success log. This is bounded evidence of that session, not a guarantee of visual Mod correctness, future compatibility, or account safety. A live game launch is intentionally not part of automated tests.

## References and implementation provenance

The following sources informed development. A reference acknowledges what was reviewed; it does not imply endorsement or permission from its authors.

| Source | How it was used |
| --- | --- |
| [BlockyDeer/endfield-nolauncher](https://github.com/BlockyDeer/endfield-nolauncher) | Its README and locally downloaded source were reviewed to understand the suspended official process and parent-process launch approach. |
| [Dr-hydra/Better-Endfield](https://github.com/Dr-hydra/Better-Endfield) | An indirect upstream reference: endfield-nolauncher identifies this project as the source of its launch implementation. It was not directly reviewed as part of this project. |
| [SpectrumQT/XXMI-Launcher](https://github.com/SpectrumQT/XXMI-Launcher) | Its public source was reviewed for Custom Launch configuration, process detection, and command-signature verification to support interoperability. |
| Microsoft Windows API documentation: [parent-process attribute](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute), [WinVerifyTrust](https://learn.microsoft.com/en-us/windows/win32/api/wintrust/nf-wintrust-winverifytrust) | API contracts used for process creation, parent selection, and official-file signature verification. |

No source files from the referenced projects were directly incorporated into this repository. Development did include reading third-party source, so this is **not** a claim of a clean-room implementation or an independently audited license-compatibility conclusion. Implementing a feature in a different language does not, by itself, establish independence from the code that was reviewed.

## License

[MIT](LICENSE) is the license declared for this project's source. It does not relicense any referenced project or third-party component. In particular, endfield-nolauncher declares AGPL-3.0; attribution alone would not satisfy that license if its protected code were copied, translated, adapted, or otherwise incorporated into a covered derivative work. Such reuse would require compliance with the applicable license or separate permission from the relevant rights holders.

Copyright licensing is also separate from the game operator's rules: neither MIT nor these acknowledgments grants permission to use third-party tools with the game. See [RISK.md](RISK.md).

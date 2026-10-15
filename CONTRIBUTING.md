# Development

Use .NET 9 SDK on Windows x64. There are no external NuGet package dependencies.

```powershell
dotnet build src/EndfieldBridge.csproj -c Release -o app
.\app\EndfieldBridge.exe --self-test
Get-Content .\app\data\self-test.txt
```

Self-tests create temporary fixtures below data/ in the output directory and start only the bridge executable as a harmless probe. They do not launch Endfield or perform injection. Run on a machine without a configured game to exercise the portable path; the official game-signature check is then skipped explicitly.

UI preview examples:

```powershell
.\app\EndfieldBridge.exe --render-preview .\app\en.png --language=en-US --page=activity
.\app\EndfieldBridge.exe --render-preview .\app\zh.png --language=zh-CN --page=setup
```

The screenshots in `docs/images` are rendered previews with blank machine-specific paths, not evidence of game compatibility. Never commit runtime profiles, logs, backups, keys, binaries, or user screenshots containing personal paths. Stage source and documentation explicitly.

When reporting bugs, include the bridge version, Windows and loader versions, language, and a redacted error message. Do not include account credentials or unreviewed XXMI configuration.

## Release builds

Source and packaging scripts are licensed AGPL-3.0-only; preserve upstream notices
in NOTICE.md. Use a clean committed checkout on Windows x64, .NET 9 SDK and Inno
Setup 6.7+ (ISCC.exe). PowerShell 7 is recommended. Obtain the compiler separately
from https://jrsoftware.org/isdl.php and verify its Authenticode publisher.
Inno Setup supports /PORTABLE=1 for a build-tools-only installation.

```powershell
$env:NUGET_PACKAGES = 'G:\Integrated_Mod_Manager_Development\tools\nuget-packages'
pwsh -File scripts/Build-Release.ps1 -InnoCompiler 'G:\path\to\ISCC.exe'
```

The script publishes a compressed self-contained Windows x64 single executable with pinned .NET
9.0.21, runs the existing isolated self-tests, excludes their fixtures and all
user profiles, groups documents and runtime license notices in docs/ and licenses/, creates a source ZIP from the
exact Git commit, compiles the installer, and hashes the final assets. Outputs
are below outputs/releases/0.3.2; use a fresh -OutputDirectory when rebuilding.
The normal developer app/ folder and real loader/manager configurations are not
used for packaging. Do not run --configure-loader during release verification.

Test an extracted portable package and a temporary installer target, including
uninstall and preservation of user-created files. Do not launch the game as part
of release tests. Publish source ZIP and SHA256SUMS.txt beside both binary assets
on the same tagged GitHub release. Mark experimental builds as pre-releases.
No signing certificate is configured; state that the project's EXE and installer
are unsigned. Build scripts and source links do not establish account safety.

The portable publish enables internal single-file compression. The installer
publish disables it and lets Inno LZMA compress the bundle instead. Both variants
run the same self-tests and are built from the same source commit; their EXE hashes
and installed sizes differ. This avoids nested compression increasing Setup size.

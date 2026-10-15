# Development

Use .NET 9 SDK on Windows x64. There are no external NuGet package dependencies.

```powershell
dotnet build src/EndfieldBridge.csproj -c Release -o app
.\app\EndfieldBridge.exe --self-test
Get-Content .\app\self-test.txt
```

Self-tests create temporary fixtures below the output directory and start only the bridge executable as a harmless probe. They do not launch Endfield or perform injection. Run on a machine without a configured game to exercise the portable path; the official game-signature check is then skipped explicitly.

UI preview examples:

```powershell
.\app\EndfieldBridge.exe --render-preview .\app\en.png --language=en-US --page=activity
.\app\EndfieldBridge.exe --render-preview .\app\zh.png --language=zh-CN --page=setup
```

The screenshots in `docs/images` are rendered previews with blank machine-specific paths, not evidence of game compatibility. Never commit runtime profiles, logs, backups, keys, binaries, or user screenshots containing personal paths. Stage source and documentation explicitly.

When reporting bugs, include the bridge version, Windows and loader versions, language, and a redacted error message. Do not include account credentials or unreviewed XXMI configuration.

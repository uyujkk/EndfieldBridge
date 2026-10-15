; SPDX-License-Identifier: AGPL-3.0-only
; Copyright (c) 2026 EndfieldBridge contributors. Modified 2026-10-15.
#ifndef AppVersion
  #error AppVersion must be supplied by Build-Release.ps1
#endif
#ifndef PayloadDir
  #error PayloadDir must be supplied by Build-Release.ps1
#endif
#ifndef ArtifactDir
  #error ArtifactDir must be supplied by Build-Release.ps1
#endif

[Setup]
AppId={{A1EA12BE-13FD-47CA-B352-FB60A8308343}
AppName=EndfieldBridge
AppVersion={#AppVersion}
AppPublisher=EndfieldBridge contributors
AppPublisherURL=https://github.com/uyujkk/EndfieldBridge
AppSupportURL=https://github.com/uyujkk/EndfieldBridge/issues
AppUpdatesURL=https://github.com/uyujkk/EndfieldBridge/releases
AppCopyright=Copyright (c) 2026 EndfieldBridge contributors; upstream credits in NOTICE.md
DefaultDirName={localappdata}\Programs\EndfieldBridge
DefaultGroupName=EndfieldBridge
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
DisableProgramGroupPage=yes
LicenseFile={#PayloadDir}\LICENSE
InfoBeforeFile=InstallRisk.txt
InfoAfterFile=InstallAfter.txt
OutputDir={#ArtifactDir}
OutputBaseFilename=EndfieldBridge-{#AppVersion}-win-x64-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\EndfieldBridge.exe
CloseApplications=no
RestartApplications=no
AppMutex=Local\EndfieldBridge.Running,Local\EndfieldBridge.GameSession
SetupMutex=EndfieldBridge.Setup
Uninstallable=yes
VersionInfoDescription=EndfieldBridge current-user setup (AGPL-3.0)

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\EndfieldBridge"; Filename: "{app}\EndfieldBridge.exe"; WorkingDir: "{app}"
Name: "{group}\Installation and risk information"; Filename: "{app}\INSTALL.md"
Name: "{group}\{cm:UninstallProgram,EndfieldBridge}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\EndfieldBridge"; Filename: "{app}\EndfieldBridge.exe"; WorkingDir: "{app}"; Tasks: desktopicon

; Deliberately no [Run] action: installation never launches the app or game.
; No [UninstallDelete] rules: user-generated settings, logs and backups survive.

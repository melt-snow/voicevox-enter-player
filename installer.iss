#define MyAppName "VOICEVOX Enter Player"
#define MyAppExeName "VoicevoxEnterPlayer.exe"

#ifndef VERSION
  #define VERSION "0.0.0"
#endif

[Setup]
AppId={{6F3A9C4E-8B2D-4E71-A5C9-1D7F0B3E9A42}}
AppName={#MyAppName}
AppVersion={#VERSION}
AppPublisher=melt-snow
AppPublisherURL=https://github.com/melt-snow/voicevox-enter-player
DefaultDirName={localappdata}\Programs\VoicevoxEnterPlayer
DisableProgramGroupPage=yes
OutputDir=installer-out
OutputBaseFilename=VoicevoxEnterPlayer-Setup-v{#VERSION}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Tasks]
Name: "desktopicon"; Description: "デスクトップにショートカットを作成する"; GroupDescription: "追加タスク:"

[Files]
Source: "bin\Release\net8.0-windows\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} を起動"; Flags: nowait postinstall skipifsilent
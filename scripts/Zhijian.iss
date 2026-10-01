; Zhijian Windows installer.
; Build from the repository root with Inno Setup 6 and pass /DAppVersion=x.y.z.

#ifndef AppVersion
#define AppVersion "0.0.0"
#endif

[Setup]
AppId={{42EB940F-A2FE-44CF-8DC0-D8392A9416A1}
AppName=Zhijian
AppVersion={#AppVersion}
AppPublisher=Dotnet9
AppPublisherURL=https://github.com/dotnet9/Zhijian
AppSupportURL=https://github.com/dotnet9/Zhijian/issues
DefaultDirName={autopf}\Zhijian
DefaultGroupName=Zhijian
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=Zhijian-v{#AppVersion}-win-x64-setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
ChangesAssociations=no
CloseApplications=yes
RestartApplications=yes
CloseApplicationsFilter=Zhijian.exe
UninstallDisplayIcon={app}\Zhijian.exe
WizardStyle=modern

[Languages]
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Zhijian"; Filename: "{app}\Zhijian.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Zhijian"; Filename: "{app}\Zhijian.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Zhijian.exe"; Description: "Launch Zhijian"; Flags: nowait postinstall skipifsilent

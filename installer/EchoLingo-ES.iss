#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

[Setup]
AppId={{91AE9D2E-C9B8-46D9-8D90-3136520D15B1}
AppName=EchoLingo ES
AppVersion={#AppVersion}
AppPublisher=YisusCraiz
AppPublisherURL=https://github.com/YisusCraiz/EchoLingo-ES
DefaultDirName={localappdata}\Programs\EchoLingo ES
DefaultGroupName=EchoLingo ES
UninstallDisplayIcon={app}\EchoLingo-ES.exe
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\artifacts
OutputBaseFilename=EchoLingo-ES-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\EchoLingo ES"; Filename: "{app}\EchoLingo-ES.exe"
Name: "{autodesktop}\EchoLingo ES"; Filename: "{app}\EchoLingo-ES.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\EchoLingo-ES.exe"; Description: "Abrir EchoLingo ES"; Flags: postinstall nowait skipifsilent

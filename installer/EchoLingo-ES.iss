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
Name: "installpython"; Description: "Instalar Python 3.13 (recomendado para la traducción local)"; GroupDescription: "Componentes opcionales:"; Flags: checkedonce

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "python-runtime\python-3.13.16-amd64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Tasks: installpython

[Icons]
Name: "{group}\EchoLingo ES"; Filename: "{app}\EchoLingo-ES.exe"
Name: "{autodesktop}\EchoLingo ES"; Filename: "{app}\EchoLingo-ES.exe"; Tasks: desktopicon

[Run]
Filename: "{tmp}\python-3.13.16-amd64.exe"; Parameters: "/quiet InstallAllUsers=0 PrependPath=1 Include_launcher=1 InstallLauncherAllUsers=0 Include_test=0"; StatusMsg: "Instalando Python 3.13 para el usuario actual..."; Flags: runhidden waituntilterminated; Tasks: installpython
Filename: "{app}\EchoLingo-ES.exe"; Description: "Abrir EchoLingo ES"; Flags: postinstall nowait skipifsilent

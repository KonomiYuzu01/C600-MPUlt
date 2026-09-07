#ifndef BundleDir
  #error BundleDir must be the verified portable application directory.
#endif
#ifndef InstallerOutput
  #error InstallerOutput must be an output directory.
#endif

[Setup]
AppId={{6C24679C-714D-487A-95B1-8F0BC6C65D28}
AppName=C600 Studio
AppVersion=0.2.4
AppVerName=C600 Studio 0.2.4
AppPublisher=C600 Studio contributors
AppPublisherURL=https://github.com/KonomiYuzu01/C600-MPUlt
AppSupportURL=https://github.com/KonomiYuzu01/C600-MPUlt/issues
DefaultDirName={localappdata}\Programs\C600Studio
DefaultGroupName=C600 Studio
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#InstallerOutput}
OutputBaseFilename=C600Studio-0.2.4-Setup
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
UninstallDisplayIcon={app}\C600Studio.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "{#BundleDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\C600 Studio"; Filename: "{app}\C600Studio.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\C600Studio.exe"; Description: "Launch C600 Studio"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

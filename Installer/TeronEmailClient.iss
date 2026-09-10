; Inno Setup script for Teron Email Client.
;
; Normally you don't need to run this directly: publishing the win-x64 or win-x86 profile
; (via "dotnet publish -p:PublishProfile=win-x64" or Visual Studio's Publish dialog) builds
; this automatically as a post-publish MSBuild step - see the BuildInnoSetupInstaller target
; in TeronEmailClient.csproj. The lines below are only needed to run it manually:
;   dotnet publish -p:PublishProfile=win-x64 -c Release
;   dotnet publish -p:PublishProfile=win-x86 -c Release
;   ISCC TeronEmailClient.iss              (defaults to x64)
;   ISCC /DArch=x86 TeronEmailClient.iss    (x86 build)
;
; Output goes to Build\InstallerPackage\TeronEmailClientSetup-<arch>.exe

#ifndef Arch
  #define Arch "x64"
#endif

#ifndef MyAppVersion
  #define MyAppVersion "2.2.0"
#endif

#define MyAppName "Teron Email Client"
#define MyAppPublisher "Teronverse"
#define MyAppExeName "TeronEmailClient.exe"
#define MyPublishDir "..\Build\Publish\TeronEmailClient\win-" + Arch

[Setup]
AppId={{7D04F7B9-39C6-46B1-968D-1EB5C6EE21D8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\TeronEmailClient
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE.txt
SetupIconFile=..\Resources\email.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=..\Build\InstallerPackage
OutputBaseFilename=TeronEmailClientSetup-{#Arch}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed={#(Arch == "x64" ? "x64compatible" : "x86compatible")}
ArchitecturesInstallIn64BitMode={#(Arch == "x64" ? "x64compatible" : "")}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

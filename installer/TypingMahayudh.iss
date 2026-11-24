; Inno Setup 6 script. Build with:  iscc installer\TypingMahayudh.iss   (after build.bat has produced publish\)
#define AppName "Typing Mahayudh"
#define AppVer "1.0.0"

[Setup]
AppId={{B7C1D3A2-5E1F-4C2B-9A55-7D0F3E9A11C4}
AppName={#AppName}
AppVersion={#AppVer}
AppPublisher=Typing Mahayudh
DefaultDirName={pf}\TypingMahayudh
DefaultGroupName={#AppName}
OutputDir=..\dist
OutputBaseFilename=TypingMahayudh_Setup_{#AppVer}
Compression=lzma2
SolidCompression=yes
MinVersion=6.1sp1
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\TypingMahayudh.exe
WizardStyle=modern
DisableProgramGroupPage=yes

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; Flags: checkedonce

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Optional: put vcredist_x86.exe / vcredist_x64.exe (Visual C++ 2013 Redistributable) in installer\redist\ to bundle them
Source: "redist\vcredist_x86.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall skipifsourcedoesntexist; Check: not Is64BitInstallMode
Source: "redist\vcredist_x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall skipifsourcedoesntexist; Check: Is64BitInstallMode

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\TypingMahayudh.exe"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\TypingMahayudh.exe"; Tasks: desktopicon

[Run]
Filename: "{tmp}\vcredist_x86.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Visual C++ runtime..."; Flags: skipifdoesntexist; Check: not Is64BitInstallMode
Filename: "{tmp}\vcredist_x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing Visual C++ runtime..."; Flags: skipifdoesntexist; Check: Is64BitInstallMode
Filename: "{app}\TypingMahayudh.exe"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
// .NET Framework 4.8 is Release >= 528040
function InitializeSetup(): Boolean;
var Rel: Cardinal;
begin
  Result := True;
  if not RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Rel) or (Rel < 528040) then
  begin
    MsgBox('Typing Mahayudh needs Microsoft .NET Framework 4.8.' + #13#10 + #13#10 +
           'Install it first (on Windows 7 SP1 it also needs the Windows 7 SHA-2 update KB4474419), then run this setup again.', mbError, MB_OK);
    Result := False;
  end;
end;

// Progress data lives in %APPDATA%\TypingMahayudh and is deliberately NOT removed on uninstall.

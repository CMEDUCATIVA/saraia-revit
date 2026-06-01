; ============================================
; SaraIA Revit - Inno Setup Installer Script
; ============================================

#define MyAppName "SaraIA Revit"
#ifndef MyAppVersion
  #define MyAppVersion "1.1.0"
#endif
#define MyAppPublisher "CMEDUCATIVA"
#define MyAppURL "https://github.com/CMEDUCATIVA/saraia-revit"
#ifndef MyBuildId
  #define MyBuildId "local"
#endif

[Setup]
AppId={{53A9F495-1F38-4B5E-9F69-5A9A1A000001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=Output
OutputBaseFilename=SaraIA_Revit_v{#MyAppVersion}_{#MyBuildId}_Setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
LicenseFile=..\LICENSE
SetupIconFile=Assets\Icons\SaraIA-icon.ico
UninstallDisplayIcon={app}\SaraIA-icon.ico
WizardImageFile=Assets\Installer\SaraIA-WizardImage.bmp
WizardSmallImageFile=Assets\Installer\SaraIA-WizardSmallImage.bmp
PrivilegesRequired=admin
DisableProgramGroupPage=yes
DisableReadyMemo=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Messages]
WelcomeLabel1=Bienvenido al instalador de [name]
WelcomeLabel2=Este asistente instalará [name/ver] en tu equipo.%n%nSaraIA es un asistente de inteligencia artificial para Autodesk Revit. Permite automatizar tareas, generar código C#, consultar el modelo y trabajar con proveedores como OpenAI/Codex, Claude, Gemini, DeepSeek y modelos locales compatibles.
FinishedHeadingLabel=SaraIA Revit se instaló correctamente
FinishedLabel=SaraIA Revit ya está instalado.%n%nAbre Autodesk Revit y busca la pestaña SaraIA en la cinta superior. Desde Ajustes podrás configurar tus claves de OpenAI, Claude, Gemini, DeepSeek o modelos locales.

[Files]
; Current payload: Revit 2026. Future versions can be enabled by adding their compiled folders.
Source: "bin\R2026\net8.0-windows\*"; DestDir: "{app}\2026"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist
Source: "redist\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
Source: "Assets\Icons\SaraIA-icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "Assets\Icons\saraia-icon.svg"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Instalando Microsoft Edge WebView2 Runtime..."; Check: NeedsWebView2Runtime; Flags: waituntilterminated

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2022\SaraIA.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2023\SaraIA.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2024\SaraIA.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2025\SaraIA.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2026\SaraIA.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2027\SaraIA.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2022\Bibim.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2023\Bibim.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2024\Bibim.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2025\Bibim.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2026\Bibim.Core.addin"
Type: files; Name: "{commonappdata}\Autodesk\Revit\Addins\2027\Bibim.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2022\SaraIA.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2023\SaraIA.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2024\SaraIA.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2025\SaraIA.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2026\SaraIA.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2027\SaraIA.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2022\Bibim.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2023\Bibim.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2024\Bibim.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2025\Bibim.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2026\Bibim.Core.addin"
Type: files; Name: "{userappdata}\Autodesk\Revit\Addins\2027\Bibim.Core.addin"

[Code]
var
  VersionPage: TInputOptionWizardPage;
  CleanPreviousInstall: Boolean;

function IsRevitRunning: Boolean;
var
  ResultCode: Integer;
begin
  Result := False;
  if Exec(
    ExpandConstant('{cmd}'),
    '/c tasklist /FI "IMAGENAME eq Revit.exe" | find /I "Revit.exe" >nul',
    '',
    SW_HIDE,
    ewWaitUntilTerminated,
    ResultCode) then
  begin
    Result := (ResultCode = 0);
  end;
end;

function RevitInstallExists(Year: string): Boolean;
begin
  Result := DirExists('C:\Program Files\Autodesk\Revit ' + Year);
end;

function AddinPath(Year: string; FileName: string): string;
begin
  Result := ExpandConstant('{commonappdata}\Autodesk\Revit\Addins\' + Year + '\' + FileName);
end;

function UserAddinPath(Year: string; FileName: string): string;
begin
  Result := ExpandConstant('{userappdata}\Autodesk\Revit\Addins\' + Year + '\' + FileName);
end;

function PreviousInstallExists: Boolean;
begin
  Result :=
    DirExists(ExpandConstant('{autopf}\SaraIA Revit')) or
    DirExists(ExpandConstant('{autopf}\Bibim')) or
    FileExists(AddinPath('2022', 'SaraIA.Core.addin')) or
    FileExists(AddinPath('2023', 'SaraIA.Core.addin')) or
    FileExists(AddinPath('2024', 'SaraIA.Core.addin')) or
    FileExists(AddinPath('2025', 'SaraIA.Core.addin')) or
    FileExists(AddinPath('2026', 'SaraIA.Core.addin')) or
    FileExists(AddinPath('2027', 'SaraIA.Core.addin')) or
    FileExists(AddinPath('2022', 'Bibim.Core.addin')) or
    FileExists(AddinPath('2023', 'Bibim.Core.addin')) or
    FileExists(AddinPath('2024', 'Bibim.Core.addin')) or
    FileExists(AddinPath('2025', 'Bibim.Core.addin')) or
    FileExists(AddinPath('2026', 'Bibim.Core.addin')) or
    FileExists(AddinPath('2027', 'Bibim.Core.addin')) or
    FileExists(UserAddinPath('2022', 'SaraIA.Core.addin')) or
    FileExists(UserAddinPath('2023', 'SaraIA.Core.addin')) or
    FileExists(UserAddinPath('2024', 'SaraIA.Core.addin')) or
    FileExists(UserAddinPath('2025', 'SaraIA.Core.addin')) or
    FileExists(UserAddinPath('2026', 'SaraIA.Core.addin')) or
    FileExists(UserAddinPath('2027', 'SaraIA.Core.addin')) or
    FileExists(UserAddinPath('2022', 'Bibim.Core.addin')) or
    FileExists(UserAddinPath('2023', 'Bibim.Core.addin')) or
    FileExists(UserAddinPath('2024', 'Bibim.Core.addin')) or
    FileExists(UserAddinPath('2025', 'Bibim.Core.addin')) or
    FileExists(UserAddinPath('2026', 'Bibim.Core.addin')) or
    FileExists(UserAddinPath('2027', 'Bibim.Core.addin'));
end;

procedure DeleteAddinIfExists(Year: string; FileName: string);
var
  Path: string;
begin
  Path := AddinPath(Year, FileName);
  if FileExists(Path) then
    DeleteFile(Path);

  Path := UserAddinPath(Year, FileName);
  if FileExists(Path) then
    DeleteFile(Path);
end;

procedure CleanupPreviousInstall;
begin
  DeleteAddinIfExists('2022', 'SaraIA.Core.addin');
  DeleteAddinIfExists('2023', 'SaraIA.Core.addin');
  DeleteAddinIfExists('2024', 'SaraIA.Core.addin');
  DeleteAddinIfExists('2025', 'SaraIA.Core.addin');
  DeleteAddinIfExists('2026', 'SaraIA.Core.addin');
  DeleteAddinIfExists('2027', 'SaraIA.Core.addin');
  DeleteAddinIfExists('2022', 'Bibim.Core.addin');
  DeleteAddinIfExists('2023', 'Bibim.Core.addin');
  DeleteAddinIfExists('2024', 'Bibim.Core.addin');
  DeleteAddinIfExists('2025', 'Bibim.Core.addin');
  DeleteAddinIfExists('2026', 'Bibim.Core.addin');
  DeleteAddinIfExists('2027', 'Bibim.Core.addin');

  if DirExists(ExpandConstant('{autopf}\SaraIA Revit')) then
    DelTree(ExpandConstant('{autopf}\SaraIA Revit'), True, True, True);

  if DirExists(ExpandConstant('{autopf}\Bibim')) then
    DelTree(ExpandConstant('{autopf}\Bibim'), True, True, True);
end;

function InitializeSetup: Boolean;
var
  Answer: Integer;
begin
  Result := True;
  CleanPreviousInstall := False;

  if IsRevitRunning then
  begin
    MsgBox(
      'Autodesk Revit está abierto.' + #13#10 + #13#10 +
      'Cierra Revit antes de instalar SaraIA para evitar archivos bloqueados.',
      mbError,
      MB_OK);
    Result := False;
    exit;
  end;

  if PreviousInstallExists then
  begin
    Answer := MsgBox(
      'Se encontró una instalación previa de SaraIA Revit o Bibim.' + #13#10 + #13#10 +
      'Se recomienda limpiar la versión anterior antes de continuar para evitar manifiestos duplicados o DLL antiguas.' + #13#10 + #13#10 +
      'Sí: limpiar versión anterior y continuar.' + #13#10 +
      'No: continuar sin limpiar.' + #13#10 +
      'Cancelar: salir del instalador.',
      mbConfirmation,
      MB_YESNOCANCEL);

    if Answer = IDCANCEL then
    begin
      Result := False;
      exit;
    end;

    CleanPreviousInstall := (Answer = IDYES);
    if CleanPreviousInstall then
      CleanupPreviousInstall;
  end;
end;

function NeedsWebView2Runtime: Boolean;
var
  Version: string;
begin
  Result := not RegQueryStringValue(
    HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
    'pv', Version);

  if Result then
    Result := not RegQueryStringValue(
      HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}',
      'pv', Version);
end;

procedure InitializeWizard;
var
  I: Integer;
begin
  VersionPage := CreateInputOptionPage(
    wpSelectDir,
    'Seleccionar versión de Revit',
    'Elige en qué versión de Autodesk Revit deseas instalar SaraIA.',
    'El instalador solo activará SaraIA en las versiones seleccionadas. Las versiones sin binario disponible aparecen deshabilitadas.',
    True,
    False);

  VersionPage.Add('Revit 2022 - No disponible en este instalador');
  VersionPage.Add('Revit 2023 - No disponible en este instalador');
  VersionPage.Add('Revit 2024 - No disponible en este instalador');
  VersionPage.Add('Revit 2025 - No disponible en este instalador');
  if RevitInstallExists('2026') then
    VersionPage.Add('Revit 2026 - Detectado y disponible')
  else
    VersionPage.Add('Revit 2026 - Disponible, no detectado en este equipo');
  VersionPage.Add('Revit 2027 - No disponible en este instalador');

  for I := 0 to 5 do
    VersionPage.CheckListBox.ItemEnabled[I] := False;

  VersionPage.CheckListBox.ItemEnabled[4] := True;
  VersionPage.Values[4] := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = VersionPage.ID then
  begin
    if not VersionPage.Values[4] then
    begin
      MsgBox('Selecciona al menos una versión disponible de Revit para continuar.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

function UpdateReadyMemo(
  Space: String;
  NewLine: String;
  MemoUserInfoInfo: String;
  MemoDirInfo: String;
  MemoTypeInfo: String;
  MemoComponentsInfo: String;
  MemoGroupInfo: String;
  MemoTasksInfo: String): String;
var
  CleanText: string;
begin
  if CleanPreviousInstall then
    CleanText := 'Sí'
  else
    CleanText := 'No';

  Result :=
    'Aplicación: SaraIA Revit' + NewLine +
    'Versión: {#MyAppVersion}' + NewLine +
    'Editor: {#MyAppPublisher}' + NewLine +
    'Destino: ' + WizardDirValue + NewLine +
    'Versiones seleccionadas: Revit 2026' + NewLine +
    'Limpieza previa: ' + CleanText + NewLine +
    'Manifiesto: SaraIA.Core.addin' + NewLine + NewLine +
    'Al instalar, SaraIA copiará sus archivos, verificará WebView2 y registrará el complemento en Autodesk Revit.';
end;

procedure WriteRevitAddinManifest(Year: string);
var
  ManifestPath: string;
  AssemblyPath: string;
  Content: string;
begin
  ManifestPath := AddinPath(Year, 'SaraIA.Core.addin');
  AssemblyPath := AddBackslash(ExpandConstant('{app}')) + Year + '\Bibim.Core.dll';

  if not FileExists(AssemblyPath) then
    exit;

  ForceDirectories(ExtractFileDir(ManifestPath));

  Content :=
    '<?xml version="1.0" encoding="utf-8"?>' + #13#10 +
    '<RevitAddIns>' + #13#10 +
    '  <AddIn Type="Application">' + #13#10 +
    '    <Name>SaraIA</Name>' + #13#10 +
    '    <Assembly>' + AssemblyPath + '</Assembly>' + #13#10 +
    '    <FullClassName>Bibim.Core.BibimApp</FullClassName>' + #13#10 +
    '    <AddInId>B1B1B1B1-B1B1-B1B1-B1B1-B1B100030001</AddInId>' + #13#10 +
    '    <VendorId>SaraIA</VendorId>' + #13#10 +
    '    <VendorDescription>SaraIA Revit - AI-powered Revit automation</VendorDescription>' + #13#10 +
    '  </AddIn>' + #13#10 +
    '  <AddIn Type="Command">' + #13#10 +
    '    <Name>SaraIA Show Panel</Name>' + #13#10 +
    '    <Assembly>' + AssemblyPath + '</Assembly>' + #13#10 +
    '    <FullClassName>Bibim.Core.BibimShowPanelCommand</FullClassName>' + #13#10 +
    '    <AddInId>B1B1B1B1-B1B1-B1B1-B1B1-B1B100030002</AddInId>' + #13#10 +
    '    <VendorId>SaraIA</VendorId>' + #13#10 +
    '  </AddIn>' + #13#10 +
    '</RevitAddIns>' + #13#10;

  SaveStringToFile(ManifestPath, Content, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if VersionPage.Values[4] then
      WriteRevitAddinManifest('2026');
  end;
end;


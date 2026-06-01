# Guía del Instalador de SaraIA Revit

Esta guía documenta cómo preparar, compilar y generar el instalador de SaraIA Revit con Inno Setup.

## Estado Actual

El instalador actual está preparado para empaquetar Revit 2022 a 2027. La compilación usa paquetes NuGet de API de Revit, por lo que no requiere tener todas las versiones de Revit instaladas en la máquina de build.

Compatibilidad objetivo del proyecto:

| Revit | Framework | Estado local |
|---|---|---|
| 2022 | net48 | Compila con `Nice3point.Revit.Api.*` |
| 2023 | net48 | Compila con `Nice3point.Revit.Api.*` |
| 2024 | net48 | Compila con `Nice3point.Revit.Api.*` |
| 2025 | net8.0-windows | Compila con `Nice3point.Revit.Api.*` |
| 2026 | net8.0-windows | Compila con `Nice3point.Revit.Api.*` |
| 2027 | net10.0-windows | Compila con `Nice3point.Revit.Api.*` |

No se deben generar binarios de una versión usando la API de otra versión de Revit. Cada configuración usa paquetes NuGet `Nice3point.Revit.Api.RevitAPI` y `Nice3point.Revit.Api.RevitAPIUI` con `Version="$(RevitVersion).*"`.

## Archivos Principales

| Archivo | Uso |
|---|---|
| `Bibim.Core/SaraIAInstaller.iss` | Script principal del instalador SaraIA Revit |
| `Bibim.Core/Assets/Icons/saraia-icon.svg` | Logo fuente del branding |
| `Bibim.Core/Assets/Icons/SaraIA-icon.ico` | Icono del instalador y desinstalador |
| `Bibim.Core/Assets/Installer/SaraIA-WizardImage.bmp` | Imagen lateral del asistente de instalación |
| `Bibim.Core/Assets/Installer/SaraIA-WizardSmallImage.bmp` | Imagen pequeña del asistente de instalación |
| `Bibim.Core/redist/MicrosoftEdgeWebview2Setup.exe` | Instalador de WebView2 Runtime |
| `Bibim.Core/Output/` | Carpeta donde Inno Setup genera el `.exe` final |

## Requisitos de la Máquina de Build

Instalar o tener disponible:

- .NET SDK compatible con el proyecto.
- Node.js y npm.
- Inno Setup 6.
- Autodesk Revit o al menos las DLL de API para cada versión que se quiera compilar.

Ubicación típica de Inno Setup:

```powershell
C:\Program Files (x86)\Inno Setup 6\ISCC.exe
```

Ubicación típica de las APIs de Revit:

```text
C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll
C:\Program Files\Autodesk\Revit 2026\RevitAPIUI.dll
```

## Verificar APIs Disponibles

Ejecutar desde la raíz del repositorio:

```powershell
$versions=@('2022','2023','2024','2025','2026','2027')
$result = foreach($v in $versions){
  $base="C:\Program Files\Autodesk\Revit $v"
  [pscustomobject]@{
    Version=$v
    RevitAPI=(Test-Path (Join-Path $base 'RevitAPI.dll'))
    RevitAPIUI=(Test-Path (Join-Path $base 'RevitAPIUI.dll'))
    Path=$base
  }
}
$result | Format-Table -AutoSize
```

Esta verificación sirve para saber qué versiones de Revit existen en la máquina de prueba. Para compilar, el proyecto usa paquetes NuGet de API de Revit y no depende de que todas las versiones estén instaladas localmente.

## Guía de DLLs

Antes de generar el instalador, compila las DLLs por versión siguiendo [GUIA-COPILAR-DLL.md](GUIA-COPILAR-DLL.md).

El instalador empaqueta las salidas de Bibim.Core/bin/R2022 a Bibim.Core/bin/R2027.

## Compilar Frontend

Desde la raíz del repositorio:

```powershell
Set-Location Bibim.Core\frontend
npm.cmd run build
Set-Location ..\..
```

Esto genera el panel web en:

```text
Bibim.Core/wwwroot
```

## Compilar Backend por Versión

### Revit 2026

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2026 -p:TargetFramework=net8.0-windows
```

Salida esperada:

```text
Bibim.Core/bin/R2026/net8.0-windows/Bibim.Core.dll
```

### Revit 2022-2024

Requieren .NET Framework 4.8 y las APIs de cada versión:

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2022 -p:TargetFramework=net48
dotnet build Bibim.Core\Bibim.Core.csproj -c R2023 -p:TargetFramework=net48
dotnet build Bibim.Core\Bibim.Core.csproj -c R2024 -p:TargetFramework=net48
```

### Revit 2025

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2025 -p:TargetFramework=net8.0-windows
```

### Revit 2027

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2027 -p:TargetFramework=net10.0-windows
```

## Generar el Instalador

Desde la raíz del repositorio:

```powershell
& 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' /DMyAppVersion=1.1.0 /DMyBuildId=local Bibim.Core\SaraIAInstaller.iss
```

Salida esperada:

```text
Bibim.Core/Output/SaraIA_Revit_v1.1.0_local_Setup.exe
```

## Flujo del Instalador

El instalador de SaraIA Revit está diseñado con este flujo:

1. Verifica si Autodesk Revit está abierto.
2. Si Revit está abierto, bloquea la instalación y pide cerrarlo.
3. Detecta instalaciones previas de SaraIA Revit o Bibim.
4. Pregunta si se desea limpiar la instalación anterior.
5. Muestra bienvenida con branding SaraIA.
6. Muestra licencia.
7. Permite elegir carpeta de instalación.
8. Permite elegir versión de Revit.
9. Muestra resumen antes de instalar.
10. Copia archivos y dependencias.
11. Instala WebView2 Runtime si falta.
12. Crea el manifiesto `SaraIA.Core.addin`.
13. Finaliza con instrucciones para abrir SaraIA dentro de Revit.

## Selección de Versiones

El instalador muestra las versiones de Revit 2022 a 2027.

En la versión actual, Revit 2022, 2023, 2024, 2025, 2026 y 2027 aparecen habilitados. Las versiones detectadas en el equipo se marcan por defecto; el usuario puede seleccionar manualmente otras versiones si desea preparar la instalación antes de instalar Revit.

## Limpieza de Instalaciones Previas

El instalador puede limpiar:

- Manifiestos `SaraIA.Core.addin`.
- Manifiestos antiguos `Bibim.Core.addin`.
- Rutas de add-ins en `ProgramData`.
- Rutas de add-ins en `%APPDATA%`.
- Carpeta antigua `C:\Program Files\Bibim`.
- Carpeta anterior `C:\Program Files\SaraIA Revit`.

No debe borrar automáticamente claves API o configuración personal del usuario salvo que se agregue una opción explícita para eso.

## Manifiesto Add-in

Para cada versión seleccionada, el instalador crea el manifiesto correspondiente. Por ejemplo, para Revit 2026 crea:

```text
C:\ProgramData\Autodesk\Revit\Addins\2026\SaraIA.Core.addin
```

El manifiesto apunta a:

```text
C:\Program Files\SaraIA Revit\2026\Bibim.Core.dll
```

El nombre interno del ensamblado sigue siendo `Bibim.Core.dll` por compatibilidad del proyecto, pero el producto visible para el usuario es SaraIA Revit.

## Branding

El instalador debe usar siempre el branding SaraIA:

- Nombre visible: `SaraIA Revit`.
- Publisher: `CMEDUCATIVA`.
- URL: `https://github.com/CMEDUCATIVA/saraia-revit`.
- Icono: `SaraIA-icon.ico`.
- Imágenes del asistente generadas desde `saraia-icon.svg`.

Si se cambia el SVG, regenerar los BMP del instalador antes de compilar el `.exe`.

## Regenerar Imágenes BMP desde el Logo

PowerShell usado para generar las imágenes del asistente:

```powershell
New-Item -ItemType Directory -Force Bibim.Core\Assets\Installer | Out-Null
Add-Type -AssemblyName System.Drawing
$src='Bibim.Core\Assets\Icons\saraia-icon.png'
$img=[System.Drawing.Image]::FromFile((Resolve-Path $src))
function New-BrandedBmp($path,$w,$h,$logoSize){
  $bmp=New-Object System.Drawing.Bitmap $w,$h
  $g=[System.Drawing.Graphics]::FromImage($bmp)
  $g.Clear([System.Drawing.Color]::White)
  $g.SmoothingMode=[System.Drawing.Drawing2D.SmoothingMode]::HighQuality
  $g.InterpolationMode=[System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $x=[int](($w-$logoSize)/2)
  $y=[int](($h-$logoSize)/2)
  $g.DrawImage($script:img,$x,$y,$logoSize,$logoSize)
  $pen=New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(230,230,230)), 1
  $g.DrawRectangle($pen,0,0,$w-1,$h-1)
  $pen.Dispose(); $g.Dispose()
  $bmp.Save((Join-Path (Get-Location) $path),[System.Drawing.Imaging.ImageFormat]::Bmp)
  $bmp.Dispose()
}
New-BrandedBmp 'Bibim.Core\Assets\Installer\SaraIA-WizardImage.bmp' 164 314 132
New-BrandedBmp 'Bibim.Core\Assets\Installer\SaraIA-WizardSmallImage.bmp' 55 55 43
$img.Dispose()
```

## Validación Manual

Después de generar el instalador:

1. Cerrar Revit.
2. Ejecutar `SaraIA_Revit_v1.1.0_local_Setup.exe` como administrador.
3. Elegir limpiar instalación anterior si el instalador lo detecta.
4. Seleccionar Revit 2026.
5. Finalizar instalación.
6. Confirmar que existe:

```text
C:\ProgramData\Autodesk\Revit\Addins\2026\SaraIA.Core.addin
```

7. Abrir Revit 2026.
8. Verificar que aparece la pestaña SaraIA.
9. Abrir el panel.
10. Confirmar que responde en español.
11. Confirmar que se pueden configurar proveedores IA como OpenAI/Codex, Claude, Gemini y DeepSeek.

## Advertencia de Inno Setup

El compilador puede mostrar una advertencia porque el instalador requiere permisos de administrador y también limpia rutas en `%APPDATA%`.

Esto es intencional para evitar duplicados con instalaciones manuales previas. La instalación oficial se escribe en `ProgramData` y `Program Files`.

## Publicación

El `.exe` generado en `Bibim.Core/Output/` está ignorado por Git. Para publicarlo, subirlo como artefacto de release en GitHub, no como archivo versionado dentro del repositorio.



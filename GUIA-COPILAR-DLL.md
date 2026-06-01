# Guía para Compilar DLLs de SaraIA Revit

Esta guía explica cómo generar las DLLs de SaraIA para Revit 2022 a 2027.

## Objetivo

Crear una DLL independiente para cada versión de Autodesk Revit soportada:

| Revit | Framework | Salida esperada |
|---|---|---|
| 2022 | `net48` | `Bibim.Core/bin/R2022/net48/Bibim.Core.dll` |
| 2023 | `net48` | `Bibim.Core/bin/R2023/net48/Bibim.Core.dll` |
| 2024 | `net48` | `Bibim.Core/bin/R2024/net48/Bibim.Core.dll` |
| 2025 | `net8.0-windows` | `Bibim.Core/bin/R2025/net8.0-windows/Bibim.Core.dll` |
| 2026 | `net8.0-windows` | `Bibim.Core/bin/R2026/net8.0-windows/Bibim.Core.dll` |
| 2027 | `net10.0-windows` | `Bibim.Core/bin/R2027/net10.0-windows/Bibim.Core.dll` |

## Enfoque de Compilación

SaraIA compila contra paquetes NuGet de API de Revit:

```xml
<PackageReference Include="Nice3point.Revit.Api.RevitAPI" Version="$(RevitVersion).*" />
<PackageReference Include="Nice3point.Revit.Api.RevitAPIUI" Version="$(RevitVersion).*" />
```

Esto permite compilar las DLLs de Revit 2022 a 2027 sin tener todas las versiones de Revit instaladas en la máquina de build.

Revit solo debe estar instalado para pruebas reales de carga y ejecución.

## Requisitos

Instalar o verificar:

- .NET SDK 9 o superior.
- .NET SDK 10 para compilar Revit 2027.
- Node.js y npm.
- Acceso a NuGet.org.
- Repositorio SaraIA actualizado.

Verificar SDKs:

```powershell
dotnet --list-sdks
```

Verificar Node/npm:

```powershell
node --version
npm.cmd --version
```

Verificar fuentes NuGet:

```powershell
dotnet nuget list source
```

Debe aparecer `nuget.org` habilitado.

## Limpiar Estado Anterior

Opcional, pero recomendado antes de generar un paquete final:

```powershell
dotnet clean Bibim.Core\Bibim.Core.csproj -c R2022 -f net48
dotnet clean Bibim.Core\Bibim.Core.csproj -c R2023 -f net48
dotnet clean Bibim.Core\Bibim.Core.csproj -c R2024 -f net48
dotnet clean Bibim.Core\Bibim.Core.csproj -c R2025 -f net8.0-windows
dotnet clean Bibim.Core\Bibim.Core.csproj -c R2026 -f net8.0-windows
dotnet clean Bibim.Core\Bibim.Core.csproj -c R2027 -f net10.0-windows
```

## Compilar Frontend

Antes de compilar las DLLs, generar el panel web:

```powershell
Set-Location Bibim.Core\frontend
npm.cmd run build
Set-Location ..\..
```

Esto actualiza:

```text
Bibim.Core/wwwroot
```

El contenido de `wwwroot` se copia dentro de cada salida de build.

## Compilar DLLs Individualmente

Ejecutar desde la raíz del repositorio.

### Revit 2022

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2022 -f net48
```

### Revit 2023

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2023 -f net48
```

### Revit 2024

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2024 -f net48
```

### Revit 2025

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2025 -f net8.0-windows
```

### Revit 2026

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2026 -f net8.0-windows
```

### Revit 2027

```powershell
dotnet build Bibim.Core\Bibim.Core.csproj -c R2027 -f net10.0-windows
```

## Compilar Todas las Versiones

Script recomendado:

```powershell
$builds = @(
  @{Version='2022'; Config='R2022'; Framework='net48'},
  @{Version='2023'; Config='R2023'; Framework='net48'},
  @{Version='2024'; Config='R2024'; Framework='net48'},
  @{Version='2025'; Config='R2025'; Framework='net8.0-windows'},
  @{Version='2026'; Config='R2026'; Framework='net8.0-windows'},
  @{Version='2027'; Config='R2027'; Framework='net10.0-windows'}
)

$results = @()
foreach($b in $builds){
  $version = $b.Version
  $config = $b.Config
  $framework = $b.Framework

  Write-Host "===== Building Revit $version / $framework ====="
  & dotnet build Bibim.Core\Bibim.Core.csproj -c $config -f $framework
  $code = $LASTEXITCODE

  $dll = "Bibim.Core\bin\$config\$framework\Bibim.Core.dll"
  $results += [pscustomobject]@{
    Revit = $version
    Framework = $framework
    ExitCode = $code
    DllExists = Test-Path $dll
    DllPath = $dll
  }
}

$results | Format-Table -AutoSize
```

Todos los `ExitCode` deben ser `0` y todos los `DllExists` deben estar en `True`.

## Validar DLLs Generadas

```powershell
$paths = @(
  'Bibim.Core\bin\R2022\net48\Bibim.Core.dll',
  'Bibim.Core\bin\R2023\net48\Bibim.Core.dll',
  'Bibim.Core\bin\R2024\net48\Bibim.Core.dll',
  'Bibim.Core\bin\R2025\net8.0-windows\Bibim.Core.dll',
  'Bibim.Core\bin\R2026\net8.0-windows\Bibim.Core.dll',
  'Bibim.Core\bin\R2027\net10.0-windows\Bibim.Core.dll'
)

foreach($p in $paths){
  Get-Item $p | Select-Object FullName,Length,LastWriteTime
}
```

## Ubicación de las DLLs

Después de compilar, las DLLs quedan en:

```text
Bibim.Core/bin/R2022/net48/Bibim.Core.dll
Bibim.Core/bin/R2023/net48/Bibim.Core.dll
Bibim.Core/bin/R2024/net48/Bibim.Core.dll
Bibim.Core/bin/R2025/net8.0-windows/Bibim.Core.dll
Bibim.Core/bin/R2026/net8.0-windows/Bibim.Core.dll
Bibim.Core/bin/R2027/net10.0-windows/Bibim.Core.dll
```

Estas carpetas completas son las que el instalador empaqueta por versión.

## Relación con el Instalador

El instalador `Bibim.Core/SaraIAInstaller.iss` espera estas rutas:

```text
bin/R2022/net48/*
bin/R2023/net48/*
bin/R2024/net48/*
bin/R2025/net8.0-windows/*
bin/R2026/net8.0-windows/*
bin/R2027/net10.0-windows/*
```

Si una carpeta no existe o está incompleta, esa versión no quedará correctamente incluida en el instalador.

## Pruebas Reales

Compilar con NuGet valida que el código es compatible con la API de cada versión, pero no reemplaza una prueba dentro de Revit.

Para validar completamente:

1. Instalar SaraIA para la versión deseada.
2. Abrir esa versión de Revit.
3. Confirmar que carga la pestaña SaraIA.
4. Abrir el panel.
5. Enviar una solicitud simple.
6. Revisar logs si Revit bloquea la carga.

## Errores Frecuentes

### `NU1202`

Significa que se está intentando restaurar un framework incorrecto para una versión de Revit.

Ejemplo: Revit 2025/2026 no deben restaurar `net48`; deben usar `net8.0-windows`.

### `NETSDK1013`

Puede ocurrir si PowerShell expande mal parámetros como `-p:TargetFramework=$b.Framework`.

Usa:

```powershell
-f $framework
```

o asigna primero la variable:

```powershell
$framework = $b.Framework
dotnet build Bibim.Core\Bibim.Core.csproj -c $config -p:TargetFramework=$framework
```

### `CS0246 Autodesk not found`

Indica que no se resolvieron las referencias de API de Revit.

Verificar:

```powershell
dotnet restore Bibim.Core\Bibim.Core.csproj -c R2026
```

Y confirmar que NuGet.org esté habilitado.

## No Versionar Binarios

Las carpetas `bin/`, `obj/` y `Output/` están ignoradas por Git.

No subir DLLs ni instaladores directamente al repositorio. Para distribución, usar GitHub Releases.

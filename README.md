# SaraIA Revit

SaraIA Revit es un complemento para Autodesk Revit que convierte instrucciones en lenguaje natural en codigo C# ejecutable dentro de Revit.

La app funciona con claves propias del usuario (**BYOK: Bring Your Own Key**). No requiere cuentas adicionales, suscripciones propias de SaraIA ni telemetria obligatoria: conectas tu proveedor de IA y eliges el modelo que quieres usar.

## Novedades principales

- Interfaz principal en español.
- Nombre actualizado a **SaraIA Revit**.
- Soporte para **DeepSeek** como proveedor independiente.
- Soporte para Anthropic Claude, OpenAI, Google Gemini y LLM local/autohospedado compatible con OpenAI.
- Selector de modelos desde el panel de configuracion.
- Guardado separado de API keys por proveedor.

## Que puede hacer

- Ejecutar tareas de Revit a partir de texto natural, por ejemplo: "Selecciona todas las puertas del Nivel 1 y cambia su comentario".
- Generar, revisar y ejecutar codigo C# dentro de Revit.
- Dividir tareas complejas en pasos.
- Hacer preguntas de aclaracion antes de aplicar cambios.
- Validar codigo con analizadores Roslyn antes de ejecutarlo.
- Mostrar una vista previa o modo de prueba antes de modificar elementos.
- Guardar fragmentos de codigo reutilizables en una biblioteca.
- Permitir deshacer cambios aplicados.

## Proveedores de IA soportados

| Proveedor | Estado | Donde obtener la clave | Variable de entorno |
|---|---|---|---|
| Anthropic Claude | Soportado | https://console.anthropic.com | `ANTHROPIC_API_KEY` o `CLAUDE_API_KEY` |
| OpenAI | Soportado | https://platform.openai.com/api-keys | `OPENAI_API_KEY` |
| DeepSeek | Soportado | https://platform.deepseek.com | `DEEPSEEK_API_KEY` |
| Google Gemini | Soportado | https://aistudio.google.com/apikey | `GEMINI_API_KEY` |
| LLM local/autohospedado | Soportado | Ollama, LM Studio, vLLM, llama.cpp u otro endpoint compatible | Configurable desde la app |

## Modelos disponibles

La lista exacta puede cambiar segun la configuracion del proyecto, pero la app ya incluye rutas para:

| Modelo | Proveedor |
|---|---|
| Claude Sonnet | Anthropic |
| Claude Opus | Anthropic |
| GPT | OpenAI |
| DeepSeek V4 Flash | DeepSeek |
| DeepSeek V4 Pro | DeepSeek |
| DeepSeek Chat | DeepSeek |
| Gemini Pro | Google Gemini |
| Local | Servidor compatible con OpenAI |

Los modelos aparecen bloqueados hasta que guardes la API key del proveedor correspondiente.

## Versiones de Revit soportadas

| Revit | .NET | Configuracion de build |
|---|---|---|
| 2022 | .NET Framework 4.8 | `R2022` |
| 2023 | .NET Framework 4.8 | `R2023` |
| 2024 | .NET Framework 4.8 | `R2024` |
| 2025 | .NET 8.0 | `R2025` |
| 2026 | .NET 8.0 | `R2026` |
| 2027 | .NET 10.0 | `R2027` |

## Inicio rapido

### 1. Consigue una API key

Elige uno o mas proveedores. Puedes guardar varias claves y cambiar de modelo desde configuracion.

Recomendacion practica:

- Usa DeepSeek si quieres una opcion economica y rapida.
- Usa Claude para tareas complejas y razonamiento cuidadoso.
- Usa OpenAI si ya trabajas con su ecosistema.
- Usa Gemini si necesitas contexto amplio.
- Usa LLM local si quieres ejecutar modelos propios o privados.

### 2. Instala el complemento

Descarga el instalador desde la seccion de releases del repositorio y ejecutalo. El instalador registra el complemento para las versiones de Revit detectadas.

### 3. Configura las claves

Dentro de Revit:

1. Abre la pestaña de SaraIA.
2. Entra al panel de configuracion.
3. Pega la API key del proveedor que quieras usar.
4. Guarda la clave.
5. Selecciona un modelo activo.
6. Escribe una tarea en lenguaje natural.

## Configuracion manual

La configuracion se guarda en:

```text
%AppData%\Bibim\rag_config.json
```

Ejemplo:

```json
{
  "claude_model": "deepseek-v4-flash",
  "api_keys": {
    "anthropic_api_key": "sk-ant-api03-...",
    "openai_api_key": "sk-...",
    "deepseek_api_key": "sk-...",
    "gemini_api_key": "AIzaSy..."
  }
}
```

Nota: el campo `claude_model` conserva ese nombre por compatibilidad historica, pero puede guardar cualquier modelo activo, incluido DeepSeek, OpenAI, Gemini o local.

Tambien puedes usar variables de entorno:

```powershell
$env:ANTHROPIC_API_KEY = "sk-ant-api03-..."
$env:OPENAI_API_KEY = "sk-..."
$env:DEEPSEEK_API_KEY = "sk-..."
$env:GEMINI_API_KEY = "AIzaSy..."
```

## LLM local/autohospedado

SaraIA puede conectarse a servidores compatibles con la API de OpenAI, por ejemplo:

- Ollama
- LM Studio
- vLLM
- llama.cpp
- Endpoints privados detras de proxy corporativo

URL comun para pruebas locales:

```text
http://localhost:11434/v1
```

Para mejores resultados, se recomienda un modelo con soporte de tool calling.

## Compilar desde codigo fuente

Requisitos:

- Visual Studio 2022 o .NET SDK 8.0+
- .NET 10 SDK si vas a compilar para Revit 2027
- Revit instalado en la ruta por defecto: `C:\Program Files\Autodesk\Revit <year>`
- Node.js 20+ para compilar el frontend

Compilar para Revit 2026:

```powershell
dotnet build "Bibim.Core\Bibim.Core.csproj" -c R2026 -p:TargetFramework=net8.0-windows
```

Compilar frontend:

```powershell
cd Bibim.Core\frontend
npm ci
npm.cmd run build
```

Build completo:

```powershell
.\build.ps1
```

Build rapido:

```powershell
.\build.ps1 -SkipFrontend -SkipTests -RevitConfig R2026
```

## Estructura del proyecto

```text
Bibim.Core/
  BibimApp.cs                    Entrada principal IExternalApplication
  BibimDockablePanelProvider.cs  Puente WebView2 entre React y C#
  Common/
    ConfigService.cs             Carga configuracion y guarda claves por proveedor
  Services/
    LlmOrchestrationService.cs   Orquestacion del flujo LLM + herramientas
    Providers/
      AnthropicProvider.cs       Claude
      OpenAIProvider.cs          OpenAI
      DeepSeekProvider.cs        DeepSeek
      GeminiProvider.cs          Google Gemini
      LocalProvider.cs           LLM local compatible con OpenAI
      LlmProviderFactory.cs      Selecciona proveedor segun modelo
    RoslynCompilerService.cs     Compilacion C# en proceso
    RoslynAnalyzerService.cs     Validaciones antes de ejecutar
    LocalRevitRagService.cs      Busqueda local sobre documentacion RevitAPI.xml
  frontend/                      React + Vite + TypeScript
  wwwroot/                       Frontend compilado usado por WebView2
```

## Logs y diagnostico

Si algo falla, revisa:

| Ruta | Contenido |
|---|---|
| `%USERPROFILE%\Bibim_v3_debug.txt` | Log principal con eventos, errores y stack traces |
| `%AppData%\Bibim\rag_config.json` | Configuracion de usuario y modelo seleccionado |
| `%AppData%\Bibim\debug\codegen\YYYYMMDD\` | Prompts, codigo generado y diagnosticos de compilacion |

Antes de compartir logs, revisa que no contengan datos sensibles del proyecto o API keys.

## Seguridad

- No subas `rag_config.json` con claves reales.
- No compartas logs sin revisarlos.
- Usa claves con permisos limitados cuando sea posible.
- Para despliegues empresariales, centraliza la gestion de claves mediante variables de entorno o configuracion controlada.

## Licencia

Apache 2.0. Consulta [LICENSE](LICENSE).

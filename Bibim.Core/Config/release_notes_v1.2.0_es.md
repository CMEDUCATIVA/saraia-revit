# SaraIA v1.2.0

Esta versión abre SaraIA a asistentes externos y hace visible el trabajo mientras se construye el modelo.

## Novedades

### Control API local
SaraIA levanta una API HTTP **solo en loopback** (`127.0.0.1`) para que un asistente o script externo pueda leer el modelo, ejecutar C# dentro de Revit y capturar la vista, sin pasar por el chat.

| Método | Ruta | Qué hace |
|---|---|---|
| GET | `/status` | Revit vivo, documento y vista activos |
| GET | `/context?tag=` | Contexto real del modelo: `@view`, `@selection`, `@levels`, `@worksets`, `@phases`, `@family:`, `@parameters:` |
| GET | `/view/image` | PNG de la vista activa en base64 |
| POST | `/execute` | Compila y ejecuta C# (`dryrun` se revierte, `commit` se aplica) |
| POST | `/undo` | Deshace N pasos |

Autenticación por token, generado en el primer arranque en `%AppData%\Bibim\control_api.json`. Se desactiva con `"enabled": false`.

### Botón «Endpoints»
Nuevo botón en la pestaña SaraIA. Abre la referencia de la API como página web dentro de Revit, con **tu URL y tu token ya puestos**, ejemplos listos para copiar y un botón «Copiar todo para la IA» para pegarlo en un asistente de escritorio. El token aparece desenfocado hasta que se pulsa.

### Ver una simulación sin tocar el modelo
`/execute` con `capture: true` en modo `dryrun` devuelve una imagen del resultado **antes de que se revierta**. Se puede comprobar cómo quedaría un cambio sin que el modelo llegue a modificarse.

### Montaje por etapas
Revit no puede redibujar la pantalla mientras ejecuta código, así que una operación larga parecía colgada. Ahora el código generado se divide en etapas (`// ETAPAS: Cimentación | Muros | Cubierta`) y se aplica una por una: **el modelo se ve construirse** y Revit sigue respondiendo.

- Cada etapa es una transacción, pero **«Deshacer» revierte la operación completa** de un solo gesto.
- Si una ejecución bloquea Revit más de 2,5 s, SaraIA lo avisa.

### Especificación OpenAPI
`openapi.json` en el repositorio describe las cinco rutas en formato estándar (OpenAPI 3.1), importable en Swagger UI o Postman.

## Correcciones
- La API usa ahora los nombres declarados en `// ETAPAS:` para el progreso; antes mostraba «Etapa 2», «Etapa 3».

## Compatibilidad
Revit 2022, 2023, 2024 (.NET Framework 4.8), 2025, 2026 (.NET 8) y 2027 (.NET 10).

## Problemas conocidos
- **Sombras proyectadas**: la API de Revit no permite activarlas por código. Actívalas una vez en la vista (*Opciones de visualización de gráficos → Sombras*) o en una plantilla de vista.
- **Familias**: las puertas, ventanas, vegetación y pilares requieren la biblioteca de contenido de Autodesk (*Insertar → Cargar familia de Autodesk*).
- Probado en vivo solo en Revit 2026. Las demás versiones compilan sin errores pero no se han probado en ejecución.

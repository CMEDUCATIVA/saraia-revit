# SaraIA Revit — Control API local

API HTTP **solo loopback** que expone el plugin dentro de Revit para que un
orquestador externo (una **skill de KUN**, un script, otro agente) pueda
**leer contexto, ejecutar C# y capturar la vista** sin usar el chat del panel.

Modelo mental: **el plugin son las "manos y ojos" dentro de Revit; el cerebro
(planificación, visión, corrección) vive fuera** (tu skill).

## Arranque y descubrimiento

- El servidor arranca automáticamente con Revit (al cargar el add-in).
- Configuración y credenciales: `%AppData%\Bibim\control_api.json`

```json
{
  "enabled": true,
  "port": 8757,
  "token": "generado-automaticamente-en-el-primer-arranque"
}
```

- Base URL: `http://127.0.0.1:8757`
- **Auth:** cabecera `Authorization: Bearer <token>` en cada petición
  (o `?token=<token>` en peticiones GET, útil para abrir la imagen en el navegador).
- Para desactivarla: pon `"enabled": false` y reinicia Revit.

La skill debe **leer `control_api.json`** para obtener `port` y `token`.

## Endpoints

### `GET /status`
Liveness + documento/vista activos.
```json
{
  "ok": true,
  "version": "1.1.0",
  "revitVersion": "2026",
  "document": {
    "hasDocument": true,
    "documentTitle": "Proyecto1",
    "documentPath": "C:\\...\\Proyecto1.rvt",
    "activeView": "Nivel 1",
    "activeViewType": "FloorPlan"
  }
}
```

### `POST /execute`
Compila (Roslyn) y ejecuta C#. Reutiliza la ejecución del panel: dry-run se
envuelve en un `TransactionGroup` con **rollback**; commit **persiste**.

Request:
```json
{ "code": "using (var tx = new Transaction(doc, \"Muro\")) { tx.Start(); /* ... */ tx.Commit(); } return \"ok\";",
  "mode": "dryrun",
  "capture": true,
  "captureWidth": 1600 }
```
- `mode`: `"dryrun"` (por defecto, se revierte) o `"commit"` (se aplica).
- `capture`: si es `true`, la respuesta incluye `imageBase64` con un PNG de la vista
  activa. **En dry-run la imagen se toma con el `TransactionGroup` todavía abierto**,
  así que muestra el resultado *simulado* — y acto seguido el rollback deja el
  documento intacto. Es la forma de **ver un paso sin aplicarlo**.
- `captureWidth`: ancho en píxeles (por defecto 1600, máximo 4096).
- `stageIndex` / `stageCount` / `stageLabel`: ejecutan **una sola etapa** de un plan
  por etapas. Omitir `stageIndex` (o `-1`) ejecuta el plan entero en esta llamada.
  Ver **Montaje por etapas** más abajo.
- `code`: mismo contrato que el panel — se inyectan `app/doc/uidoc/ctx`; usa
  `ctx.Log("...")` para trazas. También admite una clase completa con
  `public static object Execute(UIApplication uiApp, Bibim.Core.BibimExecutionContext ctx)`.

Response:
```json
{
  "success": true,
  "compiled": true,
  "mode": "dryrun",
  "output": "Muro de 5 m creado en Nivel 1.",
  "error": null,
  "affectedElementCount": 1,
  "logs": ["Muro creado con id 123456"],
  "warnings": [],
  "documentTitle": "Proyecto1",
  "imageBase64": "iVBORw0KGgo...",
  "captureError": null
}
```
- `stagesApplied`: etapas aplicadas por esta llamada.
- `mainThreadMs`: cuánto retuvo esta ejecución el hilo principal de Revit.
- `imageBase64` solo aparece si se pidió `capture`. Si la captura falla, la ejecución
  **no** falla: `captureError` explica por qué y el resto del resultado sigue siendo válido.
- `compiled:false` → falló la compilación; el detalle Roslyn está en `error`
  (aquí es donde el "cerebro" corrige y reintenta).

### `POST /undo`
Deshace N pasos en el documento. Un montaje por etapas deja **una entrada de deshacer
por etapa**, así que para revertir la operación completa se pasa el número de etapas.

```json
{ "count": 9 }
```
```json
{ "ok": true, "requested": 9, "undone": 9, "error": null }
```
Si un paso falla, `undone` dice cuántos se llegaron a deshacer.

### `GET /context?tag=@selection`
Contexto vivo del modelo (usa `RevitContextProvider`). Tags admitidos:
`@view`, `@selection`, `@levels`, `@worksets`, `@phases`,
`@family:<categoría>`, `@parameters:<categoría>`.
```json
{ "ok": true, "tag": "@selection", "text": "[Selected Elements] 2 element(s)\n  - Walls | ..." }
```

### `GET /view/image?width=1600`
Exporta la **vista activa** a PNG y la devuelve en base64 (para el bucle de visión).
```json
{ "ok": true, "format": "png", "width": 1600, "base64": "iVBORw0KGgo..." }
```

## Bucle recomendado para la skill (cerebro externo)

1. `GET /status` → ¿hay documento y vista?
2. `GET /context?tag=@levels` (y `@selection`, `@parameters:Walls`…) para aterrizar el código.
3. Generar C# con el modelo → `POST /execute {mode:"dryrun", capture:true}`.
4. Si `compiled:false` o `success:false` → realimentar `error` al modelo y volver a 3.
5. Mirar el `imageBase64` que vino en esa misma respuesta → ¿coincide con lo pedido?
   **El modelo del usuario no se ha tocado en ningún momento.**
6. Si OK → `POST /execute {mode:"commit"}`. Si no → corregir y volver a 3.

Con `capture` el bucle completo — incluida la verificación visual paso a paso — se
puede recorrer entero sin modificar el documento ni una sola vez. Antes había que
aplicar los cambios para poder verlos.

## Notas / límites (v1)

- Solo `127.0.0.1` (no accesible desde la red). El puerto específico evita
  necesitar permisos de administrador (netsh urlacl).
- El acceso a la Revit API se serializa al **hilo principal** vía `ExternalEvent`.
  Si hay un diálogo modal abierto en Revit, las peticiones esperan y pueden
  agotar el tiempo (`504`). Ejecuciones: 180 s; lecturas: 60 s.
- No expone (todavía) el planner ni el bucle LLM del propio plugin: por diseño,
  ese rol lo lleva la skill. Si en el futuro quieres delegar también el
  razonamiento al plugin, se añade un `POST /task`.

## Ejemplos rápidos (curl)

```bash
TOKEN=$(jq -r .token "$APPDATA/Bibim/control_api.json")

curl -s http://127.0.0.1:8757/status -H "Authorization: Bearer $TOKEN"

curl -s -X POST http://127.0.0.1:8757/execute \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"code":"return \"Niveles: \" + new FilteredElementCollector(doc).OfClass(typeof(Level)).GetElementCount();","mode":"dryrun"}'

curl -s "http://127.0.0.1:8757/context?tag=@levels&token=$TOKEN"
```

## Nota técnica sobre la captura en dry-run

`ImageExportOptions` / `Document.ExportImage` funcionan con un `TransactionGroup`
abierto. Lo que **no** funciona en ese estado es `Document.Regenerate()`, que exige
una `Transaction` abierta y si no lanza:

```
InvalidOperationException: Modification of the document is forbidden.
Typically, this is because there is no open transaction
```

Por eso `ViewImageExporter` solo regenera cuando `doc.IsModifiable` es `true`, y si
aun así el documento rechaza la exportación, reintenta dentro de una `Transaction`
que revierte de inmediato. No hace falta regenerar en la ruta de dry-run: confirmar
la transacción interna del código generado ya regenera la vista.

## Montaje por etapas

**Revit no repinta mientras corre tu código.** Todo lo que ejecuta `/execute` ocurre en
el hilo principal de Revit; hasta que la ejecución no termina, la ventana no se
redibuja. Una ejecución larga no se ve «en progreso»: se ve **congelada**, y a partir
de unos segundos Windows la marca como «No responde».

Por eso no sirve meter pausas dentro de una ejecución: empeoran el problema. La única
forma de que el usuario vea el modelo construirse es **una ejecución por etapa**:

```
POST /execute { code, mode:"commit", stageIndex:0, stageCount:9, stageLabel:"Cimentación" }
  → Revit recupera el bucle de mensajes y repinta
POST /execute { code, mode:"commit", stageIndex:1, stageCount:9, stageLabel:"Muros" }
  → ...
```

El mismo código se envía en todas las llamadas y se ramifica con `ctx.Stage`:

```csharp
// ETAPAS: Cimentación | Muros | Cubierta
switch (ctx.Stage)
{
    case 0: /* cimentación */ break;
    case 1: /* muros */       break;
    case 2: /* cubierta */    break;
}
```

`ctx.Stage`, `ctx.StageCount` y `ctx.StageName` están disponibles en el código generado.

Reglas que impone el diseño:

- **Nada sobrevive entre etapas.** Cada llamada es una invocación nueva: lo que una
  etapa posterior necesite se vuelve a consultar al modelo con un
  `FilteredElementCollector`, no se guarda en una variable local.
- **La espera la pone el cliente**, nunca el código: un `Thread.Sleep` dentro de
  `/execute` bloquea Revit, que es justo lo que se quiere evitar.
- **Cada etapa deja su propia entrada de deshacer.** Usa `POST /undo` con `count`
  igual al número de etapas para revertir la operación de una vez.
- En **dry-run** no hace falta trocear: omite `stageIndex` y las etapas se ejecutan
  seguidas dentro del mismo `TransactionGroup` (se acumulan y se revierten juntas).
  No hay nada que ver, así que no hay nada que repartir.

### Guardarraíl

Si una ejecución retiene el hilo principal más de 2,5 s, la respuesta incluye un aviso
en `warnings` y queda registrado como `[MAIN_THREAD_WARNING]` en el log. Es la señal de
que las etapas son demasiado grandes.

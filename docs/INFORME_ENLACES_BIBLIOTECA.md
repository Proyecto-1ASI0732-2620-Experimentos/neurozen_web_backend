# NeuroZen Backend: enlaces de YouTube en la biblioteca de recursos

Fecha: 17/11/2026 · Fuente de los enlaces: `Recursos_YouTube_Biblioteca.pdf` (tabla §4 del prompt).
Solo se registraron **URLs de texto**. No se descargó, subió ni guardó ningún archivo de video o audio.

## 1. Inspección previa

| Punto | Hallazgo en el código real |
|---|---|
| Entidad / tabla | `ResourcesLibrary/Domain/Model/Aggregates/ResourceLibrary.cs` → tabla `resource_libraries` (`id`, `title`, `description`, `content`, `author`, `resource_type`, `content_url` varchar(500), `duration` en minutos, `thumbnail`, `tags` json, `created_at`). No tiene `updated_at` |
| Carga actual | Solo `ResourceLibrarySeeder` (al arrancar), que inserta **3 recursos de demostración** y únicamente si `Seed:Demo:Enabled=true` (Development) **y** la tabla está vacía. Además existe `POST /resource-libraries` (solo ADMIN) |
| ¿Existían los 12 recursos? | **No.** Ninguno de los 12 títulos aparece en el backend (ni en seeders, ni en migraciones, ni en pruebas). Probablemente están escritos directamente en el frontend. En producción la tabla solo tiene lo que un admin haya cargado |
| Validación | `content_url`: http(s) válido, máximo 500 caracteres (solo en `POST`). `resource_type`: `Video`, `Audio`, `Article` o `Exercise` |

**Diferencia con el prompt:** como los recursos no existían, la migración **actualiza el enlace si el recurso existe** y
**lo inserta si falta**, tal como el prompt indica para ese caso.

## 2. Cómo se cargan los enlaces

Migración EF **solo de datos** (sin cambios de esquema; el snapshot no cambió):
`Shared/Infrastructure/Persistence/EFC/Migrations/20261117225609_AddYouTubeLinksToResourceLibrary.cs`.

La API la aplica sola al arrancar (`MigrateAsync`), en todos los entornos. Por cada recurso ejecuta dos sentencias:

1. `UPDATE resource_libraries SET content_url = '<enlace>' WHERE title IN ('<título>'[, '<variante con raya –>']) AND resource_type = '<Video|Audio>' AND (content_url IS NULL OR content_url <> '<enlace>')`.
   Solo cambia `content_url`. No toca el tipo, la duración, la descripción ni las etiquetas.
2. `INSERT ... SELECT ... WHERE NOT EXISTS (SELECT 1 FROM resource_libraries WHERE title IN (...))`.
   Solo inserta si no hay ninguna fila con ese título.

Las dos sentencias son **idempotentes**: si se ejecutan otra vez, no duplican ni cambian nada. Son 24 sentencias
concretas, sin cambios masivos, `DELETE`, `DROP` ni `TRUNCATE`. Los artículos y ejercicios no se tocan.

**Datos con que se insertan los recursos que faltan:** título y tipo (§4 del prompt); duración (la que figura en el
encabezado de cada tema del PDF); descripción (la del video elegido según el PDF); autor solo cuando el PDF lo
identifica (Mutua MAZ, Raquel Sastre, Pablo Gómez); etiquetas breves por tema; `thumbnail` y `content` vacíos.

**Revertir:** el `Down()` está vacío **a propósito**, para no borrar filas (el prompt pide no borrar), y porque los
enlaces de relleno anteriores no se conocen en todos los entornos. Para revertir un enlace, se actualiza su
`content_url` de forma manual (`UPDATE ... WHERE id = ...`). Para quitar la migración antes de desplegarla:
`dotnet ef migrations remove`.

## 3. Recursos y enlaces

En este repositorio ninguno de los 12 existía antes, así que el enlace anterior es "—". En una base donde ya exista
el recurso, se reemplaza el enlace que tenga.

| # | Recurso | Tipo | Enlace anterior | Enlace nuevo | Duración |
|---|---|---|---|---|---|
| 1 | Técnicas de Respiración Profunda | Video | — | https://www.youtube.com/watch?v=CVMBSsqTGOk | 15 min (sin cambios si ya existe) |
| 2 | Ejercicios de Estiramiento para Oficina | Video | — | https://www.youtube.com/watch?v=H0pbvdIcHOY | 8 min |
| 3 | Yoga para Reducir la Ansiedad | Video | — | https://www.youtube.com/watch?v=tS52C0Pxf_g | 20 min |
| 4 | Ejercicios de Relajación Muscular Progresiva | Video | — | https://www.youtube.com/watch?v=PzzAlvr3O3M | 18 min |
| 5 | Meditación para el Manejo del Dolor Crónico | Video | — | https://www.youtube.com/watch?v=r5K8Drgzj_Y | 22 min |
| 6 | Ejercicios de Atención Plena en el Trabajo | Video | — | https://www.youtube.com/watch?v=40LH8G7kI84 | 10 min |
| 7 | Meditación Guiada de 5 Minutos | Audio | — | https://www.youtube.com/watch?v=pyAyEzmiKxU | 5 min |
| 8 | Música Relajante para Dormir | Audio | — | https://www.youtube.com/watch?v=an60NyBayzE | 30 min |
| 9 | Podcast: Manejo de Crisis de Ansiedad | Audio | — | https://www.youtube.com/watch?v=A8qOLnTbQdg | 25 min |
| 10 | Sonidos de la Naturaleza - Bosque Lluvioso | Audio | — | https://www.youtube.com/watch?v=bwSdDBSJK10 | 45 min |
| 11 | Técnicas de Visualización Positiva | Audio | — | https://www.youtube.com/watch?v=GiOamuperW4 | 12 min |
| 12 | Caminata Meditativa - Guía de Audio | Audio | — | https://www.youtube.com/watch?v=QLkFTi02Geo | 15 min |

Las duraciones son las de la biblioteca (encabezados del PDF), **no** la duración real de cada video, que no se
confirmó. En los recursos que ya existían, la duración no se modifica.

**Alternativas (no cargadas):** las otras 3 opciones de cada tema en el PDF. Si un enlace elegido deja de estar
disponible o no se puede insertar, se usa la siguiente opción del mismo tema (normalizada a
`https://www.youtube.com/watch?v=ID`; por ejemplo, `m.youtube.com/watch?v=bkQwODQAdCo` y
`youtube.com/watch?v=g3-IZXI4ksw`). Si se quieren varias opciones por recurso, se propone (sin implementarla) una tabla
`resource_links(id, resource_id FK, url, label, position)`.

## 4. Pruebas (resultados reales)

Entorno: .NET SDK 10.0.115 compilando `net9.0` y MySQL 8.0.46 local.

| Momento | Resultado |
|---|---|
| Antes del cambio (suite con DNI y consentimientos) | 208/208 ✅ |
| Después del cambio | **225/225 ✅ en tres ejecuciones** (17 pruebas nuevas, ninguna eliminada) |

Pruebas nuevas en `neurozen.API.Tests/Integration/ResourcesLibrary/ResourceLibraryYouTubeLinksTests.cs`:

| Prueba | Resultado |
|---|---|
| `GET /resource-libraries` devuelve cada uno de los 12 recursos con su tipo y su enlace (12 casos) | ✅ |
| `GET /resource-libraries/{id}` y el alias `/resources/{id}` de un video y de un audio devuelven su enlace; el JSON conserva exactamente las mismas propiedades (lista y detalle) | ✅ (2 casos) |
| Volver a aplicar la migración dos veces no duplica recursos (total igual, un registro por título) | ✅ |
| En una fila existente con enlace de relleno solo se corrige `contentUrl` (misma fila, misma duración y tipo); el artículo de control no cambia | ✅ |
| Los 12 enlaces tienen el formato `https://www.youtube.com/watch?v=ID` y caben en la columna (500) | ✅ |

**Comprobación manual en MySQL** (actualización desde la migración anterior, con 4 filas previas: un artículo, dos
recursos con enlaces de relleno y uno titulado con raya «–»):
- Se corrigió solo el `content_url` de los 3 recursos existentes. Su descripción y su duración se conservaron.
- La variante del título con raya se reconoció.
- El artículo no cambió.
- Se insertaron los 9 recursos que faltaban: 13 filas en total, 13 títulos distintos, sin duplicados.

## 5. Confirmaciones

- No se guardaron archivos de video ni de audio (solo URLs de texto).
- No se modificó Docker, Render, Railway, Nixpacks, CI, `appsettings*.json`, variables de entorno ni scripts.
- No se tocaron el registro, el DNI, los consentimientos ni la verificación de psicólogos.
- No cambiaron las rutas ni los nombres de las propiedades JSON.

## 6. Archivos

| Archivo | Motivo |
|---|---|
| `neurozen.API/Shared/Infrastructure/Persistence/EFC/Migrations/20261117225609_AddYouTubeLinksToResourceLibrary.cs` (+ `.Designer.cs`) | Migración de datos idempotente con los 12 enlaces |
| `neurozen.API.Tests/Integration/ResourcesLibrary/ResourceLibraryYouTubeLinksTests.cs` | Pruebas de integración |
| `docs/INFORME_ENLACES_BIBLIOTECA.md` | Este informe |
| `neurozen.API/Shared/Infrastructure/Persistence/EFC/Migrations/20261117231316_AddYouTubeThumbnailsAndClinicalNotice.cs` (+ `.Designer.cs`) | Miniaturas y aviso clínico (§7) |
| `ResourcesLibrary/Domain/Model/Aggregates/ResourceLibrary.cs`, `Domain/Model/Commands/CreateResourceLibraryCommand.cs`, `Domain/Services/ResourceLibraryServices.cs`, `Application/Internal/CommandServices/ResourceLibraryCommandService.cs`, `Interfaces/REST/ResourcesLibraryController.cs`, `Interfaces/REST/Resources/ResourceLibraryResources.cs`, `Interfaces/REST/Transform/ResourceLibraryAssemblers.cs` | Endpoint de edición `PATCH /resource-libraries/{id}` (ADMIN), con validaciones compartidas con el `POST` |
| `ResourcesLibrary/Infrastructure/Seeding/ResourceLibrarySeeder.cs`, `Domain/Repositories/IResourceLibraryRepository.cs`, `Infrastructure/Repositories/ResourceLibraryRepository.cs` | El seeder de demo agrega cada ejemplo que falte (comprobando por título); `AnyAsync` se eliminó porque ya no se usaba |
| `tools/check-youtube-links.sh`, `tools/check-youtube-links.ps1`, `tools/youtube-links.txt` | Verificación de disponibilidad e inserción de los enlaces |
| `neurozen.API.Tests/Unit/ResourcesLibrary/ResourceLibrarySeederTests.cs` y nuevas pruebas en `ResourceLibraryYouTubeLinksTests.cs` | Pruebas de lo anterior |

## 7. Pendientes corregidos (segunda iteración)

| Pendiente | Solución | Verificación |
|---|---|---|
| **Miniaturas vacías** | Migración `AddYouTubeThumbnailsAndClinicalNotice`: asigna a cada recurso la imagen pública de su video (`https://img.youtube.com/vi/ID/hqdefault.jpg`, solo una URL de texto). Solo lo hace si el recurso sigue apuntando a ese video **y** no tiene miniatura, así que nunca reemplaza la imagen que haya puesto un admin | Pruebas de integración (12 casos) + prueba manual: la miniatura propia se conservó |
| **Aviso clínico** | La misma migración agrega «Este material no sustituye la atención de un profesional de la salud.» a la descripción de *Dolor crónico* y *Crisis de ansiedad*. Lo agrega una sola vez y solo en esos dos recursos | Prueba de integración: el aviso aparece 1 vez en los 2 recursos clínicos y 0 en los demás, aunque se aplique de nuevo |
| **Migración reversible** | El `Down()` de la nueva migración quita exactamente lo que agregó (las miniaturas generadas y el aviso). No borra filas | Prueba de integración (Down → Up) + manual: `dotnet ef database update` hacia la migración anterior revirtió y siguió habiendo 13 filas |
| **Enlaces que no se pueden insertar / duraciones distintas** | Nuevo `PATCH /api/v1/resource-libraries/{id}` (solo ADMIN) para corregir `contentUrl`, `duration`, `thumbnail`, `title`, `description`, `content`, `author` y `tags` sin entrar a la base de datos. Un campo `null` o ausente se conserva; `""` vacía un campo opcional. `resourceType` no se puede editar. Valida igual que el `POST` (URL http(s), máximo 500 caracteres, duración entre 0 y 600) | Pruebas de integración: edición correcta, 400 con datos inválidos sin cambiar nada, 403 para un USER, 404 si no existe |
| **Comprobar disponibilidad e inserción** | `tools/check-youtube-links.sh` (Linux/macOS) y `tools/check-youtube-links.ps1` (Windows) consultan el oEmbed público de YouTube (sin clave): 200 = OK; 401 = el dueño bloqueó la inserción; 403/404 = privado o eliminado. Si el enlace elegido falla, indican la primera alternativa del PDF que funciona (`tools/youtube-links.txt`) para cambiarla con el `PATCH` | Probado aquí: detecta que no hay conexión y termina con código 2. **No se pudo comprobar YouTube desde este entorno**, porque su red bloquea youtube.com |
| **Desarrollo local sin recursos de demo** | `ResourceLibrarySeeder` ahora agrega cada ejemplo de demo que falte (comprobando por título), en lugar de no hacer nada cuando la tabla tiene filas. Nunca modifica filas existentes | 3 pruebas unitarias |

Pruebas: **245/245 ✅ en tres ejecuciones** (antes de esta iteración 225/225; 20 pruebas nuevas, ninguna eliminada).

## 8. Lo que no se puede cerrar desde el código (y por qué)

- **Disponibilidad real y duración real de cada video:** se necesita acceso a YouTube, que este entorno no tiene.
  Pasos:
  1. Ejecutar `bash tools/check-youtube-links.sh` o `tools\check-youtube-links.ps1`.
  2. Si algún enlace falla, cambiarlo con el `PATCH` usando la alternativa que indique el script.
  3. Para corregir una duración (por ejemplo, la música y la lluvia, que en realidad duran horas), enviar
     `PATCH { "duration": N }`.

  El oEmbed de YouTube no informa la duración; se ve en el propio video o con la API de YouTube Data (requiere una
  clave, que es configuración externa).

## 9. Acceso a la biblioteca según el plan (tercera iteración)

### Reglas implementadas

| Quién | Qué ve en la biblioteca |
|---|---|
| Plan **Básico** (`planId = 1`) y usuarios **sin suscripción activa** | Solo los recursos marcados como Básico: **1 o 2 de cada tipo** (videos, audios, artículos, ejercicios) |
| Plan **Avanzado** (`planId = 2`) | **Toda la biblioteca actual** (Básico + Avanzado) |
| Plan **Profesional** (`planId = 3`) | **Todo**, incluidos los recursos marcados como Profesional (hoy ninguno: el nivel queda listo para contenido exclusivo) |
| Psicólogo **verificado** y **ADMIN** | Todo (no tienen suscripción; usan o administran la biblioteca) |

Supuestos:
- `planId` 1, 2 y 3 corresponden a Básico, Avanzado y Profesional, porque son los únicos valores que acepta
  `POST /subscriptions`.
- Un usuario sin suscripción se trata como Básico (vista previa), para que siga viendo algo de la biblioteca.

Si alguno de los dos no coincide con el frontend, se cambia en `ResourcePlan` / `ResourceAccess`.

Como pedía el prompt de la biblioteca, el backend **no entrega el `contentUrl`** de un recurso fuera del plan:
- `GET /resource-libraries`: ese recurso **no se lista**.
- `GET /resource-libraries/{id}` y `/resources/{id}`: responde **403** con el mensaje
  `"Este recurso está disponible desde el plan Avanzado"` (o `Profesional`), sin enlace en el cuerpo.

Subir de plan desbloquea los recursos en la siguiente petición; no hay caché por usuario. La protección es real
dentro de NeuroZen, pero los videos son públicos en YouTube: cualquiera con el enlace puede verlo fuera de la app.

### Asignación inicial (migración `20261118000530_AddResourcePlanAccess`)

- Agrega la columna `resource_libraries.required_plan` (1, 2 o 3). Todas las filas existentes quedan en **Avanzado**.
- Luego marca como **Básico** los **2 primeros recursos de cada tipo** (por id). Con los 12 de YouTube quedan en
  Básico: *Técnicas de Respiración Profunda* y *Ejercicios de Estiramiento para Oficina* (videos), y *Meditación
  Guiada de 5 Minutos* y *Música Relajante para Dormir* (audios). Los otros 8 quedan en Avanzado. Si hay artículos o
  ejercicios, también quedan 1 o 2 de cada uno en Básico.
- No inserta ni borra filas. `Down()` solo elimina la columna (probado: 16 filas antes y después de revertir).
- Los recursos nuevos (`POST`) son **Avanzado** por defecto.
- El admin puede cambiar el plan de cualquier recurso con `PATCH /resource-libraries/{id} { "requiredPlan": 1|2|3 }`
  (o enviarlo al crear con `POST`). Cualquier otro valor da 400.
- Los ejemplos de demo locales (ejercicio y artículo) se crean como Básico.

### Archivos

| Archivo | Motivo |
|---|---|
| `ResourcesLibrary/Domain/Model/ValueObjects/ResourcePlan.cs` (nuevo) | Niveles de plan y reglas |
| `ResourcesLibrary/Application/Internal/ResourceAccess.cs` (nuevo) | Resuelve el plan de quien llama (rol + suscripción activa) |
| `Subscriptions/Interfaces/ACL/ISubscriptionsContextFacade.cs` y `Services/SubscriptionsContextFacade.cs` (nuevos) | Lectura del plan activo desde otro contexto, sin exponer los datos de la tarjeta |
| `ResourcesLibrary/Domain/Model/Aggregates/ResourceLibrary.cs`, `Infrastructure/Persistence/EFC/Configuration/ResourceLibraryConfiguration.cs` | Propiedad y columna `RequiredPlan` |
| `ResourcesLibrary/Application/Internal/QueryServices/ResourceLibraryQueryService.cs` | Filtro del listado y 403 en el detalle |
| `ResourcesLibrary/Application/Internal/CommandServices/ResourceLibraryCommandService.cs`, `Domain/Model/Commands/CreateResourceLibraryCommand.cs`, `Interfaces/REST/Resources/ResourceLibraryResources.cs`, `Interfaces/REST/Transform/ResourceLibraryAssemblers.cs` | `requiredPlan` en el `POST`/`PATCH` de ADMIN, con validación |
| `ResourcesLibrary/Infrastructure/Seeding/ResourceLibrarySeeder.cs` | Ejemplos de demo como Básico |
| `Program.cs` | Registro de `ResourceAccess` y `ISubscriptionsContextFacade` (inyección de dependencias) |
| `Migrations/20261118000530_AddResourcePlanAccess.cs` (+ `.Designer.cs`, snapshot) | Columna y asignación inicial |
| `neurozen.API.Tests/Integration/ResourcesLibrary/ResourceLibraryPlanAccessTests.cs` (nuevo), `ResourceLibraryYouTubeLinksTests.cs`, `Integration/Support/TestApi.cs` | Pruebas por plan; las pruebas de enlaces usan un usuario Avanzado |

### Pruebas

**252/252 ✅ en tres ejecuciones** (antes 245; 7 pruebas nuevas, ninguna eliminada):

| Prueba | Resultado |
|---|---|
| Sin plan y plan Básico: solo los 4 recursos Básico de YouTube; ningún tipo aparece más de 2 veces | ✅ (2 casos) |
| Básico pide un recurso Avanzado por id (y por el alias): 403 con el plan necesario, sin `contentUrl` ni el id del video | ✅ |
| Avanzado ve los 12 pero no el recurso Profesional (403 en el detalle); Profesional ve todo (lista = total de la tabla) | ✅ |
| Al pasar de Básico a Avanzado se desbloquea en la siguiente petición | ✅ |
| Psicólogo verificado y ADMIN ven todo | ✅ |
| El admin cambia el `requiredPlan` (400 si es inválido; Básico lo vuelve visible) | ✅ |
| Manual en MySQL: con 3 artículos y 1 ejercicio previos, quedan en Básico 2 artículos, 1 ejercicio, 2 videos y 2 audios; revertir no pierde filas | ✅ |

### Lo que debe saber el frontend / la app

- **Contrato:** el JSON de los recursos **no cambió**. Un usuario sin plan o con Básico simplemente recibe menos
  recursos en la lista.
- **403 en el detalle:** si abre un recurso fuera de su plan (por ejemplo, desde un enlace guardado), recibe 403 con
  `message` = «Este recurso está disponible desde el plan …». Conviene mostrar ese mensaje con un botón «Mejorar plan»
  que lleve a suscripciones.
- **Recursos escritos en el frontend:** si el frontend tiene los 12 recursos fijos en su código, debe usar la lista de
  la API. Si no, mostraría recursos que el backend no entrega.
- **Vencimiento de la suscripción:** no existe. Una suscripción queda activa hasta que se cambie, porque no hay
  pasarela de pago; esto es así desde antes.

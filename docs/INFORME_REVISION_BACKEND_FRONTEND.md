# Revisión del backend de NeuroZen para el frontend web mejorado (B1–B12)

Fecha: 2026-11-26 · Proyecto: API REST **.NET 9 / ASP.NET Core** (DDD por bounded contexts: IAM, Professionals,
Appointments, Stress, ResourcesLibrary, Wellness, Subscriptions y el nuevo **Safety**), EF Core 9 + MySQL 8
(Pomelo), JWT con roles `USER` / `PSYCHOLOGIST` / `ADMIN`, middleware global de errores `{ message, errors }`.

Base de todas las rutas: `/api/v1`. Zona horaria de horarios y horas: **America/Lima** (UTC−5 fijo, sin horario de
verano; `Shared/Domain/Model/Time/LimaTime.cs`). Todas las fechas-hora se guardan y viajan en **UTC ISO con `Z`**.

---

## B. Tabla de revisión

| ID | Endpoint / comportamiento | Estado inicial | Archivos modificados o creados | Cambio realizado | Estado final |
|---|---|---|---|---|---|
| B1 | `GET /professionals`, `GET /professionals/{id}` | Existía con diferencias: faltaba `availabilitySchedule` (solo texto `availability`); no expone `licenseNumber` | `Professionals/Interfaces/REST/Resources/ProfessionalResource.cs`, `.../Transform/ProfessionalResourceFromEntityAssembler.cs` | Se agrega `availabilitySchedule { days, start, end }` (o `null`). `price` sigue `null` si no está definida. `rating` = promedio real de reseñas (0 sin reseñas), `reviews` = conteo real (`ReviewCommandService` recalcula desde la tabla `reviews`). **`licenseNumber` NO se agrega** (ver nota 1) | Corregido (colegiatura: Pendiente, decisión de producto) |
| B2 | `GET /professionals/{id}/available-slots?date=` | Existía con diferencias: bloques fijos de 60 min y solo excluía citas con la **misma hora de inicio** | `Professionals/Domain/Services/SlotAvailabilityCalculator.cs` (nuevo), `AvailabilitySchedule.cs`, `ProfessionalQueryService.cs`, `ProfessionalQueries.cs`, `ProfessionalsController.cs`, `Appointments/Interfaces/ACL/*ContextFacade.cs`, `AppointmentType.cs` | `?durationMinutes=` opcional (15–240, por defecto 60). Una cita PENDING/CONFIRMED ocupa **su duración real** (60/45/30 según el tipo) y se excluye todo bloque que se solape. 400 por fecha/duración inválida, 404 si no existe o no está verificado | Corregido |
| B3 | `POST /appointments` | Existía con diferencias: fuera de horario devolvía **400**; solo detectaba choque por hora de inicio idéntica | `AppointmentCommandService.cs`, `IAppointmentRepository.cs`, `AppointmentRepository.cs`, `AppointmentsController.cs` (solo documentación Swagger) | Fuera de horario → **409**. Solapamiento con cualquier cita activa (según duraciones) → 409 «El horario ya no está disponible». Comprobación + inserción dentro de una **transacción con `SELECT … FOR UPDATE` sobre el profesional**; se mantiene el índice único `ux_appointments_professional_active_slot`. Los tipos 2 y 3 pueden empezar también en su propia grilla (45/30 min). Ya cumplía: responde 201 con `id`, fecha pasada → 400, estado inicial `PENDING`, confirmación por `PATCH /appointments/{id}/status` | Corregido |
| B4 | `GET /appointments/availability` | No existía | `Appointments/Interfaces/REST/AppointmentAvailabilityController.cs`, `Appointments/Application/Internal/QueryServices/AppointmentAvailabilityQueryService.cs`, `Appointments/Domain/Services/IAppointmentAvailabilityQueryService.cs`, `Professionals/Interfaces/ACL/(Services/)ProfessionalAvailabilityFacade.cs`, `Professionals/Domain/Model/ReadModels/AvailabilityCalendar.cs`, recursos y ensamblador en `AppointmentResource.cs` / `AppointmentResourceFromEntityAssembler.cs`, `Program.cs` (registro DI) | Nuevo endpoint; reutiliza `SlotAvailabilityCalculator` (sin duplicar reglas). Máx. 92 días, `nextAvailableDate` (puede ser posterior a `endDate` o `null`; horizonte de búsqueda: 92 días tras `endDate`) | Agregado |
| B5 | Errores en `sign-in`, `sign-up`, `forgot-password`, `reset-password` y middleware global | Existía con diferencias: login ya respondía 401 «Correo o contraseña incorrectos», pero (a) una excepción sin mensaje producía el texto técnico de .NET, (b) 415/`NotFound()` salían como ProblemDetails **sin `message`**, (c) ruta inexistente/405 salían **sin cuerpo** | `ErrorResponse.cs`, `GlobalExceptionHandler.cs`, `ErrorResponseClientErrorFactory.cs` (nuevo, incluye `EmptyErrorBodyMiddleware`), `Program.cs` | `ErrorResponse` normaliza: nunca `null`, `""`, `"null"`, `"undefined"`, JSON ni «Exception of type…»; mensajes por código en español. Fábrica de errores de cliente MVC y red de seguridad `UseStatusCodePages` para cuerpos vacíos | Corregido |
| B6 | `GET /users/{id}` `stressData`; `GET/POST/DELETE /triggers` | `stressData`: **no existe** (correcto: no se agrega con datos de ejemplo). Triggers: existía y cumplía | — | Sin cambios. Evidencia triggers: `Stress/Interfaces/REST/TriggersController.cs` (`[Authorize(Roles = USER)]`), `StressAccess.RequireOwnUserId` (403 si `userId` ≠ token), `StressTriggerCommandService.Handle(DeleteTriggerCommand)` (404 si no es propio). `intensity` se calcula en el servidor desde `stressLevel` (se ignora el valor enviado) | Sin cambios |
| B7 | `POST /assessments`, `GET /assessments?userId=` | Existía y cumplía | — (prueba de integración nueva) | Evidencia: `AssessmentsController` (solo `USER`), `PerceivedStressScale` (ítems 4, 5, 7, 8 invertidos; 0–13 low, 14–26 moderate, 27–40 high), `AssessmentCommandService` ignora `score`, `level` y `completedAt` del cliente (usa la hora del servidor), `StressAccess` (solo el propio usuario) | Sin cambios |
| B8 | `GET /resource-libraries/search` y campos del listado | Búsqueda: no existía. Listado: existía con diferencias (`duration`, no `durationMinutes`) | `ResourceRelevanceScorer.cs`, `ResourceSearchHit.cs` (nuevos), `ResourceLibraryQueryService.cs`, `ResourceLibraryQueries.cs`, `ResourceLibraryServices.cs`, `ResourceLibraryResources.cs`, `ResourceLibraryAssemblers.cs`, `ResourcesLibraryController.cs` | Búsqueda con filtro de plan idéntico a `GET /resource-libraries`, sin tildes ni mayúsculas, título/etiquetas pesan más que descripción, sin registrar el texto. **El listado mantiene `duration`** (ver nota 2). El detalle sigue devolviendo 403 «Este recurso está disponible desde el plan …» sin `contentUrl` | Agregado (alias en listado: Pendiente, ver nota 2) |
| B9 | `GET /emergency-resources?country=PE` | No existía | `Safety/**` (nuevo contexto), `Program.cs` | Solo las dos líneas que ya usa el frontend: Línea 113 opción 5 y SAMU 106. Público (`[AllowAnonymous]`), solo lectura, catálogo en código (sin tabla, sin migración). `verifiedAt` sale de configuración (ver nota 3) | Agregado (`verifiedAt`: Pendiente de que el equipo registre la verificación) |
| B10 | `PasswordPolicy` | Existía | — | Solo se informa (ver sección B10 abajo) | Sin cambios |
| B11 | `POST /zenbot/messages` | No existía | — | No implementado; se documenta lo necesario (sección B11) | Pendiente |
| B12 | CORS | Existía y cumplía | — | `appsettings.json` → `Cors:AllowedOrigins` incluye `https://neurozen-web-frontend.vercel.app` y `http://localhost:5173` (más `:4173`); `Program.cs` usa los mismos valores por defecto si la sección falta. Ningún archivo de despliegue la sobreescribe | Sin cambios |

**Nota 1 — colegiatura pública.** El directorio público omite `licenseNumber` por decisión explícita del proyecto
(`ProfessionalResource.cs`: «Never includes licenseNumber or email»; `docs/ENTREGA_FINAL.md` §directorio) y la prueba
`BusinessFlowIntegrationTests` (línea 54) lo exige. No se cambió para no romper esa garantía ni eliminar la prueba. Si
el equipo decide publicarla (el registro del CPsP es consultable públicamente), basta con añadir `string? LicenseNumber`
al recurso público y actualizar esa aserción. El psicólogo sí ve su número en `GET /professionals/me`.

**Nota 2 — `durationMinutes` en el listado.** `ResourceLibraryYouTubeLinksTests.GetById_VideoAndAudio_ShouldReturnLinkWithSameJsonShape`
congela **el conjunto exacto** de propiedades del listado y del detalle («el JSON no puede cambiar de nombres de
propiedades»). Añadir el alias rompía esa prueba, así que se revirtió. **El frontend debe leer `duration` (minutos)**
en `GET /resource-libraries` y en el detalle. La búsqueda nueva sí usa `durationMinutes`.

**Nota 3 — verificación de las líneas de emergencia.** Desde este entorno no fue posible abrir gob.pe/minsa.gob.pe
(el proxy bloquea el host), así que **no se verificaron** los números de forma independiente ni se inventó una fecha.
Se publican solo los dos datos que ya muestra el frontend. Cuando el equipo los confirme en la fuente oficial, debe
configurar `EmergencyResources__VerifiedAt=YYYY-MM-DD`; hasta entonces `verifiedAt` es `null`.

---

## C. Contrato final para el frontend

Errores (todo endpoint): `{ "message": "texto legible", "errors": { "campo": ["mensaje"] } }` (`errors` solo cuando
hay errores por campo). 400 validación · 401 sin sesión/credenciales inválidas · 403 sin permiso · 404 inexistente ·
405 método no permitido · 409 conflicto · 415 Content-Type no JSON · 429 demasiados intentos. `message` es siempre un
texto no vacío. Las respuestas de listas siguen siendo arreglos `[...]` (paginación opcional con `X-Total-Count`),
salvo la búsqueda (`{ items: [...] }`).

### 1. `GET /professionals` · `GET /professionals/{id}`
Auth: Bearer (cualquier rol; un psicólogo no verificado recibe 403). Solo profesionales `VERIFIED` (404 en el detalle si no).
```json
{
  "id": 1, "userId": 12, "name": "Elena Ramírez", "specialty": "clinical",
  "bio": "...", "experienceYears": 8, "price": 80, "rating": 4.5, "reviews": 2,
  "availability": "Lun, Mié · 09:00 - 13:00", "image": "",
  "verificationStatus": "VERIFIED",
  "availabilitySchedule": { "days": ["monday", "wednesday"], "start": "09:00", "end": "13:00" }
}
```
Diferencias con lo pedido: **sin `licenseNumber`** (nota 1); `rating` es `0` (no `null`) cuando no hay reseñas;
`image` es `""` si no hay foto; `availabilitySchedule` es `null` si el psicólogo no lo configuró.

### 2. `GET /professionals/{id}/available-slots?date=YYYY-MM-DD[&durationMinutes=60]`
Auth: Bearer. Respuesta: `["09:00", "10:00", "11:00"]` (hora de Lima). 400 fecha mal formada o duración fuera de
15–240; 404 profesional inexistente/no verificado; día sin horas o fecha pasada → `[]`.
Reglas: días de `availabilitySchedule.days`; bloques de `durationMinutes` desde `start` mientras terminen ≤ `end`;
se excluye todo bloque que se solape con citas `PENDING`/`CONFIRMED` (cada una con su duración: tipo 1 = 60, 2 = 45,
3 = 30 min); hoy se excluyen los bloques que ya empezaron.

### 3. `POST /appointments` (rol `USER`)
Solicitud (sin cambios):
```json
{ "patientId": 7, "professionalId": 1, "appointmentDateTime": "2026-12-21T14:00:00.000Z", "appointmentType": 1, "notasAdicionales": "texto" }
```
201 → `{ "id": 15, "patientId": 7, "patientName": "Ana Torres", "professionalId": 1, "appointmentDateTime": "2026-12-21T14:00:00Z", "appointmentType": 1, "status": "PENDING", "notasAdicionales": "texto", "reviewed": false }`.
Errores: 400 datos inválidos o fecha pasada · 403 `patientId` ≠ usuario del token · 404 profesional inexistente o no
verificado · **409 «El horario elegido no está dentro de la disponibilidad del profesional»** · **409 «El horario ya
no está disponible»** (ocupado/solapado, también ante reservas simultáneas). Inicio válido: un bloque de la grilla del
tipo de cita o de la grilla de 60 min, siempre que la sesión termine dentro del horario. Cambio de estado: `PATCH
/appointments/{id}/status` `{ "status": "CONFIRMED" | "CANCELLED" | "COMPLETED" }`.

### 4. `GET /appointments/availability?professionalId=&startDate=&endDate=[&durationMinutes=60]`
Auth: `USER` y `ADMIN` cualquier profesional verificado; `PSYCHOLOGIST` solo su propio `professionalId` (403 si no).
```json
{
  "professionalId": 1, "timeZone": "America/Lima", "slotMinutes": 60,
  "days": [ { "date": "2026-12-21", "slots": ["09:00", "10:00"] }, { "date": "2026-12-22", "slots": [] } ],
  "nextAvailableDate": "2026-12-21"
}
```
400: falta `professionalId`, fechas mal formadas, `endDate < startDate`, más de 92 días (inclusive) o duración
inválida · 401 sin sesión · 403 psicólogo consultando otro perfil · 404 profesional inexistente o no verificado.
`nextAvailableDate`: primer día con horas libres desde `max(startDate, hoy)`; puede ser posterior a `endDate`
(se busca hasta 92 días después) o `null`.

### 5. `GET /resource-libraries/search?q=&tags=&type=&limit=`
Auth: Bearer; mismo filtro de plan que `GET /resource-libraries` (Básico/sin plan, Avanzado, Profesional; psicólogos
verificados y admins ven todo). Requiere `q` (palabras de 2+ letras) o `tags` (CSV). `type`: `Video|Audio|Article|Exercise`.
`limit` 1–10 (por defecto 5).
```json
{ "items": [ { "id": 3, "title": "Técnicas de Respiración Profunda", "description": "...", "resourceType": "Video",
  "contentUrl": "https://www.youtube.com/watch?v=...", "thumbnail": "...", "durationMinutes": 5,
  "tags": ["respiración"], "score": 0.82 } ] }
```
Relevancia por término: etiqueta = 3, título = 3 (+1 si es palabra completa), descripción = 1; se normaliza a 0–1;
orden descendente por `score` y luego por `id`. Sin coincidencias → `{ "items": [] }`. 400 parámetros inválidos.
El texto buscado no se registra en logs ni en base de datos.
`GET /resource-libraries` y el detalle: campos `id, title, description, resourceType, contentUrl, duration (minutos),
thumbnail, tags, author, createdAt` (+ `content` en el detalle). **Usar `duration`, no `durationMinutes`** (nota 2).

### 6. `GET /emergency-resources?country=PE`
Público (sin token). `country` ISO de 2 letras (por defecto `PE`); otro país → `[]`; formato inválido → 400.
```json
[ { "name": "Línea 113, opción 5 (salud mental)", "phone": "113", "description": "...", "country": "PE", "verifiedAt": null },
  { "name": "SAMU 106", "phone": "106", "description": "...", "country": "PE", "verifiedAt": null } ]
```
`verifiedAt` es `null` hasta que el equipo configure la fecha (nota 3).

### 7. Sin cambios de contrato
`POST /assessments` `{ userId, answers: [10 × 0–4] }` → 201 con `score`/`level` **calculados en el servidor** (los
enviados se ignoran); `GET /assessments?userId=` solo el propio usuario. `GET/POST/DELETE /triggers` como antes
(`intensity` calculada desde `stressLevel`). Autenticación: `POST /authentication/sign-in` `{ username, password }`
→ 401 «Correo o contraseña incorrectos»; `forgot-password` y `reset-password` responden `{ "message": "ok" }`.

---

## B10. Política de contraseñas (solo informe)
`IAM/Domain/Model/ValueObjects/PasswordPolicy.cs`:
- Longitud mínima **8** medida en unidades UTF-16 (`string.Length`; un emoji cuenta 2).
- Al menos una **letra** según `char.IsLetter`: **cualquier letra Unicode** (categorías Lu, Ll, Lt, Lm, Lo): A–Z, á, é,
  ñ, ü, pero también griego, cirílico, CJK, etc.
- Al menos un **número** según `char.IsDigit`: cualquier dígito decimal Unicode (Nd), no solo 0–9 (p. ej. `٣` árabe-índico).
- Máximo **72 bytes UTF-8** (límite de BCrypt).

Para alinear el frontend: `/\p{L}/u` y `/\p{Nd}/u`, longitud ≥ 8 y ≤ 72 bytes UTF-8. Si el frontend usa `[A-Za-z]`
y `\d` será más estricto que el backend (rechazará `ññññññññ1`, que el backend acepta).

## B11. ZenBot en el backend — no implementado (qué haría falta)
Se decidió no implementarlo: la detección de crisis es crítica y no debe quedar con reglas improvisadas sin validación
clínica ni en un endpoint sin el acuerdo legal requerido. Para hacerlo:
1. **Contexto `ZenBot`** con `POST /api/v1/zenbot/messages` `{ conversationId|null, message (≤1000), locale }` →
   `{ conversationId, reply: { text, intent, risk: none|support|check|crisis, actions, resourceIds } }`, rol `USER`.
2. **Detección de riesgo primero**, determinista y versionada (lista de expresiones validada por un psicólogo del
   equipo), antes de cualquier otra lógica; `crisis` devuelve siempre las líneas de `GET /emergency-resources` y la
   acción `book`. No usar el PSS-10 para decidir una crisis; no diagnosticar.
3. **Modo por reglas por defecto** (intenciones → respuestas y `resourceIds` vía `ResourceRelevanceScorer`/búsqueda,
   que ya respeta el plan). Proveedor de IA opcional detrás de `ZenBot:Provider=none` (por defecto) y la clave en una
   variable de entorno (`ZenBot__ApiKey`), nunca en el repositorio.
4. **Ley 29733**: consentimiento explícito y acuerdo de tratamiento antes de enviar texto a un tercero; retención
   mínima (p. ej. 24 h, solo `conversationId` + últimos N turnos en memoria o tabla con purga), sin guardar el texto en
   logs.
5. **429**: nueva política en `RateLimitPolicies` particionada por usuario (p. ej. 20 mensajes/min).
6. Pruebas: riesgo prioritario, no diagnóstico, límite de tasa, plan respetado.

---

## D. Informe de pruebas

Entorno: Ubuntu 24.04, SDK .NET 10.0.115 (el host de descargas de Microsoft está bloqueado en este entorno; se instaló
desde los repositorios de Ubuntu) con **runtime Microsoft.NETCore.App / AspNetCore.App 9.0.23** (paquetes de NuGet),
por lo que las pruebas `net9.0` corrieron sobre el runtime 9, como en CI. MySQL 8.0.46 local con
`NEUROZEN_TEST_MYSQL="server=127.0.0.1;port=3306;user=root;password=root"` (igual que `pipeline.yml`).

Comandos (los mismos del pipeline):
```
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build -c Release --logger "trx;LogFileName=test-results.trx" --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover,cobertura
dotnet publish neurozen.API/neurozen.API.csproj --configuration Release --no-restore   (pasos del Dockerfile)
```

| | Antes | Después |
|---|---|---|
| Build Release | 0 errores, 0 advertencias | 0 errores, 0 advertencias |
| Pruebas (unitarias + integración + BDD) | **262/262** | **346/346** (+84) |
| Cobertura de líneas (Coverlet, `neurozen.API`, incluye migraciones) | 95.45 % (12 987/13 606) | **96.10 %** (13 390/13 933) |
| Cobertura de ramas | 65.08 % (932/1 432) | **70.50 %** (1 133/1 607) |

Todas las clases nuevas tienen 100 % de líneas cubiertas; las líneas no cubiertas en los archivos modificados son
preexistentes.

Pruebas nuevas:
- `Unit/Appointments/SlotAvailabilityCalculatorTests` (días fuera de horario, citas ocupadas con distinta duración,
  horas pasadas de hoy, día sin horas → `[]`, bloques que deben caber, grilla de inicio).
- `Unit/Appointments/AvailabilityQueryTests` (duración, 400/404, rango: todos los días, `nextAvailableDate` dentro y
  después de `endDate`, `null`, límite 92/93 días, autorización USER/ADMIN/PSYCHOLOGIST, contrato del ensamblador).
- `Unit/Appointments/AppointmentBookingRulesTests` (409 fuera de horario, 409 por solapamiento dentro de la
  transacción con bloqueo, tipo corto en su grilla, duraciones por tipo).
- `Unit/Appointments/AppointmentPersistenceOverlapTests` (consultas de solapamiento y fachadas con EF InMemory).
- `Unit/Shared/ErrorResponseTests` (nunca `message: null`/técnico, manejador global, 415, cuerpos vacíos).
- `Unit/ResourcesLibrary/ResourceSearchTests` (plan, orden por relevancia sin tildes/mayúsculas, filtros, 400).
- `Unit/Safety/EmergencyResourceAndPublicProfileTests` (solo 113/106, país, `verifiedAt`; tarifa `null` ≠ 0,
  calificación 0 sin reseñas, `availabilitySchedule`).
- `Integration/Flows/FrontendContractIntegrationTests` (MySQL real: horas y reservas con duración, 409/400,
  disponibilidad por rango con autorización y límites, búsqueda por plan, emergencias públicas, PSS-10 recalculado y
  privado, errores 401/400/415/404/405 con `message`).

Prueba adaptada (no eliminada): `Unit/Professionals/ProfessionalQueryServiceTests.AvailableSlots_ShouldExcludeBookedAndPastHours`
simulaba `ListBookedStartTimesAsync`; ahora simula `ListBookedSlotsAsync` (la misma cita de las 10:00, con su duración).
El escenario y todas las aserciones se mantienen; en `AvailableSlots_DayOutsideSchedule_ShouldReturnEmpty` se añadió
la verificación equivalente para el método nuevo.

No ejecutado:
- **Imagen Docker**: no hay demonio Docker en este entorno. Se ejecutaron los mismos pasos del `Dockerfile`
  (`dotnet restore` + `dotnet publish -c Release --no-restore`) con éxito, y la app publicada arrancó contra MySQL
  (`/health` = healthy, endpoints nuevos visibles en Swagger).
- **GitHub Actions y SonarCloud** no se ejecutaron (requieren el repositorio remoto y `SONAR_TOKEN`); no se afirma
  que pasen. No existe `sonar-project.properties`: la configuración de Sonar vive en `pipeline.yml`.
- Las pruebas de **mutación** (`tools/mutation-check.ps1`) no se ejecutaron (PowerShell/Stryker no disponibles).

## E. Migraciones
**Ninguna migración nueva.** El solapamiento se calcula con columnas existentes (`AppointmentDateTime`,
`AppointmentType`, `Status`) y los índices existentes; las líneas de emergencia son un catálogo de solo lectura en
código. El esquema y los datos no cambian.

## F. Infraestructura protegida
Verificados y **no modificados**: `.github/workflows/pipeline.yml`, `deploy-staging.yml`, `deploy-production.yml`,
`Dockerfile`, `.dockerignore`, `render.yaml`, `railway.toml`, `nixpacks.toml`, `scripts/deploy.sh`,
`neurozen.API/appsettings.json`, `appsettings.Development.json`, `.csproj`, migraciones existentes, `.gitattributes`,
`.gitignore`. No se crearon ni cambiaron ramas.

Problemas encontrados (documentados, **no aplicados**):
1. `nixpacks.toml` → `[phases.setup] nixPkgs = ["dotnet-sdk_8"]`, pero el proyecto apunta a `net9.0`. Si alguna
   plataforma construye con Nixpacks en lugar del Dockerfile, el build fallará. Solución: `dotnet-sdk_9` (o eliminar
   el archivo si no se usa; Render y Railway usan el Dockerfile).
2. `railway.toml` → `[deploy] startCommand = "dotnet neurozen.API.dll"` reemplaza el `ENTRYPOINT` del Dockerfile, que
   es el que respeta `$PORT`; en Railway la app escucharía en el puerto 10000 de `ASPNETCORE_URLS` y no en el `$PORT`
   asignado. Solución: quitar `startCommand` o usar `sh -c "dotnet neurozen.API.dll --urls http://0.0.0.0:${PORT:-10000}"`.

## G. Variables de configuración nuevas
| Variable | Propósito | Valor por defecto | Dónde |
|---|---|---|---|
| `EmergencyResources__VerifiedAt` (sección `EmergencyResources:VerifiedAt`) | Fecha `YYYY-MM-DD` en que el equipo verificó la Línea 113 (opción 5) y el SAMU 106 en la fuente oficial; se publica como `verifiedAt` | vacío → `verifiedAt: null` | Variables de entorno del servicio en Render (y Railway/entorno local si aplica). No es un secreto |

No se añadieron secretos ni se modificó ningún `appsettings`.

## H. Commits
El `.zip` recibido no incluía `.git`, así que **no se ejecutó ningún commit** sobre el repositorio del equipo. Commits
recomendados (Conventional Commits), en este orden:
- `feat(appointments): compute free slots by appointment duration with ?durationMinutes`
- `feat(appointments): add availability range endpoint`
- `fix(appointments): return 409 when slot is taken, overlaps or is outside the schedule`
- `fix(appointments): serialize concurrent bookings with a professional row lock`
- `feat(professionals): expose availabilitySchedule in the public profile`
- `feat(resources): add relevance search respecting subscription plan`
- `feat(safety): add emergency resources endpoint`
- `fix(errors): never return null or technical messages`
- `test(api): cover availability, booking conflicts, search, emergency lines and error bodies`
- `docs: add backend review report for the improved web frontend`

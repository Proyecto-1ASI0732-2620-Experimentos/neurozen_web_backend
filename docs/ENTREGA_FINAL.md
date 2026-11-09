# NeuroZen Backend — Entrega final (fases 1 a 7)

Backend ASP.NET Core 9 + EF Core 9 (MySQL 8) alineado con `NeuroZen_Especificacion_Backend.md`, con pruebas
unitarias, de integración y BDD según `NeuroZen_Pruebas_Backend.md`.

## 1. Estado de verificación

Resultados reales, ejecutados por el equipo en Windows con .NET 9.0.316 y MySQL 8 (Testcontainers sobre Docker Desktop).

| Qué | Estado |
|---|---|
| `dotnet build` del backend | ✅ 0 errores, 0 advertencias |
| Migración `InitialCreate` | ✅ Generada con `dotnet ef` (`Shared/Infrastructure/Persistence/EFC/Migrations`) y aplicada por la API al arrancar en las pruebas de integración |
| Suite completa (`dotnet test`): 72 casos unitarios, 19 de integración y 15 escenarios BDD | ✅ **106/106 en verde, en tres ejecuciones consecutivas** (sin pruebas inestables), sin cambios de código tras la primera ejecución real |
| Prueba de mutación (`tools/mutation-check.ps1`, unitarias + integración) | ✅ **6/6 mutaciones detectadas** (ver `docs/mutation-results.md`) |
| Cobertura (coverlet + ReportGenerator, ensamblado `neurozen.API`) | ✅ **82 % de líneas** (5 213 de 6 357) · 48,2 % de ramas (593 de 1 228), reportada como dato. Detalle por clase en `TestResults/coverage/index.html` |
| CI (GitHub Actions con MySQL 8) | ⏳ Falta el primer push y la captura de la ejecución en verde |

## 2. Ejecutar localmente

### 2.1 Requisitos

- .NET 9 SDK (`dotnet --list-sdks` debe mostrar 9.x).
- MySQL 8 (local o en Docker: `docker run -d --name neurozen-mysql -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 mysql:8.0`).
- Herramienta de EF: `dotnet tool install --global dotnet-ef --version 9.*`.
- Para las pruebas de integración y BDD: Docker en marcha **o** la variable `NEUROZEN_TEST_MYSQL` (ver §6).

### 2.2 Secretos (nunca en el repositorio)

```powershell
cd neurozen.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "server=localhost;port=3306;database=neurozen;user=root;password=root"
# Clave JWT: mínimo 32 bytes (256 bits). Genera una aleatoria:
#   PowerShell: [Convert]::ToBase64String((1..48 | % { Get-Random -Max 256 }) -as [byte[]])
#   Bash:       openssl rand -base64 48
dotnet user-secrets set "TokenSettings:Secret" "<pega aquí la clave generada>"
```

Si falta la cadena de conexión o la clave tiene menos de 32 bytes, la API no arranca y lo dice en el error.

### 2.3 Base de datos y migraciones

El esquema se crea con migraciones de EF Core (las antiguas se eliminaron porque el modelo cambió por completo).

- **Si ya generaste `InitialCreate`** en tu copia local (el mensaje «The name 'InitialCreate' is used by an existing
  migration» lo confirma), copia esa carpeta de migraciones a este repositorio en la misma ruta y haz commit.
  Solo es válida si la generaste con el código del módulo 6 o posterior (este backend no cambió el modelo después).
- **Si no**, genérala aquí (no necesita MySQL en marcha gracias a `DesignTimeDbContextFactory`):

```powershell
cd neurozen.API
dotnet ef migrations add InitialCreate --output-dir Shared/Infrastructure/Persistence/EFC/Migrations
```

Al arrancar, la API aplica las migraciones pendientes automáticamente (`Database.MigrateAsync`).
Alternativa manual: `dotnet ef database update` (usa `ConnectionStrings__DefaultConnection` si está definida).

### 2.4 Arrancar

```powershell
dotnet run --project neurozen.API
```

- API: `http://localhost:5059` · Swagger (solo Development): `http://localhost:5059/swagger` · Salud: `/health`.
- Correos en Development: no se envían; se guardan como `.eml` en `neurozen.API/dev-mail/` (ignorado por git).
  Ahí aparece el enlace de «Olvidé mi contraseña».

### 2.5 Seeder del admin y datos de demostración

Se ejecutan al arrancar, después de las migraciones, y nunca modifican cuentas existentes.

| Cuenta (ficticia) | Contraseña | Rol | Cuándo se crea |
|---|---|---|---|
| `admin@neurozen.com` | `Admin2026` | ADMIN | Siempre que `Seed:Admin:Email` y `Seed:Admin:Password` estén configurados (en Development vienen en `appsettings.Development.json`) |
| `ana@neurozen.com` | `Secreta123` | USER | Solo con `Seed:Demo:Enabled = true` (Development) |
| `elena@psico.pe` | `Segura2026` | PSYCHOLOGIST (VERIFIED, colegiatura 45821, lun y mié 09:00-13:00) | Solo con `Seed:Demo:Enabled = true` |
| 3 recursos de bienestar | — | — | Solo con `Seed:Demo:Enabled = true` y la biblioteca vacía |

En producción **no** se usan estas contraseñas: define `Seed__Admin__Email` y `Seed__Admin__Password` con valores
propios (la contraseña debe cumplir la política: 8+ caracteres, letras y números) y deja `Seed__Demo__Enabled` sin
definir.

### 2.6 Variables de entorno (producción)

| Variable | Obligatoria | Ejemplo / nota |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Sí | `server=...;port=3306;database=neurozen;user=...;password=...` |
| `TokenSettings__Secret` | Sí | 32+ bytes aleatorios |
| `TokenSettings__ExpirationMinutes` | No | `120` (por defecto) |
| `Seed__Admin__Email`, `Seed__Admin__Password`, `Seed__Admin__FullName` | Para crear el admin | Valores propios |
| `Cors__AllowedOrigins__0`, `__1`, ... | No | Por defecto: `localhost:5173`, `localhost:4173` y el frontend en Vercel |
| `PasswordReset__FrontendBaseUrl` | Sí en producción | `https://neurozen-web-frontend.vercel.app` |
| `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__From` | Para enviar correos | SMTP del proveedor |
| `ForwardedHeaders__Enabled` | Detrás de un proxy TLS (Render) | `true` |
| `ForwardedHeaders__TrustAllProxies` | No | `false` (por defecto). Si se activa, se exige una justificación explícita porque la app confía en todos los proxies) |
| `ForwardedHeaders__KnownProxies` | No | Lista de IPs de proxy confiables; vacía por defecto |
| `ForwardedHeaders__KnownNetworks` | No | Lista de redes CIDR confiables; vacía por defecto |
| `Database__ServerVersion` | No | `8.0.36-mysql` (evita detectar la versión al arrancar) |
| `Swagger__Enabled` | No | `false` (por defecto fuera de Development) |

## 3. Matriz final de los 35 endpoints

Base: `/api/v1`. Todos con JWT salvo los cuatro de autenticación. ✅ = cumple la especificación; la última columna
indica cualquier diferencia o decisión tomada.

| # | Endpoint | Estado | Diferencia o decisión |
|---|---|---|---|
| 1 | `POST /authentication/sign-in` | ✅ | — |
| 2 | `POST /authentication/sign-up` | ✅ | Responde 201. `role` vacío = USER |
| 3 | `POST /authentication/forgot-password` | ✅ | Límite extra: 3 solicitudes por correo cada 15 min (en memoria; con varias instancias, mover a Redis) |
| 4 | `POST /authentication/reset-password` | ✅ | — |
| 5 | `GET /users/{id}` | ✅ | — |
| 6 | `PUT /users/{id}` | ✅ | Campo ausente o `null` = se conserva; `""` vacía un campo opcional |
| 7 | `PUT /users/{id}/password` | ✅ | Además, límite de intentos por IP |
| 8 | `DELETE /users/{id}` | ✅ | La fila del usuario queda anonimizada (no se borra) para no romper el historial de la otra parte |
| 9 | `GET /professionals` | ✅ | Un psicólogo no verificado recibe 403 (matriz de permisos) |
| 10 | `GET /professionals/{id}` | ✅ | Ídem |
| 11 | `GET /professionals/{id}/available-slots?date=` | ✅ | Horarios de 1 hora; Lima = UTC-5 fijo |
| 12 | `GET /professionals/{id}/reviews` | ✅ | Autor de una cuenta eliminada: «Usuario eliminado» |
| 13 | `POST /professionals/{id}/reviews` | ✅ | — |
| 14 | `GET /professionals/me` | ✅ | — |
| 15 | `PUT /professionals/me` | ✅ | Si no se envía `availability`, se genera («Lun, Mié · 09:00 - 13:00») |
| 16 | `GET /admin/professionals?status=` | ✅ | Sin `status` lista todos |
| 17 | `PATCH /admin/professionals/{id}/verify` | ✅ | Queda auditado en `professional_verification_audits` |
| 18 | `PATCH /admin/professionals/{id}/reject` | ✅ | Solo desde PENDING (409 en otro estado). Auditado |
| 19 | `GET /appointments/types` | ✅ | — |
| 20 | `GET /appointments?patientId=` | ✅ | — |
| 21 | `GET /appointments?professionalId=` | ✅ | — |
| 22 | `GET /appointments/{id}` | ✅ | — |
| 23 | `POST /appointments` | ✅ | La hora debe coincidir con un horario de `available-slots`. Doble reserva bloqueada también por índice único |
| 24 | `PATCH /appointments/{id}/status` | ✅ | Transición prohibida por rol = 403; transición inválida = 400 |
| 25 | `GET /triggers?userId=` | ✅ | — |
| 26 | `POST /triggers` | ✅ | `userId` opcional (se usa el del token; otro id = 403) |
| 27 | `DELETE /triggers/{id}` | ✅ | — |
| 28 | `GET /assessments?userId=` | ✅ | — |
| 29 | `POST /assessments` | ✅ | `completedAt` lo fija el servidor |
| 30 | `GET /reports?patientId=` / `?professionalId=` | ✅ | — |
| 31 | `POST /reports` | ✅ | `createdAt` lo fija el servidor; `weeklyChange` acepta −1000 a 1000; `lastAssessment.level` se recalcula del puntaje |
| 32 | `GET /resource-libraries` | ✅ | Un psicólogo no verificado recibe 403 |
| 33 | `GET /resource-libraries/{id}` y `GET /resources/{id}` | ✅ | — |
| 34 | `POST /subscriptions` | ✅ | `isActive` = `true` (no hay pasarela de pago). Una suscripción por usuario: un segundo POST cambia de plan y responde 200 |
| 35 | `GET /subscriptions/user/{userId}` | ✅ | — |

Endpoints fuera de la especificación que se conservan: `GET /contents/meditations` (cualquier usuario autenticado),
`GET /users/{userId}/dashboard` y `GET|POST /users/{userId}/health_metrics` (solo el dueño), `POST /resource-libraries`
(solo ADMIN) y `GET /health`. Eliminados por inseguros: `GET /users`, `POST /professionals`,
`GET /subscriptions/plan/{id}` y `GET /subscriptions/active/{x}`.

## 4. Cambios de contrato que debe conocer la app móvil

No se mantienen rutas ni formatos antiguos.

**Generales**
1. Todos los ids son números (`long`). El id de usuario ya no es un GUID.
2. Errores: `{ "message": "...", "errors": { "campo": ["..."] } }` (`errors` solo en validaciones).
   Códigos: 400 datos/regla, 401 token ausente/inválido/vencido o credenciales incorrectas en `/authentication`,
   403 sin permiso, 404 no existe o no es tuyo, 409 duplicado/conflicto, 429 demasiados intentos.
3. Fechas en ISO 8601 UTC con `Z` (`2026-10-20T14:00:00Z`). `dateOfBirth` es `YYYY-MM-DD`.
4. JWT HS256 con `sub` (id), `role` (`USER`/`PSYCHOLOGIST`/`ADMIN`), `email` y `exp` (2 h). Header
   `Authorization: Bearer <token>`. Un token de una cuenta eliminada responde 401.
5. Un psicólogo `PENDING`/`REJECTED` solo puede usar `/professionals/me` y su cuenta (`/users/{id}`); el resto, 403.

**Autenticación y cuenta**
6. `sign-in`: `{ username: <correo>, password }` → `{ id, fullName, email, role, token }`. Credenciales incorrectas: 401
   (antes 500).
7. `sign-up`: `{ email, password, fullName, role, acceptedTerms, specialty?, licenseNumber? }` → 201 con el mismo cuerpo
   que sign-in (ya inicia sesión). `acceptedTerms: true` obligatorio; `ADMIN` prohibido.
8. Nuevos: `forgot-password` (siempre 200 `{ "message": "ok" }`), `reset-password`, `PUT /users/{id}`,
   `PUT /users/{id}/password` (204), `DELETE /users/{id}` (204).
9. `GET /users` ya no existe. `GET /users/{id}` solo funciona con el propio id y devuelve el perfil completo
   (`phoneNumber`, `address`, `avatarUrl`, `dateOfBirth`, `ageRange`, `occupation`, `workType`, `createdAt`).

**Profesionales y citas**
10. Profesional: `{ id, userId, name, specialty, bio, experienceYears, price, rating, reviews, availability, image,
    verificationStatus }`. `specialty` en camelCase (`clinical`, `childAdolescent`...). `rating` decimal (4.8),
    `reviews` = cantidad. Sin `licenseNumber` ni `email`. Solo aparecen los VERIFIED. `POST /professionals` ya no existe.
11. Mis citas: `GET /appointments?patientId={miId}` (antes `/appointments/appointments/{patientId}`). Sin citas: `[]`
    (antes 404).
12. Cita: añade `status` (`PENDING|CONFIRMED|COMPLETED|CANCELLED`), `reviewed`, `patientName` y, solo para el psicólogo,
    `patientEmail`. `appointmentType` sigue siendo 1, 2 o 3.
13. Para reservar, la hora debe salir de `GET /professionals/{id}/available-slots?date=YYYY-MM-DD` (hora de Lima,
    `HH:mm`) convertida a UTC. Horario ocupado: 409 «El horario ya no está disponible».
14. Cambios de estado: `PATCH /appointments/{id}/status { status }`. Reseñas: `POST /professionals/{id}/reviews
    { appointmentId, rating, comment }` solo tras una cita COMPLETED.

**Estrés**
15. Trigger: `{ userId, description, category, stressLevel, triggeredAt }` (antes `patientId`, `categoryId`,
    `triggerDateTime`). `category` es texto (`meeting`, `deadline`, `interpersonalConflict`, `workOverload`,
    `technicalProblems`, `difficultClient`, `organizationalChanges`, `other`). `intensity` la calcula el servidor.
    Nuevos: `GET /triggers?userId=` y `DELETE /triggers/{id}`.
16. Nuevos: `/assessments` (10 respuestas 0-4) y `/reports`.

**Recursos y suscripciones**
17. Recurso: añade `thumbnail`, `tags`, `author`, `createdAt`; el detalle añade `content`. `resourceType`:
    `Video|Audio|Article|Exercise` (ya no `Book`). `duration` en minutos.
18. `POST /subscriptions`: el `userId` sale del token; ya no exige correo `@gmail.com`; valida Luhn, vencimiento
    (`MM/AA`) y CVV de 3-4 dígitos. Respuesta y `GET /subscriptions/user/{userId}`: **un objeto** (antes un arreglo) con
    `cardLast4` y `expirationDate`; nunca el número completo ni el CVV. Sin suscripción: 404.
19. Bienestar: `health_metrics` y `dashboard` usan el id numérico y solo funcionan con el propio id.

## 5. Seguridad aplicada (resumen)

BCrypt costo 12 · JWT validado (firma, emisor, audiencia, expiración) · control de dueño (BOLA) en todos los recursos
personales · asignación masiva impedida con DTOs explícitos · `ADMIN` imposible por registro · CORS restringido ·
rate limiting en autenticación · tokens de recuperación hasheados, de un solo uso y de 60 min · sin datos sensibles en
logs (ni contraseñas, ni tokens, ni contenido de salud, ni tarjetas) · PCI DSS: solo últimos 4 dígitos, el CVV nunca se
guarda · Ley 29733: términos obligatorios y borrado/anonimización de datos al eliminar la cuenta · errores sin trazas.

## 6. Pruebas

### 6.1 Ejecutar

```powershell
# Todo (unitarias + integración + BDD). Necesita Docker en marcha, o:
$env:NEUROZEN_TEST_MYSQL = "server=127.0.0.1;port=3306;user=root;password=root"   # MySQL 8 propio
dotnet test

# Solo unitarias (sin MySQL ni Docker)
dotnet test --filter "FullyQualifiedName~Unit"

# Dos veces seguidas para descartar pruebas inestables
dotnet test; dotnet test

# Cobertura (coverlet)
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/coverage" -reporttypes:"Html;TextSummary" -assemblyfilters:"+neurozen.API"

# Prueba de mutación (genera docs/mutation-results.md)
# Windows PowerShell (sin PowerShell 7):
powershell -ExecutionPolicy Bypass -File .\tools\mutation-check.ps1                    # unitarias + integración
powershell -ExecutionPolicy Bypass -File .\tools\mutation-check.ps1 -SkipIntegration   # solo unitarias
# PowerShell 7: pwsh ./tools/mutation-check.ps1
```

Las pruebas de integración crean una base `neurozen_tests_<aleatorio>` por ejecución y la eliminan al terminar.
Si el repositorio tiene migraciones, se aplican; si no, el esquema se crea desde el modelo.

### 6.2 Resumen

| Archivo | Pruebas | Regla que protege | Caminos |
|---|---|---|---|
| `Unit/IAM/PasswordPolicyTests` | 2 (5 casos) | Política de contraseñas | ✔ ✘ |
| `Unit/IAM/UserCommandServiceSignUpTests` | 6 | Rol USER; psicólogo + perfil PENDING en una transacción; sin ADMIN; términos; correo y colegiatura únicos | ✔ ✔ ✘ ✘ ✘ ✘ |
| `Unit/IAM/UserCommandServiceSignInTests` | 2 | JWT con sub/role/exp; mismo error con o sin cuenta (sin enumeración) | ✔ ✘ |
| `Unit/IAM/HashingServiceTests` | 1 | Nunca texto plano (BCrypt 12) | ✔ |
| `Unit/IAM/PasswordResetCommandServiceTests` | 4 (5 casos) | Recuperación sin enumeración; token hasheado; un solo uso; vencimiento | ✘ ✔ ✔ ✘ |
| `Unit/IAM/UserAccountCommandServiceTests` | 1 | Contraseña actual incorrecta = 400, no 401 | ✘ |
| `Unit/Professionals/ProfessionalVerificationCommandServiceTests` | 3 (5 casos) | Verificación auditada; solo desde PENDING; motivo de rechazo 10-300 | ✔ ✘ ✘ |
| `Unit/Professionals/ProfessionalProfileCommandServiceTests` | 3 | Cambio de colegiatura o reenvío → PENDING; rangos del perfil | ✔ ✔ ✘ |
| `Unit/Professionals/ProfessionalQueryServiceTests` | 3 | Directorio solo VERIFIED (repositorio real en memoria); horarios sin ocupados ni pasados; día fuera de agenda | ✔ ✔ ✘ |
| `Unit/Appointments/AppointmentCommandServiceTests` | 9 (11 casos) | Reserva válida; profesional no verificado; fecha pasada; doble reserva; máquina de estados y permisos | ✔ ✘ ✘ ✘ ✔ ✘ ✔ ✘ ✘ |
| `Unit/Stress/StressTriggerCommandServiceTests` | 2 (8 casos) | Intensidad calculada en el servidor; fecha futura y descripción corta | ✔ ✘ |
| `Unit/Stress/AssessmentTests` | 3 (11 casos) | PSS-10 con preguntas 4, 5, 7, 8 invertidas; límites 13/14/26/27; 10 respuestas 0-4 | ✔ ✔ ✘ |
| `Unit/Stress/ReportCommandServiceTests` | 3 (5 casos) | Informe solo en cita propia activa; uno por cita | ✔ ✘ ✘ |
| `Unit/Reviews/ReviewCommandServiceTests` | 2 (3 casos) | Rating recalculado y cita marcada; solo COMPLETED y una vez | ✔ ✘ |
| `Unit/Subscriptions/SubscriptionCommandServiceTests` | 2 (3 casos) | PCI: solo últimos 4, nunca CVV; Luhn y vencimiento | ✔ ✘ |
| `Integration/Security/SecurityIntegrationTests` | 10 (11 casos) | 401 sin token / vencido / alterado; BOLA 403; cita ajena 404; panel admin 403; psicólogo pendiente 403; asignación masiva; sin datos sensibles; errores JSON; rate limiting 429 | ✘ ✘ ✘ ✘ ✘ ✘ ✔ ✔ ✘ ✘ |
| `Integration/Flows/BusinessFlowIntegrationTests` | 8 | Ciclo del psicólogo; rechazo y reenvío; reservar-confirmar-atender-calificar; doble reserva 409; informe solo para su psicólogo; recuperación de contraseña; borrado de cuenta; suscripción solo con últimos 4 | ✔ ✔ ✔ ✘ ✔ ✔ ✔ ✔ |
| `BDD/Features/VerificacionDePsicologos.feature` | 4 escenarios | Aprobación, rechazo con motivo, pendiente fuera del directorio, rechazo sin motivo | ✔ ✔ ✘ ✘ |
| `BDD/Features/ReservaDeSesion.feature` | 3 escenarios | Reserva exitosa, horario ocupado, profesional no verificado | ✔ ✘ ✘ |
| `BDD/Features/CicloDeLaCita.feature` | 5 escenarios | Confirmar, cancelar, paciente no puede atender, calificar solo tras atender | ✔ ✔ ✘ ✘ ✔ |
| `BDD/Features/SeguridadDeLaCuenta.feature` | 3 escenarios | Enlace válido, enlace vencido, contraseña actual incorrecta | ✔ ✘ ✘ |
| **Total** | **46 unitarias · 18 integración · 15 escenarios BDD** | | |

### 6.3 Prueba de mutación

`tools/mutation-check.ps1` introduce cada error, ejecuta las pruebas indicadas, restaura el archivo y guarda la
evidencia en `docs/mutation-results.md`.

| Id | Mutación | Debe detectarla |
|---|---|---|
| M1 | Aceptar `ADMIN` en el registro | `SignUp_WithAdminRole_ShouldThrowValidation`, `BusinessError_ShouldReturnJsonMessageNot500` |
| M2 | Mostrar psicólogos PENDING en el directorio | `Directory_ShouldOnlyReturnVerifiedProfessionals`, `PsychologistLifecycle_RegisterVerifyAppearInDirectory` |
| M3 | Permitir la doble reserva (quitar la comprobación) | `Book_AlreadyTakenSlot_ShouldThrowConflict` (en integración el índice único sigue dando 409: defensa en profundidad) |
| M4 | No invertir las preguntas 4, 5, 7 y 8 | `Assessment_ShouldRecalculateScoreWithReversedQuestions4578` |
| M5 | Quitar el control de dueño en `GET /users/{id}` | `GetUser_OfAnotherUser_ShouldReturn403` |
| M6 | Guardar el CVV (en la columna del vencimiento) | `Subscription_ShouldStoreOnlyLast4AndNeverCvv`, `Subscription_CreateAndGet_ShouldOnlyExposeLast4` |

### 6.4 CI

`.github/workflows/main_neurozen-api.yml`: MySQL 8 como servicio, `dotnet test` de toda la suite con resultados TRX y
cobertura (resumen en la página de la ejecución y artefacto `test-results`). Los workflows de despliegue a staging y
producción solo corren si este pasa.

## 7. Limitaciones conocidas

- Sin pasarela de pago: la suscripción validada se considera activa. En producción, delegar en Culqi/Niubiz/Stripe y
  guardar solo su token.
- El límite de recuperación por correo vive en memoria (una instancia). Con varias instancias, usar Redis.
- La verificación de colegiatura es manual (el CPsP no tiene API); no se hace scraping.

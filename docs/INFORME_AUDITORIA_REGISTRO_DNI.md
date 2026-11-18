# NeuroZen Backend: auditoría, correcciones y registro con DNI

Fecha: 17/11/2026 · Alcance: solo el código fuente del backend y sus pruebas. **No se modificó** Dockerfile,
docker-compose, `render.yaml`, `railway.toml`, `nixpacks.toml`, workflows de GitHub Actions, `appsettings*.json`,
variables de entorno, URLs ni credenciales.

---

## A. Auditoría

### Arquitectura encontrada

| Aspecto | Hallazgo |
|---|---|
| Lenguaje / framework | C# 13, ASP.NET Core 9 (`net9.0`), controladores REST |
| Patrón | DDD con *bounded contexts* (IAM, Professionals, Appointments, Stress, Wellness, ResourcesLibrary, Subscriptions, Shared) y capas Domain / Application / Infrastructure / Interfaces (REST + ACL) |
| Persistencia | EF Core 9 + Pomelo MySQL 8, migraciones en `Shared/Infrastructure/Persistence/EFC/Migrations`, convención snake_case, `IUnitOfWork` con transacciones explícitas |
| Autenticación | JWT HS256 (`sub`, `role`, `email`, `sst` = security stamp, `jti`, `exp` 120 min), validado contra la BD en cada petición (cuenta eliminada o stamp distinto = 401). No hay refresh tokens. |
| Contraseñas | BCrypt costo 12, política de 8 o más caracteres con letra y número, límite de 72 bytes, verificación con hash ficticio para no revelar si el correo existe |
| Autorización | Política global "autenticado", roles `USER` / `PSYCHOLOGIST` / `ADMIN`, políticas `VerifiedIfPsychologist` y `VerifiedPsychologist`, control de dueño (BOLA) en los servicios |
| Recuperación | Token aleatorio de 32 bytes, guardado como SHA-256, de un solo uso, 60 min, con límite por correo y por IP |
| Errores | `GlobalExceptionHandler` con el formato `{ message, errors }`, 500 genérico sin traza, índices únicos traducidos a 409 |
| Pruebas | xUnit + Moq (unitarias), WebApplicationFactory + MySQL real (integración) y Reqnroll (BDD) |
| Dependencias | BCrypt.Net-Next, Cortex.Mediator (solo interfaces de eventos), Humanizer, Swashbuckle, JwtBearer, HealthChecks |

### Módulos inspeccionados

IAM (registro, inicio de sesión, recuperación, perfil, eliminación de cuenta, JWT, hashing, seeders), Professionals
(perfil, especialidades, verificación administrativa, auditoría de decisiones, reseñas, política de verificado),
Appointments (reserva, estados, acceso por participante), Stress (reportes compartidos con el psicólogo), Shared
(errores, UoW, convenciones, migraciones) y Program.cs (pipeline, CORS, rate limiting y cabeceras de proxy).

### Sistema de verificación existente (se conservó, no se creó uno paralelo)

- Estado: `Professional.VerificationStatus` = `PENDING` | `VERIFIED` | `REJECTED`. **REJECTED cumple el rol de
  "observado"**: lleva `rejectionReason` (10-300 caracteres), el psicólogo lo ve en `GET /professionals/me` y al
  corregir su perfil (`PUT /professionals/me`) vuelve a PENDING. No se agregó un estado nuevo.
- Endpoints admin: `GET /admin/professionals?status=`, `PATCH /admin/professionals/{id}/verify` y
  `PATCH .../reject` (solo `ADMIN`, solo desde PENDING). Cada decisión queda en `professional_verification_audits`.
- Restricciones: un psicólogo no verificado recibe 403 en el directorio, recursos, listado de citas y reportes, y no se
  le puede reservar (404).

### Problemas detectados

| # | Tipo | Problema | Estado |
|---|---|---|---|
| 1 | Funcionalidad faltante | El registro no pedía DNI | ✅ Corregido |
| 2 | Cumplimiento (Ley 29733) | Solo se guardaba `AcceptedTermsAt`: sin versión del documento ni constancia separada de la Política de Privacidad | ✅ Corregido |
| 3 | Seguridad / lógica | **Bypass de verificación:** un psicólogo VERIFIED podía cambiar su nombre (el dato que el admin coteja con el CPsP) mediante `PUT /users/{id}` y seguía VERIFIED. `PUT /professionals/me` sí lo devolvía a PENDING. | ✅ Corregido |
| 4 | Autorización | Un psicólogo que vuelve a PENDING (por cambiar la colegiatura o el nombre) podía **confirmar/completar** citas y **leer la cita con el correo del paciente** (`GET /appointments/{id}`); solo se bloqueaba el listado | ✅ Corregido |
| 5 | Validación | El nombre aceptaba dígitos y símbolos (`"Ana <script>"`, `"Ana Torres 2"`) y no normalizaba espacios internos | ✅ Corregido |
| 6 | Validación | La colegiatura usaba `\d`, que en .NET acepta dígitos no ASCII (`"٤٥٨٢١"`) | ✅ Corregido |
| 7 | Validación | `dateOfBirth` aceptaba basura después de la fecha (`"2000-01-01xyz"`) | ✅ Corregido |
| 8 | Código muerto | `LoggingCommandBehavior` estaba vacío, en un namespace de otro proyecto (`LearningCenterPlatform`) y no se registraba | ✅ Eliminado |
| 9 | Contrato | No existía `confirmPassword` en el backend | ✅ Agregado (opcional, no se guarda) |

No se encontraron inyección SQL (todo pasa por LINQ/EF), mass assignment (DTOs explícitos), escalada a ADMIN en el
registro, stack traces en las respuestas ni contraseñas en las respuestas.

---

## B. Correcciones

| Problema | Archivo(s) | Causa | Solución | Resultado |
|---|---|---|---|---|
| Bypass de verificación por cambio de nombre | `IAM/.../UserAccountCommandService.cs`, `Professionals/Domain/.../Professional.cs`, `Professionals/Interfaces/ACL/*` | La regla "si cambia el nombre, se vuelve a PENDING" solo existía en el servicio de Professionals | Si un `PSYCHOLOGIST` cambia `fullName` en `PUT /users/{id}`, IAM llama a `IProfessionalsContextFacade.ReturnToPendingVerificationAsync` en la misma unidad de trabajo. Nuevo método de dominio `Professional.ReturnToPendingVerification` (VERIFIED/REJECTED → PENDING, limpia verifiedAt/By/motivo; nunca aprueba) | Prueba unitaria e integración `VerifiedPsychologist_ChangingNameThroughUsersEndpoint_ShouldReturnToPending` ✅ |
| Psicólogo no verificado opera citas | `Appointments/Application/Internal/AppointmentAccess.cs`, `AppointmentCommandService.cs`, `AppointmentQueryService.cs` | Solo `GET /appointments?professionalId=` comprobaba `IsBookable` | Nuevo `EnsureVerifiedProfessionalAsync`: 403 «Tu cuenta profesional aún no está verificada» al confirmar, completar o leer la cita. Puede **cancelar** (para liberar al paciente), pero no recibe el correo del paciente | Integración `PsychologistBackToPending_CannotConfirmOrReadAppointment` ✅ |
| Nombre con caracteres inválidos | `IAM/Domain/Model/ValueObjects/PersonName.cs`, `Shared/Domain/Model/Text/TextNormalization.cs` | Solo se exigían 2 palabras y 120 caracteres como máximo | Se admiten letras de cualquier alfabeto (tildes, ñ, ü), marcas combinantes, espacio, `'`, `’`, `-` y `.`. Se normaliza con trim y espacios simples. Se aplica en el registro, en `PUT /users/{id}` y en `PUT /professionals/me` | 12 casos unitarios ✅ |
| Colegiatura con dígitos Unicode | `Professional.cs` | `\d` en .NET coincide con cualquier dígito Unicode | Regex `^[0-9]{3,6}$` | Unitaria ✅ |
| `dateOfBirth` con basura | `UserAccountCommandService.cs` | Se tomaban los primeros 10 caracteres sin mirar el resto | Solo `YYYY-MM-DD` o fecha-hora ISO (`T` o espacio en la posición 10) | Unitaria ✅ |
| Código muerto | `Shared/Infrastructure/Mediator/Cortex/Configuration/LoggingCommandBehavior.cs` | Restos de una plantilla | Archivo eliminado (Cortex.Mediator sigue en uso por `IEvent`/`IEventHandler`) | Compila sin advertencias ✅ |

---

## C. Registro de usuarios

**Contrato `POST /api/v1/authentication/sign-up` (los campos nuevos están en negrita):**

```json
{
  "fullName": "Lucía Mendoza",
  "dni": "12345678",
  "email": "lucia@correo.com",
  "password": "Secreta123",
  "confirmPassword": "Secreta123",
  "role": "USER",
  "acceptedTerms": true,
  "acceptedPrivacyPolicy": true
}
```

Los campos nuevos son `dni`, `confirmPassword` y `acceptedPrivacyPolicy`. La respuesta **no cambia**:
`201 { id, fullName, email, role, token }`, sin DNI.

| Campo | Reglas |
|---|---|
| `fullName` | Obligatorio; trim y espacios simples; nombre y apellido; 120 caracteres como máximo; solo letras, espacios, `'`, `-`, `.` |
| `dni` | **Obligatorio**; exactamente 8 dígitos ASCII; sin letras, símbolos ni espacios internos (se recortan los de los extremos). **Solo formato**: no se consulta RENIEC ni ninguna API o BD externa, y el backend no afirma que el DNI sea auténtico. Único: 409 «Ya existe una cuenta con este DNI» (el mensaje no incluye el número) |
| `email` | Obligatorio, formato válido, trim y minúsculas (estrategia existente), único (409) |
| `password` | 8 caracteres o más, al menos una letra y un número, BCrypt; nunca se devuelve |
| `confirmPassword` | Opcional. Si se envía, debe coincidir (400 en `confirmPassword`). **No se persiste ni se devuelve** |
| `acceptedTerms` | Obligatorio `true` |
| `acceptedPrivacyPolicy` | Si se envía, debe ser `true`. Si **no** se envía (frontend actual con una sola casilla «Términos y Política de Privacidad»), se usa `acceptedTerms` para ambos registros. No hay casillas premarcadas en el backend |

No se pide información clínica en el registro.

**Cambios por capa**

- Modelo: `User.Dni` (`char(8)`, nullable, índice único `ux_users_dni`). Nueva entidad `UserConsent`
  (`user_consents`: `user_id`, `type` = `TERMS` | `PRIVACY_POLICY`, `version`, `accepted_at`). La FK es `Restrict`.
- DTO/comandos: `SignUpResource` y `SignUpCommand` + `Dni`, `ConfirmPassword`, `AcceptedPrivacyPolicy` (opcionales al
  final, así no se rompe el binding).
- Servicio: `UserCommandService.Handle(SignUpCommand)` valida todo y reporta todos los errores juntos. Crea el usuario,
  los dos consentimientos y, si es psicólogo, el perfil PENDING **en una sola transacción**.
- Configuración: `LegalSettings` (sección opcional `Legal:TermsVersion` / `Legal:PrivacyPolicyVersion`, por defecto
  `"1.0"`). No se modificó `appsettings.json`.
- Endpoints: mismas rutas. `GET /users/{id}` (solo el dueño) añade `"dni"` (`""` en cuentas antiguas). El DNI no es
  editable por `PUT /users/{id}`.
- Migración `20261117203341_AddDniAndUserConsents`: **solo aditiva** (agrega una columna nullable, un índice y una
  tabla). No modifica ni borra datos. Se probó sobre una BD con usuarios previos: quedan con `dni = NULL` y el índice
  único admite varios NULL.

---

## D. Registro de psicólogos

- Campos: los mismos del usuario más `role: "PSYCHOLOGIST"`, `specialty` y `licenseNumber`.
- Especialidades (sin cambios, catálogo cerrado; cualquier otro valor da 400): `clinical`, `organizational`,
  `educational`, `family`, `childAdolescent`, `health`, `social`, `neuropsychology`.
- Colegiatura CPsP: obligatoria, de 3 a 6 dígitos ASCII y única (409). El backend **no** afirma que esté habilitada:
  lo revisa el admin en el portal del CPsP.
- Estado tras el registro: `PENDING` (sin cambios). El frontend sigue redirigiendo a su pantalla de pendiente.
- Panel admin: `AdminProfessionalResource` añade `"dni"` (solo para `ADMIN`, como apoyo para la revisión de identidad).
  El directorio público (`ProfessionalResource`) **no** incluye DNI.
- Acceso: mientras está PENDING/REJECTED recibe 403 en las funciones profesionales (ahora también al confirmar,
  completar o leer una cita), no se le puede reservar, no puede aprobarse a sí mismo (403, por rol) y un USER tampoco
  puede aprobarlo (403). Solo el admin lo aprueba. No existe aprobación automática.

---

## E. Seguridad

| Área | Estado |
|---|---|
| Autenticación | Sin cambios funcionales: JWT validado (firma, emisor, audiencia, expiración, algoritmo fijo) + security stamp |
| Autorización | Se cerraron dos brechas (§B): bypass del nombre y operaciones con citas de un psicólogo no verificado. ADMIN sigue siendo imposible por registro. El rol y el estado no se pueden asignar por el body (DTOs explícitos) |
| Contraseñas | BCrypt; `confirmPassword` no se guarda; ninguna respuesta incluye hash ni contraseña (verificado por la prueba existente) |
| DNI | No se incluye en sign-up, sign-in, JWT, directorio, citas ni reseñas. Solo aparece en `GET /users/{id}` (el propio dueño) y en el panel ADMIN. No se registra en logs (ningún `Log*` lo incluye; el logging sensible de EF sigue desactivado). El 409 por DNI duplicado no repite el número. Al eliminar la cuenta, el DNI se borra (`Anonymize`) y queda libre |
| Datos personales | Los consentimientos solo guardan `user_id`, tipo, versión y fecha. Se conservan tras la anonimización como constancia de la base legal |
| Excepciones | `ux_users_dni` se tradujo a 409 en `GlobalExceptionHandler` (también ante registros concurrentes). Los 500 siguen siendo genéricos |

---

## F. Pruebas

Entorno real de esta ejecución: Linux, **.NET SDK 10.0.115** compilando `net9.0` (con `DOTNET_ROLL_FORWARD=Major`,
porque el SDK 9 no se pudo descargar en este entorno) y **MySQL 8.0.46** local (`NEUROZEN_TEST_MYSQL`).
Antes de los cambios: **131/131** en verde. Después: **208/208** en verde en **tres ejecuciones consecutivas**.
Ninguna prueba se eliminó ni se desactivó. Las pruebas existentes de registro solo se adaptaron para enviar `dni`.

| Prueba | Resultado | Observación |
|---|---|---|
| Registro de usuario (correcto, DNI y 2 consentimientos en MySQL, DNI ausente en sign-up/sign-in) | Aprobado | Unitaria + integración |
| Nombre vacío / con símbolos / demasiado largo; nombres legítimos (ñ, tildes, `-`, `'`, `.`) | Aprobado | 12 casos |
| DNI vacío, 7 y 9 dígitos, letras, símbolos, espacio interno y dígitos Unicode | Aprobado | Unitaria (8) + integración (6) |
| DNI duplicado | Aprobado | 409 sin revelar el número (unitaria + MySQL) |
| Correo inválido / duplicado / normalización | Aprobado | |
| Contraseña < 8, sin números, sin letras; hash BCrypt | Aprobado | |
| Confirmación incorrecta | Aprobado | 400 `confirmPassword` |
| Consentimiento obligatorio no registrado (`acceptedTerms=false`, `acceptedPrivacyPolicy=false`) | Aprobado | No se crea la cuenta ni los consentimientos |
| Contrato anterior del frontend + `dni` | Aprobado | Compatibilidad |
| Registro de psicólogo (correcto, DNI inválido, correo duplicado, especialidad vacía/no permitida, colegiatura vacía/inválida, contraseña inválida, las 8 especialidades) | Aprobado | |
| Estado PENDING tras el registro, visible al admin con DNI, sin DNI en el directorio | Aprobado | Integración |
| Restricción mientras está pendiente (directorio, citas, reserva) | Aprobado | 403 / 404 |
| Autoaprobación e intento de aprobación por un USER | Aprobado | 403; sigue PENDING |
| Acceso habilitado tras la aprobación | Aprobado | |
| Cambio de nombre por `/users/{id}` → PENDING | Aprobado | Corrección del bypass |
| Psicólogo de vuelta en PENDING: confirmar/leer cita → 403, cancelar → 200 sin correo del paciente | Aprobado | Corrección |
| Eliminación de cuenta: DNI borrado y liberado, consentimientos conservados | Aprobado | |
| Inicio de sesión | Aprobado | Pruebas existentes |
| Recuperación de contraseña | Aprobado | Integración + BDD existentes |
| Verificación administrativa (aprobar, rechazar con motivo, motivo insuficiente) | Aprobado | BDD + integración existentes |
| Gestión de citas (reserva, doble reserva, confirmar, completar, cancelar, reseña) | Aprobado | BDD + integración existentes |
| Gestión de perfiles / seguridad (401, 403, BOLA, rol en el body, rate limit) | Aprobado | Existentes |
| Migración sobre una BD con datos previos | Aprobado | Manual: `dotnet ef database update` desde `ProfessionalConcurrencyToken` con 2 usuarios previos |

Comandos:

```bash
export NEUROZEN_TEST_MYSQL="server=127.0.0.1;port=3306;user=root;password=root"
dotnet test
```

---

## G. Archivos modificados

**Nuevos**
- `IAM/Domain/Model/ValueObjects/Dni.cs`: reglas de formato del DNI.
- `IAM/Domain/Model/Entities/UserConsent.cs`: constancia de consentimiento (tipo, versión, fecha).
- `IAM/Domain/Repositories/IUserConsentRepository.cs`, `IAM/Infrastructure/Persistence/EFC/Repositories/UserConsentRepository.cs`, `IAM/Infrastructure/Persistence/EFC/Configuration/UserConsentConfiguration.cs`: persistencia de los consentimientos.
- `IAM/Application/Internal/Configuration/LegalSettings.cs`: versiones vigentes de los documentos.
- `Shared/Domain/Model/Text/TextNormalization.cs`: normalización de espacios compartida.
- `Shared/Infrastructure/Persistence/EFC/Migrations/20261117203341_AddDniAndUserConsents(.Designer).cs`: migración aditiva.
- `docs/INFORME_AUDITORIA_REGISTRO_DNI.md`: este informe.
- Pruebas: `Integration/Registration/RegistrationIntegrationTests.cs` y `Unit/Professionals/ProfessionalReturnToPendingTests.cs`.

**Modificados**
- `IAM/Domain/Model/Aggregates/User.cs`: propiedad `Dni`; se borra al anonimizar.
- `IAM/Domain/Model/ValueObjects/PersonName.cs`: caracteres permitidos y normalización.
- `IAM/Domain/Model/Commands/AuthCommands.cs`, `IAM/Interfaces/REST/Resources/AuthenticationResources.cs`, `IAM/Interfaces/REST/Transform/AuthenticationAssemblers.cs`: nuevos campos del registro.
- `IAM/Application/Internal/CommandServices/UserCommandService.cs`: validaciones, duplicado de DNI y consentimientos en la transacción.
- `IAM/Application/Internal/CommandServices/UserAccountCommandService.cs`: corrección del bypass de verificación, normalización del nombre y validación de la fecha.
- `IAM/Domain/Repositories/IUserRepository.cs`, `IAM/Infrastructure/Persistence/EFC/Repositories/UserRepository.cs`: `ExistsByDniAsync`.
- `IAM/Infrastructure/Persistence/EFC/Configuration/UserConfiguration.cs`, `.../Extensions/ModelBuilderExtensions.cs`: columna e índice del DNI; registro de `UserConsent`.
- `IAM/Interfaces/REST/Resources/UserResource.cs`, `.../Transform/UserResourceFromEntityAssembler.cs`: `dni` en la cuenta propia.
- `IAM/Interfaces/ACL/IIamContextFacade.cs`, `.../Services/IamContextFacade.cs`: `UserSummary.Dni` para el panel admin.
- `Professionals/Domain/Model/Aggregates/Professional.cs`: `ReturnToPendingVerification`; regex ASCII de la colegiatura.
- `Professionals/Interfaces/ACL/IProfessionalsContextFacade.cs`, `.../Services/ProfessionalsContextFacade.cs`: `ReturnToPendingVerificationAsync`.
- `Professionals/Application/Internal/CommandServices/ProfessionalProfileCommandService.cs`: normalización del nombre.
- `Professionals/Application/Internal/CommandServices/ProfessionalVerificationCommandService.cs`, `.../QueryServices/ProfessionalQueryService.cs`, `Domain/Model/ReadModels/ProfessionalDetails.cs`, `Interfaces/REST/Resources/ProfessionalProfileResources.cs`, `Interfaces/REST/Transform/ProfessionalResourceFromEntityAssembler.cs`: `dni` en el recurso admin.
- `Appointments/Application/Internal/AppointmentAccess.cs`, `CommandServices/AppointmentCommandService.cs`, `QueryServices/AppointmentQueryService.cs`: restricción al psicólogo no verificado.
- `Shared/Infrastructure/Interfaces/ASP/ErrorHandling/GlobalExceptionHandler.cs`: mensaje 409 para `ux_users_dni`.
- `Program.cs`: registro de `LegalSettings` y `IUserConsentRepository` (inyección de dependencias; no es configuración de despliegue).
- `Migrations/AppDbContextModelSnapshot.cs`: snapshot actualizado.
- `docs/ENTREGA_FINAL.md`: contrato de sign-up actualizado (punto 7 de §4).
- Pruebas: `Integration/Support/TestApi.cs`, `Integration/Security/SecurityIntegrationTests.cs`, `Unit/IAM/UserCommandServiceSignUpTests.cs`, `UserCommandServiceSignInTests.cs`, `UserAccountCommandServiceTests.cs`.

**Eliminados**
- `Shared/Infrastructure/Mediator/Cortex/Configuration/LoggingCommandBehavior.cs`: código muerto.

---

## H. Pendientes

### Solucionados
Todos los puntos 1 a 9 de §A.

### Parcialmente solucionados
- **Consentimiento separado de la Política de Privacidad:** el backend ya guarda constancias separadas y acepta
  `acceptedPrivacyPolicy`. Mientras el frontend envíe una sola casilla, se registra la misma decisión en ambos
  documentos (compatibilidad). La separación real depende de que el frontend muestre dos casillas.
- **Cuentas antiguas sin DNI:** quedan con `dni = NULL`. No se inventaron ni forzaron datos. Si se quiere exigirlo,
  hace falta un flujo para completarlo (decisión de producto).

### Requieren cambios en el frontend
1. Agregar el campo **DNI** (8 dígitos) en los formularios de registro de usuario y de psicólogo y enviarlo como
   `"dni"`. **Sin él, el registro responde 400** (`errors.dni`). Es el único cambio que rompe el contrato y lo exige
   el requerimiento.
2. (Recomendado) Enviar `confirmPassword` y mostrar el error `errors.confirmPassword`.
3. (Recomendado) Separar las casillas y enviar `acceptedTerms` y `acceptedPrivacyPolicy`.
4. Mostrar el 409 «Ya existe una cuenta con este DNI» en el campo DNI.
5. (Opcional) Mostrar `dni` en la vista de perfil (`GET /users/{id}`) y en el panel admin
   (`GET /admin/professionals`).
6. Un psicólogo que vuelve a PENDING ahora recibe 403 al abrir o confirmar citas: conviene redirigirlo a la pantalla
   de "cuenta pendiente", como ya se hace con el listado.

### Requieren intervención administrativa o legal
- Definir las versiones reales de los Términos y de la Política de Privacidad y configurarlas con
  `Legal__TermsVersion` y `Legal__PrivacyPolicyVersion` (por defecto `1.0`). El backend no redacta textos legales.
- Evaluar con asesoría legal si el tratamiento de datos de salud (evaluaciones de estrés, reportes) necesita un
  **consentimiento específico** adicional (Ley 29733, D.S. 016-2024-JUS). El modelo `ConsentType` permite agregarlo,
  pero no se inventó una casilla ni un texto.
- Aplicar la migración en cada entorno. La API lo hace sola al arrancar (`MigrateAsync`); no se ejecutó ningún
  despliegue.

### Requieren intervención externa / infraestructura (solo se documentan, no se tocaron)
- `nixpacks.toml` declara `dotnet-sdk_8`, pero el proyecto es `net9.0`. Si alguna vez se usa el builder Nixpacks,
  fallará. Hoy Railway usa el Dockerfile.
- `railway.toml` define `startCommand = "dotnet neurozen.API.dll"`, que reemplaza el `ENTRYPOINT` del Dockerfile
  (el que respeta `$PORT`). La app escucharía en `ASPNETCORE_URLS` (`:10000`) y no en el puerto que asigna Railway.
  Conviene revisarlo.
- El límite de recuperación de contraseña es en memoria (ya documentado): con varias instancias habría que llevarlo
  a un almacenamiento compartido.
- Estas pruebas se ejecutaron con el SDK de .NET 10 apuntando a `net9.0`. Conviene repetir `dotnet test` con el SDK 9
  del equipo o en el CI.

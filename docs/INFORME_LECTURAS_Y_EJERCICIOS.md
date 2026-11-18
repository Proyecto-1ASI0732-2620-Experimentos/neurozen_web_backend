# NeuroZen Backend: 50 lecturas, 20 ejercicios y administración de la biblioteca

Fecha: 18/11/2026 · Fuente: `NeuroZen_Lecturas_y_Ejercicios.json` (70 recursos con textos originales).

## 1. Qué se agregó

| Cambio | Detalle |
|---|---|
| **50 lecturas (`Article`) y 20 ejercicios (`Exercise`)** | Migración `20261118170834_AddReadingsAndExercisesToResourceLibrary`. El texto completo va en `content` (texto plano con saltos de línea). 3 ejercicios incluyen un video de YouTube de apoyo en `contentUrl` (enlaces ya revisados en el PDF) |
| **Planes** | Básico: 2 lecturas (*Cómo reconocer las señales del estrés laboral*, *Pausas activas: por qué cinco minutos cambian tu jornada*) y 2 ejercicios (*Respiración diafragmática para principiantes*, *Pausa activa de 5 minutos*). El resto queda en Avanzado. Si en esa base ya hay 2 recursos Básico de ese tipo, los nuevos entran como Avanzado (se respeta "1-2 de cada tipo") |
| **Vista de administración** | `GET /api/v1/admin/resource-libraries` y `GET /api/v1/admin/resource-libraries/{id}` (solo ADMIN): todos los recursos, sin filtro de plan, con `content`, `requiredPlan` (1/2/3) y `requiredPlanName` (Básico/Avanzado/Profesional). Sirven para llenar la tabla y el formulario de edición |
| **Respuestas de `POST` y `PATCH`** | Devuelven la vista de administración (incluye `requiredPlan`). Solo se agregaron campos; no se quitó ninguno |
| **Ejemplos de demo locales** | Pasan a Avanzado, para que los lugares de Básico sean del contenido real |

Lo que **ya existía** y el panel admin puede usar desde ahora:
- `POST /api/v1/resource-libraries` acepta `resourceType` = `Video` | `Audio` | `Article` | `Exercise`, `content` y
  `requiredPlan`.
- `PATCH /api/v1/resource-libraries/{id}` edita todo excepto el tipo.

**El problema que viste** ("Lectura 0 / Ejercicios 0" y sin selector de tipo ni de plan) era de **datos y de
frontend**:
- No había lecturas ni ejercicios cargados. Esta migración los carga.
- El formulario del panel admin no ofrece elegir el tipo ni el plan. Hay que agregarlo en el frontend (ver
  `PROMPT_FRONTEND_ADMIN_LECTURAS.md`).

## 2. Garantías de la migración

- **Idempotente:** inserta cada recurso solo si no existe otro con el mismo título. No duplica ni modifica filas
  existentes.
- **Texto exacto:** comillas y barras se escapan para MySQL. El archivo no tiene saltos de línea literales, así que el
  texto queda igual aunque se compile en Windows (sin `\r`).
- **Reversible sin perder cambios:** `Down()` borra solo las filas que insertó y que nadie editó (mismo título, tipo y
  contenido). Lo que editó el admin se conserva.
- No hay cambios de esquema. Las migraciones están excluidas de Sonar (`sonar.exclusions`), así que no afectan el
  quality gate.

## 3. Pruebas

**260/260 ✅ en tres ejecuciones** (antes 252; 8 pruebas nuevas, ninguna eliminada).
Archivo: `neurozen.API.Tests/Integration/ResourcesLibrary/ReadingsAndExercisesTests.cs`.

| Prueba | Resultado |
|---|---|
| Se cargan 50 lecturas y 20 ejercicios, todos con contenido y duración válida; 2 de cada tipo en Básico | ✅ |
| El contenido guardado es idéntico al original (saltos de línea, tildes, sin `\r`) | ✅ |
| Usuario Básico: ve 2 lecturas y 2 ejercicios y lee su texto; una lectura Avanzada da 403 sin `content` | ✅ |
| Usuario Avanzado: ve las 50 lecturas y los 20 ejercicios | ✅ |
| Aplicar de nuevo no duplica; con 2 Básico ya ocupados, la lectura entra como Avanzado | ✅ |
| `Down` conserva la lectura editada por el admin; `Up` restaura las demás | ✅ |
| Vista admin: lista todo con su plan, detalle con `requiredPlanName`, 403 para USER, 404 si no existe | ✅ |
| El admin crea una lectura Básica y un ejercicio Profesional; tipo o plan inválido da 400 | ✅ |
| Manual en MySQL: base con un artículo Básico previo → quedan 2 lecturas Básico en total; 83 recursos; al revertir quedan 13 (se conserva el del admin) | ✅ |

## 4. Sobre el aviso «Cambia la contraseña» de Google

**No es un error del backend.** Es el **Administrador de contraseñas de Chrome**: compara la contraseña que acabas de
escribir con listas públicas de contraseñas filtradas. Las cuentas de demostración usan contraseñas simples y públicas
(`Admin2026`, `Secreta123`, `Segura2026`, que además están escritas en `appsettings.Development.json` y en la
documentación), y por eso Chrome las detecta.

- **En local o en pruebas** puedes ignorarlo.
- **En producción es importante:** el admin **no** debe usar `Admin2026`.
  - Si la cuenta ya existe, cambia su contraseña con `PUT /api/v1/users/{id}/password` (o desde el perfil). El seeder
    nunca modifica cuentas existentes.
  - Si se crea por primera vez, define `Seed__Admin__Password` en Render con una contraseña fuerte y única.
  - No hay que cambiar nada en el código.

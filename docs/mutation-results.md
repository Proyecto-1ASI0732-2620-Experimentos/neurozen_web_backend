# Resultado de la prueba de mutaciÃ³n

Fecha: 2026-11-04 06:38  
Modo: unitarias + integraciÃ³n

| Id | MutaciÃ³n introducida | Pruebas ejecutadas | Resultado |
|---|---|---|---|
| M1 | Aceptar ADMIN en el registro | `FullyQualifiedName~SignUp_WithAdminRole_ShouldThrowValidation|FullyQualifiedName~BusinessError_ShouldReturnJsonMessageNot500` | DETECTADA |
| M2 | Mostrar psicÃ³logos PENDING en el directorio | `FullyQualifiedName~Directory_ShouldOnlyReturnVerifiedProfessionals|FullyQualifiedName~PsychologistLifecycle_RegisterVerifyAppearInDirectory` | DETECTADA |
| M3 | Permitir la doble reserva (sin comprobar el horario) | `FullyQualifiedName~Book_AlreadyTakenSlot_ShouldThrowConflict` | DETECTADA |
| M4 | No invertir las preguntas 4, 5, 7 y 8 del test | `FullyQualifiedName~Assessment_ShouldRecalculateScoreWithReversedQuestions4578` | DETECTADA |
| M5 | Quitar el control de dueÃ±o en GET /users/{id} | `FullyQualifiedName~GetUser_OfAnotherUser_ShouldReturn403` | DETECTADA |
| M6 | Guardar el CVV (en la columna del vencimiento) | `FullyQualifiedName~Subscription_ShouldStoreOnlyLast4AndNeverCvv|FullyQualifiedName~Subscription_CreateAndGet_ShouldOnlyExposeLast4` | DETECTADA |

# language: es
Característica: Ciclo de la cita
  Como paciente y como psicólogo
  queremos que cada cita siga un ciclo claro
  para que solo se califiquen las sesiones que realmente ocurrieron

  Antecedentes:
    Dado un trabajador con una cita pendiente con un psicólogo verificado

  Escenario: El psicólogo confirma la cita
    Cuando el psicólogo confirma la cita
    Entonces la cita queda en estado "CONFIRMED"

  Escenario: El paciente cancela su cita
    Cuando el trabajador cancela la cita
    Entonces la cita queda en estado "CANCELLED"

  Escenario: El paciente no puede marcar la cita como atendida
    Cuando el trabajador intenta marcar la cita como atendida
    Entonces la respuesta es un error 403
    Y la cita queda en estado "PENDING"

  Escenario: No se puede calificar una sesión que no fue atendida
    Cuando el trabajador califica la sesión con 5 estrellas
    Entonces la respuesta es un error 400 con el mensaje "Solo se puede calificar una sesión atendida"

  Escenario: El paciente califica una sesión atendida
    Dado que el psicólogo confirmó la cita
    Y la sesión ya ocurrió
    Y el psicólogo marcó la cita como atendida
    Cuando el trabajador califica la sesión con 5 estrellas
    Entonces la respuesta es 201
    Y la calificación del psicólogo es "5.0" con 1 reseña

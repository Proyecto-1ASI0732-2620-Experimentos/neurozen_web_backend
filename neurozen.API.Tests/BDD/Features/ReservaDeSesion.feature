# language: es
Característica: Reserva de sesión
  Como trabajador que necesita apoyo
  quiero reservar una sesión con un psicólogo verificado en un horario libre
  para recibir atención profesional (US14)

  Antecedentes:
    Dado un trabajador con sesión iniciada

  Escenario: Reserva exitosa en un horario libre
    Dado un psicólogo verificado con disponibilidad todos los días
    Cuando el trabajador reserva la sesión de mañana a las 10:00
    Entonces la respuesta es 201
    Y la cita queda en estado "PENDING"

  Escenario: El horario ya fue reservado por otro trabajador
    Dado un psicólogo verificado con disponibilidad todos los días
    Y otro trabajador ya reservó la sesión de mañana a las 10:00
    Cuando el trabajador reserva la sesión de mañana a las 10:00
    Entonces la respuesta es un error 409 con el mensaje "El horario ya no está disponible"

  Escenario: No se puede reservar con un psicólogo no verificado
    Dado un psicólogo pendiente de verificación con disponibilidad todos los días
    Cuando el trabajador reserva la sesión de mañana a las 10:00
    Entonces la respuesta es un error 404

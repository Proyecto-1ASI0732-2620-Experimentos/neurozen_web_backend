# language: es
Característica: Verificación de psicólogos
  Para que los trabajadores solo reserven con profesionales colegiados
  como administrador de NeuroZen
  quiero aprobar o rechazar a los psicólogos después de validar su colegiatura en el CPsP

  Escenario: El administrador aprueba a un psicólogo con la colegiatura habilitada
    Dado un psicólogo registrado pendiente de verificación
    Cuando el administrador lo aprueba tras validar su colegiatura en el CPsP
    Entonces el estado del psicólogo es "VERIFIED"
    Y el psicólogo aparece en el directorio de profesionales

  Escenario: El administrador rechaza a un psicólogo indicando el motivo
    Dado un psicólogo registrado pendiente de verificación
    Cuando el administrador lo rechaza con el motivo "La colegiatura no figura como HABILITADO en el CPsP"
    Entonces el estado del psicólogo es "REJECTED"
    Y el psicólogo ve el motivo "La colegiatura no figura como HABILITADO en el CPsP"

  Escenario: Un psicólogo pendiente no aparece en el directorio
    Dado un psicólogo registrado pendiente de verificación
    Cuando un trabajador consulta el directorio de profesionales
    Entonces el psicólogo no aparece en el directorio de profesionales

  Escenario: No se puede rechazar sin un motivo suficiente
    Dado un psicólogo registrado pendiente de verificación
    Cuando el administrador lo rechaza con el motivo "Corto"
    Entonces la respuesta es un error 400
    Y el estado del psicólogo es "PENDING"

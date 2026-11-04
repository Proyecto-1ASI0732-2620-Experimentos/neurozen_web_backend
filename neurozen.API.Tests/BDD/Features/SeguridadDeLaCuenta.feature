# language: es
Característica: Seguridad de la cuenta
  Como usuario de NeuroZen
  quiero recuperar y cambiar mi contraseña de forma segura
  para que nadie más pueda entrar a mi cuenta

  Escenario: Recuperar la contraseña con un enlace válido
    Dado un trabajador registrado que olvidó su contraseña
    Cuando solicita recuperar su contraseña
    Y usa el enlace recibido para elegir la contraseña "NuevaClave2026"
    Entonces puede iniciar sesión con la contraseña "NuevaClave2026"
    Y ya no puede iniciar sesión con su contraseña anterior

  Escenario: El enlace de recuperación ya venció
    Dado un trabajador registrado que olvidó su contraseña
    Cuando solicita recuperar su contraseña
    Y el enlace recibido vence
    Y usa el enlace recibido para elegir la contraseña "NuevaClave2026"
    Entonces la respuesta es un error 400 con el mensaje "El enlace no es válido o ya venció"
    Y puede iniciar sesión con su contraseña anterior

  Escenario: Cambiar la contraseña indicando mal la contraseña actual
    Dado un trabajador con sesión iniciada
    Cuando cambia su contraseña indicando "Incorrecta99" como contraseña actual
    Entonces la respuesta es un error 400 con el mensaje "La contraseña actual es incorrecta"
    Y puede iniciar sesión con su contraseña anterior

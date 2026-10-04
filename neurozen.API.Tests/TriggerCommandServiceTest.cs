
using Microsoft.Extensions.Logging;
using Moq;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Triggers.Application.Internal.CommandServices;
using neurozen.API.Triggers.Domain.Model.Aggregates;
using neurozen.API.Triggers.Domain.Model.Commands;
using neurozen.API.Triggers.Domain.Repositories;

public class TriggerCommandServiceTest
{
  /// <summary>
    /// PRUEBA 1: Creación Exitosa de un Trigger
    /// - Por qué se realiza: Para comprobar que el servicio procesa un comando válido, 
    ///   crea la entidad y llama exactamente una vez a los métodos de persistencia y confirmación.
    /// - Por qué es importante: Garantiza que la lógica de negocio principal de escritura 
    ///   funciona correctamente de forma aislada, asegurando la integridad de los datos 
    ///   sin necesidad de una base de datos real.
  /// </summary>
  [Fact]
  public async Task CreateTriggerCommandTest()
  {
    //Arrange (preparamos los mocks)
    var mockRepo = new Mock<ITriggerRepository>();
    var mockUow = new Mock<IUnitOfWork>();
    var mockLogger = new Mock<ILogger<TriggerCommandService>>();

    var service = new TriggerCommandService(mockRepo.Object, mockUow.Object, mockLogger.Object);
    var command = new CreateTriggerCommand(1, 1, 10, DateTime.Now.AddDays(1), "trigger de prueba");

    //Act (Ejecutamos la función)
    var result = await service.Handle(command);

    //Assert (Verificamos)
    Assert.NotNull(result);
    Assert.InRange(command.StressLevel, 1, 10); //que el nivel de estrés no sea mayor a 10
    mockRepo.Verify(r => r.AddAsync(It.IsAny<Trigger>()), Times.Once);
    mockUow.Verify(u => u.CompleteAsync(), Times.Once);
  }

  /// <summary>
    /// PRUEBA 2: Manejo de Errores de Base de Datos
    /// - Por qué se realiza: Para simular una excepción a nivel de infraestructura 
    ///   (falla en el commit de la base de datos) y verificar que el bloque try-catch reacciona adecuadamente.
    /// - Por qué es importante: Evita que la aplicación crashee
    ///   ante caídas de la base de datos, asegurando un manejo de errores controlado que devuelve null de forma segura.
    /// </summary>
  [Fact]
  public async Task CreateTrigger_WhenDatabaseFails_ReturnsNull()
  {
    //Arrange
    var mockRepo = new Mock<ITriggerRepository>();
    var mockUow = new Mock<IUnitOfWork>();
    var mockLogger = new Mock<ILogger<TriggerCommandService>>();

    var service = new TriggerCommandService(mockRepo.Object, mockUow.Object, mockLogger.Object);
    var command = new CreateTriggerCommand(1, 1, 10, DateTime.Now.AddDays(1), "trigger de prueba");

    mockUow.Setup(u => u.CompleteAsync()).ThrowsAsync(new Exception("Fallo en BD")); //Simulamos un fallo

    //Act ejecutamos la función
    var result = await service.Handle(command);

    //Assert verificamos que el objeto es null pero el logger no
    Assert.Null(result);
    Assert.NotNull(mockLogger);
  }
}

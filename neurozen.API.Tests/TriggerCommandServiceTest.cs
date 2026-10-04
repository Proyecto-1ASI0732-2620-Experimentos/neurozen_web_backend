
using Microsoft.Extensions.Logging;
using Moq;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Triggers.Application.Internal.CommandServices;
using neurozen.API.Triggers.Domain.Model.Aggregates;
using neurozen.API.Triggers.Domain.Model.Commands;
using neurozen.API.Triggers.Domain.Repositories;

public class TriggerCommandServiceTest
{
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

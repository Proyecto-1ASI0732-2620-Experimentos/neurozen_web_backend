

using Microsoft.Extensions.Logging;
using Moq;
using neurozen.API.Appointments.Application.Internal.CommandServices;
using neurozen.API.Appointments.Domain.Model.Aggregates;
using neurozen.API.Appointments.Domain.Model.Commands;
using neurozen.API.Appointments.Domain.Model.ValueObjects;
using neurozen.API.Appointments.Domain.Repositories;
using neurozen.API.Shared.Domain.Repositories;

public class AppointmentCommandServiceTests
{
  [Fact]
  public async Task Handle_ValidCommand_ReturnsAppointmentAndSavesToDataBase()
  {
    // Arrange (Preparar el entorno y los mocks)
        var mockRepo = new Mock<IAppointmentRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var mockLogger = new Mock<ILogger<AppointmentCommandService>>();
        
        var service = new AppointmentCommandService(mockRepo.Object, mockUow.Object, mockLogger.Object);
        var command = new CreateAppointmentCommand(2, 2, DateTime.Now.AddDays(1), EAppointmentType.ConsultaInicial, "Nota de prueba");

        // Act (Ejecutar la función)
        var result = await service.Handle(command);

        // Assert (Verificar que ocurrió lo esperado)
        Assert.NotNull(result);
        Assert.Equal(command.PatientId, result.PatientId);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once); // Verifica que se llamó al repositorio 1 vez
        mockUow.Verify(u => u.CompleteAsync(), Times.Once); //Verificamos que se completó una vez
  }
}
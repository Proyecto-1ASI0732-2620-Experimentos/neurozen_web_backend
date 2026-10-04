using Moq;
using neurozen.API.Professionals.Application.Internal.QueryServices;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Commands;
using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Repositories;

public class ProfessionalQueryServiceTest
{
  /// <summary>
    /// PRUEBA 3: Búsqueda de Profesional por ID
    /// - Por qué se realiza: Para verificar que el servicio de consultas es capaz 
    ///   de comunicarse con el repositorio y retornar la entidad exacta solicitada mediante un query.
    /// - Por qué es importante: Asegura que los flujos de lectura de la aplicación 
    ///   recuperen la información precisa para los clientes (como la app móvil) sin alterar ningún estado.
  /// </summary>
  [Fact]
  public async Task GetProfessionalById_ReturnsCorrectId()
  {
    //Arrange
    var mockRepo = new Mock<IProfessionalRepository>();
    var serviceQuery = new ProfessionalQueryService(mockRepo.Object);
    //Creamos la entidad fake del profesional
    var command = new CreateProfessionalCommand("Miguel", "Depresión", "5 años", 0, 0, 30, "Lun. | Vier.", "test", "test.png");
    var professionalFake = new Professional(command);
    var testId = professionalFake.Id;

    //Configuramos el mock para que devuelva el profesional fake cuando le pregunten por ese ID
    mockRepo.Setup(r => r.FindByIdAsync(testId)).ReturnsAsync(professionalFake);

    var query = new GetProfessionalByIdQuery(testId);

    //Act
    var result = await serviceQuery.Handle(query);

    //Assert
    Assert.NotNull(result);
    Assert.Equal(professionalFake.Id, result.Id);
  }
}


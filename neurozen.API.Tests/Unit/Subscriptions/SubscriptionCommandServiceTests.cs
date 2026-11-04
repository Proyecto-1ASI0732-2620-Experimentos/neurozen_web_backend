using Moq;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Subscriptions.Application.Internal.CommandServices;
using neurozen.API.Subscriptions.Domain.Model.Aggregates;
using neurozen.API.Subscriptions.Domain.Model.Commands;
using neurozen.API.Subscriptions.Domain.Repositories;
using neurozen.API.Tests.Unit.Support;

namespace neurozen.API.Tests.Unit.Subscriptions;

public class SubscriptionCommandServiceTests
{
    private const string FullCardNumber = "4111111111111111";
    private const string Cvv = "987";

    private readonly Mock<ISubscriptionRepository> _subscriptions = new();
    private readonly Mock<neurozen.API.Shared.Domain.Repositories.IUnitOfWork> _unitOfWork =
        TestEntities.PassThroughUnitOfWork();

    public SubscriptionCommandServiceTests()
    {
        _subscriptions.Setup(s => s.FindByUserIdAsync(7)).ReturnsAsync((Subscription?)null);
    }

    private SubscriptionCommandService CreateService() =>
        new(_subscriptions.Object, new FakeCurrentUser(7, "USER"), _unitOfWork.Object);

    private static CreateSubscriptionCommand Subscribe(string cardNumber = "4111 1111 1111 1111",
        string expiration = "12/30") =>
        new(7, 2, "Ana", "Torres", "ana@neurozen.com", cardNumber, expiration, Cvv);

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: PCI DSS; nunca se guarda el CVV (ni cifrado) ni el número completo de la
    ///   tarjeta, solo los últimos 4 dígitos y el vencimiento.
    /// - ¿Qué hace esta prueba?: suscribe con una tarjeta de prueba, captura la entidad que se guardaría y comprueba
    ///   cardLast4 = 1111, el vencimiento normalizado, que no existe ninguna propiedad para el CVV o el número, y que
    ///   ningún valor guardado contiene el número completo ni es igual al CVV.
    /// </summary>
    [Fact]
    public async Task Subscription_ShouldStoreOnlyLast4AndNeverCvv()
    {
        // Arrange
        Subscription? saved = null;
        _subscriptions.Setup(s => s.AddAsync(It.IsAny<Subscription>()))
            .Callback<Subscription>(s => saved = s).Returns(Task.CompletedTask);

        // Act
        var result = await CreateService().Handle(Subscribe());

        // Assert
        Assert.True(result.Created);
        Assert.NotNull(saved);
        Assert.Equal("1111", saved!.CardLast4);
        Assert.Equal("12/30", saved.CardExpiry);
        var properties = typeof(Subscription).GetProperties();
        Assert.DoesNotContain(properties, p =>
            p.Name.Contains("cvv", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("CardNumber", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("NumberCard", StringComparison.OrdinalIgnoreCase));
        var storedStrings = properties.Where(p => p.PropertyType == typeof(string))
            .Select(p => (string?)p.GetValue(saved) ?? string.Empty).ToList();
        Assert.DoesNotContain(storedStrings, value => value.Contains(FullCardNumber) || value == Cvv);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: el servidor valida la tarjeta aunque el frontend ya lo haga: un número que
    ///   no pasa el algoritmo de Luhn o una tarjeta vencida deben rechazarse antes de guardar nada.
    /// - ¿Qué hace esta prueba?: envía un número con el dígito verificador alterado y una tarjeta vencida en 01/20;
    ///   ambos dan 400 en su campo y no se guarda la suscripción.
    /// </summary>
    [Theory]
    [InlineData("4111111111111112", "12/30", "numberCard")]
    [InlineData("4111111111111111", "01/20", "expirationDate")]
    public async Task Subscription_InvalidLuhnOrExpiredCard_ShouldThrowValidation(string cardNumber, string expiration,
        string field)
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(
            () => service.Handle(Subscribe(cardNumber, expiration)));

        // Assert
        Assert.True(exception.Errors!.ContainsKey(field));
        _subscriptions.Verify(s => s.AddAsync(It.IsAny<Subscription>()), Times.Never);
    }
}

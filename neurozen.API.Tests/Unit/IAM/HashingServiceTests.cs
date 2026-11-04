using neurozen.API.IAM.Infrastructure.Hashing.BCrypt.Services;

namespace neurozen.API.Tests.Unit.IAM;

public class HashingServiceTests
{
    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: si la base de datos se filtra, las contraseñas no deben poder leerse;
    ///   solo se guarda un hash BCrypt con costo 12 o mayor (la especificación pide 11 o mayor).
    /// - ¿Qué hace esta prueba?: hashea una contraseña con el servicio real y comprueba que el resultado no contiene
    ///   el texto plano, que es BCrypt con costo 12, que verifica la correcta y rechaza otra.
    /// </summary>
    [Fact]
    public void PasswordHash_ShouldNeverStorePlainText()
    {
        // Arrange
        var hashing = new HashingService();
        const string password = "Secreta123";

        // Act
        var hash = hashing.HashPassword(password);

        // Assert
        Assert.DoesNotContain(password, hash);
        Assert.StartsWith("$2", hash);
        Assert.Equal("12", hash.Split('$')[2]);
        Assert.True(hashing.VerifyPassword(password, hash));
        Assert.False(hashing.VerifyPassword("OtraClave99", hash));
    }
}

using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.Tests.Unit.IAM;

public class PasswordPolicyTests
{
    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: la política mínima de contraseñas (8 caracteres, letras y números)
    ///   debe aceptar las contraseñas que la cumplen; si las rechazara, nadie podría registrarse.
    /// - ¿Qué hace esta prueba?: valida una contraseña con letras y números y comprueba que no hay errores.
    /// </summary>
    [Fact]
    public void PasswordPolicy_WithLettersAndNumbers_ShouldBeValid()
    {
        // Arrange
        var errors = new ValidationErrors();

        // Act
        PasswordPolicy.Validate("Secreta123", "password", errors);

        // Assert
        Assert.False(errors.HasErrors);
    }

    /// <summary>
    /// NOTA DE PRUEBA:
    /// - ¿Por qué se realizó esta prueba?: contraseñas cortas, solo letras o solo números son fáciles de adivinar;
    ///   el servidor debe rechazarlas aunque el frontend ya las valide.
    /// - ¿Qué hace esta prueba?: valida cada contraseña débil y comprueba que se registra un error en el campo.
    /// </summary>
    [Theory]
    [InlineData("Ab1")]
    [InlineData("SoloLetras")]
    [InlineData("12345678")]
    [InlineData("")]
    public void PasswordPolicy_ShortOrOnlyLettersOrOnlyNumbers_ShouldBeInvalid(string password)
    {
        // Arrange
        var errors = new ValidationErrors();

        // Act
        PasswordPolicy.Validate(password, "password", errors);

        // Assert
        Assert.True(errors.HasErrors);
        var exception = Assert.Throws<ValidationFailedException>(() => errors.ThrowIfAny());
        Assert.True(exception.Errors!.ContainsKey("password"));
    }
}

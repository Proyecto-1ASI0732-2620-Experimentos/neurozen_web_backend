using System.Text;
using neurozen.API.Shared.Domain.Model.Exceptions;

namespace neurozen.API.IAM.Domain.Model.ValueObjects;

/// <summary>
///     Minimum 8 characters, at least one letter and one number.
///     Upper bound of 72 bytes because BCrypt ignores anything beyond that.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxBytes = 72;

    public static void Validate(string? password, string field, ValidationErrors errors)
    {
        if (string.IsNullOrEmpty(password))
        {
            errors.Add(field, "La contraseña es obligatoria");
            return;
        }

        errors.AddIf(password.Length < MinLength, field, $"La contraseña debe tener al menos {MinLength} caracteres");
        errors.AddIf(!password.Any(char.IsLetter) || !password.Any(char.IsDigit), field,
            "La contraseña debe incluir al menos una letra y un número");
        errors.AddIf(Encoding.UTF8.GetByteCount(password) > MaxBytes, field, "La contraseña es demasiado larga");
    }
}

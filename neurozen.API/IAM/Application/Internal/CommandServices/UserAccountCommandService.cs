using System.Globalization;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.IAM.Domain.Services;
using neurozen.API.Shared.Application.Accounts;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.IAM.Application.Internal.CommandServices;

public class UserAccountCommandService(
    IUserRepository userRepository,
    IHashingService hashingService,
    IEnumerable<IUserDataEraser> userDataErasers,
    IUnitOfWork unitOfWork)
    : IUserAccountCommandService
{
    public async Task<User> Handle(UpdateUserProfileCommand command)
    {
        var user = await FindActiveUserAsync(command.UserId);
        var errors = new ValidationErrors();

        var email = command.Email is null ? user.Email : EmailAddress.Normalize(command.Email);
        errors.AddIf(command.Email is not null && !EmailAddress.IsValid(email), "email", "Ingresa un correo válido");

        var fullName = command.FullName is null ? user.FullName : command.FullName.Trim();
        if (command.FullName is not null) PersonName.Validate(fullName, "fullName", errors);

        var phoneNumber = Merge(command.PhoneNumber, user.PhoneNumber);
        errors.AddIf(phoneNumber is not null && !IsValidPhoneNumber(phoneNumber), "phoneNumber",
            "Ingresa un teléfono válido (solo dígitos, espacios, guiones y + inicial)");

        var address = Merge(command.Address, user.Address);
        errors.AddIf(address?.Length > UserProfileRules.AddressMaxLength, "address",
            $"La dirección no puede superar {UserProfileRules.AddressMaxLength} caracteres");

        var avatarUrl = Merge(command.AvatarUrl, user.AvatarUrl);
        errors.AddIf(avatarUrl is not null && !IsValidHttpUrl(avatarUrl), "avatarUrl",
            "La foto debe ser una URL http(s) válida");

        var dateOfBirth = command.DateOfBirth is null
            ? user.DateOfBirth
            : ParseDateOfBirth(command.DateOfBirth, errors);

        var ageRange = Merge(command.AgeRange, user.AgeRange);
        errors.AddIf(ageRange is not null && !UserProfileRules.AgeRanges.Contains(ageRange), "ageRange",
            $"El rango de edad debe ser uno de: {string.Join(", ", UserProfileRules.AgeRanges)}");

        var occupation = Merge(command.Occupation, user.Occupation);
        errors.AddIf(occupation?.Length > UserProfileRules.OccupationMaxLength, "occupation",
            $"La ocupación no puede superar {UserProfileRules.OccupationMaxLength} caracteres");

        var workType = Merge(command.WorkType, user.WorkType)?.ToLowerInvariant();
        errors.AddIf(workType is not null && !UserProfileRules.WorkTypes.Contains(workType), "workType",
            "El tipo de trabajo debe ser formal o informal");

        errors.ThrowIfAny();

        if (email != user.Email && await userRepository.ExistsByEmailForOtherUserAsync(email, user.Id))
            throw new ConflictException("Ya existe una cuenta con este correo");

        user.UpdateProfile(email, fullName, phoneNumber, address, avatarUrl, dateOfBirth, ageRange, occupation,
            workType);
        await unitOfWork.CompleteAsync();
        return user;
    }

    public async Task Handle(ChangePasswordCommand command)
    {
        var user = await FindActiveUserAsync(command.UserId);

        if (!hashingService.VerifyPassword(command.CurrentPassword ?? string.Empty, user.PasswordHash))
            throw new ValidationFailedException("La contraseña actual es incorrecta",
                new Dictionary<string, string[]> { ["currentPassword"] = ["La contraseña actual es incorrecta"] });

        var errors = new ValidationErrors();
        PasswordPolicy.Validate(command.NewPassword, "newPassword", errors);
        errors.AddIf(!errors.HasErrors && command.NewPassword == command.CurrentPassword, "newPassword",
            "La nueva contraseña debe ser distinta de la actual");
        errors.ThrowIfAny("La nueva contraseña no cumple los requisitos");

        user.UpdatePasswordHash(hashingService.HashPassword(command.NewPassword!));
        await unitOfWork.CompleteAsync();
    }

    public async Task Handle(DeleteAccountCommand command)
    {
        var user = await FindActiveUserAsync(command.UserId);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var eraser in userDataErasers.OrderBy(eraser => eraser.Order))
                await eraser.EraseAsync(user.Id);

            user.Anonymize(DateTime.UtcNow);
            await unitOfWork.CompleteAsync();
        });
    }

    private async Task<User> FindActiveUserAsync(long userId)
    {
        var user = await userRepository.FindByIdAsync(userId);
        if (user is null || user.IsDeleted) throw new ResourceNotFoundException("La cuenta no existe");
        return user;
    }

    /// <summary>null = keep current, "" (or whitespace) = clear, otherwise the trimmed value.</summary>
    private static string? Merge(string? incoming, string? current)
    {
        if (incoming is null) return current;
        var trimmed = incoming.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static bool IsValidPhoneNumber(string value)
    {
        if (value.Length > UserProfileRules.PhoneNumberMaxLength) return false;
        var digits = value.Count(char.IsDigit);
        var allowed = value.Select((c, index) => char.IsDigit(c) || c is ' ' or '-' || (c == '+' && index == 0))
            .All(ok => ok);
        return allowed && digits is >= 6 and <= 15;
    }

    private static bool IsValidHttpUrl(string value) =>
        value.Length <= UserProfileRules.AvatarUrlMaxLength &&
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);

    /// <summary>"" clears the date. Accepts YYYY-MM-DD (or an ISO date-time, keeping its date part).</summary>
    private static DateOnly? ParseDateOfBirth(string value, ValidationErrors errors)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0) return null;

        var datePart = trimmed.Length >= 10 ? trimmed[..10] : trimmed;
        if (!DateOnly.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var date))
        {
            errors.Add("dateOfBirth", "La fecha de nacimiento debe tener el formato YYYY-MM-DD");
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        errors.AddIf(date > today, "dateOfBirth", "La fecha de nacimiento no puede ser futura");
        errors.AddIf(date < new DateOnly(1900, 1, 1), "dateOfBirth", "La fecha de nacimiento no es válida");
        return date;
    }
}

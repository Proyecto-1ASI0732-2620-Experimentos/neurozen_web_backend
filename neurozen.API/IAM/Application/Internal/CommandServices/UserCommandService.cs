using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.IAM.Domain.Services;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.IAM.Application.Internal.CommandServices;

public class UserCommandService(
    IUserRepository userRepository,
    IProfessionalsContextFacade professionalsContextFacade,
    ITokenService tokenService,
    IHashingService hashingService,
    IUnitOfWork unitOfWork)
    : IUserCommandService
{
    private const string InvalidCredentialsMessage = "Correo o contraseña incorrectos";

    public async Task<AuthenticatedUser> Handle(SignInCommand command)
    {
        var email = EmailAddress.Normalize(command.Email);
        var user = EmailAddress.IsValid(email) ? await userRepository.FindByEmailAsync(email) : null;

        // Always run BCrypt (against a dummy hash when there is no user) so that
        // the response time does not reveal whether the email is registered.
        var passwordMatches = hashingService.VerifyPassword(command.Password ?? string.Empty, user?.PasswordHash);

        if (user is null || !passwordMatches)
            throw new InvalidCredentialsException(InvalidCredentialsMessage);

        return new AuthenticatedUser(user, tokenService.GenerateToken(user));
    }

    public async Task<AuthenticatedUser> Handle(SignUpCommand command)
    {
        var email = EmailAddress.Normalize(command.Email);
        var fullName = (command.FullName ?? string.Empty).Trim();
        var errors = new ValidationErrors();

        errors.AddIf(!EmailAddress.IsValid(email), "email", "Ingresa un correo válido");
        PasswordPolicy.Validate(command.Password, "password", errors);
        PersonName.Validate(fullName, "fullName", errors);
        errors.AddIf(!command.AcceptedTerms, "acceptedTerms",
            "Debes aceptar los Términos y la Política de Privacidad");

        var role = ParseSignUpRole(command.Role, errors);
        if (role == UserRole.Psychologist)
            professionalsContextFacade.ValidateRegistration(command.Specialty, command.LicenseNumber, errors);

        errors.ThrowIfAny();

        if (await userRepository.ExistsByEmailAsync(email))
            throw new ConflictException("Ya existe una cuenta con este correo");

        var licenseNumber = command.LicenseNumber?.Trim() ?? string.Empty;
        if (role == UserRole.Psychologist && await professionalsContextFacade.ExistsByLicenseNumberAsync(licenseNumber))
            throw new ConflictException("Ya existe una cuenta con esta colegiatura");

        var user = new User(email, hashingService.HashPassword(command.Password!), fullName, role, DateTime.UtcNow);

        // User and professional profile are created atomically. Unique indexes on email and
        // license number catch concurrent duplicates (mapped to 409 by the global handler).
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await userRepository.AddAsync(user);
            await unitOfWork.CompleteAsync();

            if (role == UserRole.Psychologist)
            {
                await professionalsContextFacade.RegisterPendingProfessionalAsync(
                    user.Id, command.Specialty!, licenseNumber);
                await unitOfWork.CompleteAsync();
            }
        });

        return new AuthenticatedUser(user, tokenService.GenerateToken(user));
    }

    /// <summary>Only USER (default) or PSYCHOLOGIST can self-register. ADMIN is rejected (privilege escalation).</summary>
    private static UserRole ParseSignUpRole(string? value, ValidationErrors errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return UserRole.User;

        if (!UserRoleExtensions.TryFromWire(value, out var role))
        {
            errors.Add("role", "El rol debe ser USER o PSYCHOLOGIST");
            return UserRole.User;
        }

        if (role == UserRole.Admin)
        {
            errors.Add("role", "No está permitido registrarse con el rol ADMIN");
            return UserRole.User;
        }

        return role;
    }
}

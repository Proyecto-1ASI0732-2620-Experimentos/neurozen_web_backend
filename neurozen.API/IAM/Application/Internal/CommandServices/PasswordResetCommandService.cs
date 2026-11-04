using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using neurozen.API.IAM.Application.Internal.Configuration;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.IAM.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.IAM.Application.Internal.CommandServices;

public class PasswordResetCommandService(
    IUserRepository userRepository,
    IPasswordResetTokenRepository tokenRepository,
    IHashingService hashingService,
    IEmailQueue emailQueue,
    IPasswordResetThrottle throttle,
    IOptions<PasswordResetSettings> settings,
    IUnitOfWork unitOfWork)
    : IPasswordResetCommandService
{
    private const string InvalidLinkMessage = "El enlace no es válido o ya venció";
    private const int TokenBytes = 32;

    public async Task Handle(ForgotPasswordCommand command)
    {
        var email = EmailAddress.Normalize(command.Email);

        // Every early return below still produces 200 { "message": "ok" } in the controller.
        if (!EmailAddress.IsValid(email) || !throttle.TryAcquire(email)) return;

        var user = await userRepository.FindByEmailAsync(email);
        if (user is null) return;

        var now = DateTime.UtcNow;
        foreach (var previous in await tokenRepository.ListUnusedByUserIdAsync(user.Id))
            previous.MarkUsed(now);

        var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        var expiresAt = now.Add(settings.Value.EffectiveTokenLifetime);
        await tokenRepository.AddAsync(new PasswordResetToken(user.Id, HashToken(rawToken), expiresAt));
        await unitOfWork.CompleteAsync();

        var link = $"{settings.Value.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        var minutes = (int)settings.Value.EffectiveTokenLifetime.TotalMinutes;
        await emailQueue.EnqueueAsync(new EmailMessage(
            user.Email,
            "NeuroZen — Restablece tu contraseña",
            $"""
             Hola {user.FullName}:

             Recibimos una solicitud para restablecer la contraseña de tu cuenta de NeuroZen.
             Abre este enlace para elegir una nueva contraseña (vence en {minutes} minutos y solo se puede usar una vez):

             {link}

             Si no fuiste tú, ignora este correo: tu contraseña actual sigue siendo válida.
             """));
    }

    public async Task Handle(ResetPasswordCommand command)
    {
        var token = string.IsNullOrWhiteSpace(command.Token)
            ? null
            : await tokenRepository.FindByTokenHashAsync(HashToken(command.Token.Trim()));

        var now = DateTime.UtcNow;
        if (token is null || !token.IsUsable(now))
            throw new ValidationFailedException(InvalidLinkMessage);

        var errors = new ValidationErrors();
        PasswordPolicy.Validate(command.NewPassword, "newPassword", errors);
        errors.ThrowIfAny("La nueva contraseña no cumple los requisitos");

        var user = await userRepository.FindByIdAsync(token.UserId)
                   ?? throw new ValidationFailedException(InvalidLinkMessage);

        user.UpdatePasswordHash(hashingService.HashPassword(command.NewPassword!));
        foreach (var pending in await tokenRepository.ListUnusedByUserIdAsync(user.Id))
            pending.MarkUsed(now);
        token.MarkUsed(now);

        await unitOfWork.CompleteAsync();
    }

    /// <summary>SHA-256 hex of the raw token. Only this hash is persisted.</summary>
    private static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}

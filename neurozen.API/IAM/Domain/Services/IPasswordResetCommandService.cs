using neurozen.API.IAM.Domain.Model.Commands;

namespace neurozen.API.IAM.Domain.Services;

public interface IPasswordResetCommandService
{
    /// <summary>Never reveals whether the account exists; always completes normally.</summary>
    Task Handle(ForgotPasswordCommand command);

    /// <summary>Throws ValidationFailedException (400) for invalid/used/expired tokens or weak passwords.</summary>
    Task Handle(ResetPasswordCommand command);
}

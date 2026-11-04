using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;

namespace neurozen.API.IAM.Domain.Services;

/// <summary>Result of a successful sign-in or sign-up.</summary>
public record AuthenticatedUser(User User, string Token);

public interface IUserCommandService
{
    /// <summary>Throws InvalidCredentialsException (401) with the same message whether or not the email exists.</summary>
    Task<AuthenticatedUser> Handle(SignInCommand command);

    /// <summary>Creates the user (and, for PSYCHOLOGIST, its PENDING professional profile) in one transaction.</summary>
    Task<AuthenticatedUser> Handle(SignUpCommand command);
}

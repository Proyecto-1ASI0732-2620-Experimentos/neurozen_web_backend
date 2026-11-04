using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.Commands;

namespace neurozen.API.IAM.Domain.Services;

/// <summary>Owner-only account operations. Ownership is checked by the controller before calling.</summary>
public interface IUserAccountCommandService
{
    /// <summary>400 on invalid data, 409 when the email belongs to another account.</summary>
    Task<User> Handle(UpdateUserProfileCommand command);

    /// <summary>400 (never 401) when the current password is wrong, the new one is weak or equal to the current one.</summary>
    Task Handle(ChangePasswordCommand command);

    /// <summary>Deletes or anonymizes the user's data in every bounded context, then anonymizes the user.</summary>
    Task Handle(DeleteAccountCommand command);
}

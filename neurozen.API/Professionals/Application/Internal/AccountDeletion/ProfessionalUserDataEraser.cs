using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Application.Accounts;

namespace neurozen.API.Professionals.Application.Internal.AccountDeletion;

/// <summary>
///     Anonymizes the professional profile of a deleted PSYCHOLOGIST account. Runs after the
///     appointment eraser, which needs the professional row to cancel future appointments.
/// </summary>
public class ProfessionalUserDataEraser(IProfessionalRepository professionalRepository) : IUserDataEraser
{
    public int Order => 500;

    public async Task EraseAsync(long userId, CancellationToken cancellationToken = default)
    {
        var professional = await professionalRepository.FindByUserIdAsync(userId);
        professional?.MarkDeleted(DateTime.UtcNow);
    }
}

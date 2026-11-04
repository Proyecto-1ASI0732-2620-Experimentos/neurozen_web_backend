using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Stress.Domain.Model.Aggregates;
using neurozen.API.Stress.Domain.Model.Commands;
using neurozen.API.Stress.Domain.Model.ValueObjects;
using neurozen.API.Stress.Domain.Repositories;
using neurozen.API.Stress.Domain.Services;

namespace neurozen.API.Stress.Application.Internal.CommandServices;

public class StressTriggerCommandService(
    IStressTriggerRepository triggerRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IStressTriggerCommandService
{
    /// <summary>Tolerance for small clock differences between the client and the server.</summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(2);

    public async Task<StressTrigger> Handle(CreateTriggerCommand command)
    {
        var userId = StressAccess.ResolveOwnUserId(currentUser, command.UserId);
        var now = DateTime.UtcNow;
        var errors = new ValidationErrors();

        var description = (command.Description ?? string.Empty).Trim();
        errors.AddIf(description.Length is < StressTrigger.DescriptionMinLength or > StressTrigger.DescriptionMaxLength,
            "description",
            $"La descripción debe tener entre {StressTrigger.DescriptionMinLength} y {StressTrigger.DescriptionMaxLength} caracteres");

        errors.AddIf(command.StressLevel is null or < StressTrigger.MinStressLevel or > StressTrigger.MaxStressLevel,
            "stressLevel",
            $"El nivel de estrés debe estar entre {StressTrigger.MinStressLevel} y {StressTrigger.MaxStressLevel}");

        var triggeredAt = command.TriggeredAt ?? now;
        errors.AddIf(triggeredAt > now + ClockSkew, "triggeredAt", "La fecha del registro no puede ser futura");

        errors.ThrowIfAny();

        var trigger = new StressTrigger(userId, description,
            TriggerCategoryExtensions.FromWireOrOther(command.Category), command.StressLevel!.Value,
            triggeredAt > now ? now : triggeredAt);
        await triggerRepository.AddAsync(trigger);
        await unitOfWork.CompleteAsync();
        return trigger;
    }

    public async Task Handle(DeleteTriggerCommand command)
    {
        var trigger = await triggerRepository.FindByIdAsync(command.TriggerId);
        if (trigger is null || trigger.UserId != currentUser.Id)
            throw new ResourceNotFoundException("El registro no existe");

        triggerRepository.Remove(trigger);
        await unitOfWork.CompleteAsync();
    }
}

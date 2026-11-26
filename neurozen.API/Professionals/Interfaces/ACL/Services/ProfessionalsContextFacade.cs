using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Professionals.Domain.Services;

namespace neurozen.API.Professionals.Interfaces.ACL.Services;

public class ProfessionalsContextFacade(
    IProfessionalRepository professionalRepository,
    ILogger<ProfessionalsContextFacade> logger) : IProfessionalsContextFacade
{
    public void ValidateRegistration(string? specialty, string? licenseNumber, ValidationErrors errors)
    {
        errors.AddIf(!SpecialtyExtensions.TryFromWire(specialty, out _), "specialty",
            "Selecciona una especialidad válida");
        errors.AddIf(!Professional.IsValidLicenseNumber(licenseNumber), "licenseNumber",
            "El número de colegiatura debe tener entre 3 y 6 dígitos");
    }

    public Task<bool> ExistsByLicenseNumberAsync(string licenseNumber) =>
        professionalRepository.ExistsByLicenseNumberAsync(licenseNumber.Trim());

    public async Task RegisterPendingProfessionalAsync(long userId, string specialty, string licenseNumber)
    {
        var professional = new Professional(userId, SpecialtyExtensions.FromWire(specialty), licenseNumber);
        await professionalRepository.AddAsync(professional);
    }

    public async Task ReturnToPendingVerificationAsync(long userId)
    {
        var professional = await professionalRepository.FindByUserIdAsync(userId);
        if (professional is not null && professional.ReturnToPendingVerification(DateTime.UtcNow))
            logger.LogInformation("Professional {ProfessionalId} returned to PENDING verification (name changed)",
                professional.Id);
    }

    public async Task<ProfessionalReference?> FindByIdAsync(long professionalId) =>
        ToReference(await professionalRepository.FindByIdAsync(professionalId));

    public async Task<ProfessionalReference?> FindByUserIdAsync(long userId) =>
        ToReference(await professionalRepository.FindByUserIdAsync(userId));

    public Task<bool> IsWithinAvailabilityAsync(long professionalId, DateTime startUtc) =>
        IsWithinAvailabilityAsync(professionalId, startUtc, SlotAvailabilityCalculator.DefaultDurationMinutes);

    public async Task<bool> IsWithinAvailabilityAsync(long professionalId, DateTime startUtc, int durationMinutes)
    {
        var professional = await professionalRepository.FindByIdAsync(professionalId);
        return SlotAvailabilityCalculator.IsScheduledStart(professional?.AvailabilitySchedule, startUtc,
            durationMinutes);
    }

    private static ProfessionalReference? ToReference(Professional? professional) =>
        professional is null
            ? null
            : new ProfessionalReference(professional.Id, professional.UserId, professional.IsVisibleToPatients);
}

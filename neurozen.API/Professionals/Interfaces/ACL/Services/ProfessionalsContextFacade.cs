using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Time;

namespace neurozen.API.Professionals.Interfaces.ACL.Services;

public class ProfessionalsContextFacade(IProfessionalRepository professionalRepository) : IProfessionalsContextFacade
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

    public async Task<ProfessionalReference?> FindByIdAsync(long professionalId) =>
        ToReference(await professionalRepository.FindByIdAsync(professionalId));

    public async Task<ProfessionalReference?> FindByUserIdAsync(long userId) =>
        ToReference(await professionalRepository.FindByUserIdAsync(userId));

    public async Task<bool> IsWithinAvailabilityAsync(long professionalId, DateTime startUtc)
    {
        var professional = await professionalRepository.FindByIdAsync(professionalId);
        if (professional?.AvailabilitySchedule is null) return false;
        if (startUtc.Second != 0 || startUtc.Millisecond != 0) return false;

        var (date, time) = LimaTime.FromUtc(startUtc);
        return professional.AvailabilitySchedule.SlotsFor(date.DayOfWeek).Contains(time);
    }

    private static ProfessionalReference? ToReference(Professional? professional) =>
        professional is null
            ? null
            : new ProfessionalReference(professional.Id, professional.UserId, professional.IsVisibleToPatients);
}

using System.Globalization;
using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Queries;
using neurozen.API.Professionals.Domain.Model.ReadModels;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Model.Time;

namespace neurozen.API.Professionals.Application.Internal.QueryServices;

public class ProfessionalQueryService(
    IProfessionalRepository professionalRepository,
    IIamContextFacade iamContextFacade,
    IAppointmentsContextFacade appointmentsContextFacade) : IProfessionalQueryService
{
    private const string NotFoundMessage = "El profesional no existe";

    public async Task<IReadOnlyList<ProfessionalDetails>> Handle(GetAllProfessionalsQuery query) =>
        await WithOwnersAsync((await professionalRepository.ListVerifiedAsync()).ToList());

    public async Task<ProfessionalDetails> Handle(GetProfessionalByIdQuery query)
    {
        var professional = await professionalRepository.FindByIdAsync(query.Id);
        if (professional is not { IsVisibleToPatients: true })
            throw new ResourceNotFoundException(NotFoundMessage);
        return await WithOwnerAsync(professional);
    }

    public async Task<ProfessionalDetails> Handle(GetMyProfessionalProfileQuery query)
    {
        var professional = await professionalRepository.FindByUserIdAsync(query.UserId);
        if (professional is null || professional.IsDeleted)
            throw new ResourceNotFoundException("Tu cuenta no tiene un perfil profesional");
        return await WithOwnerAsync(professional);
    }

    public async Task<IReadOnlyList<ProfessionalDetails>> Handle(GetProfessionalsForAdminQuery query)
    {
        VerificationStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!VerificationStatusExtensions.TryFromWire(query.Status, out var parsed))
                throw new ValidationFailedException("El estado debe ser PENDING, VERIFIED o REJECTED",
                    new Dictionary<string, string[]>
                        { ["status"] = ["El estado debe ser PENDING, VERIFIED o REJECTED"] });
            status = parsed;
        }

        return await WithOwnersAsync(await professionalRepository.ListForAdminAsync(status));
    }

    public async Task<IReadOnlyList<string>> Handle(GetAvailableSlotsQuery query)
    {
        if (!DateOnly.TryParseExact((query.Date ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            throw new ValidationFailedException("La fecha debe tener el formato YYYY-MM-DD",
                new Dictionary<string, string[]> { ["date"] = ["La fecha debe tener el formato YYYY-MM-DD"] });

        var professional = await professionalRepository.FindByIdAsync(query.ProfessionalId);
        if (professional is not { IsVisibleToPatients: true })
            throw new ResourceNotFoundException(NotFoundMessage);

        var nowUtc = DateTime.UtcNow;
        if (professional.AvailabilitySchedule is null || date < LimaTime.Today(nowUtc)) return [];

        var slots = professional.AvailabilitySchedule.SlotsFor(date.DayOfWeek);
        if (slots.Count == 0) return [];

        var dayStartUtc = LimaTime.ToUtc(date, TimeOnly.MinValue);
        var booked = (await appointmentsContextFacade.ListBookedStartTimesAsync(
                professional.Id, dayStartUtc, dayStartUtc.AddDays(1)))
            .Select(TruncateToMinute)
            .ToHashSet();

        return slots
            .Where(slot =>
            {
                var slotUtc = LimaTime.ToUtc(date, slot);
                return slotUtc > nowUtc && !booked.Contains(slotUtc);
            })
            .Select(slot => slot.ToString("HH:mm", CultureInfo.InvariantCulture))
            .ToList();
    }

    private async Task<ProfessionalDetails> WithOwnerAsync(Professional professional) =>
        (await WithOwnersAsync([professional]))[0];

    private async Task<IReadOnlyList<ProfessionalDetails>> WithOwnersAsync(IReadOnlyList<Professional> professionals)
    {
        var owners = await iamContextFacade.FetchUserSummariesAsync(professionals.Select(p => p.UserId));
        return professionals
            .Select(p => owners.TryGetValue(p.UserId, out var owner)
                ? new ProfessionalDetails(p, owner.FullName, owner.Email)
                : new ProfessionalDetails(p, string.Empty, string.Empty))
            .ToList();
    }

    private static DateTime TruncateToMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, DateTimeKind.Utc);
}

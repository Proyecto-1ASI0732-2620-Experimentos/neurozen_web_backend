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
        var errors = new ValidationErrors();
        var date = ParseDate(query.Date, "date", errors);
        var duration = ResolveDuration(query.DurationMinutes, errors);
        errors.ThrowIfAny(date is null ? DateFormatMessage : "Hay datos inválidos en la solicitud");

        var professional = await FindBookableAsync(query.ProfessionalId);

        var nowUtc = DateTime.UtcNow;
        if (professional.AvailabilitySchedule is null || date!.Value < LimaTime.Today(nowUtc) ||
            professional.AvailabilitySchedule.SlotsFor(date.Value.DayOfWeek, duration).Count == 0)
            return [];

        var booked = await LoadBookedIntervalsAsync(professional.Id, date.Value, date.Value);
        return FormatSlots(SlotAvailabilityCalculator.FreeSlots(professional.AvailabilitySchedule, date.Value,
            duration, booked, nowUtc));
    }

    public async Task<AvailabilityCalendar> Handle(GetAvailabilityRangeQuery query)
    {
        var errors = new ValidationErrors();
        var startDate = ParseDate(query.StartDate, "startDate", errors);
        var endDate = ParseDate(query.EndDate, "endDate", errors);
        var duration = ResolveDuration(query.DurationMinutes, errors);
        if (startDate is { } first && endDate is { } last)
        {
            errors.AddIf(last < first, "endDate", "endDate no puede ser anterior a startDate");
            errors.AddIf(last.DayNumber - first.DayNumber + 1 > GetAvailabilityRangeQuery.MaxDays, "endDate",
                $"El rango no puede superar {GetAvailabilityRangeQuery.MaxDays} días");
        }

        errors.ThrowIfAny();

        var professional = await FindBookableAsync(query.ProfessionalId);
        var schedule = professional.AvailabilitySchedule;
        var nowUtc = DateTime.UtcNow;

        // One query covers the requested range and the search horizon of nextAvailableDate.
        var searchFrom = Max(startDate!.Value, LimaTime.Today(nowUtc));
        var horizon = endDate!.Value.AddDays(GetAvailabilityRangeQuery.MaxDays);
        // Dates before today never have free slots, so bookings are only loaded from searchFrom.
        var booked = schedule is null || horizon < searchFrom
            ? []
            : await LoadBookedIntervalsAsync(professional.Id, searchFrom, horizon);

        var days = new List<AvailabilityDay>();
        for (var date = startDate.Value; date <= endDate.Value; date = date.AddDays(1))
            days.Add(new AvailabilityDay(date,
                FormatSlots(SlotAvailabilityCalculator.FreeSlots(schedule, date, duration, booked, nowUtc))));

        DateOnly? nextAvailable = null;
        if (schedule is not null)
            for (var date = searchFrom; date <= horizon && nextAvailable is null; date = date.AddDays(1))
                if (SlotAvailabilityCalculator.FreeSlots(schedule, date, duration, booked, nowUtc).Count > 0)
                    nextAvailable = date;

        return new AvailabilityCalendar(professional.Id, SlotAvailabilityCalculator.TimeZoneId, duration, days,
            nextAvailable);
    }

    private const string DateFormatMessage = "La fecha debe tener el formato YYYY-MM-DD";

    private async Task<Professional> FindBookableAsync(long professionalId)
    {
        var professional = await professionalRepository.FindByIdAsync(professionalId);
        if (professional is not { IsVisibleToPatients: true })
            throw new ResourceNotFoundException(NotFoundMessage);
        return professional;
    }

    /// <summary>Booked ranges from the Lima start of <paramref name="from" /> to the Lima end of <paramref name="to" />.</summary>
    private async Task<IReadOnlyList<BookedInterval>> LoadBookedIntervalsAsync(long professionalId, DateOnly from,
        DateOnly to)
    {
        var fromUtc = LimaTime.ToUtc(from, TimeOnly.MinValue);
        var toUtc = LimaTime.ToUtc(to.AddDays(1), TimeOnly.MinValue);
        var booked = await appointmentsContextFacade.ListBookedSlotsAsync(professionalId, fromUtc, toUtc)
                     ?? [];
        return booked
            .Select(slot => BookedInterval.FromStart(TruncateToMinute(slot.StartUtc), slot.DurationMinutes))
            .ToList();
    }

    private static DateOnly? ParseDate(string? value, string field, ValidationErrors errors)
    {
        if (DateOnly.TryParseExact((value ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
            return date;
        errors.Add(field, DateFormatMessage);
        return null;
    }

    private static int ResolveDuration(int? durationMinutes, ValidationErrors errors)
    {
        var duration = durationMinutes ?? SlotAvailabilityCalculator.DefaultDurationMinutes;
        errors.AddIf(!SlotAvailabilityCalculator.IsValidDuration(duration), "durationMinutes",
            $"durationMinutes debe estar entre {SlotAvailabilityCalculator.MinDurationMinutes} y " +
            $"{SlotAvailabilityCalculator.MaxDurationMinutes}");
        return duration;
    }

    private static List<string> FormatSlots(IEnumerable<TimeOnly> slots) =>
        slots.Select(slot => slot.ToString("HH:mm", CultureInfo.InvariantCulture)).ToList();

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private async Task<ProfessionalDetails> WithOwnerAsync(Professional professional) =>
        (await WithOwnersAsync([professional]))[0];

    private async Task<IReadOnlyList<ProfessionalDetails>> WithOwnersAsync(IReadOnlyList<Professional> professionals)
    {
        var owners = await iamContextFacade.FetchUserSummariesAsync(professionals.Select(p => p.UserId));
        return professionals
            .Select(p => owners.TryGetValue(p.UserId, out var owner)
                ? new ProfessionalDetails(p, owner.FullName, owner.Email, owner.Dni)
                : new ProfessionalDetails(p, string.Empty, string.Empty))
            .ToList();
    }

    private static DateTime TruncateToMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, DateTimeKind.Utc);
}

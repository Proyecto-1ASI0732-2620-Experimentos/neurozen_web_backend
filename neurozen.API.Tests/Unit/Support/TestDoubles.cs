using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Shared.Application.Security;

namespace neurozen.API.Tests.Unit.Support;

/// <summary>Authenticated caller for application services (what the JWT would provide).</summary>
public sealed class FakeCurrentUser(long id, string role) : ICurrentUser
{
    public bool IsAuthenticated => true;
    public long Id { get; } = id;
    public string Role { get; } = role;
    public bool IsInRole(string role) => string.Equals(Role, role, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Professional profiles in a known state (fictitious data).</summary>
public static class TestProfessionals
{
    public static AvailabilitySchedule AllWeek(string start, string end) =>
        new(AvailabilitySchedule.WeekDays, start, end);

    public static Professional Pending(long userId = 20, string license = "45821", AvailabilitySchedule? schedule = null)
    {
        var professional = new Professional(userId, Specialty.Clinical, license);
        var effective = schedule ?? AllWeek("09:00", "13:00");
        professional.UpdateProfile(Specialty.Clinical, license, "Psicóloga clínica con experiencia en estrés laboral.",
            8, 80m, effective, effective.Describe(), false);
        return professional;
    }

    public static Professional Verified(long userId = 20, string license = "45821",
        AvailabilitySchedule? schedule = null)
    {
        var professional = Pending(userId, license, schedule);
        professional.Verify(1, DateTime.UtcNow);
        return professional;
    }
}

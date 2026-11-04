using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Professionals.Domain.Model.Aggregates;
using neurozen.API.Professionals.Domain.Model.Entities;
using neurozen.API.Professionals.Domain.Model.ValueObjects;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.Shared.Infrastructure.Seeding;

/// <summary>
///     Fictitious demo accounts from the specification (section 7), enabled with Seed:Demo:Enabled
///     (only in appsettings.Development.json). Passwords come from Seed:Demo:PatientPassword and
///     Seed:Demo:PsychologistPassword. Existing accounts are never modified.
/// </summary>
public static class DemoDataSeeder
{
    private const string PatientEmail = "ana@neurozen.com";
    private const string PsychologistEmail = "elena@psico.pe";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        if (!configuration.GetValue("Seed:Demo:Enabled", false)) return;

        var users = services.GetRequiredService<IUserRepository>();
        var hashing = services.GetRequiredService<IHashingService>();
        var professionals = services.GetRequiredService<IProfessionalRepository>();
        var audits = services.GetRequiredService<IVerificationAuditRepository>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        var patientPassword = configuration["Seed:Demo:PatientPassword"];
        if (!string.IsNullOrEmpty(patientPassword) && !await users.ExistsByEmailAsync(PatientEmail))
        {
            await users.AddAsync(new User(PatientEmail, hashing.HashPassword(patientPassword), "Ana Torres",
                UserRole.User, DateTime.UtcNow));
            await unitOfWork.CompleteAsync();
            logger.LogInformation("Demo patient account created");
        }

        var psychologistPassword = configuration["Seed:Demo:PsychologistPassword"];
        if (string.IsNullOrEmpty(psychologistPassword) || await users.ExistsByEmailAsync(PsychologistEmail)) return;

        var admin = await users.FindByEmailAsync(EmailAddress.Normalize(configuration["Seed:Admin:Email"]));
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = new User(PsychologistEmail, hashing.HashPassword(psychologistPassword), "Dra. Elena Ramírez",
                UserRole.Psychologist, DateTime.UtcNow);
            await users.AddAsync(user);
            await unitOfWork.CompleteAsync();

            var schedule = new AvailabilitySchedule(["monday", "wednesday"], "09:00", "13:00");
            var professional = new Professional(user.Id, Specialty.Clinical, "45821");
            professional.UpdateProfile(Specialty.Clinical, "45821",
                "Psicóloga clínica con experiencia en manejo del estrés laboral y ansiedad.",
                8, 80m, schedule, schedule.Describe(), false);
            await professionals.AddAsync(professional);
            await unitOfWork.CompleteAsync();

            if (admin is not null)
            {
                var now = DateTime.UtcNow;
                professional.Verify(admin.Id, now);
                await audits.AddAsync(new VerificationAuditEntry(professional.Id, admin.Id,
                    VerificationAuditEntry.VerifiedAction, null, now));
                await unitOfWork.CompleteAsync();
            }
        });

        logger.LogInformation(admin is null
            ? "Demo psychologist created as PENDING (no admin account found to verify it)"
            : "Demo psychologist created and VERIFIED");
    }
}

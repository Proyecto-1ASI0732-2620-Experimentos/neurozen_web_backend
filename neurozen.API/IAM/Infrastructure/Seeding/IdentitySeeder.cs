using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Domain.Model.Aggregates;
using neurozen.API.IAM.Domain.Model.ValueObjects;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.Shared.Domain.Model.Exceptions;
using neurozen.API.Shared.Domain.Repositories;

namespace neurozen.API.IAM.Infrastructure.Seeding;

/// <summary>
///     Creates the administrator account (there is no public sign-up for ADMIN).
///     Reads Seed:Admin:Email, Seed:Admin:Password and Seed:Admin:FullName from configuration.
///     Never overwrites an existing account.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAdminAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var email = EmailAddress.Normalize(configuration["Seed:Admin:Email"]);
        var password = configuration["Seed:Admin:Password"];
        var fullName = configuration["Seed:Admin:FullName"] ?? "Administrador NeuroZen";

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning("Admin seeding skipped: Seed:Admin:Email / Seed:Admin:Password are not configured");
            return;
        }

        var errors = new ValidationErrors();
        errors.AddIf(!EmailAddress.IsValid(email), "Seed:Admin:Email", "invalid email");
        PasswordPolicy.Validate(password, "Seed:Admin:Password", errors);
        if (errors.HasErrors)
        {
            logger.LogError("Admin seeding skipped: the configured admin email or password does not meet the policy");
            return;
        }

        var users = services.GetRequiredService<IUserRepository>();
        if (await users.ExistsByEmailAsync(email)) return;

        var hashing = services.GetRequiredService<IHashingService>();
        var admin = new User(email, hashing.HashPassword(password), fullName, UserRole.Admin, DateTime.UtcNow);
        await users.AddAsync(admin);
        await services.GetRequiredService<IUnitOfWork>().CompleteAsync();
        logger.LogInformation("Admin account created");
    }
}

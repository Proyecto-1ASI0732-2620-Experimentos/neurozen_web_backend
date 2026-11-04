using Microsoft.EntityFrameworkCore;
using neurozen.API.IAM.Infrastructure.Persistence.EFC.Configuration;

namespace neurozen.API.IAM.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new PasswordResetTokenConfiguration());
    }
}

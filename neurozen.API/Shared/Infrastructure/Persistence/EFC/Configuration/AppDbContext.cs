using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using neurozen.API.IAM.Infrastructure.Persistence.EFC.Configuration.Extensions;
using Microsoft.EntityFrameworkCore;

namespace neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
///     Application database context
/// </summary>
public class AppDbContext(DbContextOptions options) : DbContext(options)
{

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        base.OnConfiguring(builder);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.UseSnakeCaseNamingConvention();

        // Apply EF Core configurations explicitly from each bounded context.
        // This approach avoids applying duplicate configurations from Shared namespace.
        // Each bounded context manages its own entities: Catalog, UserManagement, Sales, and Payments.
        builder.ApplyConfiguration(new Catalog.Infrastructure.Persistence.EFC.Configuration.CategoryConfiguration());
        builder.ApplyConfiguration(new Catalog.Infrastructure.Persistence.EFC.Configuration.ProductConfiguration());
        builder.ApplyConfiguration(new Catalog.Infrastructure.Persistence.EFC.Configuration.ProductImageConfiguration());

        builder.ApplyConfiguration(new UserManagement.Infrastructure.Persistence.EFC.Configuration.UserConfiguration());
        builder.ApplyConfiguration(new UserManagement.Infrastructure.Persistence.EFC.Configuration.SessionConfiguration());
        builder.ApplyConfiguration(new UserManagement.Infrastructure.Persistence.EFC.Configuration.AddressConfiguration());
        builder.ApplyConfiguration(new UserManagement.Infrastructure.Persistence.EFC.Configuration.NotificationConfiguration());

        builder.ApplyConfiguration(new Sales.Infrastructure.Persistence.EFC.Configuration.CartConfiguration());
        builder.ApplyConfiguration(new Sales.Infrastructure.Persistence.EFC.Configuration.CartItemConfiguration());
        builder.ApplyConfiguration(new Sales.Infrastructure.Persistence.EFC.Configuration.OrderConfiguration());
        builder.ApplyConfiguration(new Sales.Infrastructure.Persistence.EFC.Configuration.OrderItemConfiguration());
        builder.ApplyConfiguration(new Sales.Infrastructure.Persistence.EFC.Configuration.AppSettingConfiguration());

        builder.ApplyConfiguration(new Payments.Infrastructure.Persistence.EFC.Configuration.PaymentConfiguration());

        builder.ApplyConfiguration(
            new Appointments.Infrastructure.Persistence.EFC.Configuration.AppointmentConfiguration());

        builder.ApplyConfiguration(new Professionals.Infrastructure.Persistence.EFC.Configuration.ProfessionalConfiguration());
        
        builder.ApplyConfiguration(
            new Triggers.Infraestructure.Persistence.EFC.Configuration.TriggerConfiguration());

        builder.ApplyConfiguration(
            new ResourcesLibrary.Infrastructure.Persistence.EFC.Configuration.ResourceLibraryConfiguration());

        builder.ApplyConfiguration(
            new Wellness.Infrastructure.Persistence.EFC.Configuration.MeditationConfiguration());

        builder.ApplyConfiguration(
            new Wellness.Infrastructure.Persistence.EFC.Configuration.HealthMetricConfiguration());

        // Apply IAM bounded context configuration
        builder.ApplyIamConfiguration();
 		
  }
}
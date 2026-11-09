using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using neurozen.API.Appointments.Application.Internal;
using neurozen.API.Appointments.Application.Internal.AccountDeletion;
using neurozen.API.Appointments.Application.Internal.CommandServices;
using neurozen.API.Appointments.Application.Internal.QueryServices;
using neurozen.API.Appointments.Domain.Repositories;
using neurozen.API.Appointments.Domain.Services;
using neurozen.API.Appointments.Interfaces.ACL;
using neurozen.API.Appointments.Interfaces.ACL.Services;
using neurozen.API.Appointments.Infrastructure.Repositories;
using neurozen.API.IAM.Application.Internal.AccountDeletion;
using neurozen.API.IAM.Application.Internal.CommandServices;
using neurozen.API.IAM.Application.Internal.Configuration;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Application.Internal.QueryServices;
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.IAM.Domain.Services;
using neurozen.API.IAM.Infrastructure.Email;
using neurozen.API.IAM.Infrastructure.Hashing.BCrypt.Services;
using neurozen.API.IAM.Infrastructure.Persistence.EFC.Repositories;
using neurozen.API.IAM.Infrastructure.Seeding;
using neurozen.API.IAM.Infrastructure.Throttling;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Extensions;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Services;
using neurozen.API.IAM.Interfaces.ACL;
using neurozen.API.IAM.Interfaces.ACL.Services;
using neurozen.API.Professionals.Application.Internal.AccountDeletion;
using neurozen.API.Professionals.Application.Internal.CommandServices;
using neurozen.API.Professionals.Application.Internal.QueryServices;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Professionals.Infrastructure.Authorization;
using neurozen.API.Professionals.Infrastructure.Repositories;
using neurozen.API.Professionals.Interfaces.ACL;
using neurozen.API.Professionals.Interfaces.ACL.Services;
using neurozen.API.ResourcesLibrary.Application.Internal.CommandServices;
using neurozen.API.ResourcesLibrary.Application.Internal.QueryServices;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.ResourcesLibrary.Domain.Services;
using neurozen.API.ResourcesLibrary.Infrastructure.Repositories;
using neurozen.API.ResourcesLibrary.Infrastructure.Seeding;
using neurozen.API.Shared.Application.Accounts;
using neurozen.API.Shared.Application.Security;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.ErrorHandling;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.Json;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.RateLimiting;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.Security;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using neurozen.API.Shared.Infrastructure.Seeding;
using neurozen.API.Stress.Application.Internal.CommandServices;
using neurozen.API.Stress.Application.Internal.QueryServices;
using neurozen.API.Stress.Domain.Repositories;
using neurozen.API.Stress.Domain.Services;
using neurozen.API.Stress.Infrastructure.AccountDeletion;
using neurozen.API.Stress.Infrastructure.Repositories;
using neurozen.API.Subscriptions.Application.Internal.CommandServices;
using neurozen.API.Subscriptions.Application.Internal.QueryServices;
using neurozen.API.Subscriptions.Domain.Repositories;
using neurozen.API.Subscriptions.Domain.Services;
using neurozen.API.Subscriptions.Infrastructure.AccountDeletion;
using neurozen.API.Subscriptions.Infrastructure.Repositories;
using neurozen.API.Wellness.Infrastructure.AccountDeletion;
using neurozen.API.Wellness.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var environment = builder.Environment;

// ---------------------------------------------------------------- Web / JSON / errors
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter()))
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ---------------------------------------------------------------- CORS (only the frontend origins)
const string frontendCorsPolicy = "Frontend";
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() is { Length: > 0 } origins
    ? origins
    : new[] { "http://localhost:5173", "http://localhost:4173", "https://neurozen-web-frontend.vercel.app" };
builder.Services.AddCors(options => options.AddPolicy(frontendCorsPolicy, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ---------------------------------------------------------------- Reverse proxy (production behind TLS)
if (configuration.GetValue("ForwardedHeaders:Enabled", false))
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

// ---------------------------------------------------------------- Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT obtained from /api/v1/authentication/sign-in"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------------------------------------------------------- Database (no sensitive data logging)
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not set. Use the ConnectionStrings__DefaultConnection " +
        "environment variable or dotnet user-secrets.");

// Database:ServerVersion (e.g. "8.0.36-mysql") avoids a connection at startup just to detect the version.
var serverVersion = string.IsNullOrWhiteSpace(configuration["Database:ServerVersion"])
    ? ServerVersion.Parse("8.0")
    : ServerVersion.Parse(configuration["Database:ServerVersion"]);
builder.Services.AddDbContext<AppDbContext>(options => options.UseMySql(connectionString, serverVersion));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---------------------------------------------------------------- Security
builder.Services.Configure<TokenSettings>(configuration.GetSection("TokenSettings"));
builder.Services.AddJwtAuthentication(configuration, environment);
builder.Services.AddApiRateLimiting(configuration);
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();
builder.Services.AddProfessionalAuthorization();

// ---------------------------------------------------------------- IAM
builder.Services.Configure<PasswordResetSettings>(configuration.GetSection("PasswordReset"));
builder.Services.Configure<EmailSettings>(configuration.GetSection("Email"));
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IHashingService, HashingService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IPasswordResetCommandService, PasswordResetCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<IUserAccountCommandService, UserAccountCommandService>();
builder.Services.AddSingleton<IPasswordResetThrottle, MemoryPasswordResetThrottle>();
builder.Services.AddSingleton<BackgroundEmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(services => services.GetRequiredService<BackgroundEmailQueue>());
builder.Services.AddHostedService<EmailDispatcherService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();

// ---------------------------------------------------------------- Professionals
builder.Services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
builder.Services.AddScoped<IVerificationAuditRepository, VerificationAuditRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewCommandService, ReviewCommandService>();
builder.Services.AddScoped<IReviewQueryService, ReviewQueryService>();
builder.Services.AddScoped<IProfessionalQueryService, ProfessionalQueryService>();
builder.Services.AddScoped<IProfessionalProfileCommandService, ProfessionalProfileCommandService>();
builder.Services.AddScoped<IProfessionalVerificationCommandService, ProfessionalVerificationCommandService>();
builder.Services.AddScoped<IProfessionalsContextFacade, ProfessionalsContextFacade>();

// ---------------------------------------------------------------- Appointments
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<AppointmentAccess>();
builder.Services.AddScoped<IAppointmentCommandService, AppointmentCommandService>();
builder.Services.AddScoped<IAppointmentQueryService, AppointmentQueryService>();
builder.Services.AddScoped<IAppointmentsContextFacade, AppointmentsContextFacade>();

// ---------------------------------------------------------------- Stress: triggers, assessments, reports
builder.Services.AddScoped<IStressTriggerRepository, StressTriggerRepository>();
builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IStressTriggerCommandService, StressTriggerCommandService>();
builder.Services.AddScoped<IStressTriggerQueryService, StressTriggerQueryService>();
builder.Services.AddScoped<IAssessmentCommandService, AssessmentCommandService>();
builder.Services.AddScoped<IAssessmentQueryService, AssessmentQueryService>();
builder.Services.AddScoped<IReportCommandService, ReportCommandService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();

// ---------------------------------------------------------------- Resource library
builder.Services.AddScoped<IResourceLibraryRepository, ResourceLibraryRepository>();
builder.Services.AddScoped<IResourceLibraryCommandService, ResourceLibraryCommandService>();
builder.Services.AddScoped<IResourceLibraryQueryService, ResourceLibraryQueryService>();

// ---------------------------------------------------------------- Subscriptions
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionCommandService, SubscriptionCommandService>();
builder.Services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();

// ---------------------------------------------------------------- Account deletion participants (one per context)
builder.Services.AddScoped<IUserDataEraser, WellnessUserDataEraser>();
builder.Services.AddScoped<IUserDataEraser, SubscriptionUserDataEraser>();
builder.Services.AddScoped<IUserDataEraser, StressUserDataEraser>();
builder.Services.AddScoped<IUserDataEraser, AppointmentUserDataEraser>();
builder.Services.AddScoped<IUserDataEraser, ProfessionalUserDataEraser>();
builder.Services.AddScoped<IUserDataEraser, IamUserDataEraser>();

var app = builder.Build();

// ---------------------------------------------------------------- Database migrations + seeding
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var context = services.GetRequiredService<AppDbContext>();

    if (context.Database.GetMigrations().Any())
    {
        await context.Database.MigrateAsync();
        await IdentitySeeder.SeedAdminAsync(services, configuration, logger);
        await DemoDataSeeder.SeedAsync(services, configuration, logger);
        await ResourceLibrarySeeder.SeedAsync(services, configuration);
        await WellnessSeeder.SeedAsync(context);
    }
    else
    {
        logger.LogWarning("No EF Core migrations found; the database was not created. " +
                          "Run: dotnet ef migrations add InitialCreate");
    }
}

// ---------------------------------------------------------------- HTTP pipeline
if (configuration.GetValue("ForwardedHeaders:Enabled", false))
    app.UseForwardedHeaders();

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment() || configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(frontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program
{
}

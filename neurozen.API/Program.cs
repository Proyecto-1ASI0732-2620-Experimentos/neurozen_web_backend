using neurozen.API.Shared.Infrastructure.Persistence.EFC.Configuration;
// IAM imports
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using neurozen.API.IAM.Infrastructure.Tokens.JWT.Services;
using neurozen.API.IAM.Application.Internal.OutboundServices;
using neurozen.API.IAM.Infrastructure.Hashing.BCrypt.Services;
using neurozen.API.IAM.Infrastructure.Persistence.EFC.Repositories;
using neurozen.API.IAM.Application.Internal.CommandServices;
using neurozen.API.IAM.Application.Internal.QueryServices;
using neurozen.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using neurozen.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
// IAM domain interfaces (ensure interface types are in scope)
using neurozen.API.IAM.Domain.Repositories;
using neurozen.API.IAM.Domain.Services;
using Microsoft.EntityFrameworkCore;
using neurozen.API.Appointments.Application.Internal.CommandServices;
using neurozen.API.Appointments.Application.Internal.QueryServices;
using neurozen.API.Appointments.Domain.Repositories;
using neurozen.API.Appointments.Domain.Services;
using neurozen.API.Appointments.Infrastructure.Repositories;
using neurozen.API.Professionals.Application.Internal.CommandServices;
using neurozen.API.Professionals.Application.Internal.QueryServices;
using neurozen.API.Professionals.Domain.Repositories;
using neurozen.API.Professionals.Domain.Services;
using neurozen.API.Professionals.Infrastructure.Repositories;
using neurozen.API.Resources;
using neurozen.API.ResourcesLibrary.Application.Internal.CommandServices;
using neurozen.API.ResourcesLibrary.Domain.Repositories;
using neurozen.API.ResourcesLibrary.Domain.Services;
using neurozen.API.ResourcesLibrary.Infrastructure.Repositories;
using neurozen.API.Triggers.Application.Internal.CommandServices;
using neurozen.API.Triggers.Domain.Repositories;
using neurozen.API.Triggers.Domain.Services;
using neurozen.API.Triggers.Infraestructure.Respositories;
using neurozen.API.Subscriptions.Application.Internal.CommandServices;
using neurozen.API.Subscriptions.Domain.Repositories; 
using neurozen.API.Subscriptions.Domain.Services;
using neurozen.API.Subscriptions.Infraestructure.Respositories;
using neurozen.API.Shared.Domain.Repositories;
using neurozen.API.Shared.Infrastructure.Interfaces.ASP.Configuration;
using neurozen.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddRouting(options => options.LowercaseUrls = true );
builder.Services.AddEndpointsApiExplorer();

// Allow the local mobile frontend to call the API during development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add controllers and apply a global authorization filter so every endpoint requires
// authorization by default. Controllers/actions decorated with [AllowAnonymous]
// will skip this filter.
builder.Services.AddControllers(options =>
{
    // Require authenticated user by default for all controllers; AllowAnonymous will opt-out.
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
}).AddDataAnnotationsLocalization(options =>
{
    options.DataAnnotationLocalizerProvider = (type, factory) =>
    {
        return factory.Create(typeof(SharedResource));
    };
});

//Localization

builder.Services.AddLocalization();
var LocalizationOptions = new RequestLocalizationOptions();


LocalizationOptions.SetDefaultCulture("es-PE");
LocalizationOptions.AddSupportedCultures("es-PE", "en-US");
LocalizationOptions.AddSupportedUICultures("es-PE", "en-US");
LocalizationOptions.ApplyCurrentCultureToResponseHeaders = true;

// Enable Swashbuckle and configure JWT Bearer support in Swagger UI
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
        Description = "Enter 'Bearer' [space] and then your valid token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});


// Register AppDbContext for all environments so DI is available during startup.
builder.Services.AddDbContext<AppDbContext>(
    options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        // Verify Database Connection String
        if (connectionString is null)
            // Stop the application if the connection string is not set.
            throw new Exception("Database connection string is not set.");
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    });
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// IAM / Identity registrations
// Bind TokenSettings from configuration
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();

// Configure JWT authentication
var tokenSecret = builder.Configuration["TokenSettings:Secret"] ?? Environment.GetEnvironmentVariable("TokenSettings__Secret");
if (string.IsNullOrEmpty(tokenSecret))
    throw new Exception("TokenSettings:Secret must be provided via configuration or TokenSettings__Secret env var");
var key = Encoding.ASCII.GetBytes(tokenSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// Appointments services
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentCommandService, AppointmentCommandService>();
builder.Services.AddScoped<IAppointmentQueryService, AppointmentQueryService>();

// Professionals services
builder.Services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
builder.Services.AddScoped<IProfessionalCommandService, ProfessionalCommandService>();
builder.Services.AddScoped<IProfessionalQueryService, ProfessionalQueryService>();

// Resource library services
builder.Services.AddScoped<IResourceLibraryRepository, ResourceLibraryRepository>();
builder.Services.AddScoped<IResourceLibraryCommandService, ResourceLibraryCommandService>();

// Triggers services
builder.Services.AddScoped<ITriggerRepository, TriggerRepository>();
builder.Services.AddScoped<ITriggerCommandService, TriggerCommandService>();

// Subscriptions services
builder.Services.AddScoped<ISubscriptionRepository, SubcriptionRepository>();
builder.Services.AddScoped<ISubscriptionCommandService, SubscriptionCommandService>();

var app = builder.Build();

// Verify Database Objects are created
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    context.Database.ExecuteSqlRaw("""
        CREATE TABLE IF NOT EXISTS meditations (
            id INT NOT NULL AUTO_INCREMENT,
            title VARCHAR(200) NOT NULL,
            description VARCHAR(1000) NOT NULL,
            duration_minutes INT NOT NULL,
            image_url VARCHAR(500) NOT NULL,
            audio_url VARCHAR(500) NOT NULL,
            PRIMARY KEY (id)
        ) CHARACTER SET utf8mb4;

        CREATE TABLE IF NOT EXISTS health_metrics (
            id INT NOT NULL AUTO_INCREMENT,
            user_id CHAR(36) NOT NULL,
            stress_level INT NOT NULL,
            heart_rate INT NULL,
            sleep_hours DECIMAL(4, 1) NULL,
            notes VARCHAR(1000) NULL,
            created_at DATETIME(6) NOT NULL,
            PRIMARY KEY (id),
            INDEX idx_health_metrics_user_id (user_id),
            INDEX idx_health_metrics_created_at (created_at)
        ) CHARACTER SET utf8mb4;

        INSERT INTO meditations (title, description, duration_minutes, image_url, audio_url)
        SELECT 'Calma Mental y Mindfulness',
               'Sesion guiada para reducir la rumiacion cognitiva.',
               15,
               'https://images.unsplash.com/photo-1518199266791',
               'https://www.soundhelix.com/examples/mp3/SoundHelix-Song-1.mp3'
        WHERE NOT EXISTS (SELECT 1 FROM meditations);
        """);
}
app.UseRequestLocalization(LocalizationOptions);
app.UseHttpsRedirection();
app.UseCors("LocalDevelopment");
// Authentication middleware must run before authorization
app.UseAuthentication();
// Custom request authorization middleware (validates JWT and sets HttpContext.Items["User"])
app.UseRequestAuthorization();
app.UseAuthorization();
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();


app.Run();

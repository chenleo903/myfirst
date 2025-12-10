using System.Text.Json.Serialization;
using CrmSystem.Api.Data;
using CrmSystem.Api.Helpers;
using CrmSystem.Api.Middleware;
using CrmSystem.Api.Repositories;
using CrmSystem.Api.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/crm-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Log startup information
Log.Information("Starting CRM System API");
Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CrmDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    
    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Configure CORS
builder.Services.AddCrmCors(builder.Configuration);

// Configure Authentication (optional)
builder.Services.AddCrmAuthentication(builder.Configuration, out var authSettings);
Log.Information("Authentication enabled: {EnableAuth}", authSettings.EnableAuth);

// Configure JSON serialization options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Add custom DateTimeOffset converters for ISO 8601 format
        options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeOffsetConverter());
        
        // Configure enum serialization as strings
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        
        // Ignore null values in responses
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        
        // Use camelCase for property names
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Register Repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IInteractionRepository, InteractionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Register Services
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IInteractionService, InteractionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<MigrationService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Log non-sensitive configuration information at startup (Requirement 7.8)
Log.Information("Configuration Summary:");
Log.Information("  - Database: {DatabaseHost}", GetDatabaseHost(connectionString));
Log.Information("  - CORS Origins: {Origins}", string.Join(", ", builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>()));
Log.Information("  - Auto Migrate: {AutoMigrate}", builder.Configuration.GetValue<bool>("Migration:AutoMigrate", false));
Log.Information("  - JWT Expiry: {JwtExpiry} minutes", authSettings.JwtExpiryMinutes);

// Handle database migrations
var autoMigrate = builder.Configuration.GetValue<bool>("Migration:AutoMigrate", false);

if (autoMigrate)
{
    Log.Information("AUTO_MIGRATE is enabled. Attempting to apply database migrations...");
    
    try
    {
        using var scope = app.Services.CreateScope();
        var migrationService = scope.ServiceProvider.GetRequiredService<MigrationService>();
        await migrationService.MigrateAsync();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to apply database migrations. Application will not start.");
        return; // Prevent application startup
    }
}
else
{
    Log.Information("AUTO_MIGRATE is disabled. Skipping automatic database migrations.");
    Log.Information("To apply migrations manually, run: dotnet ef database update");
}

// Create initial admin user if authentication is enabled (Requirements 5.1-5.3)
if (authSettings.EnableAuth)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        
        var hasUsers = await authService.HasAnyUserAsync();
        if (!hasUsers)
        {
            Log.Information("No users found in database. Creating initial admin user...");
            await authService.CreateInitialAdminAsync(
                authSettings.AdminUsername!,
                authSettings.AdminPassword!);
            Log.Information("Initial admin user '{Username}' created successfully", authSettings.AdminUsername);
        }
        else
        {
            Log.Information("Users already exist in database. Skipping initial admin creation.");
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to create initial admin user. Application will not start.");
        return; // Prevent application startup (Requirement 5.3)
    }
}

// Configure the HTTP request pipeline

// Exception handling middleware (should be first to catch all exceptions)
app.UseExceptionHandling();

// Request logging middleware
app.UseRequestLogging();

// Swagger (development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS middleware
app.UseCrmCors();

// Authentication middleware (only if enabled)
app.UseCrmAuthentication(authSettings);

app.MapControllers();

try
{
    Log.Information("CRM System API started successfully at {StartTime}", DateTimeOffset.UtcNow);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Extracts the database host from connection string without exposing sensitive information
/// </summary>
static string GetDatabaseHost(string connectionString)
{
    try
    {
        var parts = connectionString.Split(';')
            .Select(p => p.Trim())
            .Where(p => p.StartsWith("Host=", StringComparison.OrdinalIgnoreCase) ||
                       p.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        
        if (parts != null)
        {
            var value = parts.Split('=', 2);
            if (value.Length == 2)
            {
                return value[1];
            }
        }
        return "[unknown]";
    }
    catch
    {
        return "[unknown]";
    }
}


// Make the implicit Program class public so test projects can access it
public partial class Program { }

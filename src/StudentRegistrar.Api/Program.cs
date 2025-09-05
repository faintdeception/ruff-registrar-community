using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Data;
using StudentRegistrar.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AutoMapper;
using StudentRegistrar.Api.DTOs;
using Npgsql.EntityFrameworkCore.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add database
builder.AddNpgsqlDbContext<StudentRegistrarDbContext>("studentregistrar");

// Add enhanced logging for debugging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Debug);
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add repository services
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.IStudentRepository, StudentRegistrar.Data.Repositories.StudentRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.IAccountHolderRepository, StudentRegistrar.Data.Repositories.AccountHolderRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.IEnrollmentRepository, StudentRegistrar.Data.Repositories.EnrollmentRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.ISemesterRepository, StudentRegistrar.Data.Repositories.SemesterRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.ICourseRepository, StudentRegistrar.Data.Repositories.CourseRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.IPaymentRepository, StudentRegistrar.Data.Repositories.PaymentRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.ICourseInstructorRepository, StudentRegistrar.Data.Repositories.CourseInstructorRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.IGradeRepository, StudentRegistrar.Data.Repositories.GradeRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.IEducatorRepository, StudentRegistrar.Data.Repositories.EducatorRepository>();
builder.Services.AddScoped<StudentRegistrar.Data.Repositories.IRoomRepository, StudentRegistrar.Data.Repositories.RoomRepository>();

// Add modernized application services
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseServiceV2, CourseServiceV2>();
builder.Services.AddScoped<IAccountHolderService, AccountHolderService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICourseInstructorService, CourseInstructorService>();
builder.Services.AddScoped<IGradeService, GradeService>();
builder.Services.AddScoped<IEducatorService, EducatorService>();
builder.Services.AddScoped<IKeycloakService, KeycloakService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Add HttpClient for Keycloak
builder.Services.AddHttpClient<IKeycloakService, KeycloakService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001") // Support both common Next.js ports
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakUrl = builder.Configuration.GetConnectionString("keycloak") ?? "http://localhost:8080";
        var realm = builder.Configuration["Keycloak:Realm"] ?? "student-registrar";
        
        options.Authority = $"{keycloakUrl}/realms/{realm}";
        options.Audience = "student-registrar";
        options.RequireHttpsMetadata = false; // Only for development
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = new[] { "student-registrar", "account" }, // Accept both audiences
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
        
        // Configure JWT token handling for Keycloak roles
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var claimsIdentity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    // Extract roles from Keycloak's realm_access structure
                    var realmAccessClaim = context.Principal?.FindFirst("realm_access")?.Value;
                    if (!string.IsNullOrEmpty(realmAccessClaim))
                    {
                        try
                        {
                            var realmAccess = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(realmAccessClaim);
                            if (realmAccess != null && realmAccess.ContainsKey("roles"))
                            {
                                var rolesElement = (System.Text.Json.JsonElement)realmAccess["roles"];
                                if (rolesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var roleElement in rolesElement.EnumerateArray())
                                    {
                                        if (roleElement.ValueKind == System.Text.Json.JsonValueKind.String)
                                        {
                                            var role = roleElement.GetString();
                                            if (!string.IsNullOrEmpty(role))
                                            {
                                                claimsIdentity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // Ignore JSON parsing errors
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Apply migrations on startup in development
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<StudentRegistrarDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var connectionString = dbContext.Database.GetConnectionString();
        logger.LogInformation($"Using connection string: {connectionString}");
        
        // Add retry logic for database connection
        var maxRetries = 10;
        var delay = TimeSpan.FromSeconds(2);
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                logger.LogInformation($"Starting database migration attempt {i + 1}...");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migration completed successfully.");
                break;
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                logger.LogWarning($"Database migration attempt {i + 1} failed: {ex.Message}. Retrying in {delay.TotalSeconds} seconds...");
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database migration failed after {MaxRetries} attempts: {Message}", maxRetries, ex.Message);
                throw;
            }
        }
    }
}

// Only use HTTPS redirection in production
if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();

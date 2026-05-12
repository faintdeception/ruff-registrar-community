using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentRegistrar.Data;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using AutoMapper;
using StudentRegistrar.Api.DTOs;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var httpContextAccessor = new HttpContextAccessor();
const string authSessionIdClaimType = "studentregistrar:session_id";
const string csrfHeaderName = "X-CSRF-TOKEN";

var allowUntrustedKeycloakCertificates =
    builder.Configuration.GetValue<bool?>("Keycloak:AllowUntrustedCertificates")
    ?? builder.Environment.IsDevelopment();

// Add service defaults & Aspire components
builder.AddServiceDefaults();
builder.Services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add tenant services for multi-tenancy support
builder.Services.AddTenantServices();
builder.Services.AddScoped<ITenantProvider, TenantContextProvider>();

// Add tenant authorization to validate user's tenant membership
// This prevents cross-tenant access even if Host header is spoofed
builder.Services.AddTenantAuthorization();

// Add database connection configuration via Aspire
builder.Services.AddDbContext<StudentRegistrarDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("studentregistrar");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'studentregistrar' is not configured.");
    }

    connectionString = NormalizePostgresConnectionString(connectionString);

    options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure());
});

// Keep deployed logs lean; local development can stay more talkative.
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(builder.Environment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning);
});

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

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
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<IAuthSessionStore, MemoryAuthSessionStore>();

static string NormalizePostgresConnectionString(string connectionString)
{
    var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
    var host = connectionStringBuilder.Host ?? string.Empty;

    if (host.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase)
        && Uri.TryCreate(host, UriKind.Absolute, out var hostUri))
    {
        connectionStringBuilder.Host = hostUri.Host;
        if (!hostUri.IsDefaultPort)
        {
            connectionStringBuilder.Port = hostUri.Port;
        }

        host = connectionStringBuilder.Host ?? string.Empty;
    }

    if (connectionStringBuilder.SslMode == SslMode.Prefer
        && IsLocalPostgresHost(host))
    {
        connectionStringBuilder.SslMode = SslMode.Disable;
    }

    return connectionStringBuilder.ConnectionString;
}

static bool IsLocalPostgresHost(string host)
{
    return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(host, "host.docker.internal", StringComparison.OrdinalIgnoreCase);
}

// Keycloak calls use a plain HttpClient to avoid Aspire service-discovery handlers
// interfering with absolute local Keycloak endpoints during auth flows.
builder.Services.AddScoped<IKeycloakService>(serviceProvider =>
{
    var handler = new HttpClientHandler();

    if (allowUntrustedKeycloakCertificates)
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    var httpClient = new HttpClient(handler);

    return new KeycloakService(
        httpClient,
        serviceProvider.GetRequiredService<IConfiguration>(),
        serviceProvider.GetRequiredService<ILogger<KeycloakService>>(),
        serviceProvider.GetRequiredService<IPasswordService>(),
        serviceProvider.GetService<ITenantContextAccessor>());
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();
        var defaultOrigins = new[] { "http://localhost:3000", "http://localhost:3001" };
        var allowedOrigins = configuredOrigins.Length > 0 ? configuredOrigins : defaultOrigins;
        var baseDomain = builder.Configuration["Tenancy:BaseDomain"] ?? "ruffregistrar.com";

        policy.SetIsOriginAllowed(origin => IsAllowedCorsOrigin(origin, allowedOrigins, baseDomain))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "SessionOrBearer";
        options.DefaultAuthenticateScheme = "SessionOrBearer";
        options.DefaultChallengeScheme = "SessionOrBearer";
    })
    .AddPolicyScheme("SessionOrBearer", "Session or Bearer", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            return !string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? JwtBearerDefaults.AuthenticationScheme
                : CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "studentregistrar.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.SameSite = builder.Environment.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None;
        options.SlidingExpiration = false;
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var sessionStore = context.HttpContext.RequestServices.GetRequiredService<IAuthSessionStore>();
                var sessionId = context.Principal?.FindFirst(authSessionIdClaimType)?.Value;

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var session = await sessionStore.GetAsync(sessionId, context.HttpContext.RequestAborted);
                if (session == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    })
    .AddJwtBearer(options =>
    {
        var realm = builder.Configuration["Keycloak:Realm"] ?? "student-registrar";
        var clientId = builder.Configuration["Keycloak:ClientId"] ?? "student-registrar";
        var publicClientId = builder.Configuration["Keycloak:PublicClientId"] ?? "student-registrar-spa";
        var authorityBaseUrl = builder.Configuration["Keycloak:Authority"];
        if (string.IsNullOrWhiteSpace(authorityBaseUrl))
        {
            var keycloakUrl = builder.Configuration.GetConnectionString("keycloak") ?? "http://localhost:8080";
            authorityBaseUrl = keycloakUrl;
        }

        var publicAuthorityBaseUrl = builder.Configuration["Keycloak:PublicAuthority"];

        authorityBaseUrl = authorityBaseUrl!.TrimEnd('/');
        publicAuthorityBaseUrl = publicAuthorityBaseUrl?.TrimEnd('/');
        var authority = BuildRealmAuthority(authorityBaseUrl, realm);

        options.Authority = authority;
        options.Audience = clientId;
        options.RequireHttpsMetadata = !allowUntrustedKeycloakCertificates;

        if (allowUntrustedKeycloakCertificates)
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = new[] { clientId, publicClientId, "account" }, // Accept configured audiences
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKeyResolver = (_, _, keyId, _) =>
            {
                var expectedIssuer = ResolveExpectedIssuer(httpContextAccessor.HttpContext, authorityBaseUrl, realm);
                return ResolveSigningKeys(expectedIssuer, keyId, allowUntrustedKeycloakCertificates);
            },
            IssuerValidator = (issuer, _, _) =>
            {
                var validIssuers = ResolveValidIssuers(httpContextAccessor.HttpContext, authorityBaseUrl, publicAuthorityBaseUrl, realm);

                if (!validIssuers.Contains(NormalizeIssuer(issuer), StringComparer.OrdinalIgnoreCase))
                {
                    throw new SecurityTokenInvalidIssuerException($"IDX10205: Issuer validation failed. Issuer: '{issuer}'. Expected one of: '{string.Join("', '", validIssuers)}'.");
                }

                return issuer;
            }
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

var app = builder.Build();

ApiSecurityConfigurationValidator.Validate(app.Configuration, app.Environment);

{
    var configuredOrigins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? Array.Empty<string>();
    var defaultOrigins = new[] { "http://localhost:3000", "http://localhost:3001" };
    var allowedOrigins = configuredOrigins.Length > 0 ? configuredOrigins : defaultOrigins;
    app.Logger.LogInformation("CORS allowed origins: {Origins}", string.Join(", ", allowedOrigins));
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

var runMigrations = app.Configuration.GetValue<bool?>("Database:RunMigrations") ?? app.Environment.IsDevelopment();
app.Logger.LogInformation("Database migrations enabled: {RunMigrations}. Environment: {EnvironmentName}",
    runMigrations, app.Environment.EnvironmentName);
if (runMigrations)
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<StudentRegistrarDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
        var historyRepository = dbContext.GetService<IHistoryRepository>();
        var migrationIds = migrationsAssembly.Migrations.Keys.ToList();
        logger.LogInformation("Available migrations ({Count}): {Migrations}",
            migrationIds.Count, string.Join(", ", migrationIds));
        var appliedMigrationIds = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();
        var pendingMigrationIds = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
        logger.LogInformation("Applied migrations ({Count}): {Migrations}",
            appliedMigrationIds.Count, appliedMigrationIds.Count == 0 ? "<none>" : string.Join(", ", appliedMigrationIds));
        logger.LogInformation("Pending migrations ({Count}): {Migrations}",
            pendingMigrationIds.Count, pendingMigrationIds.Count == 0 ? "<none>" : string.Join(", ", pendingMigrationIds));

        await TryBaselineDevelopmentDatabaseAsync(
            dbContext,
            historyRepository,
            migrationIds,
            appliedMigrationIds,
            pendingMigrationIds,
            logger,
            app.Environment);

        var connectionString = dbContext.Database.GetConnectionString();
        var redactedConnectionString = System.Text.RegularExpressions.Regex.Replace(
            connectionString ?? "", 
            @"(password|pwd)\s*=\s*[^;]*", 
            "$1=***", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        logger.LogInformation($"Using connection string: {redactedConnectionString}");

        // Local Aspire startup can take a while to make Postgres reachable.
        // Keep retrying long enough for containers and port forwarding to settle.
        var maxRetries = 24;
        var delay = TimeSpan.FromSeconds(5);

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

app.UseForwardedHeaders();
app.UseMiddleware<ApiSecurityHeadersMiddleware>();
app.UseRouting();
app.UseCors("AllowFrontend");

// Add tenant resolution middleware
// This must come before authentication to allow tenant-specific auth configuration
app.UseTenantResolution();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (!HttpMethods.IsGet(context.Request.Method)
        && !HttpMethods.IsHead(context.Request.Method)
        && !HttpMethods.IsOptions(context.Request.Method)
        && (context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.Equals("/auth/logout", StringComparison.OrdinalIgnoreCase)))
    {
        var sessionId = context.User.FindFirst(authSessionIdClaimType)?.Value;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var sessionStore = context.RequestServices.GetRequiredService<IAuthSessionStore>();
            var session = await sessionStore.GetAsync(sessionId, context.RequestAborted);
            var csrfHeader = context.Request.Headers[csrfHeaderName].FirstOrDefault();

            if (session == null || string.IsNullOrWhiteSpace(csrfHeader) || !string.Equals(session.CsrfToken, csrfHeader, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid CSRF token." });
                return;
            }
        }
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();

static string ResolveExpectedIssuer(HttpContext? httpContext, string authorityBaseUrl, string defaultRealm)
{
    var tenantContextAccessor = httpContext?.RequestServices.GetService<ITenantContextAccessor>();
    var tenantRealm = tenantContextAccessor?.TenantContext?.Tenant?.KeycloakRealm;
    var realm = string.IsNullOrWhiteSpace(tenantRealm) ? defaultRealm : tenantRealm;

    return BuildRealmAuthority(authorityBaseUrl, realm);
}

static IReadOnlyCollection<string> ResolveValidIssuers(
    HttpContext? httpContext,
    string authorityBaseUrl,
    string? publicAuthorityBaseUrl,
    string defaultRealm)
{
    var internalIssuer = ResolveExpectedIssuer(httpContext, authorityBaseUrl, defaultRealm);
    if (string.IsNullOrWhiteSpace(publicAuthorityBaseUrl))
    {
        return new[] { NormalizeIssuer(internalIssuer) };
    }

    var tenantContextAccessor = httpContext?.RequestServices.GetService<ITenantContextAccessor>();
    var tenantRealm = tenantContextAccessor?.TenantContext?.Tenant?.KeycloakRealm;
    var realm = string.IsNullOrWhiteSpace(tenantRealm) ? defaultRealm : tenantRealm;
    var publicIssuer = BuildRealmAuthority(publicAuthorityBaseUrl, realm);

    return new[] { internalIssuer, publicIssuer }
        .Select(NormalizeIssuer)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

static string BuildRealmAuthority(string authorityBaseUrl, string realm)
{
    var normalizedAuthority = authorityBaseUrl.TrimEnd('/');
    var realmsSegment = "/realms/";
    var realmIndex = normalizedAuthority.IndexOf(realmsSegment, StringComparison.OrdinalIgnoreCase);

    if (realmIndex >= 0)
    {
        normalizedAuthority = normalizedAuthority[..realmIndex];
    }

    return $"{normalizedAuthority}{realmsSegment}{realm}";
}

static string NormalizeIssuer(string issuer)
{
    return issuer.TrimEnd('/');
}

static bool IsAllowedCorsOrigin(string origin, IReadOnlyCollection<string> allowedOrigins, string baseDomain)
{
    if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        return true;
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var normalizedBaseDomain = baseDomain.Trim().ToLowerInvariant();
    var host = uri.Host.ToLowerInvariant();

    if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    if (string.IsNullOrWhiteSpace(normalizedBaseDomain))
    {
        return false;
    }

    if (string.Equals(host, normalizedBaseDomain, StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    return host.EndsWith($".{normalizedBaseDomain}", StringComparison.OrdinalIgnoreCase);
}

static IReadOnlyCollection<SecurityKey> ResolveSigningKeys(string issuer, string? keyId, bool allowUntrustedCertificates)
{
    var normalizedIssuer = NormalizeIssuer(issuer);
    var cachedKeys = SigningKeyCache.Keys.AddOrUpdate(
        normalizedIssuer,
        _ => FetchSigningKeys(normalizedIssuer, allowUntrustedCertificates),
        (_, existing) => existing.ExpiresAt > DateTimeOffset.UtcNow
            ? existing
            : FetchSigningKeys(normalizedIssuer, allowUntrustedCertificates));

    if (string.IsNullOrWhiteSpace(keyId))
    {
        return cachedKeys.Keys;
    }

    var matchingKeys = cachedKeys.Keys.Where(key => string.Equals(key.KeyId, keyId, StringComparison.Ordinal)).ToArray();
    return matchingKeys.Length > 0 ? matchingKeys : cachedKeys.Keys;
}

static CachedSigningKeys FetchSigningKeys(string issuer, bool allowUntrustedCertificates)
{
    using var handler = new HttpClientHandler();
    if (allowUntrustedCertificates)
    {
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }

    using var client = new HttpClient(handler);
    var jwksUrl = $"{NormalizeIssuer(issuer)}/protocol/openid-connect/certs";
    var response = client.GetAsync(jwksUrl).GetAwaiter().GetResult();
    response.EnsureSuccessStatusCode();

    var jwksJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    var jwks = new JsonWebKeySet(jwksJson);

    return new CachedSigningKeys(
        jwks.GetSigningKeys().ToArray(),
        DateTimeOffset.UtcNow.AddMinutes(10));
}

static async Task TryBaselineDevelopmentDatabaseAsync(
    StudentRegistrarDbContext dbContext,
    IHistoryRepository historyRepository,
    IReadOnlyCollection<string> availableMigrationIds,
    IReadOnlyCollection<string> appliedMigrationIds,
    IReadOnlyCollection<string> pendingMigrationIds,
    ILogger logger,
    IHostEnvironment environment)
{
    if (!environment.IsDevelopment() || pendingMigrationIds.Count == 0 || appliedMigrationIds.Count == 0)
    {
        return;
    }

    var knownMigrationIds = new HashSet<string>(availableMigrationIds, StringComparer.OrdinalIgnoreCase);
    if (appliedMigrationIds.Any(knownMigrationIds.Contains))
    {
        return;
    }

    var initialMigrationId = pendingMigrationIds.First();
    if (!initialMigrationId.EndsWith("_InitialCreate", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    if (!await TableExistsAsync(dbContext, "AcademicYears"))
    {
        return;
    }

    logger.LogWarning(
        "Detected an existing development database with a pre-squash EF migration history. Recording {MigrationId} as applied so newer migrations can run.",
        initialMigrationId);

    var productVersion = GetEntityFrameworkProductVersion();
    var insertScript = historyRepository.GetInsertScript(new HistoryRow(initialMigrationId, productVersion));
    await dbContext.Database.ExecuteSqlRawAsync(insertScript);
}

static async Task<bool> TableExistsAsync(StudentRegistrarDbContext dbContext, string tableName)
{
    var connection = dbContext.Database.GetDbConnection();
    var openedConnection = connection.State != System.Data.ConnectionState.Open;

    if (openedConnection)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select exists (
                select 1
                from information_schema.tables
                where table_schema = 'public' and table_name = @tableName
            )
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync();
        return result is bool exists && exists;
    }
    finally
    {
        if (openedConnection)
        {
            await connection.CloseAsync();
        }
    }
}

static string GetEntityFrameworkProductVersion()
{
    return typeof(Migration).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        .Split('+')[0]
        ?? typeof(Migration).Assembly.GetName().Version?.ToString()
        ?? "10.0.1";
}

file sealed record CachedSigningKeys(IReadOnlyCollection<SecurityKey> Keys, DateTimeOffset ExpiresAt);

file static class SigningKeyCache
{
    internal static ConcurrentDictionary<string, CachedSigningKeys> Keys { get; } = new(StringComparer.OrdinalIgnoreCase);
}

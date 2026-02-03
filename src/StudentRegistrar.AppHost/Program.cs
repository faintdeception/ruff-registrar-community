using Projects;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var deploymentPlatform = builder.Configuration["DEPLOYMENT_PLATFORM"] ?? "aks";
var deployToAca = string.Equals(deploymentPlatform, "aca", StringComparison.OrdinalIgnoreCase);
var isPublishOrDeploy = !builder.ExecutionContext.IsRunMode;

// Read Keycloak configuration from appsettings.json
var keycloakConfig = builder.Configuration.GetSection("Keycloak");
var keycloakRealm = keycloakConfig["Realm"] ?? "student-registrar";
var keycloakClientId = keycloakConfig["ClientId"] ?? "student-registrar";
var keycloakPublicClientId = keycloakConfig["PublicClientId"] ?? "student-registrar-spa";
var keycloakClientSecret = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("keycloak-client-secret", keycloakConfig["ClientSecret"]
        ?? throw new InvalidOperationException("Keycloak:ClientSecret is required for local development."), secret: true)
    : builder.AddParameter("keycloak-client-secret",
        builder.Configuration["Keycloak:ClientSecret"]
        ?? throw new InvalidOperationException("Keycloak:ClientSecret is required for publish/deploy."),
        secret: true);

var keycloakAdminPassword = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("keycloak-admin-password", "admin123", secret: true)
    : builder.AddParameter("keycloak-admin-password",
        builder.Configuration["Keycloak:AdminPassword"]
        ?? throw new InvalidOperationException("Keycloak:AdminPassword is required for publish/deploy."),
        secret: true);

var keycloakHostname = builder.ExecutionContext.IsRunMode
    ? null
    : builder.AddParameter("keycloak-hostname",
        builder.Configuration["Keycloak:Hostname"]
        ?? throw new InvalidOperationException("Keycloak:Hostname is required for publish/deploy."));

var postgresPassword = builder.ExecutionContext.IsRunMode
    ? builder.AddParameter("postgres-password", "postgres123", secret: true)
    : builder.AddParameter("postgres-password",
        builder.Configuration["Postgres:Password"]
        ?? throw new InvalidOperationException("Postgres:Password is required for publish/deploy."),
        secret: true);

var publicApiUrlValue = builder.ExecutionContext.IsRunMode
    ? string.Empty
    : builder.Configuration["PublicApiUrl"]
        ?? throw new InvalidOperationException("PublicApiUrl is required for publish/deploy.");

var publicApiUrl = builder.ExecutionContext.IsRunMode
    ? null
    : builder.AddParameter("public-api-url", publicApiUrlValue);

var publicKeycloakUrlValue = builder.ExecutionContext.IsRunMode
    ? string.Empty
    : builder.Configuration["PublicKeycloakUrl"]
        ?? throw new InvalidOperationException("PublicKeycloakUrl is required for publish/deploy.");

var publicKeycloakUrl = builder.ExecutionContext.IsRunMode
    ? null
    : builder.AddParameter("public-keycloak-url", publicKeycloakUrlValue);

var keycloakEndpointPort = (deployToAca && isPublishOrDeploy) ? 80 : 8080;

static string? DeriveFrontendOrigin(string? publicApiUrlValue)
{
    if (string.IsNullOrWhiteSpace(publicApiUrlValue))
    {
        return null;
    }

    if (Uri.TryCreate(publicApiUrlValue, UriKind.Absolute, out var apiUri))
    {
        var host = apiUri.Host;
        if (host.StartsWith("api.", StringComparison.OrdinalIgnoreCase))
        {
            host = $"frontend.{host.Substring("api.".Length)}";
        }

        var builder = new UriBuilder(apiUri.Scheme, host, apiUri.Port);
        return builder.Uri.GetLeftPart(UriPartial.Authority);
    }

    var trimmed = publicApiUrlValue;
    if (trimmed.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
    {
        trimmed = trimmed[..^4];
    }

    return trimmed;
}

// PostgreSQL database (persisted locally; ACA volume permissions can prevent initdb)
var postgres = builder.AddPostgres("postgres", password: postgresPassword);

if (builder.ExecutionContext.IsRunMode)
{
    postgres = postgres.WithDataVolume("postgres-data-v1");
}

var studentRegistrarDb = postgres.AddDatabase("studentregistrar");
var keycloakDb = postgres.AddDatabase("keycloakdb", "keycloak");

// Keycloak for authentication - use explicit password for consistency with persistent data
// Add persistent data volume so realm/roles configuration persists
var keycloak = builder.AddKeycloak("keycloak", keycloakEndpointPort)
    // Use a named data volume so upgrades can intentionally start from a clean Keycloak state
    // (e.g., when older persisted admin credentials prevent bootstrap/login).
    .WithDataVolume("keycloak-data-v13")
    // Keycloak 26.x (Quarkus) bootstraps admin credentials via KC_BOOTSTRAP_ADMIN_*.
    // Keep legacy KEYCLOAK_ADMIN_* too for compatibility with older scripts/images.
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment(context =>
    {
        // Persist Keycloak data in Postgres (survives restarts/pod reschedules).
        context.EnvironmentVariables["KC_DB"] = "postgres";
        context.EnvironmentVariables["KC_DB_URL"] = keycloakDb.Resource.JdbcConnectionString;
        context.EnvironmentVariables["KC_DB_USERNAME"] = postgres.Resource.UserNameReference;
        context.EnvironmentVariables["KC_DB_PASSWORD"] = postgres.Resource.PasswordParameter;

        if (isPublishOrDeploy)
        {
            context.EnvironmentVariables["KC_HOSTNAME"] = keycloakHostname!.Resource;
            context.EnvironmentVariables["KC_PROXY"] = "edge";
            context.EnvironmentVariables["KC_PROXY_HEADERS"] = "xforwarded";
            context.EnvironmentVariables["KC_HOSTNAME_STRICT"] = "true";
            context.EnvironmentVariables["KC_HOSTNAME_STRICT_HTTPS"] = "true";
        }
    })
    ;

// API service
var apiService = builder.AddProject<StudentRegistrar_Api>("api")
    .WithReference(studentRegistrarDb)
    .WithReference(keycloak)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Keycloak__Realm", keycloakRealm)
    .WithEnvironment("Keycloak__ClientId", keycloakClientId)
    .WithEnvironment("Database__RunMigrations", "true")
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["Keycloak__ClientSecret"] = keycloakClientSecret.Resource;
        context.EnvironmentVariables["Keycloak__Authority"] = builder.ExecutionContext.IsRunMode
            ? keycloak.GetEndpoint("http")
            : publicKeycloakUrl!.Resource;
        context.EnvironmentVariables["Keycloak__BaseUrl"] = builder.ExecutionContext.IsRunMode
            ? keycloak.GetEndpoint("http")
            : publicKeycloakUrl!.Resource;
        context.EnvironmentVariables["Keycloak__AdminUsername"] = "admin";
        context.EnvironmentVariables["Keycloak__AdminPassword"] = keycloakAdminPassword.Resource;
        if (builder.ExecutionContext.IsRunMode)
        {
            context.EnvironmentVariables["Cors__AllowedOrigins__0"] = "http://localhost:3001";
        }
        else
        {
            var frontendOrigin = DeriveFrontendOrigin(publicApiUrlValue);
            if (string.IsNullOrWhiteSpace(frontendOrigin))
            {
                throw new InvalidOperationException("Unable to derive frontend origin from PublicApiUrl for publish/deploy.");
            }

            context.EnvironmentVariables["Cors__AllowedOrigins__0"] = frontendOrigin;
        }
    });

IResourceBuilder<ContainerResource>? frontendContainer = null;

if (builder.ExecutionContext.IsRunMode)
{
    // Next.js frontend (dev)
    var frontendDev = builder.AddNpmApp("frontend", "../../frontend", "dev")
        .WithReference(apiService)
        .WithReference(keycloak)
        .WithHttpEndpoint(port: 3001, env: "PORT")
        .WithEnvironment("NODE_ENV", "development")
        .WithEnvironment("NEXT_TELEMETRY_DISABLED", "1")
        .WithExternalHttpEndpoints();

    // Configure the frontend to use the API
    frontendDev.WithEnvironment("NEXT_PUBLIC_API_URL", apiService.GetEndpoint("http"));
    frontendDev.WithEnvironment("API_BASE_URL", apiService.GetEndpoint("http"));

    // Configure the frontend to use Keycloak directly (matches frontend auth implementation)
    frontendDev
        .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_URL", "http://localhost:8080")
        .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_REALM", keycloakRealm)
        .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_CLIENT_ID", keycloakPublicClientId);
}
else
{
    // Publish/deploy frontend as a container
    frontendContainer = builder.AddContainer("frontend", "studentregistrar-frontend")
        .WithDockerfile("../../frontend")
        .WithReference(apiService)
        .WithReference(keycloak)
        .WithHttpEndpoint(targetPort: 3000, port: deployToAca ? 80 : 3001, env: "PORT")
        .WithEnvironment("NODE_ENV", "production")
        .WithEnvironment("NEXT_TELEMETRY_DISABLED", "1")
        .WithEnvironment("NEXT_PUBLIC_API_URL", publicApiUrl!.Resource)
        .WithEnvironment("API_BASE_URL", publicApiUrl!.Resource)
        .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_URL", publicKeycloakUrl!.Resource)
        .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_REALM", keycloakRealm)
        .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_CLIENT_ID", keycloakPublicClientId);
}

// Azure Container Apps deployment wiring (opt-in)
if (isPublishOrDeploy && deployToAca)
{
    // These names match common Aspire deploy parameters (and provide defaults for CI).
    builder.AddParameter("location", "eastus");
    builder.AddParameter("environment", "aca");
    builder.AddParameter("resourceGroupName", "studentregistrar-aca");

    // Define the Container Apps environment.
    builder.AddAzureContainerAppEnvironment("aca");

    postgres.PublishAsAzureContainerApp((_, _) => { });

    keycloak.PublishAsAzureContainerApp((_, app) =>
    {
        app.Configuration.Ingress.External = true;
        app.Configuration.Ingress.TargetPort = 8080;
    });

    // Frontend runs in the browser, so API must be externally reachable.
    apiService.PublishAsAzureContainerApp((_, app) =>
    {
        app.Configuration.Ingress.External = true;
        app.Configuration.Ingress.TargetPort = 8080;
    });

    frontendContainer!.PublishAsAzureContainerApp((_, app) =>
    {
        app.Configuration.Ingress.External = true;
        app.Configuration.Ingress.TargetPort = 3000;
    });
}

await builder.Build().RunAsync();

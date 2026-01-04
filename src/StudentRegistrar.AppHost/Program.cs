using Projects;
using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

// Read Keycloak configuration from appsettings.json
var keycloakConfig = builder.Configuration.GetSection("Keycloak");
var keycloakRealm = keycloakConfig["Realm"] ?? "student-registrar";
var keycloakClientId = keycloakConfig["ClientId"] ?? "student-registrar";
var keycloakClientSecret = keycloakConfig["ClientSecret"] ?? throw new InvalidOperationException("Keycloak ClientSecret is required in appsettings.json");

// PostgreSQL database - let Aspire generate password automatically
var postgres = builder.AddPostgres("postgres");

var studentRegistrarDb = postgres.AddDatabase("studentregistrar");

// Keycloak for authentication - use explicit password for consistency with persistent data
// Add persistent data volume so realm/roles configuration persists
var keycloak = builder.AddKeycloak("keycloak", 8080)
    // Use a named data volume so upgrades can intentionally start from a clean Keycloak state
    // (e.g., when older persisted admin credentials prevent bootstrap/login).
    .WithDataVolume("keycloak-data-v13")
    // Keycloak 26.x (Quarkus) bootstraps admin credentials via KC_BOOTSTRAP_ADMIN_*.
    // Keep legacy KEYCLOAK_ADMIN_* too for compatibility with older scripts/images.
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin123")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin123")
    .WithEnvironment("KC_HEALTH_ENABLED", "true")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    ;

// API service
var apiService = builder.AddProject<StudentRegistrar_Api>("api")
    .WithReference(studentRegistrarDb)
    .WithReference(keycloak)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("Keycloak__Realm", keycloakRealm)
    .WithEnvironment("Keycloak__ClientId", keycloakClientId)
    .WithEnvironment("Keycloak__ClientSecret", keycloakClientSecret);

// Next.js frontend
var frontend = builder.AddNpmApp("frontend", "../../frontend", "dev")
    .WithReference(apiService)
    .WithReference(keycloak)
    .WithHttpEndpoint(port: 3001, env: "PORT")
    .WithEnvironment("NODE_ENV", "development")
    .WithEnvironment("NEXT_TELEMETRY_DISABLED", "1")
    .WithExternalHttpEndpoints();

// Configure the frontend to use the API
frontend.WithEnvironment("NEXT_PUBLIC_API_URL", apiService.GetEndpoint("http"));

// Configure the frontend to use Keycloak directly (matches frontend auth implementation)
frontend
    .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_URL", keycloak.GetEndpoint("http"))
    .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_REALM", keycloakRealm)
    .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_CLIENT_ID", keycloakClientId)
    .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_CLIENT_SECRET", keycloakClientSecret);

await builder.Build().RunAsync();

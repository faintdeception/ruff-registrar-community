using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace StudentRegistrar.Api.Services;

public class KeycloakService : IKeycloakService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakService> _logger;
    private readonly IPasswordService _passwordService;
    private readonly string _keycloakBaseUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string? _clientSecret;
    private readonly string? _adminUsername;
    private readonly string? _adminPassword;
    private readonly string _adminRealm;

    public KeycloakService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<KeycloakService> logger,
        IPasswordService passwordService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _passwordService = passwordService;
        
        // Load Keycloak configuration
        _keycloakBaseUrl = _configuration["Keycloak:BaseUrl"] ?? "http://localhost:8080";
        _realm = _configuration["Keycloak:Realm"] ?? "student-registrar";
        _clientId = _configuration["Keycloak:ClientId"] ?? "student-registrar";
        _clientSecret = _configuration["Keycloak:ClientSecret"];
        _adminUsername = _configuration["Keycloak:AdminUsername"];
        _adminPassword = _configuration["Keycloak:AdminPassword"];
        _adminRealm = _configuration["Keycloak:AdminRealm"] ?? "master";
        
        _logger.LogInformation("Keycloak service initialized with base URL: {BaseUrl}, realm: {Realm}", _keycloakBaseUrl, _realm);
    }

    public async Task<CreateUserResponse> CreateUserAsync(CreateUserRequest request)
    {
        try
        {
            _logger.LogInformation("Creating user in Keycloak with email: {Email}", request.Email);
            
            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync();
            
            // Generate a secure temporary password
            var temporaryPassword = _passwordService.GenerateSecurePassword(14);
            var passwordStrength = _passwordService.AssessPasswordStrength(temporaryPassword);
            
            _logger.LogDebug("Generated temporary password with strength: {Strength}", passwordStrength);
            
            // Create user representation for Keycloak
            var keycloakUser = new
            {
                username = request.Email,
                email = request.Email,
                firstName = request.FirstName,
                lastName = request.LastName,
                enabled = true,
                emailVerified = false,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = temporaryPassword,
                        temporary = true
                    }
                },
                requiredActions = new[] { "UPDATE_PASSWORD", "VERIFY_EMAIL" }
            };
            
            // Make the API call to create user
            using var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{_realm}/users");
            createRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            createRequest.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(keycloakUser),
                System.Text.Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.SendAsync(createRequest);
            
            if (response.StatusCode == System.Net.HttpStatusCode.Created)
            {
                // Extract user ID from Location header
                var locationHeader = response.Headers.Location?.ToString();
                var userId = locationHeader?.Split('/').LastOrDefault();
                
                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("Failed to extract user ID from Keycloak response");
                }
                
                _logger.LogInformation("Successfully created user in Keycloak with ID: {UserId}", userId);
                
                return new CreateUserResponse
                {
                    UserId = userId,
                    Username = request.Email,
                    TemporaryPassword = temporaryPassword,
                    IsTemporary = true
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException($"User with email {request.Email} already exists in Keycloak");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to create user in Keycloak. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user in Keycloak for email: {Email}", request.Email);
            throw;
        }
    }

    public async Task UpdateUserRoleAsync(string keycloakId, UserRole role)
    {
        try
        {
            _logger.LogInformation("Updating user role for Keycloak ID: {KeycloakId} to role: {Role}", keycloakId, role);
            
            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync();
            
            // Map UserRole to Keycloak role name
            var keycloakRoleName = role switch
            {
                UserRole.Administrator => "admin",
                UserRole.Educator => "educator",
                UserRole.Member => "student",
                _ => throw new ArgumentException($"Unsupported role: {role}")
            };
            
            // Get the role representation
            using var getRoleRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{_realm}/roles/{keycloakRoleName}");
            getRoleRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var getRoleResponse = await _httpClient.SendAsync(getRoleRequest);
            if (!getRoleResponse.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Role '{keycloakRoleName}' not found in Keycloak realm");
            }
            
            var roleJson = await getRoleResponse.Content.ReadAsStringAsync();
            var roleRepresentation = System.Text.Json.JsonSerializer.Deserialize<object>(roleJson);
            
            // Assign role to user
            using var assignRoleRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/admin/realms/{_realm}/users/{keycloakId}/role-mappings/realm");
            assignRoleRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            assignRoleRequest.Content = new StringContent(
                $"[{roleJson}]",
                System.Text.Encoding.UTF8,
                "application/json");
            
            var assignResponse = await _httpClient.SendAsync(assignRoleRequest);
            if (assignResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully updated user role for Keycloak ID: {KeycloakId} to {Role}", keycloakId, role);
            }
            else
            {
                var errorContent = await assignResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to assign role to user. Status: {assignResponse.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user role for Keycloak ID: {KeycloakId}", keycloakId);
            throw;
        }
    }

    public async Task DeactivateUserAsync(string keycloakId)
    {
        try
        {
            _logger.LogInformation("Deactivating user for Keycloak ID: {KeycloakId}", keycloakId);
            
            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync();
            
            // Update user to set enabled = false
            var userUpdate = new { enabled = false };
            
            using var updateRequest = new HttpRequestMessage(HttpMethod.Put, $"{_keycloakBaseUrl}/admin/realms/{_realm}/users/{keycloakId}");
            updateRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            updateRequest.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(userUpdate),
                System.Text.Encoding.UTF8,
                "application/json");
            
            var response = await _httpClient.SendAsync(updateRequest);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deactivated user for Keycloak ID: {KeycloakId}", keycloakId);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to deactivate user. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate user for Keycloak ID: {KeycloakId}", keycloakId);
            throw;
        }
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        try
        {
            _logger.LogInformation("Checking if user exists with email: {Email}", email);
            
            // Get admin access token
            var adminToken = await GetAdminAccessTokenAsync();
            
            // Search for user by email
            using var searchRequest = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{_realm}/users?email={Uri.EscapeDataString(email)}&exact=true");
            searchRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
            
            var response = await _httpClient.SendAsync(searchRequest);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                var users = jsonDoc.RootElement;
                
                bool userExists = users.GetArrayLength() > 0;
                _logger.LogInformation("User exists check for {Email}: {Exists}", email, userExists);
                return userExists;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to search for user. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if user exists for email: {Email}", email);
            throw;
        }
    }

    private async Task<string> GetAdminAccessTokenAsync()
    {
        try
        {
            _logger.LogDebug("Obtaining admin access token from Keycloak");

            if (!string.IsNullOrWhiteSpace(_adminUsername) && !string.IsNullOrWhiteSpace(_adminPassword))
            {
                var adminTokenRequest = new Dictionary<string, string>
                {
                    { "grant_type", "password" },
                    { "client_id", "admin-cli" },
                    { "username", _adminUsername },
                    { "password", _adminPassword }
                };

                using var adminRequest = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/realms/{_adminRealm}/protocol/openid-connect/token");
                adminRequest.Content = new FormUrlEncodedContent(adminTokenRequest);

                var adminResponse = await _httpClient.SendAsync(adminRequest);
                if (adminResponse.IsSuccessStatusCode)
                {
                    var responseContent = await adminResponse.Content.ReadAsStringAsync();
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                    var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();

                    if (string.IsNullOrEmpty(accessToken))
                    {
                        throw new InvalidOperationException("Access token is null or empty in Keycloak admin response");
                    }

                    _logger.LogDebug("Successfully obtained admin access token via admin credentials");
                    return accessToken;
                }
                else
                {
                    var adminError = await adminResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to obtain admin token via admin credentials. Status: {Status}, Error: {Error}",
                        adminResponse.StatusCode, adminError);
                }
            }
            
            if (string.IsNullOrWhiteSpace(_clientSecret))
            {
                throw new InvalidOperationException("Keycloak ClientSecret is required when admin credentials are not configured");
            }

            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _clientId },
                { "client_secret", _clientSecret }
            };
            
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_keycloakBaseUrl}/realms/{_realm}/protocol/openid-connect/token");
            request.Content = new FormUrlEncodedContent(tokenRequest);
            
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                var accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    throw new InvalidOperationException("Access token is null or empty in Keycloak response");
                }
                
                _logger.LogDebug("Successfully obtained admin access token");
                return accessToken;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to obtain access token. Status: {response.StatusCode}, Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to obtain admin access token from Keycloak");
            throw;
        }
    }
}

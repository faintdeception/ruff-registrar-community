using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;
namespace StudentRegistrar.Smoke.Tests;

public class SmokeTests
{
    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    [Fact]
    public async Task Frontend_Login_Page_Should_Load()
    {
        var webUrl = SmokeTestSettings.RequireEnv("SMOKE_WEB_URL");

        var response = await Client.GetAsync($"{webUrl}/login");

        Assert.True(response.IsSuccessStatusCode, $"Expected 2xx from /login, got {(int)response.StatusCode}");

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("login", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Api_Public_Endpoints_Should_Not_Return_Server_Error()
    {
        var apiUrl = SmokeTestSettings.RequireEnv("SMOKE_API_URL");

        await AssertNotServerError(apiUrl, "/api/semesters/active");
        await AssertNotServerError(apiUrl, "/api/rooms");
        await AssertNotServerError(apiUrl, "/api/courses");
    }

    [Fact]
    public async Task Api_Admin_Endpoint_Should_Require_Auth()
    {
        var apiUrl = SmokeTestSettings.RequireEnv("SMOKE_API_URL");

        var response = await Client.GetAsync($"{apiUrl}/api/accountholders");
        var statusCode = (int)response.StatusCode;

        Assert.True(statusCode == 401 || statusCode == 403,
            $"Expected 401 or 403 for protected endpoint, got {statusCode}");
    }

    [Fact]
    public async Task Authenticated_Profile_Request_Should_Succeed_When_Configured()
    {
        var apiUrl = SmokeTestSettings.RequireEnv("SMOKE_API_URL");
        var username = SmokeTestSettings.OptionalEnv("SMOKE_USERNAME");
        var password = SmokeTestSettings.OptionalEnv("SMOKE_PASSWORD");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("SMOKE_USERNAME/SMOKE_PASSWORD not set. Smoke tests require these values.");
        }

        var keycloakUrl = SmokeTestSettings.RequireEnv("SMOKE_KEYCLOAK_URL");
        var realm = SmokeTestSettings.OptionalEnv("SMOKE_REALM") ?? "student-registrar";
        var clientId = SmokeTestSettings.OptionalEnv("SMOKE_CLIENT_ID") ?? "student-registrar-spa";
        var clientSecret = SmokeTestSettings.OptionalEnv("SMOKE_CLIENT_SECRET");

        var accessToken = await GetAccessTokenAsync(keycloakUrl, realm, clientId, clientSecret, username!, password!);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}/api/accountholders/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await Client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, $"Expected 2xx from /api/accountholders/me, got {(int)response.StatusCode}");
    }

    private static async Task AssertNotServerError(string apiUrl, string path)
    {
        var response = await Client.GetAsync($"{apiUrl}{path}");
        var statusCode = (int)response.StatusCode;

        Assert.True(statusCode < 500, $"Expected non-5xx for {path}, got {statusCode}");
    }

    private static async Task<string> GetAccessTokenAsync(
        string keycloakUrl,
        string realm,
        string clientId,
        string? clientSecret,
        string username,
        string password)
    {
        var tokenEndpoint = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token";

        var payload = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = clientId,
            ["username"] = username,
            ["password"] = password
        };

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            payload["client_secret"] = clientSecret;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(payload)
        };

        var response = await Client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode,
            $"Token request failed with {(int)response.StatusCode}: {responseBody}");

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var token = jsonDoc.RootElement.GetProperty("access_token").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token), "Access token was empty");

        return token!;
    }
}

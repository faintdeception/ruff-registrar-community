using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Models;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class KeycloakServiceTests
{
    private readonly Mock<ILogger<KeycloakService>> _loggerMock;
    private readonly Mock<IPasswordService> _passwordServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly KeycloakService _keycloakService;

    public KeycloakServiceTests()
    {
        _loggerMock = new Mock<ILogger<KeycloakService>>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _configurationMock = new Mock<IConfiguration>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        
        // Setup configuration mock
        _configurationMock.Setup(c => c["Keycloak:BaseUrl"]).Returns("http://localhost:8080");
        _configurationMock.Setup(c => c["Keycloak:Realm"]).Returns("test-realm");
        _configurationMock.Setup(c => c["Keycloak:ClientId"]).Returns("test-client");
        _configurationMock.Setup(c => c["Keycloak:ClientSecret"]).Returns("test-secret");
        
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _keycloakService = new KeycloakService(_httpClient, _configurationMock.Object, _loggerMock.Object, _passwordServiceMock.Object);
    }

    [Fact]
    public void Constructor_MissingClientSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Keycloak:BaseUrl"]).Returns("http://localhost:8080");
        configMock.Setup(c => c["Keycloak:Realm"]).Returns("test-realm");
        configMock.Setup(c => c["Keycloak:ClientId"]).Returns("test-client");
        configMock.Setup(c => c["Keycloak:ClientSecret"]).Returns((string?)null);

        var httpClient = new HttpClient();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new KeycloakService(httpClient, configMock.Object, _loggerMock.Object, _passwordServiceMock.Object));
        
        Assert.Equal("Keycloak ClientSecret is required", exception.Message);
    }

    [Fact]
    public async Task CreateUserAsync_SuccessfulCreation_ReturnsCreateUserResponse()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var temporaryPassword = "TempPass123!";
        _passwordServiceMock.Setup(p => p.GenerateSecurePassword(14)).Returns(temporaryPassword);
        _passwordServiceMock.Setup(p => p.AssessPasswordStrength(temporaryPassword)).Returns(PasswordStrength.Strong);

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock user creation request
        var userCreationHttpResponse = new HttpResponseMessage(HttpStatusCode.Created);
        userCreationHttpResponse.Headers.Location = new Uri("http://localhost:8080/admin/realms/test-realm/users/12345");

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(userCreationHttpResponse);

        // Act
        var result = await _keycloakService.CreateUserAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("12345", result.UserId);
        Assert.Equal(request.Email, result.Username);
        Assert.Equal(temporaryPassword, result.TemporaryPassword);
        Assert.True(result.IsTemporary);

        _passwordServiceMock.Verify(p => p.GenerateSecurePassword(14), Times.Once);
        _passwordServiceMock.Verify(p => p.AssessPasswordStrength(temporaryPassword), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_UserAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "existing@example.com",
            FirstName = "Jane",
            LastName = "Doe"
        };

        _passwordServiceMock.Setup(p => p.GenerateSecurePassword(14)).Returns("TempPass123!");

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock user creation request - conflict
        var userCreationHttpResponse = new HttpResponseMessage(HttpStatusCode.Conflict);

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(userCreationHttpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _keycloakService.CreateUserAsync(request));
        
        Assert.Contains("already exists in Keycloak", exception.Message);
    }

    [Fact]
    public async Task CreateUserAsync_TokenRequestFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _passwordServiceMock.Setup(p => p.GenerateSecurePassword(14)).Returns("TempPass123!");

        // Mock failed token request
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Unauthorized")
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _keycloakService.CreateUserAsync(request));
        
        Assert.Contains("Failed to obtain access token", exception.Message);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_SuccessfulRoleUpdate_CompletesSuccessfully()
    {
        // Arrange
        var keycloakId = "user-123";
        var role = UserRole.Educator;

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock get role request
        var roleResponse = JsonSerializer.Serialize(new { id = "role-123", name = "educator" });
        var getRoleHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(roleResponse)
        };

        // Mock assign role request
        var assignRoleHttpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(getRoleHttpResponse)
            .ReturnsAsync(assignRoleHttpResponse);

        // Act
        await _keycloakService.UpdateUserRoleAsync(keycloakId, role);

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(3),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Theory]
    [InlineData(UserRole.Administrator, "admin")]
    [InlineData(UserRole.Educator, "educator")]
    [InlineData(UserRole.Member, "student")]
    public async Task UpdateUserRoleAsync_DifferentRoles_MapsCorrectly(UserRole role, string expectedKeycloakRole)
    {
        // Arrange
        var keycloakId = "user-123";

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock get role request
        var roleResponse = JsonSerializer.Serialize(new { id = "role-123", name = expectedKeycloakRole });
        var getRoleHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(roleResponse)
        };

        // Mock assign role request
        var assignRoleHttpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(getRoleHttpResponse)
            .ReturnsAsync(assignRoleHttpResponse);

        // Act
        await _keycloakService.UpdateUserRoleAsync(keycloakId, role);

        // Assert
        // Verify total of 3 HTTP calls were made
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(3),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
            
        // Verify the specific get role call was made with correct role name
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.RequestUri!.ToString().Contains($"/roles/{expectedKeycloakRole}") && 
                req.Method == HttpMethod.Get),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UpdateUserRoleAsync_RoleNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var keycloakId = "user-123";
        var role = UserRole.Educator;

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock get role request - not found
        var getRoleHttpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(getRoleHttpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _keycloakService.UpdateUserRoleAsync(keycloakId, role));
        
        Assert.Contains("Role 'educator' not found in Keycloak realm", exception.Message);
    }

    [Fact]
    public async Task DeactivateUserAsync_SuccessfulDeactivation_CompletesSuccessfully()
    {
        // Arrange
        var keycloakId = "user-123";

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock user update request
        var updateUserHttpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(updateUserHttpResponse);

        // Act
        await _keycloakService.DeactivateUserAsync(keycloakId);

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UserExistsAsync_UserExists_ReturnsTrue()
    {
        // Arrange
        var email = "existing@example.com";

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock user search request - user found
        var searchResponse = JsonSerializer.Serialize(new[] { new { id = "user-123", email = email } });
        var searchHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(searchResponse)
        };

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(searchHttpResponse);

        // Act
        var result = await _keycloakService.UserExistsAsync(email);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserExistsAsync_UserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock user search request - no users found
        var searchResponse = JsonSerializer.Serialize(Array.Empty<object>());
        var searchHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(searchResponse)
        };

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(searchHttpResponse);

        // Act
        var result = await _keycloakService.UserExistsAsync(email);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UserExistsAsync_SearchFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "test@example.com";

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock user search request - server error
        var searchHttpResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal Server Error")
        };

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(searchHttpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _keycloakService.UserExistsAsync(email));
        
        Assert.Contains("Failed to search for user", exception.Message);
    }

    [Fact]
    public async Task CreateUserAsync_InvalidLocationHeader_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe"
        };

        _passwordServiceMock.Setup(p => p.GenerateSecurePassword(14)).Returns("TempPass123!");

        // Mock token request
        var tokenResponse = JsonSerializer.Serialize(new { access_token = "mock-token" });
        var tokenHttpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(tokenResponse)
        };

        // Mock user creation request with invalid location header
        var userCreationHttpResponse = new HttpResponseMessage(HttpStatusCode.Created);
        // No location header set

        _httpMessageHandlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(tokenHttpResponse)
            .ReturnsAsync(userCreationHttpResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _keycloakService.CreateUserAsync(request));
        
        Assert.Contains("Failed to extract user ID from Keycloak response", exception.Message);
    }
}

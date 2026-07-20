using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class UsersControllerTests
{
    [Fact]
    public async Task RequestEmailChange_WhenOwnerRequestsNewEmail_PersistsPendingStateAndSendsConfirmation()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = "old@example.com",
            FirstName = "Ava",
            LastName = "Admin",
            KeycloakId = "kc-user-1",
            Role = UserRole.Member,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var keycloakService = new Mock<IKeycloakService>();
        var emailSender = new Mock<IUserIdentityEmailSender>();
        emailSender
            .Setup(sender => sender.SendEmailChangeConfirmationAsync(It.IsAny<PendingEmailChangeEmail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDispatchResult("https://app.example.test/org/sunrise/confirm-email-change?token=debug"));

        var controller = CreateController(
            dbContext,
            keycloakService.Object,
            emailSender.Object,
            keycloakId: "kc-user-1",
            role: "Member",
            tenant: new Tenant { Id = tenantId, Name = "Sunrise", Subdomain = "sunrise" },
            environmentName: "Development",
            origin: "https://app.example.test");

        var result = await controller.RequestEmailChange(userId, new RequestEmailChangeRequest
        {
            NewEmail = "new@example.com"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<RequestEmailChangeResponse>(ok.Value);
        Assert.Equal("old@example.com", payload.CurrentEmail);
        Assert.Equal("new@example.com", payload.PendingEmail);
        Assert.NotNull(payload.PendingEmailExpiresAtUtc);
        Assert.Equal("https://app.example.test/org/sunrise/confirm-email-change?token=debug", payload.DebugConfirmationUrl);

        var user = await dbContext.Users.SingleAsync();
        Assert.Equal("old@example.com", user.Email);
        Assert.Equal("new@example.com", user.PendingEmail);
        Assert.NotNull(user.PendingEmailTokenHash);

        emailSender.Verify(sender => sender.SendEmailChangeConfirmationAsync(
            It.Is<PendingEmailChangeEmail>(email =>
                email.CurrentEmail == "old@example.com" &&
                email.PendingEmail == "new@example.com" &&
                email.ConfirmationUrl.StartsWith("https://app.example.test/org/sunrise/confirm-email-change?token=")),
            It.IsAny<CancellationToken>()), Times.Once);
        keycloakService.Verify(service => service.UpdateUserEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmEmailChange_WhenTokenValid_UpdatesEmailAndLinkedRecords()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = "old@example.com",
            PendingEmail = "new@example.com",
            PendingEmailTokenHash = HashToken("confirm-token"),
            PendingEmailRequestedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            PendingEmailExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            FirstName = "Ava",
            LastName = "Admin",
            KeycloakId = "kc-user-1",
            Role = UserRole.Member,
            IsActive = true
        });
        dbContext.AccountHolders.Add(new AccountHolder
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Ava",
            LastName = "Admin",
            EmailAddress = "old@example.com",
            KeycloakUserId = "kc-user-1"
        });
        dbContext.Educators.Add(new Educator
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FirstName = "Ava",
            LastName = "Admin",
            Email = "old@example.com",
            KeycloakUserId = "kc-user-1"
        });
        await dbContext.SaveChangesAsync();

        var keycloakService = new Mock<IKeycloakService>();
        var emailSender = new Mock<IUserIdentityEmailSender>();
        var controller = CreateController(dbContext, keycloakService.Object, emailSender.Object, keycloakId: "kc-user-1", role: "Member");

        var result = await controller.ConfirmEmailChange(new ConfirmEmailChangeRequest
        {
            Token = "confirm-token"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ConfirmEmailChangeResponse>(ok.Value);
        Assert.Equal("new@example.com", payload.Email);

        var user = await dbContext.Users.SingleAsync();
        var accountHolder = await dbContext.AccountHolders.SingleAsync();
        var educator = await dbContext.Educators.SingleAsync();

        Assert.Equal("new@example.com", user.Email);
        Assert.Null(user.PendingEmail);
        Assert.Null(user.PendingEmailTokenHash);
        Assert.Equal("new@example.com", accountHolder.EmailAddress);
        Assert.Equal("new@example.com", educator.Email);
        keycloakService.Verify(service => service.UpdateUserEmailAsync("kc-user-1", "new@example.com"), Times.Once);
    }

    [Fact]
    public async Task RequestEmailChange_WhenNewEmailMatchesCurrent_CancelsPendingRequest()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = "old@example.com",
            PendingEmail = "new@example.com",
            PendingEmailTokenHash = "token-hash",
            PendingEmailRequestedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            PendingEmailExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            FirstName = "Ava",
            LastName = "Admin",
            KeycloakId = "kc-user-1",
            Role = UserRole.Member,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var keycloakService = new Mock<IKeycloakService>();
        var emailSender = new Mock<IUserIdentityEmailSender>();
        var controller = CreateController(dbContext, keycloakService.Object, emailSender.Object, keycloakId: "kc-user-1", role: "Member");

        var result = await controller.RequestEmailChange(userId, new RequestEmailChangeRequest
        {
            NewEmail = "old@example.com"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<RequestEmailChangeResponse>(ok.Value);
        Assert.Null(payload.PendingEmail);

        var user = await dbContext.Users.SingleAsync();
        Assert.Null(user.PendingEmail);
        Assert.Null(user.PendingEmailTokenHash);
        emailSender.Verify(sender => sender.SendEmailChangeConfirmationAsync(It.IsAny<PendingEmailChangeEmail>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WhenRequestIncludesDifferentEmail_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        dbContext.Users.Add(new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = "old@example.com",
            FirstName = "Ava",
            LastName = "Admin",
            KeycloakId = "kc-user-1",
            Role = UserRole.Member,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var keycloakService = new Mock<IKeycloakService>();
        var emailSender = new Mock<IUserIdentityEmailSender>();
        var controller = CreateController(dbContext, keycloakService.Object, emailSender.Object, keycloakId: "kc-user-1", role: "Member");

        var result = await controller.UpdateUser(userId, new UpdateUserRequest
        {
            Email = "new@example.com"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email changes must be confirmed through the email change request flow.", badRequest.Value);
    }

    private static UsersController CreateController(
        StudentRegistrarDbContext dbContext,
        IKeycloakService keycloakService,
        IUserIdentityEmailSender emailSender,
        string keycloakId,
        string role,
        Tenant? tenant = null,
        string environmentName = "Production",
        string? origin = null)
    {
        var mapper = new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>())
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

        var tenantContextAccessor = new Mock<ITenantContextAccessor>();
        if (tenant != null)
        {
            tenantContextAccessor.SetupProperty(accessor => accessor.TenantContext, TenantContext.ForSaaS(tenant));
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(value => value.EnvironmentName).Returns(environmentName);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim("sub", keycloakId),
            new Claim(ClaimTypes.NameIdentifier, keycloakId),
            new Claim(ClaimTypes.Role, role),
            new Claim("email", "owner@example.com")
        }, "TestAuth");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        if (!string.IsNullOrWhiteSpace(origin))
        {
            httpContext.Request.Headers.Origin = origin;
        }

        return new UsersController(
            dbContext,
            mapper,
            keycloakService,
            emailSender,
            tenantContextAccessor.Object,
            configuration,
            environment.Object,
            NullLogger<UsersController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"UsersControllerTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options);
    }

    private static string HashToken(string token)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token.Trim());
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(tokenBytes)).ToLowerInvariant();
    }
}
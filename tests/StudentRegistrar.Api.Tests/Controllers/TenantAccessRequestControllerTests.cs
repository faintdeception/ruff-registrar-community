using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class TenantAccessRequestControllerTests
{
    [Fact]
    public async Task Get_ReturnsOkWithAdminEmail_WhenTenantContextContainsTenant()
    {
        await using var dbContext = CreateDbContext();
        var tenant = new Tenant
        {
            AdminEmail = "admin@example.org"
        };

        var tenantContext = new TenantContext
        {
            TenantId = Guid.NewGuid(),
            Tenant = tenant,
            DeploymentMode = DeploymentMode.SaaS,
            SubscriptionTier = SubscriptionTier.Free
        };

        var tenantContextAccessor = new Mock<ITenantContextAccessor>();
        tenantContextAccessor.SetupGet(accessor => accessor.TenantContext).Returns(tenantContext);

        var controller = new TenantAccessRequestController(tenantContextAccessor.Object, dbContext);

        var result = await controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<Dictionary<string, string>>(ok.Value);

        Assert.Equal("admin@example.org", payload["adminEmail"]);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenTenantContextHasNoAdminEmail()
    {
        await using var dbContext = CreateDbContext();
        var tenantContext = new TenantContext
        {
            TenantId = Guid.NewGuid(),
            Tenant = new Tenant(),
            DeploymentMode = DeploymentMode.SaaS,
            SubscriptionTier = SubscriptionTier.Free
        };

        var tenantContextAccessor = new Mock<ITenantContextAccessor>();
        tenantContextAccessor.SetupGet(accessor => accessor.TenantContext).Returns(tenantContext);

        var controller = new TenantAccessRequestController(tenantContextAccessor.Object, dbContext);

        var result = await controller.Get();

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsOkWithAdminEmail_WhenSelfHosted_DefaultTenantExists()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Tenants.Add(new Tenant
        {
            Id = TenantContext.DefaultTenantId,
            Name = "Default Tenant",
            Subdomain = "default",
            KeycloakRealm = "student-registrar",
            AdminEmail = "selfhosted@example.org",
            SubscriptionTier = SubscriptionTier.Enterprise,
            SubscriptionStatus = SubscriptionStatus.Active,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var tenantContextAccessor = new Mock<ITenantContextAccessor>();
        tenantContextAccessor.SetupGet(accessor => accessor.TenantContext)
            .Returns(TenantContext.ForSelfHosted());

        var controller = new TenantAccessRequestController(tenantContextAccessor.Object, dbContext);

        var result = await controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<Dictionary<string, string>>(ok.Value);
        Assert.Equal("selfhosted@example.org", payload["adminEmail"]);
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantAccessRequestControllerTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options);
    }
}

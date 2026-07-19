using Microsoft.AspNetCore.Mvc;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class TenantAccessRequestControllerTests
{
    [Fact]
    public void Get_ReturnsOkWithAdminEmail_WhenTenantContextContainsTenant()
    {
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

        var controller = new TenantAccessRequestController(tenantContextAccessor.Object);

        var result = controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<Dictionary<string, string>>(ok.Value);

        Assert.Equal("admin@example.org", payload["adminEmail"]);
    }

    [Fact]
    public void Get_ReturnsNotFound_WhenTenantContextHasNoAdminEmail()
    {
        var tenantContext = new TenantContext
        {
            TenantId = Guid.NewGuid(),
            Tenant = new Tenant(),
            DeploymentMode = DeploymentMode.SaaS,
            SubscriptionTier = SubscriptionTier.Free
        };

        var tenantContextAccessor = new Mock<ITenantContextAccessor>();
        tenantContextAccessor.SetupGet(accessor => accessor.TenantContext).Returns(tenantContext);

        var controller = new TenantAccessRequestController(tenantContextAccessor.Object);

        var result = controller.Get();

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}

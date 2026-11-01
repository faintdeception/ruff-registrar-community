using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using StudentRegistrar.Api.Controllers;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Controllers;

public class DemoLoginControllerTests
{
    private static DemoLoginOptions EnabledOptions() => new()
    {
        Enabled = true,
        TenantSubdomain = "dreampipe-studios",
        Admin = new DemoLoginAccountOptions { Username = "demo-admin", Password = "admin-pw" },
        Member = new DemoLoginAccountOptions { Username = "demo-member", Password = "member-pw" }
    };

    private static StudentRegistrarDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"DemoLoginControllerTests-{Guid.NewGuid()}")
            .Options);

    private static DemoLoginController CreateController(DemoLoginOptions options, string? subdomain)
    {
        var accessor = new Mock<ITenantContextAccessor>();
        accessor.SetupGet(a => a.TenantContext).Returns(subdomain is null
            ? null
            : new TenantContext
            {
                TenantId = Guid.NewGuid(),
                Tenant = new Tenant { Subdomain = subdomain },
                DeploymentMode = DeploymentMode.SaaS,
                SubscriptionTier = SubscriptionTier.Free
            });

        return new DemoLoginController(accessor.Object, Options.Create(options), CreateDbContext());
    }

    [Fact]
    public async Task Get_UsesDefaultTenant_WhenSelfHosted()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Tenants.Add(new Tenant { Id = TenantContext.DefaultTenantId, Name = "Local", Subdomain = "dreampipe-studios", AdminEmail = "a@example.com" });
        await dbContext.SaveChangesAsync();
        var accessor = new Mock<ITenantContextAccessor>();
        accessor.SetupGet(a => a.TenantContext).Returns(TenantContext.ForSelfHosted());

        var result = await new DemoLoginController(accessor.Object, Options.Create(EnabledOptions()), dbContext).Get();

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsBothAccounts_ForDemoTenant()
    {
        var result = await CreateController(EnabledOptions(), "Dreampipe-Studios").Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<DemoLoginResponse>(ok.Value);
        Assert.Equal(["admin", "member"], payload.Accounts.Select(a => a.Role));
        Assert.Equal("demo-admin", payload.Accounts[0].Username);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_ForOtherTenants()
    {
        var result = await CreateController(EnabledOptions(), "someone-else").Get();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenThereIsNoTenantContext()
    {
        var result = await CreateController(EnabledOptions(), null).Get();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenDisabled()
    {
        var options = EnabledOptions();
        options.Enabled = false;

        var result = await CreateController(options, "dreampipe-studios").Get();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenNoTenantConfigured()
    {
        var options = EnabledOptions();
        options.TenantSubdomain = null;

        var result = await CreateController(options, "dreampipe-studios").Get();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Get_OmitsIncompleteAccounts()
    {
        var options = EnabledOptions();
        options.Member.Password = null;

        var result = await CreateController(options, "dreampipe-studios").Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<DemoLoginResponse>(ok.Value);
        Assert.Equal(["admin"], payload.Accounts.Select(a => a.Role));
    }
}

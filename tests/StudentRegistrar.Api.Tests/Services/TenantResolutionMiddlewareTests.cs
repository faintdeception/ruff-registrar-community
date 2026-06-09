using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class TenantResolutionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Blocks_Suspended_Offboarding_Tenant()
    {
        await using var dbContext = CreateDbContext();
        var tenant = CreateTenant();
        tenant.IsActive = false;
        tenant.SubscriptionStatus = SubscriptionStatus.Cancelled;
        tenant.OffboardingStatus = TenantOffboardingStatus.Suspended;

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var tenantContextAccessor = new TenantContextAccessor();
        var middleware = CreateMiddleware();
        var httpContext = CreateHttpContext($"/org/{tenant.Subdomain}/courses");

        await middleware.InvokeAsync(httpContext, tenantContextAccessor, dbContext);

        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        Assert.Null(tenantContextAccessor.TenantContext);

        var payload = await ReadJsonAsync(httpContext);
        Assert.Equal("Organization access is suspended", payload.GetProperty("error").GetString());
        Assert.Equal("tenant_access_blocked", payload.GetProperty("code").GetString());
        Assert.Equal("suspended", payload.GetProperty("tenantStatus").GetString());
        Assert.True(payload.GetProperty("canRecoverBilling").GetBoolean());
    }

    [Fact]
    public async Task InvokeAsync_Blocks_CancellationScheduled_Tenant_After_Access_End()
    {
        await using var dbContext = CreateDbContext();
        var tenant = CreateTenant();
        tenant.OffboardingStatus = TenantOffboardingStatus.CancellationScheduled;
        tenant.AccessEndsAtUtc = DateTime.UtcNow.AddMinutes(-5);

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var tenantContextAccessor = new TenantContextAccessor();
        var middleware = CreateMiddleware();
        var httpContext = CreateHttpContext($"/org/{tenant.Subdomain}/courses");

        await middleware.InvokeAsync(httpContext, tenantContextAccessor, dbContext);

        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        Assert.Null(tenantContextAccessor.TenantContext);

        var payload = await ReadJsonAsync(httpContext);
        Assert.Equal("Organization access has ended", payload.GetProperty("error").GetString());
        Assert.Equal("access-ended", payload.GetProperty("tenantStatus").GetString());
        Assert.True(payload.GetProperty("canRecoverBilling").GetBoolean());
    }

    [Fact]
    public async Task InvokeAsync_Blocks_BillingHold_Tenant_WithRecoveryMetadata()
    {
        await using var dbContext = CreateDbContext();
        var tenant = CreateTenant();
        tenant.IsActive = false;
        tenant.SubscriptionStatus = SubscriptionStatus.BillingHold;

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var tenantContextAccessor = new TenantContextAccessor();
        var middleware = CreateMiddleware();
        var httpContext = CreateHttpContext($"/org/{tenant.Subdomain}/courses");

        await middleware.InvokeAsync(httpContext, tenantContextAccessor, dbContext);

        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        var payload = await ReadJsonAsync(httpContext);
        Assert.Equal("Billing must be restored before organization access can continue", payload.GetProperty("error").GetString());
        Assert.Equal("billing-hold", payload.GetProperty("tenantStatus").GetString());
        Assert.True(payload.GetProperty("canRecoverBilling").GetBoolean());
    }

    private static TenantResolutionMiddleware CreateMiddleware()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DEPLOYMENT_MODE"] = "saas"
            })
            .Build();

        return new TenantResolutionMiddleware(
            _ => Task.CompletedTask,
            NullLogger<TenantResolutionMiddleware>.Instance,
            configuration,
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        return document.RootElement.Clone();
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantResolutionMiddlewareTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options);
    }

    private static Tenant CreateTenant()
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Sunrise",
            Subdomain = $"tenant-{Guid.NewGuid():N}"[..20],
            KeycloakRealm = "sunrise-org",
            AdminEmail = "admin@sunrise.local",
            SubscriptionTier = SubscriptionTier.Pro,
            SubscriptionStatus = SubscriptionStatus.Active,
            IsActive = true
        };
    }
}
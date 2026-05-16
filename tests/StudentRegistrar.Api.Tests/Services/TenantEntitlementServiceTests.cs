using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class TenantEntitlementServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_ForProTenant_ReturnsPaidFeaturesWithoutBranding()
    {
        var tenant = CreateTenant(SubscriptionTier.Pro);
        await using var provider = CreateProvider(TenantContext.ForSaaS(tenant));
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ITenantEntitlementService>();
        var snapshot = await service.GetSnapshotAsync();

        Assert.True(snapshot.HasPayments);
        Assert.True(snapshot.HasMembershipFees);
        Assert.False(snapshot.HasBranding);
        Assert.DoesNotContain(TenantFeature.Branding, snapshot.EnabledFeatures);
    }

    [Fact]
    public async Task GetSnapshotAsync_AppliesEnabledOverride()
    {
        var tenant = CreateTenant(SubscriptionTier.Pro);
        await using var provider = CreateProvider(TenantContext.ForSaaS(tenant));
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StudentRegistrarDbContext>();
        dbContext.TenantFeatureOverrides.Add(new TenantFeatureOverride
        {
            TenantId = tenant.Id,
            FeatureKey = TenantFeature.Branding,
            IsEnabled = true,
        });
        await dbContext.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<ITenantEntitlementService>();
        var snapshot = await service.GetSnapshotAsync();

        Assert.True(snapshot.HasBranding);
        Assert.Contains(TenantFeature.Branding, snapshot.EnabledFeatures);
    }

    [Fact]
    public async Task GetSnapshotAsync_AppliesDisabledOverride()
    {
        var tenant = CreateTenant(SubscriptionTier.Enterprise);
        await using var provider = CreateProvider(TenantContext.ForSaaS(tenant));
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StudentRegistrarDbContext>();
        dbContext.TenantFeatureOverrides.Add(new TenantFeatureOverride
        {
            TenantId = tenant.Id,
            FeatureKey = TenantFeature.Branding,
            IsEnabled = false,
        });
        await dbContext.SaveChangesAsync();

        var service = scope.ServiceProvider.GetRequiredService<ITenantEntitlementService>();
        var snapshot = await service.GetSnapshotAsync();

        Assert.False(snapshot.HasBranding);
        Assert.DoesNotContain(TenantFeature.Branding, snapshot.EnabledFeatures);
    }

    [Fact]
    public async Task GetSnapshotAsync_ForSelfHostedTenant_EnablesAllKnownFeatures()
    {
        await using var provider = CreateProvider(TenantContext.ForSelfHosted());
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<ITenantEntitlementService>();
        var snapshot = await service.GetSnapshotAsync();

        Assert.True(snapshot.IsSelfHostedMode);
        foreach (var feature in TenantFeature.All)
        {
            Assert.Contains(feature, snapshot.EnabledFeatures);
        }
    }

    private static ServiceProvider CreateProvider(ITenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddDbContext<StudentRegistrarDbContext>(options => options.UseInMemoryDatabase($"TenantEntitlementServiceTests-{Guid.NewGuid()}"));
        services.AddScoped<ITenantProvider, TenantContextProvider>();
        services.AddScoped<ITenantEntitlementService, TenantEntitlementService>();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DEPLOYMENT_MODE"] = tenantContext.IsSelfHostedMode ? "selfhosted" : "saas"
        }).Build());
        services.AddSingleton<ITenantContextAccessor>(new TenantContextAccessor
        {
            TenantContext = tenantContext
        });
        return services.BuildServiceProvider();
    }

    private static Tenant CreateTenant(SubscriptionTier subscriptionTier)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Subdomain = $"tenant-{Guid.NewGuid():N}"[..20],
            KeycloakRealm = "test-realm",
            AdminEmail = "admin@example.com",
            SubscriptionTier = subscriptionTier,
            SubscriptionStatus = SubscriptionStatus.Active,
            IsActive = true,
        };
    }
}
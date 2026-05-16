using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class BrandingSettingsServiceTests
{
    [Fact]
    public async Task UpdateSettingsAsync_Throws_WhenBrandingIsNotEnabled()
    {
        var tenant = CreateTenant(SubscriptionTier.Pro);
        await using var provider = CreateProvider(TenantContext.ForSaaS(tenant));
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IBrandingSettingsService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateSettingsAsync(new UpdateBrandingSettingsDto()));
    }

    [Fact]
    public async Task UpdateSettingsAsync_PersistsAndSanitizesBranding()
    {
        var tenant = CreateTenant(SubscriptionTier.Enterprise);
        await using var provider = CreateProvider(TenantContext.ForSaaS(tenant));
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IBrandingSettingsService>();
        var result = await service.UpdateSettingsAsync(new UpdateBrandingSettingsDto
        {
            DisplayName = "Acme Co-op",
            PrimaryColor = "#112233",
            SecondaryColor = "#445566",
            FooterText = "Welcome families",
            HidePoweredBy = true,
            CustomCss = "<script>alert('xss')</script>.safe { color: red; }",
        });

        Assert.Equal("Acme Co-op", result.DisplayName);
        Assert.Equal("#112233", result.PrimaryColor);
        Assert.True(result.HidePoweredBy);
        Assert.DoesNotContain("<script>", result.SanitizedCustomCss, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".safe", result.SanitizedCustomCss);
    }

    private static ServiceProvider CreateProvider(ITenantContext tenantContext)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddDbContext<StudentRegistrarDbContext>(options => options.UseInMemoryDatabase($"BrandingSettingsServiceTests-{Guid.NewGuid()}"));
        services.AddScoped<ITenantProvider, TenantContextProvider>();
        services.AddScoped<ITenantEntitlementService, TenantEntitlementService>();
        services.AddScoped<IBrandingSettingsService, BrandingSettingsService>();
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
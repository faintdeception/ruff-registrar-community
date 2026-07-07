using Microsoft.EntityFrameworkCore;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class TenantHomeContentServiceTests
{
    [Fact]
    public async Task GetHomeContentAsync_UsesTenantNameFallbackWhenNoCustomValues()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t => t.SetTheme(new TenantTheme()));

        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = new TenantHomeContentService(dbContext, accessor);

        var result = await service.GetHomeContentAsync();

        Assert.Equal("Welcome to Sunrise Homeschool Co-op", result.WelcomeTitle);
        Assert.False(result.HasCustomWelcomeTitle);
        Assert.False(result.HasCustomWelcomeBlurb);
        Assert.Contains("homeschool management system", result.WelcomeBlurb, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateHomeContentAsync_PersistsCustomValues()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);

        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = new TenantHomeContentService(dbContext, accessor);

        var result = await service.UpdateHomeContentAsync(new UpdateTenantHomeContentRequest
        {
            WelcomeTitle = "Welcome to NoVA Scoop",
            WelcomeBlurb = "A place for your coop families to register and manage classes."
        });

        Assert.Equal("Welcome to NoVA Scoop", result.WelcomeTitle);
        Assert.Equal("A place for your coop families to register and manage classes.", result.WelcomeBlurb);
        Assert.True(result.HasCustomWelcomeTitle);
        Assert.True(result.HasCustomWelcomeBlurb);

        var updatedTenant = await dbContext.Tenants.SingleAsync();
        var updatedTheme = updatedTenant.GetTheme();
        Assert.Equal("Welcome to NoVA Scoop", updatedTheme.HomeWelcomeTitle);
        Assert.Equal("A place for your coop families to register and manage classes.", updatedTheme.HomeWelcomeBlurb);
    }

    [Fact]
    public async Task UpdateHomeContentAsync_BlankValuesResetToDefaultFallback()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t =>
        {
            t.SetTheme(new TenantTheme
            {
                HomeWelcomeTitle = "Welcome to NoVA Scoop",
                HomeWelcomeBlurb = "Custom"
            });
        });

        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = new TenantHomeContentService(dbContext, accessor);

        var result = await service.UpdateHomeContentAsync(new UpdateTenantHomeContentRequest
        {
            WelcomeTitle = "   ",
            WelcomeBlurb = ""
        });

        Assert.Equal("Welcome to Sunrise Homeschool Co-op", result.WelcomeTitle);
        Assert.Contains("homeschool management system", result.WelcomeBlurb, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.HasCustomWelcomeTitle);
        Assert.False(result.HasCustomWelcomeBlurb);
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantHomeContentServiceTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options, new StaticTenantProvider());
    }

    private static Tenant SeedTenant(StudentRegistrarDbContext dbContext, Action<Tenant>? configure = null)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Sunrise Homeschool Co-op",
            Subdomain = "sunrise",
            SubscriptionTier = SubscriptionTier.Pro,
            SubscriptionStatus = SubscriptionStatus.Active,
            AdminEmail = "admin@sunrise.local",
            KeycloakRealm = "sunrise-org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        configure?.Invoke(tenant);

        dbContext.Tenants.Add(tenant);
        dbContext.SaveChanges();
        return tenant;
    }

    private sealed class StaticTenantProvider : ITenantProvider
    {
        public Guid? CurrentTenantId => Guid.NewGuid();

        public bool ShouldApplyTenantFilter => false;
    }
}

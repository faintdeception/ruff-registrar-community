using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class TenantPaymentConnectServiceTests
{
    // ──────────────────────────────────────────────
    //  GetStatusAsync — availability gate checks
    // ──────────────────────────────────────────────

    [Fact]
    public async Task GetStatusAsync_WhenSelfHosted_ReturnsUnavailableWithReason()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSelfHosted(tenant.Id)
        };

        var service = CreateService(dbContext, accessor, stripeKey: null);

        var result = await service.GetStatusAsync();

        Assert.False(result.IsAvailable);
        Assert.False(result.IsSaaSMode);
        Assert.Equal("Tenant-owned Stripe Connect is available in SaaS deployments only.", result.UnavailableReason);
        Assert.False(result.IsConnected);
    }

    [Fact]
    public async Task GetStatusAsync_WhenSaaS_FreeTier_ReturnsUnavailableWithReason()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t => t.SubscriptionTier = SubscriptionTier.Free);
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = CreateService(dbContext, accessor, stripeKey: "sk_test_fake");

        var result = await service.GetStatusAsync();

        Assert.False(result.IsAvailable);
        Assert.True(result.IsSaaSMode);
        Assert.Equal("Stripe Connect is available on paid tiers (Pro and Enterprise).", result.UnavailableReason);
    }

    [Fact]
    public async Task GetStatusAsync_WhenSaaS_ProTier_InactiveTenant_ReturnsUnavailableWithReason()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t =>
        {
            t.SubscriptionTier = SubscriptionTier.Pro;
            t.IsActive = false;
        });
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = CreateService(dbContext, accessor, stripeKey: "sk_test_fake");

        var result = await service.GetStatusAsync();

        Assert.False(result.IsAvailable);
        Assert.Equal("Tenant is inactive.", result.UnavailableReason);
    }

    [Fact]
    public async Task GetStatusAsync_WhenSaaS_ProTier_Active_NoStripeKey_ReturnsUnavailableWithReason()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = CreateService(dbContext, accessor, stripeKey: null);

        var result = await service.GetStatusAsync();

        Assert.False(result.IsAvailable);
        Assert.False(result.PlatformStripeConfigured);
        Assert.Equal("Stripe Connect is unavailable in this environment.", result.UnavailableReason);
    }

    [Fact]
    public async Task GetStatusAsync_WhenSaaS_ProTier_Active_StripeConfigured_NotConnected_ReturnsAvailable()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext); // no StripeConnectAccountId
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = CreateService(dbContext, accessor, stripeKey: "sk_test_fake");

        var result = await service.GetStatusAsync();

        Assert.True(result.IsAvailable);
        Assert.True(result.IsSaaSMode);
        Assert.True(result.PlatformStripeConfigured);
        Assert.False(result.IsConnected);
        Assert.Null(result.StripeConnectAccountId);
        Assert.Null(result.UnavailableReason);
    }

    [Fact]
    public async Task GetStatusAsync_WhenConnectedAccountExists_ReturnsConnectedStatus()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t =>
        {
            t.StripeConnectAccountId = "acct_1234567890";
            t.StripeConnectDetailsSubmitted = true;
            t.StripeConnectChargesEnabled = true;
            t.StripeConnectPayoutsEnabled = true;
            t.StripeConnectOnboardingCompletedAtUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        });
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = CreateService(dbContext, accessor, stripeKey: "sk_test_fake");

        var result = await service.GetStatusAsync();

        Assert.True(result.IsAvailable);
        Assert.True(result.IsConnected);
        Assert.Equal("acct_1234567890", result.StripeConnectAccountId);
        Assert.True(result.DetailsSubmitted);
        Assert.True(result.ChargesEnabled);
        Assert.True(result.PayoutsEnabled);
        Assert.Equal(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), result.OnboardingCompletedAtUtc);
    }

    // ──────────────────────────────────────────────
    //  RefreshStatusAsync — Stripe skip paths
    // ──────────────────────────────────────────────

    [Fact]
    public async Task RefreshStatusAsync_WhenNotAvailable_SkipsStripeAndReturnsCurrentStatus()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext); // self-hosted context → not available
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSelfHosted(tenant.Id)
        };

        // No Stripe key; any attempt to call Stripe SDK would throw.
        var service = CreateService(dbContext, accessor, stripeKey: null);

        // Should return gracefully without making a Stripe API call.
        var result = await service.RefreshStatusAsync();

        Assert.False(result.IsAvailable);
        Assert.False(result.IsConnected);
    }

    [Fact]
    public async Task RefreshStatusAsync_WhenNoAccountId_SkipsStripeAndReturnsCurrentStatus()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext); // no StripeConnectAccountId
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        // Available (Pro + active + Stripe key) but no account ID yet.
        var service = CreateService(dbContext, accessor, stripeKey: "sk_test_fake");

        // Should return gracefully without making a Stripe API call.
        var result = await service.RefreshStatusAsync();

        Assert.True(result.IsAvailable);
        Assert.False(result.IsConnected);
        Assert.Null(result.StripeConnectAccountId);
    }

    // ──────────────────────────────────────────────
    //  CreateOnboardingLinkAsync — gate throws
    // ──────────────────────────────────────────────

    [Fact]
    public async Task CreateOnboardingLinkAsync_WhenSelfHosted_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSelfHosted(tenant.Id)
        };

        var service = CreateService(dbContext, accessor, stripeKey: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOnboardingLinkAsync());
    }

    [Fact]
    public async Task CreateOnboardingLinkAsync_WhenFreeTier_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t => t.SubscriptionTier = SubscriptionTier.Free);
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = CreateService(dbContext, accessor, stripeKey: "sk_test_fake");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOnboardingLinkAsync());
    }

    [Fact]
    public async Task CreateOnboardingLinkAsync_WhenInactiveTenant_ThrowsInvalidOperationException()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t => t.IsActive = false);
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = CreateService(dbContext, accessor, stripeKey: "sk_test_fake");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOnboardingLinkAsync());
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private static TenantPaymentConnectService CreateService(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor accessor,
        string? stripeKey)
    {
        var configValues = new Dictionary<string, string?>();
        if (stripeKey is not null)
        {
            configValues["Stripe:SecretKey"] = stripeKey;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        return new TenantPaymentConnectService(
            dbContext,
            accessor,
            httpContextAccessor.Object,
            configuration,
            NullLogger<TenantPaymentConnectService>.Instance);
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantPaymentConnectServiceTests-{Guid.NewGuid()}")
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
        public Guid? CurrentTenantId => null;
        public bool ShouldApplyTenantFilter => false;
    }
}

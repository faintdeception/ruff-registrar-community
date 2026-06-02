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

public class TenantBillingServiceTests
{
    [Fact]
    public async Task GetCurrentBillingAsync_WhenSelfHosted_ReturnsUnavailableStatus()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSelfHosted(tenant.Id)
        };

        var service = CreateService(dbContext, accessor, new Mock<ITenantStripeBillingGateway>(MockBehavior.Strict).Object);

        var result = await service.GetCurrentBillingAsync();

        Assert.False(result.CanManageBilling);
        Assert.Equal("Billing is managed outside this deployment.", result.UnavailableReason);
        Assert.False(result.IsSaaSMode);
    }

    [Fact]
    public async Task ScheduleCancellationAtPeriodEndAsync_WhenEligible_RecordsOffboardingSchedule()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };
        var stripeGateway = new Mock<ITenantStripeBillingGateway>();
        stripeGateway.SetupGet(g => g.IsConfigured).Returns(true);
        stripeGateway
            .Setup(g => g.ScheduleCancellationAtPeriodEndAsync("sub_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantStripeSubscriptionSnapshot(true, new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc)));

        var service = CreateService(dbContext, accessor, stripeGateway.Object);

        var result = await service.ScheduleCancellationAtPeriodEndAsync();
        var updatedTenant = await dbContext.Tenants.SingleAsync();

        Assert.Equal("sunrise", result.Subdomain);
        Assert.False(result.AlreadyScheduled);
        Assert.Equal(TenantOffboardingStatus.CancellationScheduled, updatedTenant.OffboardingStatus);
        Assert.Equal(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc), updatedTenant.AccessEndsAtUtc);
        Assert.Equal(new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc), updatedTenant.DeletionScheduledAtUtc);
        Assert.Equal("Tenant admin scheduled in-app cancellation at period end.", updatedTenant.OffboardingReason);
    }

    [Fact]
    public async Task ScheduleCancellationAtPeriodEndAsync_WhenTenantAlreadyScheduled_ReturnsExistingSchedule()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t =>
        {
            t.OffboardingStatus = TenantOffboardingStatus.CancellationScheduled;
            t.AccessEndsAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        });
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        var service = CreateService(dbContext, accessor, new Mock<ITenantStripeBillingGateway>(MockBehavior.Strict).Object);

        var result = await service.ScheduleCancellationAtPeriodEndAsync();

        Assert.True(result.AlreadyScheduled);
        Assert.Equal(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc), result.AccessEndsAtUtc);
    }

    [Fact]
    public async Task UndoScheduledCancellationAsync_WhenScheduled_ClearsOffboardingState()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t =>
        {
            t.OffboardingStatus = TenantOffboardingStatus.CancellationScheduled;
            t.OffboardingRequestedAtUtc = new DateTime(2026, 5, 28, 12, 0, 0, DateTimeKind.Utc);
            t.OffboardingReason = "Tenant admin scheduled in-app cancellation at period end.";
            t.AccessEndsAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
            t.DeletionScheduledAtUtc = new DateTime(2026, 7, 21, 12, 0, 0, DateTimeKind.Utc);
        });
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };
        var stripeGateway = new Mock<ITenantStripeBillingGateway>();
        stripeGateway.SetupGet(g => g.IsConfigured).Returns(true);
        stripeGateway
            .Setup(g => g.UndoCancellationAtPeriodEndAsync("sub_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantStripeSubscriptionSnapshot(false, new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc)));

        var service = CreateService(dbContext, accessor, stripeGateway.Object);

        var result = await service.UndoScheduledCancellationAsync();
        var updatedTenant = await dbContext.Tenants.SingleAsync();

        Assert.False(result.CancellationScheduled);
        Assert.Null(result.AccessEndsAtUtc);
        Assert.Equal(TenantOffboardingStatus.None, updatedTenant.OffboardingStatus);
        Assert.Null(updatedTenant.OffboardingRequestedAtUtc);
        Assert.Null(updatedTenant.OffboardingReason);
        Assert.Null(updatedTenant.AccessEndsAtUtc);
        Assert.Null(updatedTenant.DeletionScheduledAtUtc);
    }

    [Fact]
    public async Task GetCurrentBillingAsync_WhenCancellationScheduled_AllowsUndo()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext, t =>
        {
            t.OffboardingStatus = TenantOffboardingStatus.CancellationScheduled;
            t.AccessEndsAtUtc = new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
        });
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };
        var stripeGateway = new Mock<ITenantStripeBillingGateway>();
        stripeGateway.SetupGet(g => g.IsConfigured).Returns(true);
        stripeGateway
            .Setup(g => g.GetSubscriptionAsync("sub_123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantStripeSubscriptionSnapshot(true, new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc)));

        var service = CreateService(dbContext, accessor, stripeGateway.Object);

        var result = await service.GetCurrentBillingAsync();

        Assert.False(result.CanManageBilling);
        Assert.True(result.CanUndoCancellation);
        Assert.True(result.CancelAtPeriodEnd);
    }

    private static TenantBillingService CreateService(
        StudentRegistrarDbContext dbContext,
        ITenantContextAccessor accessor,
        ITenantStripeBillingGateway stripeGateway)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SaaSOffboarding:RetentionDays"] = "21"
            })
            .Build();

        return new TenantBillingService(
            dbContext,
            accessor,
            stripeGateway,
            configuration,
            NullLogger<TenantBillingService>.Instance);
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantBillingServiceTests-{Guid.NewGuid()}")
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
            StripeSubscriptionId = "sub_123",
            StripeCustomerId = "cus_123",
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
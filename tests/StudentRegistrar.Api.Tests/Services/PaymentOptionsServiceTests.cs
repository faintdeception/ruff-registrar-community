using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class PaymentOptionsServiceTests
{
    [Fact]
    public async Task UpdateCurrentTenantPaymentOptionsAsync_SavesStripeSettings_ForPaidTenant()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Sunrise",
            Subdomain = "sunrise",
            SubscriptionTier = SubscriptionTier.Pro,
            SubscriptionStatus = SubscriptionStatus.Active,
            KeycloakRealm = "sunrise-org",
            AdminEmail = "admin@sunrise.local"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TenantContext.ForSaaS(new Tenant
        {
            Id = tenantId,
            SubscriptionTier = SubscriptionTier.Pro,
            Name = "Sunrise",
            Subdomain = "sunrise",
            KeycloakRealm = "sunrise-org",
            AdminEmail = "admin@sunrise.local"
        }));

        var result = await service.UpdateCurrentTenantPaymentOptionsAsync(new UpdatePaymentOptionsDto
        {
            EnableStripePayments = true,
            StripeAccountToken = "acct_test_9876"
        });

        result.EnableStripePayments.Should().BeTrue();
        result.HasStripeAccountToken.Should().BeTrue();
        result.StripeAccountTokenPreview.Should().Be("****9876");

        var persistedTenant = await dbContext.Tenants.SingleAsync(t => t.Id == tenantId);
        persistedTenant.GetPaymentOptions().Stripe.Enabled.Should().BeTrue();
        persistedTenant.GetPaymentOptions().Stripe.AccountToken.Should().NotBe("acct_test_9876");
        persistedTenant.GetPaymentOptions().Stripe.AccountToken.Should().StartWith("dp::");
        persistedTenant.PaymentOptionsJson.Should().NotContain("acct_test_9876");
    }

    [Fact]
    public async Task UpdateCurrentTenantPaymentOptionsAsync_RejectsMissingToken_WhenEnablingStripe()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Sunrise",
            Subdomain = "sunrise",
            SubscriptionTier = SubscriptionTier.Pro,
            SubscriptionStatus = SubscriptionStatus.Active,
            KeycloakRealm = "sunrise-org",
            AdminEmail = "admin@sunrise.local"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TenantContext.ForSaaS(new Tenant
        {
            Id = tenantId,
            SubscriptionTier = SubscriptionTier.Pro,
            Name = "Sunrise",
            Subdomain = "sunrise",
            KeycloakRealm = "sunrise-org",
            AdminEmail = "admin@sunrise.local"
        }));

        var act = () => service.UpdateCurrentTenantPaymentOptionsAsync(new UpdatePaymentOptionsDto
        {
            EnableStripePayments = true,
            StripeAccountToken = "  "
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Stripe account token is required*");
    }

    [Fact]
    public async Task UpdateCurrentTenantPaymentOptionsAsync_RejectsEnable_ForFreeHostedTenant()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Free Org",
            Subdomain = "free-org",
            SubscriptionTier = SubscriptionTier.Free,
            SubscriptionStatus = SubscriptionStatus.Active,
            KeycloakRealm = "free-org",
            AdminEmail = "admin@free.local"
        });
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TenantContext.ForSaaS(new Tenant
        {
            Id = tenantId,
            SubscriptionTier = SubscriptionTier.Free,
            Name = "Free Org",
            Subdomain = "free-org",
            KeycloakRealm = "free-org",
            AdminEmail = "admin@free.local"
        }));

        var act = () => service.UpdateCurrentTenantPaymentOptionsAsync(new UpdatePaymentOptionsDto
        {
            EnableStripePayments = true,
            StripeAccountToken = "acct_test_0001"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*paid hosted plans*");
    }

    [Fact]
    public async Task GetCurrentTenantPaymentOptionsAsync_CreatesSelfHostedTenant_WhenMissing()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, TenantContext.ForSelfHosted());

        var result = await service.GetCurrentTenantPaymentOptionsAsync();

        result.IsSupported.Should().BeTrue();
        result.CanManagePaymentOptions.Should().BeTrue();
        result.SubscriptionTier.Should().Be(nameof(SubscriptionTier.Enterprise));

        var persistedTenant = await dbContext.Tenants.SingleAsync();
        persistedTenant.Id.Should().Be(TenantContext.DefaultTenantId);
        persistedTenant.SubscriptionTier.Should().Be(SubscriptionTier.Enterprise);
    }

    [Fact]
    public async Task GetCurrentTenantPaymentOptionsAsync_ReadsLegacyPlaintextToken()
    {
        var tenantId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Legacy Org",
            Subdomain = "legacy-org",
            SubscriptionTier = SubscriptionTier.Pro,
            SubscriptionStatus = SubscriptionStatus.Active,
            KeycloakRealm = "legacy-org",
            AdminEmail = "admin@legacy.local"
        };
        tenant.SetPaymentOptions(new TenantPaymentOptions
        {
            Stripe = new StripeTenantPaymentOptions
            {
                Enabled = true,
                AccountToken = "acct_legacy_4242"
            }
        });
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, TenantContext.ForSaaS(new Tenant
        {
            Id = tenantId,
            SubscriptionTier = SubscriptionTier.Pro,
            Name = "Legacy Org",
            Subdomain = "legacy-org",
            KeycloakRealm = "legacy-org",
            AdminEmail = "admin@legacy.local"
        }));

        var result = await service.GetCurrentTenantPaymentOptionsAsync();

        result.EnableStripePayments.Should().BeTrue();
        result.HasStripeAccountToken.Should().BeTrue();
        result.StripeAccountTokenPreview.Should().Be("****4242");
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new StudentRegistrarDbContext(options, new DefaultTenantProvider());
    }

    private static PaymentOptionsService CreateService(StudentRegistrarDbContext dbContext, ITenantContext tenantContext)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:Realm"] = "student-registrar",
                ["SelfHosted:TenantName"] = "Self-Hosted Registrar",
                ["SelfHosted:Subdomain"] = "selfhosted",
                ["SelfHosted:AdminEmail"] = "admin@local.invalid"
            })
            .Build();

        var dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));

        return new PaymentOptionsService(
            dbContext,
            tenantContext,
            dataProtectionProvider,
            configuration,
            Mock.Of<ILogger<PaymentOptionsService>>());
    }
}
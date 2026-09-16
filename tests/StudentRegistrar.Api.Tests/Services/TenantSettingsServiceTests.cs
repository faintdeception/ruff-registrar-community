using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class TenantSettingsServiceTests
{
    [Fact]
    public async Task GetSettingsAsync_NoSettingsYet_ReturnsNotConfigured()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var validatorMock = MockValidator(valid: true);

        var service = CreateService(dbContext, tenant, validatorMock.Object);

        var result = await service.GetSettingsAsync();

        Assert.False(result.InitialInvitePasswordConfigured);
    }

    [Fact]
    public async Task UpdateInitialInvitePasswordAsync_InsecurePassword_ThrowsAndDoesNotPersist()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var validatorMock = MockValidator(valid: false, reasons: new[] { "must be at least 12 characters" });

        var service = CreateService(dbContext, tenant, validatorMock.Object);

        await Assert.ThrowsAsync<InsecureInitialInvitePasswordException>(() =>
            service.UpdateInitialInvitePasswordAsync(new UpdateTenantSettingsRequest { InitialInvitePassword = "password" }));

        var settings = await dbContext.TenantSettings.SingleOrDefaultAsync(s => s.TenantId == tenant.Id);
        Assert.True(settings == null || string.IsNullOrEmpty(settings.InitialInvitePasswordEncrypted));
    }

    [Fact]
    public async Task UpdateInitialInvitePasswordAsync_SecurePassword_PersistsEncryptedValue()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var validatorMock = MockValidator(valid: true);

        var service = CreateService(dbContext, tenant, validatorMock.Object);

        var result = await service.UpdateInitialInvitePasswordAsync(new UpdateTenantSettingsRequest { InitialInvitePassword = "Correct-Horse-99" });

        Assert.True(result.InitialInvitePasswordConfigured);

        var settings = await dbContext.TenantSettings.SingleAsync(s => s.TenantId == tenant.Id);
        Assert.NotEqual("Correct-Horse-99", settings.InitialInvitePasswordEncrypted);
        Assert.NotNull(settings.InitialInvitePasswordEncrypted);
    }

    [Fact]
    public async Task GetValidatedInitialInvitePasswordAsync_NotConfigured_Throws()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);
        var validatorMock = MockValidator(valid: true);

        var service = CreateService(dbContext, tenant, validatorMock.Object);

        await Assert.ThrowsAsync<InsecureInitialInvitePasswordException>(() => service.GetValidatedInitialInvitePasswordAsync());
    }

    [Fact]
    public async Task GetValidatedInitialInvitePasswordAsync_StoredValueNowFailsPolicy_Throws()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);

        // Save while valid...
        var service = CreateService(dbContext, tenant, MockValidator(valid: true).Object);
        await service.UpdateInitialInvitePasswordAsync(new UpdateTenantSettingsRequest { InitialInvitePassword = "Correct-Horse-99" });

        // ...but the policy has since tightened (or the fetch is failing safe-baseline) and now rejects it.
        var stricterService = CreateService(dbContext, tenant, MockValidator(valid: false, reasons: new[] { "must contain more special characters" }).Object);

        await Assert.ThrowsAsync<InsecureInitialInvitePasswordException>(() => stricterService.GetValidatedInitialInvitePasswordAsync());
    }

    [Fact]
    public async Task GetValidatedInitialInvitePasswordAsync_ValidStoredValue_ReturnsDecryptedPassword()
    {
        await using var dbContext = CreateDbContext();
        var tenant = SeedTenant(dbContext);

        var service = CreateService(dbContext, tenant, MockValidator(valid: true).Object);
        await service.UpdateInitialInvitePasswordAsync(new UpdateTenantSettingsRequest { InitialInvitePassword = "Correct-Horse-99" });

        var password = await service.GetValidatedInitialInvitePasswordAsync();

        Assert.Equal("Correct-Horse-99", password);
    }

    private static Mock<IPasswordPolicyValidator> MockValidator(bool valid, IReadOnlyList<string>? reasons = null)
    {
        var mock = new Mock<IPasswordPolicyValidator>();
        mock.Setup(v => v.ValidateAsync(It.IsAny<string?>()))
            .ReturnsAsync(valid
                ? PasswordPolicyValidationResult.Valid(usedFallbackBaseline: false)
                : PasswordPolicyValidationResult.Invalid(reasons ?? new[] { "invalid" }, usedFallbackBaseline: false));
        return mock;
    }

    private static TenantSettingsService CreateService(StudentRegistrarDbContext dbContext, Tenant tenant, IPasswordPolicyValidator validator)
    {
        var accessor = new TenantContextAccessor
        {
            TenantContext = TenantContext.ForSaaS(tenant)
        };

        return new TenantSettingsService(
            dbContext,
            accessor,
            validator,
            new EphemeralDataProtectionProvider(),
            Mock.Of<ILogger<TenantSettingsService>>());
    }

    private static StudentRegistrarDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StudentRegistrarDbContext>()
            .UseInMemoryDatabase($"TenantSettingsServiceTests-{Guid.NewGuid()}")
            .Options;

        return new StudentRegistrarDbContext(options, new StaticTenantProvider());
    }

    private static Tenant SeedTenant(StudentRegistrarDbContext dbContext)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Sunrise Learning Collective",
            Subdomain = "sunrise",
            SubscriptionTier = SubscriptionTier.Pro,
            SubscriptionStatus = SubscriptionStatus.Active,
            AdminEmail = "admin@sunrise.local",
            KeycloakRealm = "sunrise-org",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

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

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StudentRegistrar.Api.Services.Infrastructure;
using StudentRegistrar.Data;
using StudentRegistrar.Models;
using Xunit;
using RegistrarUser = StudentRegistrar.Models.User;

namespace StudentRegistrar.Api.Tests.Services;

public class TenantAuthorizationTests
{
    [Fact]
    public void AddTenantAuthorization_Should_Make_TenantMembership_The_Default_Authorization_Policy()
    {
        using var provider = BuildServiceProvider("saas", tenantContext: null);

        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.Contains(options.DefaultPolicy.Requirements, requirement => requirement is DenyAnonymousAuthorizationRequirement);
        Assert.Contains(options.DefaultPolicy.Requirements, requirement => requirement is TenantMembershipRequirement);
        Assert.Contains(options.GetPolicy("TenantMember")!.Requirements, requirement => requirement is TenantMembershipRequirement);
    }

    [Fact]
    public async Task AuthorizeAsync_Should_Deny_SaaS_Request_When_Tenant_Context_Is_Missing()
    {
        using var provider = BuildServiceProvider("saas", tenantContext: null);

        var result = await AuthorizeAsync(provider, "user-1");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_Should_Succeed_When_SaaS_User_Belongs_To_Resolved_Tenant()
    {
        var tenant = CreateTenant();
        var user = CreateUser("user-1", tenant.Id);
        using var provider = BuildServiceProvider("saas", TenantContext.ForSaaS(tenant), user);

        var result = await AuthorizeAsync(provider, user.KeycloakId);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_Should_Succeed_When_SaaS_User_Uses_Sub_Claim()
    {
        var tenant = CreateTenant();
        var user = CreateUser("user-sub", tenant.Id);
        using var provider = BuildServiceProvider("saas", TenantContext.ForSaaS(tenant), user);

        var result = await AuthorizeAsync(provider, user.KeycloakId, userIdClaimType: "sub");

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_Should_Deny_When_SaaS_User_Belongs_To_A_Different_Tenant()
    {
        var requestedTenant = CreateTenant();
        var actualTenantId = Guid.NewGuid();
        var user = CreateUser("dual-role-user", actualTenantId);
        using var provider = BuildServiceProvider("saas", TenantContext.ForSaaS(requestedTenant), user);

        var result = await AuthorizeAsync(provider, user.KeycloakId, "Member", "Educator");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthorizeAsync_Should_Succeed_In_SelfHosted_Mode_When_Tenant_Context_Is_Missing()
    {
        using var provider = BuildServiceProvider("selfhosted", tenantContext: null);

        var result = await AuthorizeAsync(provider, "selfhosted-user");

        Assert.True(result.Succeeded);
    }

    private static ServiceProvider BuildServiceProvider(
        string deploymentMode,
        ITenantContext? tenantContext,
        params RegistrarUser[] users)
    {
        var services = new ServiceCollection();
        var databaseName = $"TenantAuthorizationTests-{Guid.NewGuid()}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DEPLOYMENT_MODE"] = deploymentMode
            })
            .Build();

        var tenantContextAccessor = new TenantContextAccessor
        {
            TenantContext = tenantContext
        };

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ITenantContextAccessor>(tenantContextAccessor);
        services.AddScoped<ITenantProvider, TenantContextProvider>();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddDbContext<StudentRegistrarDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddTenantAuthorization();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StudentRegistrarDbContext>();
        dbContext.Users.AddRange(users);
        dbContext.SaveChanges();

        return provider;
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(
        IServiceProvider provider,
        string userId,
        string userIdClaimType = ClaimTypes.NameIdentifier,
        params string[] roles)
    {
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value.GetPolicy("TenantMember")!;

        return await authorizationService.AuthorizeAsync(CreatePrincipal(userId, userIdClaimType, roles), resource: null, policy);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, string userIdClaimType, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(userIdClaimType, userId)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    private static Tenant CreateTenant()
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Subdomain = $"tenant-{Guid.NewGuid():N}"[..20],
            KeycloakRealm = "test-realm",
            AdminEmail = "admin@example.com",
            SubscriptionTier = SubscriptionTier.Pro,
            SubscriptionStatus = SubscriptionStatus.Active,
            IsActive = true
        };
    }

    private static RegistrarUser CreateUser(string keycloakId, Guid tenantId)
    {
        return new RegistrarUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = $"{keycloakId}@example.com",
            FirstName = "Test",
            LastName = "User",
            KeycloakId = keycloakId,
            Role = UserRole.Member,
            IsActive = true
        };
    }
}
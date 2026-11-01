using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Api.Services.Infrastructure;
using Xunit;

namespace StudentRegistrar.Api.Tests.Services;

public class DemoAccountGuardMiddlewareTests
{
    private static DemoLoginOptions Options() => new()
    {
        Enabled = true,
        Admin = new DemoLoginAccountOptions { Username = "demo-admin", Password = "x" },
        Member = new DemoLoginAccountOptions { Username = "demo-member", Password = "x" }
    };

    private static DefaultHttpContext Context(string method, string path, string? username)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        if (username is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("preferred_username", username)], "test"));
        }

        return context;
    }

    [Theory]
    [InlineData("POST", "/api/tenant-billing/cancel-at-period-end")]
    [InlineData("PUT", "/api/tenant-settings/initial-invite-password")]
    [InlineData("POST", "/api/tenant-payment-connect/onboarding-link")]
    [InlineData("POST", "/api/tenant-stripe-payments/checkout-session")]
    [InlineData("PUT", "/api/tenant-home-content")]
    [InlineData("POST", "/api/users")]
    [InlineData("PUT", "/api/users/abc")]
    [InlineData("DELETE", "/api/users/abc")]
    [InlineData("POST", "/api/users/abc/email-change-requests")]
    [InlineData("POST", "/api/AccountHolders/bulk-import")]
    public void Blocks_SensitiveWrites_ForDemoAccounts(string method, string path)
    {
        Assert.True(DemoAccountGuardMiddleware.IsBlocked(Context(method, path, "demo-admin"), Options()));
        Assert.True(DemoAccountGuardMiddleware.IsBlocked(Context(method, path, "DEMO-MEMBER"), Options()));
    }

    [Theory]
    [InlineData("GET", "/api/users")]
    [InlineData("GET", "/api/tenant-billing")]
    [InlineData("POST", "/api/users/me/sync")]
    [InlineData("POST", "/api/courses")]
    [InlineData("POST", "/api/enrollments")]
    [InlineData("PUT", "/api/students/abc")]
    public void Allows_NormalUse_ForDemoAccounts(string method, string path)
    {
        Assert.False(DemoAccountGuardMiddleware.IsBlocked(Context(method, path, "demo-admin"), Options()));
    }

    [Fact]
    public void Allows_OtherUsers_AndAnonymous()
    {
        Assert.False(DemoAccountGuardMiddleware.IsBlocked(Context("POST", "/api/users", "real-admin"), Options()));
        Assert.False(DemoAccountGuardMiddleware.IsBlocked(Context("POST", "/api/users", null), Options()));
    }

    [Fact]
    public void Allows_Everything_WhenDemoDisabled()
    {
        var options = Options();
        options.Enabled = false;

        Assert.False(DemoAccountGuardMiddleware.IsBlocked(Context("POST", "/api/users", "demo-admin"), options));
    }
}

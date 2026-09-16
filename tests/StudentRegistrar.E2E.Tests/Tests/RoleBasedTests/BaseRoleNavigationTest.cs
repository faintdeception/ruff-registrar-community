using OpenQA.Selenium;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests.RoleBasedTests;

/// <summary>
/// Base class for role-based navigation tests.
/// Provides common methods for testing navigation permissions and role-based login.
/// </summary>
public abstract class BaseRoleNavigationTest : BaseTest
{
    protected NavigationPage NavigationPage => new(Driver);

    /// <summary>
    /// Verifies that a user can navigate to a specific page using the navigation menu
    /// </summary>
    protected void VerifyCanNavigateToPage(string navItem, string expectedUrlPart, string? description = null)
    {
        description ??= $"Should be able to navigate to {navItem}";
        
        // Go back to home first to ensure clean navigation
        Driver.Navigate().GoToUrl(BaseUrl);
        WaitForPageLoad();
        
        // Use navigation page to click the nav item
        NavigationPage.ClickNavItem(navItem);
        WaitForPageLoad();
        
        // Verify navigation succeeded
        Assert.Contains(expectedUrlPart, Driver.Url);
    }

    /// <summary>
    /// Verifies that a navigation item is visible to the current user
    /// </summary>
    protected void VerifyNavItemVisible(string navItem, string? description = null)
    {
        description ??= $"{navItem} navigation should be visible";
        Assert.True(NavigationPage.IsNavItemVisible(navItem), description);
    }

    /// <summary>
    /// Verifies that a navigation item is NOT visible to the current user
    /// </summary>
    protected void VerifyNavItemNotVisible(string navItem, string? description = null)
    {
        description ??= $"{navItem} navigation should NOT be visible";
        Assert.False(NavigationPage.IsNavItemVisible(navItem), description);
    }

    /// <summary>
    /// Verifies that a navigation item is NOT present in the DOM (not just hidden)
    /// </summary>
    protected void VerifyNavItemNotPresent(string navItem, string? description = null)
    {
        description ??= $"{navItem} navigation should NOT be present in DOM";
        Assert.False(NavigationPage.IsNavItemPresent(navItem), description);
    }

    /// <summary>
    /// Verifies user role information in the navigation
    /// </summary>
    protected void VerifyUserRole(string expectedRole, string? description = null)
    {
        description ??= $"User should have {expectedRole} role";
        var userRoles = NavigationPage.GetUserRoles();
        Assert.Contains(expectedRole, userRoles);
    }

    /// <summary>
    /// Verifies user does NOT have a specific role
    /// </summary>
    protected void VerifyUserDoesNotHaveRole(string unexpectedRole, string? description = null)
    {
        description ??= $"User should NOT have {unexpectedRole} role";
        var userRoles = NavigationPage.GetUserRoles();
        Assert.DoesNotContain(unexpectedRole, userRoles);
    }

    #region Login Methods

    /// <summary>
    /// Login as an admin user
    /// </summary>
    protected void LoginAsAdmin()
    {
        NavigateToHome();
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:AdminUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:AdminUser:Password"] ?? "AdminPass123!";

        loginPage.Login(username, password);
        WaitUntil(d => !d.Url.Contains("/login", StringComparison.OrdinalIgnoreCase), 30, 200, "Admin login did not leave the login page in time");
        WaitForElementVisible(By.Id("logout-button"), 30);

        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());
    }

    /// <summary>
    /// Login as an educator user
    /// </summary>
    protected void LoginAsEducator()
    {
        NavigateToHome();
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:EducatorUser:Username"] ?? "educator1";
        var password = Configuration["TestCredentials:EducatorUser:Password"] ?? "EducatorPass123!";

        loginPage.Login(username, password);
        WaitUntil(d => !d.Url.Contains("/login", StringComparison.OrdinalIgnoreCase), 30, 200, "Educator login did not leave the login page in time");
        WaitForElementVisible(By.Id("logout-button"), 30);

        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());
    }

    /// <summary>
    /// Login as a member user
    /// </summary>
    protected void LoginAsMember()
    {
        NavigateToHome();
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:MemberUser:Username"] ?? "member1";
        var password = Configuration["TestCredentials:MemberUser:Password"] ?? "MemberPass123!";

        loginPage.Login(username, password);
        WaitUntil(d => !d.Url.Contains("/login", StringComparison.OrdinalIgnoreCase), 30, 200, "Member login did not leave the login page in time");
        WaitForElementVisible(By.Id("logout-button"), 30);

        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());
    }

    #endregion

    /// <summary>
    /// Ensures the tenant's initial invite password (used for bulk member import) is configured,
    /// setting it via the System Settings UI if it isn't already. Admin must already be logged in.
    /// Shared by settings tests and bulk-import tests so both stay self-contained/order-independent.
    /// Navigates via UI clicks (client-side routing), NOT a hard URL navigation \u2014 a full page
    /// reload does not restore the Keycloak session on localhost (see SettingsMenuTests), which
    /// would otherwise make this helper (and anything that follows it) fail with a timeout waiting
    /// for admin-only content.
    /// </summary>
    protected void EnsureInitialInvitePasswordConfigured(string password = "Correct-Horse-Battery-99!", bool overwrite = false)
    {
        NavigationPage.ClickSettingsButton();
        WaitForPageLoad();
        NavigationPage.ClickSettingsMenuItem("system");
        WaitForUrlContains("/settings/system");
        WaitForElementVisible(By.CssSelector("[data-testid='invite-password-settings-card']"));

        if (!overwrite && Driver.FindElements(By.CssSelector("[data-testid='initial-invite-password-configured']")).Count > 0)
        {
            return;
        }

        WaitForElementVisible(By.Id("initial-invite-password"));
        var input = Driver.FindElement(By.Id("initial-invite-password"));
        input.Clear();
        input.SendKeys(password);
        WaitForElementVisible(By.CssSelector("[data-testid='save-invite-password-button']"));
        Driver.FindElement(By.CssSelector("[data-testid='save-invite-password-button']")).Click();

        WaitUntil(
            d => d.FindElements(By.CssSelector("[data-testid='initial-invite-password-configured']")).Count > 0,
            15,
            failureMessage: "Initial invite password was not confirmed as configured after saving");
    }
}

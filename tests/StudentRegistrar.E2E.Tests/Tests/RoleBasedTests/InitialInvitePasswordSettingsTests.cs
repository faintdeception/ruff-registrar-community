using OpenQA.Selenium;
using StudentRegistrar.E2E.Tests.Base;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests.RoleBasedTests;

/// <summary>
/// Tests for the admin "Initial Invite Password" system setting used by bulk member import.
/// </summary>
public class InitialInvitePasswordSettingsTests : BaseRoleNavigationTest
{
    [Fact]
    public void Admin_Should_Configure_Initial_Invite_Password_Successfully()
    {
        // Arrange
        LoginAsAdmin();
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/system");
        WaitForPageLoad();
        WaitForElementVisible(By.CssSelector("[data-testid='invite-password-settings-card']"));

        // Act
        var input = Driver.FindElement(By.Id("initial-invite-password"));
        input.Clear();
        input.SendKeys("Correct-Horse-Battery-99!");
        Driver.FindElement(By.Id("save-invite-password-button")).Click();

        // Assert
        WaitForElementVisible(By.CssSelector("[data-testid='invite-password-success']"), 15);
        WaitForElementVisible(By.CssSelector("[data-testid='initial-invite-password-configured']"), 15);
    }

    [Fact]
    public void Admin_Should_See_Clear_Error_When_Invite_Password_Is_Insecure()
    {
        // Arrange
        LoginAsAdmin();
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/system");
        WaitForPageLoad();
        WaitForElementVisible(By.CssSelector("[data-testid='invite-password-settings-card']"));

        // Act - a password that fails any real Keycloak policy (and the conservative fallback baseline)
        var input = Driver.FindElement(By.Id("initial-invite-password"));
        input.Clear();
        input.SendKeys("password");
        Driver.FindElement(By.Id("save-invite-password-button")).Click();

        // Assert - a clear, actionable error is shown instead of silently accepting a weak password
        WaitForElementVisible(By.CssSelector("[data-testid='invite-password-error']"), 15);
        var errorText = Driver.FindElement(By.CssSelector("[data-testid='invite-password-error']")).Text;
        Assert.Contains("initial_invite_password", errorText, StringComparison.OrdinalIgnoreCase);
    }
}

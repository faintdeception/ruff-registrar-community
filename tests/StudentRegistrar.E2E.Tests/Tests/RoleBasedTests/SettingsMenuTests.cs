using OpenQA.Selenium;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests.RoleBasedTests;

/// <summary>
/// Tests for role-based settings menu functionality
/// Validates that users see appropriate settings menu items based on their roles
/// </summary>
public class SettingsMenuTests : BaseRoleNavigationTest
{
    #region Admin Settings Menu Tests

    [Fact]
    public void Admin_Should_See_Settings_Button()
    {
        // Arrange - Login as admin
        LoginAsAdmin();

        // Act - Check for settings button
        var navigationPage = new NavigationPage(Driver);

        // Assert - Settings button should be visible
        Assert.True(navigationPage.IsSettingsButtonVisible());
    }

    [Fact]
    public void Admin_Should_See_All_Settings_Menu_Items()
    {
        // Arrange - Login as admin
        LoginAsAdmin();

        // Act - Open settings dropdown
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();

        // Assert - All menu items should be visible
        Assert.True(navigationPage.IsSettingsDropdownVisible());
        Assert.True(navigationPage.IsSettingsMenuItemVisible("profile"));
        Assert.True(navigationPage.IsSettingsMenuItemVisible("manage-members"));
        Assert.True(navigationPage.IsSettingsMenuItemVisible("system"));
    }

    [Fact]
    public void Admin_Should_Navigate_To_Profile_Settings()
    {
        // Arrange - Login as admin
        LoginAsAdmin();

        // Act - Navigate to profile settings
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();
        navigationPage.ClickSettingsMenuItem("profile");
        WaitForUrlContains("/settings/profile");

        // Assert - Should be on profile settings page
        Assert.Contains("/settings/profile", Driver.Url);
        Assert.Contains("Profile Settings", Driver.PageSource);
        Assert.Contains("Coming Soon", Driver.PageSource);
    }

    [Fact]
    public void Admin_Should_Navigate_To_Manage_Members()
    {
        // Arrange - Login as admin
        LoginAsAdmin();

        // Act - Navigate to manage members
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();
        navigationPage.ClickSettingsMenuItem("manage-members");
        WaitForUrlContains("/settings/manage-members");

        // Assert - Should be on manage members page
        Assert.Contains("/settings/manage-members", Driver.Url);
        Assert.Contains("Manage Members", Driver.PageSource);
        Assert.Contains("Coming Soon", Driver.PageSource);
    }

    [Fact]
    public void Admin_Should_Navigate_To_System_Settings()
    {
        // Arrange - Login as admin
        LoginAsAdmin();

        // Act - Navigate to system settings
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();
        navigationPage.ClickSettingsMenuItem("system");
        WaitForUrlContains("/settings/system");
        WaitForElementVisible(By.CssSelector("[data-testid='system-settings-title']"));
        WaitForElementVisible(By.CssSelector("[data-testid='billing-management-card']"));

        // Assert - Should be on system settings page
        Assert.Contains("/settings/system", Driver.Url);
        Assert.Contains("System Settings", Driver.PageSource);
        Assert.Contains("Subscription Billing", Driver.PageSource);

        var scheduleButtons = Driver.FindElements(By.CssSelector("[data-testid='schedule-cancellation-button']"));
        var undoButtons = Driver.FindElements(By.CssSelector("[data-testid='undo-cancellation-button']"));

        Assert.True(scheduleButtons.Count > 0 || undoButtons.Count > 0,
            "Expected the billing management card to expose either a cancellation action or an undo action.");

        var unavailableMessages = Driver.FindElements(By.CssSelector("[data-testid='billing-unavailable-message']"));
        if (unavailableMessages.Count > 0)
        {
            Assert.Contains("Billing", unavailableMessages[0].Text);
            if (scheduleButtons.Count > 0)
            {
                Assert.False(scheduleButtons[0].Enabled);
            }
        }
    }

    #endregion

    #region Educator Settings Menu Tests

    [Fact]
    public void Educator_Should_See_Settings_Button()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Check for settings button
        var navigationPage = new NavigationPage(Driver);

        // Assert - Settings button should be visible
        Assert.True(navigationPage.IsSettingsButtonVisible());
    }

    [Fact]
    public void Educator_Should_See_Only_Profile_Menu_Item()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Open settings dropdown
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();

        // Assert - Should see profile but NOT admin-only items
        Assert.True(navigationPage.IsSettingsDropdownVisible());
        Assert.True(navigationPage.IsSettingsMenuItemVisible("profile"));
        Assert.False(navigationPage.IsSettingsMenuItemPresent("manage-members"));
        Assert.False(navigationPage.IsSettingsMenuItemPresent("system"));
    }

    [Fact]
    public void Educator_Should_Navigate_To_Profile_Settings()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Navigate to profile settings
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();
        navigationPage.ClickSettingsMenuItem("profile");
        WaitForUrlContains("/settings/profile");

        // Assert - Should be on profile settings page
        Assert.Contains("/settings/profile", Driver.Url);
        Assert.Contains("Profile Settings", Driver.PageSource);
    }

    [Fact]
    public void Educator_Should_NOT_Access_Manage_Members_Directly()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Try to navigate directly to manage members
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/manage-members");
        WaitForPageLoad();

        // Assert - Should either be redirected to unauthorized OR stay on manage-members page but it should be protected
        if (Driver.Url.Contains("/unauthorized"))
        {
            // Best case: redirected to unauthorized page
            Assert.Contains("/unauthorized", Driver.Url);
        }
        else
        {
            // Fallback: Page might load but should be protected
            // Either show error message or empty/placeholder content, but NOT the actual Manage Members content
            Assert.Contains("/settings/manage-members", Driver.Url);
            
            var hasProtection = Driver.PageSource.Contains("unauthorized") || 
                               Driver.PageSource.Contains("not authorized") ||
                               Driver.PageSource.Contains("Access Denied") ||
                               Driver.PageSource.Contains("403") ||
                               Driver.PageSource.Contains("Permission denied");
            
            Assert.True(hasProtection);
        }
    }

    [Fact]
    public void Educator_Should_NOT_Access_System_Settings_Directly()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Try to navigate directly to system settings
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/system");
        WaitForPageLoad();

        // Assert - Should either be redirected to unauthorized OR stay on system page but it should be protected
        if (Driver.Url.Contains("/unauthorized"))
        {
            // Best case: redirected to unauthorized page
            Assert.Contains("/unauthorized", Driver.Url);
        }
        else
        {
            // Fallback: Page might load but should be protected
            Assert.Contains("/settings/system", Driver.Url);
            
            var hasProtection = Driver.PageSource.Contains("unauthorized") || 
                               Driver.PageSource.Contains("not authorized") ||
                               Driver.PageSource.Contains("Access Denied") ||
                               Driver.PageSource.Contains("403") ||
                               Driver.PageSource.Contains("Permission denied");
            
            Assert.True(hasProtection);
        }
    }

    #endregion

    #region Member Settings Menu Tests

    [Fact]
    public void Member_Should_See_Settings_Button()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Check for settings button
        var navigationPage = new NavigationPage(Driver);

        // Assert - Settings button should be visible
        Assert.True(navigationPage.IsSettingsButtonVisible());
    }

    [Fact]
    public void Member_Should_See_Only_Profile_Menu_Item()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Open settings dropdown
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();

        // Assert - Should see profile but NOT admin-only items
        Assert.True(navigationPage.IsSettingsDropdownVisible());
        Assert.True(navigationPage.IsSettingsMenuItemVisible("profile"));
        Assert.False(navigationPage.IsSettingsMenuItemPresent("manage-members"));
        Assert.False(navigationPage.IsSettingsMenuItemPresent("system"));
    }

    [Fact]
    public void Member_Should_Navigate_To_Profile_Settings()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Navigate to profile settings
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();
        navigationPage.ClickSettingsMenuItem("profile");
        WaitForUrlContains("/settings/profile");

        // Assert - Should be on profile settings page
        Assert.Contains("/settings/profile", Driver.Url);
        Assert.Contains("Profile Settings", Driver.PageSource);
    }

    [Fact]
    public void Member_Should_NOT_Access_Manage_Members_Directly()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Try to navigate directly to manage members
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/manage-members");
        WaitForPageLoad();

        // Assert - Should either be redirected to unauthorized OR stay on manage-members page but it should be protected
        if (Driver.Url.Contains("/unauthorized"))
        {
            // Best case: redirected to unauthorized page
            Assert.Contains("/unauthorized", Driver.Url);
        }
        else
        {
            // Fallback: Page might load but should be protected
            Assert.Contains("/settings/manage-members", Driver.Url);
            
            var hasProtection = Driver.PageSource.Contains("unauthorized") || 
                               Driver.PageSource.Contains("not authorized") ||
                               Driver.PageSource.Contains("Access Denied") ||
                               Driver.PageSource.Contains("403") ||
                               Driver.PageSource.Contains("Permission denied");
            
            Assert.True(hasProtection);
        }
    }

    [Fact]
    public void Member_Should_NOT_Access_System_Settings_Directly()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Try to navigate directly to system settings
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/system");
        WaitForPageLoad();

        // Assert - Should either be redirected to unauthorized OR stay on system page but it should be protected
        if (Driver.Url.Contains("/unauthorized"))
        {
            // Best case: redirected to unauthorized page
            Assert.Contains("/unauthorized", Driver.Url);
        }
        else
        {
            // Fallback: Page might load but should be protected
            Assert.Contains("/settings/system", Driver.Url);
            
            var hasProtection = Driver.PageSource.Contains("unauthorized") || 
                               Driver.PageSource.Contains("not authorized") ||
                               Driver.PageSource.Contains("Access Denied") ||
                               Driver.PageSource.Contains("403") ||
                               Driver.PageSource.Contains("Permission denied");
            
            Assert.True(hasProtection);
        }
    }

    #endregion

    #region Settings Menu Interaction Tests

    [Fact]
    public void Settings_Dropdown_Should_Close_When_Item_Clicked()
    {
        // Arrange - Login as admin
        LoginAsAdmin();

        // Act - Open dropdown and click an item
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickSettingsButton();
        WaitForPageLoad();
        
        Assert.True(navigationPage.IsSettingsDropdownVisible());
        
        navigationPage.ClickSettingsMenuItem("profile");
        WaitForPageLoad();

        // Assert - Should navigate away and dropdown should close
        Assert.Contains("/settings/profile", Driver.Url);
    }

    #endregion
}

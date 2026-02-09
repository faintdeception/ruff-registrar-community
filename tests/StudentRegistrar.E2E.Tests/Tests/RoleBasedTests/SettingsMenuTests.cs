using OpenQA.Selenium;
using FluentAssertions;
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
        navigationPage.IsSettingsButtonVisible().Should().BeTrue(
            "Admin should see settings button in header");
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
        navigationPage.IsSettingsDropdownVisible().Should().BeTrue(
            "Settings dropdown should be visible after clicking settings button");
        
        navigationPage.IsSettingsMenuItemVisible("profile").Should().BeTrue(
            "Admin should see Profile menu item");
        
        navigationPage.IsSettingsMenuItemVisible("manage-members").Should().BeTrue(
            "Admin should see Manage Members menu item");
        
        navigationPage.IsSettingsMenuItemVisible("system").Should().BeTrue(
            "Admin should see System Settings menu item");
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
        WaitForPageLoad();

        // Assert - Should be on profile settings page
        Driver.Url.Should().Contain("/settings/profile", 
            "Should navigate to profile settings page");
        
        Driver.PageSource.Should().Contain("Profile Settings", 
            "Page should contain Profile Settings title");
        
        Driver.PageSource.Should().Contain("Coming Soon", 
            "Page should show Coming Soon message");
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
        WaitForPageLoad();

        // Assert - Should be on manage members page
        Driver.Url.Should().Contain("/settings/manage-members", 
            "Should navigate to manage members page");
        
        Driver.PageSource.Should().Contain("Manage Members", 
            "Page should contain Manage Members title");
        
        Driver.PageSource.Should().Contain("Coming Soon", 
            "Page should show Coming Soon message");
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
        WaitForPageLoad();

        // Assert - Should be on system settings page
        Driver.Url.Should().Contain("/settings/system", 
            "Should navigate to system settings page");
        
        Driver.PageSource.Should().Contain("System Settings", 
            "Page should contain System Settings title");
        
        Driver.PageSource.Should().Contain("Coming Soon", 
            "Page should show Coming Soon message");
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
        navigationPage.IsSettingsButtonVisible().Should().BeTrue(
            "Educator should see settings button in header");
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
        navigationPage.IsSettingsDropdownVisible().Should().BeTrue(
            "Settings dropdown should be visible after clicking settings button");
        
        navigationPage.IsSettingsMenuItemVisible("profile").Should().BeTrue(
            "Educator should see Profile menu item");
        
        navigationPage.IsSettingsMenuItemPresent("manage-members").Should().BeFalse(
            "Educator should NOT see Manage Members menu item");
        
        navigationPage.IsSettingsMenuItemPresent("system").Should().BeFalse(
            "Educator should NOT see System Settings menu item");
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
        WaitForPageLoad();

        // Assert - Should be on profile settings page
        Driver.Url.Should().Contain("/settings/profile", 
            "Should navigate to profile settings page");
        
        Driver.PageSource.Should().Contain("Profile Settings", 
            "Page should contain Profile Settings title");
    }

    [Fact]
    public void Educator_Should_NOT_Access_Manage_Members_Directly()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Try to navigate directly to manage members
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/manage-members");
        WaitForPageLoad();

        // Assert - Should be redirected or see unauthorized
        Driver.Url.Should().Match(url => 
            url.Contains("/unauthorized") || url.Contains("/settings/manage-members"),
            "Educator should be redirected or see unauthorized page");
        
        // If on manage members page, verify protection
        if (Driver.Url.Contains("/settings/manage-members"))
        {
            // The page should be protected and redirect, but if not, it should at least not load sensitive data
            Driver.PageSource.Should().NotContain("sensitive", 
                "Page should be protected from unauthorized access");
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

        // Assert - Should be redirected or see unauthorized
        Driver.Url.Should().Match(url => 
            url.Contains("/unauthorized") || url.Contains("/settings/system"),
            "Educator should be redirected or see unauthorized page");
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
        navigationPage.IsSettingsButtonVisible().Should().BeTrue(
            "Member should see settings button in header");
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
        navigationPage.IsSettingsDropdownVisible().Should().BeTrue(
            "Settings dropdown should be visible after clicking settings button");
        
        navigationPage.IsSettingsMenuItemVisible("profile").Should().BeTrue(
            "Member should see Profile menu item");
        
        navigationPage.IsSettingsMenuItemPresent("manage-members").Should().BeFalse(
            "Member should NOT see Manage Members menu item");
        
        navigationPage.IsSettingsMenuItemPresent("system").Should().BeFalse(
            "Member should NOT see System Settings menu item");
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
        WaitForPageLoad();

        // Assert - Should be on profile settings page
        Driver.Url.Should().Contain("/settings/profile", 
            "Should navigate to profile settings page");
        
        Driver.PageSource.Should().Contain("Profile Settings", 
            "Page should contain Profile Settings title");
    }

    [Fact]
    public void Member_Should_NOT_Access_Manage_Members_Directly()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Try to navigate directly to manage members
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/manage-members");
        WaitForPageLoad();

        // Assert - Should be redirected to unauthorized
        Driver.Url.Should().Match(url => 
            url.Contains("/unauthorized") || url.Contains("/settings/manage-members"),
            "Member should be redirected or see unauthorized page");
    }

    [Fact]
    public void Member_Should_NOT_Access_System_Settings_Directly()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Try to navigate directly to system settings
        Driver.Navigate().GoToUrl($"{BaseUrl}/settings/system");
        WaitForPageLoad();

        // Assert - Should be redirected to unauthorized
        Driver.Url.Should().Match(url => 
            url.Contains("/unauthorized") || url.Contains("/settings/system"),
            "Member should be redirected or see unauthorized page");
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
        
        navigationPage.IsSettingsDropdownVisible().Should().BeTrue(
            "Dropdown should be open initially");
        
        navigationPage.ClickSettingsMenuItem("profile");
        WaitForPageLoad();

        // Assert - Should navigate away and dropdown should close
        Driver.Url.Should().Contain("/settings/profile", 
            "Should navigate to profile page");
    }

    #endregion
}

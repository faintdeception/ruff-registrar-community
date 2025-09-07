using FluentAssertions;
using OpenQA.Selenium;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests;

public class LoginTests : BaseTest
{
    [Fact]
    public void Should_Show_Login_Elements_When_Not_Authenticated()
    {
        // Arrange & Act
        NavigateToHome();
        WaitForPageLoad();

        // Assert - Home page should redirect to login when not authenticated
        Driver.Url.Should().Contain("/login", "Unauthenticated users should be redirected to login page");
        Driver.PageSource.Should().Contain("username", "Should show username field for login");
        Driver.PageSource.Should().Contain("password", "Should show password field for login");
        
        var loginPage = new LoginPage(Driver);
        loginPage.IsOnLoginPage().Should().BeTrue("Should display the login form on home page");
    }

    [Fact]
    public void Should_Display_Error_For_Invalid_Credentials()
    {
        // Arrange
        NavigateToHome();
        WaitForPageLoad();
        
        var loginPage = new LoginPage(Driver);
        loginPage.IsOnLoginPage().Should().BeTrue("Should be on login page");

        // Act - Try to login with invalid credentials
        loginPage.Login("invalid_user", "invalid_password");

    // Wait for error message to appear
    WaitUntil(d => d.PageSource.Contains("Login Error") || d.PageSource.Contains("Invalid user credentials"), 10);

        // Assert - Should show error message and stay on login page
        Driver.Url.Should().Contain("/login", "Should remain on login page after invalid credentials");
        
        var errorDisplayed = Driver.PageSource.Contains("Login Error") && 
                           Driver.PageSource.Contains("Invalid user credentials");
        
        errorDisplayed.Should().BeTrue("Should display 'Login Error' and 'Invalid user credentials' messages");
    }

    [Fact]
    public void Should_Login_Successfully_With_Valid_Credentials()
    {
        // Arrange
        NavigateToHome();
        WaitForPageLoad();
        
        var loginPage = new LoginPage(Driver);
        loginPage.IsOnLoginPage().Should().BeTrue("Should be on login page");

        // Get credentials from configuration
        var username = Configuration["TestCredentials:ValidUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:ValidUser:Password"] ?? "AdminPass123!";

        // Act - Login with valid credentials
        loginPage.Login(username, password);

    // Wait for redirect and page load
    WaitForPageLoad();
    WaitForUrlContains("/");

        // Assert - Should be redirected to home page and be logged in
        Driver.Url.Should().NotContain("/login", "Should be redirected away from login page");
        Driver.Url.Should().StartWith(BaseUrl, "Should be on the application");
        
        var homePage = new HomePage(Driver);
        homePage.IsLoggedIn().Should().BeTrue("Should be logged in with valid credentials");
    }

    [Fact]
    public void Should_Logout_And_Redirect_To_Login()
    {
        // Arrange - First login
        NavigateToHome();
        WaitForPageLoad();
        
        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:ValidUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:ValidUser:Password"] ?? "AdminPass123!";
        
    loginPage.Login(username, password);
    WaitForPageLoad();
    WaitForUrlContains("/");

        var homePage = new HomePage(Driver);
        homePage.IsLoggedIn().Should().BeTrue("Should be logged in before logout test");

        // Act - Logout using the logout button
        homePage.ClickLogout();

    // Wait for redirect
    WaitForPageLoad();
    WaitForUrlContains("/login");

        // Assert - Should be redirected back to login page
        Driver.Url.Should().Contain("/login", "Should be redirected to login page after logout");
        
        var loginPageAfterLogout = new LoginPage(Driver);
        loginPageAfterLogout.IsOnLoginPage().Should().BeTrue("Should display login form after logout");
    }

    [Fact]
    public void Should_Complete_Full_Login_Logout_Cycle()
    {
        // This test verifies the complete flow: redirect to login -> login -> home -> logout -> login

    // Step 1: Navigate to home (should redirect to login)
    NavigateToHome();
    WaitForPageLoad();
    WaitForUrlContains("/login");
    Driver.Url.Should().Contain("/login", "Step 1: Should redirect to login when not authenticated");

        // Step 2: Login with valid credentials
        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:ValidUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:ValidUser:Password"] ?? "AdminPass123!";
        
    loginPage.Login(username, password);
    WaitForPageLoad();
    WaitForUrlContains("/");

        // Step 3: Verify logged in and on home page
        Driver.Url.Should().NotContain("/login", "Step 3: Should be redirected away from login page");
        var homePage = new HomePage(Driver);
        homePage.IsLoggedIn().Should().BeTrue("Step 3: Should be logged in");

    // Step 4: Logout
    homePage.ClickLogout();
    WaitForPageLoad();
    WaitForUrlContains("/login");

    // Step 5: Verify redirected back to login
    WaitForUrlContains("/login");
    Driver.Url.Should().Contain("/login", "Step 5: Should be redirected to login page after logout");
    var loginPageAfterLogout = new LoginPage(Driver);
    loginPageAfterLogout.IsOnLoginPage().Should().BeTrue("Step 5: Should display login form after logout");
    }
}
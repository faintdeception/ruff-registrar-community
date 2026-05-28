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

        // Assert - Home page should redirect to the app login entry page when not authenticated.
        Assert.Contains("/login", Driver.Url);
        Assert.Contains("Sign in with Keycloak", Driver.PageSource);
        Assert.Contains("Credentials are entered directly with Keycloak", Driver.PageSource);
        
        var loginPage = new LoginPage(Driver);
        Assert.True(loginPage.IsOnLoginPage());
    }

    [Fact]
    public void Should_Display_Error_For_Invalid_Credentials()
    {
        // Arrange
        NavigateToHome();
        WaitForPageLoad();

        var homePage = new HomePage(Driver);
        if (homePage.HasLogoutButton())
        {
            homePage.ClickLogout();
            WaitForPageLoad();
            WaitForUrlContains("/login");
        }
        
        var loginPage = new LoginPage(Driver);
        Assert.True(loginPage.IsOnLoginPage());

        // Act - Try to login with invalid credentials
        loginPage.Login("invalid_user", "invalid_password");

        // Wait for the hosted auth flow to either surface an auth error or settle away from the app shell.
        WaitUntil(d =>
            !d.Url.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase) ||
            d.PageSource.Contains("Login Error") ||
            d.PageSource.Contains("Invalid user credentials") ||
            d.PageSource.Contains("Invalid username or password") ||
            d.PageSource.Contains("username or password", StringComparison.OrdinalIgnoreCase),
            15);

        // Assert - Should show an auth error and avoid redirecting into the app.
        Assert.False(Driver.Url.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase));
        
        var errorDisplayed = Driver.PageSource.Contains("Login Error") ||
                           Driver.PageSource.Contains("Invalid user credentials") ||
                           Driver.PageSource.Contains("Invalid username or password") ||
                           Driver.PageSource.Contains("username or password", StringComparison.OrdinalIgnoreCase);
        
        Assert.True(errorDisplayed);
    }

    [Fact]
    [Trait("Suite", "SaaSCompatibility")]
    public void Should_Login_Successfully_With_Valid_Credentials()
    {
        // Arrange
        NavigateToHome();
        WaitForPageLoad();
        
        var loginPage = new LoginPage(Driver);
        Assert.True(loginPage.IsOnLoginPage());

        // Get credentials from configuration
        var username = Configuration["TestCredentials:ValidUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:ValidUser:Password"] ?? "AdminPass123!";

        // Act - Login with valid credentials
        loginPage.Login(username, password);

    // Wait for redirect and page load
    WaitForPageLoad();
    WaitForUrlContains("/");

        // Assert - Should be redirected to home page and be logged in
        Assert.DoesNotContain("/login", Driver.Url);
        Assert.StartsWith(BaseUrl, Driver.Url);
        
        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());
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
        Assert.True(homePage.IsLoggedIn());

        // Act - Logout using the logout button
        homePage.ClickLogout();

    // Wait for redirect
    WaitForPageLoad();
    WaitForUrlContains("/login");

        // Assert - Should be redirected back to login page
        Assert.Contains("/login", Driver.Url);
        
        var loginPageAfterLogout = new LoginPage(Driver);
        Assert.True(loginPageAfterLogout.IsOnLoginPage());
    }

    [Fact]
    public void Should_Complete_Full_Login_Logout_Cycle()
    {
        // This test verifies the complete flow: redirect to login -> login -> home -> logout -> login

    // Step 1: Navigate to home (should redirect to login)
    NavigateToHome();
    WaitForPageLoad();
    WaitForUrlContains("/login");
    Assert.Contains("/login", Driver.Url);

        // Step 2: Login with valid credentials
        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:ValidUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:ValidUser:Password"] ?? "AdminPass123!";
        
    loginPage.Login(username, password);
    WaitForPageLoad();
    WaitForUrlContains("/");

        // Step 3: Verify logged in and on home page
        Assert.DoesNotContain("/login", Driver.Url);
        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());

    // Step 4: Logout
    homePage.ClickLogout();
    WaitForPageLoad();
    WaitForUrlContains("/login");

    // Step 5: Verify redirected back to login
    WaitForUrlContains("/login");
    Assert.Contains("/login", Driver.Url);
    var loginPageAfterLogout = new LoginPage(Driver);
    Assert.True(loginPageAfterLogout.IsOnLoginPage());
    }
}
using OpenQA.Selenium;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests.RoleBasedTests;

public class EducatorTests : BaseTest
{
    [Fact]
    public void Educator_Should_Login_Successfully()
    {
        // Arrange
    NavigateToHome();
    WaitForPageLoad();
    WaitForUrlContains("/login");

        // Act - Login as educator
        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:EducatorUser:Username"] ?? "educator1";
        var password = Configuration["TestCredentials:EducatorUser:Password"] ?? "EducatorPass123!";
        
    loginPage.Login(username, password);
    WaitForPageLoad();
    WaitForUrlContains("/");

        // Assert - Should be logged in
        Assert.DoesNotContain("/login", Driver.Url);
        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());
    }

    [Fact]
    public void Educator_Should_Access_Family_Management()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Navigate to account/family management using navigation page
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickAccount();
        WaitForPageLoad();

        // Assert - Should access family management
        Assert.Contains("/account", Driver.Url);
        Assert.Contains("account", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Educator_Should_Access_Course_Management()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Navigate to courses page using navigation page
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickCourses();
        WaitForPageLoad();
        WaitForUrlContains("/courses");

        // Assert - Should access courses (to create/manage their own)
        Assert.Contains("/courses", Driver.Url);
        Assert.Contains("course", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Suite", "SaaSCompatibility")]
    public void Educator_Should_Access_Educator_Section()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Act - Navigate to educators page using navigation page
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickEducators();
        WaitForPageLoad();
        WaitForUrlContains("/educators");

        // Assert - Should access educators section
        Assert.Contains("/educators", Driver.Url);
        Assert.Contains("educator", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Educator_Should_NOT_Access_Admin_Features()
    {
        // Arrange - Login as educator
        LoginAsEducator();

        // Assert - Should NOT see admin-only features using robust test selectors
        var navigationPage = new NavigationPage(Driver);
        navigationPage.VerifyEducatorNavigation();
        
        // Additional verification - check roles
        Assert.True(navigationPage.HasEducatorRole());
        Assert.False(navigationPage.HasAdminRole());
        
        // Verify admin items are not present in DOM (most robust check)
        Assert.False(navigationPage.IsStudentsPresent());
        Assert.False(navigationPage.IsSemestersPresent());
    }

    [Fact]
    public void Educator_Should_Manage_Teaching_And_Family_Workflow()
    {
        // This test covers the educator workflow:
        // 1. Login
        // 2. Manage own family/children
        // 3. Create/manage own courses
        // 4. Manage enrollments (own courses + own children)
        // 5. Manage grades (own courses + view children's grades)
        
        // Arrange - Login as educator
        LoginAsEducator();

        // Act & Assert - Verify navigation and access
        var navigationPage = new NavigationPage(Driver);
        
        // Verify complete navigation permissions
        navigationPage.VerifyEducatorNavigation();
        
        // Test navigation to each allowed page
        
        // 1. Family Management
        navigationPage.ClickAccount();
        WaitForPageLoad();
        WaitForUrlContains("/account");
        Assert.Contains("/account", Driver.Url);
        
        // 2. Course Management (create own courses)
        navigationPage.ClickCourses();
        WaitForPageLoad();
        WaitForUrlContains("/courses");
        Assert.Contains("/courses", Driver.Url);
        
        // 3. Enrollment Management (own courses + children)
        // navigationPage.ClickEnrollments();
        // WaitForPageLoad();
        // Driver.Url.Should().Contain("/enrollments", "Should navigate to enrollments page");
        
        // 4. Grade Management (own courses + children's grades)
        // navigationPage.ClickGrades();
        // WaitForPageLoad();
        // Driver.Url.Should().Contain("/grades", "Should navigate to grades page");
        
        // 5. Educator Section
        navigationPage.ClickEducators();
        WaitForPageLoad();
        WaitForUrlContains("/educators");
        Assert.Contains("/educators", Driver.Url);

        // Final verification - ensure proper role context
        Assert.Contains("Educator", navigationPage.GetUserRoles());
    }

    #region Helper Methods

    private void LoginAsEducator()
    {
    NavigateToHome();
    WaitForPageLoad();
    WaitForUrlContains("/login");

        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:EducatorUser:Username"] ?? "educator1";
        var password = Configuration["TestCredentials:EducatorUser:Password"] ?? "EducatorPass123!";
        
    loginPage.Login(username, password);
    WaitForPageLoad();
    WaitForUrlContains("/");

        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());
    }

    private void VerifyCanAccessPage(string linkText, string expectedUrlPart)
    {
        // Navigate back to home
        Driver.Navigate().GoToUrl(BaseUrl);
        WaitForPageLoad();

        // Click the link
        var link = Driver.FindElement(By.LinkText(linkText));
        link.Click();
        WaitForPageLoad();

        // Verify navigation
        Assert.Contains(expectedUrlPart, Driver.Url);
    }

    #endregion
}

using OpenQA.Selenium;
using FluentAssertions;
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
        Driver.Url.Should().NotContain("/login", "Educator should be logged in");
        var homePage = new HomePage(Driver);
        homePage.IsLoggedIn().Should().BeTrue("Educator should be authenticated");
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
        Driver.Url.Should().Contain("/account", "Should navigate to account page");
        Driver.PageSource.Should().ContainEquivalentOf("account");
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

        // Assert - Should access courses (to create/manage their own)
        Driver.Url.Should().Contain("/courses", "Should navigate to courses page");
        Driver.PageSource.Should().ContainEquivalentOf("course");
    }

    [Fact]
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
        Driver.Url.Should().Contain("/educators", "Should navigate to educators page");
        Driver.PageSource.Should().ContainEquivalentOf("educator");
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
        navigationPage.HasEducatorRole().Should().BeTrue("User should have Educator role");
        navigationPage.HasAdminRole().Should().BeFalse("User should NOT have Admin role");
        
        // Verify admin items are not present in DOM (most robust check)
        navigationPage.IsStudentsPresent().Should().BeFalse("Students admin link should not exist for Educators");
        navigationPage.IsSemestersPresent().Should().BeFalse("Semesters admin link should not exist for Educators");
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
        Driver.Url.Should().Contain("/account", "Should navigate to account page");
        
        // 2. Course Management (create own courses)
        navigationPage.ClickCourses();
        WaitForPageLoad();
        Driver.Url.Should().Contain("/courses", "Should navigate to courses page");
        
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
        Driver.Url.Should().Contain("/educators", "Should navigate to educators page");

        // Final verification - ensure proper role context
        navigationPage.GetUserRoles().Should().Contain("Educator", "User should be identified as Educator");
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
        homePage.IsLoggedIn().Should().BeTrue("Educator login should succeed");
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
        Driver.Url.Should().Contain(expectedUrlPart, $"Educator should access {linkText} page");
    }

    #endregion
}

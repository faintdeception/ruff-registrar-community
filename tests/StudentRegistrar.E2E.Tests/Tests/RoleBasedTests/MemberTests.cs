using OpenQA.Selenium;
using FluentAssertions;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests.RoleBasedTests;

public class MemberTests : BaseTest
{
    [Fact]
    public void Member_Should_Login_Successfully()
    {
        // Arrange
    NavigateToHome();
    WaitForPageLoad();
    WaitForUrlContains("/login");

        // Act - Login as basic member
        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:MemberUser:Username"] ?? "member1";
        var password = Configuration["TestCredentials:MemberUser:Password"] ?? "MemberPass123!";
        
    loginPage.Login(username, password);
    WaitForPageLoad();
    WaitForUrlContains("/");

        // Assert - Should be logged in
        Driver.Url.Should().NotContain("/login", "Member should be logged in");
        var homePage = new HomePage(Driver);
        homePage.IsLoggedIn().Should().BeTrue("Member should be authenticated");
    }

    [Fact]
    public void Member_Should_Access_Family_Management()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Navigate to account/family management using navigation page
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickAccount();
        WaitForPageLoad();

        // Assert - Should access family management
        Driver.Url.Should().Contain("/account", "Should navigate to account page");
        Driver.PageSource.Should().ContainEquivalentOf("account");
    }

    [Fact]
    public void Member_Should_View_Available_Courses()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act - Navigate to courses page (view only) using navigation page
        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickCourses();
        WaitForPageLoad();

        // Assert - Should view available courses
        Driver.Url.Should().Contain("/courses", "Should navigate to courses page");
        Driver.PageSource.Should().ContainEquivalentOf("course");
    }

    // [Fact]
    // public void Member_Should_Manage_Enrollments()
    // {
    //     // Arrange - Login as member
    //     LoginAsMember();

    //     // Act - Navigate to enrollments page
    //     var enrollmentsLink = Driver.FindElement(By.LinkText("Enrollments"));
    //     enrollmentsLink.Click();
    //     WaitForPageLoad();

    //     // Assert - Should manage enrollments for their children
    //     Driver.Url.Should().Contain("/enrollments", "Should navigate to enrollments page");
    //     Driver.PageSource.Should().ContainEquivalentOf("enrollment");
    // }

    // [Fact]
    // public void Member_Should_View_Grades()
    // {
    //     // Arrange - Login as member
    //     LoginAsMember();

    //     // Act - Navigate to grades page
    //     var gradesLink = Driver.FindElement(By.LinkText("Grades"));
    //     gradesLink.Click();
    //     WaitForPageLoad();

    //     // Assert - Should view their children's grades
    //     Driver.Url.Should().Contain("/grades", "Should navigate to grades page");
    //     Driver.PageSource.Should().ContainEquivalentOf("grade");
    // }

    [Fact]
    public void Member_Should_NOT_Access_Admin_Or_Educator_Features()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Assert - Should NOT see admin-only or educator-specific features using robust test selectors
        var navigationPage = new NavigationPage(Driver);
        
        // Verify admin items are not present in DOM (most robust check)
        navigationPage.IsStudentsPresent().Should().BeFalse("Students admin link should not exist for Members");
        navigationPage.IsSemestersPresent().Should().BeFalse("Semesters admin link should not exist for Members");
        
        // Additional verification - check roles
        navigationPage.HasAdminRole().Should().BeFalse("User should NOT have Admin role");
        navigationPage.HasEducatorRole().Should().BeFalse("User should NOT have Educator role");
        
        // Members should see Educators link (to contact/view) but not manage
        // This depends on your business rules - adjust as needed
    }

    [Fact]
    public void Member_Should_Complete_Family_Management_Workflow()
    {
        // This test covers the basic member workflow:
        // 1. Login
        // 2. Manage family/children
        // 3. Browse available courses
        // 4. Enroll children in courses
        // 5. View children's grades and progress
        
        // Arrange - Login as member
        LoginAsMember();

        // Act & Assert - Verify access to member functions using NavigationPage
        var navigationPage = new NavigationPage(Driver);
        
        // 1. Family Management
        navigationPage.ClickAccount();
        WaitForPageLoad();
        Driver.Url.Should().Contain("/account", "Should navigate to account page");
        
        // 2. Course Browsing
        navigationPage.ClickCourses();
        WaitForPageLoad();
        Driver.Url.Should().Contain("/courses", "Should navigate to courses page");
        
        // 3. Enrollment Management (for children)
        // navigationPage.ClickEnrollments();
        // WaitForPageLoad();
        // Driver.Url.Should().Contain("/enrollments", "Should navigate to enrollments page");
        
        // 4. Grade Viewing (children's grades)
        // navigationPage.ClickGrades();
        // WaitForPageLoad();
        // Driver.Url.Should().Contain("/grades", "Should navigate to grades page");
        
        // 5. Educator Contact/Viewing
        navigationPage.ClickEducators();
        WaitForPageLoad();
        Driver.Url.Should().Contain("/educators", "Should navigate to educators page");

        // Verify NO access to admin features using robust navigation page
        navigationPage.IsStudentsPresent().Should().BeFalse("Members should not see admin Students link");
        navigationPage.IsSemestersPresent().Should().BeFalse("Members should not see admin Semesters link");
    }

    [Fact]
    public void Member_Should_Have_Limited_Navigation_Options()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act & Assert - Verify navigation permissions using robust NavigationPage
        var navigationPage = new NavigationPage(Driver);

        // Should have access to member-appropriate features
        navigationPage.IsNavItemVisible("account").Should().BeTrue("Members should see Account link");
        navigationPage.IsNavItemVisible("courses").Should().BeTrue("Members should see Courses link");        
        navigationPage.IsNavItemVisible("educators").Should().BeTrue("Members should see Educators link");

        // Should NOT have access to admin-only features
        navigationPage.IsStudentsPresent().Should().BeFalse("Members should not see Students admin link");
        navigationPage.IsSemestersPresent().Should().BeFalse("Members should not see Semesters admin link");
        
        // Verify role context
        navigationPage.HasAdminRole().Should().BeFalse("Members should not have Admin role");
        navigationPage.HasEducatorRole().Should().BeFalse("Members should not have Educator role");
    }

    #region Helper Methods

    private void LoginAsMember()
    {
        NavigateToHome();
        WaitForPageLoad();
        Thread.Sleep(2000);

        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:MemberUser:Username"] ?? "member1";
        var password = Configuration["TestCredentials:MemberUser:Password"] ?? "MemberPass123!";
        
        loginPage.Login(username, password);
        WaitForPageLoad();
        Thread.Sleep(2000);

    var homePage = new HomePage(Driver);
    homePage.IsLoggedIn().Should().BeTrue("Member login should succeed");
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
        Driver.Url.Should().Contain(expectedUrlPart, $"Member should access {linkText} page");
    }

    #endregion
}

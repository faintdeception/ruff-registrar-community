using OpenQA.Selenium;
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
        Assert.DoesNotContain("/login", Driver.Url);
        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());
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
        WaitForUrlContains("/account");

        // Assert - Should access family management
        Assert.Contains("/account", Driver.Url);
        Assert.Contains("account", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
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
        WaitForUrlContains("/courses");

        // Assert - Should view available courses
        Assert.Contains("/courses", Driver.Url);
        Assert.Contains("course", Driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Member_Should_Add_Student_And_Enroll_In_Course()
    {
        var uniqueSuffix = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var semesterName = $"Member Signup Semester {uniqueSuffix}";
        var semesterCode = $"MS{uniqueSuffix[^6..]}";
        var courseName = $"Member Signup Course {uniqueSuffix}";

        LoginAsAdmin();

        var semestersPage = new SemestersPage(Driver);
        semestersPage.NavigateToSemesters();
        semestersPage.ClickCreateSemester();
        semestersPage.FillSemesterForm(
            name: semesterName,
            code: semesterCode,
            startDate: new DateTime(2029, 8, 20),
            endDate: new DateTime(2029, 12, 15),
            regStartDate: new DateTime(2029, 6, 15),
            regEndDate: new DateTime(2029, 8, 10),
            isActive: true);
        semestersPage.SaveSemester();
        WaitUntil(_ => semestersPage.IsSemesterVisible(semesterName), 15, 300, $"Semester '{semesterName}' was not visible after creation.");

        var coursesPage = new CoursesPage(Driver);
        coursesPage.NavigateToCourses(BaseUrl);
        coursesPage.SelectSemester(semesterName);
        coursesPage.CreateCourse(
            courseName,
            $"MSC{uniqueSuffix[^4..]}",
            "All Ages",
            12,
            45.00m,
            "MW 10:00",
            "Deterministic member enrollment course.");

        Logout();
        LoginAsMember();

        var studentFirstName = $"Mvp{uniqueSuffix}";
        var studentLastName = "Student";
        var studentFullName = $"{studentFirstName} {studentLastName}";

        var navigationPage = new NavigationPage(Driver);
        navigationPage.ClickAccount();
        WaitForPageLoad();
        WaitForUrlContains("/account");

        var accountHolderPage = new AccountHolderPage(Driver);
        accountHolderPage.AddStudent(studentFirstName, studentLastName, "4");
        Assert.True(accountHolderPage.IsStudentVisible(studentFirstName, studentLastName));

        navigationPage.ClickCourses();
        WaitForPageLoad();
        WaitForUrlContains("/courses");

        coursesPage.SelectSemester(semesterName);
        coursesPage.SignUpStudentForCourse(courseName, studentFullName);

        navigationPage.ClickAccount();
        WaitForPageLoad();
        WaitForUrlContains("/account");
        WaitUntil(_ => accountHolderPage.IsEnrollmentVisible(courseName), timeoutSeconds: 15, failureMessage: $"Enrollment for course '{courseName}' was not visible on the member account page");

        Assert.True(accountHolderPage.IsEnrollmentVisible(courseName));
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
        Assert.False(navigationPage.IsStudentsPresent());
        Assert.False(navigationPage.IsSemestersPresent());
        
        // Additional verification - check roles
        Assert.False(navigationPage.HasAdminRole());
        Assert.False(navigationPage.HasEducatorRole());
        
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
        WaitForUrlContains("/account");
        Assert.Contains("/account", Driver.Url);
        
        // 2. Course Browsing
        navigationPage.ClickCourses();
        WaitForPageLoad();
        WaitForUrlContains("/courses");
        Assert.Contains("/courses", Driver.Url);
        
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
        WaitForUrlContains("/educators");
        Assert.Contains("/educators", Driver.Url);

        // Verify NO access to admin features using robust navigation page
        Assert.False(navigationPage.IsStudentsPresent());
        Assert.False(navigationPage.IsSemestersPresent());
    }

    [Fact]
    public void Member_Should_Have_Limited_Navigation_Options()
    {
        // Arrange - Login as member
        LoginAsMember();

        // Act & Assert - Verify navigation permissions using robust NavigationPage
        var navigationPage = new NavigationPage(Driver);

        // Should have access to member-appropriate features
        Assert.True(navigationPage.IsNavItemVisible("account"));
        Assert.True(navigationPage.IsNavItemVisible("courses"));        
        Assert.True(navigationPage.IsNavItemVisible("educators"));

        // Should NOT have access to admin-only features
        Assert.False(navigationPage.IsStudentsPresent());
        Assert.False(navigationPage.IsSemestersPresent());
        
        // Verify role context
        Assert.False(navigationPage.HasAdminRole());
        Assert.False(navigationPage.HasEducatorRole());
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
    Assert.True(homePage.IsLoggedIn());
    }

    private void LoginAsAdmin()
    {
        NavigateToHome();
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:AdminUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:AdminUser:Password"] ?? "AdminPass123!";

        loginPage.Login(username, password);
        WaitForPageLoad();
        WaitForUrlContains("/");

        var homePage = new HomePage(Driver);
        Assert.True(homePage.IsLoggedIn());
    }

    private void Logout()
    {
        var homePage = new HomePage(Driver);
        Assert.True(homePage.HasLogoutButton());
        homePage.ClickLogout();
        WaitForPageLoad();
        WaitForUrlContains("/login");
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

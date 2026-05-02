using FluentAssertions;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests.Workflows;

public sealed class DashboardSetupWorkflowTests : BaseTest
{
    [Fact]
    [Trait("Suite", "SaaSCompatibility")]
    public void Existing_Admin_Should_Login_And_Complete_Basic_Setup()
    {
        LoginAsAdmin();

        var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var semesterName = $"Workflow Semester {suffix}";
        var semesterCode = $"WF{suffix[^6..]}";
        var roomOneName = $"Workflow Room A {suffix}";
        var roomTwoName = $"Workflow Room B {suffix}";
        var educatorOneFirstName = "Workflow";
        var educatorOneLastName = $"EducatorA{suffix[^6..]}";
        var educatorTwoFirstName = "Workflow";
        var educatorTwoLastName = $"EducatorB{suffix[^6..]}";

        var semestersPage = new SemestersPage(Driver);
        semestersPage.NavigateToSemesters();
        semestersPage.ClickCreateSemester();
        semestersPage.FillSemesterForm(
            name: semesterName,
            code: semesterCode,
            startDate: new DateTime(2027, 9, 1),
            endDate: new DateTime(2027, 12, 20),
            regStartDate: new DateTime(2027, 7, 15),
            regEndDate: new DateTime(2027, 8, 25),
            isActive: true);
        semestersPage.SaveSemester();
        WaitUntil(_ => semestersPage.IsSemesterVisible(semesterName), 15, 300, $"Semester '{semesterName}' was not visible after creation.");

        var roomsPage = new RoomsPage(Driver);
        roomsPage.NavigateToRooms(BaseUrl);
        roomsPage.CreateRoom(roomOneName, "Classroom", "20", "Primary workflow classroom");
        roomsPage.CreateRoom(roomTwoName, "Lab", "15", "Workflow lab space");
        roomsPage.IsRoomVisible(roomOneName).Should().BeTrue("first room should be visible after creation");
        roomsPage.IsRoomVisible(roomTwoName).Should().BeTrue("second room should be visible after creation");

        var educatorsPage = new EducatorsPage(Driver);
        educatorsPage.NavigateToEducators(BaseUrl);
        educatorsPage.CreateEducator(
            educatorOneFirstName,
            educatorOneLastName,
            $"workflow-educator-a-{suffix}@example.com",
            "555-0101",
            "STEM",
            "Primary workflow educator.");
        educatorsPage.CreateEducator(
            educatorTwoFirstName,
            educatorTwoLastName,
            $"workflow-educator-b-{suffix}@example.com",
            "555-0102",
            "Humanities",
            "Secondary workflow educator.");

        educatorsPage.IsEducatorVisible($"{educatorOneFirstName} {educatorOneLastName}").Should().BeTrue("first educator should be visible after creation");
        educatorsPage.IsEducatorVisible($"{educatorTwoFirstName} {educatorTwoLastName}").Should().BeTrue("second educator should be visible after creation");
    }

    [Fact]
    [Trait("Suite", "SaaSCompatibility")]
    public void Existing_Admin_Should_Invite_Educator_Who_Can_Create_Priced_Course()
    {
        LoginAsAdmin();

        var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var semesterName = $"Educator Course Semester {suffix}";
        var semesterCode = $"EC{suffix[^6..]}";
        var courseName = $"Educator Priced Course {suffix}";

        var semestersPage = new SemestersPage(Driver);
        semestersPage.NavigateToSemesters();
        semestersPage.ClickCreateSemester();
        semestersPage.FillSemesterForm(
            name: semesterName,
            code: semesterCode,
            startDate: new DateTime(2028, 1, 10),
            endDate: new DateTime(2028, 5, 20),
            regStartDate: new DateTime(2027, 11, 15),
            regEndDate: new DateTime(2028, 1, 5),
            isActive: true);
        semestersPage.SaveSemester();
        WaitUntil(_ => semestersPage.IsSemesterVisible(semesterName), 15, 300, $"Semester '{semesterName}' was not visible after creation.");

        var educatorsPage = new EducatorsPage(Driver);
        educatorsPage.NavigateToEducators(BaseUrl);
        var credentials = educatorsPage.InviteEducator(
            "Invited",
            $"Educator{suffix[^6..]}",
            $"invited-educator-{suffix}@example.com",
            "555-0199",
            "Arts",
            "Invited educator workflow user.");

        credentials.Username.Should().Contain("invited-educator");
        credentials.TemporaryPassword.Should().NotBeNullOrWhiteSpace();

        Logout();
        Login(credentials.Username, credentials.TemporaryPassword, "educator login should succeed after invitation");

        var coursesPage = new CoursesPage(Driver);
        coursesPage.NavigateToCourses(BaseUrl);
        coursesPage.SelectSemester(semesterName);
        coursesPage.CreateCourse(
            courseName,
            $"EPC{suffix[^4..]}",
            "All Ages",
            12,
            125.50m,
            "MW 10:00",
            "Priced course created by an invited educator.");

        coursesPage.IsCourseVisible(courseName).Should().BeTrue("the invited educator should be able to create a course");
        coursesPage.IsCourseFeeVisible(courseName, "$125.50").Should().BeTrue("the invited educator should be able to set a course price");
    }

    [Fact]
    [Trait("Suite", "SaaSCompatibility")]
    public void Existing_Admin_Should_Authorize_Parent_As_Educator_Who_Can_Create_Priced_Course()
    {
        EnsureParentEducatorMemberAccountExists();

        LoginAsAdmin();

        var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var semesterName = $"Parent Educator Semester {suffix}";
        var semesterCode = $"PE{suffix[^6..]}";
        var courseName = $"Parent Educator Course {suffix}";

        var semestersPage = new SemestersPage(Driver);
        semestersPage.NavigateToSemesters();
        semestersPage.ClickCreateSemester();
        semestersPage.FillSemesterForm(
            name: semesterName,
            code: semesterCode,
            startDate: new DateTime(2028, 8, 20),
            endDate: new DateTime(2028, 12, 15),
            regStartDate: new DateTime(2028, 6, 15),
            regEndDate: new DateTime(2028, 8, 10),
            isActive: true);
        semestersPage.SaveSemester();
        WaitUntil(_ => semestersPage.IsSemesterVisible(semesterName), 15, 300, $"Semester '{semesterName}' was not visible after creation.");

        var educatorsPage = new EducatorsPage(Driver);
        educatorsPage.NavigateToEducators(BaseUrl);
        var parentOptionText = Configuration["TestCredentials:ParentEducatorUser:OptionText"] ?? "Sarah Johnson (sarah.johnson@example.com)";
        var parentFullName = Configuration["TestCredentials:ParentEducatorUser:FullName"] ?? "Sarah Johnson";
        educatorsPage.AuthorizeExistingMemberAsEducator(
            parentOptionText,
            parentFullName,
            "Parent Educators",
            "Existing parent authorized as an educator.");

        educatorsPage.GetInviteMessage().Should().Contain("authorized");
        educatorsPage.HasTemporaryCredentials().Should().BeFalse("existing parents should keep their current credentials");
        educatorsPage.IsEducatorVisible(parentFullName).Should().BeTrue("the selected parent should appear in the educators list");

        Logout();
        LoginAsParentEducator("parent should keep the same member credentials after educator authorization");

        var coursesPage = new CoursesPage(Driver);
        coursesPage.NavigateToCourses(BaseUrl);
        coursesPage.SelectSemester(semesterName);
        coursesPage.CreateCourse(
            courseName,
            $"PEC{suffix[^4..]}",
            "All Ages",
            10,
            95.25m,
            "TR 09:00",
            "Priced course created by an existing parent educator.");

        coursesPage.IsCourseVisible(courseName).Should().BeTrue("the authorized parent should be able to create a course");
        coursesPage.IsCourseFeeVisible(courseName, "$95.25").Should().BeTrue("the authorized parent should be able to set a course price");
    }

    [Fact]
    [Trait("Suite", "SaaSCompatibility")]
    public void Existing_Member_Should_Add_Child_And_Sign_Up_For_Paid_Course()
    {
        LoginAsAdmin();

        var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var semesterName = $"Parent Signup Semester {suffix}";
        var semesterCode = $"PS{suffix[^6..]}";
        var courseName = $"Parent Signup Course {suffix}";
        var studentFirstName = "Signup";
        var studentLastName = $"Child{suffix[^6..]}";
        var studentFullName = $"{studentFirstName} {studentLastName}";

        var semestersPage = new SemestersPage(Driver);
        semestersPage.NavigateToSemesters();
        semestersPage.ClickCreateSemester();
        semestersPage.FillSemesterForm(
            name: semesterName,
            code: semesterCode,
            startDate: new DateTime(2029, 1, 10),
            endDate: new DateTime(2029, 5, 20),
            regStartDate: new DateTime(2028, 11, 15),
            regEndDate: new DateTime(2029, 1, 5),
            isActive: true);
        semestersPage.SaveSemester();
        WaitUntil(_ => semestersPage.IsSemesterVisible(semesterName), 15, 300, $"Semester '{semesterName}' was not visible after creation.");

        var coursesPage = new CoursesPage(Driver);
        coursesPage.NavigateToCourses(BaseUrl);
        coursesPage.SelectSemester(semesterName);
        coursesPage.CreateCourse(
            courseName,
            $"PSC{suffix[^4..]}",
            "All Ages",
            12,
            88.75m,
            "MW 13:00",
            "Paid course used to verify parent signup and payment recording.");
        coursesPage.IsCourseFeeVisible(courseName, "$88.75").Should().BeTrue("the course price should be visible before member signup");

        Logout();
        LoginAsMember("member should be able to sign in before adding a child and signing up for a course");

        var accountPage = new AccountHolderPage(Driver);
        accountPage.NavigateToAccount(BaseUrl);
        accountPage.AddStudent(studentFirstName, studentLastName, "4");
        accountPage.IsStudentVisible(studentFirstName, studentLastName).Should().BeTrue("the new child should appear on the member account");

        coursesPage.NavigateToCourses(BaseUrl);
        coursesPage.SelectSemester(semesterName);
        coursesPage.GetSignupButtonText(courseName).Should().Be("Pay & Sign Up", "paid courses should prompt parents to pay while signing up");
        coursesPage.SignUpStudentForCourse(courseName, studentFullName);
        coursesPage.GetSuccessMessage().Should().Contain("payment was recorded", "paid course signup should confirm payment recording");
        coursesPage.GetSignupButtonText(courseName).Should().Be("Signed Up", "the enrolled student's course card should reflect the completed signup");

        accountPage.NavigateToAccount(BaseUrl);
        WaitUntil(_ => accountPage.IsEnrollmentVisible(courseName), 15, 300, "The signed-up course did not appear on the member account.");
        accountPage.IsEnrollmentVisible(courseName).Should().BeTrue("the new enrollment should be reflected on the member account page");
    }

    private void LoginAsAdmin()
    {
        NavigateToUrl($"{BaseUrl.TrimEnd('/')}/login");
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var username = Configuration["TestCredentials:AdminUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:AdminUser:Password"] ?? "AdminPass123!";

        Login(username, password, "admin login should succeed before dashboard setup");
    }

    private void LoginAsMember(string because)
    {
        NavigateToUrl($"{BaseUrl.TrimEnd('/')}/login");
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var username = Configuration["TestCredentials:MemberUser:Username"] ?? "member1";
        var password = Configuration["TestCredentials:MemberUser:Password"] ?? "MemberPass123!";

        Login(username, password, because);
    }

    private void LoginAsParentEducator(string because)
    {
        NavigateToUrl($"{BaseUrl.TrimEnd('/')}/login");
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var username = Configuration["TestCredentials:ParentEducatorUser:Username"] ?? "parenteducator1";
        var password = Configuration["TestCredentials:ParentEducatorUser:Password"] ?? "ParentEducatorPass123!";

        Login(username, password, because);
    }

    private void EnsureParentEducatorMemberAccountExists()
    {
        LoginAsParentEducator("parent educator test user should be able to sign in before authorization");
        NavigateToUrl($"{BaseUrl.TrimEnd('/')}/account-holder");
        WaitUntil(d => d.PageSource.Contains("Sarah Johnson") || d.PageSource.Contains("No account holder data found"), 15, 300,
            "parent educator account holder page did not finish loading");
        Driver.PageSource.Should().Contain("Sarah Johnson", "the parent educator workflow requires an existing member account holder");
        Logout();
    }

    private void Login(string username, string password, string because)
    {
        NavigateToUrl($"{BaseUrl.TrimEnd('/')}/login");
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var loginPage = new LoginPage(Driver);
        loginPage.Login(username, password);
        WaitForPageLoad();
        WaitForUrlContains("/");

        var homePage = new HomePage(Driver);
        homePage.IsLoggedIn().Should().BeTrue(because);
    }

    private void Logout()
    {
        ((OpenQA.Selenium.IJavaScriptExecutor)Driver).ExecuteScript("window.localStorage.clear(); window.sessionStorage.clear();");
    }
}

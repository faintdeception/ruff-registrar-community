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

    private void LoginAsAdmin()
    {
        NavigateToUrl($"{BaseUrl.TrimEnd('/')}/login");
        WaitForPageLoad();
        WaitForUrlContains("/login");

        var loginPage = new LoginPage(Driver);
        var username = Configuration["TestCredentials:AdminUser:Username"] ?? "admin1";
        var password = Configuration["TestCredentials:AdminUser:Password"] ?? "AdminPass123!";

        loginPage.Login(username, password);
        WaitForPageLoad();
        WaitForUrlContains("/");

        var homePage = new HomePage(Driver);
        homePage.IsLoggedIn().Should().BeTrue("admin login should succeed before dashboard setup");
    }
}

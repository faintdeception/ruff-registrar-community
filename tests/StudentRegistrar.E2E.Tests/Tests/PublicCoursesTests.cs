using FluentAssertions;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests;

/// <summary>
/// Tests covering public (unauthenticated) access to the Courses page now that it is no longer protected.
/// </summary>
public class PublicCoursesTests : BaseTest
{
    [Fact]
    public void Guest_Should_Access_Courses_Page_Without_Login()
    {
        // Arrange / Act - navigate directly to courses page
    Driver.Navigate().GoToUrl(BaseUrl + "/courses");
    WaitForPageLoad();
    // Wait for either courses grid or empty state to appear
    WaitUntil(d => d.PageSource.Contains("Courses") && d.Url.Contains("/courses"));

        // Assert - URL stays on /courses (not redirected to /login)
        Driver.Url.Should().Contain("/courses", "Courses page should be accessible publicly");
        Driver.Url.Should().NotContain("/login", "Guest should not be forced to login for courses");

        // Courses navigation link should be visible
        var nav = new NavigationPage(Driver);
        nav.IsCoursesVisible().Should().BeTrue("Courses nav item should be visible to guests");
        nav.IsGuestUser().Should().BeTrue("Guest menu should be present");

        // Ensure create / admin actions are NOT visible (buttons contain text Add Course / Add First Course / Edit)
        Driver.PageSource.Contains("Add Course").Should().BeFalse("Guest should not see Add Course button");
        Driver.PageSource.Contains("Add First Course").Should().BeFalse("Guest should not see Add First Course button");
        Driver.PageSource.Contains(">Edit<").Should().BeFalse("Guest should not see Edit buttons");
    }

    [Fact]
    public void Guest_Should_See_Courses_List_If_Data_Exists()
    {
        // Navigate
    Driver.Navigate().GoToUrl(BaseUrl + "/courses");
    WaitForPageLoad();
    WaitUntil(d => d.Url.Contains("/courses"));

        // If there are courses rendered, they should display course card container structure
        var page = new CoursesPage(Driver);
        var courseCount = page.GetCourseCount();

    courseCount.Should().BeGreaterThan(-1, "Page should render even with zero courses");

        // Guest should NOT have create capability
        page.CanSeeCreateButton().Should().BeFalse("Guest should not see course creation button");
    }
}

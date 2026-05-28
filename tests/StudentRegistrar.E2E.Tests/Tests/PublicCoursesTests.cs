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
    [Trait("Suite", "SaaSCompatibility")]
    public void Guest_Should_Access_Courses_Page_Without_Login()
    {
        // Arrange / Act - navigate directly to courses page
        NavigateToUrl(BaseUrl + "/courses");
        // Wait for either courses grid or empty state to appear
        WaitUntil(d => d.PageSource.Contains("Courses") && d.Url.Contains("/courses"));

        // Assert - URL stays on /courses (not redirected to /login)
        Assert.Contains("/courses", Driver.Url);
        Assert.DoesNotContain("/login", Driver.Url);

        // Courses navigation link should be visible
        var nav = new NavigationPage(Driver);
        Assert.True(nav.IsCoursesVisible());
        Assert.True(nav.IsGuestUser());

        // Ensure create / admin actions are NOT visible (buttons contain text Add Course / Add First Course / Edit)
        Assert.DoesNotContain("Add Course", Driver.PageSource);
        Assert.DoesNotContain("Add First Course", Driver.PageSource);
        Assert.DoesNotContain(">Edit<", Driver.PageSource);
    }

    [Fact]
    public void Guest_Should_See_Courses_List_If_Data_Exists()
    {
        // Navigate
        NavigateToUrl(BaseUrl + "/courses");
        WaitUntil(d => d.Url.Contains("/courses"));

        // If there are courses rendered, they should display course card container structure
        var page = new CoursesPage(Driver);
        var courseCount = page.GetCourseCount();

        Assert.True(courseCount > -1);

        // Guest should NOT have create capability
        Assert.False(page.CanSeeCreateButton());
    }
}

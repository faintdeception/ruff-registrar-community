using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using FluentAssertions;

namespace StudentRegistrar.E2E.Tests.Pages;

public class NavigationPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public NavigationPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    // Navigation elements
    private IWebElement MainNavigation => _driver.FindElement(By.CssSelector("[data-testid='main-navigation']"));
    private IWebElement UserMenu => _driver.FindElement(By.CssSelector("[data-testid='user-menu']"));
    private IWebElement UserInfo => _driver.FindElement(By.CssSelector("[data-testid='user-info']"));
    private IWebElement UserName => _driver.FindElement(By.CssSelector("[data-testid='user-name']"));
    private IWebElement UserRoles => _driver.FindElement(By.CssSelector("[data-testid='user-roles']"));
    private IWebElement LogoutButton => _driver.FindElement(By.CssSelector("[data-testid='logout-button']"));

    // Navigation methods
    public void ClickNavItem(string navItem)
    {
        var navLink = _driver.FindElement(By.CssSelector($"[data-testid='nav-{navItem}']"));
        navLink.Click();
    }

    public void ClickAccount() => ClickNavItem("account");
    public void ClickStudents() => ClickNavItem("students");
    public void ClickCourses() => ClickNavItem("courses");
    public void ClickSemesters() => ClickNavItem("semesters");
    // public void ClickEnrollments() => ClickNavItem("enrollments");
    // public void ClickGrades() => ClickNavItem("grades");
    public void ClickEducators() => ClickNavItem("educators");

    public void Logout()
    {
        LogoutButton.Click();
    }

    // Verification methods
    public bool IsNavItemVisible(string navItem)
    {
        try
        {
            var navLink = _driver.FindElement(By.CssSelector($"[data-testid='nav-{navItem}']"));
            return navLink.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool IsNavItemPresent(string navItem)
    {
        try
        {
            _driver.FindElement(By.CssSelector($"[data-testid='nav-{navItem}']"));
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool IsAccountVisible() => IsNavItemVisible("account");
    public bool IsStudentsVisible() => IsNavItemVisible("students");
    public bool IsCoursesVisible() => IsNavItemVisible("courses");
    public bool IsSemestersVisible() => IsNavItemVisible("semesters");
    public bool IsEducatorsVisible() => IsNavItemVisible("educators");

    public bool IsStudentsPresent() => IsNavItemPresent("students");
    public bool IsSemestersPresent() => IsNavItemPresent("semesters");

    public List<string> GetVisibleNavItems()
    {
        var navItems = _driver.FindElements(By.CssSelector("[data-nav-item]"));
        return navItems
            .Where(item => item.Displayed)
            .Select(item => item.GetDomAttribute("data-nav-item"))
            .ToList();
    }

    public List<string> GetAdminOnlyNavItems()
    {
        var adminItems = _driver.FindElements(By.CssSelector("[data-admin-only='true']"));
        return adminItems
            .Where(item => item.Displayed)
            .Select(item => item.GetDomAttribute("data-nav-item"))
            .ToList();
    }

    public string GetUserName()
    {
        try
        {
            return UserName.Text.Trim();
        }
        catch (NoSuchElementException)
        {
            return "";
        }
    }

    public string GetUserRoles()
    {
        try
        {
            return UserRoles.Text.Trim();
        }
        catch (NoSuchElementException)
        {
            return "";
        }
    }

    public bool IsUserLoggedIn()
    {
        try
        {
            return UserMenu.Displayed && !string.IsNullOrEmpty(GetUserName());
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool HasAdminRole()
    {
        var roles = GetUserRoles();
        return roles.Contains("Administrator", StringComparison.OrdinalIgnoreCase);
    }

    public bool HasEducatorRole()
    {
        var roles = GetUserRoles();
        return roles.Contains("Educator", StringComparison.OrdinalIgnoreCase);
    }

    // Role-based verification methods
    public void VerifyAdminNavigation()
    {
        // Admin should see all nav items
        IsAccountVisible().Should().BeTrue("Admin should see Account nav");
        IsStudentsVisible().Should().BeTrue("Admin should see Students nav");
        IsCoursesVisible().Should().BeTrue("Admin should see Courses nav");
        IsSemestersVisible().Should().BeTrue("Admin should see Semesters nav");
        IsEducatorsVisible().Should().BeTrue("Admin should see Educators nav");
    }

    public void VerifyEducatorNavigation()
    {
        // Educator should see most nav items but NOT admin-only ones
        IsAccountVisible().Should().BeTrue("Educator should see Account nav");
        IsStudentsVisible().Should().BeFalse("Educator should NOT see Students nav");
        IsCoursesVisible().Should().BeTrue("Educator should see Courses nav");
        IsSemestersVisible().Should().BeFalse("Educator should NOT see Semesters nav");
        IsEducatorsVisible().Should().BeTrue("Educator should see Educators nav");
        
        // Double check - admin items should not be present in DOM
        IsStudentsPresent().Should().BeFalse("Students nav should not be rendered for Educator");
        IsSemestersPresent().Should().BeFalse("Semesters nav should not be rendered for Educator");
    }

    public void VerifyStudentNavigation()
    {
        // Student should see limited nav items
        IsAccountVisible().Should().BeTrue("Student should see Account nav");
        IsStudentsVisible().Should().BeFalse("Student should NOT see Students nav");
        IsCoursesVisible().Should().BeTrue("Student should see Courses nav");
        IsSemestersVisible().Should().BeFalse("Student should NOT see Semesters nav");
        IsEducatorsVisible().Should().BeTrue("Student should see Educators nav");
    }
}

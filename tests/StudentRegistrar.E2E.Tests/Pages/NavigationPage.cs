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

    // Navigation elements (tolerant of unauthenticated state)
    private IWebElement MainNavigation => _driver.FindElement(By.CssSelector("[data-testid='main-navigation']"));
    private IWebElement? TryFind(By by)
    {
        try { return _driver.FindElement(by); } catch (NoSuchElementException) { return null; }
    }
    private IWebElement? UserMenu => TryFind(By.CssSelector("[data-testid='user-menu']"));
    private IWebElement? GuestMenu => TryFind(By.CssSelector("[data-testid='guest-menu']"));
    private IWebElement? UserInfo => TryFind(By.CssSelector("[data-testid='user-info']"));
    private IWebElement? UserName => TryFind(By.CssSelector("[data-testid='user-name']"));
    private IWebElement? UserRoles => TryFind(By.CssSelector("[data-testid='user-roles']"));
    private IWebElement? LogoutButton => TryFind(By.CssSelector("[data-testid='logout-button']"));
    private IWebElement? SettingsButton => TryFind(By.CssSelector("[data-testid='settings-button']"));
    private IWebElement? SettingsDropdown => TryFind(By.CssSelector("[data-testid='settings-dropdown']"));

    // Navigation methods
    public void ClickNavItem(string navItem)
    {
        var selector = By.CssSelector($"[data-testid='nav-{navItem}']");
        _wait.Until(d =>
        {
            var el = d.FindElement(selector);
            return el.Displayed && el.Enabled;
        });

        var navLink = _driver.FindElement(selector);
        try
        {
            navLink.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", navLink);
        }
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
        LogoutButton?.Click();
    }

    // Settings menu methods
    public void ClickSettingsButton()
    {
        _wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("[data-testid='settings-button']"));
            return el.Displayed && el.Enabled;
        });
        
        var settingsBtn = _driver.FindElement(By.CssSelector("[data-testid='settings-button']"));
        try
        {
            settingsBtn.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", settingsBtn);
        }
    }

    public bool IsSettingsButtonVisible()
    {
        try
        {
            var settingsBtn = _driver.FindElement(By.CssSelector("[data-testid='settings-button']"));
            return settingsBtn.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool IsSettingsDropdownVisible()
    {
        try
        {
            var dropdown = _driver.FindElement(By.CssSelector("[data-testid='settings-dropdown']"));
            return dropdown.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public void ClickSettingsMenuItem(string menuItem)
    {
        var selector = By.CssSelector($"[data-testid='settings-{menuItem}']");
        _wait.Until(d =>
        {
            var el = d.FindElement(selector);
            return el.Displayed && el.Enabled;
        });

        var menuItemElement = _driver.FindElement(selector);
        try
        {
            menuItemElement.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", menuItemElement);
        }
    }

    public bool IsSettingsMenuItemVisible(string menuItem)
    {
        try
        {
            var item = _driver.FindElement(By.CssSelector($"[data-testid='settings-{menuItem}']"));
            return item.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool IsSettingsMenuItemPresent(string menuItem)
    {
        try
        {
            _driver.FindElement(By.CssSelector($"[data-testid='settings-{menuItem}']"));
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
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

    public string GetUserName() => UserName?.Text.Trim() ?? string.Empty;

    public string GetUserRoles() => UserRoles?.Text.Trim() ?? string.Empty;

    public bool IsUserLoggedIn() => UserMenu != null && UserMenu.Displayed && !string.IsNullOrEmpty(GetUserName());

    public bool IsGuestUser() => GuestMenu != null && GuestMenu.Displayed && !IsUserLoggedIn();

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

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public class HomePage
{
    private readonly IWebDriver _driver;

    public HomePage(IWebDriver driver)
    {
        _driver = driver;
    }

    // Page elements
    public IWebElement PageTitle => _driver.FindElement(By.TagName("h1"));
    public IWebElement NavigationBar => _driver.FindElement(By.TagName("nav"));
    public IWebElement LogoutButton => _driver.FindElement(By.CssSelector("[data-testid='logout-button'], #logout-button"));

    // Page actions
    public void ClickLogout()
    {
        LogoutButton.Click();
    }

    // Page validations
    public bool IsLoaded()
    {
        try
        {
            return PageTitle.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public string GetPageTitle()
    {
        try
        {
            return PageTitle.Text;
        }
        catch (NoSuchElementException)
        {
            return string.Empty;
        }
    }

    public bool IsLoggedIn()
    {
        try
        {
            // Check for the shared logout control to determine if the authenticated shell is active.
            return LogoutButton.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool HasLogoutButton()
    {
        try
        {
            return LogoutButton.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }
}

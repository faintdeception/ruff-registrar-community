using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public sealed class TeachingPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public TeachingPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void WaitForPageLoad()
    {
        _wait.Until(d => d.Url.Contains("/teaching", StringComparison.OrdinalIgnoreCase));
        _wait.Until(d =>
            d.FindElements(By.CssSelector("[data-testid='teaching-page-title']")).Any() ||
            d.FindElements(By.CssSelector("[data-testid='teaching-empty-state']")).Any() ||
            d.FindElements(By.CssSelector("[data-testid='teaching-roster-list']")).Any());
    }

    public bool HasEmptyState()
    {
        return _driver.FindElements(By.CssSelector("[data-testid='teaching-empty-state']")).Any(e => e.Displayed);
    }

    public bool HasTitle()
    {
        return _driver.FindElements(By.CssSelector("[data-testid='teaching-page-title']")).Any(e => e.Displayed);
    }

    public bool HasRosterContent()
    {
        return _driver.FindElements(By.CssSelector("[data-testid='teaching-roster-list'] [data-testid^='teaching-roster-entry-']")).Any(e => e.Displayed);
    }
}
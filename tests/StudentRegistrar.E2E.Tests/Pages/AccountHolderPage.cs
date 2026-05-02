using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public class AccountHolderPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public AccountHolderPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        _wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    public void NavigateToAccount(string baseUrl)
    {
        _driver.Navigate().GoToUrl($"{baseUrl.TrimEnd('/')}/account-holder");
        _wait.Until(d => d.Url.Contains("/account-holder", StringComparison.OrdinalIgnoreCase));
    }

    public void AddStudent(string firstName, string lastName, string grade)
    {
        SafeClick(FindFirstVisible(
            By.CssSelector("[data-testid='add-student-button']"),
            By.XPath("//button[contains(normalize-space(.), 'Add Student')]")));
        _wait.Until(d => FindElements(d, By.CssSelector("[data-testid='student-first-name-input']"), By.XPath("//input[@required and @type='text']")).Any(e => e.Displayed));

        Fill(FindFirstVisible(By.CssSelector("[data-testid='student-first-name-input']"), By.XPath("(//input[@required and @type='text'])[1]")), firstName);
        Fill(FindFirstVisible(By.CssSelector("[data-testid='student-last-name-input']"), By.XPath("(//input[@required and @type='text'])[2]")), lastName);
        Fill(FindFirstVisible(By.CssSelector("[data-testid='student-grade-input']"), By.XPath("//input[@placeholder='e.g., K, 1, 2, 3...']")), grade);

        SafeClick(FindFirstVisible(
            By.CssSelector("[data-testid='save-student-button']"),
            By.XPath("//button[@type='submit' and contains(normalize-space(.), 'Add Student')]")));
        _wait.Until(_ => IsStudentVisible(firstName, lastName));
    }

    public bool IsStudentVisible(string firstName, string lastName)
    {
        return _driver.PageSource.Contains($"{firstName} {lastName}", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsEnrollmentVisible(string courseName)
    {
        var slug = string.Join("-", courseName.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        return _driver.FindElements(By.CssSelector($"[data-testid='student-enrollment-{slug}']"))
            .Any(e => e.Displayed && e.Text.Contains(courseName, StringComparison.OrdinalIgnoreCase)) ||
            _driver.PageSource.Contains(courseName, StringComparison.OrdinalIgnoreCase);
    }

    private void Fill(IWebElement element, string value)
    {
        element.Clear();
        element.SendKeys(value);
    }

    private IWebElement FindFirstVisible(params By[] locators)
    {
        return _wait.Until(d => FindElements(d, locators).FirstOrDefault(e => e.Displayed && e.Enabled));
    }

    private static IEnumerable<IWebElement> FindElements(IWebDriver driver, params By[] locators)
    {
        foreach (var locator in locators)
        {
            foreach (var element in driver.FindElements(locator))
            {
                yield return element;
            }
        }
    }

    private void SafeClick(IWebElement element)
    {
        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }
    }
}

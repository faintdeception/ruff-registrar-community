using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public sealed class EducatorsPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public EducatorsPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void NavigateToEducators(string baseUrl)
    {
        _driver.Navigate().GoToUrl($"{baseUrl.TrimEnd('/')}/educators");
        WaitForPageLoad();
    }

    public void CreateEducator(string firstName, string lastName, string email, string phone, string department, string bio)
    {
        InviteEducator(firstName, lastName, email, phone, department, bio);
    }

    public EducatorInviteCredentials InviteEducator(string firstName, string lastName, string email, string phone, string department, string bio)
    {
        Click(By.Id("add-educator-btn"));
        _wait.Until(d => d.FindElements(By.Id("educator-first-name-input")).Any());

        SetText(By.Id("educator-first-name-input"), firstName);
        SetText(By.Id("educator-last-name-input"), lastName);
        SetText(By.Id("educator-email-input"), email);
        SetText(By.Id("educator-phone-input"), phone);
        SetText(By.Id("educator-department-input"), department);
        SetText(By.Id("educator-bio-input"), bio);
        Click(By.Id("save-educator-btn"));

        var fullName = $"{firstName} {lastName}";
        _wait.Until(d => IsEducatorVisible(fullName));

        var username = _wait.Until(d => d.FindElement(By.CssSelector("[data-testid='educator-invite-username']"))).Text;
        var password = _wait.Until(d => d.FindElement(By.CssSelector("[data-testid='educator-invite-password']"))).Text;
        return new EducatorInviteCredentials(username, password);
    }

    public bool IsEducatorVisible(string fullName)
    {
        return _driver.PageSource.Contains(fullName, StringComparison.OrdinalIgnoreCase);
    }

    private void WaitForPageLoad()
    {
        _wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
        _wait.Until(d => d.FindElements(By.Id("add-educator-btn")).Any()
            || d.PageSource.Contains("Educators", StringComparison.OrdinalIgnoreCase));
    }

    private void SetText(By locator, string value)
    {
        var element = _wait.Until(d => d.FindElement(locator));
        element.Clear();
        element.SendKeys(value);
    }

    private void Click(By locator)
    {
        var element = _wait.Until(d => d.FindElement(locator));
        try
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center', inline: 'nearest'});", element);
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }
    }

public sealed record EducatorInviteCredentials(string Username, string TemporaryPassword);
}

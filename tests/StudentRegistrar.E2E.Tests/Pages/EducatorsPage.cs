using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public sealed class EducatorsPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private static readonly By UserRolesSelector = By.CssSelector("[data-testid='user-roles']");
    private static readonly By AddEducatorButtonSelector = By.Id("add-educator-btn");
    private static readonly By EducatorCardSelector = By.CssSelector("li[data-testid^='educator-']");
    private static readonly By InviteMessageSelector = By.CssSelector("[data-testid='educator-invite-message']");
    private static readonly By ErrorBannerSelector = By.CssSelector("div.bg-red-100.border.border-red-400");

    public EducatorsPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
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
        WaitForSaveResult();
        _wait.Until(d => IsEducatorVisible(fullName));
        _wait.Until(d => d.FindElements(InviteMessageSelector).Any(element => element.Displayed));

        var username = _wait.Until(d => d.FindElement(By.CssSelector("[data-testid='educator-invite-username']"))).Text;
        var password = _wait.Until(d => d.FindElement(By.CssSelector("[data-testid='educator-invite-password']"))).Text;
        return new EducatorInviteCredentials(username, password);
    }

    public void AuthorizeExistingMemberAsEducator(string memberOptionText, string fullName, string department, string bio)
    {
        Click(By.Id("add-educator-btn"));
        var memberSelect = _wait.Until(d => d.FindElement(By.Id("educator-account-holder-select")));
        var select = new SelectElement(memberSelect);
        var option = select.Options.FirstOrDefault(o => o.Text.Contains(memberOptionText, StringComparison.OrdinalIgnoreCase))
            ?? throw new NoSuchElementException($"Could not find member option containing '{memberOptionText}'. Available: {string.Join(", ", select.Options.Select(o => o.Text))}");

        option.Click();
        SetText(By.Id("educator-department-input"), department);
        SetText(By.Id("educator-bio-input"), bio);
        Click(By.Id("save-educator-btn"));

        WaitForSaveResult();
        _wait.Until(d => IsEducatorVisible(fullName));
        _wait.Until(d => GetInviteMessage().Contains("authorized", StringComparison.OrdinalIgnoreCase));
    }

    public string GetInviteMessage()
    {
        return _wait.Until(d => d.FindElement(InviteMessageSelector)).Text;
    }

    public bool HasTemporaryCredentials()
    {
        return _driver.FindElements(By.CssSelector("[data-testid='educator-invite-username'], [data-testid='educator-invite-password']"))
            .Any(e => e.Displayed);
    }

    public bool IsEducatorVisible(string fullName)
    {
        return _driver.FindElements(EducatorCardSelector)
            .Any(e => e.Text.Contains(fullName, StringComparison.OrdinalIgnoreCase));
    }

    private void WaitForPageLoad()
    {
        _wait.Until(driver => string.Equals(
            ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState")?.ToString(),
            "complete",
            StringComparison.OrdinalIgnoreCase));
        _wait.Until(d => d.PageSource.Contains("Educators", StringComparison.OrdinalIgnoreCase));
        _wait.Until(d =>
        {
            if (IsAdministratorView(d))
            {
                return d.FindElements(AddEducatorButtonSelector).Any(element => element.Displayed);
            }

            return d.FindElements(EducatorCardSelector).Any(element => element.Displayed)
                || d.PageSource.Contains("No educators found", StringComparison.OrdinalIgnoreCase);
        });
    }

    private static bool IsAdministratorView(IWebDriver driver)
    {
        return driver.FindElements(UserRolesSelector)
            .Any(element => element.Displayed && element.Text.Contains("Administrator", StringComparison.OrdinalIgnoreCase));
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

    private void WaitForSaveResult()
    {
        try
        {
            _wait.Until(d =>
            {
                var errorBanner = d.FindElements(ErrorBannerSelector)
                    .FirstOrDefault(element => element.Displayed && !string.IsNullOrWhiteSpace(element.Text));
                if (errorBanner != null)
                {
                    throw new InvalidOperationException($"Educator save failed: {errorBanner.Text}");
                }

                return !d.FindElements(By.Id("save-educator-btn")).Any(element => element.Displayed);
            });
        }
        catch (WebDriverTimeoutException)
        {
            var visibleError = _driver.FindElements(ErrorBannerSelector)
                .FirstOrDefault(element => element.Displayed && !string.IsNullOrWhiteSpace(element.Text));
            if (visibleError != null)
            {
                throw new InvalidOperationException($"Educator save failed: {visibleError.Text}");
            }

            throw;
        }
    }

public sealed record EducatorInviteCredentials(string Username, string TemporaryPassword);
}

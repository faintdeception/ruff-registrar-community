using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public sealed class RoomsPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public RoomsPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void NavigateToRooms(string baseUrl)
    {
        _driver.Navigate().GoToUrl($"{baseUrl.TrimEnd('/')}/rooms");
        WaitForPageLoad();
    }

    public void CreateRoom(string name, string type, string capacity, string notes)
    {
        ClickCreateRoom();
        SetText(By.Id("room-name-input"), name);
        SelectByText(By.Id("room-type-select"), type);
        SetText(By.Id("room-capacity-input"), capacity);
        SetText(By.Id("room-notes-input"), notes);
        Click(By.Id("save-room-btn"));
        _wait.Until(d => d.FindElements(By.Id("room-modal")).All(e => !e.Displayed));
        _wait.Until(d => IsRoomVisible(name));
    }

    public bool IsRoomVisible(string name)
    {
        var slug = ToSlug(name);
        return _driver.FindElements(By.CssSelector($"[data-testid='room-{slug}']")).Any(e => e.Displayed)
            || _driver.PageSource.Contains(name, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToSlug(string value)
    {
        return string.Join("-", value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    }

    private void ClickCreateRoom()
    {
        ClickFirst(By.Id("create-room-btn"), By.Id("create-first-room-btn"));
        _wait.Until(d => d.FindElements(By.Id("room-name-input")).Any());
    }

    private void WaitForPageLoad()
    {
        _wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
        _wait.Until(d => d.FindElements(By.Id("create-room-btn")).Any()
            || d.FindElements(By.Id("create-first-room-btn")).Any()
            || d.PageSource.Contains("Room Management", StringComparison.OrdinalIgnoreCase));
    }

    private void SetText(By locator, string value)
    {
        var element = _wait.Until(d => d.FindElement(locator));
        element.Clear();
        element.SendKeys(value);
    }

    private void SelectByText(By locator, string text)
    {
        var select = new SelectElement(_wait.Until(d => d.FindElement(locator)));
        select.SelectByText(text);
    }

    private void Click(By locator)
    {
        _wait.Until(d =>
        {
            try
            {
                var element = d.FindElement(locator);
                Click(element);
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        });
    }

    private void ClickFirst(params By[] locators)
    {
        _wait.Until(d =>
        {
            foreach (var locator in locators)
            {
                var elements = d.FindElements(locator);
                if (elements.Count == 0)
                {
                    continue;
                }

                try
                {
                    Click(elements[0]);
                    return true;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
            }

            return false;
        });
    }

    private void Click(IWebElement element)
    {
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
}

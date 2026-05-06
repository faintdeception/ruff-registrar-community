using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public LoginPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    // Page elements - supports both the app login entry page and the hosted Keycloak form.
    public IWebElement UsernameField => _wait.Until(d => d.FindElement(By.Id("username")));
    public IWebElement PasswordField => _driver.FindElement(By.Id("password"));
    public IWebElement LoginButton => _driver.FindElement(By.CssSelector("button[type='submit'], input[type='submit'], .login-button"));
    public IWebElement KeycloakEntryButton => _wait.Until(d => d.FindElement(By.XPath("//button[normalize-space()='Sign in with Keycloak']")));
    
    // Error message elements
    public IWebElement LoginErrorHeading => _driver.FindElement(By.XPath("//*[contains(text(), 'Login Error')]"));
    public IWebElement InvalidCredentialsMessage => _driver.FindElement(By.XPath("//*[contains(text(), 'Invalid user credentials')]"));

    // Page actions
    public void EnterUsername(string username)
    {
        EnsureHostedLoginFormVisible();
        UsernameField.Clear();
        UsernameField.SendKeys(username);
    }

    public void EnterPassword(string password)
    {
        EnsureHostedLoginFormVisible();
        PasswordField.Clear();
        PasswordField.SendKeys(password);
    }

    public void ClickLogin()
    {
        LoginButton.Click();
    }

    public void Login(string username, string password)
    {
        EnsureHostedLoginFormVisible();
        EnterUsername(username);
        EnterPassword(password);
        ClickLogin();
    }

    // Page validations
    public bool IsOnLoginPage()
    {
        try
        {
            if (_driver.Url.Contains("/login") && _driver.FindElements(By.XPath("//button[normalize-space()='Sign in with Keycloak']")).Any())
            {
                return true;
            }

            return _driver.FindElements(By.Id("username")).Any() &&
                   _driver.FindElements(By.Id("password")).Any();
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool HasLoginError()
    {
        try
        {
            return LoginErrorHeading.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool HasInvalidCredentialsMessage()
    {
        try
        {
            return InvalidCredentialsMessage.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public bool HasErrorMessage()
    {
        // Check for either specific error elements or error text in page source
        return HasLoginError() || 
               HasInvalidCredentialsMessage() ||
               _driver.PageSource.Contains("Login Error") ||
               _driver.PageSource.Contains("Invalid user credentials");
    }

    public string GetErrorMessage()
    {
        try
        {
            if (HasLoginError())
                return LoginErrorHeading.Text;
            if (HasInvalidCredentialsMessage())
                return InvalidCredentialsMessage.Text;
            return "Error message found in page source";
        }
        catch
        {
            return string.Empty;
        }
    }

    private void EnsureHostedLoginFormVisible()
    {
        if (_driver.FindElements(By.Id("username")).Any())
        {
            return;
        }

        if (_driver.Url.Contains("/login", StringComparison.OrdinalIgnoreCase) &&
            _driver.FindElements(By.XPath("//button[normalize-space()='Sign in with Keycloak']")).Any())
        {
            KeycloakEntryButton.Click();
        }

        _wait.Until(d => d.FindElements(By.Id("username")).Any() && d.FindElements(By.Id("password")).Any());
    }
}

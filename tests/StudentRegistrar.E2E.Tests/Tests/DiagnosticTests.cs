using OpenQA.Selenium;
using StudentRegistrar.E2E.Tests.Base;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests;

public class DiagnosticTests : BaseTest
{
    [Fact]
    public void Debug_Home_Page_Content()
    {
        // Navigate to home and see what we actually get
        NavigateToHome();
        WaitForPageLoad();

        // Output diagnostic information
        var url = Driver.Url;
        var title = Driver.Title;
        var pageSource = Driver.PageSource;

        // Log what we found
        Console.WriteLine($"=== HOME PAGE DIAGNOSTICS ===");
        Console.WriteLine($"URL: {url}");
        Console.WriteLine($"Title: {title}");
        Console.WriteLine($"Page contains 'login': {pageSource.Contains("login", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"Page contains 'logout': {pageSource.Contains("logout", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"Page contains 'username': {pageSource.Contains("username", StringComparison.OrdinalIgnoreCase)}");
        Console.WriteLine($"Page contains 'password': {pageSource.Contains("password", StringComparison.OrdinalIgnoreCase)}");
        
        // Look for common login-related elements
        var hasUsernameField = IsElementPresent(By.Id("username"));
        var hasPasswordField = IsElementPresent(By.Id("password"));
        var hasLoginButton = IsElementPresent(By.CssSelector("button[type='submit']"));
        
        Console.WriteLine($"Has #username field: {hasUsernameField}");
        Console.WriteLine($"Has #password field: {hasPasswordField}");
        Console.WriteLine($"Has submit button: {hasLoginButton}");

        // Look for possible logout elements
        var hasLogoutTestId = IsElementPresent(By.CssSelector("[data-testid='logout-button']"));
        var hasLogoutButton = IsElementPresent(By.XPath("//*[contains(text(), 'Logout') or contains(text(), 'Log out') or contains(text(), 'Sign out')]"));
        
        Console.WriteLine($"Has [data-testid='logout-button']: {hasLogoutTestId}");
        Console.WriteLine($"Has logout text: {hasLogoutButton}");

        // Print first 1000 characters of page source for debugging
        var sourcePreview = pageSource.Length > 1000 ? pageSource.Substring(0, 1000) : pageSource;
        Console.WriteLine($"Page source preview: {sourcePreview}");

        // This test always passes - it's just for diagnostics
        Assert.True(true);
    }

    [Fact]
    public void Debug_After_Login_Attempt()
    {
        // Navigate to home
        NavigateToHome();
        WaitForPageLoad();

        // Try to find login form elements and attempt login
        if (IsElementPresent(By.Id("username")) && IsElementPresent(By.Id("password")))
        {
            Console.WriteLine("=== FOUND LOGIN FORM ===");
            
            var usernameField = Driver.FindElement(By.Id("username"));
            var passwordField = Driver.FindElement(By.Id("password"));
            
            usernameField.SendKeys("admin1");
            passwordField.SendKeys("AdminPass123!");
            
            // Look for submit button
            if (IsElementPresent(By.CssSelector("button[type='submit']")))
            {
                var submitButton = Driver.FindElement(By.CssSelector("button[type='submit']"));
                submitButton.Click();
                
                WaitForPageLoad();
                Thread.Sleep(3000);
                
                Console.WriteLine($"=== AFTER LOGIN ATTEMPT ===");
                Console.WriteLine($"URL after login: {Driver.Url}");
                Console.WriteLine($"Title after login: {Driver.Title}");
                
                // Check for logout elements again
                var hasLogoutTestId = IsElementPresent(By.CssSelector("[data-testid='logout-button']"));
                var hasLogoutText = IsElementPresent(By.XPath("//*[contains(text(), 'Logout') or contains(text(), 'Log out') or contains(text(), 'Sign out')]"));
                
                Console.WriteLine($"Has logout button after login: {hasLogoutTestId}");
                Console.WriteLine($"Has logout text after login: {hasLogoutText}");
                
                // Look for any buttons in the page
                var allButtons = Driver.FindElements(By.TagName("button"));
                Console.WriteLine($"Total buttons found: {allButtons.Count}");
                foreach (var button in allButtons.Take(5))
                {
                    try
                    {
                        Console.WriteLine($"Button text: '{button.Text}', class: '{button.GetDomAttribute("class")}', data-testid: '{button.GetDomAttribute("data-testid")}'");
                    }
                    catch
                    {
                        Console.WriteLine("Button info not accessible");
                    }
                }
            }
            else
            {
                Console.WriteLine("No submit button found");
            }
        }
        else
        {
            Console.WriteLine("=== NO LOGIN FORM FOUND ===");
            Console.WriteLine("This might mean we're already logged in or the page structure is different");
        }

        Assert.True(true);
    }
}

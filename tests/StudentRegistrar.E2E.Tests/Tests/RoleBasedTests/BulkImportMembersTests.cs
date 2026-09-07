using OpenQA.Selenium;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests.RoleBasedTests;

/// <summary>
/// Tests for the admin "Bulk Import Members" CSV upload flow.
/// </summary>
public class BulkImportMembersTests : BaseRoleNavigationTest
{
    [Fact]
    public void Admin_Should_Bulk_Import_Members_From_Csv()
    {
        // Arrange - client-side nav (a hard reload loses the Keycloak session on localhost)
        LoginAsAdmin();
        EnsureInitialInvitePasswordConfigured();

        NavigationPage.ClickHome();
        WaitForPageLoad();
        Driver.FindElement(By.CssSelector("[data-testid='members-card']")).Click();
        WaitForPageLoad();
        WaitUntil(d => d.Url.Contains("/members") && d.PageSource.Contains("Members Management"));

        var timestamp = DateTime.Now.Ticks.ToString()[10..];
        var email = $"bulkimport{timestamp}@example.com";
        var csvPath = CreateTempCsv("valid-members.csv", timestamp, email, "bulk-import");

        try
        {
            // Act
            Driver.FindElement(By.Id("bulk-import-button")).Click();
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-panel']"));

            var fileInput = Driver.FindElement(By.Id("bulk-import-file-input"));
            fileInput.SendKeys(csvPath);

            Driver.FindElement(By.Id("bulk-import-submit-button")).Click();

            // Assert
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-results']"), 20);
            var resultsText = Driver.FindElement(By.CssSelector("[data-testid='bulk-import-results']")).Text;
            Assert.Contains("1 of 1 member(s) created", resultsText);
            Assert.Contains(email, resultsText);

            // The new member must show up in the (unfiltered) members list once refreshed.
            WaitUntil(d => d.PageSource.Contains(email), 15,
                failureMessage: "Bulk-imported member did not appear in the members list");
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [Fact]
    public void Bulk_Imported_Member_Should_Be_Required_To_Change_Password_On_First_Login()
    {
        const string invitePassword = "Bulk-Invite-Password-99!";

        LoginAsAdmin();
        EnsureInitialInvitePasswordConfigured(invitePassword, overwrite: true);

        NavigationPage.ClickHome();
        WaitForPageLoad();
        Driver.FindElement(By.CssSelector("[data-testid='members-card']")).Click();
        WaitForPageLoad();
        WaitUntil(d => d.Url.Contains("/members") && d.PageSource.Contains("Members Management"));

        var timestamp = DateTime.Now.Ticks.ToString()[10..];
        var email = $"bulkpassword{timestamp}@example.com";
        var csvPath = CreateTempCsv("valid-members.csv", timestamp, email, "bulk-password");

        try
        {
            Driver.FindElement(By.Id("bulk-import-button")).Click();
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-panel']"));
            Driver.FindElement(By.Id("bulk-import-file-input")).SendKeys(csvPath);
            Driver.FindElement(By.Id("bulk-import-submit-button")).Click();

            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-results']"), 20);
            Assert.Contains("1 of 1 member(s) created", Driver.FindElement(By.CssSelector("[data-testid='bulk-import-results']")).Text);

            new HomePage(Driver).ClickLogout();
            WaitForUrlContains("/login", 30);

            var loginPage = new LoginPage(Driver);
            loginPage.Login(email, invitePassword);

            WaitUntil(
                d => d.PageSource.Contains("Update Password", StringComparison.OrdinalIgnoreCase)
                    || d.PageSource.Contains("New Password", StringComparison.OrdinalIgnoreCase)
                    || d.FindElements(By.Id("password-new")).Count > 0,
                30,
                failureMessage: "Bulk-imported member was not presented with the required password-change prompt");

            Assert.DoesNotContain("/members", Driver.Url, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Driver.FindElements(By.Id("logout-button")));
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [Fact]
    public void Admin_Should_See_Row_Level_Errors_For_Invalid_Csv_Rows()
    {
        // Arrange - client-side nav (a hard reload loses the Keycloak session on localhost)
        LoginAsAdmin();
        EnsureInitialInvitePasswordConfigured();

        NavigationPage.ClickHome();
        WaitForPageLoad();
        Driver.FindElement(By.CssSelector("[data-testid='members-card']")).Click();
        WaitForPageLoad();
        WaitUntil(d => d.Url.Contains("/members") && d.PageSource.Contains("Members Management"));

        var timestamp = DateTime.Now.Ticks.ToString()[10..];
        var goodEmail = $"bulkimportgood{timestamp}@example.com";
        var csvPath = CreateTempCsv("mixed-validity-members.csv", timestamp, goodEmail, "bulk-import-mixed");

        try
        {
            // Act
            Driver.FindElement(By.Id("bulk-import-button")).Click();
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-panel']"));

            var fileInput = Driver.FindElement(By.Id("bulk-import-file-input"));
            fileInput.SendKeys(csvPath);

            Driver.FindElement(By.Id("bulk-import-submit-button")).Click();

            // Assert - partial success is reported per-row, not an all-or-nothing failure
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-results']"), 20);
            var resultsText = Driver.FindElement(By.CssSelector("[data-testid='bulk-import-results']")).Text;
            Assert.Contains("1 of 2 member(s) created", resultsText);
            Assert.Contains("1 failed", resultsText);
            Assert.Contains(goodEmail, resultsText);
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    private static string CreateTempCsv(string fixtureName, string timestamp, string email, string filePrefix)
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "TestData", "BulkImport", fixtureName);
        var csv = File.ReadAllText(fixturePath)
            .Replace("{timestamp}", timestamp, StringComparison.Ordinal)
            .Replace("{email}", email, StringComparison.Ordinal);
        var csvPath = Path.Combine(Path.GetTempPath(), $"{filePrefix}-{timestamp}.csv");
        File.WriteAllText(csvPath, csv);
        return csvPath;
    }
}

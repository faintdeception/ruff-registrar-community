using System;
using System.IO;
using ClosedXML.Excel;
using OpenQA.Selenium;
using StudentRegistrar.E2E.Tests.Base;
using StudentRegistrar.E2E.Tests.Pages;
using Xunit;

namespace StudentRegistrar.E2E.Tests.Tests.RoleBasedTests;

/// <summary>
/// Tests for the admin "Bulk Import Families" .xlsx upload flow (parent + children per family).
/// Replaces the old CSV-only bulk import E2E coverage (BulkImportMembersTests, deleted).
/// </summary>
public class BulkImportFamiliesTests : BaseRoleNavigationTest
{
    [Fact]
    public void Admin_Should_Bulk_Import_Family_With_Children_From_Excel()
    {
        LoginAsAdmin();
        EnsureInitialInvitePasswordConfigured();

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10);
        var parentEmail = $"bulkfamily{timestamp}@example.com";
        var xlsxPath = CreateTempFamilyImportXlsx(parentEmail, "Bulk", $"Family{timestamp}",
            ("Alex", $"Family{timestamp}"), ("Sam", $"Family{timestamp}"));

        try
        {
            NavigateToMembersPage();

            Driver.FindElement(By.Id("bulk-import-button")).Click();
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-panel']"));

            Driver.FindElement(By.Id("bulk-import-file-input")).SendKeys(xlsxPath);
            Driver.FindElement(By.Id("bulk-import-submit-button")).Click();

            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-results']"), 20);
            var resultsText = Driver.FindElement(By.CssSelector("[data-testid='bulk-import-results']")).Text;
            Assert.Contains("1 of 1 famil", resultsText);
            Assert.Contains("2 child", resultsText);

            WaitUntil(
                d => d.PageSource.Contains(parentEmail, StringComparison.OrdinalIgnoreCase),
                20,
                failureMessage: "Bulk-imported family's parent did not appear in the members list");
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    [Fact]
    public void Bulk_Imported_Parent_Should_Be_Required_To_Change_Password_On_First_Login()
    {
        LoginAsAdmin();
        EnsureInitialInvitePasswordConfigured();

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10);
        var parentEmail = $"bulkfamilypwd{timestamp}@example.com";
        var xlsxPath = CreateTempFamilyImportXlsx(parentEmail, "Bulk", $"Password{timestamp}",
            ("Alex", $"Password{timestamp}"));

        try
        {
            NavigateToMembersPage();

            Driver.FindElement(By.Id("bulk-import-button")).Click();
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-panel']"));
            Driver.FindElement(By.Id("bulk-import-file-input")).SendKeys(xlsxPath);
            Driver.FindElement(By.Id("bulk-import-submit-button")).Click();

            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-results']"), 20);
            Assert.Contains("1 of 1 famil", Driver.FindElement(By.CssSelector("[data-testid='bulk-import-results']")).Text);

            Logout();
            NavigateToHome();
            WaitForPageLoad();
            WaitForUrlContains("/login");

            // Keycloak users created by bulk import use the parent's email as their username.
            var loginPage = new LoginPage(Driver);
            loginPage.Login(parentEmail, "Correct-Horse-Battery-99!");

            WaitUntil(
                d => d.PageSource.Contains("change", StringComparison.OrdinalIgnoreCase) &&
                     d.PageSource.Contains("password", StringComparison.OrdinalIgnoreCase),
                20,
                failureMessage: "Bulk-imported parent was not presented with the required password-change prompt");
        }
        finally
        {
            File.Delete(xlsxPath);
        }
    }

    [Fact]
    public void Bulk_Import_With_Existing_Parent_Email_Adds_Child_Instead_Of_Duplicate_Account()
    {
        LoginAsAdmin();
        EnsureInitialInvitePasswordConfigured();

        var timestamp = DateTime.Now.Ticks.ToString().Substring(10);
        var parentEmail = $"bulkfamilyexisting{timestamp}@example.com";
        var firstXlsxPath = CreateTempFamilyImportXlsx(parentEmail, "Bulk", $"Existing{timestamp}",
            ("Alex", $"Existing{timestamp}"));
        var secondXlsxPath = CreateTempFamilyImportXlsx(parentEmail, "Bulk", $"Existing{timestamp}",
            ("Sam", $"Existing{timestamp}"));

        try
        {
            NavigateToMembersPage();

            Driver.FindElement(By.Id("bulk-import-button")).Click();
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-panel']"));
            Driver.FindElement(By.Id("bulk-import-file-input")).SendKeys(firstXlsxPath);
            Driver.FindElement(By.Id("bulk-import-submit-button")).Click();
            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-results']"), 20);

            // Same panel, second upload targeting the same ParentEmail.
            Driver.FindElement(By.Id("bulk-import-file-input")).SendKeys(secondXlsxPath);
            Driver.FindElement(By.Id("bulk-import-submit-button")).Click();

            WaitForElementVisible(By.CssSelector("[data-testid='bulk-import-results']"), 20);
            var resultsText = Driver.FindElement(By.CssSelector("[data-testid='bulk-import-results']")).Text;
            Assert.Contains("Existing", resultsText);
        }
        finally
        {
            File.Delete(firstXlsxPath);
            File.Delete(secondXlsxPath);
        }
    }

    private void NavigateToMembersPage()
    {
        Driver.FindElement(By.CssSelector("[data-testid='members-card']")).Click();
        WaitForPageLoad();
        WaitUntil(d => d.Url.Contains("/members") && d.PageSource.Contains("Members Management"));
    }

    private void Logout()
    {
        var homePage = new HomePage(Driver);
        Assert.True(homePage.HasLogoutButton());
        homePage.ClickLogout();
        WaitForPageLoad();
        WaitForUrlContains("/login");
    }

    private static string CreateTempFamilyImportXlsx(
        string parentEmail, string parentFirstName, string parentLastName,
        params (string FirstName, string LastName)[] children)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Family Import");
        var headers = new[]
        {
            "ParentFirstName", "ParentLastName", "ParentEmail",
            "ChildFirstName", "ChildLastName", "ChildDateOfBirth", "ChildGrade"
        };
        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
        }

        var row = 2;
        foreach (var child in children)
        {
            sheet.Cell(row, 1).Value = parentFirstName;
            sheet.Cell(row, 2).Value = parentLastName;
            sheet.Cell(row, 3).Value = parentEmail;
            sheet.Cell(row, 4).Value = child.FirstName;
            sheet.Cell(row, 5).Value = child.LastName;
            row++;
        }

        var path = Path.Combine(Path.GetTempPath(), $"family-import-{Guid.NewGuid():N}.xlsx");
        workbook.SaveAs(path);
        return path;
    }
}

using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public class SemestersPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public SemestersPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    // Page elements using the new test IDs
    private IWebElement CreateSemesterButton => _wait.Until(d => d.FindElement(By.Id("create-semester-btn")));
    private IWebElement CreateFirstSemesterButton => _driver.FindElement(By.Id("create-first-semester-btn"));
    
    // Modal elements
    private IWebElement SemesterModal => _wait.Until(d => d.FindElement(By.Id("semester-modal")));
    private IWebElement ModalTitle => _driver.FindElement(By.Id("modal-title"));
    private IWebElement SemesterNameInput => _driver.FindElement(By.Id("semester-name-input"));
    private IWebElement SemesterCodeInput => _driver.FindElement(By.Id("semester-code-input"));
    private IWebElement StartDateInput => _driver.FindElement(By.Id("semester-start-date-input"));
    private IWebElement EndDateInput => _driver.FindElement(By.Id("semester-end-date-input"));
    private IWebElement RegistrationStartDateInput => _driver.FindElement(By.Id("semester-reg-start-date-input"));
    private IWebElement RegistrationEndDateInput => _driver.FindElement(By.Id("semester-reg-end-date-input"));
    private IWebElement IsActiveCheckbox => _driver.FindElement(By.Id("semester-is-active-checkbox"));
    private IWebElement SaveSemesterButton => _driver.FindElement(By.Id("save-semester-btn"));
    private IWebElement CancelSemesterButton => _driver.FindElement(By.Id("cancel-semester-btn"));
    private IWebElement ErrorMessage => _driver.FindElement(By.Id("error-message"));

    // Navigation
    public void NavigateToSemesters()
    {
        var semestersLink = _driver.FindElement(By.LinkText("Semesters"));
        semestersLink.Click();
        WaitForPageLoad();
    }

    // Actions
    public void ClickCreateSemester()
    {
        try
        {
            TryClickWithRetry(CreateSemesterButton);
        }
        catch (NoSuchElementException)
        {
            // If main create button not found, try the "create first semester" button
            TryClickWithRetry(CreateFirstSemesterButton);
        }
        WaitForModalToOpen();
    }

    public void WaitForModalToOpen()
    {
        _wait.Until(d => SemesterModal.Displayed);
    }

    public void FillSemesterForm(string name, string code, DateTime startDate, DateTime endDate, 
                                DateTime regStartDate, DateTime regEndDate, bool isActive = false)
    {
        SemesterNameInput.Clear();
        SemesterNameInput.SendKeys(name);
        
        SemesterCodeInput.Clear();
        SemesterCodeInput.SendKeys(code);
        
        StartDateInput.Clear();
        StartDateInput.SendKeys(startDate.ToString("MM/dd/yyyy"));
        
        EndDateInput.Clear();
        EndDateInput.SendKeys(endDate.ToString("MM/dd/yyyy"));

        RegistrationStartDateInput.Clear();
        RegistrationStartDateInput.SendKeys(regStartDate.ToString("MM/dd/yyyy"));

        RegistrationEndDateInput.Clear();
        RegistrationEndDateInput.SendKeys(regEndDate.ToString("MM/dd/yyyy"));

        if (isActive != IsActiveCheckbox.Selected)
        {
            ClickThroughOverlay(IsActiveCheckbox);
        }
    }

    public void SaveSemester()
    {
        SaveSemesterButton.Click();
        
        // Wait for error or modal close
        // try {
        //     _wait.Until(d => IsErrorDisplayed() || !SemesterModal.Displayed);
        // } catch (WebDriverTimeoutException) {
        //     // If neither error nor close, log and continue
        //     Console.WriteLine("Timeout waiting for error or modal close after save");
        // }
        if (IsErrorDisplayed()) {
            Console.WriteLine($"Error during save: {GetErrorMessage()}");
            return;
        }
        WaitForModalToClose();
    }

    public void CancelCreate()
    {
        try
        {
            CancelSemesterButton.Click();
        }
        catch (OpenQA.Selenium.ElementClickInterceptedException)
        {
            // If normal click is intercepted, use JavaScript click
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", CancelSemesterButton);
        }
        WaitForModalToClose();
    }

    // Verification methods
    public bool IsOnSemestersPage()
    {
        return _driver.Url.Contains("/semesters") && 
               _driver.PageSource.ToLower().Contains("semester");
    }

    public bool CanSeeCreateButton()
    {
        try
        {
            return CreateSemesterButton.Displayed;
        }
        catch (NoSuchElementException)
        {
            try
            {
                return CreateFirstSemesterButton.Displayed;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }
    }

    // public bool IsCreateFormVisible()
    // {
    //     try
    //     {
    //         return SemesterModal.Displayed && SemesterNameInput.Displayed;
    //     }
    //     catch (NoSuchElementException)
    //     {
    //         return false;
    //     }
    // }

    public bool IsSemesterVisible(string semesterName)
    {
        var slug = semesterName.Replace(" ", "-").ToLower();
        try
        {
            var element = _driver.FindElement(By.Id($"semester-{slug}"));
            return element.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public void DeleteSemester(string semesterName)
    {
        var slug = semesterName.Replace(" ", "-").ToLower();
        var semesterCard = _driver.FindElement(By.Id($"semester-{slug}"));
        var semesterId = semesterCard.GetDomAttribute("data-semester-id");
        var deleteButton = _driver.FindElement(By.Id($"delete-semester-{semesterId}"));
        deleteButton.Click();
        
        // Handle confirmation dialog
        var alert = _wait.Until(d => d.SwitchTo().Alert());
        alert.Accept();
    }

    public void EditSemester(string semesterName)
    {
    var slug = semesterName.Replace(" ", "-").ToLower();
    var semesterCard = _driver.FindElement(By.Id($"semester-{slug}"));
    var semesterId = semesterCard.GetDomAttribute("data-semester-id");
    var editButton = _driver.FindElement(By.Id($"edit-semester-{semesterId}"));
    SafeClick(editButton);
        WaitForModalToOpen();
    }

    public string GetModalTitle()
    {
        return ModalTitle.Text;
    }

    public bool IsErrorDisplayed()
    {
        try
        {
            return ErrorMessage.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public string GetErrorMessage()
    {
        return ErrorMessage.Text;
    }

    public string GetSuccessMessage()
    {
        try
        {
            var successElements = _driver.FindElements(By.CssSelector(".alert-success, .success, .toast-success, [data-testid='success-message']"));
            return successElements.FirstOrDefault()?.Text ?? "";
        }
        catch (NoSuchElementException)
        {
            return "";
        }
    }

    public int GetSemesterCount()
    {
        try
        {
            // Count semester cards using their new id pattern
            var semesterCards = _driver.FindElements(By.CssSelector("div[id^='semester-']"));
            return semesterCards.Count;
        }
        catch (NoSuchElementException)
        {
            return 0;
        }
    }

    public void WaitForModalToClose()
    {
        try
        {
            _wait.Until(d => {
                try
                {
                    var modal = d.FindElement(By.Id("semester-modal"));
                    return !modal.Displayed;
                }
                catch (NoSuchElementException)
                {
                    return true; // Modal is gone
                }
                catch (OpenQA.Selenium.StaleElementReferenceException)
                {
                    return true; // Modal element is stale, means it's been removed
                }
            });
        }
        catch (OpenQA.Selenium.WebDriverTimeoutException)
        {
            Console.WriteLine("Modal did not close within timeout period");
            // Take a screenshot or log page source for debugging
            Console.WriteLine($"Current URL: {_driver.Url}");
            Console.WriteLine($"Page contains modal: {_driver.PageSource.Contains("semester-modal")}");
            throw;
        }
    }

    // Helper methods
    private void WaitForPageLoad()
    {
        _wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
        // Wait for at least one semester card or create button to appear
        _wait.Until(d => d.FindElements(By.CssSelector("[data-testid^='semester-'],#create-semester-btn,#create-first-semester-btn")).Count > 0);
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

    private void TryClickWithRetry(IWebElement element, int retries = 3)
    {
        for (int attempt = 0; attempt < retries; attempt++)
        {
            try
            {
                SafeClick(element);
                return;
            }
            catch (ElementClickInterceptedException)
            {
                // Wait for potential overlay (Next.js portal) to disappear
                try
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector("nextjs-portal"))
                        .Any(el => el.Displayed));
                }
                catch {}
                if (attempt == retries - 1) throw;
            }
            catch (WebDriverException)
            {
                if (attempt == retries - 1) throw;
            }
        }
    }

    private void ClickThroughOverlay(IWebElement element)
    {
        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            // Scroll into view and try JS click after waiting for portal
            try
            {
                _wait.Until(d => !d.FindElements(By.CssSelector("nextjs-portal"))
                    .Any(el => el.Displayed));
            }
            catch {}
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", element);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }
    }
}

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
        
        SetInputValue(StartDateInput, startDate.ToString("yyyy-MM-dd"));
        SetInputValue(EndDateInput, endDate.ToString("yyyy-MM-dd"));
        SetInputValue(RegistrationStartDateInput, regStartDate.ToString("yyyy-MM-dd"));
        SetInputValue(RegistrationEndDateInput, regEndDate.ToString("yyyy-MM-dd"));

        if (isActive != IsActiveCheckbox.Selected)
        {
            ClickThroughOverlay(IsActiveCheckbox);
        }
    }

    public void SaveSemester()
    {
        TryClickWithRetry(SaveSemesterButton);

        if (IsErrorDisplayed()) {
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
        var slug = string.Join("-", semesterName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
        try
        {
            var element = _wait.Until(d =>
                d.FindElements(By.Id($"semester-{slug}")).FirstOrDefault(e => e.Displayed) ??
                d.FindElements(By.XPath($"//div[@data-testid='semester-{slug}' or @id='semester-{slug}']")).FirstOrDefault(e => e.Displayed) ??
                d.FindElements(By.XPath($"//h3[normalize-space()='{semesterName}']")).FirstOrDefault(e => e.Displayed));
            return element.Displayed;
        }
        catch (WebDriverTimeoutException)
        {
            return false;
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
        _wait.Until(d => {
            try
            {
                var modal = d.FindElement(By.Id("semester-modal"));
                return !modal.Displayed;
            }
            catch (NoSuchElementException)
            {
                return true;
            }
            catch (OpenQA.Selenium.StaleElementReferenceException)
            {
                return true;
            }
        });
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

    private void SetInputValue(IWebElement element, string value)
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
                        @"const input = arguments[0];
                            const nextValue = arguments[1];
                            const descriptor = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(input), 'value')
                                || Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value');
                            descriptor.set.call(input, nextValue);
                            input.dispatchEvent(new Event('input', { bubbles: true }));
                            input.dispatchEvent(new Event('change', { bubbles: true }));",
            element,
            value);
    }
}

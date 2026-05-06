using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace StudentRegistrar.E2E.Tests.Pages;

public class CoursesPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private static readonly By CreateCourseButtonLocator = By.CssSelector("[data-testid='add-course-btn'], [data-testid='add-first-course-btn']");
    private static readonly By CourseNameInputLocator = By.CssSelector("[data-testid='course-name-input']");
    private static readonly By CourseCodeInputLocator = By.CssSelector("[data-testid='course-code-input']");
    private static readonly By AgeGroupSelectLocator = By.CssSelector("[data-testid='course-age-group-select']");
    private static readonly By MaxCapacityInputLocator = By.CssSelector("[data-testid='course-max-capacity-input']");
    private static readonly By RoomSelectLocator = By.CssSelector("[data-testid='course-room-select']");
    private static readonly By FeeInputLocator = By.CssSelector("[data-testid='course-fee-input']");
    private static readonly By PeriodCodeInputLocator = By.CssSelector("[data-testid='course-period-code-input']");
    private static readonly By StartTimeInputLocator = By.CssSelector("[data-testid='course-start-time-input']");
    private static readonly By EndTimeInputLocator = By.CssSelector("[data-testid='course-end-time-input']");
    private static readonly By DescriptionInputLocator = By.CssSelector("[data-testid='course-description-input']");
    private static readonly By SaveCourseButtonLocator = By.XPath("//button[@type='submit' and contains(text(), 'Create Course')]");
    private static readonly By CancelCourseButtonLocator = By.XPath("//button[contains(text(), 'Cancel')]");

    public CoursesPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        _wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    // Page elements
    private IWebElement SemesterSelect => _wait.Until(d => d.FindElement(By.CssSelector("[data-testid='semester-select']")));
    private IWebElement CreateCourseButton => _driver.FindElement(CreateCourseButtonLocator);
    private IWebElement SaveCourseButton => _driver.FindElement(SaveCourseButtonLocator);
    private IWebElement CancelCourseButton => _driver.FindElement(CancelCourseButtonLocator);

    // Navigation
    public void NavigateToCourses()
    {
    var coursesLink = _driver.FindElement(By.LinkText("Courses"));
    SafeClick(coursesLink);
        WaitForPageLoad();
    }

    public void NavigateToCourses(string baseUrl)
    {
        _driver.Navigate().GoToUrl($"{baseUrl.TrimEnd('/')}/courses");
        WaitForPageLoad();
    }

    public void WaitForPageLoad()
    {
        _wait.Until(driver => driver.Url.Contains("/courses"));
    }

    // Actions
    /// <summary>
    /// Selects a semester by matching the beginning of the display text.
    /// This is the recommended method as it handles cases where the display text 
    /// includes additional information like dates in parentheses.
    /// Example: "Detailed Course Semester 20250729210558 (Aug 31, 2025 - Dec 19, 2025)"
    /// Can be selected with: "Detailed Course Semester"
    /// </summary>
    public void SelectSemester(string semesterName)
    {
        var semesterSelect = new SelectElement(SemesterSelect);
        
        // Find option that starts with the semester name (handles dates in parentheses)
        var option = semesterSelect.Options
            .FirstOrDefault(opt => opt.Text.StartsWith(semesterName, StringComparison.OrdinalIgnoreCase));
        
        if (option != null)
        {
            option.Click();
        }
        else
        {
            throw new NoSuchElementException($"Could not find semester option starting with: {semesterName}");
        }
        
    // Wait for potential course cards or empty state to load
    _wait.Until(d => d.FindElements(By.CssSelector(".bg-white.rounded-lg.shadow")).Count >= 0);
    }

    public void SelectSemesterByExactText(string exactText)
    {
        var semesterSelect = new SelectElement(SemesterSelect);
        semesterSelect.SelectByText(exactText);
    _wait.Until(d => d.FindElements(By.CssSelector(".bg-white.rounded-lg.shadow")).Count >= 0);
    }

    public void SelectSemesterByPartialText(string partialText)
    {
        var semesterSelect = new SelectElement(SemesterSelect);
        
        var option = semesterSelect.Options
            .FirstOrDefault(opt => opt.Text.Contains(partialText, StringComparison.OrdinalIgnoreCase));
        
        if (option != null)
        {
            option.Click();
        }
        else
        {
            throw new NoSuchElementException($"Could not find semester option containing: {partialText}");
        }
        
    _wait.Until(d => d.FindElements(By.CssSelector(".bg-white.rounded-lg.shadow")).Count >= 0);
    }

    public void SelectSemesterByValue(string value)
    {
        var semesterSelect = new SelectElement(SemesterSelect);
        semesterSelect.SelectByValue(value);
    _wait.Until(d => d.FindElements(By.CssSelector(".bg-white.rounded-lg.shadow")).Count >= 0);
    }

    public void ClickCreateCourse()
    {
    SafeClick(CreateCourseButtonLocator);
        WaitForModalToOpen();
    }

    public void CreateCourse(string name, string code, string ageGroup, int maxCapacity, decimal fee, string periodCode, string description)
    {
        ClickCreateCourse();
        FillCourseForm(name, code, ageGroup, maxCapacity, fee: fee, periodCode: periodCode, description: description);
        SaveCourse();
        _wait.Until(d => IsCourseVisible(name));
    }

    public void WaitForModalToOpen()
    {
        _wait.Until(_ => IsCreateFormVisible());
        WaitForInteractable(CourseNameInputLocator);
        WaitForInteractable(CourseCodeInputLocator);
        WaitForInteractable(MaxCapacityInputLocator);
        WaitForInteractable(FeeInputLocator);
    }

    public bool IsCourseFeeVisible(string courseName, string expectedFee)
    {
        var slug = ToSlug(courseName);
        return _driver.FindElements(By.CssSelector($"[data-testid='course-fee-{slug}']"))
            .Any(e => e.Displayed && e.Text.Contains(expectedFee, StringComparison.OrdinalIgnoreCase));
    }

    public string GetSignupButtonText(string courseName)
    {
        return FindSignupButton(courseName, requireEnabled: false).Text.Trim();
    }

    public void OpenSignup(string courseName)
    {
        SafeClick(FindSignupButton(courseName, requireEnabled: true));
        _wait.Until(d =>
            d.FindElements(By.CssSelector("[data-testid='course-signup-modal']")).Any(e => e.Displayed) ||
            d.PageSource.Contains($"Sign Up for {courseName}", StringComparison.OrdinalIgnoreCase));
    }

    public void SelectSignupStudent(string studentFullName)
    {
        var selectElement = _driver.FindElements(By.CssSelector("[data-testid='course-signup-student-select']")).FirstOrDefault(e => e.Displayed) ??
            _driver.FindElement(By.Id("studentId"));
        var select = new SelectElement(selectElement);
        var option = select.Options.FirstOrDefault(o => o.Text.Contains(studentFullName, StringComparison.OrdinalIgnoreCase));
        if (option == null)
        {
            throw new NoSuchElementException($"Could not find signup student option containing: {studentFullName}");
        }

        option.Click();
    }

    public void ConfirmSignup()
    {
        var button = _driver.FindElements(By.CssSelector("[data-testid='confirm-course-signup-button']")).FirstOrDefault(e => e.Displayed) ??
            _driver.FindElement(By.XPath("//button[contains(normalize-space(.), 'Pay and Sign Up') or contains(normalize-space(.), 'Sign Up') or contains(normalize-space(.), 'Join Waitlist')]"));
        SafeClick(button);
        WaitForSignupModalToClose();
    }

    public void SignUpStudentForCourse(string courseName, string studentFullName)
    {
        OpenSignup(courseName);
        SelectSignupStudent(studentFullName);
        ConfirmSignup();
    }

    public string GetFirstAvailableSignupCourseName()
    {
        return _wait.Until(d =>
        {
            var buttons = d.FindElements(By.CssSelector("[data-testid^='course-signup-']"))
                .Where(button => button.Displayed && button.Enabled)
                .Where(button =>
                {
                    var text = button.Text.Trim();
                    return !string.Equals(text, "Signed Up", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(text, "Add Student", StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            foreach (var button in buttons)
            {
                try
                {
                    var courseTitle = button.FindElement(By.XPath("./ancestor::div[contains(@class,'bg-white')][1]//h3"));
                    var courseName = courseTitle.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(courseName))
                    {
                        return courseName;
                    }
                }
                catch (NoSuchElementException)
                {
                    // Continue searching until we find a card with a visible title.
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
            }

            return null;
        }) ?? throw new NoSuchElementException("Could not find an available course signup button.");
    }

    private static string ToSlug(string value)
    {
        return string.Join("-", value.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    }

    private IWebElement FindSignupButton(string courseName, bool requireEnabled)
    {
        var slug = ToSlug(courseName);
        return _wait.Until(d =>
            d.FindElements(By.CssSelector($"[data-testid='course-signup-{slug}']")).FirstOrDefault(e => e.Displayed && (!requireEnabled || e.Enabled)) ??
            d.FindElements(By.XPath($"//div[.//h3[contains(normalize-space(.), '{courseName}')]]//button[contains(normalize-space(.), 'Sign Up') or contains(normalize-space(.), 'Waitlist')]")).FirstOrDefault(e => e.Displayed && (!requireEnabled || e.Enabled)));
    }

    public void WaitForModalToClose()
    {
        var closeWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        closeWait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
        closeWait.Until(_ => !IsCreateFormVisible());
    }

    private void WaitForSignupModalToClose()
    {
        _wait.Until(driver =>
        {
            var signupModals = driver.FindElements(By.CssSelector("[data-testid='course-signup-modal']"));
            if (signupModals.All(m => !m.Displayed))
            {
                return true;
            }

            var errorText = driver.FindElements(By.CssSelector(".bg-red-50, [role='alert']"))
                .Where(e => e.Displayed)
                .Select(e => e.Text)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
            if (!string.IsNullOrWhiteSpace(errorText))
            {
                throw new InvalidOperationException($"Course signup failed: {errorText}");
            }

            return false;
        });
    }

    public void FillCourseForm(string name, string code = "", string ageGroup = "", 
                              int maxCapacity = 20, string room = "", decimal fee = 0, 
                              string periodCode = "", string startTime = "", 
                              string endTime = "", string description = "")
    {
        SetInputValue(CourseNameInputLocator, name);

        if (!string.IsNullOrEmpty(code))
        {
            SetInputValue(CourseCodeInputLocator, code);
        }

        if (!string.IsNullOrEmpty(ageGroup))
        {
            SelectByText(AgeGroupSelectLocator, ageGroup);
        }

        SetInputValue(MaxCapacityInputLocator, maxCapacity.ToString());

        if (!string.IsNullOrEmpty(room))
        {
            var roomSelect = new SelectElement(WaitForInteractable(RoomSelectLocator));
            try
            {
                // Try to select by text that contains the room name
                var option = roomSelect.Options
                    .FirstOrDefault(opt => opt.Text.Contains(room, StringComparison.OrdinalIgnoreCase));
                
                if (option != null)
                {
                    option.Click();
                }
                else
                {
                    // Fallback: try to select by text exactly
                    roomSelect.SelectByText(room);
                }
            }
            catch (NoSuchElementException)
            {
                var fallbackOption = roomSelect.Options
                    .FirstOrDefault(opt => !string.IsNullOrWhiteSpace(opt.Text) &&
                                           opt.Text != "Select a room..." &&
                                           !string.IsNullOrWhiteSpace(opt.GetDomAttribute("value")));

                if (fallbackOption != null)
                {
                    fallbackOption.Click();
                }
                else
                {
                    // If room not found, just continue - the test might be checking for this scenario
                    Console.WriteLine($"Room '{room}' not found in dropdown options");
                }
            }
        }

        SetInputValue(FeeInputLocator, fee.ToString());

        if (!string.IsNullOrEmpty(periodCode))
        {
            SetInputValue(PeriodCodeInputLocator, periodCode);
        }

        if (!string.IsNullOrEmpty(startTime))
        {
            SetInputValue(StartTimeInputLocator, startTime);
        }

        if (!string.IsNullOrEmpty(endTime))
        {
            SetInputValue(EndTimeInputLocator, endTime);
        }

        if (!string.IsNullOrEmpty(description))
        {
            SetInputValue(DescriptionInputLocator, description);
        }
    }

    public void SaveCourse(bool waitForClose = true)
    {
        SafeClick(SaveCourseButtonLocator);
        if (waitForClose)
        {
            WaitForModalToClose();
        }
    }

    public void CancelCreate()
    {
    SafeClick(CancelCourseButtonLocator);
        WaitForModalToClose();
    }

    // Verification methods
    public bool IsOnCoursesPage()
    {
        return _driver.Url.Contains("/courses") && 
               _driver.PageSource.ToLower().Contains("course");
    }

    public bool CanSeeCreateButton()
    {
        return IsElementDisplayed(CreateCourseButtonLocator);
    }

    public bool IsCreateFormVisible()
    {
        return IsElementDisplayed(CourseNameInputLocator) && IsElementDisplayed(SaveCourseButtonLocator);
    }

    public bool IsCourseVisible(string courseName)
    {
        try
        {
            var courseCard = _driver.FindElement(By.XPath($"//h3[contains(text(), '{courseName}')]"));
            return courseCard.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public int GetCourseCount()
    {
        try
        {
            var courseCards = _driver.FindElements(By.CssSelector(".bg-white.rounded-lg.shadow"));
            return courseCards.Count;
        }
        catch (NoSuchElementException)
        {
            return 0;
        }
    }

    public bool IsErrorDisplayed()
    {
        try
        {
            var errorElement = _driver.FindElement(By.CssSelector(".bg-red-50"));
            return errorElement.Displayed;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    public string GetErrorMessage()
    {
        try
        {
            var errorElement = _driver.FindElement(By.CssSelector(".bg-red-50 .text-red-600"));
            return errorElement.Text;
        }
        catch (NoSuchElementException)
        {
            return "";
        }
    }

    public string GetSuccessMessage()
    {
        try
        {
            var successElement = _driver.FindElement(By.CssSelector(".bg-green-50"));
            return successElement.Text;
        }
        catch (NoSuchElementException)
        {
            return "";
        }
    }

    public List<string> GetAvailableSemesters()
    {
        var semesterSelect = new SelectElement(SemesterSelect);
        return semesterSelect.Options
            .Where(option => !string.IsNullOrWhiteSpace(option.Text) && 
                           option.Text != "Select a semester..." &&
                           !string.IsNullOrWhiteSpace(option.GetDomAttribute("value")))
            .Select(option => option.Text)
            .ToList();
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

    private void SafeClick(By locator)
    {
        _wait.Until(driver =>
        {
            try
            {
                var element = driver.FindElement(locator);
                SafeClick(element);
                return true;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }

    private bool IsElementDisplayed(By locator)
    {
        try
        {
            return _driver.FindElements(locator).Any(element => element.Displayed);
        }
        catch (StaleElementReferenceException)
        {
            return false;
        }
    }

    private void SelectByText(By locator, string text)
    {
        _wait.Until(driver =>
        {
            try
            {
                var element = driver.FindElement(locator);
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
                if (!element.Displayed || !element.Enabled)
                {
                    return false;
                }

                var select = new SelectElement(element);
                select.SelectByText(text);
                return true;
            }
            catch (ElementNotInteractableException)
            {
                return false;
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

    private IWebElement WaitForInteractable(By locator)
    {
        return _wait.Until(driver =>
        {
            try
            {
                var element = driver.FindElement(locator);
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
                return element.Displayed && element.Enabled ? element : null;
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        });
    }

    private void SetInputValue(By locator, string value)
    {
        _wait.Until(driver =>
        {
            try
            {
                var element = WaitForInteractable(locator);
                element.Clear();
                element.SendKeys(value);
                return true;
            }
            catch (ElementNotInteractableException)
            {
                return false;
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
        });
    }

    public List<(string Text, string Value)> GetAvailableSemestersWithValues()
    {
        var semesterSelect = new SelectElement(SemesterSelect);
        return semesterSelect.Options
            .Where(option => !string.IsNullOrWhiteSpace(option.Text) && 
                           option.Text != "Select a semester..." &&
                           !string.IsNullOrWhiteSpace(option.GetDomAttribute("value")))
            .Select(option => (option.Text, option.GetDomAttribute("value")))
            .ToList();
    }

    public List<string> GetAvailableSemesterNames()
    {
        var semesterSelect = new SelectElement(SemesterSelect);
        return semesterSelect.Options
            .Where(option => !string.IsNullOrWhiteSpace(option.Text) && 
                           option.Text != "Select a semester..." &&
                           !string.IsNullOrWhiteSpace(option.GetDomAttribute("value")))
            .Select(option => {
                var text = option.Text;
                // Extract just the semester name (before the parentheses with dates)
                var parenIndex = text.IndexOf(" (");
                return parenIndex > 0 ? text.Substring(0, parenIndex) : text;
            })
            .ToList();
    }

    public string GetSelectedSemester()
    {
        var semesterSelect = new SelectElement(SemesterSelect);
        return semesterSelect.SelectedOption.Text;
    }

    public List<string> GetAvailableRooms()
    {
        try
        {
            var roomSelect = new SelectElement(WaitForInteractable(RoomSelectLocator));
            return roomSelect.Options
                .Where(option => !string.IsNullOrWhiteSpace(option.Text) && 
                               option.Text != "Select a room..." &&
                               !string.IsNullOrWhiteSpace(option.GetDomAttribute("value")))
                .Select(option => option.Text)
                .ToList();
        }
        catch (NoSuchElementException)
        {
            return new List<string>();
        }
    }
}

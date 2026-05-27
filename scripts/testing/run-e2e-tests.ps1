[CmdletBinding()]
param(
    [switch]$Headless,
    [switch]$SetupUsers,
    [ValidateSet('all', 'admin', 'educator', 'member', 'login')]
    [string]$TestSuite = 'all',
    [switch]$NoTests,
    [string]$AdminPassword,
    [string]$KeycloakUrl,
    [string]$RealmName,
    [string]$BaseUrl = 'http://localhost:3001'
)

$ErrorActionPreference = 'Stop'

$script:ScriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:ProjectRoot = Resolve-Path (Join-Path $script:ScriptDirectory '..\..')
$script:TestProject = Join-Path $script:ProjectRoot 'tests\StudentRegistrar.E2E.Tests\StudentRegistrar.E2E.Tests.csproj'

function Write-Status([string]$Message) {
    Write-Host "[e2e] $Message" -ForegroundColor Cyan
}

function Write-Success([string]$Message) {
    Write-Host "[e2e] $Message" -ForegroundColor Green
}

function Write-ErrorAndExit([string]$Message) {
    Write-Host "[e2e] $Message" -ForegroundColor Red
    exit 1
}

function Test-ApplicationRunning {
    Write-Status "Checking application at $BaseUrl"

    try {
        Invoke-WebRequest -Uri $BaseUrl -UseBasicParsing -TimeoutSec 5 | Out-Null
        Write-Success "Application is running at $BaseUrl"
        return
    } catch {
        Write-ErrorAndExit "Application is not reachable at $BaseUrl. Start the app stack first."
    }
}

function Invoke-SetupUsers {
    $setupScript = Join-Path $script:ScriptDirectory 'setup-test-users.ps1'
    if (-not (Test-Path $setupScript)) {
        Write-ErrorAndExit 'setup-test-users.ps1 was not found.'
    }

    Write-Status 'Setting up test users in Keycloak'

    $parameters = @{}
    if (-not [string]::IsNullOrWhiteSpace($AdminPassword)) {
        $parameters.AdminPassword = $AdminPassword
    }
    if (-not [string]::IsNullOrWhiteSpace($KeycloakUrl)) {
        $parameters.KeycloakUrl = $KeycloakUrl
    }
    if (-not [string]::IsNullOrWhiteSpace($RealmName)) {
        $parameters.RealmName = $RealmName
    }

    & $setupScript @parameters
    Write-Success 'Test user setup completed'
}

function Get-TestFilter {
    switch ($TestSuite) {
        'login' { return 'FullyQualifiedName~LoginTests' }
        'admin' { return 'FullyQualifiedName~AdminTests' }
        'educator' { return 'FullyQualifiedName~EducatorTests' }
        'member' { return 'FullyQualifiedName~MemberTests' }
        default { return $null }
    }
}

function Invoke-E2ETests {
    Write-Status 'Running E2E tests'

    $previousHeadless = $null
    $hadPreviousHeadless = Test-Path Env:SeleniumSettings__Headless
    if ($hadPreviousHeadless) {
        $previousHeadless = $env:SeleniumSettings__Headless
    }

    $previousBaseUrl = $null
    $hadPreviousBaseUrl = Test-Path Env:SeleniumSettings__BaseUrl
    if ($hadPreviousBaseUrl) {
        $previousBaseUrl = $env:SeleniumSettings__BaseUrl
    }

    try {
        $env:SeleniumSettings__Headless = if ($Headless) { 'true' } else { 'false' }
        $env:SeleniumSettings__BaseUrl = $BaseUrl

        if ($Headless) {
            Write-Status 'Running in headless mode'
        } else {
            Write-Status 'Running with browser visible'
        }

        if ($TestSuite -eq 'all') {
            Write-Status 'Running all E2E tests'
        } else {
            Write-Status "Running test suite: $TestSuite"
        }

        $arguments = @(
            'test'
            $script:TestProject
            '--logger'
            'console;verbosity=normal'
            '--collect:XPlat Code Coverage'
        )

        $testFilter = Get-TestFilter
        if ($null -ne $testFilter) {
            $arguments += @('--filter', $testFilter)
        }

        Push-Location $script:ProjectRoot
        try {
            & dotnet @arguments
            if ($LASTEXITCODE -ne 0) {
                Write-ErrorAndExit "Tests failed with exit code $LASTEXITCODE"
            }
        } finally {
            Pop-Location
        }

        Write-Success 'All requested E2E tests passed'
    } finally {
        if ($hadPreviousHeadless) {
            $env:SeleniumSettings__Headless = $previousHeadless
        } else {
            Remove-Item Env:SeleniumSettings__Headless -ErrorAction SilentlyContinue
        }

        if ($hadPreviousBaseUrl) {
            $env:SeleniumSettings__BaseUrl = $previousBaseUrl
        } else {
            Remove-Item Env:SeleniumSettings__BaseUrl -ErrorAction SilentlyContinue
        }
    }
}

Write-Host 'E2E Testing Setup and Execution' -ForegroundColor White
Write-Host '===============================' -ForegroundColor White

Test-ApplicationRunning

if ($SetupUsers) {
    Invoke-SetupUsers
}

if (-not $NoTests) {
    Invoke-E2ETests
}

Write-Success 'E2E testing workflow completed'
#Requires -Version 7.0

[CmdletBinding()]
param(
    [switch]$PreflightOnly,
    [switch]$KeepRunning,
    [string]$Filter,
    [switch]$SkipSampleSeed
)

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $PSCommandPath
$projectRoot = Resolve-Path (Join-Path $scriptDir '..\..')

function Set-DefaultEnv {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($Name, 'Process'))) {
        [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
    }
}

function Write-Step([string]$Message) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message"
}

function Wait-HttpReady {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [string]$Url,
        [int]$Attempts = 60,
        [int]$DelaySeconds = 3
    )

    Write-Step "Waiting for $Name..."
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5 | Out-Null
            return
        }
        catch {
            if ($attempt -eq $Attempts) {
                throw "$Name did not become ready at $Url."
            }

            Start-Sleep -Seconds $DelaySeconds
        }
    }
}

function Invoke-Checked {
    param(
        [Parameter(Mandatory)] [string]$FilePath,
        [Parameter()] [string[]]$Arguments = @()
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

Set-DefaultEnv -Name 'KEYCLOAK_ADMIN_PASSWORD' -Value 'admin123!'
Set-DefaultEnv -Name 'POSTGRES_PASSWORD' -Value 'postgres123!'
Set-DefaultEnv -Name 'KEYCLOAK_CLIENT_SECRET' -Value 'student-registrar-local-dev-secret'
Set-DefaultEnv -Name 'KEYCLOAK_URL' -Value 'http://localhost:8080'
Set-DefaultEnv -Name 'KEYCLOAK_REALM' -Value 'student-registrar'
Set-DefaultEnv -Name 'KEYCLOAK_ADMIN_USERNAME' -Value 'admin'
Set-DefaultEnv -Name 'API_BASE_URL' -Value 'http://localhost:5000'
Set-DefaultEnv -Name 'SeleniumSettings__BaseUrl' -Value 'http://localhost:3000'
Set-DefaultEnv -Name 'SeleniumSettings__Headless' -Value 'true'
Set-DefaultEnv -Name 'DB_PASSWORD' -Value $env:POSTGRES_PASSWORD
Set-DefaultEnv -Name 'SEED_DATABASE_RESET' -Value 'true'

Push-Location $projectRoot
try {
    Write-Step 'Running Windows PowerShell CI-equivalent core E2E validation'
    Write-Step "Project root: $projectRoot"
    Write-Step "Frontend URL: $env:SeleniumSettings__BaseUrl"

    docker compose down -v
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose down failed with exit code $LASTEXITCODE."
    }

    $started = $false
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Write-Step "Building images and starting core services (attempt $attempt/3)..."
        docker compose --progress plain up --build -d postgres keycloak api frontend
        if ($LASTEXITCODE -eq 0) {
            $started = $true
            break
        }

        if ($attempt -lt 3) {
            docker compose down -v | Out-Null
            Start-Sleep -Seconds 10
        }
    }

    if (-not $started) {
        throw 'Failed to start core services after 3 attempts.'
    }

    Wait-HttpReady -Name 'Keycloak' -Url "$env:KEYCLOAK_URL/realms/master"
    Wait-HttpReady -Name 'frontend' -Url $env:SeleniumSettings__BaseUrl

    $postgresContainer = (docker compose ps -q postgres).Trim()
    if ([string]::IsNullOrWhiteSpace($postgresContainer)) {
        throw 'PostgreSQL compose container was not found after service startup.'
    }
    [Environment]::SetEnvironmentVariable('DB_CONTAINER', $postgresContainer, 'Process')
    [Environment]::SetEnvironmentVariable('POSTGRES_CONTAINER', $postgresContainer, 'Process')

    Write-Step 'Bootstrapping Keycloak realm and E2E users...'
    & (Join-Path $projectRoot 'scripts\keycloak\bootstrap-keycloak.ps1') `
        -KeycloakUrl $env:KEYCLOAK_URL `
        -AdminUsername $env:KEYCLOAK_ADMIN_USERNAME `
        -Realm $env:KEYCLOAK_REALM `
        -InitialAdminUsername 'scoopadmin' `
        -InitialAdminEmail 'scoopadmin@example.com' `
        -InitialAdminTempPass 'ChangeThis123!' `
        -ClientSecret $env:KEYCLOAK_CLIENT_SECRET
    if ($LASTEXITCODE -ne 0) {
        throw "bootstrap-keycloak.ps1 failed with exit code $LASTEXITCODE."
    }

    & (Join-Path $projectRoot 'scripts\testing\setup-test-users.ps1') `
        -KeycloakUrl $env:KEYCLOAK_URL `
        -RealmName $env:KEYCLOAK_REALM `
        -AdminUser $env:KEYCLOAK_ADMIN_USERNAME `
        -AdminPassword $env:KEYCLOAK_ADMIN_PASSWORD `
        -DbContainer $postgresContainer `
        -DbPassword $env:POSTGRES_PASSWORD
    if ($LASTEXITCODE -ne 0) {
        throw "setup-test-users.ps1 failed with exit code $LASTEXITCODE."
    }

    $seedScript = Join-Path $projectRoot 'scripts\testing\seed-database.ps1'
    if (-not $SkipSampleSeed -and (Test-Path -LiteralPath $seedScript)) {
        Write-Step 'Seeding sample E2E data...'
        & $seedScript `
            -DbContainer $postgresContainer `
            -DbPassword $env:POSTGRES_PASSWORD `
            -KeycloakUrl $env:KEYCLOAK_URL `
            -KeycloakRealm $env:KEYCLOAK_REALM `
            -KeycloakAdminUser $env:KEYCLOAK_ADMIN_USERNAME `
            -KeycloakAdminPassword $env:KEYCLOAK_ADMIN_PASSWORD `
            -Reset
        if ($LASTEXITCODE -ne 0) {
            throw "seed-database.ps1 failed with exit code $LASTEXITCODE."
        }
    }
    elseif (-not $PreflightOnly -and -not $SkipSampleSeed) {
        throw 'Full E2E requires scripts/testing/seed-database.ps1 for Windows parity. Run with -SkipSampleSeed only for focused suites that do not need sample data.'
    }

    Invoke-Checked -FilePath (Join-Path $projectRoot 'scripts\testing\preflight-ci-e2e.ps1')

    if (-not $PreflightOnly) {
        $testArgs = @('test', 'tests/StudentRegistrar.E2E.Tests/', '--logger', 'console;verbosity=normal')
        if (-not [string]::IsNullOrWhiteSpace($Filter)) {
            $testArgs += @('--filter', $Filter)
        }

        dotnet @testArgs
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code $LASTEXITCODE."
        }
    }

    Write-Step 'CI-equivalent core E2E validation completed.'
}
catch {
    Write-Host ''
    Write-Error $_
    Write-Host ''
    Write-Host 'CI-equivalent E2E run failed. Recent compose logs:'
    docker compose logs --tail 200 | Out-Host
    exit 1
}
finally {
    if (-not $KeepRunning) {
        docker compose down -v | Out-Null
    }
    else {
        Write-Host 'Keeping compose services running for inspection.'
    }

    Pop-Location
}

[CmdletBinding()]
param(
    [switch]$Watch
)

$ErrorActionPreference = 'Stop'

function Write-Status([string]$Message) {
    Write-Host $Message
}

Write-Status 'Running Student Registrar Tests'
Write-Status '================================='

if ($Watch) {
    Write-Status 'Running in watch mode...'
    Push-Location $PSScriptRoot
    try {
        & dotnet watch test .\tests\StudentRegistrar.Models.Tests\StudentRegistrar.Models.Tests.csproj
    } finally {
        Pop-Location
    }

    exit $LASTEXITCODE
}

Write-Status 'Running model and API test projects...'

Push-Location $PSScriptRoot
try {
    & dotnet test .\tests\StudentRegistrar.Models.Tests\StudentRegistrar.Models.Tests.csproj --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    & dotnet test .\tests\StudentRegistrar.Api.Tests\StudentRegistrar.Api.Tests.csproj --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}

Write-Status ''
Write-Status 'Test Summary:'
Write-Status '- Model and API test projects'
Write-Status '- Browser E2E and smoke projects are not invoked by default'
Write-Status ''
Write-Status 'All tests should be passing!'
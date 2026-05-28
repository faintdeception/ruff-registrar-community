[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Write-Host 'Running Student Registrar Smoke Tests'
Write-Host '======================================='
Write-Host 'Required environment variables:'
Write-Host '  SMOKE_WEB_URL, SMOKE_API_URL, SMOKE_KEYCLOAK_URL, SMOKE_USERNAME, SMOKE_PASSWORD'
Write-Host 'Optional environment variables:'
Write-Host '  SMOKE_REALM, SMOKE_CLIENT_ID, SMOKE_CLIENT_SECRET'

Push-Location $PSScriptRoot
try {
    & dotnet test .\tests\StudentRegistrar.Smoke.Tests\ --verbosity normal
    exit $LASTEXITCODE
} finally {
    Pop-Location
}
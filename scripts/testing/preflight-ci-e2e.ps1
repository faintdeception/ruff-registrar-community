#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$KeycloakUrl = $(if ($env:KEYCLOAK_URL) { $env:KEYCLOAK_URL } else { 'http://localhost:8080' }),
    [string]$RealmName = $(if ($env:KEYCLOAK_REALM) { $env:KEYCLOAK_REALM } else { 'student-registrar' }),
    [string]$KeycloakAdminUser = $(if ($env:KEYCLOAK_ADMIN_USERNAME) { $env:KEYCLOAK_ADMIN_USERNAME } elseif ($env:KEYCLOAK_ADMIN_USER) { $env:KEYCLOAK_ADMIN_USER } else { 'admin' }),
    [string]$KeycloakAdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD,
    [string]$ClientId = $(if ($env:KEYCLOAK_CLIENT_ID) { $env:KEYCLOAK_CLIENT_ID } else { 'student-registrar' }),
    [string]$ClientSecret = $(if ($env:KEYCLOAK_CLIENT_SECRET) { $env:KEYCLOAK_CLIENT_SECRET } else { 'student-registrar-local-dev-secret' }),
    [string]$ApiBaseUrl = $(if ($env:API_BASE_URL) { $env:API_BASE_URL } else { 'http://localhost:5000' }),
    [string]$DbContainer = $env:DB_CONTAINER,
    [string]$DbName = $(if ($env:DB_NAME) { $env:DB_NAME } else { 'studentregistrar' }),
    [string]$DbUser = $(if ($env:DB_USER) { $env:DB_USER } else { 'postgres' }),
    [string]$DbPassword = $(if ($env:DB_PASSWORD) { $env:DB_PASSWORD } elseif ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { 'postgres123!' }),
    [string]$AdminUsername = $(if ($env:E2E_ADMIN_USERNAME) { $env:E2E_ADMIN_USERNAME } else { 'admin1' }),
    [string]$AdminPassword = $(if ($env:E2E_ADMIN_PASSWORD) { $env:E2E_ADMIN_PASSWORD } else { 'AdminPass123!' })
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    Write-Host "==> $Message"
}

function Get-RequiredValue {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [AllowNull()] [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "$Name is required for E2E preflight."
    }

    $Value
}

function Resolve-PostgresContainer {
    if (-not [string]::IsNullOrWhiteSpace($DbContainer)) {
        return $DbContainer
    }

    $container = docker compose ps -q postgres
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($container)) {
        throw 'PostgreSQL compose container was not found.'
    }

    $container.Trim()
}

function Get-KeycloakToken {
    param(
        [Parameter(Mandatory)] [string]$Url,
        [Parameter(Mandatory)] [string]$Realm,
        [Parameter(Mandatory)] [hashtable]$Body
    )

    $response = Invoke-RestMethod `
        -Method Post `
        -Uri "$Url/realms/$Realm/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body $Body

    if ([string]::IsNullOrWhiteSpace($response.access_token)) {
        throw "Token endpoint returned no access_token for realm '$Realm'."
    }

    $response
}

function Invoke-PostgresScalar {
    param(
        [Parameter(Mandatory)] [string]$Container,
        [Parameter(Mandatory)] [string]$Sql
    )

    $arguments = @(
        'exec',
        '-e', "PGPASSWORD=$DbPassword",
        $Container,
        'psql',
        '-U', $DbUser,
        '-d', $DbName,
        '-t',
        '-A',
        '-c', $Sql
    )

    $result = & docker @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "psql preflight query failed with exit code $LASTEXITCODE."
    }

    ($result | Select-Object -First 1).Trim()
}

$KeycloakUrl = $KeycloakUrl.TrimEnd('/')
$KeycloakAdminPassword = Get-RequiredValue -Name 'KEYCLOAK_ADMIN_PASSWORD' -Value $KeycloakAdminPassword

Write-Step 'Running CI-equivalent E2E preflight'

$adminTokenResponse = Get-KeycloakToken -Url $KeycloakUrl -Realm 'master' -Body @{
    username = $KeycloakAdminUser
    password = $KeycloakAdminPassword
    grant_type = 'password'
    client_id = 'admin-cli'
}
$headers = @{ Authorization = "Bearer $($adminTokenResponse.access_token)" }

$clients = Invoke-RestMethod `
    -Method Get `
    -Uri "$KeycloakUrl/admin/realms/$RealmName/clients?clientId=$([uri]::EscapeDataString($ClientId))" `
    -Headers $headers
$client = @($clients)[0]
if (-not $client) {
    throw "Keycloak client '$ClientId' was not found in realm '$RealmName'."
}

if ($client.directAccessGrantsEnabled -ne $true) {
    throw "Keycloak client '$ClientId' must have directAccessGrantsEnabled=true for app login."
}

Get-KeycloakToken -Url $KeycloakUrl -Realm $RealmName -Body @{
    username = $AdminUsername
    password = $AdminPassword
    grant_type = 'password'
    client_id = $ClientId
    client_secret = $ClientSecret
} | Out-Null

$users = Invoke-RestMethod `
    -Method Get `
    -Uri "$KeycloakUrl/admin/realms/$RealmName/users?username=$([uri]::EscapeDataString($AdminUsername))&exact=true" `
    -Headers $headers
$adminUser = @($users)[0]
if (-not $adminUser -or [string]::IsNullOrWhiteSpace($adminUser.id)) {
    throw "Keycloak user '$AdminUsername' was not found."
}

$container = Resolve-PostgresContainer
$keycloakIdLiteral = $adminUser.id.Replace("'", "''")
$dbMatchSql = 'SELECT COUNT(*) FROM "Users" WHERE "KeycloakId" = ''{0}'';' -f $keycloakIdLiteral
$dbMatchCount = Invoke-PostgresScalar -Container $container -Sql $dbMatchSql
if ($dbMatchCount -ne '1') {
    throw "App database does not contain exactly one user mapped to Keycloak id '$($adminUser.id)' for '$AdminUsername'."
}

$loginBody = @{
    email = $AdminUsername
    password = $AdminPassword
} | ConvertTo-Json -Depth 4 -Compress

$loginResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "$($ApiBaseUrl.TrimEnd('/'))/auth/login" `
    -ContentType 'application/json' `
    -Body $loginBody

if ($loginResponse.success -ne $true) {
    throw 'API /auth/login preflight did not return success=true.'
}

Write-Host 'CI-equivalent E2E preflight passed.'

#Requires -Version 7.0

<#
.SYNOPSIS
    Prepares a Keycloak realm for the public "click to try" demo login. Idempotent; safe to run daily.

.DESCRIPTION
    - Enables direct access grants on the SPA client (this realm only).
    - Disables the account-console client so visitors cannot change the shared password.
    - Disables brute-force protection, because the demo password is public and lockouts would be a denial of service.
    - Creates demo-admin / demo-member if missing (existing users and their ids are kept) and resets their password.
    - Prints each user's Keycloak id. The app database needs a User row with that KeycloakId in the demo tenant
      (Administrator / Member) or the API will reject the login.

    Only run this against the demo tenant's realm. Never against a real organization's realm.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Realm,
    [string]$KeycloakUrl = $(if ($env:KEYCLOAK_URL) { $env:KEYCLOAK_URL } else { 'http://localhost:8080' }),
    [string]$AdminUsername = $(if ($env:KEYCLOAK_ADMIN_USER) { $env:KEYCLOAK_ADMIN_USER } else { 'admin' }),
    [string]$AdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD,
    [string]$DemoPassword = $env:DEMO_LOGIN_PASSWORD,
    [string]$ClientId = 'student-registrar-spa',
    [switch]$SkipCertificateCheck
)

$ErrorActionPreference = 'Stop'
$KeycloakUrl = $KeycloakUrl.TrimEnd('/')
$PSDefaultParameterValues['Invoke-RestMethod:SkipCertificateCheck'] = [bool]$SkipCertificateCheck

if ([string]::IsNullOrWhiteSpace($AdminPassword)) { throw 'Set KEYCLOAK_ADMIN_PASSWORD or pass -AdminPassword.' }
if ([string]::IsNullOrWhiteSpace($DemoPassword)) { throw 'Set DEMO_LOGIN_PASSWORD or pass -DemoPassword.' }

$token = (Invoke-RestMethod -Method Post -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
    -ContentType 'application/x-www-form-urlencoded' `
    -Body @{ username = $AdminUsername; password = $AdminPassword; grant_type = 'password'; client_id = 'admin-cli' }).access_token
$headers = @{ Authorization = "Bearer $token" }
$admin = "$KeycloakUrl/admin/realms/$Realm"

function Invoke-Admin {
    param([string]$Method, [string]$Path, $Body)
    $parameters = @{ Method = $Method; Uri = "$admin$Path"; Headers = $headers }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = ConvertTo-Json -InputObject $Body -Depth 10 -Compress
    }
    Invoke-RestMethod @parameters
}

# SPA client: allow the password grant used by the demo buttons.
$client = (Invoke-Admin Get "/clients?clientId=$ClientId" | ForEach-Object { $_ } | Select-Object -First 1)
if (-not $client) { throw "Client '$ClientId' not found in realm '$Realm'." }
if (-not $client.directAccessGrantsEnabled) {
    $client.directAccessGrantsEnabled = $true
    Invoke-Admin Put "/clients/$($client.id)" $client | Out-Null
    Write-Host "Enabled direct access grants on '$ClientId'."
}

# Stop visitors from changing the shared demo password via the self-service account console.
$accountConsole = (Invoke-Admin Get '/clients?clientId=account-console' | ForEach-Object { $_ } | Select-Object -First 1)
if ($accountConsole -and $accountConsole.enabled) {
    $accountConsole.enabled = $false
    Invoke-Admin Put "/clients/$($accountConsole.id)" $accountConsole | Out-Null
    Write-Host "Disabled 'account-console' client."
}

Invoke-Admin Put '' @{ realm = $Realm; bruteForceProtected = $false } | Out-Null

$accounts = @(
    @{ Username = 'demo-admin';  First = 'Demo'; Last = 'Organizer'; Role = 'Administrator' },
    @{ Username = 'demo-member'; First = 'Demo'; Last = 'Family';    Role = 'Member' }
)

foreach ($account in $accounts) {
    $user = (Invoke-Admin Get "/users?username=$($account.Username)&exact=true" | ForEach-Object { $_ } | Select-Object -First 1)
    if (-not $user) {
        Invoke-Admin Post '/users' @{
            username = $account.Username
            email = "$($account.Username)@demo.invalid"
            firstName = $account.First
            lastName = $account.Last
            enabled = $true
            emailVerified = $true
        } | Out-Null
        $user = (Invoke-Admin Get "/users?username=$($account.Username)&exact=true" | ForEach-Object { $_ } | Select-Object -First 1)
        Write-Host "Created $($account.Username)."
    }

    Invoke-Admin Put "/users/$($user.id)/reset-password" @{ type = 'password'; value = $DemoPassword; temporary = $false } | Out-Null

    $role = Invoke-Admin Get "/roles/$($account.Role)"
    $existing = @(Invoke-Admin Get "/users/$($user.id)/role-mappings/realm" | ForEach-Object { $_ } | ForEach-Object { $_.name })
    if ($existing -notcontains $role.name) {
        Invoke-Admin Post "/users/$($user.id)/role-mappings/realm" @(@{ id = $role.id; name = $role.name }) | Out-Null
    }

    Write-Host "$($account.Username)  role=$($account.Role)  KeycloakId=$($user.id)"
}

Write-Host 'Done. Ensure the app database has User rows with these KeycloakIds in the demo tenant.'

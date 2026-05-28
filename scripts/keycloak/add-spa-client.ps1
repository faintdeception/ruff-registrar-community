#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$Realm = 'student-registrar',
    [string]$KeycloakUrl = 'http://localhost:8080',
    [string]$AdminUsername = 'admin',
    [string]$ClientId = 'student-registrar-spa',
    [string[]]$RedirectUris = $(if ($env:REDIRECT_URIS) { $env:REDIRECT_URIS -split ',' } else { @('http://localhost:3000/*', 'http://localhost:3001/*') }),
    [string[]]$WebOrigins = $(if ($env:WEB_ORIGINS) { $env:WEB_ORIGINS -split ',' } else { @('http://localhost:3000', 'http://localhost:3001') }),
    [string]$AdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

function Write-Usage {
    @"
Usage: ./scripts/keycloak/add-spa-client.ps1 [options]
  -Realm NAME          Realm name (default: student-registrar)
  -KeycloakUrl URL     Base Keycloak URL (default: http://localhost:8080)
  -AdminUsername NAME  Master realm admin username (default: admin)
  -ClientId NAME       SPA client id (default: student-registrar-spa)
  -RedirectUris CSV    Redirect URIs as a PowerShell array or comma-separated env var
  -WebOrigins CSV      Web origins as a PowerShell array or comma-separated env var
  -Help                Show this help

Environment:
  KEYCLOAK_ADMIN_PASSWORD   Master admin password (skips prompt)
  REDIRECT_URIS             Comma-separated redirect URIs (overrides defaults)
  WEB_ORIGINS               Comma-separated web origins (overrides defaults)
"@
}

if ($Help) {
    Write-Usage
    exit 0
}

function Normalize-StringArray {
    param([string[]]$Values)

    @($Values |
        ForEach-Object { $_ -split ',' } |
        ForEach-Object { $_.Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

function Get-AdminPasswordValue {
    if (-not [string]::IsNullOrWhiteSpace($AdminPassword)) {
        return $AdminPassword
    }

    $securePassword = Read-Host "Enter Keycloak master admin password for user '$AdminUsername'" -AsSecureString
    return ([pscredential]::new('keycloak-admin', $securePassword)).GetNetworkCredential().Password
}

$RedirectUris = Normalize-StringArray -Values $RedirectUris
$WebOrigins = Normalize-StringArray -Values $WebOrigins
$AdminPassword = Get-AdminPasswordValue

$tokenResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "$($KeycloakUrl.TrimEnd('/'))/realms/master/protocol/openid-connect/token" `
    -ContentType 'application/x-www-form-urlencoded' `
    -Body @{
        username   = $AdminUsername
        password   = $AdminPassword
        client_id  = 'admin-cli'
        grant_type = 'password'
    }

$token = $tokenResponse.access_token
if ([string]::IsNullOrWhiteSpace($token)) {
    throw 'Failed to obtain admin token'
}

$headers = @{ Authorization = "Bearer $token" }
$baseUrl = $KeycloakUrl.TrimEnd('/')
$clientLookup = Invoke-RestMethod -Method Get -Uri "$baseUrl/admin/realms/$Realm/clients?clientId=$([uri]::EscapeDataString($ClientId))" -Headers $headers
$clientUuid = @($clientLookup)[0].id

$clientPayload = @{
    clientId                  = $ClientId
    enabled                   = $true
    publicClient              = $true
    protocol                  = 'openid-connect'
    standardFlowEnabled       = $true
    directAccessGrantsEnabled = $false
    serviceAccountsEnabled    = $false
    redirectUris              = $RedirectUris
    webOrigins                = $WebOrigins
    attributes                = @{ 'pkce.code.challenge.method' = 'S256' }
} | ConvertTo-Json -Depth 10 -Compress

if ([string]::IsNullOrWhiteSpace($clientUuid)) {
    Write-Host "Creating SPA client '$ClientId'..."
    $response = Invoke-WebRequest -Method Post -Uri "$baseUrl/admin/realms/$Realm/clients" -Headers $headers -ContentType 'application/json' -Body $clientPayload -SkipHttpErrorCheck
    if (@(201, 409) -notcontains [int]$response.StatusCode) {
        throw "Failed to create client (HTTP $($response.StatusCode))"
    }

    Write-Host 'Client created'
} else {
    Write-Host "Updating SPA client '$ClientId'..."
    $response = Invoke-WebRequest -Method Put -Uri "$baseUrl/admin/realms/$Realm/clients/$clientUuid" -Headers $headers -ContentType 'application/json' -Body $clientPayload -SkipHttpErrorCheck
    if ([int]$response.StatusCode -ne 204) {
        throw "Failed to update client (HTTP $($response.StatusCode))"
    }

    Write-Host 'Client updated'
}
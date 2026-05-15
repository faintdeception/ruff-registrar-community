#Requires -Version 7.0

<#!
.SYNOPSIS
One-time bootstrap of Keycloak realm and first application administrator.

.DESCRIPTION
PowerShell equivalent of bootstrap-keycloak.sh.
- Creates realm from realm-student-registrar.template.json
- Creates initial app admin user and assigns Administrator role
- Configures service account realm-management roles for the API client
- Exits with code 10 when realm already exists (idempotency policy)

.PARAMETER Realm
Realm name. Defaults to 'student-registrar'.

.PARAMETER KeycloakUrl
Base Keycloak URL. Defaults to 'http://localhost:8080'.

.PARAMETER AdminUsername
Master realm admin username. Defaults to 'admin'.

.PARAMETER InitialAdminUsername
First application admin username. Prompted if omitted.

.PARAMETER InitialAdminEmail
First application admin email. Prompted if omitted.

.PARAMETER InitialAdminTempPass
First application admin temporary password. Prompted if omitted.

.PARAMETER ClientSecret
Optional confidential client secret to apply to the student-registrar client. Use for local/dev parity with docker-compose/AppHost settings.

.PARAMETER AdminPasswordFile
Path to file containing Keycloak master admin password.

.PARAMETER Help
Shows usage text.
#>
[CmdletBinding()]
param(
    [string]$Realm = 'student-registrar',
    [string]$KeycloakUrl = 'http://localhost:8080',
    [string]$AdminUsername = 'admin',
    [string]$InitialAdminUsername,
    [string]$InitialAdminEmail,
    [string]$InitialAdminTempPass,
    [string]$ClientSecret = $env:KEYCLOAK_CLIENT_SECRET,
    [string]$AdminPasswordFile,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

function Write-Usage {
    @"
Usage: ./scripts/keycloak/bootstrap-keycloak.ps1 [options]
  -Realm NAME                  Realm name (default: student-registrar)
  -KeycloakUrl URL             Base Keycloak URL (default: http://localhost:8080)
  -AdminUsername NAME          Master realm admin username (default: admin)
  -InitialAdminUsername NAME   First application admin username (non-interactive)
  -InitialAdminEmail EMAIL     First application admin email (non-interactive)
    -InitialAdminTempPass PWD    First app admin temp password (non-interactive)
    -ClientSecret SECRET         Optional secret for the student-registrar confidential client
  -AdminPasswordFile PATH      File containing Keycloak master admin password
  -Help                        Show this help

Environment overrides:
  KEYCLOAK_ADMIN_PASSWORD   Master admin password (skips prompt)
    INITIAL_ADMIN_TEMP_PASS   Temp password for first app admin (skips prompt if other fields supplied)
    KEYCLOAK_CLIENT_SECRET    Secret for the student-registrar confidential client

Behavior:
  - If realm already exists, exits with code 10.
  - If initial admin fields are missing, prompts interactively.
"@
}

function Invoke-Http {
    param(
        [Parameter(Mandatory)] [string]$Method,
        [Parameter(Mandatory)] [string]$Uri,
        [hashtable]$Headers,
        [AllowNull()]$Body,
        [string]$ContentType
    )

    $params = @{
        Method             = $Method
        Uri                = $Uri
        Headers            = $Headers
        SkipHttpErrorCheck = $true
        ErrorAction        = 'Stop'
    }

    if ($PSBoundParameters.ContainsKey('Body') -and $null -ne $Body) {
        $params.Body = $Body
    }

    if ($ContentType) {
        $params.ContentType = $ContentType
    }

    Invoke-WebRequest @params
}

if ($Help) {
    Write-Usage
    exit 0
}

$scriptDir = Split-Path -Parent $PSCommandPath
$templateJson = Join-Path $scriptDir 'realm-student-registrar.template.json'

if (-not (Test-Path -LiteralPath $templateJson)) {
    Write-Error "Template not found: $templateJson"
    exit 3
}

Write-Host "Keycloak bootstrap (realm: $Realm)"

# Resolve master admin password
$masterAdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD
if ($AdminPasswordFile) {
    if (-not (Test-Path -LiteralPath $AdminPasswordFile)) {
        Write-Error "Admin password file not found: $AdminPasswordFile"
        exit 2
    }
    $masterAdminPassword = (Get-Content -LiteralPath $AdminPasswordFile -Raw).Trim()
}

if ([string]::IsNullOrWhiteSpace($masterAdminPassword)) {
    $secure = Read-Host "Enter Keycloak master admin password for user '$AdminUsername'" -AsSecureString
    $masterAdminPassword = ([pscredential]::new('admin', $secure)).GetNetworkCredential().Password
}

# Obtain token
$tokenResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
    -ContentType 'application/x-www-form-urlencoded' `
    -Body @{
        username   = $AdminUsername
        password   = $masterAdminPassword
        client_id  = 'admin-cli'
        grant_type = 'password'
    }

$token = $tokenResponse.access_token
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Error 'Failed to obtain admin token'
    exit 4
}

$authHeaders = @{ Authorization = "Bearer $token" }

Write-Host 'Checking if realm already exists...'
$realmCheck = Invoke-Http -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm" -Headers $authHeaders
if ([int]$realmCheck.StatusCode -eq 200) {
    Write-Error "Realm '$Realm' already exists. Aborting (idempotency policy)."
    exit 10
}

Write-Host 'Creating realm from template...'
$templateBody = Get-Content -LiteralPath $templateJson -Raw
$createRealm = Invoke-Http -Method Post -Uri "$KeycloakUrl/admin/realms" -Headers $authHeaders -Body $templateBody -ContentType 'application/json'
if (@(201, 409) -notcontains [int]$createRealm.StatusCode) {
    Write-Error "Realm creation failed (HTTP $($createRealm.StatusCode))"
    exit 11
}

# Gather initial application admin values
if ([string]::IsNullOrWhiteSpace($InitialAdminUsername)) {
    $InitialAdminUsername = Read-Host 'Initial application admin username'
}
if ([string]::IsNullOrWhiteSpace($InitialAdminEmail)) {
    $InitialAdminEmail = Read-Host 'Initial application admin email'
}
if ([string]::IsNullOrWhiteSpace($InitialAdminTempPass)) {
    if (-not [string]::IsNullOrWhiteSpace($env:INITIAL_ADMIN_TEMP_PASS)) {
        $InitialAdminTempPass = $env:INITIAL_ADMIN_TEMP_PASS
    } else {
        $secure = Read-Host 'Initial application admin temporary password' -AsSecureString
        $InitialAdminTempPass = ([pscredential]::new('admin', $secure)).GetNetworkCredential().Password
    }
}

$userPayload = @{
    username      = $InitialAdminUsername
    email         = $InitialAdminEmail
    enabled       = $true
    emailVerified = $true
    firstName     = 'Admin'
    lastName      = 'User'
} | ConvertTo-Json -Depth 5 -Compress

$createUser = Invoke-Http -Method Post -Uri "$KeycloakUrl/admin/realms/$Realm/users" -Headers $authHeaders -Body $userPayload -ContentType 'application/json'
if ([int]$createUser.StatusCode -ne 201) {
    Write-Error "Failed to create initial admin user (HTTP $($createUser.StatusCode))"
    exit 12
}

$users = Invoke-RestMethod -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/users?username=$([uri]::EscapeDataString($InitialAdminUsername))" -Headers $authHeaders
$userId = @($users)[0].id
if ([string]::IsNullOrWhiteSpace($userId)) {
    Write-Error 'Could not retrieve new user id'
    exit 13
}

$resetBody = @{
    type      = 'password'
    temporary = $true
    value     = $InitialAdminTempPass
} | ConvertTo-Json -Depth 4 -Compress

$resetPass = Invoke-Http -Method Put -Uri "$KeycloakUrl/admin/realms/$Realm/users/$userId/reset-password" -Headers $authHeaders -Body $resetBody -ContentType 'application/json'
if ([int]$resetPass.StatusCode -ne 204) {
    Write-Error "Failed to set password (HTTP $($resetPass.StatusCode))"
    exit 14
}

$adminRole = Invoke-RestMethod -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/roles/Administrator" -Headers $authHeaders
$assignRoleBody = @(@{ id = $adminRole.id; name = 'Administrator' }) | ConvertTo-Json -Depth 4 -Compress -AsArray
$assignRole = Invoke-Http -Method Post -Uri "$KeycloakUrl/admin/realms/$Realm/users/$userId/role-mappings/realm" -Headers $authHeaders -Body $assignRoleBody -ContentType 'application/json'
if ([int]$assignRole.StatusCode -ne 204) {
    Write-Error "Failed to assign Administrator role (HTTP $($assignRole.StatusCode))"
    exit 15
}

$clientId = 'student-registrar'
Write-Host "Configuring service account permissions for client '$clientId'..."

$client = @(Invoke-RestMethod -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/clients?clientId=$clientId" -Headers $authHeaders)[0]
$clientUuid = $client.id

if ([string]::IsNullOrWhiteSpace($clientUuid)) {
    Write-Warning "Could not resolve client '$clientId'. Skipping service account role grants."
} else {
    if (-not [string]::IsNullOrWhiteSpace($ClientSecret)) {
        Write-Host "Configuring confidential client secret for '$clientId'..."
        $client | Add-Member -NotePropertyName secret -NotePropertyValue $ClientSecret -Force
        $client | Add-Member -NotePropertyName publicClient -NotePropertyValue $false -Force
        $client | Add-Member -NotePropertyName directAccessGrantsEnabled -NotePropertyValue $true -Force
        $client | Add-Member -NotePropertyName serviceAccountsEnabled -NotePropertyValue $true -Force
        $clientBody = $client | ConvertTo-Json -Depth 20 -Compress

        $setSecret = Invoke-Http -Method Put -Uri "$KeycloakUrl/admin/realms/$Realm/clients/$clientUuid" -Headers $authHeaders -Body $clientBody -ContentType 'application/json'
        if ([int]$setSecret.StatusCode -ne 204) {
            Write-Error "Failed to configure confidential client secret (HTTP $($setSecret.StatusCode))"
            exit 16
        }
    }

    $serviceAccountUser = Invoke-RestMethod -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/clients/$clientUuid/service-account-user" -Headers $authHeaders
    $serviceAccountUserId = $serviceAccountUser.id

    if ([string]::IsNullOrWhiteSpace($serviceAccountUserId)) {
        Write-Warning "Could not resolve service account user for client '$clientId'. Skipping role grants."
    } else {
        $realmManagementClient = @(Invoke-RestMethod -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/clients?clientId=realm-management" -Headers $authHeaders)[0]
        $realmManagementClientId = $realmManagementClient.id

        if ([string]::IsNullOrWhiteSpace($realmManagementClientId)) {
            Write-Warning "Could not find 'realm-management' client in realm '$Realm'. Skipping role grants."
        } else {
            $roleNames = @('manage-users', 'view-users', 'query-users', 'view-realm')
            foreach ($roleName in $roleNames) {
                try {
                    $role = Invoke-RestMethod -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/clients/$realmManagementClientId/roles/$roleName" -Headers $authHeaders
                    if ([string]::IsNullOrWhiteSpace($role.id)) {
                        Write-Warning "Could not find realm-management role '$roleName'."
                        continue
                    }

                    $grantBody = @(@{ id = $role.id; name = $roleName }) | ConvertTo-Json -Depth 4 -Compress -AsArray
                    Invoke-Http -Method Post -Uri "$KeycloakUrl/admin/realms/$Realm/users/$serviceAccountUserId/role-mappings/clients/$realmManagementClientId" -Headers $authHeaders -Body $grantBody -ContentType 'application/json' | Out-Null
                } catch {
                    Write-Warning "Failed to grant role '$roleName' to service account: $_"
                }
            }

            Write-Host 'Service account permissions configured'
        }
    }
}

@"
Bootstrap complete
Realm: $Realm
Initial admin (app): $InitialAdminUsername (temp password must be changed at first login)
Client ID: $clientId
Client Secret: $(if ([string]::IsNullOrWhiteSpace($ClientSecret)) { '[not configured by script; retrieve from Keycloak Admin Console or via API after bootstrap]' } else { '[configured from -ClientSecret / KEYCLOAK_CLIENT_SECRET; not displayed]' })
Add to appsettings / secrets accordingly.
"@ | Write-Host

exit 0
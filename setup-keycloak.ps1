#Requires -Version 7.0

<#!
.SYNOPSIS
Set up the Keycloak realm, roles, client, and test users for Student Registrar (LOCAL DEV).

.DESCRIPTION
PowerShell equivalent of setup-keycloak.sh. Targets local development
(Aspire / docker-compose) only. For cloud environments (STG, PRD) use
scripts/keycloak/bootstrap-keycloak.ps1 + scripts/keycloak/harden-realm.sh, which
apply the hardened realm template. This script does NOT enforce password policy
or brute-force protection.

Steps performed:
  - Create the realm (idempotent)
  - Create Administrator / Educator / Member roles (idempotent)
  - Create the confidential 'student-registrar' client with a service account
  - Grant the service account realm-management roles (manage-users, manage-realm,
    view-users, query-users) so the API can provision/manage users
  - Create scoopadmin / scoopmember / scoopinstructor test users (idempotent)

.PARAMETER KeycloakUrl
Base Keycloak URL. Defaults to http://localhost:8080. Falls back to the detected
Aspire Keycloak container endpoint (https://127.0.0.1:<port->8443) when the
default is unreachable, matching setup-test-users.ps1.

.PARAMETER AdminUser
Master realm admin username. Defaults to 'admin'.

.PARAMETER AdminPassword
Master realm admin password. Falls back to KEYCLOAK_ADMIN_PASSWORD, then prompts.

.EXAMPLE
./setup-keycloak.ps1

.EXAMPLE
./setup-keycloak.ps1 -AdminPassword admin123!
#>

[CmdletBinding()]
param(
    [string]$KeycloakUrl = $(if ($env:KEYCLOAK_URL) { $env:KEYCLOAK_URL } else { 'http://localhost:8080' }),
    [string]$AdminUser = $(if ($env:KEYCLOAK_ADMIN_USERNAME) { $env:KEYCLOAK_ADMIN_USERNAME } elseif ($env:KEYCLOAK_ADMIN_USER) { $env:KEYCLOAK_ADMIN_USER } else { 'admin' }),
    [string]$AdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD,
    [int]$MaxWaitSeconds = $(if ($env:KEYCLOAK_MAX_WAIT_SECONDS) { [int]$env:KEYCLOAK_MAX_WAIT_SECONDS } else { 180 })
)

$ErrorActionPreference = 'Stop'

$RealmName = 'student-registrar'
$ClientId = 'student-registrar'

function Write-Step([string]$Message) { Write-Host "==> $Message" }

function Resolve-KeycloakUrl {
    param([Parameter(Mandatory)] [string]$Url)

    if (Test-KeycloakReachable -Url $Url) {
        return $Url.TrimEnd('/')
    }

    # Fall back to the Aspire Keycloak container endpoint (https on 8443), which is
    # the actual container port. The fixed http/8080 endpoint is Aspire's dashboard proxy.
    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if ($docker) {
        $line = docker ps --format '{{.Names}} {{.Ports}}' |
            Where-Object { $_ -match '^keycloak-' } |
            Select-Object -First 1

        if ($line -match '127\.0\.0\.1:(\d+)->8443/tcp') {
            $detected = "https://127.0.0.1:$($Matches[1])"
            if (Test-KeycloakReachable -Url $detected) {
                Write-Host "Detected Aspire Keycloak container at $detected"
                return $detected
            }
        }
    }

    return $Url.TrimEnd('/')
}

function Test-KeycloakReachable {
    param([Parameter(Mandatory)] [string]$Url)

    try {
        Invoke-WebRequest -Uri "$Url/realms/master" -UseBasicParsing -SkipCertificateCheck -TimeoutSec 5 | Out-Null
        return $true
    } catch {
        return $false
    }
}

# Lightweight HTTP helper that never throws on non-2xx, so we can branch on status.
function Invoke-Http {
    param(
        [Parameter(Mandatory)] [string]$Method,
        [Parameter(Mandatory)] [string]$Uri,
        [hashtable]$Headers,
        [AllowNull()]$Body,
        [string]$ContentType
    )

    $params = @{
        Method               = $Method
        Uri                  = $Uri
        SkipHttpErrorCheck   = $true
        SkipCertificateCheck = $true
        ErrorAction          = 'Stop'
    }
    if ($Headers) { $params.Headers = $Headers }
    if ($PSBoundParameters.ContainsKey('Body') -and $null -ne $Body) { $params.Body = $Body }
    if ($ContentType) { $params.ContentType = $ContentType }

    Invoke-WebRequest @params
}

function Get-AdminToken {
    param([Parameter(Mandatory)] [string]$Password)

    try {
        $response = Invoke-RestMethod `
            -Method Post `
            -Uri "$script:KeycloakUrl/realms/master/protocol/openid-connect/token" `
            -ContentType 'application/x-www-form-urlencoded' `
            -SkipCertificateCheck `
            -Body @{
                username   = $AdminUser
                password   = $Password
                grant_type = 'password'
                client_id  = 'admin-cli'
            }
        return $response.access_token
    } catch {
        return $null
    }
}

function New-KeycloakUser {
    param(
        [Parameter(Mandatory)] [string]$Username,
        [Parameter(Mandatory)] [string]$Email,
        [Parameter(Mandatory)] [string]$FirstName,
        [Parameter(Mandatory)] [string]$LastName,
        [Parameter(Mandatory)] [string]$Password,
        [Parameter(Mandatory)] [string]$Role
    )

    $headers = $script:AuthHeaders

    # Does the user already exist?
    $existing = Invoke-RestMethod -Method Get `
        -Uri "$script:KeycloakUrl/admin/realms/$RealmName/users?username=$([uri]::EscapeDataString($Username))&exact=true" `
        -Headers $headers -SkipCertificateCheck
    if (@($existing).Count -gt 0) {
        Write-Host "   User $Username already exists in realm $RealmName."
        return
    }

    Write-Host "   Creating user: $Username"

    $userBody = @{
        username      = $Username
        enabled       = $true
        emailVerified = $true
        firstName     = $FirstName
        lastName      = $LastName
        email         = $Email
    } | ConvertTo-Json -Depth 5 -Compress

    $create = Invoke-Http -Method Post -Uri "$script:KeycloakUrl/admin/realms/$RealmName/users" `
        -Headers $headers -Body $userBody -ContentType 'application/json'
    if ([int]$create.StatusCode -ne 201) {
        throw "Failed to create user $Username (HTTP $([int]$create.StatusCode)): $($create.Content)"
    }

    $created = Invoke-RestMethod -Method Get `
        -Uri "$script:KeycloakUrl/admin/realms/$RealmName/users?username=$([uri]::EscapeDataString($Username))&exact=true" `
        -Headers $headers -SkipCertificateCheck
    $userId = @($created)[0].id
    if ([string]::IsNullOrWhiteSpace($userId)) {
        throw "Could not retrieve id for user $Username after creation."
    }

    # Set password (non-temporary)
    $resetBody = @{ type = 'password'; value = $Password; temporary = $false } | ConvertTo-Json -Compress
    $reset = Invoke-Http -Method Put -Uri "$script:KeycloakUrl/admin/realms/$RealmName/users/$userId/reset-password" `
        -Headers $headers -Body $resetBody -ContentType 'application/json'
    if ([int]$reset.StatusCode -ne 204) {
        Write-Warning "User $Username created but failed to set password (HTTP $([int]$reset.StatusCode))."
    }

    # Assign realm role
    $roleRep = Invoke-RestMethod -Method Get `
        -Uri "$script:KeycloakUrl/admin/realms/$RealmName/roles/$Role" `
        -Headers $headers -SkipCertificateCheck
    if ($null -eq $roleRep -or [string]::IsNullOrWhiteSpace($roleRep.id)) {
        Write-Warning "User $Username created but role '$Role' was not found."
        return
    }

    $roleBody = @(@{ id = $roleRep.id; name = $Role }) | ConvertTo-Json -Depth 4 -Compress -AsArray
    $assign = Invoke-Http -Method Post -Uri "$script:KeycloakUrl/admin/realms/$RealmName/users/$userId/role-mappings/realm" `
        -Headers $headers -Body $roleBody -ContentType 'application/json'
    if ([int]$assign.StatusCode -ne 204) {
        Write-Warning "User $Username created but failed to assign role '$Role' (HTTP $([int]$assign.StatusCode))."
        return
    }

    Write-Host "   User $Username created with $Role role."
}

# ---------------------------------------------------------------------------
Write-Host 'Setting up Keycloak for Student Registrar'
Write-Host '============================================='
Write-Host ''

# Resolve + wait for connectivity
Write-Host "Testing Keycloak connectivity..."
$script:KeycloakUrl = Resolve-KeycloakUrl -Url $KeycloakUrl

$elapsed = 0
while (-not (Test-KeycloakReachable -Url $script:KeycloakUrl)) {
    if ($elapsed -ge $MaxWaitSeconds) {
        Write-Error "Failed to connect to Keycloak at $script:KeycloakUrl after ${MaxWaitSeconds}s. Start the app (dotnet run --project src/StudentRegistrar.AppHost) or pass -KeycloakUrl."
        exit 1
    }
    Start-Sleep -Seconds 5
    $elapsed += 5
}
Write-Host "Keycloak is accessible at $script:KeycloakUrl"

# Resolve admin password
if ([string]::IsNullOrWhiteSpace($AdminPassword)) {
    Write-Host ''
    Write-Host 'Get your Keycloak admin password from the Aspire Dashboard (Resources tab).'
    $secure = Read-Host "Enter Keycloak admin password for user '$AdminUser'" -AsSecureString
    $AdminPassword = ([pscredential]::new('admin', $secure)).GetNetworkCredential().Password
}

# Obtain admin token (with retry while Keycloak finishes booting)
Write-Host 'Getting admin access token...'
$token = $null
$tokenElapsed = 0
$tokenMaxWait = 120
while ($tokenElapsed -le $tokenMaxWait) {
    $token = Get-AdminToken -Password $AdminPassword
    if (-not [string]::IsNullOrWhiteSpace($token)) { break }
    Start-Sleep -Seconds 5
    $tokenElapsed += 5
}
if ([string]::IsNullOrWhiteSpace($token)) {
    Write-Error 'Failed to get admin token. Check the admin password and try again.'
    exit 1
}
Write-Host 'Admin token obtained successfully'

$script:AuthHeaders = @{ Authorization = "Bearer $token" }

# Create realm (idempotent)
Write-Host "Creating realm: $RealmName"
$realmBody = @{
    realm                 = $RealmName
    enabled               = $true
    displayName           = 'Student Registrar'
    loginWithEmailAllowed = $true
    registrationAllowed   = $false
    rememberMe            = $true
    verifyEmail           = $false
    resetPasswordAllowed  = $true
} | ConvertTo-Json -Depth 5 -Compress

$createRealm = Invoke-Http -Method Post -Uri "$script:KeycloakUrl/admin/realms" `
    -Headers $script:AuthHeaders -Body $realmBody -ContentType 'application/json'
if ([int]$createRealm.StatusCode -eq 409) {
    Write-Host "Realm $RealmName already exists."
} elseif ([int]$createRealm.StatusCode -notin @(201, 204)) {
    Write-Warning "Realm creation returned HTTP $([int]$createRealm.StatusCode): $($createRealm.Content)"
}

# Create roles (idempotent)
Write-Host 'Creating user roles...'
foreach ($role in @('Administrator', 'Educator', 'Member')) {
    $roleBody = @{ name = $role; description = "$role role for Student Registrar" } | ConvertTo-Json -Compress
    $createRole = Invoke-Http -Method Post -Uri "$script:KeycloakUrl/admin/realms/$RealmName/roles" `
        -Headers $script:AuthHeaders -Body $roleBody -ContentType 'application/json'
    if ([int]$createRole.StatusCode -eq 409) {
        Write-Host "   Role $role already exists."
    } elseif ([int]$createRole.StatusCode -in @(201, 204)) {
        Write-Host "   Created role: $role"
    } else {
        Write-Warning "   Role $role creation returned HTTP $([int]$createRole.StatusCode)."
    }
}

# Create confidential client (idempotent)
Write-Host "Creating client: $ClientId"
$clientBody = @{
    clientId                 = $ClientId
    enabled                  = $true
    publicClient             = $false
    bearerOnly               = $false
    standardFlowEnabled      = $true
    directAccessGrantsEnabled = $false
    serviceAccountsEnabled   = $true
    redirectUris             = @('http://localhost:3000/*', 'http://localhost:3001/*')
    webOrigins               = @('http://localhost:3000', 'http://localhost:3001')
    attributes               = @{}
} | ConvertTo-Json -Depth 6 -Compress

$createClient = Invoke-Http -Method Post -Uri "$script:KeycloakUrl/admin/realms/$RealmName/clients" `
    -Headers $script:AuthHeaders -Body $clientBody -ContentType 'application/json'
if ([int]$createClient.StatusCode -eq 409) {
    Write-Host "Client $ClientId already exists."
} elseif ([int]$createClient.StatusCode -notin @(201, 204)) {
    Write-Warning "Client creation returned HTTP $([int]$createClient.StatusCode): $($createClient.Content)"
}

# Resolve client UUID + secret
Write-Host 'Retrieving client secret...'
$clients = Invoke-RestMethod -Method Get `
    -Uri "$script:KeycloakUrl/admin/realms/$RealmName/clients?clientId=$ClientId" `
    -Headers $script:AuthHeaders -SkipCertificateCheck
$clientUuid = @($clients)[0].id
$clientSecret = $null

if (-not [string]::IsNullOrWhiteSpace($clientUuid)) {
    $secretRep = Invoke-RestMethod -Method Get `
        -Uri "$script:KeycloakUrl/admin/realms/$RealmName/clients/$clientUuid/client-secret" `
        -Headers $script:AuthHeaders -SkipCertificateCheck
    $clientSecret = $secretRep.value

    # Grant service account realm-management roles so the API can manage users.
    Write-Host 'Configuring service account permissions...'
    $saUser = Invoke-RestMethod -Method Get `
        -Uri "$script:KeycloakUrl/admin/realms/$RealmName/clients/$clientUuid/service-account-user" `
        -Headers $script:AuthHeaders -SkipCertificateCheck
    $saUserId = $saUser.id

    if (-not [string]::IsNullOrWhiteSpace($saUserId)) {
        $rmClients = Invoke-RestMethod -Method Get `
            -Uri "$script:KeycloakUrl/admin/realms/$RealmName/clients?clientId=realm-management" `
            -Headers $script:AuthHeaders -SkipCertificateCheck
        $rmClientId = @($rmClients)[0].id

        if (-not [string]::IsNullOrWhiteSpace($rmClientId)) {
            # Roles needed for the API's user/role management operations.
            $rolesToGrant = @('manage-users', 'manage-realm', 'view-users', 'query-users')
            $roleReps = @()

            foreach ($roleName in $rolesToGrant) {
                $roleRep = Invoke-RestMethod -Method Get `
                    -Uri "$script:KeycloakUrl/admin/realms/$RealmName/clients/$rmClientId/roles/$roleName" `
                    -Headers $script:AuthHeaders -SkipCertificateCheck
                if ($null -ne $roleRep -and -not [string]::IsNullOrWhiteSpace($roleRep.id)) {
                    $roleReps += @{ id = $roleRep.id; name = $roleRep.name }
                } else {
                    Write-Warning "Could not find realm-management role '$roleName'."
                }
            }

            if ($roleReps.Count -gt 0) {
                $grantBody = $roleReps | ConvertTo-Json -Depth 4 -Compress -AsArray
                $grant = Invoke-Http -Method Post `
                    -Uri "$script:KeycloakUrl/admin/realms/$RealmName/users/$saUserId/role-mappings/clients/$rmClientId" `
                    -Headers $script:AuthHeaders -Body $grantBody -ContentType 'application/json'
                if ([int]$grant.StatusCode -eq 204) {
                    Write-Host "Service account granted: $($rolesToGrant -join ', ')"
                } else {
                    Write-Warning "Failed to grant service account roles (HTTP $([int]$grant.StatusCode)): $($grant.Content)"
                }
            }
        } else {
            Write-Warning 'Could not find realm-management client.'
        }
    } else {
        Write-Warning 'Could not find service account user.'
    }

    # Test users
    New-KeycloakUser -Username 'scoopadmin'      -Email 'scoopadmin@example.com'      -FirstName 'Scoop' -LastName 'Admin'      -Password 'ChangeThis123!'           -Role 'Administrator'
    New-KeycloakUser -Username 'scoopmember'     -Email 'scoopmember@example.com'     -FirstName 'Scoop' -LastName 'Member'     -Password 'ChangeThisMember123!'     -Role 'Member'
    New-KeycloakUser -Username 'scoopinstructor' -Email 'scoopinstructor@example.com' -FirstName 'Scoop' -LastName 'Instructor' -Password 'ChangeThisInstructor123!' -Role 'Educator'
} else {
    Write-Warning "Could not resolve client UUID for '$ClientId'; skipping service account + test user setup."
}

Write-Host ''
Write-Host 'Setup complete!'
Write-Host ''
Write-Host 'Configuration Summary:'
Write-Host '========================='
Write-Host "Realm:         $RealmName"
Write-Host "Client ID:     $ClientId"
Write-Host "Client Secret: $clientSecret"
Write-Host "Keycloak URL:  $script:KeycloakUrl"
Write-Host ''
Write-Host 'Test Users:'
Write-Host '  scoopadmin      / ChangeThis123!            (Administrator)'
Write-Host '  scoopmember     / ChangeThisMember123!      (Member)'
Write-Host '  scoopinstructor / ChangeThisInstructor123!  (Educator)'

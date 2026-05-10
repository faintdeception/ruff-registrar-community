#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$KeycloakUrl = $(if ($env:KEYCLOAK_URL) { $env:KEYCLOAK_URL } else { 'http://localhost:8080' }),
    [string]$Realm = $(if ($env:KEYCLOAK_REALM) { $env:KEYCLOAK_REALM } else { 'student-registrar' }),
    [string]$AdminUsername = $(if ($env:KEYCLOAK_ADMIN_USER) { $env:KEYCLOAK_ADMIN_USER } else { 'admin' }),
    [string]$AdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD
)

$ErrorActionPreference = 'Stop'

function Test-KeycloakUrl {
    param([Parameter(Mandatory)] [string]$Url)

    try {
        Invoke-WebRequest -Uri "$($Url.TrimEnd('/'))/realms/master/.well-known/openid-configuration" -UseBasicParsing -TimeoutSec 5 | Out-Null
        return $true
    } catch {
        return $false
    }
}

function Resolve-KeycloakUrl {
    if ($PSBoundParameters.ContainsKey('KeycloakUrl') -or $env:KEYCLOAK_URL) {
        return $KeycloakUrl.TrimEnd('/')
    }

    $defaultUrl = $KeycloakUrl.TrimEnd('/')
    if (Test-KeycloakUrl -Url $defaultUrl) {
        return $defaultUrl
    }

    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $docker) {
        return $defaultUrl
    }

    $keycloakContainer = docker ps --format '{{.Names}} {{.Ports}}' |
        Where-Object { $_ -match '^keycloak-' } |
        Select-Object -First 1

    if ($keycloakContainer -match '127\.0\.0\.1:(\d+)->8080/tcp') {
        $detectedUrl = "http://127.0.0.1:$($Matches[1])"
        Write-Host "Detected Aspire Keycloak container at $detectedUrl"
        return $detectedUrl
    }

    return $defaultUrl
}

function Get-AdminPasswordValue {
    if (-not [string]::IsNullOrWhiteSpace($AdminPassword)) {
        return $AdminPassword
    }

    $securePassword = Read-Host "Enter Keycloak master admin password for user '$AdminUsername'" -AsSecureString
    $credential = [pscredential]::new('keycloak-admin', $securePassword)
    return $credential.GetNetworkCredential().Password
}

function Invoke-KeycloakJson {
    param(
        [Parameter(Mandatory)] [string]$Method,
        [Parameter(Mandatory)] [string]$Uri,
        [hashtable]$Headers,
        $Body
    )

    $parameters = @{
        Method = $Method
        Uri = $Uri
    }

    if ($Headers) {
        $parameters.Headers = $Headers
    }

    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        if ($Body -is [array]) {
            $parameters.Body = ($Body | ConvertTo-Json -Depth 10 -Compress -AsArray)
        } else {
            $parameters.Body = ($Body | ConvertTo-Json -Depth 10 -Compress)
        }
    }

    Invoke-RestMethod @parameters
}

function Get-AdminToken {
    param([Parameter(Mandatory)] [string]$Password)

    $tokenResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body @{
            username = $AdminUsername
            password = $Password
            grant_type = 'password'
            client_id = 'admin-cli'
        }

    if ([string]::IsNullOrWhiteSpace($tokenResponse.access_token)) {
        throw 'Failed to get Keycloak admin access token.'
    }

    $tokenResponse.access_token
}

function Get-KeycloakUser {
    param(
        [Parameter(Mandatory)] [string]$Username,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    $users = Invoke-KeycloakJson -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/users?username=$([uri]::EscapeDataString($Username))&exact=true" -Headers $Headers
    @($users)[0]
}

function Get-RealmRole {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    Invoke-KeycloakJson -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/roles/$Name" -Headers $Headers
}

function Get-UserRealmRoles {
    param(
        [Parameter(Mandatory)] [string]$UserId,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    $result = Invoke-KeycloakJson -Method Get -Uri "$KeycloakUrl/admin/realms/$Realm/users/$UserId/role-mappings/realm" -Headers $Headers
    if ($null -eq $result) {
        return @()
    }

    if ($result -is [array]) {
        return @($result)
    }

    return @($result)
}

function Add-UserRealmRole {
    param(
        [Parameter(Mandatory)] [string]$UserId,
        [Parameter(Mandatory)] $Role,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    $existingRoleNames = @(Get-UserRealmRoles -UserId $UserId -Headers $Headers | ForEach-Object { $_.name })
    if ($existingRoleNames -contains $Role.name) {
        return
    }

    $roleMappingBody = [object[]]@(@{ id = $Role.id; name = $Role.name })
    Invoke-KeycloakJson -Method Post -Uri "$KeycloakUrl/admin/realms/$Realm/users/$UserId/role-mappings/realm" -Headers $Headers -Body $roleMappingBody | Out-Null
}

function Test-UserPasswordValid {
    param(
        [Parameter(Mandatory)] [string]$Username,
        [Parameter(Mandatory)] [string]$Password
    )

    try {
        $response = Invoke-RestMethod `
            -Method Post `
            -Uri "$KeycloakUrl/realms/$Realm/protocol/openid-connect/token" `
            -ContentType 'application/x-www-form-urlencoded' `
            -Body @{
                client_id = 'student-registrar-spa'
                grant_type = 'password'
                username = $Username
                password = $Password
                scope = 'openid profile email'
            }

        return -not [string]::IsNullOrWhiteSpace($response.access_token)
    } catch {
        return $false
    }
}

function Set-UserPassword {
    param(
        [Parameter(Mandatory)] [string]$UserId,
        [Parameter(Mandatory)] [string]$Username,
        [Parameter(Mandatory)] [string]$Password,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    try {
        Invoke-KeycloakJson -Method Put -Uri "$KeycloakUrl/admin/realms/$Realm/users/$UserId/reset-password" -Headers $Headers -Body @{ type = 'password'; value = $Password; temporary = $false } | Out-Null
    } catch {
        if (-not (Test-UserPasswordValid -Username $Username -Password $Password)) {
            throw
        }
    }
}

function Ensure-KeycloakUser {
    param(
        [Parameter(Mandatory)] [hashtable]$User,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    Write-Host "Ensuring user $($User.Username) ($($User.Role))"
    $existingUser = Get-KeycloakUser -Username $User.Username -Headers $Headers

    if (-not $existingUser) {
        Invoke-KeycloakJson -Method Post -Uri "$KeycloakUrl/admin/realms/$Realm/users" -Headers $Headers -Body @{
            username = $User.Username
            enabled = $true
            emailVerified = $true
            firstName = $User.FirstName
            lastName = $User.LastName
            email = $User.Email
            requiredActions = @()
        } | Out-Null

        $existingUser = Get-KeycloakUser -Username $User.Username -Headers $Headers
    }

    if (-not $existingUser) {
        throw "Unable to find or create Keycloak user '$($User.Username)'."
    }

    Invoke-KeycloakJson -Method Put -Uri "$KeycloakUrl/admin/realms/$Realm/users/$($existingUser.id)" -Headers $Headers -Body @{
        id = $existingUser.id
        username = $User.Username
        enabled = $true
        emailVerified = $true
        firstName = $User.FirstName
        lastName = $User.LastName
        email = $User.Email
        requiredActions = @()
    } | Out-Null

    Set-UserPassword -UserId $existingUser.id -Username $User.Username -Password $User.Password -Headers $Headers

    $role = Get-RealmRole -Name $User.Role -Headers $Headers
    Add-UserRealmRole -UserId $existingUser.id -Role $role -Headers $Headers

    $assignedRoles = @(Get-UserRealmRoles -UserId $existingUser.id -Headers $Headers | ForEach-Object { $_.name })
    Write-Host "    $($User.Username): $($assignedRoles -join ', ')"
}

$KeycloakUrl = Resolve-KeycloakUrl
if (-not (Test-KeycloakUrl -Url $KeycloakUrl)) {
    throw "Cannot reach Keycloak at $KeycloakUrl"
}

$adminPasswordValue = Get-AdminPasswordValue
$token = Get-AdminToken -Password $adminPasswordValue
$headers = @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }

$users = @(
    @{
        Username = 'scoopadmin'
        Password = 'ChangeThis123!'
        FirstName = 'Scoop'
        LastName = 'Admin'
        Email = 'scoopadmin@example.com'
        Role = 'Administrator'
    },
    @{
        Username = 'scoopmember'
        Password = 'MemberPass123!'
        FirstName = 'Scoop'
        LastName = 'Member'
        Email = 'scoopmember@example.com'
        Role = 'Member'
    },
    @{
        Username = 'scoopeducator'
        Password = 'EducatorPass123!'
        FirstName = 'Scoop'
        LastName = 'Educator'
        Email = 'scoopeducator@example.com'
        Role = 'Educator'
    }
)

foreach ($user in $users) {
    Ensure-KeycloakUser -User $user -Headers $headers
}

Write-Host '✅ Test users seeded'
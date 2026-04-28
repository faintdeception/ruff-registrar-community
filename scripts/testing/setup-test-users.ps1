# Setup Test Users for E2E Testing
# Creates the required Keycloak users for local browser E2E tests.

[CmdletBinding()]
param(
    [string]$KeycloakUrl = $(if ($env:KEYCLOAK_URL) { $env:KEYCLOAK_URL } else { 'http://localhost:8080' }),
    [string]$RealmName = $(if ($env:KEYCLOAK_REALM) { $env:KEYCLOAK_REALM } else { 'student-registrar' }),
    [string]$AdminUser = $(if ($env:KEYCLOAK_ADMIN_USER) { $env:KEYCLOAK_ADMIN_USER } else { 'admin' }),
    [string]$AdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD,
    [switch]$SkipBaselineReset,
    [switch]$SkipDatabaseSync,
    [string]$DbContainer = $env:POSTGRES_CONTAINER,
    [string]$DbName = $(if ($env:POSTGRES_DB) { $env:POSTGRES_DB } else { 'studentregistrar' }),
    [string]$DbUser = $(if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { 'postgres' }),
    [string]$DbPassword = $(if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { 'postgres123' })
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    Write-Host "==> $Message"
}

function Test-KeycloakUrl {
    param([Parameter(Mandatory)] [string]$Url)

    try {
        Invoke-WebRequest -Uri "$Url/realms/master" -UseBasicParsing -TimeoutSec 5 | Out-Null
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

function Resolve-PostgresContainer {
    if (-not [string]::IsNullOrWhiteSpace($DbContainer)) {
        return $DbContainer
    }

    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $docker) {
        return $null
    }

    $container = docker ps --format '{{.Names}} {{.Ports}}' |
        Where-Object { $_ -match '^postgres-' } |
        Select-Object -First 1

    if ($container -match '^(\S+)') {
        return $Matches[1]
    }

    return $null
}

function Get-AdminPassword {
    if (-not [string]::IsNullOrWhiteSpace($AdminPassword)) {
        return $AdminPassword
    }

    $securePassword = Read-Host 'Enter Keycloak admin password' -AsSecureString
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
    param([string]$Password)

    $tokenResponse = Invoke-RestMethod `
        -Method Post `
        -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
        -ContentType 'application/x-www-form-urlencoded' `
        -Body @{
            username = $AdminUser
            password = $Password
            grant_type = 'password'
            client_id = 'admin-cli'
        }

    if ([string]::IsNullOrWhiteSpace($tokenResponse.access_token)) {
        throw 'Failed to get Keycloak admin access token.'
    }

    $tokenResponse.access_token
}

function Get-RealmRole {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    Invoke-KeycloakJson -Method Get -Uri "$KeycloakUrl/admin/realms/$RealmName/roles/$Name" -Headers $Headers
}

function Get-KeycloakUser {
    param(
        [Parameter(Mandatory)] [string]$Username,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    $users = Invoke-KeycloakJson -Method Get -Uri "$KeycloakUrl/admin/realms/$RealmName/users?username=$([uri]::EscapeDataString($Username))&exact=true" -Headers $Headers
    @($users)[0]
}

function Set-UserPassword {
    param(
        [Parameter(Mandatory)] [string]$UserId,
        [Parameter(Mandatory)] [string]$Password,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    Invoke-KeycloakJson `
        -Method Put `
        -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$UserId/reset-password" `
        -Headers $Headers `
        -Body @{ type = 'password'; value = $Password; temporary = $false } | Out-Null
}

function Get-UserRealmRoles {
    param(
        [Parameter(Mandatory)] [string]$UserId,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    $result = Invoke-KeycloakJson -Method Get -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$UserId/role-mappings/realm" -Headers $Headers
    if ($null -eq $result) {
        return
    }

    if ($result -is [array]) {
        foreach ($role in $result) {
            $role
        }
        return
    }

    $result
}

function Add-UserRealmRole {
    param(
        [Parameter(Mandatory)] [string]$UserId,
        [Parameter(Mandatory)] $Role,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    $roleMappingBody = [object[]]@(@{ id = $Role.id; name = $Role.name })

    Invoke-KeycloakJson `
        -Method Post `
        -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$UserId/role-mappings/realm" `
        -Headers $Headers `
        -Body $roleMappingBody | Out-Null
}

function Remove-UserRealmRolesByName {
    param(
        [Parameter(Mandatory)] [string]$UserId,
        [Parameter(Mandatory)] [string[]]$RoleNames,
        [Parameter(Mandatory)] [hashtable]$Headers
    )

    $rolesToRemove = Get-UserRealmRoles -UserId $UserId -Headers $Headers |
        Where-Object { $RoleNames -contains $_.name }

    if (@($rolesToRemove).Count -eq 0) {
        return
    }

    $roleMappingBody = [object[]]@($rolesToRemove)

    Invoke-KeycloakJson `
        -Method Delete `
        -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$UserId/role-mappings/realm" `
        -Headers $Headers `
        -Body $roleMappingBody | Out-Null
}

function Ensure-TestUser {
    param(
        [Parameter(Mandatory)] [hashtable]$User,
        [Parameter(Mandatory)] [hashtable]$Headers,
        [string[]]$RemoveRoles = @()
    )

    Write-Step "Ensuring Keycloak user '$($User.Username)'"
    $existingUser = Get-KeycloakUser -Username $User.Username -Headers $Headers

    if (-not $existingUser) {
        Invoke-KeycloakJson `
            -Method Post `
            -Uri "$KeycloakUrl/admin/realms/$RealmName/users" `
            -Headers $Headers `
            -Body @{
                username = $User.Username
                enabled = $true
                emailVerified = $true
                firstName = $User.FirstName
                lastName = $User.LastName
                email = $User.Email
            } | Out-Null

        $existingUser = Get-KeycloakUser -Username $User.Username -Headers $Headers
    }

    if (-not $existingUser) {
        throw "Unable to find or create Keycloak user '$($User.Username)'."
    }

    Set-UserPassword -UserId $existingUser.id -Password $User.Password -Headers $Headers

    foreach ($roleName in $User.Roles) {
        $role = Get-RealmRole -Name $roleName -Headers $Headers
        Add-UserRealmRole -UserId $existingUser.id -Role $role -Headers $Headers
    }

    if (-not $SkipBaselineReset -and $RemoveRoles.Count -gt 0) {
        Remove-UserRealmRolesByName -UserId $existingUser.id -RoleNames $RemoveRoles -Headers $Headers
    }

    $assignedRoles = @(Get-UserRealmRoles -UserId $existingUser.id -Headers $Headers |
        ForEach-Object { $_.name } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

    $User.KeycloakId = $existingUser.id
    Write-Host "    $($User.Username): $($assignedRoles -join ', ')"
}

function ConvertTo-SqlLiteral {
    param([AllowNull()] [string]$Value)

    if ($null -eq $Value) {
        return 'NULL'
    }

    return "'$($Value.Replace("'", "''"))'"
}

function Invoke-PostgresSql {
    param(
        [Parameter(Mandatory)] [string]$Container,
        [Parameter(Mandatory)] [string]$Sql
    )

    $arguments = @(
        'exec',
        '-i',
        '-e', "PGPASSWORD=$DbPassword",
        $Container,
        'psql',
        '-U', $DbUser,
        '-d', $DbName,
        '-v', 'ON_ERROR_STOP=1'
    )

    $Sql | & docker @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed with exit code $LASTEXITCODE."
    }
}

function Sync-TestDatabaseUsers {
    param([Parameter(Mandatory)] [object[]]$Users)

    if ($SkipDatabaseSync) {
        return
    }

    $container = Resolve-PostgresContainer
    if ([string]::IsNullOrWhiteSpace($container)) {
        Write-Warning 'PostgreSQL container was not found; skipping local database test user sync.'
        return
    }

    Write-Step "Syncing E2E test users in PostgreSQL container '$container'"

    $userValues = foreach ($user in $Users) {
        "(gen_random_uuid(), $(ConvertTo-SqlLiteral $user.Email), $(ConvertTo-SqlLiteral $user.FirstName), $(ConvertTo-SqlLiteral $user.LastName), $(ConvertTo-SqlLiteral $user.KeycloakId), $($user.DatabaseRole), true, NOW(), NOW(), '00000000-0000-0000-0000-000000000000')"
    }

    $accountHolderValues = foreach ($user in $Users) {
        $addressJson = '{"street":"300 Test User Rd","city":"Testville","state":"CA","postalCode":"12350","country":"US"}'
        $emergencyJson = '{"firstName":"Test","lastName":"Contact","homePhone":"555-1303","mobilePhone":"555-1304","email":"test.contact@example.com"}'
        "(gen_random_uuid(), $(ConvertTo-SqlLiteral $user.FirstName), $(ConvertTo-SqlLiteral $user.LastName), $(ConvertTo-SqlLiteral $user.Email), '555-1301', '555-1302', '$addressJson'::jsonb, '$emergencyJson'::jsonb, 0.00, 0.00, $(ConvertTo-SqlLiteral $user.KeycloakId), NOW(), NOW(), NOW(), NOW(), '00000000-0000-0000-0000-000000000000')"
    }

    $sql = @"
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "KeycloakId", "Role", "IsActive", "CreatedAt", "UpdatedAt", "TenantId") VALUES
$($userValues -join ",`n")
ON CONFLICT ("TenantId", "Email") DO UPDATE SET
    "FirstName" = EXCLUDED."FirstName",
    "LastName" = EXCLUDED."LastName",
    "KeycloakId" = EXCLUDED."KeycloakId",
    "Role" = EXCLUDED."Role",
    "IsActive" = true,
    "UpdatedAt" = NOW();

INSERT INTO "AccountHolders" ("Id", "FirstName", "LastName", "EmailAddress", "HomePhone", "MobilePhone", "AddressJson", "EmergencyContactJson", "MembershipDuesOwed", "MembershipDuesReceived", "KeycloakUserId", "MemberSince", "LastEdit", "CreatedAt", "UpdatedAt", "TenantId") VALUES
$($accountHolderValues -join ",`n")
ON CONFLICT ("TenantId", "EmailAddress") DO UPDATE SET
    "FirstName" = EXCLUDED."FirstName",
    "LastName" = EXCLUDED."LastName",
    "HomePhone" = EXCLUDED."HomePhone",
    "MobilePhone" = EXCLUDED."MobilePhone",
    "AddressJson" = EXCLUDED."AddressJson",
    "EmergencyContactJson" = EXCLUDED."EmergencyContactJson",
    "KeycloakUserId" = EXCLUDED."KeycloakUserId",
    "LastEdit" = NOW(),
    "UpdatedAt" = NOW();
"@

    Invoke-PostgresSql -Container $container -Sql $sql
}

$KeycloakUrl = Resolve-KeycloakUrl
Write-Step "Setting up E2E test users in realm '$RealmName' at $KeycloakUrl"
$password = Get-AdminPassword
$token = Get-AdminToken -Password $password
$headers = @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }

$testUsers = @(
    @{
        Username = 'admin1'
        Password = 'AdminPass123!'
        FirstName = 'Admin'
        LastName = 'Test'
        Email = 'admin.test@example.com'
        Roles = @("default-roles-$RealmName", 'Administrator')
        RemoveRoles = @('Educator', 'Member')
        DatabaseRole = 3
    },
    @{
        Username = 'educator1'
        Password = 'EducatorPass123!'
        FirstName = 'Emily'
        LastName = 'Educator'
        Email = 'emily.educator@example.com'
        Roles = @("default-roles-$RealmName", 'Educator')
        RemoveRoles = @('Administrator')
        DatabaseRole = 2
    },
    @{
        Username = 'member1'
        Password = 'MemberPass123!'
        FirstName = 'Mark'
        LastName = 'Member'
        Email = 'mark.member@example.com'
        Roles = @("default-roles-$RealmName", 'Member')
        RemoveRoles = @('Administrator', 'Educator')
        DatabaseRole = 1
    },
    @{
        Username = 'parenteducator1'
        Password = 'ParentEducatorPass123!'
        FirstName = 'Sarah'
        LastName = 'Johnson'
        Email = 'sarah.johnson@example.com'
        Roles = @("default-roles-$RealmName", 'Member')
        RemoveRoles = @('Administrator', 'Educator')
        DatabaseRole = 1
    }
)

foreach ($user in $testUsers) {
    Ensure-TestUser -User $user -Headers $headers -RemoveRoles $user.RemoveRoles
}

Sync-TestDatabaseUsers -Users $testUsers

Write-Host ''
Write-Host 'E2E test user setup complete.'
Write-Host 'Test users:'
Write-Host '  admin1 / AdminPass123! - Administrator'
Write-Host '  educator1 / EducatorPass123! - Educator'
Write-Host '  member1 / MemberPass123! - Member baseline'
Write-Host '  parenteducator1 / ParentEducatorPass123! - Member promoted by parent educator workflow'
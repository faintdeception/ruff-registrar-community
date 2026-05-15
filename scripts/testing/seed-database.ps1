#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$DbContainer = $(if ($env:DB_CONTAINER) { $env:DB_CONTAINER } elseif ($env:POSTGRES_CONTAINER) { $env:POSTGRES_CONTAINER } else { '' }),
    [string]$DbName = $(if ($env:DB_NAME) { $env:DB_NAME } else { 'studentregistrar' }),
    [string]$DbUser = $(if ($env:DB_USER) { $env:DB_USER } else { 'postgres' }),
    [string]$DbPassword = $(if ($env:DB_PASSWORD) { $env:DB_PASSWORD } elseif ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { 'postgres123!' }),
    [string]$TenantId = $(if ($env:SEED_TENANT_ID) { $env:SEED_TENANT_ID } else { '00000000-0000-0000-0000-000000000001' }),
    [string]$KeycloakUrl = $(if ($env:KEYCLOAK_URL) { $env:KEYCLOAK_URL } else { 'http://localhost:8080' }),
    [string]$KeycloakRealm = $(if ($env:KEYCLOAK_REALM) { $env:KEYCLOAK_REALM } else { 'student-registrar' }),
    [string]$KeycloakAdminUser = $(if ($env:KEYCLOAK_ADMIN_USERNAME) { $env:KEYCLOAK_ADMIN_USERNAME } elseif ($env:KEYCLOAK_ADMIN_USER) { $env:KEYCLOAK_ADMIN_USER } else { 'admin' }),
    [string]$KeycloakAdminPassword = $env:KEYCLOAK_ADMIN_PASSWORD,
    [switch]$Reset
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    Write-Host "==> $Message"
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
        [Parameter(Mandatory)] [string]$Sql,
        [switch]$Scalar
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

    if ($Scalar) {
        $arguments += @('-t', '-A')
    }

    $result = $Sql | & docker @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed with exit code $LASTEXITCODE."
    }

    if ($Scalar) {
        return (($result | Select-Object -First 1) ?? '').Trim()
    }

    $result | Out-Host
}

function Get-KeycloakAdminToken {
    if ([string]::IsNullOrWhiteSpace($KeycloakAdminPassword)) {
        return $null
    }

    try {
        $response = Invoke-RestMethod `
            -Method Post `
            -Uri "$($KeycloakUrl.TrimEnd('/'))/realms/master/protocol/openid-connect/token" `
            -ContentType 'application/x-www-form-urlencoded' `
            -Body @{
                username = $KeycloakAdminUser
                password = $KeycloakAdminPassword
                grant_type = 'password'
                client_id = 'admin-cli'
            }

        return $response.access_token
    }
    catch {
        Write-Warning "Could not get Keycloak admin token for live ID lookup: $($_.Exception.Message)"
        return $null
    }
}

function Get-KeycloakUserId {
    param(
        [Parameter(Mandatory)] [string]$Username,
        [Parameter(Mandatory)] [string]$FallbackId,
        [AllowNull()] [string]$Token
    )

    if ([string]::IsNullOrWhiteSpace($Token)) {
        return $FallbackId
    }

    try {
        $users = Invoke-RestMethod `
            -Method Get `
            -Uri "$($KeycloakUrl.TrimEnd('/'))/admin/realms/$KeycloakRealm/users?username=$([uri]::EscapeDataString($Username))&exact=true" `
            -Headers @{ Authorization = "Bearer $Token" }

        $user = @($users)[0]
        if ($user -and -not [string]::IsNullOrWhiteSpace($user.id)) {
            return $user.id
        }
    }
    catch {
        Write-Warning "Could not look up Keycloak user '$Username': $($_.Exception.Message)"
    }

    $FallbackId
}

Write-Step 'Starting database seed'
$container = Resolve-PostgresContainer
$adminToken = Get-KeycloakAdminToken

$scoopAdminKeycloakId = Get-KeycloakUserId -Username 'scoopadmin' -FallbackId 'scoopadmin-keycloak-id' -Token $adminToken
$admin1KeycloakId = Get-KeycloakUserId -Username 'admin1' -FallbackId 'admin1-keycloak-id' -Token $adminToken
$educator1KeycloakId = Get-KeycloakUserId -Username 'educator1' -FallbackId 'educator1-keycloak-id' -Token $adminToken
$parentEducatorKeycloakId = Get-KeycloakUserId -Username 'parenteducator1' -FallbackId 'parenteducator1-keycloak-id' -Token $adminToken
$member1KeycloakId = Get-KeycloakUserId -Username 'member1' -FallbackId 'member1-keycloak-id' -Token $adminToken

$accountHoldersCount = Invoke-PostgresSql -Container $container -Sql 'SELECT COUNT(*) FROM "AccountHolders";' -Scalar
$semestersCount = Invoke-PostgresSql -Container $container -Sql 'SELECT COUNT(*) FROM "Semesters";' -Scalar
if (($accountHoldersCount -gt 0 -or $semestersCount -gt 0) -and -not $Reset) {
    Write-Host 'Database already contains data; use -Reset to clear and reseed.'
    return
}

if ($Reset) {
    Write-Step 'Clearing existing data'
    Invoke-PostgresSql -Container $container -Sql @'
TRUNCATE TABLE "Payments" CASCADE;
TRUNCATE TABLE "Enrollments" CASCADE;
TRUNCATE TABLE "CourseInstructors" CASCADE;
TRUNCATE TABLE "Courses" CASCADE;
TRUNCATE TABLE "Rooms" CASCADE;
TRUNCATE TABLE "Students" CASCADE;
TRUNCATE TABLE "Semesters" CASCADE;
TRUNCATE TABLE "AccountHolders" CASCADE;
TRUNCATE TABLE "Users" CASCADE;
TRUNCATE TABLE "Tenants" CASCADE;
'@
}

Write-Step 'Creating tenant, users, and account holders'
Invoke-PostgresSql -Container $container -Sql @"
INSERT INTO "Tenants" ("Id", "Name", "Subdomain", "SubscriptionTier", "SubscriptionStatus", "ThemeConfigJson", "KeycloakRealm", "AdminEmail", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('$TenantId', 'E2E Test Tenant', 'test', 1, 0, '{}', 'student-registrar', 'admin.test@example.com', true, NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO "Users" ("Id", "TenantId", "Email", "FirstName", "LastName", "KeycloakId", "Role", "IsActive", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', 'scoopadmin@example.com', 'Scoop', 'Admin', $(ConvertTo-SqlLiteral $scoopAdminKeycloakId), 3, true, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'admin.test@example.com', 'Admin', 'Test', $(ConvertTo-SqlLiteral $admin1KeycloakId), 3, true, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'emily.educator@example.com', 'Emily', 'Educator', $(ConvertTo-SqlLiteral $educator1KeycloakId), 2, true, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'sarah.johnson@example.com', 'Sarah', 'Johnson', $(ConvertTo-SqlLiteral $parentEducatorKeycloakId), 1, true, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'mark.member@example.com', 'Mark', 'Member', $(ConvertTo-SqlLiteral $member1KeycloakId), 1, true, NOW(), NOW());

INSERT INTO "AccountHolders" ("Id", "TenantId", "FirstName", "LastName", "EmailAddress", "HomePhone", "MobilePhone", "AddressJson", "EmergencyContactJson", "MembershipDuesOwed", "MembershipDuesReceived", "KeycloakUserId", "MemberSince", "LastEdit", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', 'John', 'Smith', 'scoopmember@example.com', '555-0101', '555-0102', '{"street":"123 Main St","city":"Anytown","state":"CA","postalCode":"12345","country":"US"}', '{"firstName":"Jane","lastName":"Smith","homePhone":"555-0103","mobilePhone":"555-0104","email":"jane.smith@example.com"}', 100.00, 75.00, 'scoopmember-keycloak-id', '2024-01-15', NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Sarah', 'Johnson', 'sarah.johnson@example.com', '555-0201', '555-0202', '{"street":"456 Oak Ave","city":"Somewhere","state":"CA","postalCode":"12346","country":"US"}', '{"firstName":"Mike","lastName":"Johnson","homePhone":"555-0203","mobilePhone":"555-0204","email":"mike.johnson@example.com"}', 150.00, 150.00, $(ConvertTo-SqlLiteral $parentEducatorKeycloakId), '2023-09-10', NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Michael', 'Brown', 'michael.brown@example.com', '555-0301', '555-0302', '{"street":"789 Pine Rd","city":"Elsewhere","state":"CA","postalCode":"12347","country":"US"}', '{"firstName":"Lisa","lastName":"Brown","homePhone":"555-0303","mobilePhone":"555-0304","email":"lisa.brown@example.com"}', 200.00, 100.00, 'michael-keycloak-id', '2024-03-20', NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Admin', 'Test', 'admin.test@example.com', '555-1101', '555-1102', '{"street":"100 Admin St","city":"AdminTown","state":"CA","postalCode":"12348","country":"US"}', '{"firstName":"Test","lastName":"Admin","homePhone":"555-1103","mobilePhone":"555-1104","email":"test.admin@example.com"}', 0.00, 0.00, $(ConvertTo-SqlLiteral $admin1KeycloakId), NOW(), NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Emily', 'Educator', 'emily.educator@example.com', '555-1201', '555-1202', '{"street":"200 Educator Ave","city":"TeacherTown","state":"CA","postalCode":"12349","country":"US"}', '{"firstName":"Education","lastName":"Contact","homePhone":"555-1203","mobilePhone":"555-1204","email":"education.contact@example.com"}', 0.00, 0.00, $(ConvertTo-SqlLiteral $educator1KeycloakId), NOW(), NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Mark', 'Member', 'mark.member@example.com', '555-1301', '555-1302', '{"street":"300 Member Rd","city":"MemberVille","state":"CA","postalCode":"12350","country":"US"}', '{"firstName":"Member","lastName":"Contact","homePhone":"555-1303","mobilePhone":"555-1304","email":"member.contact@example.com"}', 50.00, 25.00, $(ConvertTo-SqlLiteral $member1KeycloakId), NOW(), NOW(), NOW(), NOW());
"@

Write-Step 'Creating semesters, students, rooms, and courses'
Invoke-PostgresSql -Container $container -Sql @"
INSERT INTO "Semesters" ("Id", "TenantId", "Name", "Code", "StartDate", "EndDate", "RegistrationStartDate", "RegistrationEndDate", "IsActive", "PeriodConfigJson", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', 'Fall 2025', 'FALL2025', '2025-09-01', '2025-12-15', '2025-08-01', '2025-08-31', true, '{"periods":[{"name":"Period 1","code":"P1","startDate":"2025-09-01","endDate":"2025-12-15","isActive":true,"description":"Morning session"},{"name":"Period 2","code":"P2","startDate":"2025-09-01","endDate":"2025-12-15","isActive":true,"description":"Mid-morning session"},{"name":"Period 3","code":"P3","startDate":"2025-09-01","endDate":"2025-12-15","isActive":true,"description":"Afternoon session"},{"name":"Period 4","code":"P4","startDate":"2025-09-01","endDate":"2025-12-15","isActive":true,"description":"Late afternoon session"}],"holidays":[{"name":"Labor Day","date":"2025-09-01","description":"National holiday"},{"name":"Thanksgiving","date":"2025-11-27","description":"Thanksgiving break"}]}', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Spring 2026', 'SPRING2026', '2026-01-15', '2026-05-15', '2025-12-01', '2026-01-10', false, '{"periods":[{"name":"Period 1","code":"P1","startDate":"2026-01-15","endDate":"2026-05-15","isActive":true,"description":"Morning session"},{"name":"Period 2","code":"P2","startDate":"2026-01-15","endDate":"2026-05-15","isActive":true,"description":"Mid-morning session"},{"name":"Period 3","code":"P3","startDate":"2026-01-15","endDate":"2026-05-15","isActive":true,"description":"Afternoon session"},{"name":"Period 4","code":"P4","startDate":"2026-01-15","endDate":"2026-05-15","isActive":true,"description":"Late afternoon session"}],"holidays":[{"name":"Presidents Day","date":"2026-02-16","description":"National holiday"},{"name":"Spring Break","date":"2026-03-23","description":"Spring break week"}]}', NOW(), NOW());

INSERT INTO "Students" ("Id", "TenantId", "AccountHolderId", "FirstName", "LastName", "Grade", "DateOfBirth", "StudentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Emma', 'Smith', '3', '2016-04-15', '{"specialConditions":["Allergic to peanuts"],"allergies":["Peanuts","Tree nuts"],"medications":[],"preferredName":"Em","parentNotes":"Very outgoing child, loves art"}', 'Enrolled in art classes', NOW(), NOW() FROM "AccountHolders" WHERE "EmailAddress" = 'scoopmember@example.com';
INSERT INTO "Students" ("Id", "TenantId", "AccountHolderId", "FirstName", "LastName", "Grade", "DateOfBirth", "StudentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Liam', 'Smith', '1', '2018-09-22', '{"specialConditions":[],"allergies":[],"medications":[],"preferredName":"Liam","parentNotes":"Shy but loves science"}', 'Interested in science experiments', NOW(), NOW() FROM "AccountHolders" WHERE "EmailAddress" = 'scoopmember@example.com';
INSERT INTO "Students" ("Id", "TenantId", "AccountHolderId", "FirstName", "LastName", "Grade", "DateOfBirth", "StudentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Sophia', 'Johnson', '5', '2014-11-08', '{"specialConditions":["ADHD"],"allergies":[],"medications":["Ritalin"],"preferredName":"Sophie","parentNotes":"Needs movement breaks"}', 'Requires accommodations for ADHD', NOW(), NOW() FROM "AccountHolders" WHERE "EmailAddress" = 'sarah.johnson@example.com';
INSERT INTO "Students" ("Id", "TenantId", "AccountHolderId", "FirstName", "LastName", "Grade", "DateOfBirth", "StudentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Oliver', 'Brown', '2', '2017-02-14', '{"specialConditions":[],"allergies":["Dairy"],"medications":[],"preferredName":"Ollie","parentNotes":"Lactose intolerant"}', 'Bring dairy-free snacks', NOW(), NOW() FROM "AccountHolders" WHERE "EmailAddress" = 'michael.brown@example.com';
INSERT INTO "Students" ("Id", "TenantId", "AccountHolderId", "FirstName", "LastName", "Grade", "DateOfBirth", "StudentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Ava', 'Brown', '4', '2015-07-30', '{"specialConditions":[],"allergies":[],"medications":[],"preferredName":"Ava","parentNotes":"Very social and helpful"}', 'Natural leader, good with younger kids', NOW(), NOW() FROM "AccountHolders" WHERE "EmailAddress" = 'michael.brown@example.com';

INSERT INTO "Rooms" ("Id", "TenantId", "Name", "Capacity", "Notes", "RoomType", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', 'Art Room A', 15, 'Art supplies and easels available', 0, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Science Lab', 12, 'Safety equipment and lab benches', 1, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Classroom B', 20, 'Standard classroom with tables and chairs', 0, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Math Room', 10, 'Whiteboards and calculators available', 0, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Auditorium', 50, 'Stage and sound system for performances', 2, NOW(), NOW());

INSERT INTO "Courses" ("Id", "TenantId", "SemesterId", "Name", "Code", "Description", "RoomId", "MaxCapacity", "Fee", "PeriodCode", "StartTime", "EndTime", "CourseConfigJson", "AgeGroup", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', s."Id", 'Beginning Art', 'ART101', 'Introduction to basic art techniques using various media', r."Id", 12, 45.00, 'P1', '10:00', '11:00', '{"prerequisites":[],"materials":["Watercolors","Brushes","Paper"],"daysOfWeek":["Monday","Wednesday","Friday"],"gradeRange":"K-5"}', 'Elementary', NOW(), NOW() FROM "Semesters" s, "Rooms" r WHERE s."Code" = 'FALL2025' AND r."Name" = 'Art Room A';
INSERT INTO "Courses" ("Id", "TenantId", "SemesterId", "Name", "Code", "Description", "RoomId", "MaxCapacity", "Fee", "PeriodCode", "StartTime", "EndTime", "CourseConfigJson", "AgeGroup", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', s."Id", 'Young Scientists', 'SCI101', 'Hands-on science experiments for curious minds', r."Id", 10, 50.00, 'P2', '11:15', '12:15', '{"prerequisites":[],"materials":["Safety goggles","Lab notebook"],"daysOfWeek":["Tuesday","Thursday"],"gradeRange":"1-3"}', 'Elementary', NOW(), NOW() FROM "Semesters" s, "Rooms" r WHERE s."Code" = 'FALL2025' AND r."Name" = 'Science Lab';
INSERT INTO "Courses" ("Id", "TenantId", "SemesterId", "Name", "Code", "Description", "RoomId", "MaxCapacity", "Fee", "PeriodCode", "StartTime", "EndTime", "CourseConfigJson", "AgeGroup", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', s."Id", 'Creative Writing', 'ENG201', 'Develop writing skills through storytelling and poetry', r."Id", 15, 40.00, 'P3', '13:00', '14:00', '{"prerequisites":["Basic reading skills"],"materials":["Notebook","Pencils"],"daysOfWeek":["Monday","Wednesday"],"gradeRange":"3-6"}', 'Elementary', NOW(), NOW() FROM "Semesters" s, "Rooms" r WHERE s."Code" = 'FALL2025' AND r."Name" = 'Classroom B';
INSERT INTO "Courses" ("Id", "TenantId", "SemesterId", "Name", "Code", "Description", "RoomId", "MaxCapacity", "Fee", "PeriodCode", "StartTime", "EndTime", "CourseConfigJson", "AgeGroup", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', s."Id", 'Math Games', 'MATH101', 'Make math fun with interactive games and puzzles', r."Id", 8, 35.00, 'P4', '14:15', '15:15', '{"prerequisites":[],"materials":["Calculator","Workbook"],"daysOfWeek":["Tuesday","Thursday"],"gradeRange":"K-4"}', 'Elementary', NOW(), NOW() FROM "Semesters" s, "Rooms" r WHERE s."Code" = 'FALL2025' AND r."Name" = 'Math Room';
INSERT INTO "Courses" ("Id", "TenantId", "SemesterId", "Name", "Code", "Description", "RoomId", "MaxCapacity", "Fee", "PeriodCode", "StartTime", "EndTime", "CourseConfigJson", "AgeGroup", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', s."Id", 'Drama Club', 'DRAMA101', 'Explore acting, improvisation, and theater skills', r."Id", 20, 55.00, 'P1', '10:00', '11:00', '{"prerequisites":[],"materials":["Comfortable clothes"],"daysOfWeek":["Friday"],"gradeRange":"2-8"}', 'Elementary/Middle', NOW(), NOW() FROM "Semesters" s, "Rooms" r WHERE s."Code" = 'FALL2025' AND r."Name" = 'Auditorium';
"@

Write-Step 'Creating instructors, enrollments, and payments'
Invoke-PostgresSql -Container $container -Sql @"
INSERT INTO "CourseInstructors" ("Id", "TenantId", "CourseId", "FirstName", "LastName", "Email", "Phone", "IsPrimary", "InstructorInfoJson", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Maria', 'Rodriguez', 'maria.rodriguez@example.com', '555-1001', true, '{"bio":"Professional artist with 10 years teaching experience","qualifications":["BFA in Fine Arts","Elementary Teaching Certificate"]}', NOW(), NOW() FROM "Courses" WHERE "Code" = 'ART101';
INSERT INTO "CourseInstructors" ("Id", "TenantId", "CourseId", "FirstName", "LastName", "Email", "Phone", "IsPrimary", "InstructorInfoJson", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'David', 'Chen', 'david.chen@example.com', '555-1002', true, '{"bio":"Former NASA engineer turned educator","qualifications":["MS in Aerospace Engineering","Science Education Certificate"]}', NOW(), NOW() FROM "Courses" WHERE "Code" = 'SCI101';
INSERT INTO "CourseInstructors" ("Id", "TenantId", "CourseId", "FirstName", "LastName", "Email", "Phone", "IsPrimary", "InstructorInfoJson", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Jennifer', 'Williams', 'jennifer.williams@example.com', '555-1003', true, '{"bio":"Published author and writing instructor","qualifications":["MFA in Creative Writing","Published novelist"]}', NOW(), NOW() FROM "Courses" WHERE "Code" = 'ENG201';
INSERT INTO "CourseInstructors" ("Id", "TenantId", "CourseId", "FirstName", "LastName", "Email", "Phone", "IsPrimary", "InstructorInfoJson", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Robert', 'Taylor', 'robert.taylor@example.com', '555-1004', true, '{"bio":"Math teacher who makes numbers fun","qualifications":["MS in Mathematics","Elementary Education Certificate"]}', NOW(), NOW() FROM "Courses" WHERE "Code" = 'MATH101';
INSERT INTO "CourseInstructors" ("Id", "TenantId", "CourseId", "FirstName", "LastName", "Email", "Phone", "IsPrimary", "InstructorInfoJson", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", 'Lisa', 'Anderson', 'lisa.anderson@example.com', '555-1005', true, '{"bio":"Professional actress and drama coach","qualifications":["BFA in Theater Arts","Youth Theater Director"]}', NOW(), NOW() FROM "Courses" WHERE "Code" = 'DRAMA101';

INSERT INTO "Enrollments" ("Id", "TenantId", "StudentId", "CourseId", "SemesterId", "EnrollmentType", "EnrollmentDate", "FeeAmount", "AmountPaid", "PaymentStatus", "EnrollmentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', st."Id", c."Id", c."SemesterId", 0, NOW(), 45.00, 45.00, 1, '{"accommodations":[],"specialInstructions":"Loves to paint"}', 'Enrolled in art class', NOW(), NOW() FROM "Students" st, "Courses" c WHERE st."FirstName" = 'Emma' AND st."LastName" = 'Smith' AND c."Code" = 'ART101';
INSERT INTO "Enrollments" ("Id", "TenantId", "StudentId", "CourseId", "SemesterId", "EnrollmentType", "EnrollmentDate", "FeeAmount", "AmountPaid", "PaymentStatus", "EnrollmentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', st."Id", c."Id", c."SemesterId", 0, NOW(), 50.00, 25.00, 2, '{"accommodations":[],"specialInstructions":"Needs encouragement"}', 'Partial payment received', NOW(), NOW() FROM "Students" st, "Courses" c WHERE st."FirstName" = 'Liam' AND st."LastName" = 'Smith' AND c."Code" = 'SCI101';
INSERT INTO "Enrollments" ("Id", "TenantId", "StudentId", "CourseId", "SemesterId", "EnrollmentType", "EnrollmentDate", "FeeAmount", "AmountPaid", "PaymentStatus", "EnrollmentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', st."Id", c."Id", c."SemesterId", 0, NOW(), 40.00, 0.00, 0, '{"accommodations":["Movement breaks"],"specialInstructions":"ADHD accommodations needed"}', 'Needs payment', NOW(), NOW() FROM "Students" st, "Courses" c WHERE st."FirstName" = 'Sophia' AND st."LastName" = 'Johnson' AND c."Code" = 'ENG201';
INSERT INTO "Enrollments" ("Id", "TenantId", "StudentId", "CourseId", "SemesterId", "EnrollmentType", "EnrollmentDate", "FeeAmount", "AmountPaid", "PaymentStatus", "EnrollmentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', st."Id", c."Id", c."SemesterId", 0, NOW(), 35.00, 35.00, 1, '{"accommodations":[],"specialInstructions":"Dairy-free snacks only"}', 'Paid in full', NOW(), NOW() FROM "Students" st, "Courses" c WHERE st."FirstName" = 'Oliver' AND st."LastName" = 'Brown' AND c."Code" = 'MATH101';
INSERT INTO "Enrollments" ("Id", "TenantId", "StudentId", "CourseId", "SemesterId", "EnrollmentType", "EnrollmentDate", "FeeAmount", "AmountPaid", "PaymentStatus", "EnrollmentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', st."Id", c."Id", c."SemesterId", 0, NOW(), 55.00, 55.00, 1, '{"accommodations":[],"specialInstructions":"Natural leader"}', 'Loves performing', NOW(), NOW() FROM "Students" st, "Courses" c WHERE st."FirstName" = 'Ava' AND st."LastName" = 'Brown' AND c."Code" = 'DRAMA101';
INSERT INTO "Enrollments" ("Id", "TenantId", "StudentId", "CourseId", "SemesterId", "EnrollmentType", "EnrollmentDate", "FeeAmount", "AmountPaid", "PaymentStatus", "EnrollmentInfoJson", "Notes", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), '$TenantId', st."Id", c."Id", c."SemesterId", 1, NOW(), 55.00, 0.00, 0, '{"accommodations":[],"specialInstructions":"Interested in drama too"}', 'Waitlisted for drama', NOW(), NOW() FROM "Students" st, "Courses" c WHERE st."FirstName" = 'Emma' AND st."LastName" = 'Smith' AND c."Code" = 'DRAMA101';

INSERT INTO "Payments" ("Id", "TenantId", "AccountHolderId", "EnrollmentId", "Amount", "PaymentDate", "PaymentMethod", "PaymentType", "TransactionId", "PaymentInfoJson", "Notes", "CreatedAt")
SELECT gen_random_uuid(), '$TenantId', ah."Id", e."Id", 45.00, NOW(), 1, 0, 'TXN001', '{"checkNumber":"1234"}', 'Payment for Emma art class', NOW() FROM "AccountHolders" ah, "Students" st, "Courses" c, "Enrollments" e WHERE ah."EmailAddress" = 'scoopmember@example.com' AND st."FirstName" = 'Emma' AND st."LastName" = 'Smith' AND c."Code" = 'ART101' AND e."StudentId" = st."Id" AND e."CourseId" = c."Id";
INSERT INTO "Payments" ("Id", "TenantId", "AccountHolderId", "EnrollmentId", "Amount", "PaymentDate", "PaymentMethod", "PaymentType", "TransactionId", "PaymentInfoJson", "Notes", "CreatedAt")
SELECT gen_random_uuid(), '$TenantId', ah."Id", e."Id", 25.00, NOW(), 0, 0, 'CASH001', '{}', 'Partial payment for Liam science class', NOW() FROM "AccountHolders" ah, "Students" st, "Courses" c, "Enrollments" e WHERE ah."EmailAddress" = 'scoopmember@example.com' AND st."FirstName" = 'Liam' AND st."LastName" = 'Smith' AND c."Code" = 'SCI101' AND e."StudentId" = st."Id" AND e."CourseId" = c."Id";
INSERT INTO "Payments" ("Id", "TenantId", "AccountHolderId", "EnrollmentId", "Amount", "PaymentDate", "PaymentMethod", "PaymentType", "TransactionId", "PaymentInfoJson", "Notes", "CreatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", NULL, 75.00, NOW(), 2, 1, 'CC001', '{"cardLast4":"1234"}', 'Membership dues payment', NOW() FROM "AccountHolders" WHERE "EmailAddress" = 'scoopmember@example.com';
INSERT INTO "Payments" ("Id", "TenantId", "AccountHolderId", "EnrollmentId", "Amount", "PaymentDate", "PaymentMethod", "PaymentType", "TransactionId", "PaymentInfoJson", "Notes", "CreatedAt")
SELECT gen_random_uuid(), '$TenantId', ah."Id", e."Id", 35.00, NOW(), 1, 0, 'TXN002', '{"checkNumber":"5678"}', 'Payment for Oliver math class', NOW() FROM "AccountHolders" ah, "Students" st, "Courses" c, "Enrollments" e WHERE ah."EmailAddress" = 'michael.brown@example.com' AND st."FirstName" = 'Oliver' AND st."LastName" = 'Brown' AND c."Code" = 'MATH101' AND e."StudentId" = st."Id" AND e."CourseId" = c."Id";
INSERT INTO "Payments" ("Id", "TenantId", "AccountHolderId", "EnrollmentId", "Amount", "PaymentDate", "PaymentMethod", "PaymentType", "TransactionId", "PaymentInfoJson", "Notes", "CreatedAt")
SELECT gen_random_uuid(), '$TenantId', ah."Id", e."Id", 55.00, NOW(), 2, 0, 'CC002', '{"cardLast4":"5678"}', 'Payment for Ava drama class', NOW() FROM "AccountHolders" ah, "Students" st, "Courses" c, "Enrollments" e WHERE ah."EmailAddress" = 'michael.brown@example.com' AND st."FirstName" = 'Ava' AND st."LastName" = 'Brown' AND c."Code" = 'DRAMA101' AND e."StudentId" = st."Id" AND e."CourseId" = c."Id";
INSERT INTO "Payments" ("Id", "TenantId", "AccountHolderId", "EnrollmentId", "Amount", "PaymentDate", "PaymentMethod", "PaymentType", "TransactionId", "PaymentInfoJson", "Notes", "CreatedAt")
SELECT gen_random_uuid(), '$TenantId', "Id", NULL, 100.00, NOW(), 2, 1, 'CC003', '{"cardLast4":"5678"}', 'Membership dues payment', NOW() FROM "AccountHolders" WHERE "EmailAddress" = 'michael.brown@example.com';
"@

Write-Host 'Database seeding completed successfully.'
Write-Host 'Created: 1 tenant, 5 users, 6 account holders, 2 semesters, 5 students, 5 rooms, 5 courses, 5 instructors, 6 enrollments, 6 payments.'

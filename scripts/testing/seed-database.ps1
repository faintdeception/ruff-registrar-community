# Database seeding script for Student Registrar (Windows-native).
#
# PowerShell equivalent of seed-database.sh. Populates the local database with
# the same sample data (tenant, users, account holders, semesters, students,
# rooms, courses, instructors, enrollments, payments) for E2E testing.
#
# Windows-first parity notes:
#   - Auto-detects the Postgres container for docker compose AND Aspire runs
#     (Aspire names its container like "postgres-xxxxxxxx").
#   - Defaults to the fixed local dev password 'postgres123'.
#   - Supports non-interactive use via -Force and -Reset (or SEED_DATABASE_RESET=true).
#
# Examples:
#   ./scripts/testing/seed-database.ps1
#   ./scripts/testing/seed-database.ps1 -Reset -Force
#   ./scripts/testing/seed-database.ps1 -DbContainer postgres-xsjxcrxy

#Requires -Version 7.0

[CmdletBinding()]
param(
    [string]$DbName = $(if ($env:DB_NAME) { $env:DB_NAME } else { 'studentregistrar' }),
    [string]$DbUser = $(if ($env:DB_USER) { $env:DB_USER } else { 'postgres' }),
    [string]$DbPassword = $(if ($env:DB_PASSWORD) { $env:DB_PASSWORD } elseif ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { 'postgres123' }),
    [string]$DbContainer = $(if ($env:DB_CONTAINER) { $env:DB_CONTAINER } else { $env:POSTGRES_CONTAINER }),
    [string]$TenantId = $(if ($env:SEED_TENANT_ID) { $env:SEED_TENANT_ID } else { '00000000-0000-0000-0000-000000000001' }),
    # Clear existing data and reseed without prompting.
    [switch]$Reset,
    # Never prompt; assume non-interactive. Combine with -Reset to force a clean reseed.
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Message) {
    Write-Host $Message
}

function Resolve-PostgresContainer {
    if (-not [string]::IsNullOrWhiteSpace($DbContainer)) {
        return $DbContainer
    }

    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $docker) {
        return $null
    }

    # Prefer a docker compose managed postgres service when present.
    $composeId = (docker compose ps -q postgres 2>$null | Select-Object -First 1)
    if (-not [string]::IsNullOrWhiteSpace($composeId)) {
        return $composeId.Trim()
    }

    # Fall back to an Aspire-managed postgres container (named like "postgres-xxxxxxxx").
    $aspire = docker ps --format '{{.Names}}' 2>$null |
        Where-Object { $_ -match '^postgres-' } |
        Select-Object -First 1
    if (-not [string]::IsNullOrWhiteSpace($aspire)) {
        return $aspire.Trim()
    }

    # Last resort: any running container whose name contains "postgres".
    $any = docker ps --format '{{.Names}}' 2>$null |
        Where-Object { $_ -match 'postgres' } |
        Select-Object -First 1
    if (-not [string]::IsNullOrWhiteSpace($any)) {
        return $any.Trim()
    }

    return $null
}

function Invoke-Sql {
    param([Parameter(Mandatory)] [string]$Sql)

    $Sql | docker exec -i -e "PGPASSWORD=$DbPassword" $script:Container psql -U $DbUser -d $DbName -v ON_ERROR_STOP=1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "psql command failed (exit $LASTEXITCODE)."
    }
}

function Invoke-SqlScalar {
    param([Parameter(Mandatory)] [string]$Sql)

    $result = $Sql | docker exec -i -e "PGPASSWORD=$DbPassword" $script:Container psql -U $DbUser -d $DbName -t -A 2>$null
    if ($null -eq $result) {
        return ''
    }
    return (($result | Select-Object -First 1) ?? '').Trim()
}

Write-Host '🌱 Starting database seeding...'

$script:Container = Resolve-PostgresContainer
if ([string]::IsNullOrWhiteSpace($script:Container)) {
    throw 'No PostgreSQL container found. Start the app stack (Aspire AppHost or docker compose) or pass -DbContainer.'
}
Write-Step "Using PostgreSQL container '$script:Container' (db '$DbName', user '$DbUser')."

Write-Host '📊 Checking existing data...'
$accountHoldersCount = [int](Invoke-SqlScalar 'SELECT COUNT(*) FROM "AccountHolders";')
$semestersCount = [int](Invoke-SqlScalar 'SELECT COUNT(*) FROM "Semesters";')

if ($accountHoldersCount -gt 0 -or $semestersCount -gt 0) {
    $doReset = $false
    if ($Reset -or $env:SEED_DATABASE_RESET -eq 'true') {
        $doReset = $true
    } elseif (-not $Force) {
        $reply = Read-Host '⚠️  Database already contains data. Do you want to clear it and reseed? (y/N)'
        $doReset = $reply -match '^[Yy]'
    }

    if ($doReset) {
        Write-Host '🗑️  Clearing existing data...'
        Invoke-Sql @'
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
        Write-Host '✅ Data cleared'
    } else {
        Write-Host 'ℹ️  Seeding cancelled'
        exit 0
    }
}

Write-Host '🏢 Creating Tenant...'
Invoke-Sql @"
INSERT INTO "Tenants" ("Id", "Name", "Subdomain", "SubscriptionTier", "SubscriptionStatus", "IsComplimentary", "ThemeConfigJson", "KeycloakRealm", "AdminEmail", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('$TenantId', 'E2E Test Tenant', 'test', 1, 0, false, '{}', 'student-registrar', 'admin.test@example.com', true, NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;
"@

Write-Host '🧑‍💼 Creating Users...'
Invoke-Sql @"
INSERT INTO "Users" ("Id", "TenantId", "Email", "FirstName", "LastName", "KeycloakId", "Role", "IsActive", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', 'scoopadmin@example.com', 'Scoop', 'Admin', 'scoopadmin-keycloak-id', 3, true, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'admin.test@example.com', 'Admin', 'Test', 'admin1-keycloak-id', 3, true, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'emily.educator@example.com', 'Emily', 'Educator', 'educator1-keycloak-id', 2, true, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'sarah.johnson@example.com', 'Sarah', 'Johnson', 'parenteducator1-keycloak-id', 1, true, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'mark.member@example.com', 'Mark', 'Member', 'member1-keycloak-id', 1, true, NOW(), NOW());
"@

Write-Host '👥 Creating AccountHolders...'
Invoke-Sql @"
INSERT INTO "AccountHolders" ("Id", "TenantId", "FirstName", "LastName", "EmailAddress", "HomePhone", "MobilePhone", "AddressJson", "EmergencyContactJson", "MembershipDuesOwed", "MembershipDuesReceived", "KeycloakUserId", "MemberSince", "LastEdit", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', 'John', 'Smith', 'scoopmember@example.com', '555-0101', '555-0102', '{"street": "123 Main St", "city": "Anytown", "state": "CA", "postalCode": "12345", "country": "US"}', '{"firstName": "Jane", "lastName": "Smith", "homePhone": "555-0103", "mobilePhone": "555-0104", "email": "jane.smith@example.com"}', 100.00, 75.00, 'scoopmember-keycloak-id', '2024-01-15', NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Sarah', 'Johnson', 'sarah.johnson@example.com', '555-0201', '555-0202', '{"street": "456 Oak Ave", "city": "Somewhere", "state": "CA", "postalCode": "12346", "country": "US"}', '{"firstName": "Mike", "lastName": "Johnson", "homePhone": "555-0203", "mobilePhone": "555-0204", "email": "mike.johnson@example.com"}', 150.00, 150.00, 'parenteducator1-keycloak-id', '2023-09-10', NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Michael', 'Brown', 'michael.brown@example.com', '555-0301', '555-0302', '{"street": "789 Pine Rd", "city": "Elsewhere", "state": "CA", "postalCode": "12347", "country": "US"}', '{"firstName": "Lisa", "lastName": "Brown", "homePhone": "555-0303", "mobilePhone": "555-0304", "email": "lisa.brown@example.com"}', 200.00, 100.00, 'michael-keycloak-id', '2024-03-20', NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Admin', 'Test', 'admin.test@example.com', '555-1101', '555-1102', '{"street": "100 Admin St", "city": "AdminTown", "state": "CA", "postalCode": "12348", "country": "US"}', '{"firstName": "Test", "lastName": "Admin", "homePhone": "555-1103", "mobilePhone": "555-1104", "email": "test.admin@example.com"}', 0.00, 0.00, 'admin1-keycloak-id', NOW(), NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Emily', 'Educator', 'emily.educator@example.com', '555-1201', '555-1202', '{"street": "200 Educator Ave", "city": "TeacherTown", "state": "CA", "postalCode": "12349", "country": "US"}', '{"firstName": "Education", "lastName": "Contact", "homePhone": "555-1203", "mobilePhone": "555-1204", "email": "education.contact@example.com"}', 0.00, 0.00, 'educator1-keycloak-id', NOW(), NOW(), NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Mark', 'Member', 'mark.member@example.com', '555-1301', '555-1302', '{"street": "300 Member Rd", "city": "MemberVille", "state": "CA", "postalCode": "12350", "country": "US"}', '{"firstName": "Member", "lastName": "Contact", "homePhone": "555-1303", "mobilePhone": "555-1304", "email": "member.contact@example.com"}', 50.00, 25.00, 'member1-keycloak-id', NOW(), NOW(), NOW(), NOW());
"@

Write-Host '📅 Creating Semesters...'
Invoke-Sql @"
INSERT INTO "Semesters" ("Id", "TenantId", "Name", "Code", "StartDate", "EndDate", "RegistrationStartDate", "RegistrationEndDate", "IsActive", "PeriodConfigJson", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', 'Fall 2025', 'FALL2025', '2025-09-01', '2025-12-15', '2025-08-01', '2025-08-31', true, '{"periods": [{"name": "Period 1", "code": "P1", "startDate": "2025-09-01", "endDate": "2025-12-15", "isActive": true, "description": "Morning session"}, {"name": "Period 2", "code": "P2", "startDate": "2025-09-01", "endDate": "2025-12-15", "isActive": true, "description": "Mid-morning session"}, {"name": "Period 3", "code": "P3", "startDate": "2025-09-01", "endDate": "2025-12-15", "isActive": true, "description": "Afternoon session"}, {"name": "Period 4", "code": "P4", "startDate": "2025-09-01", "endDate": "2025-12-15", "isActive": true, "description": "Late afternoon session"}], "holidays": [{"name": "Labor Day", "date": "2025-09-01", "description": "National holiday"}, {"name": "Thanksgiving", "date": "2025-11-27", "description": "Thanksgiving break"}]}', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Spring 2026', 'SPRING2026', '2026-01-15', '2026-05-15', '2025-12-01', '2026-01-10', false, '{"periods": [{"name": "Period 1", "code": "P1", "startDate": "2026-01-15", "endDate": "2026-05-15", "isActive": true, "description": "Morning session"}, {"name": "Period 2", "code": "P2", "startDate": "2026-01-15", "endDate": "2026-05-15", "isActive": true, "description": "Mid-morning session"}, {"name": "Period 3", "code": "P3", "startDate": "2026-01-15", "endDate": "2026-05-15", "isActive": true, "description": "Afternoon session"}, {"name": "Period 4", "code": "P4", "startDate": "2026-01-15", "endDate": "2026-05-15", "isActive": true, "description": "Late afternoon session"}], "holidays": [{"name": "Presidents Day", "date": "2026-02-16", "description": "National holiday"}, {"name": "Spring Break", "date": "2026-03-23", "description": "Spring break week"}]}', NOW(), NOW());
"@

Write-Host '👨‍👩‍👧‍👦 Creating Students...'
$scoopMemberId = Invoke-SqlScalar "SELECT ""Id"" FROM ""AccountHolders"" WHERE ""EmailAddress"" = 'scoopmember@example.com';"
$sarahId = Invoke-SqlScalar "SELECT ""Id"" FROM ""AccountHolders"" WHERE ""EmailAddress"" = 'sarah.johnson@example.com';"
$michaelId = Invoke-SqlScalar "SELECT ""Id"" FROM ""AccountHolders"" WHERE ""EmailAddress"" = 'michael.brown@example.com';"
Invoke-Sql @"
INSERT INTO "Students" ("Id", "TenantId", "AccountHolderId", "FirstName", "LastName", "Grade", "DateOfBirth", "StudentInfoJson", "Notes", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', '$scoopMemberId', 'Emma', 'Smith', '3', '2016-04-15', '{"specialConditions": ["Allergic to peanuts"], "allergies": ["Peanuts", "Tree nuts"], "medications": [], "preferredName": "Em", "parentNotes": "Very outgoing child, loves art"}', 'Enrolled in art classes', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$scoopMemberId', 'Liam', 'Smith', '1', '2018-09-22', '{"specialConditions": [], "allergies": [], "medications": [], "preferredName": "Liam", "parentNotes": "Shy but loves science"}', 'Interested in science experiments', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$sarahId', 'Sophia', 'Johnson', '5', '2014-11-08', '{"specialConditions": ["ADHD"], "allergies": [], "medications": ["Ritalin"], "preferredName": "Sophie", "parentNotes": "Needs movement breaks"}', 'Requires accommodations for ADHD', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$michaelId', 'Oliver', 'Brown', '2', '2017-02-14', '{"specialConditions": [], "allergies": ["Dairy"], "medications": [], "preferredName": "Ollie", "parentNotes": "Lactose intolerant"}', 'Bring dairy-free snacks', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$michaelId', 'Ava', 'Brown', '4', '2015-07-30', '{"specialConditions": [], "allergies": [], "medications": [], "preferredName": "Ava", "parentNotes": "Very social and helpful"}', 'Natural leader, good with younger kids', NOW(), NOW());
"@

Write-Host '🏫 Creating Rooms...'
Invoke-Sql @"
INSERT INTO "Rooms" ("Id", "TenantId", "Name", "Capacity", "Notes", "RoomType", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', 'Art Room A', 15, 'Art supplies and easels available', 0, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Science Lab', 12, 'Safety equipment and lab benches', 1, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Classroom B', 20, 'Standard classroom with tables and chairs', 0, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Math Room', 10, 'Whiteboards and calculators available', 0, NOW(), NOW()),
(gen_random_uuid(), '$TenantId', 'Auditorium', 50, 'Stage and sound system for performances', 2, NOW(), NOW());
"@

Write-Host '🎓 Creating Courses...'
$fall2025Id = Invoke-SqlScalar "SELECT ""Id"" FROM ""Semesters"" WHERE ""Code"" = 'FALL2025';"
$artRoomId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Rooms"" WHERE ""Name"" = 'Art Room A';"
$scienceLabId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Rooms"" WHERE ""Name"" = 'Science Lab';"
$classroomBId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Rooms"" WHERE ""Name"" = 'Classroom B';"
$mathRoomId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Rooms"" WHERE ""Name"" = 'Math Room';"
$auditoriumId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Rooms"" WHERE ""Name"" = 'Auditorium';"
Invoke-Sql @"
INSERT INTO "Courses" ("Id", "TenantId", "SemesterId", "Name", "Code", "Description", "RoomId", "MaxCapacity", "Fee", "PeriodCode", "StartTime", "EndTime", "CourseConfigJson", "AgeGroup", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', '$fall2025Id', 'Beginning Art', 'ART101', 'Introduction to basic art techniques using various media', '$artRoomId', 12, 45.00, 'P1', '10:00', '11:00', '{"prerequisites": [], "materials": ["Watercolors", "Brushes", "Paper"], "daysOfWeek": ["Monday", "Wednesday", "Friday"], "gradeRange": "K-5"}', 'Elementary', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$fall2025Id', 'Young Scientists', 'SCI101', 'Hands-on science experiments for curious minds', '$scienceLabId', 10, 50.00, 'P2', '11:15', '12:15', '{"prerequisites": [], "materials": ["Safety goggles", "Lab notebook"], "daysOfWeek": ["Tuesday", "Thursday"], "gradeRange": "1-3"}', 'Elementary', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$fall2025Id', 'Creative Writing', 'ENG201', 'Develop writing skills through storytelling and poetry', '$classroomBId', 15, 40.00, 'P3', '13:00', '14:00', '{"prerequisites": ["Basic reading skills"], "materials": ["Notebook", "Pencils"], "daysOfWeek": ["Monday", "Wednesday"], "gradeRange": "3-6"}', 'Elementary', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$fall2025Id', 'Math Games', 'MATH101', 'Make math fun with interactive games and puzzles', '$mathRoomId', 8, 35.00, 'P4', '14:15', '15:15', '{"prerequisites": [], "materials": ["Calculator", "Workbook"], "daysOfWeek": ["Tuesday", "Thursday"], "gradeRange": "K-4"}', 'Elementary', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$fall2025Id', 'Drama Club', 'DRAMA101', 'Explore acting, improvisation, and theater skills', '$auditoriumId', 20, 55.00, 'P1', '10:00', '11:00', '{"prerequisites": [], "materials": ["Comfortable clothes"], "daysOfWeek": ["Friday"], "gradeRange": "2-8"}', 'Elementary/Middle', NOW(), NOW());
"@

Write-Host '👨‍🏫 Creating Course Instructors...'
$artCourseId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Courses"" WHERE ""Code"" = 'ART101';"
$sciCourseId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Courses"" WHERE ""Code"" = 'SCI101';"
$engCourseId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Courses"" WHERE ""Code"" = 'ENG201';"
$mathCourseId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Courses"" WHERE ""Code"" = 'MATH101';"
$dramaCourseId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Courses"" WHERE ""Code"" = 'DRAMA101';"
Invoke-Sql @"
INSERT INTO "CourseInstructors" ("Id", "TenantId", "CourseId", "FirstName", "LastName", "Email", "Phone", "IsPrimary", "InstructorInfoJson", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', '$artCourseId', 'Maria', 'Rodriguez', 'maria.rodriguez@example.com', '555-1001', true, '{"bio": "Professional artist with 10 years teaching experience", "qualifications": ["BFA in Fine Arts", "Elementary Teaching Certificate"]}', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$sciCourseId', 'David', 'Chen', 'david.chen@example.com', '555-1002', true, '{"bio": "Former NASA engineer turned educator", "qualifications": ["MS in Aerospace Engineering", "Science Education Certificate"]}', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$engCourseId', 'Jennifer', 'Williams', 'jennifer.williams@example.com', '555-1003', true, '{"bio": "Published author and writing instructor", "qualifications": ["MFA in Creative Writing", "Published novelist"]}', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$mathCourseId', 'Robert', 'Taylor', 'robert.taylor@example.com', '555-1004', true, '{"bio": "Math teacher who makes numbers fun", "qualifications": ["MS in Mathematics", "Elementary Education Certificate"]}', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$dramaCourseId', 'Lisa', 'Anderson', 'lisa.anderson@example.com', '555-1005', true, '{"bio": "Professional actress and drama coach", "qualifications": ["BFA in Theater Arts", "Youth Theater Director"]}', NOW(), NOW());
"@

Write-Host '📝 Creating Enrollments...'
$emmaId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Students"" WHERE ""FirstName"" = 'Emma' AND ""LastName"" = 'Smith';"
$liamId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Students"" WHERE ""FirstName"" = 'Liam' AND ""LastName"" = 'Smith';"
$sophiaId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Students"" WHERE ""FirstName"" = 'Sophia' AND ""LastName"" = 'Johnson';"
$oliverId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Students"" WHERE ""FirstName"" = 'Oliver' AND ""LastName"" = 'Brown';"
$avaId = Invoke-SqlScalar "SELECT ""Id"" FROM ""Students"" WHERE ""FirstName"" = 'Ava' AND ""LastName"" = 'Brown';"
Invoke-Sql @"
INSERT INTO "Enrollments" ("Id", "TenantId", "StudentId", "CourseId", "SemesterId", "EnrollmentType", "EnrollmentDate", "FeeAmount", "AmountPaid", "PaymentStatus", "EnrollmentInfoJson", "Notes", "CreatedAt", "UpdatedAt") VALUES
(gen_random_uuid(), '$TenantId', '$emmaId', '$artCourseId', '$fall2025Id', 0, NOW(), 45.00, 45.00, 1, '{"accommodations": [], "specialInstructions": "Loves to paint"}', 'Enrolled in art class', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$liamId', '$sciCourseId', '$fall2025Id', 0, NOW(), 50.00, 25.00, 2, '{"accommodations": [], "specialInstructions": "Needs encouragement"}', 'Partial payment received', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$sophiaId', '$engCourseId', '$fall2025Id', 0, NOW(), 40.00, 0.00, 0, '{"accommodations": ["Movement breaks"], "specialInstructions": "ADHD accommodations needed"}', 'Needs payment', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$oliverId', '$mathCourseId', '$fall2025Id', 0, NOW(), 35.00, 35.00, 1, '{"accommodations": [], "specialInstructions": "Dairy-free snacks only"}', 'Paid in full', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$avaId', '$dramaCourseId', '$fall2025Id', 0, NOW(), 55.00, 55.00, 1, '{"accommodations": [], "specialInstructions": "Natural leader"}', 'Loves performing', NOW(), NOW()),
(gen_random_uuid(), '$TenantId', '$emmaId', '$dramaCourseId', '$fall2025Id', 1, NOW(), 55.00, 0.00, 0, '{"accommodations": [], "specialInstructions": "Interested in drama too"}', 'Waitlisted for drama', NOW(), NOW());
"@

Write-Host '💳 Creating Payments...'
$emmaArtEnrollment = Invoke-SqlScalar "SELECT ""Id"" FROM ""Enrollments"" WHERE ""StudentId"" = '$emmaId' AND ""CourseId"" = '$artCourseId';"
$liamSciEnrollment = Invoke-SqlScalar "SELECT ""Id"" FROM ""Enrollments"" WHERE ""StudentId"" = '$liamId' AND ""CourseId"" = '$sciCourseId';"
$oliverMathEnrollment = Invoke-SqlScalar "SELECT ""Id"" FROM ""Enrollments"" WHERE ""StudentId"" = '$oliverId' AND ""CourseId"" = '$mathCourseId';"
$avaDramaEnrollment = Invoke-SqlScalar "SELECT ""Id"" FROM ""Enrollments"" WHERE ""StudentId"" = '$avaId' AND ""CourseId"" = '$dramaCourseId';"
Invoke-Sql @"
INSERT INTO "Payments" ("Id", "TenantId", "AccountHolderId", "EnrollmentId", "Amount", "PaymentDate", "PaymentMethod", "PaymentType", "TransactionId", "PaymentInfoJson", "Notes", "CreatedAt") VALUES
(gen_random_uuid(), '$TenantId', '$scoopMemberId', '$emmaArtEnrollment', 45.00, NOW(), 1, 0, 'TXN001', '{"checkNumber": "1234"}', 'Payment for Emma art class', NOW()),
(gen_random_uuid(), '$TenantId', '$scoopMemberId', '$liamSciEnrollment', 25.00, NOW(), 0, 0, 'CASH001', '{}', 'Partial payment for Liam science class', NOW()),
(gen_random_uuid(), '$TenantId', '$scoopMemberId', NULL, 75.00, NOW(), 2, 1, 'CC001', '{"cardLast4": "1234"}', 'Membership dues payment', NOW()),
(gen_random_uuid(), '$TenantId', '$michaelId', '$oliverMathEnrollment', 35.00, NOW(), 1, 0, 'TXN002', '{"checkNumber": "5678"}', 'Payment for Oliver math class', NOW()),
(gen_random_uuid(), '$TenantId', '$michaelId', '$avaDramaEnrollment', 55.00, NOW(), 2, 0, 'CC002', '{"cardLast4": "5678"}', 'Payment for Ava drama class', NOW()),
(gen_random_uuid(), '$TenantId', '$michaelId', NULL, 100.00, NOW(), 2, 1, 'CC003', '{"cardLast4": "5678"}', 'Membership dues payment', NOW());
"@

Write-Host ''
Write-Host '✅ Database seeding completed successfully!'
Write-Host ''
Write-Host '📊 Summary of created data:'
Write-Host '=========================='
Write-Host '• 1 Tenant (E2E Test Tenant)'
Write-Host '• 5 Users (1 system admin, 4 test users)'
Write-Host '• 6 AccountHolders (3 test families + 3 test users from Keycloak)'
Write-Host '• 2 Semesters (Fall 2025, Spring 2026)'
Write-Host '• 5 Students across the families'
Write-Host '• 5 Rooms (Art Room A, Science Lab, Classroom B, Math Room, Auditorium)'
Write-Host '• 5 Courses for Fall 2025'
Write-Host '• 5 Course Instructors'
Write-Host '• 6 Enrollments (including 1 waitlist)'
Write-Host '• 6 Payments (course fees and membership dues)'
Write-Host ''
Write-Host 'ℹ️  Note: User records are created with placeholder KeycloakId values'
Write-Host ''
Write-Host '🔑 System login credentials:'
Write-Host '============================'
Write-Host '• scoopadmin / ChangeThis123! (Administrator) [SYSTEM ACCOUNT]'
Write-Host ''
Write-Host '🧪 Test login credentials:'
Write-Host '=========================='
Write-Host '• admin1 / AdminPass123! (Administrator) [TEST ONLY]'
Write-Host '• educator1 / EducatorPass123! (Educator) [TEST ONLY]'
Write-Host '• member1 / MemberPass123! (Member) [TEST ONLY]'
Write-Host ''
Write-Host '🎉 Ready for testing!'

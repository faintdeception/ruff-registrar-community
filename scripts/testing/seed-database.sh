#!/bin/bash

# Database seeding script for Student Registrar Data
# This script populates the database with sample data for testing

set -e

echo "🌱 Starting database seeding..."


# Prompt for required DB connection details, with sensible defaults
read -p "Enter DB host [localhost]: " DB_HOST
DB_HOST=${DB_HOST:-localhost}

read -p "Enter DB port (required): " DB_PORT
if [ -z "$DB_PORT" ]; then
  echo "Error: DB port is required."
  exit 1
fi

read -p "Enter DB name [studentregistrar]: " DB_NAME
DB_NAME=${DB_NAME:-studentregistrar}

read -p "Enter DB user [postgres]: " DB_USER
DB_USER=${DB_USER:-postgres}

read -p "Enter DB password (required): " DB_PASSWORD
if [ -z "$DB_PASSWORD" ]; then
  echo "Error: DB password is required."
  exit 1
fi

read -p "Enter DB container name (required): " DB_CONTAINER
if [ -z "$DB_CONTAINER" ]; then
  echo "Error: DB container name is required."
  exit 1
fi

# Function to execute SQL
execute_sql() {
    local sql="$1"
    if [ -z "$DB_PASSWORD" ]; then
        docker exec "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c "$sql"
    else
        docker exec -e PGPASSWORD="$DB_PASSWORD" "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c "$sql"
    fi
}

# Function to execute SQL query that returns a value
execute_sql_query() {
    local sql="$1"
    if [ -z "$DB_PASSWORD" ]; then
        docker exec "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -t -c "$sql"
    else
        docker exec -e PGPASSWORD="$DB_PASSWORD" "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -t -c "$sql"
    fi
}

# Function to check if table exists and has data
check_table_data() {
    local table="$1"
    local count=$(execute_sql_query "SELECT COUNT(*) FROM \"$table\";")
    echo $count
}

echo "📊 Checking existing data..."

# Check if we need to seed
account_holders_count=$(check_table_data "AccountHolders")
semesters_count=$(check_table_data "Semesters")

if [ "$account_holders_count" -gt 0 ] || [ "$semesters_count" -gt 0 ]; then
    read -p "⚠️  Database already contains data. Do you want to clear it and reseed? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        echo "🗑️  Clearing existing data..."
        execute_sql "TRUNCATE TABLE \"Payments\" CASCADE;"
        execute_sql "TRUNCATE TABLE \"Enrollments\" CASCADE;"
        execute_sql "TRUNCATE TABLE \"CourseInstructors\" CASCADE;"
        execute_sql "TRUNCATE TABLE \"Courses\" CASCADE;"
        execute_sql "TRUNCATE TABLE \"Rooms\" CASCADE;"
        execute_sql "TRUNCATE TABLE \"Students\" CASCADE;"
        execute_sql "TRUNCATE TABLE \"Semesters\" CASCADE;"
        execute_sql "TRUNCATE TABLE \"AccountHolders\" CASCADE;"
        execute_sql "TRUNCATE TABLE \"Users\" CASCADE;"
        echo "✅ Data cleared"
    else
        echo "ℹ️  Seeding cancelled"
        exit 0
    fi
fi

echo "🧑‍💼 Creating Users..."

# Create User records for both system and test users
# Note: KeycloakId values are placeholders - in real usage these would come from Keycloak
execute_sql "INSERT INTO \"Users\" (\"Id\", \"Email\", \"FirstName\", \"LastName\", \"KeycloakId\", \"Role\", \"IsActive\", \"CreatedAt\", \"UpdatedAt\") VALUES
(gen_random_uuid(), 'scoopadmin@example.com', 'Scoop', 'Admin', 'scoopadmin-keycloak-id', 3, true, NOW(), NOW()),
(gen_random_uuid(), 'admin.test@example.com', 'Admin', 'Test', 'admin1-keycloak-id', 3, true, NOW(), NOW()),
(gen_random_uuid(), 'emily.educator@example.com', 'Emily', 'Educator', 'educator1-keycloak-id', 2, true, NOW(), NOW()),
(gen_random_uuid(), 'sarah.johnson@example.com', 'Sarah', 'Johnson', 'parenteducator1-keycloak-id', 1, true, NOW(), NOW()),
(gen_random_uuid(), 'mark.member@example.com', 'Mark', 'Member', 'member1-keycloak-id', 1, true, NOW(), NOW());"

echo "👥 Creating AccountHolders..."

# Create AccountHolders (both test families and test users from Keycloak)
execute_sql "INSERT INTO \"AccountHolders\" (\"Id\", \"FirstName\", \"LastName\", \"EmailAddress\", \"HomePhone\", \"MobilePhone\", \"AddressJson\", \"EmergencyContactJson\", \"MembershipDuesOwed\", \"MembershipDuesReceived\", \"KeycloakUserId\", \"MemberSince\", \"LastEdit\", \"CreatedAt\", \"UpdatedAt\") VALUES
(gen_random_uuid(), 'John', 'Smith', 'scoopmember@example.com', '555-0101', '555-0102', '{\"street\": \"123 Main St\", \"city\": \"Anytown\", \"state\": \"CA\", \"postalCode\": \"12345\", \"country\": \"US\"}', '{\"firstName\": \"Jane\", \"lastName\": \"Smith\", \"homePhone\": \"555-0103\", \"mobilePhone\": \"555-0104\", \"email\": \"jane.smith@example.com\"}', 100.00, 75.00, 'scoopmember-keycloak-id', '2024-01-15', NOW(), NOW(), NOW()),
(gen_random_uuid(), 'Sarah', 'Johnson', 'sarah.johnson@example.com', '555-0201', '555-0202', '{\"street\": \"456 Oak Ave\", \"city\": \"Somewhere\", \"state\": \"CA\", \"postalCode\": \"12346\", \"country\": \"US\"}', '{\"firstName\": \"Mike\", \"lastName\": \"Johnson\", \"homePhone\": \"555-0203\", \"mobilePhone\": \"555-0204\", \"email\": \"mike.johnson@example.com\"}', 150.00, 150.00, 'parenteducator1-keycloak-id', '2023-09-10', NOW(), NOW(), NOW()),
(gen_random_uuid(), 'Michael', 'Brown', 'michael.brown@example.com', '555-0301', '555-0302', '{\"street\": \"789 Pine Rd\", \"city\": \"Elsewhere\", \"state\": \"CA\", \"postalCode\": \"12347\", \"country\": \"US\"}', '{\"firstName\": \"Lisa\", \"lastName\": \"Brown\", \"homePhone\": \"555-0303\", \"mobilePhone\": \"555-0304\", \"email\": \"lisa.brown@example.com\"}', 200.00, 100.00, 'michael-keycloak-id', '2024-03-20', NOW(), NOW(), NOW()),
(gen_random_uuid(), 'Admin', 'Test', 'admin.test@example.com', '555-1101', '555-1102', '{\"street\": \"100 Admin St\", \"city\": \"AdminTown\", \"state\": \"CA\", \"postalCode\": \"12348\", \"country\": \"US\"}', '{\"firstName\": \"Test\", \"lastName\": \"Admin\", \"homePhone\": \"555-1103\", \"mobilePhone\": \"555-1104\", \"email\": \"test.admin@example.com\"}', 0.00, 0.00, 'admin1-keycloak-id', NOW(), NOW(), NOW(), NOW()),
(gen_random_uuid(), 'Emily', 'Educator', 'emily.educator@example.com', '555-1201', '555-1202', '{\"street\": \"200 Educator Ave\", \"city\": \"TeacherTown\", \"state\": \"CA\", \"postalCode\": \"12349\", \"country\": \"US\"}', '{\"firstName\": \"Education\", \"lastName\": \"Contact\", \"homePhone\": \"555-1203\", \"mobilePhone\": \"555-1204\", \"email\": \"education.contact@example.com\"}', 0.00, 0.00, 'educator1-keycloak-id', NOW(), NOW(), NOW(), NOW()),
(gen_random_uuid(), 'Mark', 'Member', 'mark.member@example.com', '555-1301', '555-1302', '{\"street\": \"300 Member Rd\", \"city\": \"MemberVille\", \"state\": \"CA\", \"postalCode\": \"12350\", \"country\": \"US\"}', '{\"firstName\": \"Member\", \"lastName\": \"Contact\", \"homePhone\": \"555-1303\", \"mobilePhone\": \"555-1304\", \"email\": \"member.contact@example.com\"}', 50.00, 25.00, 'member1-keycloak-id', NOW(), NOW(), NOW(), NOW());"

echo "📅 Creating Semesters..."

# Create Semesters
execute_sql "INSERT INTO \"Semesters\" (\"Id\", \"Name\", \"Code\", \"StartDate\", \"EndDate\", \"RegistrationStartDate\", \"RegistrationEndDate\", \"IsActive\", \"PeriodConfigJson\", \"CreatedAt\", \"UpdatedAt\") VALUES
(gen_random_uuid(), 'Fall 2025', 'FALL2025', '2025-09-01', '2025-12-15', '2025-08-01', '2025-08-31', true, '{\"periods\": [{\"name\": \"Period 1\", \"code\": \"P1\", \"startDate\": \"2025-09-01\", \"endDate\": \"2025-12-15\", \"isActive\": true, \"description\": \"Morning session\"}, {\"name\": \"Period 2\", \"code\": \"P2\", \"startDate\": \"2025-09-01\", \"endDate\": \"2025-12-15\", \"isActive\": true, \"description\": \"Mid-morning session\"}, {\"name\": \"Period 3\", \"code\": \"P3\", \"startDate\": \"2025-09-01\", \"endDate\": \"2025-12-15\", \"isActive\": true, \"description\": \"Afternoon session\"}, {\"name\": \"Period 4\", \"code\": \"P4\", \"startDate\": \"2025-09-01\", \"endDate\": \"2025-12-15\", \"isActive\": true, \"description\": \"Late afternoon session\"}], \"holidays\": [{\"name\": \"Labor Day\", \"date\": \"2025-09-01\", \"description\": \"National holiday\"}, {\"name\": \"Thanksgiving\", \"date\": \"2025-11-27\", \"description\": \"Thanksgiving break\"}]}', NOW(), NOW()),
(gen_random_uuid(), 'Spring 2026', 'SPRING2026', '2026-01-15', '2026-05-15', '2025-12-01', '2026-01-10', false, '{\"periods\": [{\"name\": \"Period 1\", \"code\": \"P1\", \"startDate\": \"2026-01-15\", \"endDate\": \"2026-05-15\", \"isActive\": true, \"description\": \"Morning session\"}, {\"name\": \"Period 2\", \"code\": \"P2\", \"startDate\": \"2026-01-15\", \"endDate\": \"2026-05-15\", \"isActive\": true, \"description\": \"Mid-morning session\"}, {\"name\": \"Period 3\", \"code\": \"P3\", \"startDate\": \"2026-01-15\", \"endDate\": \"2026-05-15\", \"isActive\": true, \"description\": \"Afternoon session\"}, {\"name\": \"Period 4\", \"code\": \"P4\", \"startDate\": \"2026-01-15\", \"endDate\": \"2026-05-15\", \"isActive\": true, \"description\": \"Late afternoon session\"}], \"holidays\": [{\"name\": \"Presidents Day\", \"date\": \"2026-02-16\", \"description\": \"National holiday\"}, {\"name\": \"Spring Break\", \"date\": \"2026-03-23\", \"description\": \"Spring break week\"}]}', NOW(), NOW());"

echo "👨‍👩‍👧‍👦 Creating Students..."

# Get AccountHolder IDs for foreign key references
SCOOPMEMBER_ID=$(execute_sql_query "SELECT \"Id\" FROM \"AccountHolders\" WHERE \"EmailAddress\" = 'scoopmember@example.com';")
SARAH_ID=$(execute_sql_query "SELECT \"Id\" FROM \"AccountHolders\" WHERE \"EmailAddress\" = 'sarah.johnson@example.com';")
MICHAEL_ID=$(execute_sql_query "SELECT \"Id\" FROM \"AccountHolders\" WHERE \"EmailAddress\" = 'michael.brown@example.com';")

# Clean up whitespace from IDs
SCOOPMEMBER_ID=$(echo "$SCOOPMEMBER_ID" | tr -d ' ')
SARAH_ID=$(echo "$SARAH_ID" | tr -d ' ')
MICHAEL_ID=$(echo "$MICHAEL_ID" | tr -d ' ')

# Create Students
execute_sql "INSERT INTO \"Students\" (\"Id\", \"AccountHolderId\", \"FirstName\", \"LastName\", \"Grade\", \"DateOfBirth\", \"StudentInfoJson\", \"Notes\", \"CreatedAt\", \"UpdatedAt\") VALUES
(gen_random_uuid(), '$SCOOPMEMBER_ID', 'Emma', 'Smith', '3', '2016-04-15', '{\"specialConditions\": [\"Allergic to peanuts\"], \"allergies\": [\"Peanuts\", \"Tree nuts\"], \"medications\": [], \"preferredName\": \"Em\", \"parentNotes\": \"Very outgoing child, loves art\"}', 'Enrolled in art classes', NOW(), NOW()),
(gen_random_uuid(), '$SCOOPMEMBER_ID', 'Liam', 'Smith', '1', '2018-09-22', '{\"specialConditions\": [], \"allergies\": [], \"medications\": [], \"preferredName\": \"Liam\", \"parentNotes\": \"Shy but loves science\"}', 'Interested in science experiments', NOW(), NOW()),
(gen_random_uuid(), '$SARAH_ID', 'Sophia', 'Johnson', '5', '2014-11-08', '{\"specialConditions\": [\"ADHD\"], \"allergies\": [], \"medications\": [\"Ritalin\"], \"preferredName\": \"Sophie\", \"parentNotes\": \"Needs movement breaks\"}', 'Requires accommodations for ADHD', NOW(), NOW()),
(gen_random_uuid(), '$MICHAEL_ID', 'Oliver', 'Brown', '2', '2017-02-14', '{\"specialConditions\": [], \"allergies\": [\"Dairy\"], \"medications\": [], \"preferredName\": \"Ollie\", \"parentNotes\": \"Lactose intolerant\"}', 'Bring dairy-free snacks', NOW(), NOW()),
(gen_random_uuid(), '$MICHAEL_ID', 'Ava', 'Brown', '4', '2015-07-30', '{\"specialConditions\": [], \"allergies\": [], \"medications\": [], \"preferredName\": \"Ava\", \"parentNotes\": \"Very social and helpful\"}', 'Natural leader, good with younger kids', NOW(), NOW());"

echo "� Creating Rooms..."

# Create Rooms first (needed for Courses foreign key)
execute_sql "INSERT INTO \"Rooms\" (\"Id\", \"Name\", \"Capacity\", \"Notes\", \"RoomType\", \"CreatedAt\", \"UpdatedAt\") VALUES
(gen_random_uuid(), 'Art Room A', 15, 'Art supplies and easels available', 0, NOW(), NOW()),
(gen_random_uuid(), 'Science Lab', 12, 'Safety equipment and lab benches', 1, NOW(), NOW()),
(gen_random_uuid(), 'Classroom B', 20, 'Standard classroom with tables and chairs', 0, NOW(), NOW()),
(gen_random_uuid(), 'Math Room', 10, 'Whiteboards and calculators available', 0, NOW(), NOW()),
(gen_random_uuid(), 'Auditorium', 50, 'Stage and sound system for performances', 2, NOW(), NOW());"

echo "�🎓 Creating Courses..."

# Get Semester ID for Fall 2025
FALL2025_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Semesters\" WHERE \"Code\" = 'FALL2025';")
FALL2025_ID=$(echo "$FALL2025_ID" | tr -d ' ')

# Get Room IDs for course assignment
ART_ROOM_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Rooms\" WHERE \"Name\" = 'Art Room A';")
SCIENCE_LAB_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Rooms\" WHERE \"Name\" = 'Science Lab';")
CLASSROOM_B_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Rooms\" WHERE \"Name\" = 'Classroom B';")
MATH_ROOM_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Rooms\" WHERE \"Name\" = 'Math Room';")
AUDITORIUM_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Rooms\" WHERE \"Name\" = 'Auditorium';")

# Clean up whitespace from IDs
ART_ROOM_ID=$(echo "$ART_ROOM_ID" | tr -d ' ')
SCIENCE_LAB_ID=$(echo "$SCIENCE_LAB_ID" | tr -d ' ')
CLASSROOM_B_ID=$(echo "$CLASSROOM_B_ID" | tr -d ' ')
MATH_ROOM_ID=$(echo "$MATH_ROOM_ID" | tr -d ' ')
AUDITORIUM_ID=$(echo "$AUDITORIUM_ID" | tr -d ' ')

# Create Courses
execute_sql "INSERT INTO \"Courses\" (\"Id\", \"SemesterId\", \"Name\", \"Code\", \"Description\", \"RoomId\", \"MaxCapacity\", \"Fee\", \"PeriodCode\", \"StartTime\", \"EndTime\", \"CourseConfigJson\", \"AgeGroup\", \"CreatedAt\", \"UpdatedAt\") VALUES
(gen_random_uuid(), '$FALL2025_ID', 'Beginning Art', 'ART101', 'Introduction to basic art techniques using various media', '$ART_ROOM_ID', 12, 45.00, 'P1', '10:00', '11:00', '{\"prerequisites\": [], \"materials\": [\"Watercolors\", \"Brushes\", \"Paper\"], \"daysOfWeek\": [\"Monday\", \"Wednesday\", \"Friday\"], \"gradeRange\": \"K-5\"}', 'Elementary', NOW(), NOW()),
(gen_random_uuid(), '$FALL2025_ID', 'Young Scientists', 'SCI101', 'Hands-on science experiments for curious minds', '$SCIENCE_LAB_ID', 10, 50.00, 'P2', '11:15', '12:15', '{\"prerequisites\": [], \"materials\": [\"Safety goggles\", \"Lab notebook\"], \"daysOfWeek\": [\"Tuesday\", \"Thursday\"], \"gradeRange\": \"1-3\"}', 'Elementary', NOW(), NOW()),
(gen_random_uuid(), '$FALL2025_ID', 'Creative Writing', 'ENG201', 'Develop writing skills through storytelling and poetry', '$CLASSROOM_B_ID', 15, 40.00, 'P3', '13:00', '14:00', '{\"prerequisites\": [\"Basic reading skills\"], \"materials\": [\"Notebook\", \"Pencils\"], \"daysOfWeek\": [\"Monday\", \"Wednesday\"], \"gradeRange\": \"3-6\"}', 'Elementary', NOW(), NOW()),
(gen_random_uuid(), '$FALL2025_ID', 'Math Games', 'MATH101', 'Make math fun with interactive games and puzzles', '$MATH_ROOM_ID', 8, 35.00, 'P4', '14:15', '15:15', '{\"prerequisites\": [], \"materials\": [\"Calculator\", \"Workbook\"], \"daysOfWeek\": [\"Tuesday\", \"Thursday\"], \"gradeRange\": \"K-4\"}', 'Elementary', NOW(), NOW()),
(gen_random_uuid(), '$FALL2025_ID', 'Drama Club', 'DRAMA101', 'Explore acting, improvisation, and theater skills', '$AUDITORIUM_ID', 20, 55.00, 'P1', '10:00', '11:00', '{\"prerequisites\": [], \"materials\": [\"Comfortable clothes\"], \"daysOfWeek\": [\"Friday\"], \"gradeRange\": \"2-8\"}', 'Elementary/Middle', NOW(), NOW());"

echo "👨‍🏫 Creating Course Instructors..."

# Get Course IDs for instructor assignment
ART_COURSE_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Courses\" WHERE \"Code\" = 'ART101';")
SCI_COURSE_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Courses\" WHERE \"Code\" = 'SCI101';")
ENG_COURSE_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Courses\" WHERE \"Code\" = 'ENG201';")
MATH_COURSE_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Courses\" WHERE \"Code\" = 'MATH101';")
DRAMA_COURSE_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Courses\" WHERE \"Code\" = 'DRAMA101';")

# Clean up whitespace
ART_COURSE_ID=$(echo "$ART_COURSE_ID" | tr -d ' ')
SCI_COURSE_ID=$(echo "$SCI_COURSE_ID" | tr -d ' ')
ENG_COURSE_ID=$(echo "$ENG_COURSE_ID" | tr -d ' ')
MATH_COURSE_ID=$(echo "$MATH_COURSE_ID" | tr -d ' ')
DRAMA_COURSE_ID=$(echo "$DRAMA_COURSE_ID" | tr -d ' ')

# Create Course Instructors
execute_sql "INSERT INTO \"CourseInstructors\" (\"Id\", \"CourseId\", \"FirstName\", \"LastName\", \"Email\", \"Phone\", \"IsPrimary\", \"InstructorInfoJson\", \"CreatedAt\", \"UpdatedAt\") VALUES
(gen_random_uuid(), '$ART_COURSE_ID', 'Maria', 'Rodriguez', 'maria.rodriguez@example.com', '555-1001', true, '{\"bio\": \"Professional artist with 10 years teaching experience\", \"qualifications\": [\"BFA in Fine Arts\", \"Elementary Teaching Certificate\"]}', NOW(), NOW()),
(gen_random_uuid(), '$SCI_COURSE_ID', 'David', 'Chen', 'david.chen@example.com', '555-1002', true, '{\"bio\": \"Former NASA engineer turned educator\", \"qualifications\": [\"MS in Aerospace Engineering\", \"Science Education Certificate\"]}', NOW(), NOW()),
(gen_random_uuid(), '$ENG_COURSE_ID', 'Jennifer', 'Williams', 'jennifer.williams@example.com', '555-1003', true, '{\"bio\": \"Published author and writing instructor\", \"qualifications\": [\"MFA in Creative Writing\", \"Published novelist\"]}', NOW(), NOW()),
(gen_random_uuid(), '$MATH_COURSE_ID', 'Robert', 'Taylor', 'robert.taylor@example.com', '555-1004', true, '{\"bio\": \"Math teacher who makes numbers fun\", \"qualifications\": [\"MS in Mathematics\", \"Elementary Education Certificate\"]}', NOW(), NOW()),
(gen_random_uuid(), '$DRAMA_COURSE_ID', 'Lisa', 'Anderson', 'lisa.anderson@example.com', '555-1005', true, '{\"bio\": \"Professional actress and drama coach\", \"qualifications\": [\"BFA in Theater Arts\", \"Youth Theater Director\"]}', NOW(), NOW());"

echo "📝 Creating Enrollments..."

# Get Student IDs for enrollment
EMMA_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Students\" WHERE \"FirstName\" = 'Emma' AND \"LastName\" = 'Smith';")
LIAM_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Students\" WHERE \"FirstName\" = 'Liam' AND \"LastName\" = 'Smith';")
SOPHIA_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Students\" WHERE \"FirstName\" = 'Sophia' AND \"LastName\" = 'Johnson';")
OLIVER_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Students\" WHERE \"FirstName\" = 'Oliver' AND \"LastName\" = 'Brown';")
AVA_ID=$(execute_sql_query "SELECT \"Id\" FROM \"Students\" WHERE \"FirstName\" = 'Ava' AND \"LastName\" = 'Brown';")

# Clean up whitespace
EMMA_ID=$(echo "$EMMA_ID" | tr -d ' ')
LIAM_ID=$(echo "$LIAM_ID" | tr -d ' ')
SOPHIA_ID=$(echo "$SOPHIA_ID" | tr -d ' ')
OLIVER_ID=$(echo "$OLIVER_ID" | tr -d ' ')
AVA_ID=$(echo "$AVA_ID" | tr -d ' ')

# Create Enrollments
execute_sql "INSERT INTO \"Enrollments\" (\"Id\", \"StudentId\", \"CourseId\", \"SemesterId\", \"EnrollmentType\", \"EnrollmentDate\", \"FeeAmount\", \"AmountPaid\", \"PaymentStatus\", \"EnrollmentInfoJson\", \"Notes\", \"CreatedAt\", \"UpdatedAt\") VALUES
(gen_random_uuid(), '$EMMA_ID', '$ART_COURSE_ID', '$FALL2025_ID', 0, NOW(), 45.00, 45.00, 1, '{\"accommodations\": [], \"specialInstructions\": \"Loves to paint\"}', 'Enrolled in art class', NOW(), NOW()),
(gen_random_uuid(), '$LIAM_ID', '$SCI_COURSE_ID', '$FALL2025_ID', 0, NOW(), 50.00, 25.00, 2, '{\"accommodations\": [], \"specialInstructions\": \"Needs encouragement\"}', 'Partial payment received', NOW(), NOW()),
(gen_random_uuid(), '$SOPHIA_ID', '$ENG_COURSE_ID', '$FALL2025_ID', 0, NOW(), 40.00, 0.00, 0, '{\"accommodations\": [\"Movement breaks\"], \"specialInstructions\": \"ADHD accommodations needed\"}', 'Needs payment', NOW(), NOW()),
(gen_random_uuid(), '$OLIVER_ID', '$MATH_COURSE_ID', '$FALL2025_ID', 0, NOW(), 35.00, 35.00, 1, '{\"accommodations\": [], \"specialInstructions\": \"Dairy-free snacks only\"}', 'Paid in full', NOW(), NOW()),
(gen_random_uuid(), '$AVA_ID', '$DRAMA_COURSE_ID', '$FALL2025_ID', 0, NOW(), 55.00, 55.00, 1, '{\"accommodations\": [], \"specialInstructions\": \"Natural leader\"}', 'Loves performing', NOW(), NOW()),
(gen_random_uuid(), '$EMMA_ID', '$DRAMA_COURSE_ID', '$FALL2025_ID', 1, NOW(), 55.00, 0.00, 0, '{\"accommodations\": [], \"specialInstructions\": \"Interested in drama too\"}', 'Waitlisted for drama', NOW(), NOW());"

echo "💳 Creating Payments..."

# Get Enrollment IDs for payment tracking
EMMA_ART_ENROLLMENT=$(execute_sql_query "SELECT \"Id\" FROM \"Enrollments\" WHERE \"StudentId\" = '$EMMA_ID' AND \"CourseId\" = '$ART_COURSE_ID';")
LIAM_SCI_ENROLLMENT=$(execute_sql_query "SELECT \"Id\" FROM \"Enrollments\" WHERE \"StudentId\" = '$LIAM_ID' AND \"CourseId\" = '$SCI_COURSE_ID';")
OLIVER_MATH_ENROLLMENT=$(execute_sql_query "SELECT \"Id\" FROM \"Enrollments\" WHERE \"StudentId\" = '$OLIVER_ID' AND \"CourseId\" = '$MATH_COURSE_ID';")
AVA_DRAMA_ENROLLMENT=$(execute_sql_query "SELECT \"Id\" FROM \"Enrollments\" WHERE \"StudentId\" = '$AVA_ID' AND \"CourseId\" = '$DRAMA_COURSE_ID';")

# Clean up whitespace
EMMA_ART_ENROLLMENT=$(echo "$EMMA_ART_ENROLLMENT" | tr -d ' ')
LIAM_SCI_ENROLLMENT=$(echo "$LIAM_SCI_ENROLLMENT" | tr -d ' ')
OLIVER_MATH_ENROLLMENT=$(echo "$OLIVER_MATH_ENROLLMENT" | tr -d ' ')
AVA_DRAMA_ENROLLMENT=$(echo "$AVA_DRAMA_ENROLLMENT" | tr -d ' ')

# Create Payments
execute_sql "INSERT INTO \"Payments\" (\"Id\", \"AccountHolderId\", \"EnrollmentId\", \"Amount\", \"PaymentDate\", \"PaymentMethod\", \"PaymentType\", \"TransactionId\", \"PaymentInfoJson\", \"Notes\", \"CreatedAt\") VALUES
(gen_random_uuid(), '$SCOOPMEMBER_ID', '$EMMA_ART_ENROLLMENT', 45.00, NOW(), 1, 0, 'TXN001', '{\"checkNumber\": \"1234\"}', 'Payment for Emma art class', NOW()),
(gen_random_uuid(), '$SCOOPMEMBER_ID', '$LIAM_SCI_ENROLLMENT', 25.00, NOW(), 0, 0, 'CASH001', '{}', 'Partial payment for Liam science class', NOW()),
(gen_random_uuid(), '$SCOOPMEMBER_ID', NULL, 75.00, NOW(), 2, 1, 'CC001', '{\"cardLast4\": \"1234\"}', 'Membership dues payment', NOW()),
(gen_random_uuid(), '$MICHAEL_ID', '$OLIVER_MATH_ENROLLMENT', 35.00, NOW(), 1, 0, 'TXN002', '{\"checkNumber\": \"5678\"}', 'Payment for Oliver math class', NOW()),
(gen_random_uuid(), '$MICHAEL_ID', '$AVA_DRAMA_ENROLLMENT', 55.00, NOW(), 2, 0, 'CC002', '{\"cardLast4\": \"5678\"}', 'Payment for Ava drama class', NOW()),
(gen_random_uuid(), '$MICHAEL_ID', NULL, 100.00, NOW(), 2, 1, 'CC003', '{\"cardLast4\": \"5678\"}', 'Membership dues payment', NOW());"

echo "✅ Database seeding completed successfully!"
echo ""
echo "📊 Summary of created data:"
echo "=========================="
echo "• 4 Users (1 system admin, 3 test users)"
echo "• 6 AccountHolders (3 test families + 3 test users from Keycloak)"
echo "• 2 Semesters (Fall 2025, Spring 2026)"
echo "• 5 Students across the families"
echo "• 5 Rooms (Art Room A, Science Lab, Classroom B, Math Room, Auditorium)"
echo "• 5 Courses for Fall 2025"
echo "• 5 Course Instructors"
echo "• 6 Enrollments (including 1 waitlist)"
echo "• 6 Payments (course fees and membership dues)"
echo ""
echo "ℹ️  Note: User records are created with placeholder KeycloakId values"
echo ""
echo "🔑 System login credentials:"
echo "============================"
echo "• scoopadmin / changethis123! (Administrator) [SYSTEM ACCOUNT]"
echo ""
echo "🧪 Test login credentials:"
echo "=========================="
echo "• admin1 / AdminPass123! (Administrator) [TEST ONLY]"
echo "• educator1 / EducatorPass123! (Educator) [TEST ONLY]"
echo "• member1 / MemberPass123! (Member) [TEST ONLY]"
echo ""
echo "🎉 Ready for testing!"

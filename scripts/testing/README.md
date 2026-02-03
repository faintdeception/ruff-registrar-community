# Testing Scripts

This directory contains all scripts related to testing the Student Registrar application.

## 📂 Directory Structure

```
scripts/testing/
├── run-e2e-tests.sh       # 🎯 Main E2E testing script (recommended)
├── setup-test-users.sh    # 👥 Creates test users in Keycloak
├── test-e2e-only.sh       # 🔧 Simple E2E test runner
├── seed-database.sh       # 🌱 Seeds database with test data
└── Dockerfile             # 🐳 Docker container for E2E tests
```

## 🚀 Quick Start

### Run All E2E Tests (Recommended)
```bash
# Run all tests with browser visible
./scripts/testing/run-e2e-tests.sh

# Run all tests in headless mode
./scripts/testing/run-e2e-tests.sh --headless

# Setup test users and run all tests
./scripts/testing/run-e2e-tests.sh --setup-users
```

### Run Specific Test Suites
```bash
# Run only admin tests
./scripts/testing/run-e2e-tests.sh --test-suite admin

# Run only educator tests  
./scripts/testing/run-e2e-tests.sh --test-suite educator

# Run only member tests
./scripts/testing/run-e2e-tests.sh --test-suite member

# Run only login/logout tests
./scripts/testing/run-e2e-tests.sh --test-suite login
```

### Setup Only (No Tests)
```bash
# Create test users without running tests
./scripts/testing/run-e2e-tests.sh --setup-users --no-tests
```

## 📋 Prerequisites

1. **Application Running**: Student Registrar must be running at `http://localhost:3001`
   - Recommended: `dotnet run --project src/StudentRegistrar.AppHost`
   - Or via Docker Compose: `docker-compose up frontend`

2. **Keycloak Running**: For user management (if using `--setup-users`)
   - If using Aspire: Keycloak starts with the AppHost
   - If using Docker Compose: `docker-compose up keycloak`

3. **Database Seeded** (optional for realistic scenarios)
   ```bash
   ./scripts/testing/seed-database.sh
   ```

## 🧪 Test Users

The following test users are created by `setup-test-users.sh`:

| Username   | Password         | Role        | Purpose                      |
|------------|------------------|-------------|------------------------------|
| scoopadmin | changethis123!       | Admin       | Full system access (existing) |
| educator1  | EducatorPass123! | Educator    | Teaching + family management |
| member1    | MemberPass123!   | Member      | Family management only       |

## 📊 Test Organization

Tests are organized by user roles to reflect real-world usage:

### **AdminTests** - Full System Access
- ✅ Student management
- ✅ Semester management
- ✅ All educator and member capabilities
- ✅ Complete admin workflow testing

### **EducatorTests** - Teaching + Family Management
- ✅ Course creation/management (own courses)
- ✅ Grade management (own courses)
- ✅ Family management (own children)
- ❌ Admin-only features (proper restrictions)

### **MemberTests** - Family Management Only
- ✅ Family/children management
- ✅ Course browsing and enrollment
- ✅ Viewing children's grades
- ❌ Course creation or admin features

## 🔧 Individual Scripts

### `run-e2e-tests.sh` (Recommended)
**Purpose**: Complete E2E testing workflow with setup and execution
**Features**:
- ✅ Application connectivity check
- ✅ Optional test user creation
- ✅ Flexible test suite selection
- ✅ Headless/visible browser modes
- ✅ Colored output and error handling

```bash
./scripts/testing/run-e2e-tests.sh --help  # See all options
```

### `setup-test-users.sh`
**Purpose**: Creates required test users in Keycloak
**Features**:
- ✅ Creates educator1 and member1 users
- ✅ Assigns proper roles
- ✅ Checks for existing users
- ✅ Validates Keycloak connectivity

```bash
./scripts/testing/setup-test-users.sh
```

### `test-e2e-only.sh`
**Purpose**: Simple E2E test runner (assumes app is running)
**Usage**:
```bash
./scripts/testing/test-e2e-only.sh          # Browser visible
./scripts/testing/test-e2e-only.sh headless # Headless mode
```

### `seed-database.sh`
**Purpose**: Populates database with comprehensive test data
**Features**:
- ✅ Account holders and families
- ✅ Students with realistic data
- ✅ Semesters and courses
- ✅ Enrollments and instructors
- ✅ Prevents duplicate data

```bash
./scripts/testing/seed-database.sh
```

### `Dockerfile`
**Purpose**: Containerized E2E test execution
**Usage**:
```bash
# Build test container
docker build -f scripts/testing/Dockerfile -t student-registrar-e2e .

# Run tests in container
docker run --rm student-registrar-e2e
```

## 🎯 Common Workflows

### Development Testing
```bash
# Quick test during development
./scripts/testing/run-e2e-tests.sh --test-suite login

# Full role-based testing
./scripts/testing/run-e2e-tests.sh --setup-users
```

### CI/CD Pipeline
```bash
# Headless mode for automated testing
./scripts/testing/run-e2e-tests.sh --headless --setup-users
```

### Debugging
```bash
# Browser visible for debugging
./scripts/testing/run-e2e-tests.sh --test-suite admin

# Setup users only for manual testing
./scripts/testing/run-e2e-tests.sh --setup-users --no-tests
```

## 🚨 Troubleshooting

### Application Not Running
```
❌ Application is not running at http://localhost:3001
```
**Solution**: Start the application with `dotnet run --project src/StudentRegistrar.AppHost` or `docker-compose up frontend`

### Keycloak Not Accessible
```
❌ Keycloak is not accessible at http://localhost:8080
```
**Solution**: Start Keycloak by running the AppHost or `docker-compose up keycloak`

### Realm Missing / Login Fails
Symptoms: login tests stay on `/login` or Keycloak returns `Realm does not exist`.

**Solution**:
1. Run `./setup-keycloak.sh` to recreate the realm/client.
2. Update `src/StudentRegistrar.AppHost/appsettings.json` with the new client secret.
3. Run `./scripts/keycloak/add-spa-client.sh` to ensure the public SPA client exists.
4. Restart the AppHost.
5. Run `./scripts/testing/setup-test-users.sh`.

### Test Failures
1. Check application logs: `docker-compose logs frontend`
2. Verify test users exist in Keycloak admin console
3. Run with browser visible to see what's happening
4. Check ChromeDriver compatibility

### Database Issues
**Solution**: Reseed the database with `./scripts/testing/seed-database.sh`

## 📈 Future Enhancements

- [ ] Visual regression testing
- [ ] Performance testing integration
- [ ] Cross-browser testing support
- [ ] Parallel test execution
- [ ] Test result reporting dashboard

---

**💡 Tip**: Use the main `run-e2e-tests.sh` script for most scenarios - it handles setup, validation, and provides comprehensive options for different testing needs.

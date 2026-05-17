# Testing Scripts

This directory contains all scripts related to testing the Student Registrar application.

## 📂 Directory Structure

```
scripts/testing/
├── run-ci-equivalent-e2e.sh  # 🎯 Linux/WSL/CI Docker Compose confidence lane
├── run-ci-equivalent-e2e.ps1 # 🎯 Windows/PowerShell Docker Compose confidence lane
├── run-e2e-tests.sh       # 🎯 Linux/WSL/CI E2E testing script
├── setup-test-users.sh    # 👥 Linux/WSL Keycloak test user setup
├── setup-test-users.ps1   # 👥 Windows/PowerShell test user and DB sync setup
├── verify-api-migrations.ps1 # 🧪 Standalone API migration verification
├── test-e2e-only.sh       # 🔧 Simple E2E test runner
├── seed-database.sh       # 🌱 Seeds database with test data
├── seed-database.ps1      # 🌱 Windows/PowerShell test data seed
└── Dockerfile             # 🐳 Docker container for E2E tests
```

## 🚀 Quick Start

### Run All E2E Tests

Windows PowerShell developers can use the CI-equivalent Docker Compose runner without WSL. It rebuilds the Linux images, starts the same local services used by CI, bootstraps Keycloak, syncs users, seeds sample data, runs preflight checks, and then runs E2E tests:

```powershell
./scripts/testing/run-ci-equivalent-e2e.ps1

# Fast confidence check without Selenium.
./scripts/testing/run-ci-equivalent-e2e.ps1 -PreflightOnly
```

Linux/WSL/CI users can use the Bash CI-equivalent runner or the lighter local E2E runner:

```bash
# Rebuild images, start Docker Compose, bootstrap, seed, preflight, and run E2E.
./scripts/testing/run-ci-equivalent-e2e.sh

# Run all tests with browser visible
./scripts/testing/run-e2e-tests.sh

# Run all tests in headless mode
./scripts/testing/run-e2e-tests.sh --headless

# Setup test users and run all tests
./scripts/testing/run-e2e-tests.sh --setup-users
```

The PowerShell and Bash confidence lanes should remain behaviorally equivalent. The difference is the host shell, not the target runtime: CI and Azure use Linux images/services, and the Windows PowerShell path should operate against that same topology. When changing test user, database sync, seed, or E2E setup behavior, update both paths or document the intentional gap.

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

# Windows/PowerShell: create or reset test users directly
./scripts/testing/setup-test-users.ps1 -KeycloakUrl http://localhost:8080 -AdminPassword 'admin123!'
```

### Verify API Migrations Without Launch Profile
```powershell
./scripts/testing/verify-api-migrations.ps1 `
   -StudentRegistrarConnectionString 'Host=127.0.0.1;Port=59432;Database=studentregistrar_migrationverify;Username=postgres;Password=postgres123' `
   -KeycloakUrl 'http://127.0.0.1:59454' `
   -KeycloakAuthority 'http://127.0.0.1:59454'
```

This PowerShell helper starts the API with `--no-launch-profile`, forces an isolated `BaseOutputPath`, waits for successful startup, and then stops the process. Use it when you want to prove that automatic EF migrations still apply cleanly on a fresh database without colliding with an already-running local stack.

## 📋 Prerequisites

1. **Application Running**: Student Registrar must be running at `http://localhost:3001`
   - Recommended: `dotnet run --project src/StudentRegistrar.AppHost`
   - Or via Docker Compose: `docker-compose up frontend`

2. **Keycloak Running**: For user management (if using `--setup-users`)
   - If using Aspire: Keycloak starts with the AppHost
   - If using Docker Compose: `docker-compose up keycloak`

3. **Database Seeded or Synced**
   ```bash
   ./scripts/testing/seed-database.sh
   ```
   ```powershell
   ./scripts/testing/seed-database.ps1 -Reset
   ```
   On Windows, `setup-test-users.ps1` syncs the required E2E `Users` and `AccountHolders` rows with live Keycloak IDs when a local PostgreSQL container is available, and `seed-database.ps1` creates the broader sample-data set used by the CI-equivalent runner.

## 🧪 Test Users

The following test users are created by `setup-test-users.sh` and `setup-test-users.ps1`:

| Username        | Password                 | Role     | Purpose                                      |
|-----------------|--------------------------|----------|----------------------------------------------|
| scoopadmin      | ChangeThis123!           | Admin    | Full system access (existing)                |
| admin1          | AdminPass123!            | Admin    | E2E administrator workflows                   |
| educator1       | EducatorPass123!         | Educator | Teaching + family management                 |
| member1         | MemberPass123!           | Member   | Stable member-only baseline                  |
| parenteducator1 | ParentEducatorPass123!   | Member   | Promoted during parent-as-educator workflow  |

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

### `run-ci-equivalent-e2e.ps1` (Windows/PowerShell)
**Purpose**: Windows-native CI-equivalent Docker Compose confidence lane
**Features**:
- Rebuilds API and frontend Linux Docker images
- Starts PostgreSQL, Keycloak, API, and frontend through Docker Compose
- Bootstraps Keycloak with the deterministic local client secret
- Creates E2E users and syncs live Keycloak IDs into PostgreSQL
- Seeds comprehensive E2E sample data with `seed-database.ps1`
- Runs preflight checks before optional Selenium E2E execution

```powershell
./scripts/testing/run-ci-equivalent-e2e.ps1
./scripts/testing/run-ci-equivalent-e2e.ps1 -PreflightOnly
./scripts/testing/run-ci-equivalent-e2e.ps1 -Filter 'FullyQualifiedName~Login'
```

### `run-ci-equivalent-e2e.sh` (Linux/WSL/CI)
**Purpose**: Bash CI-equivalent Docker Compose confidence lane used by CI

```bash
./scripts/testing/run-ci-equivalent-e2e.sh
./scripts/testing/run-ci-equivalent-e2e.sh --preflight-only
./scripts/testing/run-ci-equivalent-e2e.sh --filter 'FullyQualifiedName~Login'
```

### `run-e2e-tests.sh` (Linux/WSL/CI)
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
- ✅ Creates admin1, educator1, member1, and parenteducator1 users
- ✅ Assigns proper roles
- ✅ Checks for existing users
- ✅ Validates Keycloak connectivity

```bash
./scripts/testing/setup-test-users.sh
```

### `setup-test-users.ps1`
**Purpose**: Windows-native Keycloak test user setup
**Features**:
- Creates the same test users as the Bash script
- Resets baseline roles so `member1` remains member-only between E2E reruns
- Assigns the realm default role so SPA tokens include the API-accepted audience
- Syncs local `Users` and `AccountHolders` rows with live Keycloak IDs when a PostgreSQL container is available
- Supports local Aspire or deployed Keycloak URLs

```powershell
./scripts/testing/setup-test-users.ps1 -AdminPassword 'admin123!'
./scripts/testing/setup-test-users.ps1 -KeycloakUrl http://localhost:8080 -RealmName student-registrar
```

When `-KeycloakUrl` is omitted, the PowerShell script tries `http://localhost:8080` first and then falls back to the mapped Aspire Keycloak Docker port when available.
When `-DbContainer` is omitted, it auto-detects the mapped Aspire PostgreSQL container. Use `-SkipDatabaseSync` to update only Keycloak.

### `verify-api-migrations.ps1`
**Purpose**: Verifies standalone API startup and automatic EF migration application against an explicit connection string.
**Features**:
- Starts the API with `--no-launch-profile` so launch-settings HTTPS bindings do not interfere
- Uses an isolated `BaseOutputPath` so a running local API does not lock build outputs
- Accepts explicit Keycloak and PostgreSQL endpoints for fresh-database checks
- Stops the temporary API process automatically once startup is confirmed

```powershell
./scripts/testing/verify-api-migrations.ps1 `
   -StudentRegistrarConnectionString 'Host=127.0.0.1;Port=59432;Database=studentregistrar_migrationverify;Username=postgres;Password=postgres123' `
   -KeycloakUrl 'http://127.0.0.1:59454'
```

### `test-e2e-only.sh`
**Purpose**: Simple E2E test runner (assumes app is running)
**Usage**:
```bash
./scripts/testing/test-e2e-only.sh          # Browser visible
./scripts/testing/test-e2e-only.sh headless # Headless mode
```

### `seed-database.sh` / `seed-database.ps1`
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

```powershell
./scripts/testing/seed-database.ps1 -Reset
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

### Windows / PowerShell
```powershell
# Setup users and required local database rows
./scripts/testing/setup-test-users.ps1 -AdminPassword 'admin123!'

# Run the full browser suite headless
$env:SeleniumSettings__Headless='true'
dotnet test tests/StudentRegistrar.E2E.Tests/StudentRegistrar.E2E.Tests.csproj --logger "console;verbosity=normal" --collect:"XPlat Code Coverage"
Remove-Item Env:SeleniumSettings__Headless
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
1. Run `./scripts/keycloak/bootstrap-keycloak.sh` to recreate the realm/client, then `./scripts/keycloak/add-spa-client.sh` to add the public SPA client. (`./setup-keycloak.sh` is a legacy local-dev helper that should no longer be used — it does not apply password policy or PKCE hardening.)
2. Update `src/StudentRegistrar.AppHost/appsettings.json` with the new client secret.
3. Run `./scripts/keycloak/add-spa-client.sh` to ensure the public SPA client exists.
4. Restart the AppHost.
5. Run `./scripts/testing/setup-test-users.sh`, or on Windows run `./scripts/testing/setup-test-users.ps1 -AdminPassword 'admin123!'` so database rows are synced too.
6. If database state is stale, reseed/reset schema (`./scripts/testing/seed-database.sh` or `./scripts/testing/seed-database.ps1 -Reset`) before touching volumes.
7. Treat deleting local dev volumes as last resort only, after bootstrap + user sync + reseed/schema reset fail to recover the environment.

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

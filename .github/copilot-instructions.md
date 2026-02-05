# Copilot Instructions for Student Registrar

## Project Overview

Student Registrar is a comprehensive homeschool management system built with modern cloud-native technologies. The application provides student management, course creation, enrollment tracking, and grade recording capabilities with secure authentication.

## Technology Stack

- **Backend**: .NET 10 Web API with Entity Framework Core
- **Frontend**: Next.js 15 with TypeScript and Tailwind CSS
- **Database**: PostgreSQL 15
- **Authentication**: Keycloak
- **Orchestration**: .NET Aspire (for local development)
- **Testing**: xUnit (unit/integration), Selenium WebDriver (E2E)
- **Containerization**: Docker and Docker Compose

## Architecture

The solution follows a clean architecture pattern:

- `StudentRegistrar.AppHost` - .NET Aspire orchestration host
- `StudentRegistrar.ServiceDefaults` - Shared service configuration
- `StudentRegistrar.Api` - Web API with controllers and endpoints
- `StudentRegistrar.Data` - Entity Framework DbContext and repositories
- `StudentRegistrar.Models` - Domain models and DTOs
- `frontend/` - Next.js frontend application

## Development Environment

### Running the Application

**Primary Method (Recommended):**
```bash
# Run with .NET Aspire orchestration
dotnet run --project src/StudentRegistrar.AppHost

# Or use the aspire CLI
aspire run
```

**Alternative Method (Manual):**
```bash
# Terminal 1: API
cd src/StudentRegistrar.Api
dotnet run

# Terminal 2: Frontend
cd frontend
npm run dev
```

### Initial Setup

```bash
# Automated setup (recommended)
./dev-setup.sh

# Manual setup
cd frontend && npm install
dotnet restore
dotnet tool restore
dotnet ef migrations add InitialCreate --project src/StudentRegistrar.Data --startup-project src/StudentRegistrar.Api
```

### Keycloak Configuration

```bash
# Bootstrap Keycloak realm, roles, and test users
./setup-keycloak.sh

# Configure SPA client for frontend authentication
./scripts/keycloak/add-spa-client.sh
```

## Database Management

### Migrations

```bash
# Create new migration
dotnet ef migrations add <MigrationName> --project src/StudentRegistrar.Data --startup-project src/StudentRegistrar.Api

# Apply migrations
dotnet ef database update --project src/StudentRegistrar.Data --startup-project src/StudentRegistrar.Api

# Note: When using Aspire, migrations are applied automatically on startup
```

## Testing

### Unit and Integration Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/StudentRegistrar.Api.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

The project uses:
- **xUnit** for test framework
- **FluentAssertions** for readable assertions
- **Moq** for mocking
- **Microsoft.AspNetCore.Mvc.Testing** for integration tests

### End-to-End Tests

```bash
# Run all E2E tests (visible browser)
./scripts/testing/run-e2e-tests.sh

# Run in headless mode (CI/CD)
./scripts/testing/run-e2e-tests.sh --headless

# Run specific role-based test suite
./scripts/testing/run-e2e-tests.sh --test-suite admin
./scripts/testing/run-e2e-tests.sh --test-suite educator
./scripts/testing/run-e2e-tests.sh --test-suite member
```

**Test Organization:**
- `AdminTests` - Full system access tests
- `EducatorTests` - Teaching and family management tests
- `MemberTests` - Family management only tests

### Smoke Tests

```bash
# Quick validation tests
./run-smoke-tests.sh
```

## Code Style and Quality

### Linting

```bash
# Frontend linting
cd frontend
npm run lint

# .NET formatting (if configured)
dotnet format
```

## API Structure

### Key Endpoints

- **Students**: `/api/students` - CRUD operations for students
- **Courses**: `/api/courses` - CRUD operations for courses
- **Enrollments**: `/api/enrollments` - Enrollment management
- **Grades**: `/api/grades` - Grade recording and retrieval

All endpoints require authentication via Keycloak JWT tokens.

## Security Considerations

### Development Security

- **No Hardcoded Secrets**: Use .NET user secrets or environment variables
- **Auto-Generated Passwords**: Aspire generates secure passwords automatically
- **View Credentials**: Check Aspire Dashboard at http://localhost:15888

### Production Security Checklist

- Change all default passwords
- Enable HTTPS/TLS
- Use encrypted database connections
- Configure Keycloak for production
- Set specific CORS origins (no wildcards)
- Configure security headers
- Implement proper error handling (don't expose sensitive info)

### Managing Secrets

```bash
# Initialize user secrets
dotnet user-secrets init --project src/StudentRegistrar.AppHost

# Set secret values
dotnet user-secrets set "Parameters:postgres-password" "your-secure-password" --project src/StudentRegistrar.AppHost

# List secrets
dotnet user-secrets list --project src/StudentRegistrar.AppHost
```

## Common Tasks

### Adding New Entity

1. Create model in `StudentRegistrar.Models`
2. Add DbSet to `StudentRegistrarDbContext` in `StudentRegistrar.Data`
3. Create migration: `dotnet ef migrations add Add<Entity>`
4. Create controller in `StudentRegistrar.Api/Controllers`
5. Add API tests in `tests/StudentRegistrar.Api.Tests`

### Adding New Dependency

1. Add package reference: `dotnet add package <PackageName>`
2. Update `StudentRegistrar.ServiceDefaults` if shared across services
3. Ensure compatibility with .NET 10

### Frontend Development

```bash
cd frontend

# Install new package
npm install <package-name>

# Run development server with hot reload
npm run dev

# Build for production
npm run build

# Run production build locally
npm run start
```

## Aspire-Specific Guidance

For detailed Aspire orchestration guidance, see [AGENTS.md](../AGENTS.md). Key points:

- Run `aspire run` before making changes to establish known state
- Changes to `Program.cs` in AppHost require application restart
- Use Aspire MCP tools to check resource status and debug issues
- Leverage structured logs, console logs, and traces for diagnostics
- Use Playwright MCP server for functional testing

## Debugging

### Local Debugging

1. **API**: Set breakpoints in Visual Studio/VS Code, F5 to debug
2. **Frontend**: Use browser DevTools and React Developer Tools
3. **Database**: Use Aspire Dashboard to view connection strings
4. **Logs**: Check Aspire Dashboard for all service logs

### Common Issues

- **Authentication Fails**: Ensure Keycloak is configured and SPA client exists
- **Database Connection**: Check Aspire Dashboard for correct connection string
- **Frontend Can't Connect to API**: Verify API URL in frontend environment variables
- **Migrations Not Applied**: Ensure using correct startup project and connection string

## Deployment

### Docker Compose

```bash
# Build and start all services
docker-compose up --build -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Azure Deployment

See deployment documentation:
- [Azure Container Apps](./deploy/aca/README.md)
- [Azure Kubernetes Service](./deploy/aks/README.md)

## Contributing Guidelines

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make minimal, focused changes
4. Add/update tests for new functionality
5. Ensure all tests pass (`dotnet test`)
6. Run linters (frontend: `npm run lint`)
7. Update documentation if needed
8. Submit a pull request with clear description

## Documentation

- [Main README](../README.md) - Comprehensive project documentation
- [AGENTS.md](../AGENTS.md) - Aspire-specific instructions
- [Keycloak Setup](../scripts/keycloak/README.md) - Authentication configuration
- [Testing Guide](../scripts/testing/README.md) - Detailed testing documentation
- [Security](../SECURITY.md) - Security policies and reporting

## Useful Commands Reference

```bash
# Development
aspire run                                    # Start Aspire orchestration
dotnet run --project src/StudentRegistrar.AppHost  # Start application
dotnet watch run --project src/StudentRegistrar.AppHost  # Start with hot reload

# Testing
dotnet test                                   # Run all tests
./scripts/testing/run-e2e-tests.sh           # E2E tests
./run-smoke-tests.sh                         # Smoke tests

# Database
dotnet ef migrations add <Name> --project src/StudentRegistrar.Data --startup-project src/StudentRegistrar.Api
dotnet ef database update --project src/StudentRegistrar.Data --startup-project src/StudentRegistrar.Api

# Tools
dotnet tool restore                          # Restore local tools
dotnet tool install --global dotnet-outdated-tool  # Install dependency checker
```

## Getting Help

- Open an issue on GitHub for bugs or feature requests
- Check existing documentation in the repository
- Review Aspire documentation at https://aspire.dev
- Consult .NET documentation at https://learn.microsoft.com/dotnet

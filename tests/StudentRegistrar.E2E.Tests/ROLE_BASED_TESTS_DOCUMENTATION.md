# Role-Based E2E Test Organization

## Overview

The E2E tests are organized by **user roles** to reflect real-world usage patterns and permission levels. This approach provides comprehensive coverage while minimizing overlap and ensuring each role's capabilities are thoroughly tested.

## Test Structure

### 1. **Base Tests** (`Tests/LoginTests.cs`)
- **Purpose**: Foundation authentication and core functionality
- **Coverage**: 
  - Login/logout flows
  - Basic navigation
  - Common UI elements
- **User**: Admin user (most permissive for baseline tests)

### 2. **AdminTests** (`Tests/RoleBasedTests/AdminTests.cs`)
- **Purpose**: Full system access testing
- **User Role**: Administrator
- **Capabilities Tested**:
  - ✅ All basic member capabilities
  - ✅ All educator capabilities  
  - ✅ Admin-only features:
    - Student management (`/students`)
    - Semester management (`/semesters`)
    - System-wide oversight
- **Key Tests**:
  - `Admin_Should_Access_All_Navigation_Links()` - Verifies complete access
  - `Admin_Should_Manage_Complete_Workflow()` - End-to-end admin workflow

### 3. **EducatorTests** (`Tests/RoleBasedTests/EducatorTests.cs`)
- **Purpose**: Teaching + family management testing
- **User Role**: Educator
- **Capabilities Tested**:
  - ✅ All basic member capabilities (for own family)
  - ✅ Course creation and management (own courses only)
  - ✅ Student enrollment in their courses
  - ✅ Grade management for their courses
  - ❌ Admin-only features (Students, Semesters pages)
- **Key Tests**:
  - `Educator_Should_NOT_Access_Admin_Features()` - Permission boundaries
  - `Educator_Should_Manage_Teaching_And_Family_Workflow()` - Dual role workflow

### 4. **MemberTests** (`Tests/RoleBasedTests/MemberTests.cs`)
- **Purpose**: Family management only testing
- **User Role**: Basic Member
- **Capabilities Tested**:
  - ✅ Family/children management
  - ✅ Course browsing (view available courses)
  - ✅ Course enrollment for their children
  - ✅ Viewing children's grades and progress
  - ✅ Basic account management
  - ❌ Admin features (Students, Semesters)
  - ❌ Course creation (educator feature)
- **Key Tests**:
  - `Member_Should_Have_Limited_Navigation_Options()` - Proper permission restrictions
  - `Member_Should_Complete_Family_Management_Workflow()` - Complete family workflow

## Role Hierarchy & Capabilities

```
Administrator
├── Full system access
├── User management
├── Semester management  
├── All educator capabilities
└── All member capabilities

Educator
├── Course creation/management (own courses)
├── Grade management (own courses)
├── Student enrollment (in own courses)
├── All member capabilities (own family)
└── Contact/view other educators

Member
├── Family/children management
├── Course browsing
├── Enrollment management (own children)
├── Grade viewing (own children)
└── Educator contact/viewing
```

## Test Credentials

Configured in `appsettings.json`:

```json
{
  "TestCredentials": {
    "AdminUser": {
      "Username": "admin1",
      "Password": "AdminPass123!"
    },
    "EducatorUser": {
      "Username": "educator1", 
      "Password": "EducatorPass123!"
    },
    "MemberUser": {
      "Username": "member1",
      "Password": "MemberPass123!"
    }
  }
}
```

> **Note**: You'll need to create the `educator1` and `member1` test users in your system with appropriate roles.

## Benefits of This Organization

### 1. **Real-World Alignment**
- Tests mirror actual user workflows
- Permission boundaries are explicitly tested
- Role-specific features get focused coverage

### 2. **Minimal Overlap**
- Each test suite focuses on role-specific capabilities
- Inheritance of capabilities is tested appropriately
- No redundant testing of shared features

### 3. **Maintainable**
- Clear separation of concerns
- Easy to add new role-specific tests
- Helper methods reduce code duplication

### 4. **Comprehensive Coverage**
- **AdminTests**: Ensures full system functionality
- **EducatorTests**: Validates dual-role permissions
- **MemberTests**: Confirms basic user experience and restrictions

## Usage Examples

### Running All Role-Based Tests
```bash
dotnet test tests/StudentRegistrar.E2E.Tests/Tests/RoleBasedTests --verbosity normal
```

### Running Specific Role Tests
```bash
# Admin-only tests
dotnet test tests/StudentRegistrar.E2E.Tests/Tests/RoleBasedTests/AdminTests.cs --verbosity normal

# Educator-only tests  
dotnet test tests/StudentRegistrar.E2E.Tests/Tests/RoleBasedTests/EducatorTests.cs --verbosity normal

# Member-only tests
dotnet test tests/StudentRegistrar.E2E.Tests/Tests/RoleBasedTests/MemberTests.cs --verbosity normal
```

## Future Extensions

As your application grows, you can easily:

1. **Add New Roles**: Create new test classes following the same pattern
2. **Add Workflow Tests**: Create specific workflow test classes that use multiple roles
3. **Add Integration Tests**: Test interactions between different role types
4. **Add Permission Edge Cases**: Test boundary conditions between roles

This organization provides a solid foundation that scales with your application's complexity while maintaining clear, comprehensive test coverage for each user type's capabilities and restrictions.

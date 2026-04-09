#!/bin/bash

# Complete E2E Testing Setup and Execution Script
# This script handles the complete E2E testing workflow

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Default values
HEADLESS_MODE=""
SETUP_USERS="no"
RUN_TESTS="yes"
TEST_SUITE="all"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Print colored output
print_status() {
    echo -e "${BLUE}🔧 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Usage information
show_usage() {
    echo "E2E Testing Setup and Execution Script"
    echo ""
    echo "Usage: $0 [options]"
    echo ""
    echo "Options:"
    echo "  -h, --headless          Run tests in headless mode (no browser window)"
    echo "  -s, --setup-users       Create test users in Keycloak before running tests"
    echo "  -t, --test-suite SUITE  Run specific test suite (all|admin|educator|member|login)"
    echo "  -n, --no-tests          Skip running tests (useful with --setup-users)"
    echo "  --help                  Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                           # Run all tests with browser visible"
    echo "  $0 --headless                # Run all tests in headless mode"
    echo "  $0 --setup-users             # Create test users and run all tests"
    echo "  $0 --test-suite admin        # Run only admin tests"
    echo "  $0 --setup-users --no-tests  # Only create test users, don't run tests"
    echo ""
    echo "Test Suites:"
    echo "  all      - All E2E tests (default)"
    echo "  login    - Basic login/logout tests"
    echo "  admin    - Administrator role tests"
    echo "  educator - Educator role tests"
    echo "  member   - Member role tests"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--headless)
            HEADLESS_MODE="headless"
            shift
            ;;
        -s|--setup-users)
            SETUP_USERS="yes"
            shift
            ;;
        -t|--test-suite)
            TEST_SUITE="$2"
            shift 2
            ;;
        -n|--no-tests)
            RUN_TESTS="no"
            shift
            ;;
        --help)
            show_usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

# Check if application is running
check_application() {
    print_status "Checking if Student Registrar application is running..."
    if curl -s "http://localhost:3001" > /dev/null 2>&1; then
        print_success "Application is running at http://localhost:3001"
        return 0
    else
        print_error "Application is not running at http://localhost:3001"
        echo "Please start the application with: aspire run"
        return 1
    fi
}

# Setup test users
setup_test_users() {
    print_status "Setting up test users in Keycloak..."
    
    if [[ -f "$SCRIPT_DIR/setup-test-users.sh" ]]; then
        bash "$SCRIPT_DIR/setup-test-users.sh"
        print_success "Test users setup completed"
    else
        print_error "setup-test-users.sh not found"
        return 1
    fi
}

# Run E2E tests
run_e2e_tests() {
    print_status "Running E2E tests..."
    
    cd "$PROJECT_ROOT"
    
    # Determine test filter based on suite
    local test_filter=""
    case $TEST_SUITE in
        login)
            test_filter="--filter FullyQualifiedName~LoginTests"
            ;;
        admin)
            test_filter="--filter FullyQualifiedName~AdminTests"
            ;;
        educator)
            test_filter="--filter FullyQualifiedName~EducatorTests"
            ;;
        member)
            test_filter="--filter FullyQualifiedName~MemberTests"
            ;;
        all)
            test_filter=""
            ;;
        *)
            print_error "Invalid test suite: $TEST_SUITE"
            print_error "Valid options: all, login, admin, educator, member"
            return 1
            ;;
    esac
    
    # Set environment variables
    if [[ "$HEADLESS_MODE" == "headless" ]]; then
        export SeleniumSettings__Headless=true
        print_status "Running tests in HEADLESS mode (no browser window)"
    else
        print_status "Running tests with BROWSER VISIBLE (you can watch the tests)"
    fi
    
    # Run the tests
    local test_command="dotnet test tests/StudentRegistrar.E2E.Tests/ --logger 'console;verbosity=normal' --collect:'XPlat Code Coverage'"
    
    if [[ -n "$test_filter" ]]; then
        test_command="$test_command $test_filter"
        print_status "Running test suite: $TEST_SUITE"
    else
        print_status "Running all E2E tests"
    fi
    
    echo ""
    eval $test_command
    local exit_code=$?
    
    if [[ $exit_code -eq 0 ]]; then
        print_success "All tests passed!"
    else
        print_error "Some tests failed (exit code: $exit_code)"
    fi
    
    return $exit_code
}

# Main execution
main() {
    echo "🧪 E2E Testing Setup and Execution"
    echo "==================================="
    echo ""
    
    # Check application
    if ! check_application; then
        exit 1
    fi
    
    # Setup users if requested
    if [[ "$SETUP_USERS" == "yes" ]]; then
        echo ""
        if ! setup_test_users; then
            exit 1
        fi
    fi
    
    # Run tests if requested
    if [[ "$RUN_TESTS" == "yes" ]]; then
        echo ""
        if ! run_e2e_tests; then
            exit 1
        fi
    fi
    
    echo ""
    print_success "E2E testing workflow completed!"
    
    if [[ "$RUN_TESTS" == "no" ]]; then
        echo ""
        print_status "To run tests later, use:"
        echo "  $0 --test-suite $TEST_SUITE"
        if [[ "$HEADLESS_MODE" == "headless" ]]; then
            echo "  $0 --headless --test-suite $TEST_SUITE"
        fi
    fi
}

main

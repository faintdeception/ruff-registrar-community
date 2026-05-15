#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)
PROJECT_ROOT=$(cd -- "${SCRIPT_DIR}/../.." >/dev/null 2>&1 && pwd)

RUN_TESTS=true
KEEP_RUNNING=false
TEST_FILTER=""

log_step() {
    printf '[%s] %s\n' "$(date '+%H:%M:%S')" "$*"
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --preflight-only)
            RUN_TESTS=false
            shift
            ;;
        --keep-running)
            KEEP_RUNNING=true
            shift
            ;;
        --filter)
            TEST_FILTER="$2"
            shift 2
            ;;
        --help|-h)
            cat <<EOF
Usage: $0 [--preflight-only] [--keep-running] [--filter TEST_FILTER]

Runs the Docker Compose based E2E path used by CI so local validation and
GitHub Actions exercise the same cold-start stack.
EOF
            exit 0
            ;;
        *)
            echo "Unknown option: $1" >&2
            exit 2
            ;;
    esac
done

export KEYCLOAK_ADMIN_PASSWORD="${KEYCLOAK_ADMIN_PASSWORD:-admin123!}"
export POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres123!}"
export KEYCLOAK_CLIENT_SECRET="${KEYCLOAK_CLIENT_SECRET:-student-registrar-local-dev-secret}"
export KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
export KEYCLOAK_REALM="${KEYCLOAK_REALM:-student-registrar}"
export KEYCLOAK_ADMIN_USERNAME="${KEYCLOAK_ADMIN_USERNAME:-admin}"
export API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
export SeleniumSettings__BaseUrl="${SeleniumSettings__BaseUrl:-http://localhost:3000}"
export SeleniumSettings__Headless="${SeleniumSettings__Headless:-true}"
export DB_PASSWORD="${DB_PASSWORD:-$POSTGRES_PASSWORD}"
export SEED_DATABASE_RESET="${SEED_DATABASE_RESET:-true}"

cleanup() {
    local exit_code=$?
    if [ "$exit_code" -ne 0 ]; then
        echo ""
        echo "❌ CI-equivalent E2E run failed. Recent compose logs:"
        docker compose logs --tail 200 || true
    fi

    if [ "$KEEP_RUNNING" != "true" ]; then
        docker compose down -v || true
    else
        echo "Keeping compose services running for inspection."
    fi
}

trap cleanup EXIT

cd "$PROJECT_ROOT"

log_step "Running CI-equivalent core E2E validation"
log_step "Project root: $PROJECT_ROOT"
log_step "Frontend URL: $SeleniumSettings__BaseUrl"

docker compose down -v

for attempt in 1 2 3; do
    log_step "Building images and starting core services (attempt ${attempt}/3)..."
    if docker compose --progress plain up --build -d postgres keycloak api frontend; then
        break
    fi

    if [ "$attempt" -eq 3 ]; then
        echo "Failed to start core services after 3 attempts." >&2
        exit 1
    fi

    docker compose down -v || true
    sleep 10
done

log_step "Waiting for Keycloak..."
for i in {1..60}; do
    if curl -fsS "${KEYCLOAK_URL}/realms/master" >/dev/null 2>&1; then
        break
    fi
    if [ "$i" -eq 60 ]; then
        echo "Keycloak did not become ready." >&2
        exit 1
    fi
    sleep 3
done

log_step "Waiting for frontend..."
for i in {1..60}; do
    if curl -fsS "${SeleniumSettings__BaseUrl}" >/dev/null 2>&1; then
        break
    fi
    if [ "$i" -eq 60 ]; then
        echo "Frontend did not become ready." >&2
        exit 1
    fi
    sleep 3
done

log_step "Bootstrapping Keycloak realm and test data..."
./setup-keycloak.sh
./scripts/keycloak/add-spa-client.sh
./scripts/testing/setup-test-users.sh
./scripts/testing/seed-database.sh
./scripts/testing/preflight-ci-e2e.sh

if [ "$RUN_TESTS" = "true" ]; then
    test_args=(tests/StudentRegistrar.E2E.Tests/ --logger "console;verbosity=normal")
    if [ -n "$TEST_FILTER" ]; then
        test_args+=(--filter "$TEST_FILTER")
    fi

    dotnet test "${test_args[@]}"
fi

log_step "CI-equivalent core E2E validation completed."

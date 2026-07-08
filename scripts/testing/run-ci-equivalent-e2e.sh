#!/usr/bin/env bash
#
# run-ci-equivalent-e2e.sh
#
# Spins up a docker-compose-based core stack (postgres + keycloak + api) plus a
# `next dev` frontend, bootstraps Keycloak, seeds data, and runs the core Selenium
# E2E suite against http://localhost:3000. Mirrors what the GitHub `core-e2e-tests`
# job runs, and can also be run locally to reproduce CI.
#
# Required environment:
#   KEYCLOAK_ADMIN_PASSWORD   Master admin password (e.g. admin123!)
#   POSTGRES_PASSWORD         Postgres password (e.g. postgres123!)
#
# Optional environment:
#   CORE_E2E_TEST_FILTER      dotnet test --filter expression (default: run all tests)
#   CORE_E2E_BASE_URL         Frontend base URL for Selenium (default: http://localhost:3000)
#   FRONTEND_PORT             Port to serve `next dev` on (default: 3000)
#   KEYCLOAK_CLIENT_SECRET    Confidential client secret (default: student-registrar-local-dev-secret)
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CORE_DIR="$(cd "${SCRIPT_DIR}/../.." && pwd)"
cd "${CORE_DIR}"

KEYCLOAK_ADMIN_PASSWORD="${KEYCLOAK_ADMIN_PASSWORD:?KEYCLOAK_ADMIN_PASSWORD is required}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}"
export KEYCLOAK_ADMIN_PASSWORD POSTGRES_PASSWORD

CORE_E2E_BASE_URL="${CORE_E2E_BASE_URL:-http://localhost:3000}"
FRONTEND_PORT="${FRONTEND_PORT:-3000}"
KEYCLOAK_CLIENT_SECRET="${KEYCLOAK_CLIENT_SECRET:-student-registrar-local-dev-secret}"
export KEYCLOAK_CLIENT_SECRET
CORE_E2E_TEST_FILTER="${CORE_E2E_TEST_FILTER:-}"

KEYCLOAK_URL="http://localhost:8080"
API_URL="http://localhost:5000"
FRONTEND_PID=""

log() { printf '\n=== %s ===\n' "$*"; }

cleanup() {
  local exit_code=$?
  if [[ -n "${FRONTEND_PID}" ]] && kill -0 "${FRONTEND_PID}" 2>/dev/null; then
    log "Stopping frontend (pid ${FRONTEND_PID})"
    kill "${FRONTEND_PID}" 2>/dev/null || true
  fi
  log "Shutting down docker-compose stack"
  docker compose down -v || true
  exit "${exit_code}"
}
trap cleanup EXIT

wait_for_url() {
  local url="$1" name="$2" attempts="${3:-60}" delay="${4:-5}"
  log "Waiting for ${name} (${url})"
  for ((i = 1; i <= attempts; i++)); do
    if curl -fsS --max-time 10 "${url}" >/dev/null 2>&1; then
      echo "${name} is ready"
      return 0
    fi
    sleep "${delay}"
  done
  echo "ERROR: ${name} did not become ready at ${url}" >&2
  return 1
}

# ---------------------------------------------------------------------------
# 1. Bring up postgres + keycloak + api (build the api image).
# ---------------------------------------------------------------------------
log "Starting docker-compose stack (postgres, keycloak, api)"
docker compose up -d --build postgres keycloak api

wait_for_url "${KEYCLOAK_URL}/realms/master/.well-known/openid-configuration" "Keycloak" 60 5
wait_for_url "${API_URL}/swagger/index.html" "Core API" 60 5

# ---------------------------------------------------------------------------
# 2. Bootstrap the student-registrar realm (template import: both clients with
#    the fixed confidential client secret + realm roles + service-account grants).
# ---------------------------------------------------------------------------
log "Bootstrapping Keycloak realm"
INITIAL_ADMIN_USERNAME="${INITIAL_ADMIN_USERNAME:-ci-bootstrap-admin}" \
INITIAL_ADMIN_EMAIL="${INITIAL_ADMIN_EMAIL:-ci-bootstrap-admin@example.com}" \
INITIAL_ADMIN_TEMP_PASS="${INITIAL_ADMIN_TEMP_PASS:-ChangeThis123!}" \
KEYCLOAK_URL="${KEYCLOAK_URL}" \
  bash scripts/keycloak/bootstrap-keycloak.sh

# ---------------------------------------------------------------------------
# 3. Create deterministic E2E test users (admin1 / educator1 / member1 / parenteducator1).
# ---------------------------------------------------------------------------
log "Setting up E2E test users"
KEYCLOAK_URL="${KEYCLOAK_URL}" bash scripts/testing/setup-test-users.sh

# ---------------------------------------------------------------------------
# 4. Seed baseline data (semesters, rooms, courses) into the compose postgres.
# ---------------------------------------------------------------------------
log "Seeding database"
DB_PASSWORD="${POSTGRES_PASSWORD}" bash scripts/testing/seed-database.sh

# ---------------------------------------------------------------------------
# 5. Serve the tenant frontend with `next dev` (matches the dev-mode behaviour
#    validated locally) on FRONTEND_PORT.
# ---------------------------------------------------------------------------
log "Installing frontend dependencies"
npm ci --prefix frontend --no-audit --no-fund

log "Starting frontend (next dev) on port ${FRONTEND_PORT}"
NEXT_PUBLIC_API_URL="${API_URL}" \
API_BASE_URL="${API_URL}" \
NEXT_PUBLIC_KEYCLOAK_URL="${KEYCLOAK_URL}" \
NEXT_PUBLIC_KEYCLOAK_REALM="student-registrar" \
NEXT_PUBLIC_KEYCLOAK_CLIENT_ID="student-registrar-spa" \
  npm run dev --prefix frontend -- --hostname 0.0.0.0 --port "${FRONTEND_PORT}" > frontend-e2e.log 2>&1 &
FRONTEND_PID=$!

wait_for_url "${CORE_E2E_BASE_URL}" "Frontend" 40 3

# ---------------------------------------------------------------------------
# 6. Run the Selenium E2E suite headless against the frontend.
# ---------------------------------------------------------------------------
log "Running core E2E tests"
export SeleniumSettings__Headless="true"
export SeleniumSettings__BaseUrl="${CORE_E2E_BASE_URL}"

TEST_ARGS=(test tests/StudentRegistrar.E2E.Tests/StudentRegistrar.E2E.Tests.csproj --logger "console;verbosity=normal")
if [[ -n "${CORE_E2E_TEST_FILTER}" ]]; then
  TEST_ARGS+=(--filter "${CORE_E2E_TEST_FILTER}")
fi

dotnet "${TEST_ARGS[@]}"

#!/usr/bin/env bash

set -euo pipefail

KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
KEYCLOAK_REALM="${KEYCLOAK_REALM:-student-registrar}"
KEYCLOAK_ADMIN_USERNAME="${KEYCLOAK_ADMIN_USERNAME:-${KEYCLOAK_ADMIN_USER:-admin}}"
KEYCLOAK_CLIENT_ID="${KEYCLOAK_CLIENT_ID:-student-registrar}"
KEYCLOAK_CLIENT_SECRET="${KEYCLOAK_CLIENT_SECRET:-student-registrar-local-dev-secret}"
API_BASE_URL="${API_BASE_URL:-http://localhost:5000}"
DB_CONTAINER="${DB_CONTAINER:-$(docker compose ps -q postgres)}"
DB_NAME="${DB_NAME:-studentregistrar}"
DB_USER="${DB_USER:-postgres}"
DB_PASSWORD="${DB_PASSWORD:-${POSTGRES_PASSWORD:-postgres123!}}"
ADMIN_USERNAME="${E2E_ADMIN_USERNAME:-admin1}"
ADMIN_PASSWORD="${E2E_ADMIN_PASSWORD:-AdminPass123!}"

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Required command not found: $1" >&2
        exit 2
    fi
}

require_command curl
require_command jq
require_command docker

if [ -z "${KEYCLOAK_ADMIN_PASSWORD:-}" ]; then
    echo "KEYCLOAK_ADMIN_PASSWORD is required for E2E preflight." >&2
    exit 2
fi

echo "🔎 Running CI-equivalent E2E preflight..."

ADMIN_TOKEN=$(curl -fsS -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    --data-urlencode "username=${KEYCLOAK_ADMIN_USERNAME}" \
    --data-urlencode "password=${KEYCLOAK_ADMIN_PASSWORD}" \
    -d "grant_type=password" \
    -d "client_id=admin-cli" | jq -r '.access_token // empty')

if [ -z "$ADMIN_TOKEN" ]; then
    echo "Failed to obtain Keycloak admin token." >&2
    exit 1
fi

CLIENT=$(curl -fsS -G "${KEYCLOAK_URL}/admin/realms/${KEYCLOAK_REALM}/clients" \
    -H "Authorization: Bearer $ADMIN_TOKEN" \
    --data-urlencode "clientId=${KEYCLOAK_CLIENT_ID}")

CLIENT_UUID=$(echo "$CLIENT" | jq -r '.[0].id // empty')
if [ -z "$CLIENT_UUID" ]; then
    echo "Keycloak client '${KEYCLOAK_CLIENT_ID}' was not found in realm '${KEYCLOAK_REALM}'." >&2
    exit 1
fi

DIRECT_ACCESS=$(echo "$CLIENT" | jq -r '.[0].directAccessGrantsEnabled // false')
if [ "$DIRECT_ACCESS" != "true" ]; then
    echo "Keycloak client '${KEYCLOAK_CLIENT_ID}' must have directAccessGrantsEnabled=true for app login." >&2
    exit 1
fi

TOKEN_RESPONSE=$(curl -sS -w '\n%{http_code}' -X POST "${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    --data-urlencode "username=${ADMIN_USERNAME}" \
    --data-urlencode "password=${ADMIN_PASSWORD}" \
    --data-urlencode "client_id=${KEYCLOAK_CLIENT_ID}" \
    --data-urlencode "client_secret=${KEYCLOAK_CLIENT_SECRET}" \
    -d "grant_type=password")

TOKEN_STATUS=$(echo "$TOKEN_RESPONSE" | tail -n 1)
TOKEN_BODY=$(echo "$TOKEN_RESPONSE" | sed '$d')
if [ "$TOKEN_STATUS" != "200" ]; then
    echo "Keycloak password grant failed for '${ADMIN_USERNAME}' with HTTP ${TOKEN_STATUS}." >&2
    echo "$TOKEN_BODY" >&2
    exit 1
fi

ADMIN_KEYCLOAK_ID=$(curl -fsS -G "${KEYCLOAK_URL}/admin/realms/${KEYCLOAK_REALM}/users" \
    -H "Authorization: Bearer $ADMIN_TOKEN" \
    --data-urlencode "username=${ADMIN_USERNAME}" \
    --data-urlencode "exact=true" | jq -r '.[0].id // empty')

if [ -z "$ADMIN_KEYCLOAK_ID" ]; then
    echo "Keycloak user '${ADMIN_USERNAME}' was not found." >&2
    exit 1
fi

DB_MATCH_COUNT=$(docker exec -e PGPASSWORD="$DB_PASSWORD" "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -t -A -c \
    "SELECT COUNT(*) FROM \"Users\" WHERE \"KeycloakId\" = '${ADMIN_KEYCLOAK_ID}';")

if [ "$DB_MATCH_COUNT" != "1" ]; then
    echo "App database does not contain exactly one user mapped to Keycloak id '${ADMIN_KEYCLOAK_ID}' for '${ADMIN_USERNAME}'." >&2
    exit 1
fi

LOGIN_RESPONSE=$(curl -sS -w '\n%{http_code}' -X POST "${API_BASE_URL}/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"${ADMIN_USERNAME}\",\"password\":\"${ADMIN_PASSWORD}\"}")

LOGIN_STATUS=$(echo "$LOGIN_RESPONSE" | tail -n 1)
LOGIN_BODY=$(echo "$LOGIN_RESPONSE" | sed '$d')
if [ "$LOGIN_STATUS" != "200" ] || [ "$(echo "$LOGIN_BODY" | jq -r '.success // false')" != "true" ]; then
    echo "API /auth/login preflight failed with HTTP ${LOGIN_STATUS}." >&2
    echo "$LOGIN_BODY" >&2
    exit 1
fi

echo "✅ CI-equivalent E2E preflight passed."

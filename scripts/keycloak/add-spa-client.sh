#!/usr/bin/env bash
# add-spa-client.sh
# Adds or updates the public SPA client used by the frontend (student-registrar-spa).
# Requires jq and curl. Prompts for Keycloak admin password if not provided.

set -euo pipefail

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)

REALM="student-registrar"
KEYCLOAK_URL="http://localhost:8080"
ADMIN_USERNAME="admin"
CLIENT_ID="student-registrar-spa"

usage() {
  cat <<EOF
Usage: $0 [options]
  --realm NAME          Realm name (default: ${REALM})
  --keycloak-url URL    Base Keycloak URL (default: ${KEYCLOAK_URL})
  --admin-username NAME Master realm admin username (default: ${ADMIN_USERNAME})
  --client-id NAME      SPA client id (default: ${CLIENT_ID})
  --help                Show this help

Environment:
  KEYCLOAK_ADMIN_PASSWORD   Master admin password (skips prompt)
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --realm) REALM="$2"; shift 2;;
    --keycloak-url) KEYCLOAK_URL="$2"; shift 2;;
    --admin-username) ADMIN_USERNAME="$2"; shift 2;;
    --client-id) CLIENT_ID="$2"; shift 2;;
    --help|-h) usage; exit 0;;
    *) echo "Unknown option: $1" >&2; usage; exit 1;;
  esac
done

if ! command -v jq >/dev/null; then
  echo "jq is required" >&2; exit 2; fi
if ! command -v curl >/dev/null; then
  echo "curl is required" >&2; exit 2; fi

if [[ -z "${KEYCLOAK_ADMIN_PASSWORD:-}" ]]; then
  read -s -p "Enter Keycloak master admin password for user '${ADMIN_USERNAME}': " KEYCLOAK_ADMIN_PASSWORD; echo
fi

TOKEN=$(curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode "username=${ADMIN_USERNAME}" \
  --data-urlencode "password=${KEYCLOAK_ADMIN_PASSWORD}" \
  -d 'client_id=admin-cli' \
  -d 'grant_type=password' | jq -r '.access_token')

if [[ -z "$TOKEN" || "$TOKEN" == null ]]; then
  echo "Failed to obtain admin token" >&2; exit 3; fi

# Check for existing client
CLIENT_UUID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=${CLIENT_ID}" | jq -r '.[0].id')

CLIENT_PAYLOAD=$(jq -n --arg clientId "$CLIENT_ID" '{
  clientId: $clientId,
  enabled: true,
  publicClient: true,
  protocol: "openid-connect",
  standardFlowEnabled: true,
  directAccessGrantsEnabled: true,
  serviceAccountsEnabled: false,
  redirectUris: ["http://localhost:3000/*", "http://localhost:3001/*"],
  webOrigins: ["http://localhost:3000", "http://localhost:3001"],
  attributes: {}
}')

if [[ -z "$CLIENT_UUID" || "$CLIENT_UUID" == null ]]; then
  echo "Creating SPA client '${CLIENT_ID}'..."
  RESPONSE=$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
    -d "$CLIENT_PAYLOAD" "${KEYCLOAK_URL}/admin/realms/${REALM}/clients")
  if [[ "$RESPONSE" != 201 && "$RESPONSE" != 409 ]]; then
    echo "Failed to create client (HTTP $RESPONSE)" >&2; exit 4; fi
  echo "✅ Client created"
else
  echo "Updating SPA client '${CLIENT_ID}'..."
  RESPONSE=$(curl -s -o /dev/null -w '%{http_code}' -X PUT -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
    -d "$CLIENT_PAYLOAD" "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${CLIENT_UUID}")
  if [[ "$RESPONSE" != 204 ]]; then
    echo "Failed to update client (HTTP $RESPONSE)" >&2; exit 5; fi
  echo "✅ Client updated"
fi

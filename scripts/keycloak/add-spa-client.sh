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
DEFAULT_REDIRECT_URIS="http://localhost:3000/*,http://localhost:3001/*"
DEFAULT_WEB_ORIGINS="http://localhost:3000,http://localhost:3001"

usage() {
  cat <<EOF
Usage: $0 [options]
  --realm NAME          Realm name (default: ${REALM})
  --keycloak-url URL    Base Keycloak URL (default: ${KEYCLOAK_URL})
  --admin-username NAME Master realm admin username (default: ${ADMIN_USERNAME})
  --client-id NAME      SPA client id (default: ${CLIENT_ID})
  --redirect-uris CSV   Comma-separated redirect URIs
  --web-origins CSV     Comma-separated web origins
  --help                Show this help

Environment:
  KEYCLOAK_ADMIN_PASSWORD   Master admin password (skips prompt)
  REDIRECT_URIS             Comma-separated redirect URIs (overrides default)
  WEB_ORIGINS               Comma-separated web origins (overrides default)
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --realm) REALM="$2"; shift 2;;
    --keycloak-url) KEYCLOAK_URL="$2"; shift 2;;
    --admin-username) ADMIN_USERNAME="$2"; shift 2;;
    --client-id) CLIENT_ID="$2"; shift 2;;
    --redirect-uris) REDIRECT_URIS="$2"; shift 2;;
    --web-origins) WEB_ORIGINS="$2"; shift 2;;
    --help|-h) usage; exit 0;;
    *) echo "Unknown option: $1" >&2; usage; exit 1;;
  esac
done

REDIRECT_URIS="${REDIRECT_URIS:-$DEFAULT_REDIRECT_URIS}"
WEB_ORIGINS="${WEB_ORIGINS:-$DEFAULT_WEB_ORIGINS}"

json_array_from_csv() {
  local csv="$1"
  local out="["
  local first=1
  IFS=',' read -r -a items <<< "$csv"
  for item in "${items[@]}"; do
    item="$(echo "$item" | xargs)"
    [[ -z "$item" ]] && continue
    if [[ $first -eq 0 ]]; then
      out+=" , "
    else
      first=0
    fi
    out+="\"$item\""
  done
  out+="]"
  echo "$out"
}

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

REDIRECT_URIS_JSON=$(json_array_from_csv "$REDIRECT_URIS")
WEB_ORIGINS_JSON=$(json_array_from_csv "$WEB_ORIGINS")

CLIENT_PAYLOAD=$(jq -n --arg clientId "$CLIENT_ID" \
  --argjson redirectUris "$REDIRECT_URIS_JSON" \
  --argjson webOrigins "$WEB_ORIGINS_JSON" '{
  clientId: $clientId,
  enabled: true,
  publicClient: true,
  protocol: "openid-connect",
  standardFlowEnabled: true,
  directAccessGrantsEnabled: true,
  serviceAccountsEnabled: false,
  redirectUris: $redirectUris,
  webOrigins: $webOrigins,
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

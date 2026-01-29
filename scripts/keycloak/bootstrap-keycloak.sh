#!/usr/bin/env bash
#!/usr/bin/env bash
# bootstrap-keycloak.sh
# One-time bootstrap of Keycloak realm & first application Administrator (no test users).
# Fails fast if realm already exists.
# Supports interactive mode (default) and non-interactive mode via flags / env vars for CI.
#
# Non-interactive inputs (optional):
#   --initial-admin-username <name>
#   --initial-admin-email <email>
#   --initial-admin-temp-pass <password> (or INITIAL_ADMIN_TEMP_PASS env var)
#   --admin-password-file <path> (file containing Keycloak master admin password)
#   (or KEYCLOAK_ADMIN_PASSWORD env var)
# If all initial admin fields + master admin password are provided, prompts are skipped.

set -euo pipefail

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)
TEMPLATE_JSON="${SCRIPT_DIR}/realm-student-registrar.template.json"

REALM="student-registrar"
KEYCLOAK_URL="http://localhost:8080"
# Aspire dev default master admin username is typically 'admin'.
ADMIN_USERNAME="admin" # Keycloak master realm admin username (can override)
CLIENT_ID="student-registrar"

# Initial application admin (first real user inside the realm)
INIT_APP_ADMIN_USERNAME=""
INIT_APP_ADMIN_EMAIL=""
INIT_APP_ADMIN_TEMP_PASS=""
ADMIN_PASSWORD_FILE=""

usage() {
  cat <<EOF
Usage: $0 [options]
  --realm NAME                   Realm name (default: ${REALM})
  --keycloak-url URL             Base Keycloak URL (default: ${KEYCLOAK_URL})
  --admin-username NAME          Master realm admin username (default: ${ADMIN_USERNAME})
  --initial-admin-username NAME  First application admin username (non-interactive)
  --initial-admin-email EMAIL    First application admin email (non-interactive)
  --initial-admin-temp-pass PWD  First app admin temp password (non-interactive; WARNING: appears in shell history). Alternatively set env INITIAL_ADMIN_TEMP_PASS.
  --admin-password-file PATH     File whose contents are the Keycloak master admin password (safer than passing inline)
  --help                         Show this help

Environment overrides:
  KEYCLOAK_ADMIN_PASSWORD   Master admin password (skips prompt)
  INITIAL_ADMIN_TEMP_PASS   Temp password for first app admin (skips prompt if other identity fields supplied)

Behavior:
  - If realm already exists, exits with code 10.
  - If any of the initial admin fields are missing (and not supplied via flags/env), prompts interactively.
  - Avoid supplying secrets directly on command line in production; prefer files or environment variables.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --realm) REALM="$2"; shift 2;;
    --keycloak-url) KEYCLOAK_URL="$2"; shift 2;;
    --admin-username) ADMIN_USERNAME="$2"; shift 2;;
    --initial-admin-username) INIT_APP_ADMIN_USERNAME="$2"; shift 2;;
    --initial-admin-email) INIT_APP_ADMIN_EMAIL="$2"; shift 2;;
    --initial-admin-temp-pass) INIT_APP_ADMIN_TEMP_PASS="$2"; shift 2;;
    --admin-password-file) ADMIN_PASSWORD_FILE="$2"; shift 2;;
    --help|-h) usage; exit 0;;
    *) echo "Unknown option: $1" >&2; usage; exit 1;;
  esac
done

if ! command -v jq >/dev/null; then
  echo "jq is required" >&2; exit 2; fi
if ! command -v curl >/dev/null; then
  echo "curl is required" >&2; exit 2; fi

if [[ ! -f "$TEMPLATE_JSON" ]]; then
  echo "Template not found: $TEMPLATE_JSON" >&2; exit 3; fi

echo "🔐 Keycloak bootstrap (realm: $REALM)"

# Resolve master admin password
if [[ -n "$ADMIN_PASSWORD_FILE" ]]; then
  if [[ ! -f "$ADMIN_PASSWORD_FILE" ]]; then
    echo "Admin password file not found: $ADMIN_PASSWORD_FILE" >&2; exit 2; fi
  KEYCLOAK_ADMIN_PASSWORD=$(<"$ADMIN_PASSWORD_FILE")
fi

if [[ -z "${KEYCLOAK_ADMIN_PASSWORD:-}" ]]; then
  read -s -p "Enter Keycloak master admin password for user '${ADMIN_USERNAME}': " KEYCLOAK_ADMIN_PASSWORD; echo
fi

# Obtain token
TOKEN=$(curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode "username=${ADMIN_USERNAME}" \
  --data-urlencode "password=${KEYCLOAK_ADMIN_PASSWORD}" \
  -d 'client_id=admin-cli' \
  -d 'grant_type=password' | jq -r '.access_token')

if [[ -z "$TOKEN" || "$TOKEN" == null ]]; then
  echo "Failed to obtain admin token" >&2; exit 4; fi

echo "Checking if realm already exists..."
REALM_STATUS=$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}")
if [[ "$REALM_STATUS" == "200" ]]; then
  echo "❌ Realm '${REALM}' already exists. Aborting (idempotency policy)." >&2
  exit 10
fi

echo "Creating realm from template..."
# Post template (strip whitespace just in case)
RESPONSE=$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d @"${TEMPLATE_JSON}" "${KEYCLOAK_URL}/admin/realms")
if [[ "$RESPONSE" != 201 && "$RESPONSE" != 409 ]]; then
  echo "Realm creation failed (HTTP $RESPONSE)" >&2; exit 11; fi

# Gather initial application admin (interactive if needed)
if [[ -z "$INIT_APP_ADMIN_USERNAME" ]]; then
  read -p "Initial application admin username: " INIT_APP_ADMIN_USERNAME
fi
if [[ -z "$INIT_APP_ADMIN_EMAIL" ]]; then
  read -p "Initial application admin email: " INIT_APP_ADMIN_EMAIL
fi
if [[ -z "$INIT_APP_ADMIN_TEMP_PASS" ]]; then
  if [[ -n "${INITIAL_ADMIN_TEMP_PASS:-}" ]]; then
    INIT_APP_ADMIN_TEMP_PASS="$INITIAL_ADMIN_TEMP_PASS"
  else
    read -s -p "Initial application admin temporary password: " INIT_APP_ADMIN_TEMP_PASS; echo
  fi
fi

APP_ADMIN_USERNAME="$INIT_APP_ADMIN_USERNAME"
APP_ADMIN_EMAIL="$INIT_APP_ADMIN_EMAIL"
APP_ADMIN_PASSWORD="$INIT_APP_ADMIN_TEMP_PASS"

USER_PAYLOAD=$(jq -n --arg u "$APP_ADMIN_USERNAME" --arg e "$APP_ADMIN_EMAIL" '{username:$u, email:$e, enabled:true, emailVerified:false, firstName:"Admin", lastName:"User"}')
CREATE_USER_STATUS=$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "$USER_PAYLOAD" "${KEYCLOAK_URL}/admin/realms/${REALM}/users")
if [[ "$CREATE_USER_STATUS" != 201 ]]; then
  echo "Failed to create initial admin user (HTTP $CREATE_USER_STATUS)" >&2; exit 12; fi

USER_ID=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/users?username=${APP_ADMIN_USERNAME}" | jq -r '.[0].id')
if [[ -z "$USER_ID" || "$USER_ID" == null ]]; then
  echo "Could not retrieve new user id" >&2; exit 13; fi

RESET_STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X PUT -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "{\"type\":\"password\",\"temporary\":true,\"value\":\"${APP_ADMIN_PASSWORD//"/\\"}\"}" \
  "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${USER_ID}/reset-password")
if [[ "$RESET_STATUS" != 204 ]]; then
  echo "Failed to set password (HTTP $RESET_STATUS)" >&2; exit 14; fi

# Assign Administrator role
ADMIN_ROLE=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/roles/Administrator")
ROLE_ID=$(echo "$ADMIN_ROLE" | jq -r '.id')
ASSIGN_STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X POST -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  -d "[{\"id\":\"$ROLE_ID\",\"name\":\"Administrator\"}]" \
  "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${USER_ID}/role-mappings/realm")
if [[ "$ASSIGN_STATUS" != 204 ]]; then
  echo "Failed to assign Administrator role (HTTP $ASSIGN_STATUS)" >&2; exit 15; fi

echo "Fetching client secret..."
CLIENT_UUID=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=${CLIENT_ID}" | jq -r '.[0].id')
CLIENT_SECRET=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${CLIENT_UUID}/client-secret" | jq -r '.value')

echo "Configuring service account permissions for client '${CLIENT_ID}'..."
# Grant the service account permissions to manage users in the realm.
# This is required for the API to call Keycloak Admin endpoints like:
#   POST /admin/realms/<realm>/users
SERVICE_ACCOUNT_USER_ID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${CLIENT_UUID}/service-account-user" | jq -r '.id')

if [[ -z "$SERVICE_ACCOUNT_USER_ID" || "$SERVICE_ACCOUNT_USER_ID" == null ]]; then
  echo "⚠️  Could not resolve service account user for client '${CLIENT_ID}'. Skipping role grants." >&2
else
  REALM_MGMT_CLIENT_ID=$(curl -s -H "Authorization: Bearer $TOKEN" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=realm-management" | jq -r '.[0].id')

  if [[ -z "$REALM_MGMT_CLIENT_ID" || "$REALM_MGMT_CLIENT_ID" == null ]]; then
    echo "⚠️  Could not find 'realm-management' client in realm '${REALM}'. Skipping role grants." >&2
  else
    # These client roles cover the admin operations this app performs.
    # - manage-users: create/update users
    # - view-users: query users
    # - query-users: search users
    # - view-realm: read roles for role assignment
    ROLE_NAMES=(manage-users view-users query-users view-realm)
    for role_name in "${ROLE_NAMES[@]}"; do
      ROLE_JSON=$(curl -s -H "Authorization: Bearer $TOKEN" \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${REALM_MGMT_CLIENT_ID}/roles/${role_name}")
      ROLE_ID=$(echo "$ROLE_JSON" | jq -r '.id')
      if [[ -z "$ROLE_ID" || "$ROLE_ID" == null ]]; then
        echo "⚠️  Could not find realm-management role '${role_name}'." >&2
        continue
      fi
      # POST is idempotent; re-granting an existing role is fine.
      curl -s -o /dev/null -X POST \
        -H "Authorization: Bearer $TOKEN" \
        -H 'Content-Type: application/json' \
        -d "[{\"id\":\"${ROLE_ID}\",\"name\":\"${role_name}\"}]" \
        "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${SERVICE_ACCOUNT_USER_ID}/role-mappings/clients/${REALM_MGMT_CLIENT_ID}" || true
    done
    echo "✅ Service account permissions configured"
  fi
fi

cat <<SUMMARY
✅ Bootstrap complete
Realm: ${REALM}
Initial admin (app): ${APP_ADMIN_USERNAME} (temp password must be changed at first login)
Client ID: ${CLIENT_ID}
Client Secret: ${CLIENT_SECRET}
Add to appsettings / secrets accordingly.
SUMMARY

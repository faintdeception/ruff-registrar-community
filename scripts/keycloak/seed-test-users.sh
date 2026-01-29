#!/usr/bin/env bash
# seed-test-users.sh
# Adds deterministic test users (NOT for production)

set -euo pipefail
REALM="student-registrar"
KEYCLOAK_URL="http://localhost:8080"
# Aspire dev default master admin username is typically 'admin'.
ADMIN_USERNAME="admin"

usage(){ cat <<EOF
Usage: $0 [options]
  --realm NAME              Realm name (default: $REALM)
  --keycloak-url URL        Keycloak base URL (default: $KEYCLOAK_URL)
  --admin-username NAME     Master realm admin username (default: $ADMIN_USERNAME)
  --help                    Show help
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --realm) REALM="$2"; shift 2;;
    --keycloak-url) KEYCLOAK_URL="$2"; shift 2;;
    --admin-username) ADMIN_USERNAME="$2"; shift 2;;
    --help|-h) usage; exit 0;;
    *) echo "Unknown option $1"; usage; exit 1;;
  esac
done

if ! command -v jq >/dev/null; then echo "jq required"; exit 2; fi

if [[ -n "${KEYCLOAK_ADMIN_PASSWORD:-}" ]]; then
  ADMIN_PASSWORD="$KEYCLOAK_ADMIN_PASSWORD"
  echo "🔑 Using KEYCLOAK_ADMIN_PASSWORD from environment"
else
  read -s -p "Enter Keycloak master admin password: " ADMIN_PASSWORD; echo
fi
TOKEN=$(curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode "username=${ADMIN_USERNAME}" \
  --data-urlencode "password=${ADMIN_PASSWORD}" \
  -d 'client_id=admin-cli' -d 'grant_type=password' | jq -r '.access_token')

if [[ -z "$TOKEN" || "$TOKEN" == null ]]; then echo "Failed to get token" >&2; exit 3; fi

create_user(){
  local username="$1" role="$2" pass="$3" email="$4"
  echo "Creating user $username ($role)"
  EXISTS=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/users?username=${username}" | jq 'length')
  if [[ "$EXISTS" != 0 ]]; then echo "  Skipping (exists)"; return 0; fi
  PAYLOAD=$(jq -n --arg u "$username" --arg e "$email" '{username:$u, email:$e, enabled:true, emailVerified:true, firstName:$u, lastName:"Test"}')
  STATUS=$(curl -s -o /dev/null -w '%{http_code}' -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d "$PAYLOAD" "${KEYCLOAK_URL}/admin/realms/${REALM}/users")
  if [[ "$STATUS" != 201 ]]; then echo "  Create failed HTTP $STATUS"; return 1; fi
  UID=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/users?username=${username}" | jq -r '.[0].id')
  curl -s -X PUT -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
    -d "{\"type\":\"password\",\"temporary\":false,\"value\":\"${pass}\"}" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${UID}/reset-password" >/dev/null
  ROLE_JSON=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/roles/${role}")
  RID=$(echo "$ROLE_JSON" | jq -r '.id')
  curl -s -X POST -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
    -d "[{\"id\":\"${RID}\",\"name\":\"${role}\"}]" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${UID}/role-mappings/realm" >/dev/null
}

create_user scoopadmin Administrator 'Admin!12345' scoopadmin@example.com
create_user scoopmember Member 'Member!12345' scoopmember@example.com
create_user scoopeducator Educator 'Educator!12345' scoopeducator@example.com

echo "✅ Test users seeded"

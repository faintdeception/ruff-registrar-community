#!/usr/bin/env bash
# Prepares a Keycloak realm for the public "click to try" demo login. Idempotent; safe to run daily.
#
# - Enables direct access grants on the SPA client (this realm only).
# - Disables brute-force protection (the demo password is public; lockouts would be a denial of service).
# - Creates demo-admin / demo-member if missing (existing users and ids are kept) and resets their password.
# - Prints each user's Keycloak id. The app database needs a User row with that KeycloakId in the demo
#   tenant (Administrator / Member) or the API will reject the login.
#
# Only run this against the demo tenant's realm. Never against a real organization's realm.
#
# Usage: KEYCLOAK_ADMIN_PASSWORD=... DEMO_LOGIN_PASSWORD=... ./setup-demo-login.sh --realm <realm> [--keycloak-url URL]

set -euo pipefail

REALM=""
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
ADMIN_USERNAME="${KEYCLOAK_ADMIN_USER:-admin}"
CLIENT_ID="student-registrar-spa"

while [ $# -gt 0 ]; do
  case "$1" in
    --realm) REALM="$2"; shift 2 ;;
    --keycloak-url) KEYCLOAK_URL="$2"; shift 2 ;;
    --admin-username) ADMIN_USERNAME="$2"; shift 2 ;;
    --client-id) CLIENT_ID="$2"; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

[ -n "$REALM" ] || { echo "--realm is required" >&2; exit 2; }
[ -n "${KEYCLOAK_ADMIN_PASSWORD:-}" ] || { echo "Set KEYCLOAK_ADMIN_PASSWORD" >&2; exit 2; }
[ -n "${DEMO_LOGIN_PASSWORD:-}" ] || { echo "Set DEMO_LOGIN_PASSWORD" >&2; exit 2; }
command -v jq >/dev/null || { echo "jq is required" >&2; exit 2; }

KEYCLOAK_URL="${KEYCLOAK_URL%/}"
ADMIN_API="$KEYCLOAK_URL/admin/realms/$REALM"

TOKEN=$(curl -fsS -X POST "$KEYCLOAK_URL/realms/master/protocol/openid-connect/token" \
  -d "username=$ADMIN_USERNAME" --data-urlencode "password=$KEYCLOAK_ADMIN_PASSWORD" \
  -d "grant_type=password" -d "client_id=admin-cli" | jq -r .access_token)

api() { # method path [json-body]
  local method="$1" path="$2" body="${3:-}"
  if [ -n "$body" ]; then
    curl -fsS -X "$method" "$ADMIN_API$path" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" -d "$body"
  else
    curl -fsS -X "$method" "$ADMIN_API$path" -H "Authorization: Bearer $TOKEN"
  fi
}

CLIENT=$(api GET "/clients?clientId=$CLIENT_ID" | jq '.[0]')
[ "$CLIENT" != "null" ] || { echo "Client '$CLIENT_ID' not found in realm '$REALM'." >&2; exit 1; }
if [ "$(echo "$CLIENT" | jq -r .directAccessGrantsEnabled)" != "true" ]; then
  api PUT "/clients/$(echo "$CLIENT" | jq -r .id)" "$(echo "$CLIENT" | jq '.directAccessGrantsEnabled = true')" >/dev/null
  echo "Enabled direct access grants on '$CLIENT_ID'."
fi

ACCOUNT_CONSOLE=$(api GET "/clients?clientId=account-console" | jq '.[0]')
if [ "$ACCOUNT_CONSOLE" != "null" ] && [ "$(echo "$ACCOUNT_CONSOLE" | jq -r .enabled)" = "true" ]; then
  api PUT "/clients/$(echo "$ACCOUNT_CONSOLE" | jq -r .id)" "$(echo "$ACCOUNT_CONSOLE" | jq '.enabled = false')" >/dev/null
  echo "Disabled 'account-console' client."
fi

api PUT "" "$(jq -n --arg realm "$REALM" '{realm: $realm, bruteForceProtected: false}')" >/dev/null

setup_user() { # username first last role
  local username="$1" first="$2" last="$3" role="$4" user_id
  user_id=$(api GET "/users?username=$username&exact=true" | jq -r '.[0].id // empty')
  if [ -z "$user_id" ]; then
    api POST "/users" "$(jq -n --arg u "$username" --arg f "$first" --arg l "$last" \
      '{username: $u, email: ($u + "@demo.invalid"), firstName: $f, lastName: $l, enabled: true, emailVerified: true}')" >/dev/null
    user_id=$(api GET "/users?username=$username&exact=true" | jq -r '.[0].id')
    echo "Created $username."
  fi

  api PUT "/users/$user_id/reset-password" \
    "$(jq -n --arg p "$DEMO_LOGIN_PASSWORD" '{type: "password", value: $p, temporary: false}')" >/dev/null

  if ! api GET "/users/$user_id/role-mappings/realm" | jq -e --arg r "$role" 'any(.[]; .name == $r)' >/dev/null; then
    api POST "/users/$user_id/role-mappings/realm" "[$(api GET "/roles/$role" | jq -c '{id, name}')]" >/dev/null
  fi

  echo "$username  role=$role  KeycloakId=$user_id"
}

setup_user demo-admin Demo Organizer Administrator
setup_user demo-member Demo Family Member

echo "Done. Ensure the app database has User rows with these KeycloakIds in the demo tenant."

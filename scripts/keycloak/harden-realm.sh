#!/usr/bin/env bash
# harden-realm.sh
# Applies security hardening to an existing deployed Keycloak realm.
# Safe to run against STG or PRD — all operations are idempotent PUTs.
#
# Usage:
#   ./harden-realm.sh --keycloak-url https://keycloak-stg.ruffregistrar.com \
#                     --admin-password-file /path/to/password.txt
#
# Non-interactive via env var:
#   KEYCLOAK_ADMIN_PASSWORD=... ./harden-realm.sh --keycloak-url https://...

set -euo pipefail

REALM="student-registrar"
KEYCLOAK_URL=""
ADMIN_USERNAME="admin"
ADMIN_PASSWORD_FILE=""

usage() {
  cat <<EOF
Usage: $0 [options]
  --realm NAME                   Realm name (default: ${REALM})
  --keycloak-url URL             Base Keycloak URL (required)
  --admin-username NAME          Master realm admin username (default: ${ADMIN_USERNAME})
  --admin-password-file PATH     File whose contents are the master admin password
  --help                         Show this help

Environment overrides:
  KEYCLOAK_ADMIN_PASSWORD   Master admin password (skips prompt)
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --realm) REALM="$2"; shift 2;;
    --keycloak-url) KEYCLOAK_URL="$2"; shift 2;;
    --admin-username) ADMIN_USERNAME="$2"; shift 2;;
    --admin-password-file) ADMIN_PASSWORD_FILE="$2"; shift 2;;
    --help|-h) usage; exit 0;;
    *) echo "Unknown option: $1" >&2; usage; exit 1;;
  esac
done

if [[ -z "$KEYCLOAK_URL" ]]; then
  echo "Error: --keycloak-url is required" >&2; usage; exit 1; fi

if ! command -v jq >/dev/null; then
  echo "jq is required" >&2; exit 2; fi
if ! command -v curl >/dev/null; then
  echo "curl is required" >&2; exit 2; fi

if [[ -n "$ADMIN_PASSWORD_FILE" ]]; then
  if [[ ! -f "$ADMIN_PASSWORD_FILE" ]]; then
    echo "Admin password file not found: $ADMIN_PASSWORD_FILE" >&2; exit 2; fi
  KEYCLOAK_ADMIN_PASSWORD=$(<"$ADMIN_PASSWORD_FILE")
fi

if [[ -z "${KEYCLOAK_ADMIN_PASSWORD:-}" ]]; then
  read -s -p "Enter Keycloak master admin password for user '${ADMIN_USERNAME}': " KEYCLOAK_ADMIN_PASSWORD; echo
fi

echo "🔐 Hardening realm '${REALM}' at ${KEYCLOAK_URL}"

TOKEN=$(curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode "username=${ADMIN_USERNAME}" \
  --data-urlencode "password=${KEYCLOAK_ADMIN_PASSWORD}" \
  -d 'client_id=admin-cli' \
  -d 'grant_type=password' | jq -r '.access_token')

if [[ -z "$TOKEN" || "$TOKEN" == null ]]; then
  echo "Failed to obtain admin token — check credentials and URL" >&2; exit 4; fi

echo "✅ Admin token obtained"

# ─── 1. Harden the realm itself ─────────────────────────────────────────────
echo ""
echo "Applying realm security settings..."

REALM_PATCH=$(jq -n '{
  sslRequired: "external",
  verifyEmail: true,
  bruteForceProtected: true,
  permanentLockout: false,
  maxFailureWaitSeconds: 900,
  minimumQuickLoginWaitSeconds: 60,
  waitIncrementSeconds: 60,
  quickLoginCheckMilliSeconds: 1000,
  maxDeltaTimeSeconds: 43200,
  failureFactor: 5,
  passwordPolicy: "length(12) and upperCase(1) and lowerCase(1) and digits(1) and specialChars(1) and notUsername(undefined) and passwordHistory(3)",
  accessTokenLifespan: 300,
  ssoSessionIdleTimeout: 1800,
  ssoSessionMaxLifespan: 36000,
  offlineSessionIdleTimeout: 2592000,
  offlineSessionMaxLifespanEnabled: true,
  offlineSessionMaxLifespan: 5184000,
  accessCodeLifespan: 60,
  accessCodeLifespanLogin: 1800,
  accessCodeLifespanUserAction: 300
}')

STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "$REALM_PATCH" \
  "${KEYCLOAK_URL}/admin/realms/${REALM}")

if [[ "$STATUS" != "204" ]]; then
  echo "❌ Realm update failed (HTTP $STATUS)" >&2; exit 5; fi

echo "✅ Realm settings applied"

# ─── 2. Disable ROPC on the confidential backend client ─────────────────────
echo ""
echo "Disabling Direct Access Grants on 'student-registrar' client..."

CLIENT_UUID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=student-registrar" | jq -r '.[0].id')

if [[ -z "$CLIENT_UUID" || "$CLIENT_UUID" == null ]]; then
  echo "⚠️  Could not find client 'student-registrar' — skipping" >&2
else
  # Fetch the current full client representation so the PUT includes all existing fields
  CURRENT_CLIENT=$(curl -s -H "Authorization: Bearer $TOKEN" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${CLIENT_UUID}")
  
  UPDATED_CLIENT=$(echo "$CURRENT_CLIENT" | jq '.directAccessGrantsEnabled = false')

  STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X PUT \
    -H "Authorization: Bearer $TOKEN" \
    -H 'Content-Type: application/json' \
    -d "$UPDATED_CLIENT" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${CLIENT_UUID}")

  if [[ "$STATUS" != "204" ]]; then
    echo "❌ Client update failed (HTTP $STATUS)" >&2; exit 6; fi

  echo "✅ Direct Access Grants disabled on 'student-registrar'"
fi

# ─── 3. Verify SPA client has PKCE enforced ──────────────────────────────────
echo ""
echo "Verifying PKCE on 'student-registrar-spa'..."

SPA_UUID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "${KEYCLOAK_URL}/admin/realms/${REALM}/clients?clientId=student-registrar-spa" | jq -r '.[0].id')

if [[ -z "$SPA_UUID" || "$SPA_UUID" == null ]]; then
  echo "⚠️  Could not find client 'student-registrar-spa' — skipping" >&2
else
  SPA_CLIENT=$(curl -s -H "Authorization: Bearer $TOKEN" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${SPA_UUID}")

  PKCE_METHOD=$(echo "$SPA_CLIENT" | jq -r '.attributes["pkce.code.challenge.method"] // "NOT SET"')

  if [[ "$PKCE_METHOD" == "S256" ]]; then
    echo "✅ PKCE S256 already enforced on SPA client"
  else
    echo "Applying PKCE S256 to SPA client (currently: ${PKCE_METHOD})..."
    UPDATED_SPA=$(echo "$SPA_CLIENT" | jq '.attributes["pkce.code.challenge.method"] = "S256" | .directAccessGrantsEnabled = false')

    STATUS=$(curl -s -o /dev/null -w '%{http_code}' -X PUT \
      -H "Authorization: Bearer $TOKEN" \
      -H 'Content-Type: application/json' \
      -d "$UPDATED_SPA" \
      "${KEYCLOAK_URL}/admin/realms/${REALM}/clients/${SPA_UUID}")

    if [[ "$STATUS" != "204" ]]; then
      echo "❌ SPA client update failed (HTTP $STATUS)" >&2; exit 7; fi

    echo "✅ PKCE S256 applied to SPA client"
  fi
fi

echo ""
echo "🔒 Hardening complete for realm '${REALM}' at ${KEYCLOAK_URL}"
echo ""
echo "⚠️  Manual follow-up required:"
echo "  1. Verify redirect URIs on both clients are HTTPS-only (not localhost)"
echo "     Admin UI: ${KEYCLOAK_URL}/admin/master/console/#/${REALM}/clients"
echo "  2. If verifyEmail was just enabled, existing unverified accounts will be"
echo "     prompted to verify on next login — expected and correct behaviour."
echo "  3. The new password policy applies at next password change only."
echo "     Require password reset for existing accounts if immediate enforcement is needed."
echo "  4. Review Keycloak admin console access — consider IP-restricting at the"
echo "     ingress/network level if your hosting allows it."

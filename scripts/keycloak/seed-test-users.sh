#!/usr/bin/env bash
# seed-test-users.sh
# Adds deterministic test users (NOT for production)

set -euo pipefail
REALM="student-registrar"
KEYCLOAK_URL="http://localhost:8080"
# Aspire dev default master admin username is typically 'admin'.
ADMIN_USERNAME="admin"
EXPLICIT_KEYCLOAK_URL=0

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
    --keycloak-url) KEYCLOAK_URL="$2"; EXPLICIT_KEYCLOAK_URL=1; shift 2;;
    --admin-username) ADMIN_USERNAME="$2"; shift 2;;
    --help|-h) usage; exit 0;;
    *) echo "Unknown option $1"; usage; exit 1;;
  esac
done

if ! command -v jq >/dev/null; then echo "jq required"; exit 2; fi

test_keycloak_url(){
  local url="$1"
  curl -fsS --connect-timeout 5 "${url}/realms/master/.well-known/openid-configuration" >/dev/null 2>&1
}

is_wsl(){
  grep -qi microsoft /proc/version 2>/dev/null
}

delegate_to_powershell_seed(){
  local ps_runner
  if command -v pwsh.exe >/dev/null 2>&1; then
    ps_runner="pwsh.exe"
  elif command -v powershell.exe >/dev/null 2>&1; then
    ps_runner="powershell.exe"
  else
    return 1
  fi

  local script_dir script_path windows_script
  script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
  script_path="${script_dir}/seed-test-users.ps1"
  windows_script=$(wslpath -w "$script_path")

  echo "⚠️  localhost is not reachable from WSL; delegating to Windows PowerShell seed script" >&2
  "$ps_runner" -NoProfile -ExecutionPolicy Bypass -File "$windows_script" \
    -KeycloakUrl "$KEYCLOAK_URL" \
    -Realm "$REALM" \
    -AdminUsername "$ADMIN_USERNAME" \
    -AdminPassword "$ADMIN_PASSWORD"
}

resolve_keycloak_url(){
  if [[ "$EXPLICIT_KEYCLOAK_URL" == "1" ]]; then
    echo "$KEYCLOAK_URL"
    return
  fi

  if test_keycloak_url "$KEYCLOAK_URL"; then
    echo "$KEYCLOAK_URL"
    return
  fi

  if is_wsl; then
    local default_host_ip
    default_host_ip=$(awk '/nameserver/ { print $2; exit }' /etc/resolv.conf 2>/dev/null || true)
    if [[ -n "$default_host_ip" ]]; then
      local fallback_url
      fallback_url=$(echo "$KEYCLOAK_URL" | sed -E "s#(https?://)(localhost|127\.0\.0\.1)(:[0-9]+)?#\1${default_host_ip}\3#")
      if [[ "$fallback_url" != "$KEYCLOAK_URL" ]] && test_keycloak_url "$fallback_url"; then
        echo "⚠️  localhost is not reachable from WSL; using Windows host Keycloak at $fallback_url" >&2
        echo "$fallback_url"
        return
      fi
    fi
  fi

  echo "$KEYCLOAK_URL"
}

KEYCLOAK_URL=$(resolve_keycloak_url)

if [[ -n "${KEYCLOAK_ADMIN_PASSWORD:-}" ]]; then
  ADMIN_PASSWORD="$KEYCLOAK_ADMIN_PASSWORD"
  echo "🔑 Using KEYCLOAK_ADMIN_PASSWORD from environment"
else
  read -s -p "Enter Keycloak master admin password: " ADMIN_PASSWORD; echo
fi

if ! test_keycloak_url "$KEYCLOAK_URL"; then
  if is_wsl && [[ "$KEYCLOAK_URL" =~ ^http://(localhost|127\.0\.0\.1): ]] && delegate_to_powershell_seed; then
    exit 0
  fi

  echo "❌ Cannot reach Keycloak at ${KEYCLOAK_URL}" >&2
  exit 4
fi

TOKEN=$(curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  --data-urlencode "username=${ADMIN_USERNAME}" \
  --data-urlencode "password=${ADMIN_PASSWORD}" \
  -d 'client_id=admin-cli' -d 'grant_type=password' | jq -r '.access_token // empty')

if [[ -z "$TOKEN" || "$TOKEN" == null ]]; then echo "Failed to get token" >&2; exit 3; fi

check_status(){
  local status="$1" action="$2" body="${3:-}"
  if [[ "$status" =~ ^2 ]]; then
    return 0
  fi

  echo "  ${action} failed HTTP ${status}" >&2
  if [[ -n "$body" ]]; then
    echo "  Response: ${body}" >&2
  fi
  return 1
}

ensure_password(){
  local username="$1" password="$2" uid="$3"
  local response status body token_check

  response=$(curl -s -w $'\n%{http_code}' -X PUT \
    -H "Authorization: Bearer $TOKEN" \
    -H 'Content-Type: application/json' \
    -d "{\"type\":\"password\",\"temporary\":false,\"value\":\"${password}\"}" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${uid}/reset-password")
  status=$(echo "$response" | tail -n1)
  body=$(echo "$response" | sed '$d')

  if [[ "$status" != "204" ]]; then
    token_check=$(curl -s -X POST "${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token" \
      -H 'Content-Type: application/x-www-form-urlencoded' \
      --data-urlencode "client_id=student-registrar-spa" \
      -d 'grant_type=password' \
      --data-urlencode "username=${username}" \
      --data-urlencode "password=${password}" \
      -d 'scope=openid profile email')

    if [[ -n "$(echo "$token_check" | jq -r '.access_token // empty')" ]]; then
      echo "  Password already valid"
      return 0
    fi

    check_status "$status" "reset password" "$body" || return 1
  fi
}

ensure_role(){
  local uid="$1" role="$2"
  local role_json rid existing_roles

  existing_roles=$(curl -s -H "Authorization: Bearer $TOKEN" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${uid}/role-mappings/realm")
  if echo "$existing_roles" | jq -e --arg role "$role" '.[]? | select(.name == $role)' >/dev/null; then
    echo "  Role ${role} already assigned"
    return 0
  fi

  role_json=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/roles/${role}")
  rid=$(echo "$role_json" | jq -r '.id // empty')
  if [[ -z "$rid" ]]; then
    echo "  Role ${role} not found" >&2
    return 1
  fi

  local response status body
  response=$(curl -s -w $'\n%{http_code}' -X POST \
    -H "Authorization: Bearer $TOKEN" \
    -H 'Content-Type: application/json' \
    -d "[{\"id\":\"${rid}\",\"name\":\"${role}\"}]" \
    "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${uid}/role-mappings/realm")
  status=$(echo "$response" | tail -n1)
  body=$(echo "$response" | sed '$d')
  check_status "$status" "assign role ${role}" "$body"
}

create_user(){
  local username="$1" role="$2" pass="$3" email="$4" first_name="$5" last_name="$6"
  local user_lookup uid payload response status body

  echo "Ensuring user $username ($role)"
  user_lookup=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/users?username=${username}")
  uid=$(echo "$user_lookup" | jq -r '.[0].id // empty')
  payload=$(jq -n \
    --arg u "$username" \
    --arg e "$email" \
    --arg first "$first_name" \
    --arg last "$last_name" \
    '{username:$u, email:$e, enabled:true, emailVerified:true, firstName:$first, lastName:$last, requiredActions:[]}')

  if [[ -z "$uid" ]]; then
    response=$(curl -s -w $'\n%{http_code}' -X POST \
      -H "Authorization: Bearer $TOKEN" \
      -H 'Content-Type: application/json' \
      -d "$payload" \
      "${KEYCLOAK_URL}/admin/realms/${REALM}/users")
    status=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    check_status "$status" "create user ${username}" "$body" || return 1

    user_lookup=$(curl -s -H "Authorization: Bearer $TOKEN" "${KEYCLOAK_URL}/admin/realms/${REALM}/users?username=${username}")
    uid=$(echo "$user_lookup" | jq -r '.[0].id // empty')
    if [[ -z "$uid" ]]; then
      echo "  Unable to resolve created user ${username}" >&2
      return 1
    fi
  else
    response=$(curl -s -w $'\n%{http_code}' -X PUT \
      -H "Authorization: Bearer $TOKEN" \
      -H 'Content-Type: application/json' \
      -d "$(echo "$payload" | jq --arg id "$uid" '. + {id:$id}')" \
      "${KEYCLOAK_URL}/admin/realms/${REALM}/users/${uid}")
    status=$(echo "$response" | tail -n1)
    body=$(echo "$response" | sed '$d')
    check_status "$status" "update user ${username}" "$body" || return 1
  fi

  ensure_password "$username" "$pass" "$uid" || return 1
  ensure_role "$uid" "$role" || return 1
}

create_user scoopadmin Administrator 'ChangeThis123!' scoopadmin@example.com Scoop Admin
create_user scoopmember Member 'MemberPass123!' scoopmember@example.com Scoop Member
create_user scoopeducator Educator 'EducatorPass123!' scoopeducator@example.com Scoop Educator

echo "✅ Test users seeded"

# Keycloak Bootstrap & Seeding

This directory contains scripts and assets to bootstrap a new Keycloak instance for the Student Registrar application.

## Goals
- One-time, fail-fast bootstrap for production install (no test users).
- Optional dev/test seed with deterministic test users & roles.
- Operator prompted for initial real Administrator account.
- Realm configuration expressed as code (JSON) for reproducibility.

## Components
- `bootstrap-keycloak.sh` – Creates realm, roles, application client, and prompts for the first Administrator user (no test users). Designed to run exactly once.
- `seed-test-users.sh` – Adds development / test users (Administrator, Educator, Member variants) – NOT for production.
- `realm-student-registrar.template.json` – Minimal sanitized realm definition used as a baseline. Dynamic values (hostnames, client redirect URIs) are applied by bootstrap script.
- `add-spa-client.sh` – Adds/updates the public SPA client used by the frontend.

## SPA client setup (local vs ACA)

### Local development
```bash
KEYCLOAK_ADMIN_PASSWORD=admin123! \
  ./scripts/keycloak/add-spa-client.sh \
  --keycloak-url http://localhost:8080
```

### Azure Container Apps (ACA)
```bash
KEYCLOAK_ADMIN_PASSWORD='<admin-password>' \
  ./scripts/keycloak/add-spa-client.sh \
  --keycloak-url https://keycloak.<env>.<region>.azurecontainerapps.io \
  --redirect-uris "https://frontend.<env>.<region>.azurecontainerapps.io/*" \
  --web-origins "https://frontend.<env>.<region>.azurecontainerapps.io"
```

You can also provide the same values via `REDIRECT_URIS` and `WEB_ORIGINS` env vars.

## Usage

### Production / First Install (Interactive)
```
./scripts/keycloak/bootstrap-keycloak.sh --keycloak-url http://keycloak:8080 \
  --admin-username admin --realm student-registrar
```
Prompts:
1. Master (Keycloak) admin password
2. First application admin username
3. First application admin email
4. Temp password (marked temporary; user must change on first login)

If the realm exists the script exits with code 10 (no changes).

### Non-Interactive (CI / Automated Install)
Avoid putting secrets directly in command line history; prefer files or env vars.

Option A (env vars + flags):
```
export KEYCLOAK_ADMIN_PASSWORD="$(cat /run/secrets/kc_admin_pass)"
export INITIAL_ADMIN_TEMP_PASS="TempAdmin!12345"
./scripts/keycloak/bootstrap-keycloak.sh \
  --realm student-registrar \
  --keycloak-url http://keycloak:8080 \
  --admin-username admin \
  --initial-admin-username registrar-admin \
  --initial-admin-email admin@example.org
```

Option B (password file):
```
./scripts/keycloak/bootstrap-keycloak.sh \
  --admin-password-file /run/secrets/kc_admin_pass \
  --initial-admin-username registrar-admin \
  --initial-admin-email admin@example.org \
  --initial-admin-temp-pass TempAdmin!12345
```

Exit codes:
- 0 success
- 10 realm already exists (safe to treat as success in idempotent automation)
- >0 other failure

### Dev / Test Seeding
After bootstrap (or using existing dev realm):
```
./scripts/keycloak/seed-test-users.sh --keycloak-url http://localhost:8080 --realm student-registrar
```
Creates deterministic users used by automated tests.

## Regenerating Realm Template
1. Make interactive changes in a disposable Keycloak instance.
2. Export realm without users:
```
docker exec -it <kc-container> /opt/keycloak/bin/kc.sh export --realm student-registrar --dir /tmp/export --users skip
```
3. Copy `/tmp/export/student-registrar-realm.json` out, sanitize secrets, rename to `realm-student-registrar.template.json`.
4. Commit changes.

## Notes
- Do NOT commit real secrets or certificates.
- Test scripts rely on stable usernames from `seed-test-users.sh`.
- Future: Add automated smoke test invoking token endpoint with seeded client.

#!/bin/bash

# Smoke test runner script for production or pre-prod checks
# Required env vars:
#   SMOKE_WEB_URL, SMOKE_API_URL, SMOKE_KEYCLOAK_URL, SMOKE_USERNAME, SMOKE_PASSWORD
# Optional:
#   SMOKE_REALM, SMOKE_CLIENT_ID, SMOKE_CLIENT_SECRET

set -euo pipefail

echo "🧪 Running Student Registrar Smoke Tests"
echo "======================================="

dotnet test tests/StudentRegistrar.Smoke.Tests/ --verbosity normal

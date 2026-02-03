#!/bin/sh
set -e

if [ -z "${POSTGRES_USER:-}" ]; then
  echo "POSTGRES_USER is not set" >&2
  exit 1
fi

if [ -z "${POSTGRES_DB:-}" ]; then
  echo "POSTGRES_DB is not set" >&2
  exit 1
fi

# Create keycloak database if it does not exist
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" <<'SQL'
SELECT 1 FROM pg_database WHERE datname = 'keycloak';
SQL

if ! psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" -tAc "SELECT 1 FROM pg_database WHERE datname = 'keycloak'" | grep -q 1; then
  echo "Creating keycloak database..."
  createdb -U "$POSTGRES_USER" keycloak
else
  echo "keycloak database already exists."
fi

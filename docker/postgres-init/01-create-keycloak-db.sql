SELECT 'CREATE DATABASE keycloak'
WHERE NOT EXISTS (
	SELECT 1 FROM pg_database WHERE datname = 'keycloak'
)\gexec

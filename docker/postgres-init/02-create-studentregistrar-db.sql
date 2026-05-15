SELECT 'CREATE DATABASE studentregistrar'
WHERE NOT EXISTS (
	SELECT 1 FROM pg_database WHERE datname = 'studentregistrar'
)
\gexec
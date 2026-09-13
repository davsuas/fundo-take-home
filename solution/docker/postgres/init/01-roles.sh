#!/bin/sh
# Runs once, on first container init, as the bootstrap superuser (docker-entrypoint-initdb.d).
# Creates two roles: a migrator (owns the schema, used only by the one-shot `migrate` service)
# and a least-privilege app role (no DDL rights) used by api/worker at runtime, so the running
# application can never alter its own schema. A shell script rather than .sql because the role
# passwords come from env vars, which .sql init files do not expand.
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  CREATE ROLE ${POSTGRES_MIGRATOR_USER} LOGIN PASSWORD '${POSTGRES_MIGRATOR_PASSWORD}';
  ALTER DATABASE ${POSTGRES_DB} OWNER TO ${POSTGRES_MIGRATOR_USER};
  GRANT ALL PRIVILEGES ON DATABASE ${POSTGRES_DB} TO ${POSTGRES_MIGRATOR_USER};

  CREATE ROLE ${POSTGRES_APP_USER} LOGIN PASSWORD '${POSTGRES_APP_PASSWORD}';
  GRANT CONNECT ON DATABASE ${POSTGRES_DB} TO ${POSTGRES_APP_USER};
EOSQL

# Schema-level grants must run after the migrator has created the schema, so the app role's
# default privileges are set up here too (applies to tables created later by the migrator).
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_MIGRATOR_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  GRANT USAGE ON SCHEMA public TO ${POSTGRES_APP_USER};
  ALTER DEFAULT PRIVILEGES FOR ROLE ${POSTGRES_MIGRATOR_USER} IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ${POSTGRES_APP_USER};
  ALTER DEFAULT PRIVILEGES FOR ROLE ${POSTGRES_MIGRATOR_USER} IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO ${POSTGRES_APP_USER};
EOSQL

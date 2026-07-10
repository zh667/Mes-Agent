#!/usr/bin/env bash
set -euo pipefail

if [ ! -s "$PGDATA/PG_VERSION" ]; then
  rm -rf "${PGDATA:?}"/*
  export PGPASSWORD="$REPLICATION_PASSWORD"
  until pg_isready -h "$PRIMARY_HOST" -U "$POSTGRES_USER"; do sleep 2; done
  pg_basebackup -h "$PRIMARY_HOST" -D "$PGDATA" -U "$REPLICATION_USER" -Fp -Xs -P -R
  chmod 700 "$PGDATA"
fi

exec docker-entrypoint.sh postgres

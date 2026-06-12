#!/bin/bash
set -euo pipefail

if [ -z "${MSSQL_SA_PASSWORD:-}" ]; then
  echo "ERROR: MSSQL_SA_PASSWORD environment variable is required."
  exit 1
fi

# Railway volumes are mounted as root; SQL Server runs as mssql (UID 10001).
if [ -d /var/opt/mssql ]; then
  mkdir -p /var/opt/mssql/data /var/opt/mssql/log /var/opt/mssql/secrets
  chown -R 10001:0 /var/opt/mssql
  chmod -R 770 /var/opt/mssql
fi

exec su mssql -c "/opt/mssql/bin/launch_sqlservr.sh"

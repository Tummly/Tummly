#!/bin/bash
set -eu

echo "[entrypoint] Starting TummlyDb SQL Server setup..."

if [ -z "${MSSQL_SA_PASSWORD:-}" ]; then
  echo "[entrypoint] ERROR: MSSQL_SA_PASSWORD is not set."
  exit 1
fi

echo "[entrypoint] MSSQL_MEMORY_LIMIT_MB=${MSSQL_MEMORY_LIMIT_MB:-not set}"

if [ -d /var/opt/mssql ]; then
  echo "[entrypoint] Preparing /var/opt/mssql volume permissions..."
  mkdir -p /var/opt/mssql/data /var/opt/mssql/log /var/opt/mssql/secrets
  chown -R 10001:0 /var/opt/mssql
  chmod -R 770 /var/opt/mssql
else
  echo "[entrypoint] No volume at /var/opt/mssql — using ephemeral storage."
fi

# Let the official launcher drop from root to mssql (same as Microsoft's image).
echo "[entrypoint] Launching SQL Server..."
exec /opt/mssql/bin/launch_sqlservr.sh

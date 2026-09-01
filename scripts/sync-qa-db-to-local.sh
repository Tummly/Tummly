#!/usr/bin/env bash
# Export Azure QA SQL (sqldb-tummly-qa) and import into local Docker TummlyDB.
#
# Prerequisites:
#   - Docker: backend/TummlyDb/vps/.env with MSSQL_SA_PASSWORD
#   - QA creds: infra/qa/secrets.qa.env (ConnectionStrings__DefaultConnection)
#   - SqlPackage on PATH (~/.local/sqlpackage or dotnet tool)
#   - Your public IP allowed on the Azure SQL firewall
#
# Usage:
#   ./scripts/sync-qa-db-to-local.sh
#   ./scripts/sync-qa-db-to-local.sh --skip-export   # re-import existing BACPAC
#   ./scripts/sync-qa-db-to-local.sh --bacpac /path/to/file.bacpac
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VPS_DIR="$ROOT/backend/TummlyDb/vps"
VPS_ENV="$VPS_DIR/.env"
QA_SECRETS="$ROOT/infra/qa/secrets.qa.env"
WORKDIR="${TUMMLY_DB_SYNC_DIR:-/tmp/tummly-db}"
BACPAC="${WORKDIR}/sqldb-tummly-qa.bacpac"
LOCAL_DB="${TUMMLY_LOCAL_DB_NAME:-TummlyDB}"
CONTAINER="${TUMMLY_MSSQL_CONTAINER:-tummly-mssql}"
SKIP_EXPORT=0

usage() {
  cat <<'EOF'
Usage: sync-qa-db-to-local.sh [options]

Options:
  --skip-export     Import an existing BACPAC (skip Azure export)
  --bacpac PATH     BACPAC file to import (default: /tmp/tummly-db/sqldb-tummly-qa.bacpac)
  --workdir PATH    Directory for BACPAC and temp files (default: /tmp/tummly-db)
  -h, --help        Show this help

After import, run the API against local SQL:
  ./scripts/run-local-api.sh
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --skip-export) SKIP_EXPORT=1; shift ;;
    --bacpac)
      [[ $# -ge 2 ]] || { echo "Missing value for --bacpac" >&2; exit 1; }
      BACPAC="$2"
      shift 2
      ;;
    --workdir)
      [[ $# -ge 2 ]] || { echo "Missing value for --workdir" >&2; exit 1; }
      WORKDIR="$2"
      BACPAC="$WORKDIR/sqldb-tummly-qa.bacpac"
      shift 2
      ;;
    -h | --help) usage; exit 0 ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

mkdir -p "$WORKDIR"
chmod 700 "$WORKDIR" 2>/dev/null || true

if [[ ! -f "$VPS_ENV" ]]; then
  echo "Missing $VPS_ENV — copy from .env.example and set MSSQL_SA_PASSWORD." >&2
  exit 1
fi

MSSQL_SA_PASSWORD="$(grep -E '^MSSQL_SA_PASSWORD=' "$VPS_ENV" | head -1 | cut -d= -f2-)"
if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
  echo "MSSQL_SA_PASSWORD is empty in $VPS_ENV" >&2
  exit 1
fi

resolve_sqlpackage() {
  export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
  export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

  # Prefer standalone build (no .NET runtime required at launch).
  if [[ -x "${HOME}/.local/sqlpackage/sqlpackage" ]]; then
    echo "${HOME}/.local/sqlpackage/sqlpackage"
    return
  fi
  if command -v sqlpackage >/dev/null 2>&1; then
    command -v sqlpackage
    return
  fi
  echo "SqlPackage not found. Install:" >&2
  echo "  curl -fsSL https://aka.ms/sqlpackage-linux -o /tmp/sqlpackage.zip" >&2
  echo "  unzip -o /tmp/sqlpackage.zip -d ~/.local/sqlpackage" >&2
  echo "  chmod +x ~/.local/sqlpackage/sqlpackage" >&2
  exit 1
}

SQLPACKAGE="$(resolve_sqlpackage)"

setup_openssl_for_azure() {
  export OPENSSL_CONF="$WORKDIR/openssl-sql.cnf"
  cat >"$OPENSSL_CONF" <<'EOF'
openssl_conf = openssl_init
[openssl_init]
ssl_conf = ssl_sect
[ssl_sect]
system_default = system_default_sect
[system_default_sect]
MinProtocol = TLSv1.2
CipherString = DEFAULT:@SECLEVEL=0
EOF
}

read_qa_connection_string() {
  if [[ ! -f "$QA_SECRETS" ]]; then
    echo "Missing $QA_SECRETS (QA SQL connection string)." >&2
    exit 1
  fi
  python3 - <<'PY' "$QA_SECRETS"
import sys
from pathlib import Path

path = Path(sys.argv[1])
conn = None
for line in path.read_text().splitlines():
    line = line.strip()
    if line.startswith("ConnectionStrings__DefaultConnection="):
        conn = line.split("=", 1)[1].strip().rstrip(";")
        break

if not conn:
    raise SystemExit("ConnectionStrings__DefaultConnection not found in secrets file")

parts = {}
order = []
for bit in conn.split(";"):
    if not bit.strip() or "=" not in bit:
        continue
    k, v = bit.split("=", 1)
    key = k.strip().lower()
    parts[key] = (k.strip(), v.strip())
    if key not in {x.lower() for x in order}:
        order.append(key)

parts["encrypt"] = ("Encrypt", "True")
parts["trustservercertificate"] = ("TrustServerCertificate", "True")

seen = set()
out = []
for key in order + ["encrypt", "trustservercertificate"]:
    if key in seen or key not in parts:
        continue
    seen.add(key)
    name, val = parts[key]
    out.append(f"{name}={val}")

print(";".join(out))
PY
}

wait_for_sql() {
  local sqlcmd=""
  for candidate in \
    /opt/mssql-tools18/bin/sqlcmd \
    /opt/mssql-tools/bin/sqlcmd; do
    if docker exec "$CONTAINER" test -x "$candidate" 2>/dev/null; then
      sqlcmd="$candidate"
      break
    fi
  done
  if [[ -z "$sqlcmd" ]]; then
    echo "sqlcmd not found in container $CONTAINER" >&2
    exit 1
  fi

  echo "==> Waiting for SQL Server in $CONTAINER..."
  for i in $(seq 1 60); do
    if docker exec "$CONTAINER" "$sqlcmd" \
      -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
      echo "    SQL ready."
      return
    fi
    sleep 2
  done
  echo "SQL Server did not become ready in time." >&2
  docker logs "$CONTAINER" 2>&1 | tail -30 >&2
  exit 1
}

ensure_docker_sql() {
  echo "==> Starting local SQL (docker compose)..."
  (cd "$VPS_DIR" && docker compose up -d)
  wait_for_sql
}

drop_local_database() {
  local sqlcmd=""
  for candidate in \
    /opt/mssql-tools18/bin/sqlcmd \
    /opt/mssql-tools/bin/sqlcmd; do
    if docker exec "$CONTAINER" test -x "$candidate" 2>/dev/null; then
      sqlcmd="$candidate"
      break
    fi
  done

  echo "==> Dropping local database '$LOCAL_DB' if it exists..."
  docker exec "$CONTAINER" "$sqlcmd" \
    -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q \
    "IF DB_ID('$LOCAL_DB') IS NOT NULL BEGIN ALTER DATABASE [$LOCAL_DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$LOCAL_DB]; END"
}

export_qa_bacpac() {
  local qa_conn
  qa_conn="$(read_qa_connection_string)"
  setup_openssl_for_azure

  echo "==> Exporting QA database to $BACPAC ..."
  rm -f "$BACPAC"
  "$SQLPACKAGE" /Action:Export \
    /TargetFile:"$BACPAC" \
    /SourceConnectionString:"$qa_conn"
  chmod 600 "$BACPAC"
  echo "    Export complete ($(du -h "$BACPAC" | cut -f1))."
}

import_bacpac() {
  if [[ ! -f "$BACPAC" ]]; then
    echo "BACPAC not found: $BACPAC" >&2
    exit 1
  fi

  setup_openssl_for_azure
  local target_conn="Server=localhost,1433;Database=$LOCAL_DB;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=True;"

  echo "==> Importing into local '$LOCAL_DB'..."
  "$SQLPACKAGE" /Action:Import \
    /SourceFile:"$BACPAC" \
    /TargetConnectionString:"$target_conn"
  echo "    Import complete."
}

print_row_counts() {
  local sqlcmd=""
  for candidate in \
    /opt/mssql-tools18/bin/sqlcmd \
    /opt/mssql-tools/bin/sqlcmd; do
    if docker exec "$CONTAINER" test -x "$candidate" 2>/dev/null; then
      sqlcmd="$candidate"
      break
    fi
  done

  echo "==> Row counts:"
  docker exec "$CONTAINER" "$sqlcmd" \
    -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d "$LOCAL_DB" -Q \
    "SET NOCOUNT ON;
     SELECT 'Users' AS [Table], COUNT(*) AS [Rows] FROM dbo.Users
     UNION ALL SELECT 'Restaurants', COUNT(*) FROM dbo.Restaurants
     UNION ALL SELECT 'Feedbacks', COUNT(*) FROM dbo.Feedbacks
     UNION ALL SELECT 'BillingAccounts', COUNT(*) FROM dbo.BillingAccounts;"
}

main() {
  ensure_docker_sql

  if [[ "$SKIP_EXPORT" -eq 0 ]]; then
    export_qa_bacpac
  else
    echo "==> Skipping export; using existing BACPAC: $BACPAC"
    [[ -f "$BACPAC" ]] || { echo "BACPAC missing." >&2; exit 1; }
  fi

  drop_local_database
  import_bacpac
  print_row_counts

  cat <<EOF

Done. Local database '$LOCAL_DB' now mirrors QA data in $BACPAC.

Start the API:
  ./scripts/run-local-api.sh

Do not run 'dotnet ef database update' if you want this exact QA snapshot.
EOF
}

main "$@"

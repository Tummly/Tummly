#!/usr/bin/env bash
# Delete all application rows from Azure QA SQL. Keeps schema and
# dbo.__EFMigrationsHistory so EF migrations stay applied.
#
# Prerequisites:
#   - Docker container tummly-mssql with sqlcmd18
#   - infra/qa/secrets.qa.env (ConnectionStrings__DefaultConnection)
#   - Your public IP allowed on the Azure SQL firewall
#
# Usage:
#   ./scripts/purge-qa-db.sh --confirm
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
QA_SECRETS="$ROOT/infra/qa/secrets.qa.env"
CONTAINER="${TUMMLY_MSSQL_CONTAINER:-tummly-mssql}"
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
WORKDIR="${TUMMLY_DB_SYNC_DIR:-/tmp/tummly-db}"

usage() {
  cat <<'EOF'
Usage: purge-qa-db.sh --confirm

Deletes ALL rows in sqldb-tummly-qa except dbo.__EFMigrationsHistory.
Requires --confirm. Irreversible.
EOF
}

CONFIRM=0
while [[ $# -gt 0 ]]; do
  case "$1" in
    --confirm) CONFIRM=1; shift ;;
    -h | --help) usage; exit 0 ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ "$CONFIRM" -ne 1 ]]; then
  usage >&2
  exit 1
fi

if [[ ! -f "$QA_SECRETS" ]]; then
  echo "Missing $QA_SECRETS" >&2
  exit 1
fi

if ! docker exec "$CONTAINER" test -x "$SQLCMD" 2>/dev/null; then
  echo "sqlcmd not found in container $CONTAINER ($SQLCMD)" >&2
  exit 1
fi

mkdir -p "$WORKDIR"
chmod 700 "$WORKDIR" 2>/dev/null || true

# shellcheck disable=SC1091
eval "$(
  python3 - <<'PY' "$QA_SECRETS"
import shlex
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
    raise SystemExit("ConnectionStrings__DefaultConnection not found")

parts = {}
for bit in conn.split(";"):
    if not bit.strip() or "=" not in bit:
        continue
    k, v = bit.split("=", 1)
    parts[k.strip().lower()] = v.strip()

server = parts.get("server") or parts.get("data source")
database = parts.get("initial catalog") or parts.get("database")
user = parts.get("user id") or parts.get("uid")
password = parts.get("password") or parts.get("pwd")
if not all([server, database, user, password]):
    raise SystemExit("Connection string missing Server/Database/User/Password")

server = server.removeprefix("tcp:")
print(f"QA_SERVER={shlex.quote(server)}")
print(f"QA_DATABASE={shlex.quote(database)}")
print(f"QA_USER={shlex.quote(user)}")
print(f"QA_PASSWORD={shlex.quote(password)}")
PY
)"

PURGE_SQL_FILE="$WORKDIR/purge-qa.sql"
cat >"$PURGE_SQL_FILE" <<'SQL'
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRAN;

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql + N'ALTER TABLE '
  + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name)
  + N' NOCHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables
WHERE is_ms_shipped = 0
  AND name <> N'__EFMigrationsHistory';

EXEC sys.sp_executesql @sql;

SET @sql = N'';
SELECT @sql = @sql + N'DELETE FROM '
  + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name)
  + N';' + CHAR(10)
FROM sys.tables
WHERE is_ms_shipped = 0
  AND name <> N'__EFMigrationsHistory';

EXEC sys.sp_executesql @sql;

SET @sql = N'';
SELECT @sql = @sql + N'ALTER TABLE '
  + QUOTENAME(SCHEMA_NAME(schema_id)) + N'.' + QUOTENAME(name)
  + N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(10)
FROM sys.tables
WHERE is_ms_shipped = 0
  AND name <> N'__EFMigrationsHistory';

EXEC sys.sp_executesql @sql;

-- Reseed identity columns so new signups start near 1.
DECLARE reseed CURSOR LOCAL FAST_FORWARD FOR
SELECT SCHEMA_NAME(t.schema_id) AS SchemaName, t.name AS TableName
FROM sys.tables AS t
INNER JOIN sys.identity_columns AS ic
  ON ic.object_id = t.object_id
WHERE t.is_ms_shipped = 0
  AND t.name <> N'__EFMigrationsHistory';

DECLARE @schema SYSNAME;
DECLARE @table SYSNAME;
DECLARE @ident NVARCHAR(512);

OPEN reseed;
FETCH NEXT FROM reseed INTO @schema, @table;
WHILE @@FETCH_STATUS = 0
BEGIN
  SET @ident = @schema + N'.' + @table;
  DBCC CHECKIDENT (@ident, RESEED, 0) WITH NO_INFOMSGS;
  FETCH NEXT FROM reseed INTO @schema, @table;
END
CLOSE reseed;
DEALLOCATE reseed;

COMMIT;

SELECT
  t.name AS TableName,
  SUM(p.rows) AS ApproxRows
FROM sys.tables AS t
INNER JOIN sys.partitions AS p
  ON p.object_id = t.object_id
 AND p.index_id IN (0, 1)
WHERE t.is_ms_shipped = 0
GROUP BY t.name
HAVING SUM(p.rows) > 0
ORDER BY t.name;
SQL

echo "==> Purging ALL rows in ${QA_DATABASE} on ${QA_SERVER}"
echo "    Keeping dbo.__EFMigrationsHistory"
echo "    This cannot be undone."

docker cp "$PURGE_SQL_FILE" "$CONTAINER:/tmp/purge-qa.sql"

docker exec \
  -e QA_SERVER="$QA_SERVER" \
  -e QA_DATABASE="$QA_DATABASE" \
  -e QA_USER="$QA_USER" \
  -e QA_PASSWORD="$QA_PASSWORD" \
  "$CONTAINER" \
  "$SQLCMD" \
  -S "$QA_SERVER" \
  -d "$QA_DATABASE" \
  -U "$QA_USER" \
  -P "$QA_PASSWORD" \
  -C \
  -b \
  -i /tmp/purge-qa.sql

docker exec "$CONTAINER" rm -f /tmp/purge-qa.sql >/dev/null 2>&1 || true
rm -f "$PURGE_SQL_FILE"

echo "==> Purge finished."
echo "    Tables still listing rows above (expect only __EFMigrationsHistory)."
echo "    Schema and EF migration history were preserved."

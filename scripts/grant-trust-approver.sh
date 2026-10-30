#!/usr/bin/env bash
# Grant or revoke the trust indicator approver flag on one Admin.
#
# Only an approver can publish trust indicators (CONTRACT §6). No API or UI sets
# the flag, so this script is the only provisioning path. The flag change and its
# AdminAuditEvents row commit in one SQL transaction.
#
# Usage:
#   ./scripts/grant-trust-approver.sh --email admin@example.com --environment local
#   ./scripts/grant-trust-approver.sh --email admin@example.com --environment local --revoke
#   CONNECTION_STRING='Server=...;Database=...;User Id=...;Password=...;' \
#     ./scripts/grant-trust-approver.sh --email admin@example.com --environment qa
#
# Target:
#   Default: the local Docker container (TUMMLY_MSSQL_CONTAINER, default tummly-mssql),
#   database TUMMLY_LOCAL_DB_NAME (default TummlyDB). CONNECTION_STRING overrides it and
#   needs a host sqlcmd on PATH.
#
# The environment is not trusted from the flag. The script reads @@SERVERNAME and DB_NAME()
# from the target and maps them to an environment (CONTRACT §6). Mapped pairs:
#   local: the local container's hostname and TummlyDB
#   qa:    sql-tummly-qa-uksouth and sqldb-tummly-qa (infra/qa/main.bicep and
#          scripts/sync-qa-db-to-local.sh; the server name is compared case-insensitively)
# Any other pair is refused. --environment must equal the derived value. The write checks the
# same server and database again inside its transaction.
#
# Dry check, no connection: ./scripts/grant-trust-approver.sh --print-map SERVER DATABASE
#
# Production is refused unless TRUST_APPROVER_PRODUCTION_CONFIRMED=yes is set. Production is
# not mapped either, so the flag cannot make the script write to production.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VPS_ENV="$ROOT/backend/TummlyDb/vps/.env"
CONTAINER="${TUMMLY_MSSQL_CONTAINER:-tummly-mssql}"
LOCAL_DB="${TUMMLY_LOCAL_DB_NAME:-TummlyDB}"
# The fixed map (CONTRACT §6): the local container's TummlyDB is the only mapped database. It is a constant,
# not LOCAL_DB, so an overridden TUMMLY_LOCAL_DB_NAME cannot make another database count as local.
MAPPED_LOCAL_DB="TummlyDB"
# The QA pair from infra/qa/main.bicep (sql-tummly-${environment}-${sqlLocation}, environment qa, uksouth) and
# scripts/sync-qa-db-to-local.sh (sqldb-tummly-qa). Azure SQL reports the short server name.
MAPPED_QA_SERVER="sql-tummly-qa-uksouth"
MAPPED_QA_DB="sqldb-tummly-qa"
EMAIL_PATTERN='^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$'
NAME_PATTERN='^[A-Za-z0-9._-]+$'

usage() {
  cat <<'EOF'
Usage: grant-trust-approver.sh --email EMAIL --environment local|qa|production [--revoke]
       grant-trust-approver.sh --print-map SERVER DATABASE

Options:
  --email EMAIL          Admin account email (required)
  --environment ENV      local | qa | production (required; must match the target)
  --revoke               Clear the flag (default grants it)
  --print-map S D        Dry check: print the environment that server S and database D map to, and exit
                         (0 mapped, 3 unmapped). No connection is made.
  -h, --help             Show this help

Environment:
  CONNECTION_STRING                 Target database (default: the local container)
  TRUST_APPROVER_PRODUCTION_CONFIRMED  Must be "yes" for --environment production (production is not mapped)
  TUMMLY_MSSQL_CONTAINER            Local container name (default tummly-mssql)
  TUMMLY_LOCAL_DB_NAME              Local database name (default TummlyDB)

Exit codes: 0 changed; 1 SQL error or no change made; 2 bad usage; 3 refused by a safety gate.
EOF
}

EMAIL=""
ENVIRONMENT=""
ACTION="grant"
PRINT_MAP=0
PRINT_MAP_SERVER=""
PRINT_MAP_DB=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --email | --environment)
      [[ $# -ge 2 ]] || { echo "Missing value for $1" >&2; exit 2; }
      if [[ "$1" == "--email" ]]; then EMAIL="$2"; else ENVIRONMENT="$2"; fi
      shift 2
      ;;
    --revoke)
      ACTION="revoke"
      shift
      ;;
    --print-map)
      [[ $# -ge 3 ]] || { echo "Missing values: --print-map SERVER DATABASE" >&2; exit 2; }
      PRINT_MAP=1
      PRINT_MAP_SERVER="$2"
      PRINT_MAP_DB="$3"
      shift 3
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

# CONTRACT §6 derivation: the server name and database a target reports map to one environment, or "unmapped".
# Pure except for one read-only docker inspect (the local container's hostname, which is its @@SERVERNAME).
map_environment() {
  local server="$1" database="$2" local_host server_lc database_lc
  local_host="$(docker inspect -f '{{.Config.Hostname}}' "$CONTAINER" 2>/dev/null || true)"
  if [[ -n "$local_host" && "$server" == "$local_host" && "$database" == "$MAPPED_LOCAL_DB" ]]; then
    echo "local"
    return
  fi
  server_lc="$(printf '%s' "$server" | tr '[:upper:]' '[:lower:]')"
  database_lc="$(printf '%s' "$database" | tr '[:upper:]' '[:lower:]')"
  if [[ "$server_lc" == "$MAPPED_QA_SERVER" && "$database_lc" == "$MAPPED_QA_DB" ]]; then
    echo "qa"
    return
  fi
  echo "unmapped"
}

if [[ "$PRINT_MAP" == 1 ]]; then
  if [[ ! "$PRINT_MAP_SERVER" =~ $NAME_PATTERN || ! "$PRINT_MAP_DB" =~ $NAME_PATTERN ]]; then
    echo "Refused: the server or database name has characters this script does not allow." >&2
    exit 3
  fi
  MAP_RESULT="$(map_environment "$PRINT_MAP_SERVER" "$PRINT_MAP_DB")"
  if [[ "$MAP_RESULT" == "unmapped" ]]; then
    echo "unmapped: server $PRINT_MAP_SERVER, database $PRINT_MAP_DB is not a mapped environment." >&2
    exit 3
  fi
  echo "$MAP_RESULT"
  exit 0
fi

case "$ENVIRONMENT" in
  local | qa | production) ;;
  "") echo "--environment is required (local, qa or production)." >&2; usage >&2; exit 2 ;;
  *) echo "Unknown --environment: $ENVIRONMENT" >&2; exit 2 ;;
esac

if [[ -z "$EMAIL" ]]; then
  echo "--email is required." >&2
  usage >&2
  exit 2
fi

# The email is written into SQL text, so reject quotes and semicolons before anything else.
if [[ "$EMAIL" == *"'"* || "$EMAIL" == *";"* ]]; then
  echo "Refused: the email contains a quote or semicolon." >&2
  exit 2
fi
if [[ ${#EMAIL} -gt 320 || ! "$EMAIL" =~ $EMAIL_PATTERN ]]; then
  echo "Refused: the email is not a plain address." >&2
  exit 2
fi

OS_USER="$(id -un)"
if [[ ! "$OS_USER" =~ ^[A-Za-z0-9._-]+$ ]]; then
  echo "Refused: the OS user name has characters this script does not allow." >&2
  exit 2
fi

if [[ "$ENVIRONMENT" == "production" && "${TRUST_APPROVER_PRODUCTION_CONFIRMED:-}" != "yes" ]]; then
  echo "Refused: production approvers are not named. Set TRUST_APPROVER_PRODUCTION_CONFIRMED=yes only with the Director's written approval." >&2
  exit 3
fi

if [[ "$ACTION" == "grant" ]]; then
  AFTER=1
  AUDIT_ACTION="trust.approver_grant"
else
  AFTER=0
  AUDIT_ACTION="trust.approver_revoke"
fi

CONNECTION_STRING="${CONNECTION_STRING:-}"

# Set by resolve_target. TARGET_MODE is "container" or "connection".
TARGET_MODE=""
TARGET_LABEL=""
CONTAINER_SQLCMD=""
SA_PASSWORD=""
CS_FLAGS=()
CS_PASSWORD=""

resolve_target() {
  if [[ -n "$CONNECTION_STRING" ]]; then
    # Split the ADO connection string with python (no secrets on the command line).
    local -a fields
    mapfile -d '' -t fields < <(CS="$CONNECTION_STRING" python3 -I -c '
import os
parts = {}
for bit in os.environ["CS"].split(";"):
    if "=" not in bit:
        continue
    key, value = bit.split("=", 1)
    parts[key.strip().lower()] = value.strip()
def first(*keys):
    for key in keys:
        if parts.get(key):
            return parts[key]
    return ""
server = first("server", "data source", "address", "addr")
database = first("database", "initial catalog")
user = first("user id", "uid", "user")
password = first("password", "pwd")
encrypt = first("encrypt").lower() in ("true", "yes", "mandatory", "strict")
trust = first("trustservercertificate").lower() in ("true", "yes")
fields = [server, database, user, password, "1" if encrypt else "0", "1" if trust else "0"]
print("\0".join(fields), end="")
')
    local server="${fields[0]:-}" database="${fields[1]:-}" user="${fields[2]:-}"
    local password="${fields[3]:-}" encrypt="${fields[4]:-0}" trust="${fields[5]:-0}"

    if [[ -z "$server" || -z "$database" || -z "$user" || -z "$password" ]]; then
      echo "CONNECTION_STRING needs Server, Database, User Id and Password." >&2
      exit 1
    fi
    if ! command -v sqlcmd >/dev/null 2>&1; then
      echo "CONNECTION_STRING needs a host sqlcmd on PATH (go-sqlcmd or mssql-tools18)." >&2
      exit 1
    fi

    TARGET_MODE="connection"
    TARGET_LABEL="CONNECTION_STRING (server $server, database $database)"
    CS_FLAGS=(-S "$server" -d "$database" -U "$user" -b -W)
    [[ "$encrypt" == "1" ]] && CS_FLAGS+=(-N)
    [[ "$trust" == "1" ]] && CS_FLAGS+=(-C)
    CS_PASSWORD="$password"
    return
  fi

  if [[ "$(docker inspect -f '{{.State.Running}}' "$CONTAINER" 2>/dev/null || true)" != "true" ]]; then
    echo "Container $CONTAINER is not running. Start it (backend/TummlyDb/vps: docker compose up -d) and retry." >&2
    exit 1
  fi
  if [[ ! -f "$VPS_ENV" ]]; then
    echo "Missing $VPS_ENV (needs MSSQL_SA_PASSWORD)." >&2
    exit 1
  fi
  SA_PASSWORD="$(grep -E '^MSSQL_SA_PASSWORD=' "$VPS_ENV" | head -1 | cut -d= -f2-)"
  if [[ -z "$SA_PASSWORD" ]]; then
    echo "MSSQL_SA_PASSWORD is empty in $VPS_ENV." >&2
    exit 1
  fi
  CONTAINER_SQLCMD="$(find_container_sqlcmd)"
  TARGET_MODE="container"
  TARGET_LABEL="container $CONTAINER, database $LOCAL_DB"
}

find_container_sqlcmd() {
  local candidate
  for candidate in /opt/mssql-tools18/bin/sqlcmd /opt/mssql-tools/bin/sqlcmd; do
    if docker exec "$CONTAINER" test -x "$candidate" 2>/dev/null; then
      echo "$candidate"
      return
    fi
  done
  echo "sqlcmd not found in container $CONTAINER." >&2
  exit 1
}

# Runs the SQL on stdin against the resolved target. Extra arguments go to sqlcmd.
run_sql() {
  if [[ "$TARGET_MODE" == "container" ]]; then
    docker exec -i -e SQLCMDPASSWORD="$SA_PASSWORD" "$CONTAINER" "$CONTAINER_SQLCMD" \
      -S localhost -U sa -C -d "$LOCAL_DB" -b -W "$@" -i /dev/stdin
  else
    SQLCMDPASSWORD="$CS_PASSWORD" sqlcmd "${CS_FLAGS[@]}" "$@" -i /dev/stdin
  fi
}

# Build the SQL. Every value is validated above or below; only fixed words and checked names are interpolated.
build_sql() {
  cat <<SQL
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF @@SERVERNAME <> N'$PROBED_SERVER' OR DB_NAME() <> N'$PROBED_DB'
    THROW 50005, 'Target changed after the environment check. No change made.', 1;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Matches int = (SELECT COUNT(*) FROM dbo.Admins WHERE Email = N'$EMAIL');
IF @Matches <> 1
    THROW 50001, 'Expected exactly one Admin with that email. No change made.', 1;
DECLARE @AdminId int = (SELECT Id FROM dbo.Admins WHERE Email = N'$EMAIL');
DECLARE @Role nvarchar(max) = (SELECT Role FROM dbo.Admins WHERE Id = @AdminId);
DECLARE @IsActive bit = (SELECT IsActive FROM dbo.Admins WHERE Id = @AdminId);
DECLARE @IsLocked bit = (SELECT IsLocked FROM dbo.Admins WHERE Id = @AdminId);
DECLARE @Before bit = (SELECT CanApproveTrustIndicators FROM dbo.Admins WHERE Id = @AdminId);
-- A grant needs a usable approver account. THROW aborts the batch and rolls back under XACT_ABORT
-- (RAISERROR does not), so each refusal is a THROW. A revoke works on any matching row.
IF $AFTER = 1 AND @Role <> N'Admin'
    THROW 50002, 'Grant refused: the account Role is not Admin. No change made.', 1;
IF $AFTER = 1 AND @IsActive = 0
    THROW 50003, 'Grant refused: the account is not active. No change made.', 1;
IF $AFTER = 1 AND @IsLocked = 1
    THROW 50004, 'Grant refused: the account is locked. No change made.', 1;
UPDATE dbo.Admins SET CanApproveTrustIndicators = $AFTER WHERE Id = @AdminId;
INSERT INTO dbo.AdminAuditEvents
    ([Id], [OccurredAtUtc], [Action], [ActorAdminUserId], [ActorIdentity], [TargetType], [TargetId], [RestaurantId], [DetailJson], [Succeeded], [CorrelationId])
VALUES
    (NEWID(), SYSUTCDATETIME(), N'$AUDIT_ACTION', NULL, N'script:$OS_USER', N'staff_account', CAST(@AdminId AS nvarchar(32)), NULL,
     N'{"email":"$EMAIL","environment":"$DERIVED_ENVIRONMENT","before":' + CASE WHEN @Before = 1 THEN N'true' ELSE N'false' END
     + N',"after":' + CASE WHEN $AFTER = 1 THEN N'true' ELSE N'false' END + N'}',
     1, NULL);
COMMIT TRANSACTION;
SELECT Id, Email, FullName, Role, IsActive, CanApproveTrustIndicators FROM dbo.Admins WHERE Id = @AdminId;
SQL
}

resolve_target
echo "Target: $TARGET_LABEL."

# Read the target's identity first. Its SQL name and database decide the environment.
PROBE_LINE="$(printf 'SET NOCOUNT ON;\nSELECT @@SERVERNAME, DB_NAME();\n' | run_sql -h -1 -s '|' | sed -n '/[^[:space:]]/{p;q;}')" || {
  echo "Could not read the target's server name and database. No change made." >&2
  exit 1
}
IFS='|' read -r PROBED_SERVER PROBED_DB <<< "$PROBE_LINE"
PROBED_SERVER="${PROBED_SERVER//[[:space:]]/}"
PROBED_DB="${PROBED_DB//[[:space:]]/}"
if [[ ! "$PROBED_SERVER" =~ $NAME_PATTERN || ! "$PROBED_DB" =~ $NAME_PATTERN ]]; then
  echo "Refused: the target reported a server or database name this script does not allow. No change made." >&2
  exit 3
fi

DERIVED_ENVIRONMENT="$(map_environment "$PROBED_SERVER" "$PROBED_DB")"
if [[ "$DERIVED_ENVIRONMENT" == "unmapped" ]]; then
  echo "Refused: the target (server $PROBED_SERVER, database $PROBED_DB) is not a mapped environment. No change made." >&2
  exit 3
fi
if [[ "$DERIVED_ENVIRONMENT" != "$ENVIRONMENT" ]]; then
  echo "Refused: --environment $ENVIRONMENT, but the target is $DERIVED_ENVIRONMENT. No change made." >&2
  exit 3
fi

build_sql | run_sql
echo "Done: $ACTION recorded for $EMAIL on $DERIVED_ENVIRONMENT (audit row $AUDIT_ACTION)."

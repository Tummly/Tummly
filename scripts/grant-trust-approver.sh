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
#   database TUMMLY_LOCAL_DB_NAME (default TummlyDB). Only --environment local may use it.
#   CONNECTION_STRING overrides the target. It needs a host sqlcmd on PATH.
#
# Production is refused unless TRUST_APPROVER_PRODUCTION_CONFIRMED=yes is set.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VPS_ENV="$ROOT/backend/TummlyDb/vps/.env"
CONTAINER="${TUMMLY_MSSQL_CONTAINER:-tummly-mssql}"
LOCAL_DB="${TUMMLY_LOCAL_DB_NAME:-TummlyDB}"
EMAIL_PATTERN='^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$'

usage() {
  cat <<'EOF'
Usage: grant-trust-approver.sh --email EMAIL --environment local|qa|production [--revoke]

Options:
  --email EMAIL          Admin account email (required)
  --environment ENV      local | qa | production (required)
  --revoke               Clear the flag (default grants it)
  -h, --help             Show this help

Environment:
  CONNECTION_STRING                 Target database (needed for qa and production)
  TRUST_APPROVER_PRODUCTION_CONFIRMED  Must be "yes" for --environment production
  TUMMLY_MSSQL_CONTAINER            Local container name (default tummly-mssql)
  TUMMLY_LOCAL_DB_NAME              Local database name (default TummlyDB)

Exit codes: 0 changed; 1 SQL error or no change made; 2 bad usage; 3 refused by a safety gate.
EOF
}

EMAIL=""
ENVIRONMENT=""
ACTION="grant"

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

# Build the SQL. Every value is validated above; only fixed words and the checked email are interpolated.
build_sql() {
  cat <<SQL
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
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
    (NEWID(), SYSUTCDATETIME(), N'$AUDIT_ACTION', NULL, N'script:$OS_USER', N'admin', CAST(@AdminId AS nvarchar(32)), NULL,
     N'{"email":"$EMAIL","environment":"$ENVIRONMENT","before":' + CASE WHEN @Before = 1 THEN N'true' ELSE N'false' END
     + N',"after":' + CASE WHEN $AFTER = 1 THEN N'true' ELSE N'false' END + N'}',
     1, NULL);
COMMIT TRANSACTION;
SELECT Id, Email, FullName, Role, IsActive, CanApproveTrustIndicators FROM dbo.Admins WHERE Id = @AdminId;
SQL
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

run_local_container() {
  if [[ "$(docker inspect -f '{{.State.Running}}' "$CONTAINER" 2>/dev/null || true)" != "true" ]]; then
    echo "Container $CONTAINER is not running. Start it (backend/TummlyDb/vps: docker compose up -d) and retry." >&2
    exit 1
  fi
  if [[ ! -f "$VPS_ENV" ]]; then
    echo "Missing $VPS_ENV (needs MSSQL_SA_PASSWORD)." >&2
    exit 1
  fi
  local sa_password sqlcmd
  sa_password="$(grep -E '^MSSQL_SA_PASSWORD=' "$VPS_ENV" | head -1 | cut -d= -f2-)"
  if [[ -z "$sa_password" ]]; then
    echo "MSSQL_SA_PASSWORD is empty in $VPS_ENV." >&2
    exit 1
  fi
  sqlcmd="$(find_container_sqlcmd)"

  echo "Target: container $CONTAINER, database $LOCAL_DB."
  build_sql | docker exec -i -e SQLCMDPASSWORD="$sa_password" "$CONTAINER" \
    "$sqlcmd" -S localhost -U sa -C -d "$LOCAL_DB" -b -W -i /dev/stdin
}

run_connection_string() {
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

  if [[ "$ENVIRONMENT" == "local" ]]; then
    local host="${server#tcp:}"
    host="${host%%,*}"
    host="${host%%\\*}"
    case "$host" in
      localhost | 127.0.0.1 | .) ;;
      *)
        echo "Refused: --environment local but CONNECTION_STRING points at $host." >&2
        exit 3
        ;;
    esac
  fi

  if ! command -v sqlcmd >/dev/null 2>&1; then
    echo "CONNECTION_STRING needs a host sqlcmd on PATH (go-sqlcmd or mssql-tools18)." >&2
    exit 1
  fi

  local -a flags=(-S "$server" -d "$database" -U "$user" -b -W)
  [[ "$encrypt" == "1" ]] && flags+=(-N)
  [[ "$trust" == "1" ]] && flags+=(-C)

  echo "Target: CONNECTION_STRING (server $server, database $database)."
  build_sql | SQLCMDPASSWORD="$password" sqlcmd "${flags[@]}" -i /dev/stdin
}

if [[ -n "$CONNECTION_STRING" ]]; then
  run_connection_string
elif [[ "$ENVIRONMENT" == "local" ]]; then
  run_local_container
else
  echo "Refused: --environment $ENVIRONMENT needs CONNECTION_STRING. The default target is the local container only." >&2
  exit 3
fi

echo "Done: $ACTION recorded for $EMAIL on $ENVIRONMENT (audit row $AUDIT_ACTION)."

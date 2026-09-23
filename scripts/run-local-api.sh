#!/usr/bin/env bash
# Run the API against local Docker SQL (does not touch Azure QA).
# Requires: tummly-mssql up, and a branch that includes the SignalR DI fix
# (e.g. fix/signalr-publisher-scoped-permissions) until that fix is on qa.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
VPS_ENV="$ROOT/backend/TummlyDb/vps/.env"

if [[ ! -f "$VPS_ENV" ]]; then
  echo "Missing $VPS_ENV — copy from .env.example and set MSSQL_SA_PASSWORD." >&2
  exit 1
fi

# shellcheck disable=SC1090
set -a
# Only import the SA password key from the compose env file.
MSSQL_SA_PASSWORD="$(grep -E '^MSSQL_SA_PASSWORD=' "$VPS_ENV" | head -1 | cut -d= -f2-)"
set +a

if [[ -z "${MSSQL_SA_PASSWORD:-}" ]]; then
  echo "MSSQL_SA_PASSWORD is empty in $VPS_ENV" >&2
  exit 1
fi

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
export ASPNETCORE_ENVIRONMENT=Development

# Optional backend secrets (Ideal Postcodes, Twilio, ExternalAuth, etc.)
# See backend/TummlyBackend/.env.example. Connection string is set after this
# so Docker SQL always wins over any ConnectionStrings__* in .env.
BACKEND_ENV="$ROOT/backend/TummlyBackend/.env"
if [[ -f "$BACKEND_ENV" ]]; then
  set -a
  # Strip CR so Windows-checked-out .env files source cleanly on Linux/macOS.
  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line//$'\r'/}"
    [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
    [[ "$line" =~ ^[A-Za-z_][A-Za-z0-9_]*= ]] || continue
    key="${line%%=*}"
    val="${line#*=}"
    # Strip optional surrounding quotes (common when pasting secrets).
    if [[ "$val" =~ ^\".*\"$ ]]; then val="${val:1:-1}"; fi
    if [[ "$val" =~ ^\'.*\'$ ]]; then val="${val:1:-1}"; fi
    export "${key}=${val}"
  done < "$BACKEND_ENV"
  set +a
fi

# Always use local Docker SQL (do not keep Azure / quoted CS from .env).
export ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=TummlyDB;User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True;Encrypt=True;"

cd "$ROOT/backend/TummlyBackend"
exec dotnet run --launch-profile http "$@"

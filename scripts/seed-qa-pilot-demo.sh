#!/usr/bin/env bash
# Seed Location Guests + classified Feedback onto an existing Pilot restaurant
# in Azure QA SQL (or any SQL via CONNECTION_STRING override).
#
# Prerequisites:
#   - Account already signed up on Pilot (script does not create users)
#   - infra/qa/secrets.qa.env with ConnectionStrings__DefaultConnection
#     (or export CONNECTION_STRING / ConnectionStrings__DefaultConnection)
#   - Your public IP allowed on the Azure SQL firewall
#
# Usage:
#   ./scripts/seed-qa-pilot-demo.sh --email owner@example.com
#   ./scripts/seed-qa-pilot-demo.sh --email owner@example.com --dry-run
#   ./scripts/seed-qa-pilot-demo.sh --email owner@example.com --replace
#   ./scripts/seed-qa-pilot-demo.sh --email owner@example.com --force
#   ./scripts/seed-qa-pilot-demo.sh --email owner@example.com --guests 20 --feedback 36
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
QA_SECRETS="$ROOT/infra/qa/secrets.qa.env"
PROJECT="$ROOT/backend/tools/QaPilotDemoSeed/QaPilotDemoSeed.csproj"

usage() {
  cat <<'EOF'
Usage: seed-qa-pilot-demo.sh --email OWNER_EMAIL [options]

Options:
  --email EMAIL     Owner account email (required)
  --dry-run         Resolve account and print plan; no writes
  --replace         Delete prior @qa-seed.tummly.invalid rows, then reseed
  --force           Seed even when Subscription plan is not Pilot
  --guests N        Guests per location (default 20)
  --feedback N      Feedback rows per location (default 36)
  -h, --help        Show this help

Connection string:
  Prefer CONNECTION_STRING / ConnectionStrings__DefaultConnection from the
  environment. Otherwise load ConnectionStrings__DefaultConnection from
  infra/qa/secrets.qa.env.
EOF
}

FORWARD=()
while [[ $# -gt 0 ]]; do
  case "$1" in
    -h | --help)
      usage
      exit 0
      ;;
    --email | --guests | --feedback)
      [[ $# -ge 2 ]] || { echo "Missing value for $1" >&2; exit 1; }
      FORWARD+=("$1" "$2")
      shift 2
      ;;
    --dry-run | --replace | --force)
      FORWARD+=("$1")
      shift
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ ${#FORWARD[@]} -eq 0 ]]; then
  usage >&2
  exit 1
fi

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

if [[ -z "${ConnectionStrings__DefaultConnection:-}" && -z "${CONNECTION_STRING:-}" ]]; then
  export ConnectionStrings__DefaultConnection="$(read_qa_connection_string)"
elif [[ -z "${ConnectionStrings__DefaultConnection:-}" && -n "${CONNECTION_STRING:-}" ]]; then
  export ConnectionStrings__DefaultConnection="$CONNECTION_STRING"
fi

echo "==> Running QaPilotDemoSeed against configured SQL..."
dotnet run --project "$PROJECT" -c Release -- "${FORWARD[@]}"

#!/usr/bin/env bash
# Replay a missed Revolut ORDER_COMPLETED webhook into QA (or a target API).
#
# Use when Merchant shows order state=completed but Tummly still has an open
# RevolutPendingPaySession / RevolutOrderIntent and no ORDER_COMPLETED claim.
# See infra/qa/REVOLUT-QA-SANDBOX.md § Stuck or fail-closed.
#
# Usage:
#   ./scripts/replay-revolut-order-completed.sh <revolut-order-id>
#
# Env (optional):
#   WEBHOOK_URL   default: QA ACA webhook URL
#   SECRETS_FILE  default: infra/qa/secrets.qa.env (must contain
#                 Revolut__WebhookSigningSecret=…)
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ORDER_ID="${1:-}"
if [[ -z "${ORDER_ID}" ]]; then
  echo "Usage: $0 <revolut-order-id>" >&2
  exit 2
fi

WEBHOOK_URL="${WEBHOOK_URL:-https://ca-tummly-qa-api.agreeablewater-62e50314.uksouth.azurecontainerapps.io/api/webhooks/revolut}"
SECRETS_FILE="${SECRETS_FILE:-${ROOT}/infra/qa/secrets.qa.env}"

if [[ ! -f "${SECRETS_FILE}" ]]; then
  echo "Missing secrets file: ${SECRETS_FILE}" >&2
  exit 1
fi

SIGNING_SECRET="$(
  python3 - <<PY
from pathlib import Path
path = Path("${SECRETS_FILE}")
for line in path.read_text().splitlines():
    if line.startswith("Revolut__WebhookSigningSecret="):
        print(line.split("=", 1)[1].strip().strip('"').strip("'"), end="")
        break
else:
    raise SystemExit("Revolut__WebhookSigningSecret missing in secrets file")
PY
)"

echo "POST ORDER_COMPLETED for ${ORDER_ID}"
echo "URL ${WEBHOOK_URL}"

HTTP_CODE="$(
  python3 - <<PY
import hmac, hashlib, json, time, urllib.request, urllib.error, os, sys

secret = """${SIGNING_SECRET}"""
order_id = """${ORDER_ID}"""
url = """${WEBHOOK_URL}"""
body = json.dumps(
    {"event": "ORDER_COMPLETED", "order_id": order_id},
    separators=(",", ":"),
)
ts = str(int(time.time()))
payload = f"v1.{ts}.{body}".encode()
sig = hmac.new(secret.encode(), payload, hashlib.sha256).hexdigest()
req = urllib.request.Request(
    url,
    data=body.encode(),
    method="POST",
    headers={
        "Content-Type": "application/json",
        "Revolut-Request-Timestamp": ts,
        "Revolut-Signature": f"v1={sig}",
    },
)
try:
    with urllib.request.urlopen(req, timeout=90) as resp:
        print(resp.status)
        sys.stdout.flush()
except urllib.error.HTTPError as e:
    print(e.code, file=sys.stderr)
    err = e.read()[:500].decode(errors="replace")
    if err:
        print(err, file=sys.stderr)
    sys.exit(1)
PY
)"

echo "HTTP ${HTTP_CODE}"
if [[ "${HTTP_CODE}" == "204" ]]; then
  echo "Verdict: accepted (or replay). Refresh Billing & credits; expect plan / credits / TM invoice."
  exit 0
fi

echo "Verdict: unexpected HTTP ${HTTP_CODE}" >&2
exit 1

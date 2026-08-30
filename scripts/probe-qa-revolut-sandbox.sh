#!/usr/bin/env bash
# Probe QA API Revolut Sandbox readiness (non-secret /health/revolut).
# See infra/qa/REVOLUT-QA-SANDBOX.md.
set -euo pipefail

API_BASE="${API_BASE:-https://api.qa.tummly.com}"
API_BASE="${API_BASE%/}"
URL="${API_BASE}/health/revolut"

echo "GET ${URL}"
body="$(curl -sS --max-time 20 "${URL}" || true)"
if [[ -z "${body}" ]]; then
  echo "Verdict: no response (deploy / DNS / stuck revision?)."
  echo "Also try: ./scripts/probe-qa-api-revision.sh"
  exit 1
fi

echo "${body}"
echo

parse_json() {
  python3 -c '
import json, sys
d = json.loads(sys.stdin.read())
def out(k, v):
    if v is None:
        print(f"{k}=")
    elif isinstance(v, bool):
        print(f"{k}={'true' if v else 'false'}")
    else:
        print(f"{k}={v}")
out("status", d.get("status"))
out("host", d.get("hostMode"))
out("require_sandbox", d.get("requireSandboxHost"))
out("blocked", d.get("createBlockedCode"))
out("vars", d.get("planVariationsConfigured"))
out("expected", d.get("planVariationsExpected"))
'
}

eval "$(printf '%s' "${body}" | parse_json)"

blocked_ok=0
if [[ -z "${blocked}" || "${blocked}" == "null" ]]; then
  blocked_ok=1
fi

if [[ "${status}" == "ready" && "${host}" == "sandbox" && "${blocked_ok}" -eq 1 ]]; then
  echo "Verdict: Sandbox Merchant create gate is ready for QA test-card flows."
  exit 0
fi

echo "Verdict: not ready for QA sandbox pay rehearsal."
echo "  hostMode=${host} (want sandbox)"
echo "  requireSandboxHost=${require_sandbox} (want true on QA)"
echo "  planVariationsConfigured=${vars}/${expected}"
echo "  createBlockedCode=${blocked:-null}"
echo "Next: infra/qa/REVOLUT-QA-SANDBOX.md (Sandbox secret, webhook, eight variations, VAT)."
exit 1

#!/usr/bin/env bash
# Probe QA (or another) API for a stuck revision: new routes 404 while old routes
# still answer. See docs/agents/stuck-revision.md and ADR 0015.
set -euo pipefail

API_BASE="${API_BASE:-https://api.qa.tummly.com}"
API_BASE="${API_BASE%/}"

# Known-old routes that exist on pre–Team-permissions revisions.
OLD_PATHS=(
  /api/auth/me
  /api/restaurant/locations
)

# Known-new routes that only exist after Team & permissions / Account workspace cutovers.
NEW_PATHS=(
  /api/team-permissions
  /api/account-workspace
  /api/team-invitations/preview
)

code_for() {
  local path="$1"
  curl -sS -o /dev/null -w '%{http_code}' --max-time 15 "${API_BASE}${path}" || echo "000"
}

echo "API_BASE=${API_BASE}"
echo

old_ok=0
for path in "${OLD_PATHS[@]}"; do
  code="$(code_for "$path")"
  echo "old  ${path} -> ${code}"
  if [[ "$code" == "401" || "$code" == "200" ]]; then
    old_ok=1
  fi
done

echo

new_404=0
new_live=0
for path in "${NEW_PATHS[@]}"; do
  code="$(code_for "$path")"
  echo "new  ${path} -> ${code}"
  if [[ "$code" == "404" ]]; then
    new_404=1
  fi
  if [[ "$code" == "401" || "$code" == "200" ]]; then
    new_live=1
  fi
done

echo

if [[ "$old_ok" -eq 1 && "$new_404" -eq 1 && "$new_live" -eq 0 ]]; then
  echo "Verdict: stuck revision likely (old routes live, new routes empty 404)."
  echo "Next: compare latestReadyRevisionName vs latestRevisionName; read migrate logs."
elif [[ "$new_live" -eq 1 ]]; then
  echo "Verdict: new routes are on the live process (401/200)."
else
  echo "Verdict: inconclusive (API down, wrong base, or unexpected status codes)."
fi

if command -v az >/dev/null 2>&1; then
  RG="${AZ_RESOURCE_GROUP:-rg-tummly-qa}"
  NAME="${AZ_CONTAINER_APP:-ca-tummly-qa-api}"
  if az account show >/dev/null 2>&1; then
    echo
    echo "Azure Container App ${NAME} (${RG}):"
    az containerapp show \
      --name "$NAME" \
      --resource-group "$RG" \
      --query "{latestRevision:properties.latestRevisionName,latestReady:properties.latestReadyRevisionName,fqdn:properties.configuration.ingress.fqdn}" \
      -o json 2>/dev/null || echo "(az show failed — check login and names)"
  else
    echo
    echo "(az logged out — skip Ready vs Latest)"
  fi
else
  echo
  echo "(az not installed — skip Ready vs Latest)"
fi

#!/usr/bin/env bash
# Apply infra/qa/secrets.qa.env to ca-tummly-qa-api (bash twin of apply-aca-secrets.ps1).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="${1:-$SCRIPT_DIR/secrets.qa.env}"
RESOURCE_GROUP="${RESOURCE_GROUP:-rg-tummly-qa}"
CONTAINER_APP="${CONTAINER_APP:-ca-tummly-qa-api}"
API_MI_CLIENT_ID="${API_MI_CLIENT_ID:-55d29a4d-e60d-44ab-9aab-9bb69ee0ab08}"

if ! command -v az >/dev/null 2>&1; then
  echo "Azure CLI (az) is required. Install it, then: az login" >&2
  exit 1
fi

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing $ENV_FILE — copy secrets.qa.env.example and fill values." >&2
  exit 1
fi

pairs=()
pairs+=("ASPNETCORE_URLS=http://+:8080")
pairs+=("AZURE_CLIENT_ID=$API_MI_CLIENT_ID")

while IFS= read -r line || [[ -n "$line" ]]; do
  line="${line#"${line%%[![:space:]]*}"}"
  line="${line%"${line##*[![:space:]]}"}"
  [[ -z "$line" || "$line" == \#* ]] && continue
  [[ "$line" != *=* ]] && continue
  key="${line%%=*}"
  val="${line#*=}"
  key="${key%"${key##*[![:space:]]}"}"
  [[ "$key" == "ASPNETCORE_URLS" || "$key" == "AZURE_CLIENT_ID" ]] && continue
  if [[ -z "${val// }" || "$val" == REPLACE* ]]; then
    echo "Skipping unset/placeholder: $key" >&2
    continue
  fi
  pairs+=("$key=$val")
done <"$ENV_FILE"

echo "Applying ${#pairs[@]} env vars to $CONTAINER_APP ..."
az containerapp update \
  --name "$CONTAINER_APP" \
  --resource-group "$RESOURCE_GROUP" \
  --set-env-vars "${pairs[@]}" \
  --query "{image:properties.template.containers[0].image, latestReady:properties.latestReadyRevisionName, runningStatus:properties.runningStatus}" \
  --output json

echo
echo "Done. Probe: https://ca-tummly-qa-api.agreeablewater-62e50314.uksouth.azurecontainerapps.io/health/ready"

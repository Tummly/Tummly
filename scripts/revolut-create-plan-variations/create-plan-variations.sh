#!/usr/bin/env bash
# Create (or dry-run) the eight recurring Revolut plan variations from the
# pack pricebook. See README.md in this directory.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="$ROOT/backend/tools/RevolutPlanVariationCatalogCli/RevolutPlanVariationCatalogCli.csproj"

exec dotnet run --project "$PROJECT" -- "$@"

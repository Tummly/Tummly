#!/usr/bin/env bash
# ADR-0015: reconcile migration filenames with `dotnet ef migrations list --no-connect`.
set -euo pipefail

MIG_DIR="${1:-Migrations}"
PROJECT="${2:-TummlyBackend.csproj}"

if [[ ! -d "$MIG_DIR" ]]; then
  echo "::error::Migrations directory not found: $MIG_DIR"
  exit 1
fi

list_out=$(dotnet ef migrations list --no-connect --project "$PROJECT" --startup-project "$PROJECT")
echo "$list_out"

missing=0
shopt -s nullglob
for f in "$MIG_DIR"/*.cs; do
  base=$(basename "$f")
  case "$base" in
    *ModelSnapshot.cs|*Designer.cs) continue ;;
  esac
  id="${base%.cs}"
  if ! grep -Fq "$id" <<<"$list_out"; then
    echo "::error::Migration file $base is not discoverable by EF (missing from migrations list). Often a missing Designer / [Migration] attribute."
    missing=1
  fi
done

if [[ "$missing" -ne 0 ]]; then
  echo "EF migrations list ↔ files check failed."
  exit 1
fi

echo "EF migrations list ↔ files OK."

#!/usr/bin/env bash
# ADR-0015: fail if any EF migration class lacks a Designer (or vice versa).
set -euo pipefail

MIG_DIR="${1:-Migrations}"

if [[ ! -d "$MIG_DIR" ]]; then
  echo "::error::Migrations directory not found: $MIG_DIR"
  exit 1
fi

missing=0

shopt -s nullglob
for f in "$MIG_DIR"/*.cs; do
  base=$(basename "$f")
  case "$base" in
    *ModelSnapshot.cs|*Designer.cs) continue ;;
  esac
  designer="$MIG_DIR/${base%.cs}.Designer.cs"
  if [[ ! -f "$designer" ]]; then
    echo "::error::Missing Designer for $base"
    missing=1
  fi
done

for d in "$MIG_DIR"/*.Designer.cs; do
  main="$MIG_DIR/$(basename "$d" .Designer.cs).cs"
  if [[ ! -f "$main" ]]; then
    echo "::error::Missing migration class for $(basename "$d")"
    missing=1
  fi
done

if [[ "$missing" -ne 0 ]]; then
  echo "EF migration ↔ Designer pairing check failed."
  exit 1
fi

echo "EF migration ↔ Designer pairing OK."

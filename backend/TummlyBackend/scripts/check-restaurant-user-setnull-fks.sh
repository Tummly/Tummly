#!/usr/bin/env bash
# Fail when a migration Up (non-Designer) adds a Restaurants → Users FK with
# ON DELETE SET NULL. SQL Server error 1785 (multiple cascade paths) when
# OwnerUserId already Cascades. See CODING_STANDARDS.md EF migrations.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
MIG_DIR="${1:-$PROJECT_DIR/Migrations}"

if [[ ! -d "$MIG_DIR" ]]; then
  echo "Migrations directory not found: $MIG_DIR" >&2
  exit 1
fi

failed=0
while IFS= read -r -d '' file; do
  base="$(basename "$file")"
  case "$base" in
    *Designer.cs|ApplicationDbContextModelSnapshot.cs) continue ;;
  esac

  # Match AddForeignKey blocks that target Restaurants → Users with SetNull.
  if awk '
    BEGIN { in_fk = 0; table = ""; principals = ""; delete_action = "" }
    /AddForeignKey\(/ { in_fk = 1; table = ""; principals = ""; delete_action = "" }
    in_fk && /table: "Restaurants"/ { table = "Restaurants" }
    in_fk && /principalTable: "Users"/ { principals = "Users" }
    in_fk && /ReferentialAction\.SetNull/ { delete_action = "SetNull" }
    in_fk && /\);/ {
      if (table == "Restaurants" && principals == "Users" && delete_action == "SetNull") {
        exit 2
      }
      in_fk = 0
    }
  ' "$file"; then
    :
  else
    status=$?
    if [[ "$status" -eq 2 ]]; then
      echo "Error: $base adds Restaurants → Users FK with ON DELETE SET NULL (SQL Server 1785 risk)." >&2
      failed=1
    else
      echo "Error: awk failed on $file (exit $status)" >&2
      exit 1
    fi
  fi
done < <(find "$MIG_DIR" -maxdepth 1 -type f -name '*.cs' -print0 | sort -z)

if [[ "$failed" -ne 0 ]]; then
  echo "Restaurant → Users SET NULL FK check failed. Prefer ReferentialAction.NoAction and Update ApplicationDbContext the same way." >&2
  exit 1
fi

echo "Restaurant → Users SET NULL FK check passed."

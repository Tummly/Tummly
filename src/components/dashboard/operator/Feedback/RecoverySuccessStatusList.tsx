import { Badge } from "@/components/ui/badge"
import type { RecoverySuccessStatusRow } from "@/lib/operatorFeedback/recoverySuccessPresentation"

type RecoverySuccessStatusListProps = {
  rows: readonly RecoverySuccessStatusRow[]
}

/** Figma Success key/value status rows (U-07). */
export function RecoverySuccessStatusList({
  rows,
}: RecoverySuccessStatusListProps) {
  return (
    <dl className="flex w-full max-w-[600px] flex-col gap-6">
      {rows.map((row) => (
        <div
          key={row.label}
          className="flex w-full items-center justify-between gap-4"
        >
          <dt className="shrink-0 text-base font-semibold text-op-text-muted">
            {row.label}
          </dt>
          <dd className="min-w-0 text-right text-sm font-medium text-op-text-primary">
            {row.valueKind === "badge" ? (
              <Badge variant="tag">{row.value}</Badge>
            ) : (
              row.value
            )}
          </dd>
        </div>
      ))}
    </dl>
  )
}

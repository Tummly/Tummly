import { Badge } from "@/components/ui/badge"
import { GUESTS_TABLE_LOCATION_CLASS } from "@/lib/operatorGuests/guestsPresentation"

export function GuestProfileIssueTagsCell({
  labels,
}: {
  labels: string[] | null
}) {
  if (labels == null || labels.length === 0) {
    return <span className={GUESTS_TABLE_LOCATION_CLASS}>—</span>
  }

  return (
    <div className="flex flex-wrap gap-1.5">
      {labels.map((label) => (
        <Badge key={label} variant="tag">
          {label}
        </Badge>
      ))}
    </div>
  )
}

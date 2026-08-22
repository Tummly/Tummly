import type { FilterChip } from "@/lib/operatorFilterSheet"

import { GuestsRemovableChip } from "./GuestsRemovableChip"

type GuestsFilterChipRowProps = {
  chips: readonly FilterChip[]
  onRemoveChip: (chip: FilterChip) => void
  /** Optional session chip shown before sheet filter chips (not counted in Filters n). */
  leadingChip?: {
    label: string
    onRemove: () => void
  } | null
}

export function GuestsFilterChipRow({
  chips,
  onRemoveChip,
  leadingChip = null,
}: GuestsFilterChipRowProps) {
  if (leadingChip == null && chips.length === 0) {
    return null
  }

  return (
    <div className="flex flex-wrap gap-2" aria-label="Applied filters">
      {leadingChip != null ? (
        <GuestsRemovableChip
          label={leadingChip.label}
          removeLabel={`Remove ${leadingChip.label}`}
          onRemove={leadingChip.onRemove}
        />
      ) : null}
      {chips.map((chip) => (
        <GuestsRemovableChip
          key={chip.id}
          label={chip.label}
          removeLabel={`Remove ${chip.label}`}
          onRemove={() => onRemoveChip(chip)}
        />
      ))}
    </div>
  )
}

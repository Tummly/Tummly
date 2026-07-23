import type { FilterChip } from "@/lib/operatorFilterSheet"

import { GuestsRemovableChip } from "./GuestsRemovableChip"

type GuestsFilterChipRowProps = {
  chips: readonly FilterChip[]
  onRemoveChip: (chip: FilterChip) => void
}

export function GuestsFilterChipRow({
  chips,
  onRemoveChip,
}: GuestsFilterChipRowProps) {
  if (chips.length === 0) {
    return null
  }

  return (
    <div className="flex flex-wrap gap-2" aria-label="Applied filters">
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

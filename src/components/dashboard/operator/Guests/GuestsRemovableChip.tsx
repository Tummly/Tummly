import { XIcon } from "lucide-react"
import type { ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"

type GuestsRemovableChipProps = {
  label: ReactNode
  removeLabel: string
  onRemove: () => void
}

/** Removable tag Badge used by Filters chips and Add Tag pending chips. */
export function GuestsRemovableChip({
  label,
  removeLabel,
  onRemove,
}: GuestsRemovableChipProps) {
  return (
    <Badge variant="tag" className="gap-2.5 px-3 py-2">
      {label}
      <Button
        type="button"
        variant="ghost"
        className="inline-flex size-3.5 shrink-0 items-center justify-center p-0 opacity-70 hover:bg-transparent hover:opacity-100"
        aria-label={removeLabel}
        onClick={onRemove}
      >
        <XIcon className="size-3.5" aria-hidden />
      </Button>
    </Badge>
  )
}

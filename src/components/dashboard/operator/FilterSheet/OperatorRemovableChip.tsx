import { XIcon } from "lucide-react"
import type { ReactNode } from "react"

import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"

type OperatorRemovableChipProps = {
  label: ReactNode
  removeLabel: string
  onRemove: () => void
}

/** Removable tag Badge used by Operator filter chips and related tag UIs. */
export function OperatorRemovableChip({
  label,
  removeLabel,
  onRemove,
}: OperatorRemovableChipProps) {
  return (
    <Badge variant="tag" className="gap-2.5 px-3 py-2 leading-normal">
      {label}
      <Button
        type="button"
        variant="ghost"
        className="inline-flex size-3.5 min-h-0 min-w-0 shrink-0 items-center justify-center rounded-none border-0 p-0 text-inherit hover:bg-transparent hover:text-inherit"
        aria-label={removeLabel}
        onClick={onRemove}
      >
        <XIcon className="size-3.5" strokeWidth={1.5} aria-hidden />
      </Button>
    </Badge>
  )
}

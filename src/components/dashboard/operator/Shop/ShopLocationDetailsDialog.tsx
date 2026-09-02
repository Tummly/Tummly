import { useState, useEffect } from "react"
import { Check, ChevronDown } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { cn } from "@/lib/utils"

export type LocationDetails = {
  tableCount: number
  counterCount?: number
  entranceCount?: number
  secondaryEntranceCount?: number
  takeawayVolume?: string
  promptLocations?: string
  existingMaterials?: string
}

/** Persist 0 as "0"; only blank when the field was never set. */
function countToInput(value: number | undefined): string {
  return typeof value === "number" ? String(value) : ""
}

type ShopLocationDetailsDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSaveDetails: (details: LocationDetails) => Promise<void>
  initialDetails?: LocationDetails | null
  locationName: string
}

const TAKEAWAY_OPTIONS = [
  { value: "fewer-than-100", label: "Fewer than 100" },
  { value: "100-249", label: "100–249" },
  { value: "250-499", label: "250–499" },
  { value: "500-999", label: "500–999" },
  { value: "1000-plus", label: "1,000 or more" },
  { value: "not-sure", label: "Not sure" },
]

export const SHOP_PROMPT_OPTIONS = [
  { id: "tables", label: "Dine-in tables" },
  { id: "counters", label: "Ordering or payment counter" },
  { id: "collection", label: "Collection point" },
  { id: "packaging", label: "Takeaway packaging" },
  { id: "delivery", label: "Delivery orders" },
  { id: "receipts", label: "Printed receipts" },
  { id: "windows", label: "Entrance or window" },
] as const

export const EXISTING_MATERIALS_OPTIONS = [
  { value: "no", label: "No, this is the first order" },
  { value: "yes", label: "Yes, materials are already in use" },
  { value: "not-sure", label: "Not sure" },
] as const

export function ShopLocationDetailsDialog({
  open,
  onOpenChange,
  onSaveDetails,
  initialDetails,
  locationName,
}: ShopLocationDetailsDialogProps) {
  const [tableCount, setTableCount] = useState<string>(
    countToInput(initialDetails?.tableCount)
  )
  const [counterCount, setCounterCount] = useState<string>(
    countToInput(initialDetails?.counterCount)
  )
  const [entranceCount, setEntranceCount] = useState<string>(
    countToInput(initialDetails?.entranceCount)
  )
  const [secondaryEntranceCount, setSecondaryEntranceCount] = useState<string>(
    countToInput(initialDetails?.secondaryEntranceCount)
  )
  const [takeawayVolume, setTakeawayVolume] = useState<string>(
    initialDetails?.takeawayVolume ?? ""
  )
  const [selectedPrompts, setSelectedPrompts] = useState<string[]>(() => {
    if (!initialDetails?.promptLocations) return []
    return initialDetails.promptLocations.split(",").filter(Boolean)
  })
  const [existingMaterials, setExistingMaterials] = useState<string>(
    initialDetails?.existingMaterials ?? ""
  )
  const [isPromptsOpen, setIsPromptsOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    if (open) {
      setTableCount(countToInput(initialDetails?.tableCount))
      setCounterCount(countToInput(initialDetails?.counterCount))
      setEntranceCount(countToInput(initialDetails?.entranceCount))
      setSecondaryEntranceCount(
        countToInput(initialDetails?.secondaryEntranceCount)
      )
      setTakeawayVolume(initialDetails?.takeawayVolume ?? "")
      setSelectedPrompts(
        initialDetails?.promptLocations
          ? initialDetails.promptLocations.split(",").filter(Boolean)
          : []
      )
      setExistingMaterials(initialDetails?.existingMaterials ?? "")
    }
  }, [open, initialDetails])

  const handleTogglePrompt = (id: string) => {
    setSelectedPrompts((prev) =>
      prev.includes(id) ? prev.filter((p) => p !== id) : [...prev, id]
    )
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const parsedTableCount = parseInt(tableCount, 10) || 0
    const parsedCounterCount = parseInt(counterCount, 10) || 0
    const parsedEntranceCount = parseInt(entranceCount, 10) || 0
    const parsedSecondaryEntrance = parseInt(secondaryEntranceCount, 10) || 0

    setIsSaving(true)
    try {
      await onSaveDetails({
        tableCount: parsedTableCount,
        counterCount: parsedCounterCount,
        entranceCount: parsedEntranceCount,
        secondaryEntranceCount: parsedSecondaryEntrance,
        takeawayVolume: takeawayVolume || "not-sure",
        promptLocations: selectedPrompts.join(","),
        existingMaterials: existingMaterials || "no",
      })
      onOpenChange(false)
    } finally {
      setIsSaving(false)
    }
  }

  const promptTriggerLabel = () => {
    if (selectedPrompts.length === 0) return null
    const labels = selectedPrompts
      .map((id) => SHOP_PROMPT_OPTIONS.find((o) => o.id === id)?.label)
      .filter(Boolean)
    if (labels.length <= 2) return labels.join(", ")
    return `${labels.slice(0, 2).join(", ")} (+${labels.length - 2})`
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className="z-[120] max-h-[95vh] w-full overflow-y-auto rounded-[6px] border border-op-border-default bg-op-card-background p-6 sm:max-w-4xl sm:p-8 lg:max-w-[980px] [scrollbar-width:none] [-ms-overflow-style:none] [&::-webkit-scrollbar]:hidden"
      >
        <form onSubmit={handleSubmit} className="relative">
          <button
            type="button"
            className="absolute right-0 top-0 flex size-8 items-center justify-center rounded-[4px] bg-op-surface-secondary text-muted-foreground transition-colors hover:bg-op-action-secondary-hover hover:text-foreground focus-visible:outline-none"
            aria-label="Close"
            onClick={() => onOpenChange(false)}
          >
            <span className="text-base font-semibold leading-none">✕</span>
          </button>

          <DialogHeader className="gap-1.5 pr-12 text-left">
            <DialogTitle className="text-xl font-bold tracking-tight text-foreground sm:text-2xl">
              Tell us about {locationName}
            </DialogTitle>
            <DialogDescription className="text-xs text-muted-foreground">
              These details help Tummly recommend practical materials and quantities. You can update them at any time.
            </DialogDescription>
          </DialogHeader>

          <div className="mt-8 grid grid-cols-1 gap-x-8 gap-y-6 md:grid-cols-2">
            {/* Field 1: Table count */}
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="tableCount"
                className="text-xs font-semibold text-foreground"
              >
                How many guest tables are there?
              </Label>
              <Input
                id="tableCount"
                type="number"
                min="0"
                placeholder="Enter number of tables"
                value={tableCount}
                onChange={(e) => setTableCount(e.target.value)}
                className="h-11 w-full rounded-[4px] border border-op-border-default bg-op-background-primary px-4 text-xs text-foreground placeholder:text-muted-foreground focus-visible:ring-1 focus-visible:ring-op-action-primary"
              />
              <p className="text-[11px] text-muted-foreground">
                Include tables that are normally available for dine-in guests.
              </p>
            </div>

            {/* Field 2: Counter count */}
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="counterCount"
                className="text-xs font-semibold text-foreground"
              >
                How many service or collection counters are there?
              </Label>
              <Input
                id="counterCount"
                type="number"
                min="0"
                placeholder="Enter number of counters"
                value={counterCount}
                onChange={(e) => setCounterCount(e.target.value)}
                className="h-11 w-full rounded-[4px] border border-op-border-default bg-op-background-primary px-4 text-xs text-foreground placeholder:text-muted-foreground focus-visible:ring-1 focus-visible:ring-op-action-primary"
              />
              <p className="text-[11px] text-muted-foreground">
                Include ordering, payment and collection counters where a QR card could be displayed.
              </p>
            </div>

            {/* Field 3: Entrance count */}
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="entranceCount"
                className="text-xs font-semibold text-foreground"
              >
                How many guest-facing entrances or windows could display a sticker?
              </Label>
              <Input
                id="entranceCount"
                type="number"
                min="0"
                placeholder="Enter number of entrances or windows"
                value={entranceCount}
                onChange={(e) => setEntranceCount(e.target.value)}
                className="h-11 w-full rounded-[4px] border border-op-border-default bg-op-background-primary px-4 text-xs text-foreground placeholder:text-muted-foreground focus-visible:ring-1 focus-visible:ring-op-action-primary"
              />
            </div>

            {/* Field 4: Secondary entrance count */}
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="secondaryEntranceCount"
                className="text-xs font-semibold text-foreground"
              >
                How many guest-facing entrances or windows could display a sticker?
              </Label>
              <Input
                id="secondaryEntranceCount"
                type="number"
                min="0"
                placeholder="Enter number of entrances or windows"
                value={secondaryEntranceCount}
                onChange={(e) => setSecondaryEntranceCount(e.target.value)}
                className="h-11 w-full rounded-[4px] border border-op-border-default bg-op-background-primary px-4 text-xs text-foreground placeholder:text-muted-foreground focus-visible:ring-1 focus-visible:ring-op-action-primary"
              />
              <p className="text-[11px] text-muted-foreground">
                Include ordering, payment and collection counters where a QR card could be displayed.
              </p>
            </div>

            {/* Field 5: Takeaway orders */}
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="takeawayVolume"
                className="text-xs font-semibold text-foreground"
              >
                How many takeaway or delivery orders does this location complete each week?
              </Label>
              <Select
                value={takeawayVolume}
                onValueChange={setTakeawayVolume}
              >
                <SelectTrigger
                  id="takeawayVolume"
                  className="h-11 w-full rounded-[4px] border border-op-border-default bg-op-background-primary px-4 text-xs text-foreground data-placeholder:text-muted-foreground"
                >
                  <SelectValue placeholder="Select" />
                </SelectTrigger>
                <SelectContent className="z-[130] rounded-[4px] border border-op-border-default bg-op-card-background text-xs text-foreground">
                  {TAKEAWAY_OPTIONS.map((opt) => (
                    <SelectItem key={opt.value} value={opt.value}>
                      {opt.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Field 6: Prompt touchpoints (Multi-select) */}
            <div className="flex flex-col gap-2">
              <Label
                htmlFor="promptLocations"
                className="text-xs font-semibold text-foreground"
              >
                Where should guests see a QR prompt?
              </Label>
              <Popover open={isPromptsOpen} onOpenChange={setIsPromptsOpen}>
                <PopoverTrigger asChild>
                  <button
                    type="button"
                    id="promptLocations"
                    className="flex h-11 w-full items-center justify-between rounded-[4px] border border-op-border-default bg-op-background-primary px-4 text-left text-xs text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-op-action-primary"
                  >
                    <span
                      className={cn(
                        "truncate",
                        !promptTriggerLabel() && "text-muted-foreground"
                      )}
                    >
                      {promptTriggerLabel() ?? "Select all that apply"}
                    </span>
                    <ChevronDown className="size-4 shrink-0 text-muted-foreground" />
                  </button>
                </PopoverTrigger>
                <PopoverContent
                  align="start"
                  className="z-[140] w-(--radix-popover-trigger-width) min-w-[280px] rounded-[4px] border border-op-border-default bg-op-card-background p-1.5 text-xs text-foreground shadow-xl"
                >
                  <div className="flex flex-col gap-1">
                    {SHOP_PROMPT_OPTIONS.map((opt) => {
                      const isChecked = selectedPrompts.includes(opt.id)
                      return (
                        <button
                          key={opt.id}
                          type="button"
                          onClick={() => handleTogglePrompt(opt.id)}
                          className="flex w-full items-center gap-2.5 rounded-[4px] px-2.5 py-2 text-left text-xs text-foreground transition-colors hover:bg-op-surface-secondary"
                        >
                          <div
                            className={cn(
                              "flex size-4 shrink-0 items-center justify-center rounded-[3px] border border-op-border-default transition-colors",
                              isChecked
                                ? "border-op-action-primary bg-op-action-primary text-white"
                                : "bg-op-background-primary"
                            )}
                          >
                            {isChecked && <Check className="size-3" />}
                          </div>
                          <span className="flex-1">{opt.label}</span>
                        </button>
                      )
                    })}
                  </div>
                </PopoverContent>
              </Popover>
            </div>

            {/* Field 7: Existing materials (full width) */}
            <div className="flex flex-col gap-2 md:col-span-2">
              <Label
                htmlFor="existingMaterials"
                className="text-xs font-semibold text-foreground"
              >
                Does this location already use any Tummly QR materials?
              </Label>
              <Select
                value={existingMaterials}
                onValueChange={setExistingMaterials}
              >
                <SelectTrigger
                  id="existingMaterials"
                  className="h-11 w-full rounded-[4px] border border-op-border-default bg-op-background-primary px-4 text-xs text-foreground data-placeholder:text-muted-foreground"
                >
                  <SelectValue placeholder="Select" />
                </SelectTrigger>
                <SelectContent className="z-[130] rounded-[4px] border border-op-border-default bg-op-card-background text-xs text-foreground">
                  {EXISTING_MATERIALS_OPTIONS.map((opt) => (
                    <SelectItem key={opt.value} value={opt.value}>
                      {opt.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {existingMaterials === "yes" && (
                <p className="text-[11px] text-muted-foreground">
                  Tummly will use active placement and previous-order information when preparing the recommendation.
                </p>
              )}
            </div>
          </div>

          {/* Action buttons aligned to bottom-left */}
          <div className="mt-8 flex items-center justify-start gap-3">
            <Button
              type="submit"
              variant="op-primary"
              disabled={isSaving}
              className="h-10 rounded-[4px] bg-op-action-primary px-5 text-xs font-semibold text-white hover:opacity-90"
            >
              {isSaving ? "Saving…" : "Generate recommendation"}
            </Button>
            <Button
              type="button"
              variant="op-secondary"
              className="h-10 rounded-[4px] bg-op-surface-secondary px-5 text-xs font-medium text-foreground hover:bg-op-action-secondary-hover"
              onClick={() => onOpenChange(false)}
            >
              Cancel
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}


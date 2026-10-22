import { FileUp, Plus } from "lucide-react"

import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

type SignupLocationsActionCardsProps = {
  disabled?: boolean
  onAddIndividually: () => void
  onImportCsv: () => void
  className?: string
}

export function SignupLocationsActionCards({
  disabled = false,
  onAddIndividually,
  onImportCsv,
  className,
}: SignupLocationsActionCardsProps) {
  return (
    <div
      className={cn(
        "grid w-full grid-cols-1 gap-4 sm:grid-cols-2",
        className
      )}
    >
      <Button
        type="button"
        variant="outline"
        disabled={disabled}
        onClick={onAddIndividually}
        className="h-auto min-h-0 w-full flex-col items-start gap-3 rounded-[8px] border-[#e0e0e0] bg-white px-5 py-5 text-left shadow-none hover:bg-[#f7f7f7]"
      >
        <span className="flex size-10 items-center justify-center rounded-[8px] bg-[#e7f7ec] text-[#14a74a]">
          <Plus className="size-5" aria-hidden />
        </span>
        <span className="flex flex-col gap-1">
          <span className="font-sans text-base font-semibold text-[#141414]">
            Add individually
          </span>
          <span className="font-sans text-sm font-normal leading-5 text-[#5c5c5c]">
            Enter each restaurant&apos;s name and address.
          </span>
        </span>
      </Button>

      <Button
        type="button"
        variant="outline"
        disabled={disabled}
        onClick={onImportCsv}
        className="h-auto min-h-0 w-full flex-col items-start gap-3 rounded-[8px] border-[#e0e0e0] bg-white px-5 py-5 text-left shadow-none hover:bg-[#f7f7f7]"
      >
        <span className="flex size-10 items-center justify-center rounded-[8px] bg-[#e7f7ec] text-[#14a74a]">
          <FileUp className="size-5" aria-hidden />
        </span>
        <span className="flex flex-col gap-1">
          <span className="font-sans text-base font-semibold text-[#141414]">
            Import Locations
          </span>
          <span className="font-sans text-sm font-normal leading-5 text-[#5c5c5c]">
            Upload a CSV file to add multiple restaurants.
          </span>
        </span>
      </Button>
    </div>
  )
}

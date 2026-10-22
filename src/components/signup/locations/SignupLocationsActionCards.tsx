import type { LucideIcon } from "lucide-react"
import { Download, MapPinPlusInside } from "lucide-react"

import { cn } from "@/lib/utils"

type SignupLocationsActionCardsProps = {
  disabled?: boolean
  /** `stacked` before any Location exists; `row` once the list shows. */
  layout?: "stacked" | "row"
  onAddIndividually: () => void
  onImportCsv: () => void
  className?: string
}

type ActionCardProps = {
  icon: LucideIcon
  title: string
  description: string
  tone: "outline" | "accent"
  disabled: boolean
  onClick: () => void
}

function ActionCard({
  icon: Icon,
  title,
  description,
  tone,
  disabled,
  onClick,
}: ActionCardProps) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={onClick}
      className={cn(
        "flex h-[154px] min-w-0 flex-1 cursor-pointer flex-col items-center justify-center gap-[22px] rounded-[6px] px-4 py-6 text-center text-black outline-none transition-colors focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50",
        tone === "outline"
          ? "border border-[#cacaca] hover:bg-white"
          : "bg-[#d6e7dc] hover:bg-[#cfe2d5]"
      )}
    >
      <Icon className="size-6" strokeWidth={1.5} aria-hidden />
      <span className="flex w-[231px] max-w-full flex-col gap-1.5 font-sans leading-[18px]">
        <span className="text-sm font-bold">{title}</span>
        <span className="text-xs">{description}</span>
      </span>
    </button>
  )
}

/** Figma `6777:41507` (stacked) / `6780:42236` (row). */
export function SignupLocationsActionCards({
  disabled = false,
  layout = "stacked",
  onAddIndividually,
  onImportCsv,
  className,
}: SignupLocationsActionCardsProps) {
  return (
    <div
      className={cn(
        "flex w-full flex-col gap-4",
        layout === "row" && "sm:flex-row sm:items-center",
        className
      )}
    >
      <ActionCard
        icon={MapPinPlusInside}
        title="Add individually"
        description="Enter each restaurant's name and address."
        tone="outline"
        disabled={disabled}
        onClick={onAddIndividually}
      />
      <ActionCard
        icon={Download}
        title="Import Locations"
        description="Upload a CSV file to add multiple restaurants."
        tone="accent"
        disabled={disabled}
        onClick={onImportCsv}
      />
    </div>
  )
}

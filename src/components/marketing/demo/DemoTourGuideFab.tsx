import { cn } from "@/lib/utils"

type DemoTourGuideFabProps = {
  label: string
  /** Badge count — Figma shows total steps (e.g. 4). */
  badge: number | string
  onClick: () => void
  className?: string
  "aria-expanded"?: boolean
}

/** Figma Tour guide pill `5740:20149` — green pill, white badge, green numeral. */
export function DemoTourGuideFab({
  label,
  badge,
  onClick,
  className,
  "aria-expanded": ariaExpanded,
}: DemoTourGuideFabProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-expanded={ariaExpanded}
      className={cn(
        "font-sans pointer-events-auto absolute right-[46px] bottom-10 z-30 inline-flex items-center justify-center gap-6 overflow-clip rounded-[68px] bg-[#14a74a] px-6 py-4 text-lg font-semibold text-white shadow-[0_8px_24px_rgba(20,167,74,0.35)]",
        className
      )}
    >
      {label}
      <span className="inline-flex min-w-[30px] items-center justify-center rounded-[30px] bg-white px-2.5 py-1.5 text-sm font-semibold text-[#14a74a]">
        {badge}
      </span>
    </button>
  )
}

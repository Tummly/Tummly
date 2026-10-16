import { cn } from "@/lib/utils"

type DemoTourGuideFabProps = {
  label: string
  /** Badge count — Figma shows total chapters (e.g. 4). */
  badge: number | string
  onClick: () => void
  className?: string
  "aria-expanded"?: boolean
}

/** Tour guide pill — green pill, white badge. Positioned by parent. */
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
        "font-sans pointer-events-auto inline-flex items-center justify-center gap-4 overflow-clip rounded-full bg-[#14a74a] px-5 py-3 text-base font-semibold text-white shadow-[0_8px_24px_rgba(20,167,74,0.35)]",
        className
      )}
    >
      {label}
      <span className="inline-flex min-w-[28px] items-center justify-center rounded-full bg-white px-2 py-1 text-xs font-semibold text-[#14a74a]">
        {badge}
      </span>
    </button>
  )
}

import type { LucideIcon } from "lucide-react"

import { cn } from "@/lib/utils"

/**
 * Figma black circle icon — 46px, `#141414`, white glyph (Mission `7023:13878`
 * 20.5px, Company `7024:13962` 24px).
 */
export function AboutIconBadge({
  icon: Icon,
  glyphClassName = "size-5",
}: {
  icon: LucideIcon
  glyphClassName?: string
}) {
  return (
    <div
      className="flex size-[46px] shrink-0 items-center justify-center rounded-full bg-[#141414] text-white"
      aria-hidden
    >
      <Icon className={cn(glyphClassName)} strokeWidth={1.75} />
    </div>
  )
}

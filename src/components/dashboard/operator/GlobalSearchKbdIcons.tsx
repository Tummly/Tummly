import { cn } from "@/lib/utils"

type IconProps = {
  className?: string
}

/** Figma 6518:13717 — Move down (flip for up). */
export function GlobalSearchKbdArrowIcon({ className }: IconProps) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={18}
      height={18}
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden
      className={cn("size-[18px] shrink-0 opacity-40", className)}
    >
      <path
        d="M13.5 9.375L9 13.875L4.5 9.375M9 13.875V4.5"
        stroke="currentColor"
      />
    </svg>
  )
}

/** Figma 6518:13723 — Open / Enter. */
export function GlobalSearchKbdEnterIcon({ className }: IconProps) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={18}
      height={18}
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden
      className={cn("size-[18px] shrink-0 opacity-40", className)}
    >
      <path
        d="M7.73218 14.6006L4.75659 11.625L7.73218 8.64941M4.75659 11.625H13.2444V4.5"
        stroke="currentColor"
      />
    </svg>
  )
}

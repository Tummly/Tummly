import { cn } from "@/lib/utils"

type OperatorSearchIconProps = {
  className?: string
}

/**
 * Operator search glyph from `assets/svg/ui-icons/search.svg`.
 * Uses `currentColor` so light/dark tokens (e.g. `text-op-header-search-text`,
 * `text-op-icon-default`) colour the icon.
 */
export function OperatorSearchIcon({ className }: OperatorSearchIconProps) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      width={16}
      height={16}
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden
      className={cn("size-4 shrink-0", className)}
    >
      <path
        d="M15.9023 14.9597L11.2545 10.3118C12.136 9.22342 12.6667 7.8398 12.6667 6.33336C12.6667 2.84116 9.82552 0 6.33332 0C2.84113 0 0 2.84116 0 6.33336C0 9.82555 2.84116 12.6667 6.33336 12.6667C7.8398 12.6667 9.22342 12.136 10.3118 11.2545L14.9597 15.9024C15.0899 16.0325 15.3009 16.0325 15.4311 15.9024L15.9024 15.4311C16.0325 15.3009 16.0325 15.0898 15.9023 14.9597ZM6.33336 11.3334C3.57619 11.3334 1.33335 9.09052 1.33335 6.33336C1.33335 3.5762 3.57619 1.33335 6.33336 1.33335C9.09052 1.33335 11.3334 3.5762 11.3334 6.33336C11.3334 9.09052 9.09052 11.3334 6.33336 11.3334Z"
        fill="currentColor"
      />
    </svg>
  )
}

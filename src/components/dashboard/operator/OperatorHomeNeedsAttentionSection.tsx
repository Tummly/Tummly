import { ChevronDownIcon } from "lucide-react"

const EMPTY_COPY = "Nothing needs attention right now."

/** Figma Needs attention — component-owned empty shell. */
export function OperatorHomeNeedsAttentionSection() {
  return (
    <section className="flex flex-col gap-10 overflow-hidden rounded-[10px] bg-[#f8f8f8] p-5 dark:bg-white/5">
      <div className="flex items-center justify-between gap-4">
        <div className="flex flex-col gap-2">
          <h2 className="text-xl font-bold text-foreground">Needs attention</h2>
          <p className="text-sm font-medium text-foreground/70">
            Review issues that may require action.
          </p>
        </div>
        <span
          className="flex size-[42px] shrink-0 items-center justify-center rounded-xl bg-white dark:bg-white/10"
          aria-hidden
        >
          <ChevronDownIcon className="size-[18px] text-foreground" />
        </span>
      </div>
      <div className="flex min-h-[180px] items-center justify-center py-10">
        <p className="text-center text-base font-medium text-[#4b4b4b] dark:text-white/70">
          {EMPTY_COPY}
        </p>
      </div>
    </section>
  )
}

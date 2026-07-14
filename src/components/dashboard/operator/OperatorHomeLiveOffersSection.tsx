import { Button } from "@/components/ui/button"

const EMPTY_COPY = "No live offers or campaigns"
const EMPTY_HELPER =
  "Create a return-visit offer or start from a campaign template."

const primaryButtonClassName =
  "h-[37px] min-h-0 rounded-lg border-transparent bg-primary px-[17px] py-[11px] text-xs font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"

const outlineButtonClassName =
  "h-[37px] min-h-0 rounded-lg border border-foreground bg-transparent px-[17px] py-[11px] text-xs font-medium text-foreground hover:bg-black/5 dark:hover:bg-white/10 disabled:opacity-50"

/** Figma Live offers and campaigns — component-owned empty shell with disabled CTAs. */
export function OperatorHomeLiveOffersSection() {
  return (
    <section className="flex flex-col gap-10 rounded-[10px] bg-[#f8f8f8] p-5 dark:bg-white/5">
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-bold text-foreground">
          Live offers and campaigns
        </h2>
        <p className="text-sm font-medium text-foreground/70">
          See what is currently running and how it is performing.
        </p>
      </div>
      <div className="flex min-h-[180px] flex-col items-center justify-center gap-[30px] py-10">
        <div className="flex flex-col items-center gap-2.5 text-center">
          <p className="text-base font-medium text-[#4b4b4b] dark:text-white/70">
            {EMPTY_COPY}
          </p>
          <p className="max-w-[228px] text-sm font-medium leading-[18px] text-[#999]">
            {EMPTY_HELPER}
          </p>
        </div>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <Button
            type="button"
            className={primaryButtonClassName}
            disabled
            aria-disabled
            aria-label="Create offer (unavailable)"
          >
            Create offer
          </Button>
          <Button
            type="button"
            variant="outline"
            className={outlineButtonClassName}
            disabled
            aria-disabled
            aria-label="Create campaign (unavailable)"
          >
            Create campaign
          </Button>
        </div>
      </div>
    </section>
  )
}

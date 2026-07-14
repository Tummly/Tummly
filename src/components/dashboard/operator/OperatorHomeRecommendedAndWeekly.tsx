import { SparklesIcon } from "lucide-react"

import { Button } from "@/components/ui/button"

const RECOMMENDED_EMPTY_COPY =
  "A recommended action will appear once there is enough guest activity."

const WEEKLY_BRIEF_EMPTY_COPY =
  "Your weekly brief will appear once there is enough scan, feedback and guest activity."

const mutedButtonClassName =
  "h-auto min-h-0 rounded-lg border border-[#dcdcdc] bg-white px-[17px] py-[11px] text-sm font-medium leading-5 text-foreground hover:bg-white disabled:opacity-50 dark:border-white/15 dark:bg-transparent"

/** Figma Recommended next step — component-owned empty full-width card. */
export function OperatorHomeRecommendedNextStep() {
  return (
    <section className="rounded-lg border border-[#dcdcdc] bg-white py-[25px] shadow-[0px_1px_1.5px_rgba(19,29,43,0.04),0px_8px_12px_rgba(19,29,43,0.08)] dark:border-white/10 dark:bg-white/5 dark:shadow-none">
      <div className="border-b border-[#e1e1e1] px-6 pb-6 dark:border-white/10">
        <div className="flex items-center gap-2">
          <SparklesIcon className="size-4 text-primary" aria-hidden />
          <h2 className="text-lg font-semibold text-foreground">
            Recommended next step
          </h2>
        </div>
        <p className="mt-2 text-sm font-medium text-foreground/70">
          AI-assisted guidance based on your recent guest activity.
        </p>
      </div>
      <div className="px-6 pt-5">
        <div className="rounded-lg bg-primary/8 p-5">
          <p className="text-sm font-medium text-[#5c697a] dark:text-white/70">
            {RECOMMENDED_EMPTY_COPY}
          </p>
        </div>
      </div>
    </section>
  )
}

/** Figma Weekly brief — component-owned empty full-width card. */
export function OperatorHomeWeeklyBriefSection() {
  return (
    <section className="flex flex-col justify-between rounded-lg border border-[#dcdcdc] bg-white py-[25px] shadow-[0px_1px_1.5px_rgba(19,29,43,0.04),0px_8px_12px_rgba(19,29,43,0.08)] dark:border-white/10 dark:bg-white/5 dark:shadow-none">
      <div>
        <div className="border-b border-[#e1e1e1] px-6 pb-6 dark:border-white/10">
          <h2 className="text-lg font-semibold text-foreground">Weekly brief</h2>
          <p className="mt-2 text-sm font-medium text-foreground/70">
            A short summary of your guest capture, feedback and campaign
            activity.
          </p>
        </div>
        <p className="px-6 pt-6 text-sm font-medium text-[#5c697a] dark:text-white/70">
          {WEEKLY_BRIEF_EMPTY_COPY}
        </p>
      </div>
      <div className="px-6 pt-8">
        <Button
          type="button"
          className={mutedButtonClassName}
          disabled
          aria-disabled
          aria-label="Open weekly brief (unavailable)"
        >
          Open weekly brief
        </Button>
      </div>
    </section>
  )
}

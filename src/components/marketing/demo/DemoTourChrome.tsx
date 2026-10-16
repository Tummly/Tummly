import { Check, Sparkles, X } from "lucide-react"

import { DemoTourGuideFab } from "@/components/marketing/demo/DemoTourGuideFab"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"
import { cn } from "@/lib/utils"

type DemoTourChromeProps = {
  stepIndex: number
  exploreOpen: boolean
  onToggleExplore: () => void
  onSelectStep: (index: number) => void
  onBack: () => void
  onContinue: () => void
  onFinish: () => void
}

/**
 * One chrome layer at a time (Figma tour): tip card OR explore panel, plus FAB.
 * Never stack tip + explore together.
 */
export function DemoTourChrome({
  stepIndex,
  exploreOpen,
  onToggleExplore,
  onSelectStep,
  onBack,
  onContinue,
  onFinish,
}: DemoTourChromeProps) {
  const { steps, fabLabel, fabBadgeTotal, exploreTitle, exploreBody } =
    PRODUCT_DEMO.tour
  const step = steps[stepIndex] ?? steps[0]
  const total = steps.length
  /** Completed share — 0% until the first tip is done. */
  const progress = Math.round((stepIndex / total) * 100)
  const isLast = stepIndex >= total - 1

  return (
    <>
      {exploreOpen ? (
        <div
          className="pointer-events-auto absolute inset-0 z-20"
          aria-hidden
          onClick={onToggleExplore}
        />
      ) : null}

      {exploreOpen ? (
        <div
          className="pointer-events-auto absolute right-4 bottom-28 z-30 w-[min(100%-2rem,380px)] rounded-[12px] bg-white p-6 shadow-[0_12px_40px_rgba(0,0,0,0.18)] sm:right-10 sm:p-[30px]"
          role="dialog"
          aria-modal="true"
          aria-label={exploreTitle}
        >
          <div className="mb-4 flex items-start justify-between gap-3">
            <div className="flex flex-col gap-2">
              <p className="font-sans text-sm leading-[19px] font-semibold text-[#141414]">
                {exploreTitle}
              </p>
              <p className="font-sans max-w-[272px] text-sm leading-5 text-[#4e4e4e]">
                {exploreBody}
              </p>
            </div>
            <button
              type="button"
              onClick={onToggleExplore}
              className="inline-flex size-8 shrink-0 items-center justify-center rounded-md text-[#4e4e4e] hover:bg-[#f3f3f3]"
              aria-label="Close explore panel"
            >
              <X className="size-4" />
            </button>
          </div>
          <div className="mb-6 flex items-center gap-2">
            <div className="h-1 flex-1 overflow-hidden rounded-full bg-[#ebebeb]">
              <div
                className="h-full rounded-full bg-[#14a74a] transition-[width] duration-300"
                style={{ width: `${progress}%` }}
              />
            </div>
            <span className="font-sans text-xs text-[#4e4e4e]">{progress}%</span>
          </div>
          <ol className="flex flex-col gap-6">
            {steps.map((item, index) => {
              const done = index < stepIndex
              const current = index === stepIndex
              return (
                <li key={item.id}>
                  <button
                    type="button"
                    onClick={() => onSelectStep(index)}
                    className="flex w-full items-start gap-3 text-left"
                  >
                    <span
                      className={cn(
                        "mt-0.5 flex size-4 shrink-0 items-center justify-center rounded-full border",
                        done
                          ? "border-[#14a74a] bg-[#14a74a] text-white"
                          : current
                            ? "border-transparent bg-transparent text-[#14a74a]"
                            : "border-[#cfcfcf] border-dashed bg-white"
                      )}
                      aria-hidden
                    >
                      {done ? (
                        <Check className="size-2.5" strokeWidth={3} />
                      ) : current ? (
                        <Sparkles className="size-3.5" strokeWidth={2} />
                      ) : null}
                    </span>
                    <span className="flex min-w-0 flex-col gap-1.5">
                      <span className="font-sans text-sm leading-[17px] font-medium text-[#141414]">
                        {item.title}
                      </span>
                      <span className="font-sans text-sm leading-5 text-[#4e4e4e]">
                        {item.body}
                      </span>
                    </span>
                  </button>
                </li>
              )
            })}
          </ol>
        </div>
      ) : (
        <div
          className="pointer-events-auto absolute top-[38%] left-14 z-20 w-[min(100%-3rem,300px)] rounded-[10px] border border-[#ebebeb] bg-white p-5 shadow-[0_12px_40px_rgba(0,0,0,0.14)] sm:left-[72px]"
          role="status"
          aria-live="polite"
        >
          <p className="font-jakarta mb-2 text-base font-semibold text-[#141414]">
            {step.tipTitle}
          </p>
          <p className="font-sans mb-5 text-sm leading-5 text-[#4e4e4e]">
            {step.tipBody}
          </p>
          <div className="flex items-center justify-between gap-3">
            <span className="font-sans text-xs text-[#7c7c7c]">
              {stepIndex + 1} of {total}
            </span>
            <div className="flex gap-2">
              <MarketingButton
                type="button"
                variant="secondary"
                showArrow={false}
                density="compact"
                disabled={stepIndex === 0}
                onClick={onBack}
              >
                Back
              </MarketingButton>
              <MarketingButton
                type="button"
                variant="primary"
                showArrow={false}
                density="compact"
                onClick={isLast ? onFinish : onContinue}
              >
                {isLast ? "Finish" : "Continue"}
              </MarketingButton>
            </div>
          </div>
        </div>
      )}

      <DemoTourGuideFab
        label={fabLabel}
        badge={fabBadgeTotal}
        onClick={onToggleExplore}
        aria-expanded={exploreOpen}
      />
    </>
  )
}

import { Check, Sparkles, X } from "lucide-react"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { DemoTourGuideFab } from "@/components/marketing/demo/DemoTourGuideFab"
import { PRODUCT_DEMO, productDemoChapterProgressPercent } from "@/content/marketing/productDemoPage"
import { cn } from "@/lib/utils"

type DemoTourChromeProps = {
  chapterIndex: number
  stepIndex: number
  completedChapterIds: readonly string[]
  exploreOpen: boolean
  showFab: boolean
  onToggleExplore: () => void
  onSelectChapter: (chapterIndex: number) => void
  /** Resumes the current chapter. Hidden when every chapter is complete. */
  onContinue?: () => void
}

/**
 * Explore panel + FAB. Coachmarks are rendered separately via SpotlightCoachmark.
 */
export function DemoTourChrome({
  chapterIndex,
  stepIndex,
  completedChapterIds,
  exploreOpen,
  showFab,
  onToggleExplore,
  onSelectChapter,
  onContinue,
}: DemoTourChromeProps) {
  const {
    chapters,
    fabLabel,
    fabBadgeTotal,
    exploreTitle,
    exploreBody,
    continueLabel,
    completeBody,
  } = PRODUCT_DEMO.tour
  const progress = productDemoChapterProgressPercent(completedChapterIds)
  const tourComplete = progress >= 100

  return (
    <>
      {exploreOpen ? (
        <button
          type="button"
          className="pointer-events-auto absolute inset-0 z-20 cursor-default bg-transparent"
          aria-label="Close tour guide"
          onClick={onToggleExplore}
        />
      ) : null}

      <div className="pointer-events-none absolute right-4 bottom-4 z-30 flex max-w-[min(100%-2rem,320px)] flex-col items-end gap-3 sm:right-8 sm:bottom-8">
        {exploreOpen ? (
          <div
            className="pointer-events-auto w-full rounded-[12px] bg-white p-5 shadow-[0_12px_40px_rgba(0,0,0,0.16)]"
            role="dialog"
            aria-modal="true"
            aria-label={exploreTitle}
            onClick={(event) => event.stopPropagation()}
          >
            <div className="mb-3 flex items-start justify-between gap-3">
              <div className="flex flex-col gap-1.5">
                <p className="font-sans text-sm leading-5 font-semibold text-[#141414]">
                  {exploreTitle}
                </p>
                <p className="font-sans text-xs leading-4 text-[#4e4e4e]">
                  {tourComplete ? completeBody : exploreBody}
                </p>
              </div>
              <button
                type="button"
                onClick={onToggleExplore}
                className="inline-flex size-7 shrink-0 items-center justify-center rounded-md text-[#4e4e4e] hover:bg-[#f3f3f3]"
                aria-label="Close explore panel"
              >
                <X className="size-4" />
              </button>
            </div>
            <div className="mb-4 flex items-center gap-2">
              <div className="h-1 flex-1 overflow-hidden rounded-full bg-[#ebebeb]">
                <div
                  className="h-full rounded-full bg-[#14a74a] transition-[width] duration-300"
                  style={{ width: `${progress}%` }}
                />
              </div>
              <span className="font-sans text-xs text-[#4e4e4e]">{progress}%</span>
            </div>
            <ol className="flex flex-col gap-4">
              {chapters.map((chapter, index) => {
                const done = completedChapterIds.includes(chapter.id)
                const current = index === chapterIndex && !done
                return (
                  <li key={chapter.id}>
                    <button
                      type="button"
                      onClick={() => onSelectChapter(index)}
                      className="flex w-full items-start gap-2.5 text-left"
                    >
                      <span
                        className={cn(
                          "mt-0.5 flex size-4 shrink-0 items-center justify-center rounded-full border",
                          done
                            ? "border-[#14a74a] bg-[#14a74a] text-white"
                            : current
                              ? "border-transparent bg-transparent text-[#14a74a]"
                              : "border-[#d0d0d0] bg-transparent"
                        )}
                      >
                        {done ? (
                          <Check className="size-3" strokeWidth={3} />
                        ) : current ? (
                          <Sparkles className="size-3.5" />
                        ) : null}
                      </span>
                      <span className="flex flex-col gap-0.5">
                        <span className="font-sans text-sm font-semibold text-[#141414]">
                          {index + 1}. {chapter.label}
                        </span>
                        <span className="font-sans text-xs text-[#7c7c7c]">
                          {chapter.steps.length} steps
                          {current
                            ? ` · Step ${stepIndex + 1} of ${chapter.steps.length}`
                            : done
                              ? " · Complete"
                              : ""}
                        </span>
                      </span>
                    </button>
                  </li>
                )
              })}
            </ol>
            {!tourComplete && onContinue ? (
              <MarketingButton
                type="button"
                variant="primary"
                showArrow={false}
                className="mt-5 w-full min-h-11"
                onClick={onContinue}
              >
                {continueLabel}
              </MarketingButton>
            ) : null}
          </div>
        ) : null}

        {showFab ? (
          <div className="pointer-events-auto">
            <DemoTourGuideFab
              label={fabLabel}
              badge={fabBadgeTotal}
              aria-expanded={exploreOpen}
              onClick={onToggleExplore}
            />
          </div>
        ) : null}
      </div>
    </>
  )
}

import { Check } from "lucide-react"

import { DemoTourGuideFab } from "@/components/marketing/demo/DemoTourGuideFab"
import { PRODUCT_DEMO, productDemoChapterProgressPercent } from "@/content/marketing/productDemoPage"
import { cn } from "@/lib/utils"

type DemoTourChromeProps = {
  chapterIndex: number
  completedChapterIds: readonly string[]
  exploreOpen: boolean
  showFab: boolean
  onToggleExplore: () => void
  onSelectChapter: (chapterIndex: number) => void
  /** Resumes the current chapter where the visitor left it. */
  onContinue: () => void
}

/**
 * Tour guide panel (Figma `5734:19888`) + FAB. Coachmarks are rendered
 * separately via SpotlightCoachmark.
 */
export function DemoTourChrome({
  chapterIndex,
  completedChapterIds,
  exploreOpen,
  showFab,
  onToggleExplore,
  onSelectChapter,
  onContinue,
}: DemoTourChromeProps) {
  const { chapters, fabLabel, fabBadgeTotal, exploreTitle, exploreBody, completeBody } =
    PRODUCT_DEMO.tour
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

      <div className="pointer-events-none absolute right-4 bottom-4 z-30 flex max-w-[calc(100%-2rem)] flex-col items-end gap-3 sm:right-8 sm:bottom-8">
        {exploreOpen ? (
          <div
            className="pointer-events-auto flex max-h-[calc(100dvh-7rem)] w-[422px] max-w-full flex-col gap-[26px] overflow-y-auto rounded-[12px] bg-white p-[30px] shadow-[0_12px_40px_rgba(0,0,0,0.16)]"
            role="dialog"
            aria-modal="true"
            aria-labelledby="demo-tour-guide-title"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="flex flex-col gap-2 text-black">
              <h2
                id="demo-tour-guide-title"
                className="font-sans text-base leading-normal font-semibold"
              >
                {exploreTitle}
              </h2>
              <p className="font-sans max-w-[272px] text-xs leading-normal">
                {tourComplete ? completeBody : exploreBody}
              </p>
            </div>

            <div className="flex items-center gap-2">
              <div
                role="progressbar"
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={progress}
                aria-label="Tour progress"
                className="h-4 flex-1 overflow-hidden rounded-[30px] bg-[#ebebeb]"
              >
                <div
                  className="h-full rounded-[30px] bg-[#14a74a] transition-[width] duration-300"
                  style={{ width: `${progress}%` }}
                />
              </div>
              <span className="font-sans text-xs leading-normal text-black">
                {progress}%
              </span>
            </div>

            <ol className="flex flex-col gap-[26px]">
              {chapters.map((chapter, index) => {
                const done = completedChapterIds.includes(chapter.id)
                const current = index === chapterIndex && !done
                return (
                  <li key={chapter.id}>
                    <button
                      type="button"
                      onClick={() =>
                        current ? onContinue() : onSelectChapter(index)
                      }
                      aria-current={current ? "step" : undefined}
                      className="group flex w-full items-start gap-3 rounded-[6px] text-left outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"
                    >
                      <span
                        aria-hidden
                        className={cn(
                          "flex size-4 shrink-0 items-center justify-center rounded-[30px] border",
                          done
                            ? "border-[#14a74a] bg-[#14a74a] text-white"
                            : current
                              ? "border-dashed border-[#14a74a]"
                              : "border-dashed border-[#cacaca]"
                        )}
                      >
                        {done ? (
                          <Check className="size-3" strokeWidth={3} />
                        ) : current ? (
                          <span className="size-2.5 rounded-[30px] bg-[#14a74a]" />
                        ) : null}
                      </span>
                      <span className="flex max-w-[287px] flex-col gap-2 text-black">
                        <span className="font-sans text-sm leading-normal font-semibold group-hover:underline">
                          {index + 1}. {chapter.label}
                        </span>
                        <span className="font-sans text-xs leading-normal">
                          {chapter.description}
                        </span>
                      </span>
                    </button>
                  </li>
                )
              })}
            </ol>
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

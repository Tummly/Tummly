import { useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"

import purposeVisualImage from "@/assets/images/marketing/pilot/purpose-visual.png"
import {
  PILOT_PURPOSE,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
  PILOT_SECTION_Y,
  type PilotPurposeOption,
} from "@/content/marketing/pilotPage"
import { cn } from "@/lib/utils"

const CROSSFADE_MS = 0.25

/** Same visual for every option until per-option assets ship. */
const PURPOSE_IMAGES = {
  "real-deployment": purposeVisualImage,
  "private-listening": purposeVisualImage,
  "guest-context": purposeVisualImage,
  "action-learning": purposeVisualImage,
} as const satisfies Record<PilotPurposeOption["id"], string>

function PurposeOptions({
  activeId,
  onSelect,
}: {
  activeId: PilotPurposeOption["id"]
  onSelect: (id: PilotPurposeOption["id"]) => void
}) {
  const options = PILOT_PURPOSE.options

  const moveActive = (delta: number) => {
    const index = options.findIndex((option) => option.id === activeId)
    const next = options[(index + delta + options.length) % options.length]
    if (next != null) {
      onSelect(next.id)
    }
  }

  return (
    <div
      role="tablist"
      aria-label="What the Pilot is for"
      className="flex w-full flex-col gap-[30px]"
      onKeyDown={(event) => {
        if (event.key === "ArrowDown" || event.key === "ArrowRight") {
          event.preventDefault()
          moveActive(1)
        } else if (event.key === "ArrowUp" || event.key === "ArrowLeft") {
          event.preventDefault()
          moveActive(-1)
        } else if (event.key === "Home") {
          event.preventDefault()
          onSelect(options[0].id)
        } else if (event.key === "End") {
          event.preventDefault()
          onSelect(options[options.length - 1]!.id)
        }
      }}
    >
      {options.map((option) => {
        const active = option.id === activeId
        return (
          <button
            key={option.id}
            type="button"
            role="tab"
            id={`pilot-purpose-tab-${option.id}`}
            aria-selected={active}
            aria-controls="pilot-purpose-panel"
            tabIndex={active ? 0 : -1}
            ref={(node) => {
              if (active && node != null && document.activeElement !== node) {
                const tablist = node.closest('[role="tablist"]')
                if (
                  tablist != null &&
                  tablist.contains(document.activeElement)
                ) {
                  node.focus()
                }
              }
            }}
            onClick={() => {
              onSelect(option.id)
            }}
            className={cn(
              "flex w-full flex-col items-start gap-[18px] border-0 border-b border-solid bg-transparent pb-5 text-left text-[#141414]",
              "cursor-pointer transition-colors duration-200 ease-out motion-reduce:transition-none",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
              active ? "border-[#14a946]" : "border-[#7c7c7c]",
            )}
          >
            <span className="font-jakarta text-2xl font-normal leading-normal">
              {option.title}
            </span>
            <span className="font-sans text-base font-normal leading-normal">
              {option.body}
            </span>
          </button>
        )
      })}
    </div>
  )
}

/**
 * Figma Pilot “What the Pilot is for” `5661:19885`.
 * Option list (left) drives the visual (right); same image for all options for now.
 */
export function PilotPurpose() {
  const reduceMotion = useReducedMotion()
  const [activeId, setActiveId] = useState<PilotPurposeOption["id"]>(
    PILOT_PURPOSE.options[0].id,
  )
  const imageSrc = PURPOSE_IMAGES[activeId]

  return (
    <section className="w-full bg-[#f0f0f0]">
      <div
        className={cn(
          "flex w-full flex-col gap-10 lg:gap-[70px]",
          PILOT_SECTION_INSET,
          PILOT_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[654px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-sm font-medium uppercase leading-normal text-black lg:text-base">
            {PILOT_PURPOSE.eyebrow}
          </p>
          <h2 className={cn("m-0", PILOT_SECTION_HEADING)}>
            {PILOT_PURPOSE.title}
          </h2>
          <p className={cn("m-0 max-w-[644px]", PILOT_SECTION_BODY)}>
            {PILOT_PURPOSE.body}
          </p>
        </div>

        <div className="flex w-full flex-col items-stretch gap-10 lg:flex-row lg:items-start lg:gap-[70px]">
          <div className="w-full min-w-0 flex-1">
            <PurposeOptions activeId={activeId} onSelect={setActiveId} />
          </div>

          <div
            id="pilot-purpose-panel"
            role="tabpanel"
            aria-labelledby={`pilot-purpose-tab-${activeId}`}
            className="relative aspect-[739/568] w-full min-w-0 flex-1 overflow-hidden bg-white lg:aspect-auto lg:h-[568px]"
          >
            <AnimatePresence initial={false} mode="wait">
              <motion.img
                key={activeId}
                src={imageSrc}
                alt={PILOT_PURPOSE.imageAlt}
                width={1448}
                height={1086}
                initial={reduceMotion ? false : { opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={reduceMotion ? undefined : { opacity: 0 }}
                transition={{
                  duration: reduceMotion ? 0 : CROSSFADE_MS,
                  ease: "easeOut",
                }}
                className="absolute inset-0 size-full object-cover"
                decoding="async"
              />
            </AnimatePresence>
          </div>
        </div>

        <p className="m-0 max-w-[644px] font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-[18px] lg:leading-6">
          {PILOT_PURPOSE.footnote}
        </p>
      </div>
    </section>
  )
}

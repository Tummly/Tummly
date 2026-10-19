import { useState } from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  PILOT_IN_ACTION,
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
  type PilotInActionTab,
} from "@/content/marketing/pilotPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const CROSSFADE_MS = 0.25

const TAB_IMAGES = {
  hear: remoteSimpleImg("pilot/in-action-hear"),
  connected: remoteSimpleImg("pilot/in-action-connected"),
  opportunity: remoteSimpleImg("pilot/in-action-opportunity"),
  working: remoteSimpleImg("pilot/in-action-working"),
} as const

function InActionTabs({
  activeId,
  onSelect,
}: {
  activeId: PilotInActionTab["id"]
  onSelect: (id: PilotInActionTab["id"]) => void
}) {
  const tabs = PILOT_IN_ACTION.tabs

  const moveActive = (delta: number) => {
    const index = tabs.findIndex((tab) => tab.id === activeId)
    const next = tabs[(index + delta + tabs.length) % tabs.length]
    if (next != null) {
      onSelect(next.id)
    }
  }

  return (
    <div
      role="tablist"
      aria-label="See Tummly in action"
      className="flex w-full gap-2 overflow-x-auto [-ms-overflow-style:none] [scrollbar-width:none] sm:gap-2.5 lg:gap-[18px] [&::-webkit-scrollbar]:hidden"
      onKeyDown={(event) => {
        if (event.key === "ArrowDown" || event.key === "ArrowRight") {
          event.preventDefault()
          moveActive(1)
        } else if (event.key === "ArrowUp" || event.key === "ArrowLeft") {
          event.preventDefault()
          moveActive(-1)
        } else if (event.key === "Home") {
          event.preventDefault()
          onSelect(tabs[0]!.id)
        } else if (event.key === "End") {
          event.preventDefault()
          onSelect(tabs[tabs.length - 1]!.id)
        }
      }}
    >
      {tabs.map((tab) => {
        const active = tab.id === activeId
        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            id={`pilot-in-action-tab-${tab.id}`}
            aria-selected={active}
            aria-controls="pilot-in-action-panel"
            tabIndex={active ? 0 : -1}
            onClick={() => {
              onSelect(tab.id)
            }}
            className={cn(
              "shrink-0 rounded-[4px] border-0 px-3 py-2.5 text-left font-sans text-xs leading-4 text-black sm:px-4 sm:py-3.5 sm:text-sm sm:leading-5",
              "cursor-pointer transition-colors duration-200 ease-out motion-reduce:transition-none",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
              "lg:flex-1 lg:px-[26px]",
              !active && "bg-[#f0f0f0]",
            )}
            style={
              active
                ? { backgroundColor: PILOT_IN_ACTION.activeTabColor }
                : undefined
            }
          >
            {tab.tabLabel}
          </button>
        )
      })}
    </div>
  )
}

/**
 * Figma Pilot “See Tummly in Action” —
 * `6495:18145` / `6495:18183` / `6495:18223` / `6495:18254`.
 * Shared header + tabs; panel copy and mockup swap with the active tab.
 */
export function PilotInAction() {
  const reduceMotion = useReducedMotion()
  const [activeId, setActiveId] = useState<PilotInActionTab["id"]>(
    PILOT_IN_ACTION.tabs[0]!.id,
  )
  const activeTab = PILOT_IN_ACTION.tabs.find((tab) => tab.id === activeId)!
  const image = TAB_IMAGES[activeId]

  return (
    <section
      className="w-full bg-white"
      aria-labelledby="pilot-in-action-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-center gap-[30px]",
          PILOT_SECTION_INSET,
          "pt-[50px] pb-10 lg:pt-[70px]",
        )}
      >
        <header className="flex w-full max-w-[590px] flex-col items-center gap-6 text-center">
          <div className="flex w-full flex-col items-center gap-3.5 text-black">
            <p className="m-0 font-sans text-sm font-medium uppercase leading-normal tracking-[1.12px]">
              {PILOT_IN_ACTION.eyebrow}
            </p>
            <h2
              id="pilot-in-action-heading"
              className={cn(
                "m-0 w-full max-w-[584px] text-black",
                PILOT_SECTION_HEADING,
              )}
            >
              {PILOT_IN_ACTION.title}
            </h2>
          </div>
          <p className={cn("m-0 w-full", PILOT_SECTION_BODY)}>
            {PILOT_IN_ACTION.body}
          </p>
        </header>

        <InActionTabs activeId={activeId} onSelect={setActiveId} />

        <div
          id="pilot-in-action-panel"
          role="tabpanel"
          aria-labelledby={`pilot-in-action-tab-${activeId}`}
          className="flex w-full flex-col items-start gap-10 lg:flex-row lg:items-start lg:justify-between"
        >
          <div className="flex w-full max-w-[582px] flex-col items-start gap-[52px] pt-0 lg:pt-10">
            <AnimatePresence initial={false} mode="wait">
              <motion.div
                key={activeTab.id}
                initial={reduceMotion ? false : { opacity: 0, y: 8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={reduceMotion ? undefined : { opacity: 0, y: -8 }}
                transition={{
                  duration: reduceMotion ? 0 : CROSSFADE_MS,
                  ease: "easeOut",
                }}
                className="flex w-full flex-col items-start gap-[18px] text-[#141414]"
              >
                <h3 className="m-0 max-w-[466px] font-jakarta text-[28px] font-medium leading-normal lg:text-[36px]">
                  {activeTab.title}
                </h3>
                <p className="m-0 max-w-[541px] font-sans text-base font-normal leading-5">
                  {activeTab.body}
                </p>
              </motion.div>
            </AnimatePresence>

            <div className="flex flex-wrap items-center gap-4">
              <MarketingButton asChild variant="primary">
                <RequestTrialLink planIntent={PILOT_IN_ACTION.planIntent}>
                  {PILOT_IN_ACTION.primaryLabel}
                </RequestTrialLink>
              </MarketingButton>
              <MarketingButton asChild variant="secondary">
                <Link to={PILOT_IN_ACTION.secondaryTo}>
                  {PILOT_IN_ACTION.secondaryLabel}
                </Link>
              </MarketingButton>
            </div>
          </div>

          <div className="relative aspect-[750/600] w-full overflow-hidden rounded-[12px] bg-[#edece8] lg:h-[600px] lg:w-[min(100%,750px)] lg:max-w-[750px] lg:shrink-0 lg:aspect-auto">
            <AnimatePresence initial={false} mode="wait">
              <motion.img
                key={activeTab.id}
                src={image.src}
                srcSet={image.srcSet}
                sizes="(min-width: 1024px) 750px, 100vw"
                alt={activeTab.imageAlt}
                width={750}
                height={600}
                initial={reduceMotion ? false : { opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={reduceMotion ? undefined : { opacity: 0 }}
                transition={{
                  duration: reduceMotion ? 0 : CROSSFADE_MS,
                  ease: "easeOut",
                }}
                className="absolute inset-0 size-full object-cover object-left-top"
                decoding="async"
              />
            </AnimatePresence>
          </div>
        </div>
      </div>
    </section>
  )
}

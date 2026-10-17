import { useState } from "react"
import { motion, useReducedMotion } from "framer-motion"

import {
  HOME_HOW_IT_WORKS_PLACE_MEDIA,
  type HomeHowItWorksPlaceMedia,
} from "@/content/marketing/homePage"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

/** Match Pricing QR expand timing — Place panels grow/collapse in place. */
const PLACE_EXPAND_MS = 0.85
const PLACE_EXPAND_EASE = [0.22, 1, 0.36, 1] as const
/** Active panel vs peek share inside the 600px Place media frame. */
const PLACE_EXPANDED_GROW = 6.6
const PLACE_COLLAPSED_GROW = 1

const placeMediaImages = Object.fromEntries(
  HOME_HOW_IT_WORKS_PLACE_MEDIA.map((item) => [
    item.id,
    remoteSimpleUrl(item.mediaId),
  ]),
) as Record<HomeHowItWorksPlaceMedia["id"], string>

/**
 * Place the Guest Loop media stack — Figma `5339:30041` / How it works `5661:19556`.
 * Three material panels expand/collapse; only in-image label buttons change the active panel.
 */
export function MarketingPlaceStepVisual() {
  const reduceMotion = useReducedMotion()
  const [activeId, setActiveId] = useState<HomeHowItWorksPlaceMedia["id"]>(
    HOME_HOW_IT_WORKS_PLACE_MEDIA[0].id,
  )
  const duration = reduceMotion ? 0 : PLACE_EXPAND_MS

  return (
    <div
      className="relative flex h-[min(345px,36vh)] max-h-[280px] w-full flex-col overflow-hidden rounded-[12px] bg-[#e2eae5] sm:aspect-[750/600] sm:h-auto sm:max-h-[min(42vh,360px)] lg:aspect-auto lg:h-[600px] lg:max-h-none"
      role="group"
      aria-label="Guest Loop placement materials"
    >
      {HOME_HOW_IT_WORKS_PLACE_MEDIA.map((item) => {
        const active = item.id === activeId
        return (
          <motion.div
            key={item.id}
            initial={false}
            animate={{
              flexGrow: active ? PLACE_EXPANDED_GROW : PLACE_COLLAPSED_GROW,
            }}
            transition={{ duration, ease: PLACE_EXPAND_EASE }}
            className="relative min-h-0 w-full basis-0 overflow-hidden"
            style={{ flexShrink: 1 }}
          >
            <img
              src={placeMediaImages[item.id]}
              alt={active ? item.imageAlt : ""}
              width={750}
              height={600}
              className="pointer-events-none absolute inset-0 size-full object-cover"
              draggable={false}
              decoding="async"
              aria-hidden={!active}
            />
            <div
              aria-hidden
              className={cn(
                "pointer-events-none absolute inset-0",
                active
                  ? "bg-[linear-gradient(180deg,rgba(0,0,0,0)_0%,rgb(0,0,0)_116%),linear-gradient(90deg,rgba(0,0,0,0.2),rgba(0,0,0,0.2))]"
                  : "bg-black/80",
              )}
            />
            <button
              type="button"
              aria-pressed={active}
              onClick={() => {
                setActiveId(item.id)
              }}
              className={cn(
                "absolute left-1/2 z-10 -translate-x-1/2 cursor-pointer rounded-[30px] border border-[#363636] bg-transparent px-[18px] py-2 font-jakarta font-semibold uppercase text-white",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/60 focus-visible:ring-offset-2 focus-visible:ring-offset-transparent",
                active
                  ? "bottom-8 text-sm leading-normal lg:bottom-10"
                  : "top-1/2 -translate-y-1/2 text-xs leading-normal",
              )}
            >
              {item.label}
            </button>
          </motion.div>
        )
      })}
    </div>
  )
}

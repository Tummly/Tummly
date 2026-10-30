import { useEffect, useId, useRef, useState } from "react"
import { ChevronLeft, ChevronRight } from "lucide-react"

import {
  carouselScrollStep,
  measureCarouselEdges,
} from "@/components/marketing/marketingCertificationScroll"
import { TrustIndicatorTile } from "@/components/marketing/trust/TrustIndicatorTile"
import { Button } from "@/components/ui/button"
import type { PublicTrustIndicator } from "@/lib/marketing/publicTrustIndicators"
import { cn } from "@/lib/utils"

/**
 * 48×48 bordered arrow. `aria-disabled` (not `disabled`) keeps keyboard focus on
 * the button when it reaches an end of the track.
 */
const CAROUSEL_BUTTON_CLASS =
  "size-12 min-h-12 rounded-[2px] border-[#4e4e4e] bg-transparent p-3 text-[#141414] hover:bg-transparent hover:text-[#141414] focus-visible:border-[#4e4e4e] focus-visible:ring-2 focus-visible:ring-[#141414]/40 aria-disabled:cursor-default aria-disabled:opacity-20 aria-disabled:active:scale-100"

/**
 * Trust indicator row with previous / next controls — Figma Product Trust
 * & privacy `6937:13422` and For Groups hero `6424:16986`. Tiles scroll
 * horizontally; the buttons look disabled at either end. The caller passes only
 * indicators that may be shown (see `visibleTrustIndicators`).
 */
export function MarketingCertificationCarousel({
  indicators,
  className,
}: {
  indicators: readonly PublicTrustIndicator[]
  className?: string
}) {
  const trackRef = useRef<HTMLUListElement>(null)
  const trackId = useId()
  const [edges, setEdges] = useState({ start: true, end: false })

  // Edges also change when the viewport resizes, not only on scroll.
  useEffect(() => {
    const track = trackRef.current
    if (track == null) {
      return
    }
    const sync = () => {
      setEdges((prev) => {
        const next = measureCarouselEdges(track)
        return prev.start === next.start && prev.end === next.end ? prev : next
      })
    }
    sync()
    const observer = new ResizeObserver(sync)
    observer.observe(track)
    return () => {
      observer.disconnect()
    }
  }, [])

  const scrollTrack = (direction: -1 | 1) => {
    const track = trackRef.current
    track?.scrollBy({
      left: carouselScrollStep(direction, track.clientWidth),
      behavior: "smooth",
    })
  }

  return (
    <div className={cn("flex w-full flex-col items-end gap-2.5", className)}>
      <ul
        ref={trackRef}
        id={trackId}
        onScroll={(event) => {
          setEdges(measureCarouselEdges(event.currentTarget))
        }}
        className="m-0 flex w-full list-none gap-6 overflow-x-auto p-0 pb-1 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        {indicators.map((indicator) => (
          <li
            key={indicator.itemNumber}
            className="flex w-[clamp(140px,10.3vw,172px)] shrink-0"
          >
            <TrustIndicatorTile indicator={indicator} variant="carousel" />
          </li>
        ))}
      </ul>

      <div className="flex items-start gap-5">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label="Previous trust indicators"
          aria-controls={trackId}
          aria-disabled={edges.start}
          onClick={() => {
            if (!edges.start) {
              scrollTrack(-1)
            }
          }}
          className={CAROUSEL_BUTTON_CLASS}
        >
          <ChevronLeft
            className="size-[22px]"
            strokeWidth={1.5}
            aria-hidden
          />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label="Next trust indicators"
          aria-controls={trackId}
          aria-disabled={edges.end}
          onClick={() => {
            if (!edges.end) {
              scrollTrack(1)
            }
          }}
          className={CAROUSEL_BUTTON_CLASS}
        >
          <ChevronRight
            className="size-[22px]"
            strokeWidth={1.5}
            aria-hidden
          />
        </Button>
      </div>
    </div>
  )
}

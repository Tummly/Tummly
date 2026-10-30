import { useEffect, useId, useRef, useState } from "react"
import { ChevronLeft, ChevronRight } from "lucide-react"

import iso27001Cert from "@/assets/svg/marketing/product/certs/iso-27001.svg"
import cyberEssentialsPlusCert from "@/assets/svg/marketing/product/certs/cyber-essentials-plus.svg"
import icoDpeCert from "@/assets/svg/marketing/product/certs/ico-dpe.svg"
import icoDataCert from "@/assets/svg/marketing/product/certs/ico-data.svg"
import cyberEssentialsCert from "@/assets/svg/marketing/product/certs/cyber-essentials.svg"
import iasmeLevel1Cert from "@/assets/svg/marketing/product/certs/iasme-level-1.svg"
import goodBusinessCharterCert from "@/assets/svg/marketing/product/certs/good-business-charter.svg"
import iso27701Cert from "@/assets/svg/marketing/product/certs/iso-27701.svg"
import ukGdprCert from "@/assets/svg/marketing/product/certs/uk-gdpr.svg"
import privacyChoicesCert from "@/assets/svg/marketing/product/certs/privacy-choices.svg"
import optInMarketingCert from "@/assets/svg/marketing/product/certs/opt-in-marketing.svg"
import dataRightsCert from "@/assets/svg/marketing/product/certs/data-rights.svg"
import {
  carouselScrollStep,
  measureCarouselEdges,
} from "@/components/marketing/marketingCertificationScroll"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

/** Figma export size (viewBox) per badge, so the `<img>` reserves its box. */
const CERTIFICATION_SIZES: Readonly<
  Record<string, { width: number; height: number }>
> = {
  "iso-27001": { width: 74, height: 74 },
  "cyber-essentials-plus": { width: 74, height: 74 },
  "ico-dpe": { width: 62, height: 74 },
  "ico-data": { width: 72, height: 72 },
  "cyber-essentials": { width: 64, height: 74 },
  "iasme-level-1": { width: 70, height: 70 },
  "good-business-charter": { width: 66, height: 82 },
  "iso-27701": { width: 74, height: 74 },
  "uk-gdpr": { width: 58, height: 74 },
  "privacy-choices": { width: 66, height: 66 },
  "opt-in-marketing": { width: 62, height: 71 },
  "data-rights": { width: 73, height: 72 },
}

const CERTIFICATION_ICONS: Readonly<Record<string, string>> = {
  "iso-27001": iso27001Cert,
  "cyber-essentials-plus": cyberEssentialsPlusCert,
  "ico-dpe": icoDpeCert,
  "ico-data": icoDataCert,
  "cyber-essentials": cyberEssentialsCert,
  "iasme-level-1": iasmeLevel1Cert,
  "good-business-charter": goodBusinessCharterCert,
  "iso-27701": iso27701Cert,
  "uk-gdpr": ukGdprCert,
  "privacy-choices": privacyChoicesCert,
  "opt-in-marketing": optInMarketingCert,
  "data-rights": dataRightsCert,
}

/**
 * 48×48 bordered arrow. `aria-disabled` (not `disabled`) keeps keyboard focus on
 * the button when it reaches an end of the track.
 */
const CAROUSEL_BUTTON_CLASS =
  "size-12 min-h-12 rounded-[2px] border-[#4e4e4e] bg-transparent p-3 text-[#141414] hover:bg-transparent hover:text-[#141414] focus-visible:border-[#4e4e4e] focus-visible:ring-2 focus-visible:ring-[#141414]/40 aria-disabled:cursor-default aria-disabled:opacity-20 aria-disabled:active:scale-100"

export type MarketingCertification = {
  id: string
  label: string
}

/**
 * Certification badge row with previous / next controls — Figma Product Trust
 * & privacy `6937:13422` and For Groups hero `6424:16986`. Tiles scroll
 * horizontally; the buttons look disabled at either end.
 */
export function MarketingCertificationCarousel({
  certifications,
  className,
}: {
  certifications: readonly MarketingCertification[]
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
        {certifications.map((cert) => (
          <li
            key={cert.id}
            className="flex w-[clamp(140px,10.3vw,172px)] shrink-0 flex-col items-center gap-6"
          >
            <div className="flex h-[152px] w-full items-center justify-center rounded-[12px] bg-[#f2f0ec]">
              <img
                src={CERTIFICATION_ICONS[cert.id]}
                alt=""
                width={CERTIFICATION_SIZES[cert.id]?.width}
                height={CERTIFICATION_SIZES[cert.id]?.height}
                className="block max-h-[82px] max-w-[74px]"
                aria-hidden
                decoding="async"
                loading="lazy"
              />
            </div>
            <p className="m-0 w-full max-w-[132px] text-center font-sans text-sm font-normal leading-[17px] text-[#141414]">
              {cert.label}
            </p>
          </li>
        ))}
      </ul>

      <div className="flex items-start gap-5">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          aria-label="Previous certifications"
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
          aria-label="Next certifications"
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

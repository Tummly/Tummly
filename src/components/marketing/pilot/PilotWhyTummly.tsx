import { ChevronRight } from "lucide-react"

import {
  PILOT_SECTION_BODY,
  PILOT_SECTION_HEADING,
  PILOT_SECTION_INSET,
  PILOT_WHY_TUMMLY,
  type PilotWhyCard,
} from "@/content/marketing/pilotPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

/** Figma `6543:18776` top-rail arrow slots. */
const LOOP_ARROW_TOP = [
  "left-[63px]",
  "left-1/2 -translate-x-1/2",
  "left-[calc(50%+428px)] -translate-x-1/2",
] as const

/** Figma `6543:18776` bottom-rail arrow slots (pre-rotate frame). */
const LOOP_ARROW_BOTTOM = [
  "left-[83px]",
  "left-1/2 -translate-x-1/2",
  "left-[calc(50%+448px)] -translate-x-1/2",
] as const

function LoopHalfArrows({
  positions,
}: {
  positions: readonly string[]
}) {
  return (
    <>
      {positions.map((positionClass) => (
        <span
          key={positionClass}
          className={cn(
            "absolute top-[-8.5px] flex h-4 w-6 items-center justify-center overflow-hidden",
            positionClass,
          )}
        >
          <ChevronRight
            className="size-3.5 text-[#a2a2a2]"
            strokeWidth={1.87}
            aria-hidden
          />
        </span>
      ))}
    </>
  )
}

/**
 * Figma cycle track `6543:18776` — two dashed `#ededed` halves with rail arrows.
 */
function WhyLoopTrack() {
  return (
    <div
      className="pointer-events-none absolute inset-0 hidden flex-col lg:flex"
      aria-hidden
    >
      <div className="relative min-h-0 flex-1 rounded-[28px] border border-dashed border-[#a2a2a2] bg-[#ededed]">
        <LoopHalfArrows positions={LOOP_ARROW_TOP} />
      </div>
      <div className="relative min-h-0 flex-1">
        <div className="relative size-full rotate-180 rounded-[28px] border border-dashed border-[#a2a2a2] bg-[#ededed]">
          <LoopHalfArrows positions={LOOP_ARROW_BOTTOM} />
        </div>
      </div>
    </div>
  )
}

const CARD_IMAGES = {
  hear: remoteSimpleImg("pilot/why-hear"),
  relationships: remoteSimpleImg("pilot/why-relationships"),
  return: remoteSimpleImg("pilot/why-return"),
} as const

function WhyCard({ card }: { card: PilotWhyCard }) {
  const image = CARD_IMAGES[card.id]

  return (
    <article
      className={cn(
        "relative flex w-full flex-col items-center overflow-hidden bg-white",
        "border border-solid border-[#cecece]",
        "rounded-[22px] px-6 pt-9 pb-8",
        "lg:h-[470px] lg:w-[min(100%,395px)] lg:shrink-0 lg:px-8",
      )}
    >
      <h3 className="m-0 max-w-[286px] shrink-0 text-center font-jakarta text-2xl font-semibold leading-normal text-[#141414]">
        {card.title}
      </h3>
      <div className="flex min-h-0 w-full flex-1 items-center justify-center py-3">
        <img
          src={image.src}
          srcSet={image.srcSet}
          sizes={`${card.imageWidth}px`}
          alt={card.imageAlt}
          width={card.imageWidth}
          height={card.imageHeight}
          className="block h-auto max-h-full w-full max-w-full object-contain"
          style={{ maxWidth: card.imageWidth }}
          decoding="async"
        />
      </div>
      <p className="m-0 max-w-[292px] shrink-0 text-center font-sans text-sm font-normal leading-[18px] text-[#141414]">
        {card.body}
      </p>
    </article>
  )
}

/**
 * Figma Pilot “Why Tummly” `6543:18855`.
 * Centered header + three cards over dashed cycle loop `6543:18776`.
 */
export function PilotWhyTummly() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="pilot-why-tummly-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-center",
          PILOT_SECTION_INSET,
          "rounded-[12px] bg-[#f0f0f0] pt-[50px] pb-[70px] lg:pt-[70px] lg:pb-[100px]",
        )}
      >
        <div className="flex w-full max-w-[1270px] flex-col items-center gap-10 lg:gap-14">
          <header className="flex w-full max-w-[670px] flex-col items-center gap-[18px] text-center">
            <div className="flex w-full flex-col items-center gap-3.5 text-black">
              <p className="m-0 font-sans text-sm font-medium uppercase leading-normal tracking-[1.12px]">
                {PILOT_WHY_TUMMLY.eyebrow}
              </p>
              <h2
                id="pilot-why-tummly-heading"
                className={cn(
                  "m-0 w-full max-w-[578px] text-black",
                  PILOT_SECTION_HEADING,
                )}
              >
                {PILOT_WHY_TUMMLY.title}
              </h2>
            </div>
            <p className={cn("m-0 w-full", PILOT_SECTION_BODY)}>
              {PILOT_WHY_TUMMLY.body}
            </p>
          </header>

          <div className="relative w-full max-w-[1270px] lg:h-[530px]">
            <WhyLoopTrack />

            <div
              className={cn(
                "relative z-10 grid w-full grid-cols-1 gap-5 sm:grid-cols-2",
                "lg:absolute lg:top-1/2 lg:left-1/2 lg:flex lg:w-[1183px] lg:max-w-[calc(100%-5.5rem)] lg:-translate-x-1/2 lg:-translate-y-1/2 lg:grid-cols-none lg:items-center lg:justify-center lg:gap-0",
              )}
            >
              {PILOT_WHY_TUMMLY.cards.map((card) => (
                <WhyCard key={card.id} card={card} />
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

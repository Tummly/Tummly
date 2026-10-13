import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type CSSProperties,
} from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { ChevronRight } from "lucide-react"

import {
  PRODUCT_CAPTURE_MATERIALS,
  type ProductCaptureMaterial,
} from "@/content/marketing/productPage"
import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const PANEL_HEIGHT_PX = 661
const MATERIAL_COUNT = PRODUCT_CAPTURE_MATERIALS.length
/** Desktop scroll track — one panel-height step per material. */
const TRACK_HEIGHT_PX = PANEL_HEIGHT_PX * MATERIAL_COUNT
const CROSSFADE_MS = 0.25
/** List row: 32px type + 22px gap (Figma). */
const LIST_ROW_STRIDE_PX = 54
/** Minimum clearance under sticky marketing header chrome. */
const HEADER_CLEARANCE_PX = 100
const LG_MQ = "(min-width: 1024px)"

/** Vertically centre the panel in the viewport; never sit under the nav. */
function stickyTopForViewport(viewportHeight: number) {
  const centered = (viewportHeight - PANEL_HEIGHT_PX) / 2
  return Math.max(HEADER_CLEARANCE_PX, centered)
}

const materialImages = Object.fromEntries(
  PRODUCT_CAPTURE_MATERIALS.map((material) => [
    material.id,
    remoteSimpleUrl(material.mediaId),
  ]),
) as Record<(typeof PRODUCT_CAPTURE_MATERIALS)[number]["id"], string>

function clampIndex(index: number) {
  return Math.max(0, Math.min(MATERIAL_COUNT - 1, index))
}

function useIsLgUp() {
  const [isLgUp, setIsLgUp] = useState(() =>
    typeof window !== "undefined" ? window.matchMedia(LG_MQ).matches : false,
  )

  useEffect(() => {
    const mq = window.matchMedia(LG_MQ)
    const onChange = () => {
      setIsLgUp(mq.matches)
    }
    onChange()
    mq.addEventListener("change", onChange)
    return () => {
      mq.removeEventListener("change", onChange)
    }
  }, [])

  return isLgUp
}

function MaterialListItem({
  material,
  active,
  onSelect,
}: {
  material: ProductCaptureMaterial
  active: boolean
  onSelect: () => void
}) {
  return (
    <button
      type="button"
      aria-pressed={active}
      onClick={onSelect}
      className={cn(
        "flex w-full shrink-0 items-center justify-between gap-3 border-0 bg-transparent p-0 text-left",
        "cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
      )}
    >
      <span
        className={cn(
          "font-jakarta text-[22px] leading-normal whitespace-nowrap sm:text-[28px] lg:text-[32px]",
          active
            ? "font-semibold text-black"
            : "font-medium text-[#9e9e9e]",
        )}
      >
        {material.label}
      </span>
      {active ? (
        <ChevronRight
          aria-hidden
          className="size-4 shrink-0 text-black lg:size-5"
          strokeWidth={2.5}
        />
      ) : null}
    </button>
  )
}

function MaterialsList({
  activeIndex,
  onSelect,
  translateY,
  className,
  style,
}: {
  activeIndex: number
  onSelect: (index: number) => void
  translateY: number
  className?: string
  style?: CSSProperties
}) {
  // Top fade only after the list has scrolled — otherwise the active first
  // row ("QR Guest Cards") sits under the gradient and looks washed out.
  const showTopFade = activeIndex > 0
  const showBottomFade = activeIndex < MATERIAL_COUNT - 1

  return (
    <div
      className={cn("relative flex min-h-0 flex-1 flex-col overflow-hidden", className)}
      style={style}
    >
      {showTopFade ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 z-10 h-12 bg-linear-to-b from-[#f0f0f0] to-transparent"
        />
      ) : null}
      {showBottomFade ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 bottom-0 z-10 h-16 bg-linear-to-t from-[#f0f0f0] to-transparent"
        />
      ) : null}
      <ul
        className="m-0 flex list-none flex-col gap-[22px] p-0 transition-transform duration-300 ease-out motion-reduce:transition-none"
        style={{ transform: `translateY(${translateY}px)` }}
      >
        {PRODUCT_CAPTURE_MATERIALS.map((material, index) => (
          <li key={material.id} className="m-0 p-0">
            <MaterialListItem
              material={material}
              active={index === activeIndex}
              onSelect={() => {
                onSelect(index)
              }}
            />
          </li>
        ))}
      </ul>
    </div>
  )
}

function MobileMaterialsList({
  activeIndex,
  onSelect,
}: {
  activeIndex: number
  onSelect: (index: number) => void
}) {
  const scrollerRef = useRef<HTMLDivElement>(null)
  const [showTopFade, setShowTopFade] = useState(false)
  const [showBottomFade, setShowBottomFade] = useState(true)

  const syncFades = useEffectEvent(() => {
    const el = scrollerRef.current
    if (el == null) {
      return
    }
    const { scrollTop, scrollHeight, clientHeight } = el
    setShowTopFade(scrollTop > 4)
    setShowBottomFade(scrollTop + clientHeight < scrollHeight - 4)
  })

  useEffect(() => {
    syncFades()
    const el = scrollerRef.current
    if (el == null) {
      return
    }
    el.addEventListener("scroll", syncFades, { passive: true })
    window.addEventListener("resize", syncFades)
    return () => {
      el.removeEventListener("scroll", syncFades)
      window.removeEventListener("resize", syncFades)
    }
  }, [])

  useEffect(() => {
    syncFades()
  }, [activeIndex])

  return (
    <div className="relative max-h-[280px]">
      {showTopFade ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 z-10 h-10 bg-linear-to-b from-[#f0f0f0] to-transparent"
        />
      ) : null}
      <div
        ref={scrollerRef}
        className="max-h-[280px] overflow-y-auto overscroll-contain [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
      >
        <ul className="m-0 flex list-none flex-col gap-[22px] p-0">
          {PRODUCT_CAPTURE_MATERIALS.map((item, index) => (
            <li key={item.id} className="m-0 p-0">
              <MaterialListItem
                material={item}
                active={index === activeIndex}
                onSelect={() => {
                  onSelect(index)
                }}
              />
            </li>
          ))}
        </ul>
      </div>
      {showBottomFade ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 bottom-0 z-10 h-10 bg-linear-to-t from-[#f0f0f0] to-transparent"
        />
      ) : null}
    </div>
  )
}

/** Figma frames pin Table Tent / Receipt Label toward the lower subject. */
const MEDIA_OBJECT_POSITION: Partial<
  Record<(typeof PRODUCT_CAPTURE_MATERIALS)[number]["id"], string>
> = {
  "table-tent": "object-bottom",
  "receipt-label": "object-bottom",
}

function MediaPane({
  material,
  reduceMotion,
}: {
  material: ProductCaptureMaterial
  reduceMotion: boolean | null
}) {
  const imageSrc = materialImages[material.id]
  const objectPosition =
    MEDIA_OBJECT_POSITION[material.id] ?? "object-center"

  return (
    <div className="relative h-[min(70vw,420px)] w-full overflow-hidden rounded-[12px] lg:h-full lg:min-h-0 lg:flex-1 lg:rounded-none">
      <AnimatePresence initial={false} mode="sync">
        <motion.div
          key={material.id}
          initial={reduceMotion ? false : { opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={reduceMotion ? undefined : { opacity: 0 }}
          transition={{
            duration: reduceMotion ? 0 : CROSSFADE_MS,
            ease: "easeInOut",
          }}
          className="absolute inset-0"
        >
          <div
            aria-hidden
            className="absolute inset-0 bg-[linear-gradient(180deg,rgb(240,240,240)_0%,rgb(36,138,68)_139%)]"
          />
          {/*
            Always fill the media frame (no side letterbox). Portrait sources
            crop top/bottom; landscape sources crop the sides.
          */}
          <img
            src={imageSrc}
            alt={material.imageAlt}
            width={750}
            height={661}
            className={cn(
              "absolute inset-0 size-full object-cover",
              objectPosition,
            )}
            draggable={false}
            decoding="async"
          />
          <div
            aria-hidden
            className="absolute inset-0 bg-[linear-gradient(178.4deg,rgba(20,20,20,0)_43%,rgb(20,20,20)_98.5%),linear-gradient(90deg,rgba(0,0,0,0.2),rgba(0,0,0,0.2))]"
          />
          <p className="absolute bottom-8 left-8 m-0 max-w-[307px] font-jakarta text-base font-semibold leading-[22px] text-white sm:bottom-10 sm:left-10 lg:bottom-[50px] lg:left-[50px]">
            {material.caption}
          </p>
        </motion.div>
      </AnimatePresence>
    </div>
  )
}

export type CaptureMaterialsIntro = {
  eyebrow: string
  title: string
  body: string
  titleId?: string
}

function MaterialsIntro({ intro }: { intro: CaptureMaterialsIntro }) {
  return (
    <div className="flex w-full shrink-0 flex-col gap-[18px] text-[#141414]">
      <div className="flex flex-col gap-3.5">
        <p className="m-0 font-jakarta text-sm font-medium leading-4">
          {intro.eyebrow}
        </p>
        <h2
          id={intro.titleId}
          className="m-0 max-w-[565px] font-jakarta text-[28px] font-medium leading-9 lg:text-[36px] lg:leading-[48px]"
        >
          {intro.title}
        </h2>
      </div>
      <p className="m-0 max-w-[588px] font-sans text-base font-normal leading-[22px] lg:text-[18px] lg:leading-6">
        {intro.body}
      </p>
    </div>
  )
}

function PanelChrome({
  activeIndex,
  onSelect,
  listTranslateY,
  reduceMotion,
  listScrollable,
  intro,
}: {
  activeIndex: number
  onSelect: (index: number) => void
  listTranslateY: number
  reduceMotion: boolean | null
  listScrollable?: boolean
  intro?: CaptureMaterialsIntro
}) {
  const material =
    PRODUCT_CAPTURE_MATERIALS[activeIndex] ?? PRODUCT_CAPTURE_MATERIALS[0]

  return (
    <div
      className={cn(
        "flex w-full flex-col overflow-hidden rounded-[12px] bg-[#f0f0f0] lg:h-[661px] lg:flex-row lg:items-stretch",
      )}
      role="region"
      aria-label="QR capture materials"
      aria-labelledby={intro?.titleId}
    >
      <div className="flex min-h-0 w-full flex-col gap-8 overflow-hidden px-5 py-8 sm:px-8 lg:w-[min(100%,798px)] lg:flex-1 lg:gap-[50px] lg:px-[50px] lg:pt-[50px] lg:pb-0">
        {intro != null ? <MaterialsIntro intro={intro} /> : null}
        {listScrollable ? (
          <MobileMaterialsList
            activeIndex={activeIndex}
            onSelect={onSelect}
          />
        ) : (
          <MaterialsList
            activeIndex={activeIndex}
            onSelect={onSelect}
            translateY={listTranslateY}
          />
        )}
      </div>
      <MediaPane material={material} reduceMotion={reduceMotion} />
    </div>
  )
}

type ProductCaptureMaterialsPanelProps = {
  /**
   * Optional copy above the materials list (Home QR materials `4902:19649`).
   * Product Capture omits this — header lives outside the panel.
   */
  intro?: CaptureMaterialsIntro
}

/**
 * Figma Capture materials panel `6106:17567` … `6106:18055`.
 * Desktop (lg+): sticky scroll-driven active material. Below lg: tap to select.
 */
export function ProductCaptureMaterialsPanel({
  intro,
}: ProductCaptureMaterialsPanelProps = {}) {
  const isLgUp = useIsLgUp()
  const reduceMotion = useReducedMotion()
  const trackRef = useRef<HTMLDivElement>(null)
  const [activeIndex, setActiveIndex] = useState(0)
  const [stickyTopPx, setStickyTopPx] = useState(() =>
    typeof window !== "undefined"
      ? stickyTopForViewport(window.innerHeight)
      : HEADER_CLEARANCE_PX,
  )

  const syncFromScroll = useEffectEvent(() => {
    const track = trackRef.current
    if (track == null) {
      return
    }
    const rect = track.getBoundingClientRect()
    const scrollable = TRACK_HEIGHT_PX - PANEL_HEIGHT_PX
    if (scrollable <= 0) {
      return
    }
    // Progress starts when the track top reaches the sticky offset (panel
    // centred in the viewport), not when it hits the nav.
    const scrolled = Math.max(
      0,
      Math.min(scrollable, stickyTopPx - rect.top),
    )
    const next = clampIndex(Math.floor((scrolled / scrollable) * MATERIAL_COUNT))
    setActiveIndex((prev) => (prev === next ? prev : next))
  })

  useEffect(() => {
    if (!isLgUp) {
      return
    }
    const onResize = () => {
      setStickyTopPx(stickyTopForViewport(window.innerHeight))
    }
    onResize()
    syncFromScroll()
    window.addEventListener("scroll", syncFromScroll, { passive: true })
    window.addEventListener("resize", onResize)
    return () => {
      window.removeEventListener("scroll", syncFromScroll)
      window.removeEventListener("resize", onResize)
    }
  }, [isLgUp])

  useEffect(() => {
    if (!isLgUp) {
      return
    }
    syncFromScroll()
  }, [isLgUp, stickyTopPx])

  const scrollToIndex = (index: number) => {
    const next = clampIndex(index)
    if (!isLgUp) {
      setActiveIndex(next)
      return
    }
    const track = trackRef.current
    if (track == null) {
      setActiveIndex(next)
      return
    }
    const scrollable = TRACK_HEIGHT_PX - PANEL_HEIGHT_PX
    const step = scrollable / MATERIAL_COUNT
    const trackTop = window.scrollY + track.getBoundingClientRect().top
    window.scrollTo({
      top: trackTop - stickyTopPx + next * step,
      behavior: reduceMotion ? "auto" : "smooth",
    })
  }

  const listTranslateY = isLgUp
    ? -activeIndex * LIST_ROW_STRIDE_PX
    : 0

  if (!isLgUp) {
    return (
      <PanelChrome
        activeIndex={activeIndex}
        onSelect={scrollToIndex}
        listTranslateY={0}
        reduceMotion={reduceMotion}
        listScrollable
        intro={intro}
      />
    )
  }

  return (
    <div
      ref={trackRef}
      className="relative w-full"
      style={{ height: TRACK_HEIGHT_PX }}
    >
      <div className="sticky w-full" style={{ top: stickyTopPx }}>
        <PanelChrome
          activeIndex={activeIndex}
          onSelect={scrollToIndex}
          listTranslateY={listTranslateY}
          reduceMotion={reduceMotion}
          intro={intro}
        />
      </div>
    </div>
  )
}

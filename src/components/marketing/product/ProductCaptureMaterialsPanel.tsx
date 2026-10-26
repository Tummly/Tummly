import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type CSSProperties,
  type RefObject,
} from "react"
import { AnimatePresence, motion, useReducedMotion } from "framer-motion"
import { ChevronRight } from "lucide-react"

import { PRODUCT_CAPTURE_MATERIALS } from "@/content/marketing/productPage"
import { remoteSimpleImg } from "@/lib/marketingMediaPictures"
import { cn } from "@/lib/utils"

const MATERIAL_COUNT = PRODUCT_CAPTURE_MATERIALS.length
const CROSSFADE_MS = 0.25
/** List row: 32px type + 22px gap (Figma). */
const LIST_ROW_STRIDE_PX = 54
/** Wheel distance (px) before advancing one material while the list is hovered. */
const WHEEL_STEP_PX = 40
/** Ignore further wheel steps briefly so trackpad inertia cannot skip items. */
const WHEEL_STEP_COOLDOWN_MS = 320
const LG_MQ = "(min-width: 1024px)"

function normalizeWheelDeltaY(event: WheelEvent) {
  if (event.deltaMode === WheelEvent.DOM_DELTA_LINE) {
    return event.deltaY * 16
  }
  if (event.deltaMode === WheelEvent.DOM_DELTA_PAGE) {
    return event.deltaY * WHEEL_STEP_PX
  }
  return event.deltaY
}

const materialImages = Object.fromEntries(
  PRODUCT_CAPTURE_MATERIALS.map((material) => [
    material.id,
    remoteSimpleImg(material.mediaId),
  ]),
) as Record<
  (typeof PRODUCT_CAPTURE_MATERIALS)[number]["id"],
  ReturnType<typeof remoteSimpleImg>
>
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

type CaptureMaterial = (typeof PRODUCT_CAPTURE_MATERIALS)[number]

function MaterialListItem({
  material,
  active,
  onSelect,
}: {
  material: CaptureMaterial
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
          /* Mobile Figma `6241:18095`: 24px / ~30px. Desktop keeps 32px. */
          "font-jakarta text-2xl leading-[30px] whitespace-nowrap sm:text-[28px] sm:leading-[normal] lg:text-[32px]",
          active
            ? "font-semibold text-black"
            : "font-medium text-[#9d9d9d]",
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
  listRef,
  onListPointerEnter,
  onListPointerLeave,
  className,
  style,
  showFades = true,
}: {
  activeIndex: number
  onSelect: (index: number) => void
  translateY: number
  listRef?: RefObject<HTMLDivElement | null>
  onListPointerEnter?: () => void
  onListPointerLeave?: () => void
  className?: string
  style?: CSSProperties
  /** Desktop windowed list only — Home mobile shows the full catalogue. */
  showFades?: boolean
}) {
  // Top fade only after the list has scrolled — otherwise the active first
  // row ("QR Guest Cards") sits under the gradient and looks washed out.
  const showTopFade = showFades && activeIndex > 0
  const showBottomFade = showFades && activeIndex < MATERIAL_COUNT - 1

  return (
    <div
      ref={listRef}
      onPointerEnter={onListPointerEnter}
      onPointerLeave={onListPointerLeave}
      className={cn(
        "relative flex flex-col",
        showFades && "min-h-0 flex-1 overflow-hidden",
        className,
      )}
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
    <div className="relative max-h-[min(612px,70vh)]">
      {showTopFade ? (
        <div
          aria-hidden
          className="pointer-events-none absolute inset-x-0 top-0 z-10 h-10 bg-linear-to-b from-[#f0f0f0] to-transparent"
        />
      ) : null}
      <div
        ref={scrollerRef}
        className="max-h-[min(612px,70vh)] overflow-y-auto overscroll-contain [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
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
  compact = false,
}: {
  material: CaptureMaterial
  reduceMotion: boolean | null
  /** Home mobile Figma `6241:18115`: 255×163 floating card. */
  compact?: boolean
}) {
  const image = materialImages[material.id]
  const objectPosition =
    MEDIA_OBJECT_POSITION[material.id] ?? "object-center"

  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-[12px]",
        compact
          ? "size-full"
          : "h-[min(70vw,420px)] w-full lg:h-full lg:min-h-0 lg:flex-1 lg:rounded-none",
      )}
    >
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
            src={image.src}
            srcSet={image.srcSet}
            sizes={
              compact
                ? "255px"
                : "(min-width: 1024px) 50vw, 100vw"
            }
            alt={material.imageAlt}
            width={image.width}
            height={image.height}
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
          <p
            className={cn(
              "absolute m-0 font-jakarta font-semibold text-white",
              compact
                ? "bottom-5 left-5 max-w-[208px] text-xs leading-[18px]"
                : "bottom-8 left-8 max-w-[307px] text-base leading-[22px] sm:bottom-10 sm:left-10 lg:bottom-[50px] lg:left-[50px]",
            )}
          >
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
          className="m-0 max-w-[565px] font-jakarta text-[28px] font-medium leading-[38px] lg:text-[36px] lg:leading-[48px]"
        >
          {intro.title}
        </h2>
      </div>
      <p className="m-0 max-w-[588px] font-sans text-sm font-normal leading-5 lg:text-[18px] lg:leading-6">
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
  listRef,
  onListPointerEnter,
  onListPointerLeave,
  intro,
}: {
  activeIndex: number
  onSelect: (index: number) => void
  listTranslateY: number
  reduceMotion: boolean | null
  listScrollable?: boolean
  listRef?: RefObject<HTMLDivElement | null>
  onListPointerEnter?: () => void
  onListPointerLeave?: () => void
  intro?: CaptureMaterialsIntro
}) {
  const material =
    PRODUCT_CAPTURE_MATERIALS[activeIndex] ?? PRODUCT_CAPTURE_MATERIALS[0]
  /** Home catalogue: floating card on mobile (`6241:18115`). Product: stacked media. */
  const homeFloatingMedia = intro != null

  return (
    <div
      className={cn(
        "relative flex w-full flex-col overflow-hidden bg-[#f0f0f0] lg:h-[661px] lg:flex-row lg:items-stretch lg:rounded-[12px]",
        /* Home mobile Figma `6241:18094`: 12px radius on the grey card. */
        "rounded-[12px]",
      )}
      role="region"
      aria-label="QR capture materials"
      aria-labelledby={intro?.titleId}
    >
      <div
        className={cn(
          "flex min-h-0 w-full flex-col overflow-hidden sm:px-8 lg:w-[min(100%,798px)] lg:flex-1 lg:gap-[50px] lg:px-[50px] lg:pt-[50px] lg:pb-0",
          /* Home mobile Figma `6241:18095`: 30px sides, 50px top, 40px gap, no bottom pad
           * (outer `6241:18093` supplies the 20px white pad below the card). */
          intro != null
            ? "gap-10 px-[30px] pt-[50px] pb-0 sm:px-8 lg:gap-[50px] lg:px-[50px] lg:pt-[50px]"
            : "gap-8 px-5 py-8",
        )}
      >
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
            listRef={listRef}
            onListPointerEnter={onListPointerEnter}
            onListPointerLeave={onListPointerLeave}
            showFades={listRef != null}
          />
        )}
      </div>
      {homeFloatingMedia ? (
        <>
          {/* Mobile Figma `6241:18115`: 255×163 floating card over the list. */}
          <div className="pointer-events-none absolute top-[498px] right-2.5 z-10 h-[163px] w-[255px] overflow-hidden rounded-[12px] lg:hidden">
            <MediaPane
              material={material}
              reduceMotion={reduceMotion}
              compact
            />
          </div>
          <div className="max-lg:hidden lg:min-h-0 lg:flex-1">
            <MediaPane material={material} reduceMotion={reduceMotion} />
          </div>
        </>
      ) : (
        /* Product / Pricing: MediaPane is absolute-fill — wrapper must take
         * remaining flex width or the right pane collapses to 0 on lg+. */
        <div className="min-h-0 w-full lg:flex-1">
          <MediaPane material={material} reduceMotion={reduceMotion} />
        </div>
      )}
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
 * Desktop: click selects immediately; wheel advances only while the left list
 * is hovered (page scroll is normal otherwise). Below lg: tap to select.
 */
export function ProductCaptureMaterialsPanel({
  intro,
}: ProductCaptureMaterialsPanelProps = {}) {
  const isLgUp = useIsLgUp()
  const reduceMotion = useReducedMotion()
  const listRef = useRef<HTMLDivElement>(null)
  const [activeIndex, setActiveIndex] = useState(0)
  const [listHovered, setListHovered] = useState(false)
  const activeIndexRef = useRef(activeIndex)
  const wheelAccRef = useRef(0)
  const lastWheelStepAtRef = useRef(0)

  activeIndexRef.current = activeIndex

  const selectIndex = (index: number) => {
    setActiveIndex(clampIndex(index))
  }

  useEffect(() => {
    if (!isLgUp || !listHovered) {
      wheelAccRef.current = 0
      return
    }

    const list = listRef.current
    if (list == null) {
      return
    }

    const onWheel = (event: WheelEvent) => {
      const index = activeIndexRef.current
      const delta = normalizeWheelDeltaY(event)

      if (delta > 0 && index >= MATERIAL_COUNT - 1) {
        return
      }
      if (delta < 0 && index <= 0) {
        return
      }

      event.preventDefault()

      const now = performance.now()
      if (now - lastWheelStepAtRef.current < WHEEL_STEP_COOLDOWN_MS) {
        // Drop inertia leftovers so the next gesture starts clean.
        wheelAccRef.current = 0
        return
      }

      wheelAccRef.current += delta
      if (Math.abs(wheelAccRef.current) < WHEEL_STEP_PX) {
        return
      }

      // One material per gesture — large deltas must not skip the next item.
      const direction = wheelAccRef.current > 0 ? 1 : -1
      wheelAccRef.current = 0
      lastWheelStepAtRef.current = now

      const next = clampIndex(index + direction)
      if (next !== index) {
        activeIndexRef.current = next
        setActiveIndex(next)
      }
    }

    list.addEventListener("wheel", onWheel, { passive: false })
    return () => {
      list.removeEventListener("wheel", onWheel)
    }
  }, [isLgUp, listHovered])

  const listTranslateY = isLgUp ? -activeIndex * LIST_ROW_STRIDE_PX : 0
  /** Home catalogue (`6241:18095`) shows the full list; Product keeps a max-height scroller. */
  const listScrollable = !isLgUp && intro == null

  return (
    <PanelChrome
      activeIndex={activeIndex}
      onSelect={selectIndex}
      listTranslateY={listTranslateY}
      reduceMotion={reduceMotion}
      listScrollable={listScrollable}
      listRef={isLgUp ? listRef : undefined}
      onListPointerEnter={
        isLgUp
          ? () => {
              setListHovered(true)
            }
          : undefined
      }
      onListPointerLeave={
        isLgUp
          ? () => {
              setListHovered(false)
            }
          : undefined
      }
      intro={intro}
    />
  )
}

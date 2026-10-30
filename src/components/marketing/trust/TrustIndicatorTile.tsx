import { useEffect, useReducer, useRef } from "react"
import { ExternalLink } from "lucide-react"
import { Popover as PopoverPrimitive } from "radix-ui"
import { Link } from "react-router-dom"

import { MarketingNavLink } from "@/components/marketing/MarketingNavLink"
import { TrustIndicatorIcon } from "@/components/marketing/trust/TrustIndicatorIcon"
import {
  TRUST_TOOLTIP_CLOSE_DELAY_MS,
  TRUST_TOOLTIP_INITIAL_STATE,
  trustTooltipReducer,
} from "@/components/marketing/trust/trustTooltipReducer"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import type {
  PublicTrustCta,
  PublicTrustIndicator,
} from "@/lib/marketing/publicTrustIndicators"
import { cn } from "@/lib/utils"

type TrustIndicatorTileProps = {
  indicator: PublicTrustIndicator
  variant: "footer" | "carousel"
}

/** Figma tile border `#b3b3b3` has no design token; the other colours map to `--op-color-*`. */
const TILE_CLASS =
  "flex shrink-0 cursor-pointer flex-col items-center gap-1.5 rounded-[7px] border-[0.8px] border-[#b3b3b3] bg-transparent p-2 text-center font-sans font-normal text-[var(--op-color-black)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--op-color-black)]/40"

const TILE_WIDTH_CLASS = { footer: "w-[94px]", carousel: "w-[120px]" } as const

/** Figma bubble: fill `#d5d5d5` (`--op-color-gray-300`), radius 4px, padding 12px, Figma drop-shadow stack. Text 12/16 (Ruling 20). */
const BUBBLE_CLASS =
  "w-max max-w-[min(260px,calc(100vw-24px))] gap-2 rounded-[4px] bg-[var(--op-color-gray-300)] p-3 text-[12px] leading-4 text-[var(--op-color-black)] shadow-none ring-0 [filter:drop-shadow(4px_23px_3.5px_rgba(0,0,0,0))_drop-shadow(3px_15px_3px_rgba(0,0,0,0.01))_drop-shadow(1px_8px_2.5px_rgba(0,0,0,0.03))_drop-shadow(1px_4px_2px_rgba(0,0,0,0.05))_drop-shadow(0px_1px_1px_rgba(0,0,0,0.05))]"

const CTA_CLASS =
  "inline-flex w-fit items-center gap-1 rounded-sm text-[12px] leading-4 font-normal text-[var(--op-color-black)] underline underline-offset-2 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--op-color-black)]/40"

/**
 * Footer and carousel tile with the status text and a toggletip. Opens on mouse
 * hover, keyboard focus or click; the open rules are in `trustTooltipReducer`.
 */
export function TrustIndicatorTile({ indicator, variant }: TrustIndicatorTileProps) {
  const [state, dispatch] = useReducer(trustTooltipReducer, TRUST_TOOLTIP_INITIAL_STATE)
  const closedByEscape = useRef(false)
  const cta = indicator.status === "Verified" ? indicator.cta : null
  const verified = indicator.status === "Verified"

  // Only a mouse leave arms the close timer. Re-entering the trigger or the content
  // clears `closePending`, and that cleanup clears the timer. Unmount does the same.
  useEffect(() => {
    if (!state.closePending) return
    const timer = window.setTimeout(
      () => dispatch({ type: "close-timer" }),
      TRUST_TOOLTIP_CLOSE_DELAY_MS,
    )
    return () => {
      window.clearTimeout(timer)
    }
  }, [state.closePending])

  return (
    <Popover
      open={state.open}
      onOpenChange={(next) => {
        dispatch({ type: next ? "click" : "close" })
      }}
    >
      <PopoverTrigger asChild>
        <button
          type="button"
          aria-label={`${indicator.title}, ${indicator.statusLabel}`}
          className={cn(TILE_CLASS, TILE_WIDTH_CLASS[variant])}
          onPointerEnter={(event) => {
            if (event.pointerType === "mouse") dispatch({ type: "hover-enter" })
          }}
          onPointerLeave={(event) => {
            if (event.pointerType === "mouse") dispatch({ type: "pointer-leave" })
          }}
          onFocus={(event) => {
            if (event.currentTarget.matches(":focus-visible")) {
              dispatch({ type: "focus-visible" })
            }
          }}
        >
          <TrustIndicatorIcon
            iconKey={indicator.iconKey}
            imageUrl={indicator.imageUrl}
            altText={indicator.altText}
            size={variant === "carousel" ? "lg" : "sm"}
          />
          <span className="line-clamp-3 w-full text-[12px] leading-[15px] font-medium">
            {indicator.title}
          </span>
          <span className="inline-flex items-center gap-1 text-[11px] leading-[14px]">
            {verified ? (
              <span
                aria-hidden="true"
                className="size-1.5 shrink-0 rounded-full bg-[var(--op-color-black)]"
              />
            ) : null}
            <span
              className={
                verified ? "text-[var(--op-color-black)]" : "text-[var(--op-color-gray-700)]"
              }
            >
              {indicator.statusLabel}
            </span>
          </span>
        </button>
      </PopoverTrigger>
      <PopoverContent
        side="top"
        align="start"
        sideOffset={8}
        collisionPadding={12}
        aria-label={indicator.title}
        className={BUBBLE_CLASS}
        onOpenAutoFocus={(event) => {
          // Only a click moves focus into the bubble, and only when there is a CTA to reach.
          if (state.source !== "click" || cta === null) event.preventDefault()
        }}
        onCloseAutoFocus={(event) => {
          // Escape returns focus to the trigger (Radix default). Other closes must not move it.
          if (!closedByEscape.current) event.preventDefault()
          closedByEscape.current = false
        }}
        onEscapeKeyDown={() => {
          closedByEscape.current = true
        }}
        onPointerEnter={(event) => {
          if (event.pointerType === "mouse") dispatch({ type: "content-enter" })
        }}
        onPointerLeave={(event) => {
          if (event.pointerType === "mouse") dispatch({ type: "pointer-leave" })
        }}
      >
        <TrustIndicatorTooltipBody indicator={indicator} />
        <PopoverPrimitive.Arrow
          width={14}
          height={12}
          aria-hidden="true"
          className="block fill-[var(--op-color-gray-300)]"
        />
      </PopoverContent>
    </Popover>
  )
}

/** Tooltip text, then the CTA. The CTA shows only for a Verified item that has one. */
export function TrustIndicatorTooltipBody({ indicator }: { indicator: PublicTrustIndicator }) {
  const cta = indicator.status === "Verified" ? indicator.cta : null
  return (
    <>
      {indicator.tooltip !== "" ? (
        <p className="m-0 text-[12px] leading-4 text-[var(--op-color-black)]">{indicator.tooltip}</p>
      ) : null}
      {cta !== null ? <TrustCta cta={cta} /> : null}
    </>
  )
}

function TrustCta({ cta }: { cta: PublicTrustCta }) {
  if (cta.type === "CookieSettings") {
    return (
      <MarketingNavLink
        label={cta.label}
        href={{ kind: "cookie-settings" }}
        className={CTA_CLASS}
      />
    )
  }
  if (cta.href === null) return null

  if (cta.type === "InternalPage") {
    return (
      <Link to={cta.href} className={CTA_CLASS}>
        {cta.label}
      </Link>
    )
  }

  return (
    <a href={cta.href} target="_blank" rel="noopener noreferrer" className={CTA_CLASS}>
      {cta.label}
      <ExternalLink aria-hidden="true" className="size-3 shrink-0" />
      <span className="sr-only"> (opens in a new tab)</span>
    </a>
  )
}

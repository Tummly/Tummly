import { useEffect, useId, useReducer, useRef } from "react"
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
import {
  Popover,
  PopoverAnchor,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover"
import type {
  PublicTrustCta,
  PublicTrustIndicator,
} from "@/lib/marketing/publicTrustIndicators"
import { cn } from "@/lib/utils"

type TrustIndicatorTileProps = {
  indicator: PublicTrustIndicator
  variant: "footer" | "carousel"
}

/**
 * Literal hex, not `--op-color-*`: marketing routes do not set `html.op`, so those tokens are
 * undefined there. The Figma tile border `#b3b3b3` has no token either.
 */
const TILE_CLASS =
  "flex shrink-0 cursor-pointer flex-col items-center gap-1.5 rounded-[7px] border-[0.825px] border-[#b3b3b3] bg-transparent p-2 text-center font-sans font-normal text-[#141414] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40"

/** Footer tiles have a fixed Figma width. Carousel tiles fill the slot the caller gives them. */
const TILE_WIDTH_CLASS = { footer: "w-[94px]", carousel: "w-full" } as const

/** Figma bubble: fill `#d5d5d5`, radius 4px, padding 12px, Figma drop-shadow stack. Text 12/16 (Ruling 20). */
const BUBBLE_CLASS =
  "w-max max-w-[min(260px,calc(100vw-24px))] gap-2 rounded-[4px] bg-[#d5d5d5] p-3 text-[12px] leading-4 text-[#141414] shadow-none ring-0 [filter:drop-shadow(4px_23px_3.5px_rgba(0,0,0,0))_drop-shadow(3px_15px_3px_rgba(0,0,0,0.01))_drop-shadow(1px_8px_2.5px_rgba(0,0,0,0.03))_drop-shadow(1px_4px_2px_rgba(0,0,0,0.05))_drop-shadow(0px_1px_1px_rgba(0,0,0,0.05))]"

const CTA_CLASS =
  "inline-flex w-fit items-center gap-1 rounded-sm text-[12px] leading-4 font-normal text-[#141414] underline underline-offset-2 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40"

/**
 * Footer and carousel tile with the status text and a toggletip. Opens on mouse
 * hover, keyboard focus or click; the open rules are in `trustTooltipReducer`.
 */
export function TrustIndicatorTile({ indicator, variant }: TrustIndicatorTileProps) {
  const [state, dispatch] = useReducer(trustTooltipReducer, TRUST_TOOLTIP_INITIAL_STATE)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const contentRef = useRef<HTMLDivElement>(null)
  const closedByEscape = useRef(false)
  // The bubble is portalled, so screen readers get the tooltip text from this sr-only copy.
  const descriptionId = useId()
  const verified = indicator.status === "Verified"
  const cta = verified ? indicator.cta : null

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

  // After Escape or an outside pointer, Radix may return focus to the trigger. Keep that
  // focus from reopening the tooltip until the next pointer or key press. Capture
  // listeners, so the Escape key that closed it does not clear the flag.
  useEffect(() => {
    if (!state.suppressFocus) return
    const clear = () => {
      dispatch({ type: "input" })
    }
    document.addEventListener("pointerdown", clear, true)
    document.addEventListener("keydown", clear, true)
    return () => {
      document.removeEventListener("pointerdown", clear, true)
      document.removeEventListener("keydown", clear, true)
    }
  }, [state.suppressFocus])

  // A click that takes over a hover or focus open moves focus into the bubble, so the CTA
  // is reachable. Without a CTA the focus stays on the trigger.
  useEffect(() => {
    if (!state.open || state.source !== "click") return
    contentRef.current?.querySelector<HTMLElement>("a[href], button")?.focus()
  }, [state.open, state.source])

  return (
    <div className={cn("relative", variant === "carousel" ? "flex w-full" : "inline-flex")}>
      <Popover
        open={state.open}
        onOpenChange={(next) => {
          // A trigger click goes through `onClick` below, so this only sees dismissals.
          if (!next) dispatch({ type: "close" })
        }}
      >
        <PopoverTrigger
          asChild
          onClick={(event) => {
            // Blocks Radix's own toggle. The reducer decides what a click does.
            event.preventDefault()
            dispatch({ type: "click" })
          }}
        >
          <button
            ref={triggerRef}
            type="button"
            aria-label={`${indicator.title}, ${indicator.statusLabel}`}
            aria-describedby={indicator.tooltip !== "" ? descriptionId : undefined}
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
              status={indicator.status}
            />
            <span className="line-clamp-3 w-full text-[12px] leading-[15px] font-medium">
              {indicator.title}
            </span>
            <span className="inline-flex items-center gap-1 text-[11px] leading-[14px]">
              {verified ? (
                <span
                  aria-hidden="true"
                  className="size-1.5 shrink-0 rounded-full bg-[#141414]"
                />
              ) : null}
              <span
                className={
                  verified ? "text-[#141414]" : "text-[#5d5d5d]"
                }
              >
                {indicator.statusLabel}
              </span>
            </span>
          </button>
        </PopoverTrigger>
        {/*
          Figma: the arrow sits about 21px from the bubble's left edge. This 42px anchor is at the
          tile's left edge, so its centre is 21px in; `alignOffset={0}` keeps the bubble on the tile.
          It goes after the trigger on purpose: refs attach in tree order, so this anchor registers
          last. Radix remounts the trigger once it sees a custom anchor, and an earlier anchor would
          keep pointing at the detached first button.
        */}
        <PopoverAnchor className="pointer-events-none absolute inset-y-0 left-0 w-[42px]" />
        <PopoverContent
          ref={contentRef}
          side="top"
          align="start"
          alignOffset={0}
          sideOffset={8}
          collisionPadding={12}
          aria-label={indicator.title}
          className={BUBBLE_CLASS}
          onOpenAutoFocus={(event) => {
            // Only a click moves focus into the bubble, and only when there is a CTA to reach.
            if (state.source !== "click" || cta === null) event.preventDefault()
          }}
          onCloseAutoFocus={(event) => {
            // Radix's own focus-return to the trigger did not run after Escape from the CTA, so
            // this handler does it. Escape returns focus to the trigger; other closes leave it.
            event.preventDefault()
            if (closedByEscape.current) triggerRef.current?.focus()
            closedByEscape.current = false
          }}
          onEscapeKeyDown={() => {
            closedByEscape.current = true
            dispatch({ type: "dismiss" })
          }}
          onPointerDownOutside={(event) => {
            // The trigger's own pointerdown is a click, not an outside dismissal.
            if (!triggerRef.current?.contains(event.target as Node)) {
              dispatch({ type: "dismiss" })
            }
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
            className="block fill-[#d5d5d5]"
          />
        </PopoverContent>
      </Popover>
      {indicator.tooltip !== "" ? (
        <span id={descriptionId} className="sr-only">
          {indicator.tooltip}
        </span>
      ) : null}
    </div>
  )
}

/** Tooltip text, then the CTA. The CTA shows only for a Verified item that has one. */
export function TrustIndicatorTooltipBody({ indicator }: { indicator: PublicTrustIndicator }) {
  const cta = indicator.status === "Verified" ? indicator.cta : null
  return (
    <>
      {indicator.tooltip !== "" ? (
        <p className="m-0 text-[12px] leading-4 text-[#141414]">{indicator.tooltip}</p>
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

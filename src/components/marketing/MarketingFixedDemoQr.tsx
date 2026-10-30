import { motion, useReducedMotion } from "framer-motion"
import { useState } from "react"
import { Link } from "react-router-dom"

import demoQrImage from "@/assets/images/marketing/home/demo-qr.png"
import { HOME_DEMO_QR } from "@/content/marketing/homePage"
import { cn } from "@/lib/utils"

type MarketingFixedDemoQrProps = {
  className?: string
}

/**
 * Figma Scan `5559:7309` (Variant2 collapsed) → `5557:7269` (Variant3 expanded):
 * ON_HOVER CHANGE_TO, SMART_ANIMATE, EASE_IN, 300ms.
 */
const COLLAPSED_WIDTH_PX = 121
const EXPANDED_WIDTH_PX = 271
const PANEL_HEIGHT_PX = 124
const TITLE_WIDTH_PX = 137
/** Figma SMART_ANIMATE duration (Variant2 → Variant3). */
const EXPAND_S = 0.3
/**
 * Hold title opacity until the panel is nearly open so wrap stays stable
 * (animating title width mid-expand reflows lines).
 */
const TITLE_REVEAL_DELAY_S = 0.2
const TITLE_FADE_S = 0.1

/**
 * Fixed demo QR — Figma Demo `5661:30036` / Scan variants.
 * Collapsed chip; hover/focus smart-animates to expanded title + QR.
 * Desktop only (`lg+`). Offset: right 60px, bottom 35px.
 */
export function MarketingFixedDemoQr({ className }: MarketingFixedDemoQrProps) {
  const reduceMotion = useReducedMotion()
  const [expanded, setExpanded] = useState(false)
  const open = () => setExpanded(true)
  const close = () => setExpanded(false)

  const widthTransition = {
    duration: reduceMotion ? 0 : EXPAND_S,
    ease: "easeIn" as const,
  }

  const titleTransition = {
    duration: reduceMotion ? 0 : TITLE_FADE_S,
    delay: reduceMotion ? 0 : expanded ? TITLE_REVEAL_DELAY_S : 0,
    ease: "easeIn" as const,
  }

  return (
    <div
      className={cn(
        "pointer-events-auto fixed right-[60px] bottom-[35px] z-40 hidden lg:block",
        className,
      )}
      onMouseEnter={open}
      onMouseLeave={close}
      onFocusCapture={open}
      onBlurCapture={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
          close()
        }
      }}
    >
      <Link
        to={HOME_DEMO_QR.href}
        aria-label={HOME_DEMO_QR.ariaLabel}
        aria-expanded={expanded}
        className={cn(
          "block overflow-hidden rounded-[12px] bg-[#141414]",
          "shadow-[0_8px_24px_rgba(0,0,0,0.18)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/40",
          "focus-visible:ring-offset-2",
        )}
      >
        <motion.div
          initial={false}
          animate={{
            width: expanded ? EXPANDED_WIDTH_PX : COLLAPSED_WIDTH_PX,
          }}
          transition={widthTransition}
          className="flex items-center justify-end overflow-hidden"
          style={{ height: PANEL_HEIGHT_PX }}
        >
          {/*
            Fixed title width (Figma 137px) — parent clips during expand.
            Opacity waits until near end so lines do not reflow mid-motion.
          */}
          <motion.p
            initial={false}
            animate={{ opacity: expanded ? 1 : 0 }}
            transition={titleTransition}
            aria-hidden={!expanded}
            className="m-0 ml-[13px] shrink-0 text-center font-jakarta text-[18px] font-bold leading-[21px] text-white"
            style={{ width: TITLE_WIDTH_PX }}
          >
            {HOME_DEMO_QR.title}
          </motion.p>

          <span className="flex h-full shrink-0 items-center px-[13px] py-3.5">
            <span className="block size-[95px] overflow-hidden rounded-[8px] bg-white p-1">
              <img
                src={demoQrImage}
                alt=""
                width={87}
                height={87}
                decoding="async"
                className="block size-full object-contain"
              />
            </span>
          </span>
        </motion.div>
      </Link>
    </div>
  )
}

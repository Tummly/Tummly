import { useEffect, useLayoutEffect, useState } from "react"
import { createPortal } from "react-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { cn } from "@/lib/utils"

type SpotlightCoachmarkProps = {
  target: string
  chapterLabel: string
  stepLabel: string
  title: string
  body: string
  ctaLabel: string
  onCta: () => void
  onSkip?: () => void
}

type Box = { top: number; left: number; width: number; height: number }

function readTargetBox(target: string): Box | null {
  const el = document.querySelector<HTMLElement>(`[data-demo-tour="${target}"]`)
  if (!el) return null
  const rect = el.getBoundingClientRect()
  if (rect.width < 2 && rect.height < 2) return null
  return {
    top: rect.top + window.scrollY,
    left: rect.left + window.scrollX,
    width: rect.width,
    height: rect.height,
  }
}

/** DOM-anchored coachmark with spotlight cutout (no tour library). */
export function SpotlightCoachmark({
  target,
  chapterLabel,
  stepLabel,
  title,
  body,
  ctaLabel,
  onCta,
  onSkip,
}: SpotlightCoachmarkProps) {
  const [box, setBox] = useState<Box | null>(null)
  const reduceMotion =
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches

  useLayoutEffect(() => {
    const update = () => setBox(readTargetBox(target))
    update()
    const id = window.setInterval(update, 400)
    window.addEventListener("resize", update)
    window.addEventListener("scroll", update, true)
    return () => {
      window.clearInterval(id)
      window.removeEventListener("resize", update)
      window.removeEventListener("scroll", update, true)
    }
  }, [target])

  useEffect(() => {
    const el = document.querySelector<HTMLElement>(`[data-demo-tour="${target}"]`)
    el?.scrollIntoView({ block: "nearest", inline: "nearest", behavior: reduceMotion ? "auto" : "smooth" })
  }, [target, reduceMotion])

  if (typeof document === "undefined") {
    return null
  }

  const pad = 8
  const tipTop = box
    ? Math.min(
        box.top + box.height + pad + 12,
        window.scrollY + window.innerHeight - 220
      )
    : window.scrollY + window.innerHeight / 3
  const tipLeft = box
    ? Math.max(16, Math.min(box.left, window.innerWidth - 360))
    : 24

  return createPortal(
    <div className="pointer-events-none fixed inset-0 z-[85]">
      {box ? (
        <div
          aria-hidden
          className={cn(
            "absolute rounded-md ring-2 ring-[#14a74a] ring-offset-2 ring-offset-transparent",
            !reduceMotion && "transition-[top,left,width,height] duration-200"
          )}
          style={{
            top: box.top - pad,
            left: box.left - pad,
            width: box.width + pad * 2,
            height: box.height + pad * 2,
            boxShadow: "0 0 0 9999px rgba(20,20,20,0.35)",
          }}
        />
      ) : (
        <div
          aria-hidden
          className="absolute inset-0 bg-[rgba(20,20,20,0.22)]"
        />
      )}

      <div
        role="dialog"
        aria-labelledby="demo-coachmark-title"
        className="pointer-events-auto absolute z-10 w-[min(100%-2rem,340px)] rounded-[12px] bg-white p-5 shadow-[0_12px_40px_rgba(0,0,0,0.2)]"
        style={{ top: tipTop, left: tipLeft }}
      >
        <p className="font-sans mb-2 text-[11px] font-medium tracking-[0.06em] text-[#7c7c7c] uppercase">
          {chapterLabel} · {stepLabel}
        </p>
        <h2
          id="demo-coachmark-title"
          className="font-jakarta mb-2 text-lg font-semibold text-[#141414]"
        >
          {title}
        </h2>
        <p className="font-sans mb-4 text-sm leading-5 text-[#4e4e4e]">{body}</p>
        <div className="flex flex-col gap-2">
          <MarketingButton
            type="button"
            variant="primary"
            showArrow={false}
            className="w-full min-h-11"
            onClick={onCta}
          >
            {ctaLabel}
          </MarketingButton>
          {onSkip ? (
            <button
              type="button"
              onClick={onSkip}
              className="font-sans min-h-11 text-sm text-[#4e4e4e] underline-offset-2 hover:underline"
            >
              Open tour guide
            </button>
          ) : null}
        </div>
      </div>
    </div>,
    document.body
  )
}

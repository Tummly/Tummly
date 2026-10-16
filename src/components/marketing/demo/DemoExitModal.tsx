import { Check } from "lucide-react"
import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"

type DemoExitModalProps = {
  onContinueExploring: () => void
  /** Clear demo session then go to this path (home, pricing, signup). */
  onLeaveDemo: (to: string) => void
}

/** Figma exit card `5712:20214` — Ready to try with your own guests. */
export function DemoExitModal({
  onContinueExploring,
  onLeaveDemo,
}: DemoExitModalProps) {
  const copy = PRODUCT_DEMO.exit

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="demo-exit-title"
      className="relative z-10 flex w-full max-w-[662px] flex-col items-center gap-10 overflow-clip rounded-[10px] bg-white px-8 py-10 shadow-[0_8px_40px_rgba(0,0,0,0.18)] sm:px-[50px]"
    >
      <MarketingLogo
        onLight
        width={145}
        height={37}
        className="h-[37px] w-auto"
      />

      <div className="flex w-full flex-col items-center gap-[50px]">
        <div className="flex w-full flex-col items-center gap-[22px] text-center">
          <h1
            id="demo-exit-title"
            className="font-jakarta max-w-[393px] text-[32px] leading-tight font-semibold text-[#141414] sm:text-[38px]"
          >
            {copy.title}
          </h1>
          <p className="font-sans max-w-[510px] text-base leading-6 text-[#141414] sm:text-lg">
            {copy.body}
          </p>
          <div className="flex flex-wrap items-center justify-center gap-3.5">
            {copy.bullets.map((bullet) => (
              <div
                key={bullet}
                className="font-sans flex items-center gap-2 rounded-[30px] px-2.5 py-2 text-xs font-medium text-[#727272]"
              >
                <span className="inline-flex size-4 items-center justify-center text-[#14a74a]">
                  <Check className="size-3.5" strokeWidth={3} />
                </span>
                {bullet}
              </div>
            ))}
          </div>
        </div>

        <div className="flex w-full flex-col gap-[26px]">
          <div className="flex w-full flex-col gap-3 sm:flex-row sm:gap-5">
            <MarketingButton
              variant="primary"
              showArrow
              asChild
              className="w-full flex-1"
            >
              <Link
                to={copy.primaryTo}
                onClick={(event) => {
                  event.preventDefault()
                  onLeaveDemo(copy.primaryTo)
                }}
              >
                {copy.primaryLabel}
              </Link>
            </MarketingButton>
            <MarketingButton
              variant="secondary"
              showArrow={false}
              asChild
              className="w-full flex-1"
            >
              <Link
                to={copy.secondaryTo}
                onClick={(event) => {
                  event.preventDefault()
                  onLeaveDemo(copy.secondaryTo)
                }}
              >
                {copy.secondaryLabel}
              </Link>
            </MarketingButton>
          </div>
          <div className="h-px w-full bg-[#ebebeb]" />
          <button
            type="button"
            onClick={onContinueExploring}
            className="font-sans text-center text-sm font-medium tracking-[0.4px] text-[#14a74a] underline underline-offset-2"
          >
            {copy.continueLabel}
          </button>
        </div>
      </div>
    </div>
  )
}

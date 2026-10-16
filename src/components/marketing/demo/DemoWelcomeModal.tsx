import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"

type DemoWelcomeModalProps = {
  hasGuestSubmission: boolean
  onStartTour: () => void
  onExplore: () => void
}

/** Restaurant-view welcome (§6) — shown on the dashboard after demo JWT. */
export function DemoWelcomeModal({
  hasGuestSubmission,
  onStartTour,
  onExplore,
}: DemoWelcomeModalProps) {
  const copy = PRODUCT_DEMO.welcome

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="demo-welcome-title"
      className="relative z-10 flex w-full max-w-[520px] flex-col items-center gap-10 overflow-clip rounded-[10px] bg-white px-8 py-10 shadow-[0_8px_40px_rgba(0,0,0,0.18)] sm:px-12"
    >
      <MarketingLogo
        onLight
        width={145}
        height={37}
        className="h-[37px] w-auto"
      />

      <div className="flex w-full flex-col items-center gap-[22px] text-center">
        <p className="font-sans text-[11px] font-medium tracking-[0.08em] text-[#7c7c7c] uppercase">
          {copy.eyebrow}
        </p>
        <h1
          id="demo-welcome-title"
          className="font-jakarta text-[32px] leading-tight font-semibold text-[#141414] sm:text-[38px]"
        >
          {copy.title}
        </h1>
        <p className="font-sans max-w-[420px] text-base leading-6 text-[#4e4e4e] sm:text-lg sm:leading-6">
          {hasGuestSubmission ? copy.bodyWithSubmission : copy.body}
        </p>
      </div>

      <div className="flex w-full flex-col items-center gap-3">
        <MarketingButton
          type="button"
          variant="primary"
          showArrow={false}
          className="w-full max-w-[320px] min-h-11"
          onClick={onStartTour}
        >
          {copy.primaryLabel}
        </MarketingButton>
        <MarketingButton
          type="button"
          variant="secondary"
          showArrow={false}
          className="w-full max-w-[320px] min-h-11"
          onClick={onExplore}
        >
          {copy.secondaryLabel}
        </MarketingButton>
        <p className="font-sans text-sm text-[#7c7c7c]">{copy.safetyLine}</p>
      </div>
    </div>
  )
}

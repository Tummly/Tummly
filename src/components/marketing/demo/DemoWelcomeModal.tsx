import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"

type DemoWelcomeModalProps = {
  hasGuestSubmission: boolean
  onStartTour: () => void
}

/**
 * Restaurant-view welcome (§6) — shown on the dashboard after demo JWT.
 * Figma `5736:19952` (Tummly - Marketing Website): one CTA, Start tour.
 */
export function DemoWelcomeModal({
  hasGuestSubmission,
  onStartTour,
}: DemoWelcomeModalProps) {
  const copy = PRODUCT_DEMO.welcome

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="demo-welcome-title"
      className="relative z-10 flex w-full max-w-[706px] flex-col items-center gap-12 overflow-clip rounded-[10px] bg-white px-6 py-10 shadow-[0_8px_40px_rgba(0,0,0,0.18)] sm:gap-[70px] sm:px-[100px]"
    >
      <MarketingLogo
        onLight
        width={144}
        height={36}
        className="h-9 w-auto"
      />

      <div className="flex w-full flex-col items-center gap-[34px]">
        <div className="flex w-full flex-col items-center gap-[22px] text-center">
          <div className="flex flex-col items-center gap-1.5">
            <p className="font-sans text-sm leading-4 font-medium text-[#4a4a4a] uppercase">
              {copy.eyebrow}
            </p>
            <h1
              id="demo-welcome-title"
              className="font-jakarta text-[26px] leading-[1.26] font-semibold text-[#141414] sm:text-[30px]"
            >
              {copy.title}
            </h1>
          </div>
          <p className="font-sans max-w-[506px] text-base leading-6 text-[#141414]">
            {hasGuestSubmission ? copy.bodyWithSubmission : copy.body}
          </p>
        </div>

        <MarketingButton
          type="button"
          variant="primary"
          showArrow={false}
          className="h-[50px] w-full max-w-[376px] rounded-[4px]"
          onClick={onStartTour}
          autoFocus
        >
          {copy.primaryLabel}
        </MarketingButton>
      </div>

      <p className="font-sans text-center text-sm leading-4 text-[#141414]">
        {copy.safetyLine}
      </p>
    </div>
  )
}

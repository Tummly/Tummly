import { MarketingButton } from "@/components/marketing/MarketingButton"
import { PRODUCT_DEMO } from "@/content/marketing/productDemoPage"

type DemoGuestThankYouProps = {
  restaurantName: string
  locationName: string
  onSeeInTummly: () => void
  onStartPilot: () => void
}

/** §4.2 guest thank-you with non-redeemable demo offer. */
export function DemoGuestThankYou({
  restaurantName,
  locationName,
  onSeeInTummly,
  onStartPilot,
}: DemoGuestThankYouProps) {
  const copy = PRODUCT_DEMO.guestThankYou

  return (
    <div className="mx-auto flex w-full max-w-md flex-col items-center gap-8 px-4 py-10 text-center">
      <p className="font-sans text-[11px] font-medium tracking-[0.08em] text-white/55 uppercase">
        Guest view · Demo
      </p>
      <div className="flex flex-col gap-2">
        <p className="font-jakarta text-xl font-semibold text-white">
          {restaurantName}
        </p>
        <p className="font-sans text-sm text-white/70">{locationName}</p>
      </div>
      <h1 className="font-jakarta text-[28px] font-semibold text-white">
        {copy.headline}
      </h1>
      <div className="w-full rounded-[12px] border border-dashed border-white/25 bg-white px-5 py-6">
        <p className="font-sans mb-2 text-[11px] font-semibold tracking-[0.08em] text-[#b42318] uppercase">
          {copy.offerLabel}
        </p>
        <p className="font-jakarta text-lg font-semibold text-[#141414]">
          {copy.offerTitle}
        </p>
      </div>
      <div className="flex w-full flex-col gap-3">
        <MarketingButton
          type="button"
          variant="primary"
          showArrow={false}
          className="w-full min-h-11"
          onClick={onSeeInTummly}
        >
          {copy.primaryLabel}
        </MarketingButton>
        <MarketingButton
          type="button"
          variant="secondary-inverse"
          showArrow={false}
          className="w-full min-h-11"
          onClick={onStartPilot}
        >
          {copy.secondaryLabel}
        </MarketingButton>
      </div>
      <p className="font-sans text-sm text-white/55">
        Demo only. No messages are sent. This offer cannot be redeemed.
      </p>
    </div>
  )
}

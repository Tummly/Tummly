import { Button } from "@/components/ui/button"
import type { GuestPreviewOfferCouponView } from "@/lib/operatorFeedback/guestPreviewPresentation"

type GuestPreviewOfferCouponProps = {
  coupon: GuestPreviewOfferCouponView
}

/**
 * Email Guest preview offer coupon — title, description, placeholder code,
 * expiry. Copy is display-only chrome in preview.
 * Uses fixed dark email-canvas colours (matches sent guest response email).
 */
export function GuestPreviewOfferCoupon({ coupon }: GuestPreviewOfferCouponProps) {
  return (
    <div className="flex w-full flex-col items-center gap-10 overflow-clip rounded-op-xl bg-[var(--op-color-black)] px-5 py-10">
      <div className="flex w-full flex-col items-center gap-3">
        <p className="m-0 text-center text-lg font-medium leading-normal text-[var(--op-color-white)]">
          {coupon.title}
        </p>
        {coupon.description !== "" ? (
          <p className="m-0 max-w-sm text-center text-xs font-medium leading-[17px] text-[var(--op-color-white)]/40">
            {coupon.description}
          </p>
        ) : null}
      </div>

      <div className="flex w-full max-w-sm flex-col items-stretch gap-2.5">
        <div className="flex items-stretch overflow-hidden rounded-[4px] border border-[var(--op-color-gray-950)] bg-[color-mix(in_srgb,var(--op-color-gray-900)_15%,transparent)]">
          <div className="flex min-w-0 flex-1 items-center px-3 py-3">
            <p className="m-0 truncate text-sm font-normal text-[var(--op-color-gray-550)]">
              {coupon.redemptionCode}
            </p>
          </div>
          <div className="flex shrink-0 items-center border-l border-[var(--op-color-gray-950)]">
            <Button
              type="button"
              variant="op-ghost"
              size="sm"
              disabled
              className="rounded-none px-3 py-3 text-sm font-medium text-[var(--op-color-gray-550)]"
            >
              {coupon.copyLabel}
            </Button>
          </div>
        </div>
        <p className="m-0 text-center text-xs font-medium leading-[17px] text-[var(--op-color-white)]/50">
          {coupon.expiryLabel}
        </p>
      </div>
    </div>
  )
}

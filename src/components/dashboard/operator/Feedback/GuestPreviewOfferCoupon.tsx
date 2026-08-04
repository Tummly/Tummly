import type { GuestPreviewOfferCouponView } from "@/lib/operatorFeedback/guestPreviewPresentation"

type GuestPreviewOfferCouponProps = {
  coupon: GuestPreviewOfferCouponView
}

/**
 * Email Guest preview offer coupon — title, description, placeholder code,
 * expiry. Copy is display-only chrome in preview.
 */
export function GuestPreviewOfferCoupon({ coupon }: GuestPreviewOfferCouponProps) {
  return (
    <div className="flex w-full flex-col items-center gap-10 overflow-clip rounded-[8px] bg-[var(--op-color-gray-995)] px-5 py-10">
      <div className="flex w-full flex-col items-center gap-3">
        <p className="m-0 text-center text-lg font-medium leading-normal text-op-text-primary">
          {coupon.title}
        </p>
        {coupon.description !== "" ? (
          <p className="m-0 max-w-[364px] text-center text-xs font-medium leading-[17px] text-op-text-muted">
            {coupon.description}
          </p>
        ) : null}
      </div>

      <div className="flex w-full max-w-[382px] flex-col items-stretch gap-2.5">
        <div className="flex items-stretch overflow-hidden rounded-[4px] border border-op-card-border bg-op-background-secondary/15">
          <div className="flex min-w-0 flex-1 items-center px-[13px] py-3">
            <p className="m-0 truncate text-sm font-normal text-op-text-muted">
              {coupon.redemptionCode}
            </p>
          </div>
          <div className="flex shrink-0 items-center border-l border-op-card-border px-3 py-3">
            <span className="text-sm font-medium text-op-text-muted">
              {coupon.copyLabel}
            </span>
          </div>
        </div>
        <p className="m-0 text-center text-xs font-medium leading-[17px] text-op-text-muted">
          {coupon.expiryLabel}
        </p>
      </div>
    </div>
  )
}

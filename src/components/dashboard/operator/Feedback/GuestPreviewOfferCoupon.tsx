import { Button } from "@/components/ui/button"
import { OfferClaimQrImage } from "@/components/dashboard/operator/Feedback/OfferClaimQrImage"
import type { GuestPreviewOfferCouponView } from "@/lib/operatorFeedback/guestPreviewPresentation"
import { cn } from "@/lib/utils"

type GuestPreviewOfferCouponSurface = "email" | "thankYou"

type GuestPreviewOfferCouponProps = {
  coupon: GuestPreviewOfferCouponView
  /**
   * `email` — Operator dark canvas tokens (`html.op`). `thankYou` — guest-feedback
   * tokens so the block paints on public `/scan/:token` (not `html.op`).
   */
  surface?: GuestPreviewOfferCouponSurface
}

const EMAIL_SURFACE = {
  root: "rounded-op-xl bg-[var(--op-color-black)]",
  title: "text-[var(--op-color-white)]",
  description: "text-[var(--op-color-white)]/40",
  codeRow:
    "border-[var(--op-color-gray-950)] bg-[color-mix(in_srgb,var(--op-color-gray-900)_15%,transparent)]",
  codeText: "text-[var(--op-color-gray-550)]",
  codeDivider: "border-[var(--op-color-gray-950)]",
  copyButton: "text-[var(--op-color-gray-550)]",
  expiry: "text-[var(--op-color-white)]/50",
} as const

const THANK_YOU_SURFACE = {
  root: "rounded-[8px] bg-guest-feedback-bg",
  title: "text-guest-feedback-text",
  description: "text-guest-feedback-text/40",
  codeRow: "border-guest-feedback-border bg-guest-feedback-surface/15",
  codeText: "text-guest-feedback-placeholder",
  codeDivider: "border-guest-feedback-border",
  copyButton: "text-guest-feedback-placeholder",
  expiry: "text-guest-feedback-text/50",
} as const

/**
 * Guest-facing offer coupon — Offer claim QR, title, description, code, Copy,
 * expiry. Preview keeps Copy display-only; live thank-you enables Copy.
 */
export function GuestPreviewOfferCoupon({
  coupon,
  surface = "email",
}: GuestPreviewOfferCouponProps) {
  const copyEnabled = coupon.copyEnabled === true
  const tokens = surface === "thankYou" ? THANK_YOU_SURFACE : EMAIL_SURFACE

  return (
    <div
      className={cn(
        "flex w-full flex-col items-center gap-[33px] overflow-clip p-5",
        tokens.root
      )}
    >
      <OfferClaimQrImage claimCode={coupon.redemptionCode} />

      <div className="flex w-full flex-col items-center gap-3">
        <p
          className={cn(
            "m-0 text-center text-base font-medium leading-normal",
            tokens.title
          )}
        >
          {coupon.title}
        </p>
        {coupon.description !== "" ? (
          <p
            className={cn(
              "m-0 max-w-sm text-center text-xs font-medium leading-[17px]",
              tokens.description
            )}
          >
            {coupon.description}
          </p>
        ) : null}
      </div>

      <div className="flex w-full max-w-sm flex-col items-stretch gap-2.5">
        <div
          className={cn(
            "flex items-stretch overflow-hidden rounded-[4px] border",
            tokens.codeRow
          )}
        >
          <div className="flex min-w-0 flex-1 items-center px-3 py-3">
            <p className={cn("m-0 truncate text-sm font-normal", tokens.codeText)}>
              {coupon.redemptionCode}
            </p>
          </div>
          <div
            className={cn("flex shrink-0 items-center border-l", tokens.codeDivider)}
          >
            <Button
              type="button"
              variant="op-ghost"
              size="sm"
              disabled={!copyEnabled}
              onClick={
                copyEnabled
                  ? () => {
                      void navigator.clipboard.writeText(coupon.redemptionCode)
                    }
                  : undefined
              }
              className={cn(
                "rounded-none px-3 py-3 text-sm font-medium",
                tokens.copyButton
              )}
            >
              {coupon.copyLabel}
            </Button>
          </div>
        </div>
        <p
          className={cn(
            "m-0 text-center text-xs font-medium leading-[17px]",
            tokens.expiry
          )}
        >
          {coupon.expiryLabel}
        </p>
      </div>
    </div>
  )
}

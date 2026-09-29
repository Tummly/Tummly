import { Button } from "@/components/ui/button"
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip"
import { OfferClaimQrImage } from "@/components/dashboard/operator/Feedback/OfferClaimQrImage"
import {
  GUEST_PREVIEW_OFFER_CLAIM_CODE_TOKEN_HELPER,
  GUEST_THANK_YOU_OFFER_ALREADY_CLAIMED,
  GUEST_THANK_YOU_OFFER_EYEBROW,
  GUEST_THANK_YOU_OFFER_TERMS_LABEL,
  isGuestPreviewOfferClaimCodePlaceholder,
  type GuestPreviewOfferCouponView,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import {
  OPERATOR_SHELL_TOOLTIP_ARROW_CLASS,
  OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
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
  eyebrow: "text-[var(--op-color-white)]",
  title: "text-[var(--op-color-white)]",
  description: "text-[var(--op-color-white)]/40",
  codeRow:
    "border-[var(--op-color-gray-950)] bg-[color-mix(in_srgb,var(--op-color-gray-900)_15%,transparent)]",
  codeText: "text-[var(--op-color-gray-550)]",
  codeDivider: "border-[var(--op-color-gray-950)]",
  copyButton:
    "text-[var(--op-color-gray-550)] hover:text-[var(--op-color-white)]",
  expiry: "text-[var(--op-color-white)]/50",
  terms: "text-[var(--op-color-white)]/50",
} as const

const THANK_YOU_SURFACE = {
  root: "rounded-[8px] bg-guest-feedback-bg",
  eyebrow: "text-guest-feedback-text",
  title: "text-guest-feedback-text",
  description: "text-guest-feedback-text/40",
  codeRow: "border-guest-feedback-border bg-guest-feedback-surface/15",
  codeText: "text-guest-feedback-placeholder",
  codeDivider: "border-guest-feedback-border",
  // Override op-ghost `hover:text-foreground` (near-black on guest dark bg).
  copyButton:
    "text-guest-feedback-placeholder hover:text-guest-feedback-text",
  expiry: "text-guest-feedback-text/50",
  terms: "text-guest-feedback-text/50",
} as const

function PreviewClaimCodeLabel({
  code,
  className,
}: {
  code: string
  className: string
}) {
  if (!isGuestPreviewOfferClaimCodePlaceholder(code)) {
    return (
      <p className={cn("m-0 truncate text-sm font-normal", className)}>{code}</p>
    )
  }

  return (
    <TooltipProvider delayDuration={200}>
      <Tooltip>
        <TooltipTrigger asChild>
          <p
            className={cn(
              "m-0 truncate text-sm font-normal underline decoration-dotted underline-offset-4",
              className
            )}
          >
            {code}
          </p>
        </TooltipTrigger>
        <TooltipContent
          side="top"
          sideOffset={6}
          className={`${OPERATOR_SHELL_TOOLTIP_CONTENT_CLASS} z-[140] max-w-sm px-3 py-2 text-left text-xs leading-5 whitespace-normal`}
          arrowClassName={OPERATOR_SHELL_TOOLTIP_ARROW_CLASS}
        >
          {GUEST_PREVIEW_OFFER_CLAIM_CODE_TOKEN_HELPER}
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}

/**
 * Guest-facing offer coupon — Offer claim QR, title, description, code, Copy,
 * expiry. Preview keeps Copy display-only; live thank-you enables Copy.
 */
export function GuestPreviewOfferCoupon({
  coupon,
  surface = "email",
}: GuestPreviewOfferCouponProps) {
  const copyEnabled = coupon.copyEnabled === true
  const isThankYou = surface === "thankYou"
  const tokens = isThankYou ? THANK_YOU_SURFACE : EMAIL_SURFACE
  const statusMessage = coupon.alreadyClaimed
    ? GUEST_THANK_YOU_OFFER_ALREADY_CLAIMED
    : coupon.description !== ""
      ? coupon.description
      : null

  return (
    <div
      className={cn(
        "flex w-full flex-col items-center gap-[33px] overflow-clip p-5",
        tokens.root
      )}
    >
      <div className="flex w-full flex-col items-center gap-[18px]">
        <OfferClaimQrImage claimCode={coupon.redemptionCode} />

        <div className="flex w-full flex-col items-center gap-3 text-center">
          <div className="flex flex-col items-center gap-2">
            {isThankYou ? (
              <p
                className={cn(
                  "m-0 text-[10px] font-medium leading-normal",
                  tokens.eyebrow
                )}
              >
                {GUEST_THANK_YOU_OFFER_EYEBROW}
              </p>
            ) : null}
            <p
              className={cn(
                "m-0 text-base font-medium leading-normal",
                tokens.title
              )}
            >
              {coupon.title}
            </p>
          </div>
          {statusMessage != null ? (
            <p
              className={cn(
                "m-0 max-w-sm text-xs font-medium leading-[17px]",
                tokens.description
              )}
            >
              {statusMessage}
            </p>
          ) : null}
        </div>
      </div>

      <div className="flex w-full max-w-sm flex-col items-stretch gap-2.5">
        <div
          className={cn(
            "flex items-stretch overflow-hidden rounded-[4px] border",
            tokens.codeRow
          )}
        >
          <div className="flex min-w-0 flex-1 items-center px-3 py-3">
            <PreviewClaimCodeLabel
              code={coupon.redemptionCode}
              className={tokens.codeText}
            />
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
        {isThankYou ? (
          <div
            className={cn(
              "flex w-full items-center justify-between text-xs font-medium leading-[17px]",
              tokens.expiry
            )}
          >
            <p className="m-0">{coupon.expiryLabel}</p>
            <p className={cn("m-0", tokens.terms)}>
              {GUEST_THANK_YOU_OFFER_TERMS_LABEL}
            </p>
          </div>
        ) : (
          <p
            className={cn(
              "m-0 text-center text-xs font-medium leading-[17px]",
              tokens.expiry
            )}
          >
            {coupon.expiryLabel}
          </p>
        )}
      </div>
    </div>
  )
}

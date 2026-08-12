import { useEffect, useState, type ReactNode } from "react"
import {
  SendIcon,
  XIcon,
} from "lucide-react"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import authHeroLogo from "@/assets/images/auth-hero-logo.png"
import { topDecorationPicture } from "@/assets/guest-feedback-images"
import { GuestFeedbackBottomEdge } from "@/components/guest-feedback/GuestFeedbackBottomEdge"
import { OperatorGuestPreviewShell } from "@/components/dashboard/operator/shared/OperatorGuestPreviewShell"
import { Button } from "@/components/ui/button"
import {
  CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS,
  CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS,
  CAPTURE_GUEST_PREVIEW_TITLE_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import {
  GUEST_PREVIEW_CLOSE_LABEL,
  GUEST_PREVIEW_DESKTOP_LABEL,
  GUEST_PREVIEW_DEVICE,
  GUEST_PREVIEW_EDIT_TEXT_LABEL,
  GUEST_PREVIEW_EMPTY_VALUE,
  GUEST_PREVIEW_FOOTER_COOKIE,
  GUEST_PREVIEW_FOOTER_PRIVACY,
  GUEST_PREVIEW_FOOTER_TERMS,
  GUEST_PREVIEW_FOOTER_UNSUBSCRIBE,
  GUEST_PREVIEW_HEADING,
  GUEST_PREVIEW_MOBILE_LABEL,
  GUEST_PREVIEW_OVERLAY_BODY_CLASS,
  GUEST_PREVIEW_OVERLAY_CLASS,
  GUEST_PREVIEW_POWERED_BY_LABEL,
  GUEST_PREVIEW_SEND_TEST_LABEL,
  guestPreviewBrandSubtitle,
  guestPreviewBrandTitle,
  guestPreviewFooterAddress,
  guestPreviewFooterDisclaimer,
  type GuestPreviewDevice,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"
import { pictureToImageSet } from "@/lib/pictureBackground"
import { cn } from "@/lib/utils"

export type GuestPreviewOverlayProps = {
  open: boolean
  channel: RespondToGuestChannel | null
  subject: string
  message: string
  locationName: string | null
  locationAddress: string | null
  /** Restaurant brand when available; falls back to location name. */
  brandName?: string | null
  /** Offer coupon block — only for Respond with a recovery offer. */
  offerCoupon?: ReactNode
  onClose: () => void
  onEditText: () => void
  onSendTest?: () => void
  sendTestDisabled?: boolean
  sendTestBusy?: boolean
}

const topDecorationBackground = pictureToImageSet(topDecorationPicture)

function BrandLogo({ sizeClass }: { sizeClass: string }) {
  return (
    <span
      className={cn(
        "relative shrink-0 overflow-hidden rounded-[2px]",
        sizeClass
      )}
      aria-hidden
    >
      <img
        src={brandLogoPlaceholder}
        alt=""
        className="size-full object-cover"
      />
    </span>
  )
}

export type GuestPreviewEmailChromeProps = {
  brandName: string | null | undefined
  locationName: string | null
  locationAddress: string | null
  subject: string
  message: string
  offerCoupon?: ReactNode
  device?: GuestPreviewDevice
  className?: string
}

  /**
   * Venue-branded email canvas for Guest preview — Figma recovery email chrome.
   * Always dark canvas tokens (matches sent guest response email), not operator theme.
   * Message and optional offer coupon only — no Give feedback CTA.
   */
export function GuestPreviewEmailChrome({
  brandName,
  locationName,
  locationAddress,
  subject,
  message,
  offerCoupon,
  device = GUEST_PREVIEW_DEVICE.desktop,
  className,
}: GuestPreviewEmailChromeProps) {
  const title = guestPreviewBrandTitle(brandName, locationName)
  const subtitle = guestPreviewBrandSubtitle(brandName, locationName)
  const disclaimer = guestPreviewFooterDisclaimer(title)
  const addressLine = guestPreviewFooterAddress(title, locationAddress)
  const isMobile = device === GUEST_PREVIEW_DEVICE.mobile
  const trimmedSubject = subject.trim()
  const trimmedMessage = message.trim() || GUEST_PREVIEW_EMPTY_VALUE

  return (
    <div
      className={cn(
        "mx-auto w-full overflow-hidden rounded-[4px] bg-[var(--op-color-black)]",
        isMobile ? "max-w-[393px]" : "max-w-[600px]",
        className
      )}
    >
      <div className="relative flex flex-col items-start pl-8 pr-[52px] pt-[62px]">
        <div
          aria-hidden
          className="pointer-events-none absolute right-0 top-0 h-[138px] w-[314px] overflow-hidden"
        >
          <div
            className="absolute inset-0"
            style={{
              backgroundImage: topDecorationBackground,
              backgroundRepeat: "no-repeat",
              backgroundSize: "cover",
              backgroundPosition: "right top",
            }}
          />
          <div
            className="absolute inset-0"
            style={{
              backgroundImage:
                "linear-gradient(19.66deg, var(--op-color-black) 25.8%, transparent 110%), linear-gradient(37.61deg, var(--op-color-black) 32.9%, transparent 71.4%)",
            }}
          />
        </div>

        <div className="relative z-10 flex items-center gap-3">
          <BrandLogo sizeClass="size-12" />
          <div className="flex min-w-0 flex-col gap-1">
            <p className="m-0 text-[22px] font-semibold leading-normal text-[var(--op-color-white)]">
              {title}
            </p>
            {subtitle != null ? (
              <p className="m-0 text-xs font-semibold leading-normal text-[var(--op-color-white)]/80">
                {subtitle}
              </p>
            ) : null}
          </div>
        </div>
      </div>

      <div className="flex flex-col items-start bg-[var(--op-color-black)] px-8 py-10">
        <div className="relative flex w-full flex-col gap-[30px] rounded-op-xl border border-[var(--op-color-gray-980)] bg-[var(--op-color-gray-995)] p-8">
          <div className="flex w-full flex-col gap-3">
            {trimmedSubject ? (
              <p className="m-0 text-sm font-semibold leading-5 text-[var(--op-color-white)]">
                {trimmedSubject}
              </p>
            ) : null}
            <p className="m-0 whitespace-pre-wrap text-sm font-normal leading-5 text-[var(--op-color-white)]">
              {trimmedMessage}
            </p>
          </div>

          {offerCoupon}

          <span>
            aria-hidden
            className="absolute -left-3 top-1/2 size-[18px] -translate-y-1/2 rounded-[20px] bg-[var(--op-color-black)]"
          />
          <span
            aria-hidden
            className="absolute -right-3 top-1/2 size-[18px] -translate-y-1/2 rounded-[20px] bg-[var(--op-color-black)]"
          />
        </div>
      </div>

      <div className="flex flex-col items-center overflow-clip bg-[var(--op-color-black)] px-8 pb-[60px] pt-8">
        <div className="flex w-full flex-col items-center gap-[26px]">
          <div className="mx-auto flex max-w-[440px] flex-col items-center gap-3 text-center">
            <p className="m-0 text-sm font-normal leading-[19px] text-[var(--op-color-white)]">
              {disclaimer}
            </p>
            <p className="m-0 text-xs font-normal leading-normal text-[var(--op-color-white)]">
              {addressLine}
            </p>
          </div>
          <div className="h-px w-full bg-[var(--op-color-gray-980)]" />
          <div className="flex flex-wrap items-center justify-center gap-5 text-xs font-medium text-[var(--op-color-gray-550)]">
            <span>{GUEST_PREVIEW_FOOTER_UNSUBSCRIBE}</span>
            <span>{GUEST_PREVIEW_FOOTER_TERMS}</span>
            <span>{GUEST_PREVIEW_FOOTER_PRIVACY}</span>
            <span>{GUEST_PREVIEW_FOOTER_COOKIE}</span>
          </div>
        </div>
      </div>

      <div className="flex flex-col items-center">
        <div className="mb-2 flex items-start gap-1.5">
          <span className="text-[10px] font-medium leading-normal text-[var(--op-color-white)]">
            {GUEST_PREVIEW_POWERED_BY_LABEL}
          </span>
          <img src={authHeroLogo} alt="Tummly" className="h-[19px] w-auto" />
        </div>
        {/* Same green paper tear as the Feedback form shell. */}
        <GuestFeedbackBottomEdge placement="inline" />
      </div>
    </div>
  )
}

function SmsPreviewChrome({
  message,
}: {
  message: string
}) {
  // SMS offer path: Claim code stays in message text only — no Offer claim QR image.
  return (
    <div
      className={cn(
        CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS,
        "rounded-[4px] border border-op-card-border bg-op-background-secondary p-6"
      )}
    >
      <p className="m-0 whitespace-pre-wrap text-sm font-medium leading-5 text-op-text-primary">
        {message.trim() || GUEST_PREVIEW_EMPTY_VALUE}
      </p>
    </div>
  )
}

/**
 * Full-screen Guest preview for recovery drafts — Capture-like chrome,
 * email/SMS canvas (no Capture guest form).
 */
export function GuestPreviewOverlay({
  open,
  channel,
  subject,
  message,
  locationName,
  locationAddress,
  brandName = null,
  offerCoupon,
  onClose,
  onEditText,
  onSendTest,
  sendTestDisabled = true,
  sendTestBusy = false,
}: GuestPreviewOverlayProps) {
  const [device, setDevice] = useState<GuestPreviewDevice>(
    GUEST_PREVIEW_DEVICE.desktop
  )

  useEffect(() => {
    if (!open) {
      return
    }

    setDevice(GUEST_PREVIEW_DEVICE.desktop)
  }, [open])

  const isSms = channel === "sms"

  return (
    <OperatorGuestPreviewShell
      open={open}
      onClose={onClose}
      titleId="recovery-guest-preview-title"
      overlayClassName={GUEST_PREVIEW_OVERLAY_CLASS}
      bodyClassName={GUEST_PREVIEW_OVERLAY_BODY_CLASS}
      device={device}
      onDeviceChange={setDevice}
      desktopLabel={GUEST_PREVIEW_DESKTOP_LABEL}
      mobileLabel={GUEST_PREVIEW_MOBILE_LABEL}
      portaled
      removeScroll
      trapEscape
      lead={
        <h2
          id="recovery-guest-preview-title"
          className={cn(CAPTURE_GUEST_PREVIEW_TITLE_CLASS, "font-bold")}
        >
          {GUEST_PREVIEW_HEADING}
        </h2>
      }
      headerActions={
        <div className={CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-tertiary"
            size="sm"
            disabled={sendTestDisabled || sendTestBusy || onSendTest == null}
            onClick={onSendTest}
          >
            <SendIcon data-icon="inline-start" aria-hidden />
            {GUEST_PREVIEW_SEND_TEST_LABEL}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            size="sm"
            onClick={onEditText}
          >
            {GUEST_PREVIEW_EDIT_TEXT_LABEL}
          </Button>
          <Button
            type="button"
            variant="op-secondary"
            size="icon-sm"
            aria-label={GUEST_PREVIEW_CLOSE_LABEL}
            onClick={onClose}
            className="shrink-0 rounded-[2px]"
          >
            <XIcon data-icon="inline-start" aria-hidden />
          </Button>
        </div>
      }
    >
      {isSms ? (
        <SmsPreviewChrome message={message} />
      ) : (
        <GuestPreviewEmailChrome
          brandName={brandName}
          locationName={locationName}
          locationAddress={locationAddress}
          subject={subject}
          message={message}
          offerCoupon={offerCoupon}
          device={device}
        />
      )}
    </OperatorGuestPreviewShell>
  )
}

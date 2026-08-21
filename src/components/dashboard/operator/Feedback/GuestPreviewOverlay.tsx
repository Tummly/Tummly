import { useEffect, useState, type ReactNode } from "react"
import {
  SendIcon,
  XIcon,
} from "lucide-react"

import { BaseNonTransactionalEmail } from "@/components/email/BaseNonTransactionalEmail"
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
  GUEST_PREVIEW_HEADING,
  GUEST_PREVIEW_MOBILE_LABEL,
  GUEST_PREVIEW_OVERLAY_BODY_CLASS,
  GUEST_PREVIEW_OVERLAY_CLASS,
  GUEST_PREVIEW_SEND_TEST_LABEL,
  type GuestPreviewDevice,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"
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

export type GuestPreviewEmailChromeProps = {
  brandName: string | null | undefined
  locationName: string | null
  locationAddress: string | null
  subject: string
  message: string
  offerCoupon?: ReactNode
  device?: GuestPreviewDevice
  className?: string
  /** Overrides the device-based email max width when set. */
  maxWidthClass?: string
}

/**
 * Venue-branded email canvas for Guest preview — Base Non-Transactional Template.
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
  maxWidthClass,
}: GuestPreviewEmailChromeProps) {
  const isMobile = device === GUEST_PREVIEW_DEVICE.mobile

  return (
    <BaseNonTransactionalEmail
      brandName={brandName}
      locationName={locationName}
      locationAddress={locationAddress}
      subject={subject}
      message={message}
      offer={offerCoupon}
      className={className}
      maxWidthClass={
        maxWidthClass
        ?? (isMobile ? "max-w-[393px]" : "max-w-[600px]")
      }
    />
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

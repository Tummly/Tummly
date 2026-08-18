import { useState, type ReactNode } from "react"
import { SendIcon } from "lucide-react"

import {
  GuestPreviewEmailChrome,
  GuestPreviewOverlay,
} from "@/components/dashboard/operator/Feedback/GuestPreviewOverlay"
import { Button } from "@/components/ui/button"
import {
  GUEST_PREVIEW_CONTROL_LABEL,
  GUEST_PREVIEW_EDIT_TEXT_LABEL,
  GUEST_PREVIEW_EMPTY_VALUE,
  GUEST_PREVIEW_HEADING,
  GUEST_PREVIEW_RAIL_CLASS,
  GUEST_PREVIEW_RAIL_HEADING_CLASS,
  GUEST_PREVIEW_RAIL_SMS_CLASS,
  GUEST_PREVIEW_RAIL_SMS_TEXT_CLASS,
  GUEST_PREVIEW_RAIL_VEIL_CLASS,
  GUEST_PREVIEW_RAIL_VEIL_FADE_CLASS,
  GUEST_PREVIEW_RAIL_VEIL_WASH_CLASS,
  GUEST_PREVIEW_SEND_TEST_LABEL,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"

type GuestPreviewPanelProps = {
  channel: RespondToGuestChannel | null
  subject: string
  message: string
  locationName: string | null
  locationAddress: string | null
  brandName?: string | null
  disabled?: boolean
  /** Controlled overlay open — when omitted, panel owns local open state. */
  guestPreviewOpen?: boolean
  onOpenPreview?: () => void
  onClosePreview?: () => void
  onEditText: () => void
  /** Email-channel Guest preview send test. SMS stays disabled. */
  onSendTest?: () => void
  sendTestDisabled?: boolean
  sendTestBusy?: boolean
  /** Email-only offer coupon (Respond with a recovery offer). */
  offerCoupon?: ReactNode
}

/**
 * Review right-rail Guest preview — dimmed email shell + Preview control.
 * Send test is enabled for email-channel drafts when onSendTest is provided.
 */
export function GuestPreviewPanel({
  channel,
  subject,
  message,
  locationName,
  locationAddress,
  brandName = null,
  disabled = false,
  guestPreviewOpen,
  onOpenPreview,
  onClosePreview,
  onEditText,
  onSendTest,
  sendTestDisabled = false,
  sendTestBusy = false,
  offerCoupon,
}: GuestPreviewPanelProps) {
  const [localOpen, setLocalOpen] = useState(false)
  const isControlled = guestPreviewOpen !== undefined
  const open = isControlled ? guestPreviewOpen : localOpen
  const isSms = channel === "sms"
  const canSendTest =
    channel === "email"
    && onSendTest != null
    && !sendTestDisabled
    && !disabled

  const openPreview = () => {
    if (isControlled) {
      onOpenPreview?.()
      return
    }
    setLocalOpen(true)
  }

  const closePreview = () => {
    if (isControlled) {
      onClosePreview?.()
      return
    }
    setLocalOpen(false)
  }

  const handleEditText = () => {
    closePreview()
    onEditText()
  }

  return (
    <>
      <aside className="flex w-full flex-1 flex-col">
        <div className={GUEST_PREVIEW_RAIL_CLASS}>
          <div
            className="pointer-events-none absolute inset-x-0 top-[50px] flex justify-center overflow-hidden"
            aria-hidden
          >
            {isSms ? (
              <div className={GUEST_PREVIEW_RAIL_SMS_CLASS}>
                <p className={GUEST_PREVIEW_RAIL_SMS_TEXT_CLASS}>
                  {message.trim() || GUEST_PREVIEW_EMPTY_VALUE}
                </p>
              </div>
            ) : (
              <GuestPreviewEmailChrome
                brandName={brandName}
                locationName={locationName}
                locationAddress={locationAddress}
                subject={subject}
                message={message}
                offerCoupon={offerCoupon}
                className="w-[474px] max-w-none scale-[0.92] opacity-90"
              />
            )}
          </div>

          <div className={GUEST_PREVIEW_RAIL_VEIL_CLASS}>
            <div className={GUEST_PREVIEW_RAIL_VEIL_WASH_CLASS} aria-hidden />
            <div className={GUEST_PREVIEW_RAIL_VEIL_FADE_CLASS} aria-hidden />
            <h2 className={GUEST_PREVIEW_RAIL_HEADING_CLASS}>
              {GUEST_PREVIEW_HEADING}
            </h2>

            <div className="relative flex flex-1 items-center justify-center">
              <Button
                type="button"
                variant="op-secondary"
                onClick={openPreview}
              >
                {GUEST_PREVIEW_CONTROL_LABEL}
              </Button>
            </div>

            <div className="relative flex flex-wrap gap-3">
              <Button
                type="button"
                variant="op-tertiary"
                size="sm"
                disabled={!canSendTest || sendTestBusy}
                onClick={onSendTest}
              >
                <SendIcon data-icon="inline-start" aria-hidden />
                {GUEST_PREVIEW_SEND_TEST_LABEL}
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                size="sm"
                disabled={disabled}
                onClick={handleEditText}
              >
                {GUEST_PREVIEW_EDIT_TEXT_LABEL}
              </Button>
            </div>
          </div>
        </div>
      </aside>

      <GuestPreviewOverlay
        open={open}
        channel={channel}
        subject={subject}
        message={message}
        locationName={locationName}
        locationAddress={locationAddress}
        brandName={brandName}
        offerCoupon={offerCoupon}
        onClose={closePreview}
        onEditText={handleEditText}
        onSendTest={onSendTest}
        sendTestDisabled={!canSendTest}
        sendTestBusy={sendTestBusy}
      />
    </>
  )
}

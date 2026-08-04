import { useState, type ReactNode } from "react"
import { SendIcon } from "lucide-react"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import { GuestPreviewOverlay } from "@/components/dashboard/operator/Feedback/GuestPreviewOverlay"
import { Button } from "@/components/ui/button"
import {
  GUEST_PREVIEW_CONTROL_LABEL,
  GUEST_PREVIEW_EDIT_TEXT_LABEL,
  GUEST_PREVIEW_HEADING,
  GUEST_PREVIEW_SEND_TEST_LABEL,
  guestPreviewBrandSubtitle,
  guestPreviewBrandTitle,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"
import { cn } from "@/lib/utils"

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
  /** Email-only offer coupon (Respond with a recovery offer). */
  offerCoupon?: ReactNode
}

/**
 * Review right-rail Guest preview — dimmed branded shell + Preview control.
 * Send test is discoverable but disabled until a test-send API exists.
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
  offerCoupon,
}: GuestPreviewPanelProps) {
  const [localOpen, setLocalOpen] = useState(false)
  const isControlled = guestPreviewOpen !== undefined
  const open = isControlled ? guestPreviewOpen : localOpen

  const title = guestPreviewBrandTitle(brandName, locationName)
  const subtitle = guestPreviewBrandSubtitle(brandName, locationName)

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
      <aside className="flex w-full flex-1 flex-col gap-2.5">
        <h2 className="text-base font-semibold text-op-text-primary">
          {GUEST_PREVIEW_HEADING}
        </h2>

        <div className="flex min-h-[280px] w-full flex-col overflow-clip rounded-[4px] bg-[var(--op-color-gray-990)]">
          <div
            className="relative flex min-h-[280px] flex-1 flex-col justify-between gap-4 p-6"
            style={{
              backgroundImage:
                "linear-gradient(90deg, color-mix(in srgb, var(--op-color-black) 55%, transparent) 0%, color-mix(in srgb, var(--op-color-black) 55%, transparent) 100%), linear-gradient(180deg, transparent 26%, var(--op-color-gray-995) 88%)",
            }}
          >
            <div
              className="pointer-events-none absolute inset-0 flex items-center justify-center px-6"
              aria-hidden
            >
              <div className="flex max-h-[70%] w-full flex-col overflow-hidden rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)] p-4 opacity-40">
                <div className="flex items-center gap-2">
                  <span className="relative size-8 shrink-0 overflow-hidden rounded-[2px]">
                    <img
                      src={brandLogoPlaceholder}
                      alt=""
                      className="size-full object-cover"
                    />
                  </span>
                  <div className="flex min-w-0 flex-col gap-0.5">
                    <p className="truncate text-sm font-semibold text-op-text-primary">
                      {title}
                    </p>
                    {subtitle != null ? (
                      <p className="truncate text-xs text-op-text-muted">
                        {subtitle}
                      </p>
                    ) : null}
                  </div>
                </div>
                <div className="mt-4 h-16 rounded-[2px] bg-op-background-secondary" />
              </div>
            </div>

            <div className="relative z-10 flex flex-1 items-center justify-center">
              <Button
                type="button"
                variant="op-secondary"
                size="sm"
                onClick={openPreview}
              >
                {GUEST_PREVIEW_CONTROL_LABEL}
              </Button>
            </div>
          </div>
        </div>

        <div className="flex flex-wrap gap-3">
          <Button
            type="button"
            variant="op-tertiary"
            size="sm"
            disabled
            className={cn("gap-2")}
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
      />
    </>
  )
}

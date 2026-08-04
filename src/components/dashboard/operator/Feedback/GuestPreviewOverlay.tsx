import { useEffect, useState, type ReactNode } from "react"
import { MonitorIcon, SmartphoneIcon, XIcon } from "lucide-react"

import brandLogoPlaceholder from "@/assets/images/brand-logo-placeholder.png"
import authHeroLogo from "@/assets/images/auth-hero-logo.png"
import { Button } from "@/components/ui/button"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import {
  CAPTURE_GUEST_PREVIEW_BODY_CLASS,
  CAPTURE_GUEST_PREVIEW_CANVAS_CLASS,
  CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS,
  CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS,
  CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS,
  CAPTURE_GUEST_PREVIEW_HEADER_CLASS,
  CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS,
  CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS,
  CAPTURE_GUEST_PREVIEW_TITLE_CLASS,
  CAPTURE_GUEST_PREVIEW_TOOLBAR_CLASS,
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
  GUEST_PREVIEW_POWERED_BY_LABEL,
  GUEST_PREVIEW_SEND_TEST_LABEL,
  guestPreviewBrandSubtitle,
  guestPreviewBrandTitle,
  guestPreviewFooterAddress,
  guestPreviewFooterDisclaimer,
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
}

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

function EmailPreviewChrome({
  brandName,
  locationName,
  locationAddress,
  subject,
  message,
  offerCoupon,
  device,
}: {
  brandName: string | null | undefined
  locationName: string | null
  locationAddress: string | null
  subject: string
  message: string
  offerCoupon?: ReactNode
  device: GuestPreviewDevice
}) {
  const title = guestPreviewBrandTitle(brandName, locationName)
  const subtitle = guestPreviewBrandSubtitle(brandName, locationName)
  const disclaimer = guestPreviewFooterDisclaimer(title)
  const addressLine = guestPreviewFooterAddress(title, locationAddress)
  const isMobile = device === GUEST_PREVIEW_DEVICE.mobile
  const trimmedSubject = subject.trim()

  return (
    <div
      className={cn(
        "mx-auto w-full overflow-hidden rounded-[4px] border border-op-card-border bg-[var(--op-color-gray-990)]",
        isMobile ? "max-w-[393px]" : "max-w-[600px]"
      )}
    >
      <div className="relative flex flex-col gap-4 px-8 pb-4 pt-16">
        <div className="flex items-center gap-3">
          <BrandLogo sizeClass="size-12" />
          <div className="flex min-w-0 flex-col gap-1">
            <p className="m-0 text-lg font-semibold leading-normal text-op-text-primary">
              {title}
            </p>
            {subtitle != null ? (
              <p className="m-0 text-xs font-medium leading-normal text-op-text-muted">
                {subtitle}
              </p>
            ) : null}
          </div>
        </div>
      </div>

      <div className="flex flex-col gap-6 px-8 py-10">
        <div className="rounded-[4px] border border-op-card-border bg-op-background-secondary px-8 py-8">
          {trimmedSubject ? (
            <p className="m-0 mb-3 text-sm font-semibold leading-5 text-op-text-primary">
              {trimmedSubject}
            </p>
          ) : null}
          <p className="m-0 whitespace-pre-wrap text-sm font-medium leading-5 text-op-text-primary">
            {message.trim() || GUEST_PREVIEW_EMPTY_VALUE}
          </p>
        </div>
        {offerCoupon != null ? offerCoupon : null}
      </div>

      <div className="flex flex-col gap-4 px-8 pb-8 pt-8">
        <div className="mx-auto flex max-w-[440px] flex-col gap-3 text-center">
          <p className="m-0 text-xs font-medium leading-normal text-op-text-muted">
            {disclaimer}
          </p>
          <p className="m-0 text-xs font-medium leading-normal text-op-text-muted">
            {addressLine}
          </p>
        </div>
        <div className="mx-auto flex flex-wrap items-center justify-center gap-4 text-xs font-medium text-op-text-muted">
          <span>{GUEST_PREVIEW_FOOTER_UNSUBSCRIBE}</span>
          <span>{GUEST_PREVIEW_FOOTER_TERMS}</span>
          <span>{GUEST_PREVIEW_FOOTER_PRIVACY}</span>
          <span>{GUEST_PREVIEW_FOOTER_COOKIE}</span>
        </div>
      </div>

      <div className="flex flex-col items-center gap-2 pb-0 pt-0">
        <div className="flex items-center gap-1.5 pt-0">
          <span className="text-[10px] font-medium text-op-text-muted">
            {GUEST_PREVIEW_POWERED_BY_LABEL}
          </span>
          <img src={authHeroLogo} alt="Tummly" className="h-[19px] w-auto" />
        </div>
        <div className="h-[30px] w-full bg-[var(--op-color-gray-950)]" />
      </div>
    </div>
  )
}

function SmsPreviewChrome({
  message,
}: {
  message: string
}) {
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
}: GuestPreviewOverlayProps) {
  const [device, setDevice] = useState<GuestPreviewDevice>(
    GUEST_PREVIEW_DEVICE.desktop
  )

  useEffect(() => {
    if (!open) {
      return
    }

    setDevice(GUEST_PREVIEW_DEVICE.desktop)

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose()
      }
    }

    window.addEventListener("keydown", onKeyDown)
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = "hidden"

    return () => {
      window.removeEventListener("keydown", onKeyDown)
      document.body.style.overflow = previousOverflow
    }
  }, [open, onClose])

  if (!open) {
    return null
  }

  const isSms = channel === "sms"

  return (
    <div
      className={CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS}
      role="dialog"
      aria-modal="true"
      aria-labelledby="recovery-guest-preview-title"
    >
      <header className={CAPTURE_GUEST_PREVIEW_HEADER_CLASS}>
        <h2
          id="recovery-guest-preview-title"
          className={CAPTURE_GUEST_PREVIEW_TITLE_CLASS}
        >
          {GUEST_PREVIEW_HEADING}
        </h2>
        <div className={CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS}>
          <Button type="button" variant="op-tertiary" disabled>
            {GUEST_PREVIEW_SEND_TEST_LABEL}
          </Button>
          <Button type="button" variant="op-tertiary" onClick={onEditText}>
            {GUEST_PREVIEW_EDIT_TEXT_LABEL}
          </Button>
          <Button
            type="button"
            variant="op-ghost"
            size="icon-sm"
            aria-label={GUEST_PREVIEW_CLOSE_LABEL}
            onClick={onClose}
            className="shrink-0"
          >
            <XIcon data-icon="inline-start" aria-hidden />
          </Button>
        </div>
      </header>

      <div className={CAPTURE_GUEST_PREVIEW_BODY_CLASS}>
        <div className={CAPTURE_GUEST_PREVIEW_TOOLBAR_CLASS}>
          <div className="flex-1" />
          <ToggleGroup
            type="single"
            value={device}
            onValueChange={(value) => {
              if (
                value === GUEST_PREVIEW_DEVICE.desktop
                || value === GUEST_PREVIEW_DEVICE.mobile
              ) {
                setDevice(value)
              }
            }}
            variant="default"
            spacing={0}
            className={CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS}
            aria-label="Preview device"
          >
            <ToggleGroupItem
              value={GUEST_PREVIEW_DEVICE.desktop}
              className={CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS}
            >
              <MonitorIcon data-icon="inline-start" aria-hidden />
              {GUEST_PREVIEW_DESKTOP_LABEL}
            </ToggleGroupItem>
            <ToggleGroupItem
              value={GUEST_PREVIEW_DEVICE.mobile}
              className={CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS}
            >
              <SmartphoneIcon data-icon="inline-start" aria-hidden />
              {GUEST_PREVIEW_MOBILE_LABEL}
            </ToggleGroupItem>
          </ToggleGroup>
          <div className="flex-1" />
        </div>

        <div className={CAPTURE_GUEST_PREVIEW_CANVAS_CLASS}>
          {isSms ? (
            <SmsPreviewChrome message={message} />
          ) : (
            <EmailPreviewChrome
              brandName={brandName}
              locationName={locationName}
              locationAddress={locationAddress}
              subject={subject}
              message={message}
              offerCoupon={offerCoupon}
              device={device}
            />
          )}
        </div>
      </div>
    </div>
  )
}

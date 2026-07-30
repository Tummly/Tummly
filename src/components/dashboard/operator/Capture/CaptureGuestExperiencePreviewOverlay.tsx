import { useEffect, useState, type ReactNode } from "react"
import { MonitorIcon, SmartphoneIcon, XIcon } from "lucide-react"

import { GuestFeedbackForm } from "@/components/guest-feedback/GuestFeedbackForm"
import { GuestFeedbackShell } from "@/components/guest-feedback/GuestFeedbackShell"
import { GuestFeedbackSuccess } from "@/components/guest-feedback/GuestFeedbackSuccess"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import type { OperatorCaptureGuestExperienceView } from "@/lib/operatorCapture/buildCaptureGuestExperience"
import {
  CAPTURE_GUEST_PREVIEW_BODY_CLASS,
  CAPTURE_GUEST_PREVIEW_CANVAS_CLASS,
  CAPTURE_GUEST_PREVIEW_DEVICE,
  CAPTURE_GUEST_PREVIEW_DEVICE_GROUP_CLASS,
  CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS,
  CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS,
  CAPTURE_GUEST_PREVIEW_HEADER_CLASS,
  CAPTURE_GUEST_PREVIEW_HEADER_COPY_CLASS,
  CAPTURE_GUEST_PREVIEW_META_ITEM_CLASS,
  CAPTURE_GUEST_PREVIEW_META_LABEL_CLASS,
  CAPTURE_GUEST_PREVIEW_META_ROW_CLASS,
  CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS,
  CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS,
  CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS,
  CAPTURE_GUEST_PREVIEW_PAGE_TAB,
  CAPTURE_GUEST_PREVIEW_PAGE_TAB_TRIGGER_CLASS,
  CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS,
  CAPTURE_GUEST_PREVIEW_SHELL_CLASS,
  CAPTURE_GUEST_PREVIEW_SHELL_MOBILE_CONTENT_CLASS,
  CAPTURE_GUEST_PREVIEW_SUBTITLE_CLASS,
  CAPTURE_GUEST_PREVIEW_TITLE_CLASS,
  CAPTURE_GUEST_PREVIEW_TOOLBAR_CLASS,
  OPERATOR_CAPTURE_GUEST_PREVIEW_COPY,
} from "@/lib/operatorCapture/capturePresentation"

type CaptureGuestExperiencePreviewOverlayProps = {
  open: boolean
  guestExperience: OperatorCaptureGuestExperienceView
  /** When set (e.g. from Placement Detail), overrides the Smart Guest default label. */
  previewPlacementLabel?: string | null
  onClose: () => void
}

type PreviewPageTab =
  (typeof CAPTURE_GUEST_PREVIEW_PAGE_TAB)[keyof typeof CAPTURE_GUEST_PREVIEW_PAGE_TAB]

type PreviewDevice =
  (typeof CAPTURE_GUEST_PREVIEW_DEVICE)[keyof typeof CAPTURE_GUEST_PREVIEW_DEVICE]

function PreviewGuestCanvas({
  device,
  children,
}: {
  device: PreviewDevice
  children: ReactNode
}) {
  const isMobile = device === CAPTURE_GUEST_PREVIEW_DEVICE.mobile

  return (
    <div className={isMobile ? CAPTURE_GUEST_PREVIEW_MOBILE_FRAME_CLASS : undefined}>
      <GuestFeedbackShell
        className={CAPTURE_GUEST_PREVIEW_SHELL_CLASS}
        contentClassName={
          isMobile ? CAPTURE_GUEST_PREVIEW_SHELL_MOBILE_CONTENT_CLASS : undefined
        }
      >
        {children}
      </GuestFeedbackShell>
    </div>
  )
}

/** In-app read-only guest form overlay (Smart Guest / location-default). */
export function CaptureGuestExperiencePreviewOverlay({
  open,
  guestExperience,
  previewPlacementLabel = null,
  onClose,
}: CaptureGuestExperiencePreviewOverlayProps) {
  const [pageTab, setPageTab] = useState<PreviewPageTab>(
    CAPTURE_GUEST_PREVIEW_PAGE_TAB.feedback
  )
  const [device, setDevice] = useState<PreviewDevice>(
    CAPTURE_GUEST_PREVIEW_DEVICE.desktop
  )

  useEffect(() => {
    if (!open) {
      return
    }

    setPageTab(CAPTURE_GUEST_PREVIEW_PAGE_TAB.feedback)
    setDevice(CAPTURE_GUEST_PREVIEW_DEVICE.desktop)

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

  const copy = OPERATOR_CAPTURE_GUEST_PREVIEW_COPY

  return (
    <div
      className={CAPTURE_GUEST_PREVIEW_OVERLAY_CLASS}
      role="dialog"
      aria-modal="true"
      aria-labelledby="capture-guest-experience-preview-title"
    >
      <header className={CAPTURE_GUEST_PREVIEW_HEADER_CLASS}>
        <div className={CAPTURE_GUEST_PREVIEW_HEADER_COPY_CLASS}>
          <div className="flex flex-col gap-2">
            <h2
              id="capture-guest-experience-preview-title"
              className={CAPTURE_GUEST_PREVIEW_TITLE_CLASS}
            >
              {copy.title}
            </h2>
            <p className={CAPTURE_GUEST_PREVIEW_SUBTITLE_CLASS}>
              {copy.description}
            </p>
          </div>
          <div className={CAPTURE_GUEST_PREVIEW_META_ROW_CLASS}>
            <div className={CAPTURE_GUEST_PREVIEW_META_ITEM_CLASS}>
              <span className={CAPTURE_GUEST_PREVIEW_META_LABEL_CLASS}>
                {copy.locationLabel}
              </span>
              <span className={CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS}>
                {guestExperience.locationName}
              </span>
            </div>
            <div className={CAPTURE_GUEST_PREVIEW_META_ITEM_CLASS}>
              <span className={CAPTURE_GUEST_PREVIEW_META_LABEL_CLASS}>
                {copy.placementLabel}
              </span>
              <span className={CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS}>
                {previewPlacementLabel
                  ?? guestExperience.previewPlacementLabel}
              </span>
            </div>
            <div className={CAPTURE_GUEST_PREVIEW_META_ITEM_CLASS}>
              <span className={CAPTURE_GUEST_PREVIEW_META_LABEL_CLASS}>
                {copy.guestFormLabel}
              </span>
              <span className={CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS}>
                {copy.guestFormValue}
              </span>
            </div>
            <div className={CAPTURE_GUEST_PREVIEW_META_ITEM_CLASS}>
              <span className={CAPTURE_GUEST_PREVIEW_META_LABEL_CLASS}>
                {copy.connectedOfferLabel}
              </span>
              <span className={CAPTURE_GUEST_PREVIEW_META_VALUE_CLASS}>
                {guestExperience.connectedOffersText}
              </span>
            </div>
          </div>
        </div>
        <div className={CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS}>
          <Button type="button" variant="op-tertiary" disabled>
            {copy.editGuestFormCta}
          </Button>
          <Button type="button" variant="op-tertiary" disabled>
            {copy.openPreviewInNewTabCta}
          </Button>
          <Button
            type="button"
            variant="op-ghost"
            size="icon-sm"
            aria-label={copy.closeLabel}
            onClick={onClose}
            className="shrink-0"
          >
            <XIcon data-icon="inline-start" aria-hidden />
          </Button>
        </div>
      </header>

      <div className={CAPTURE_GUEST_PREVIEW_BODY_CLASS}>
        <Tabs
          value={pageTab}
          onValueChange={(value) => {
            if (
              value === CAPTURE_GUEST_PREVIEW_PAGE_TAB.feedback ||
              value === CAPTURE_GUEST_PREVIEW_PAGE_TAB.thankYou
            ) {
              setPageTab(value)
            }
          }}
          className="flex flex-col gap-0"
        >
          <div className={CAPTURE_GUEST_PREVIEW_TOOLBAR_CLASS}>
            <TabsList
              variant="default"
              className={CAPTURE_GUEST_PREVIEW_PAGE_TABS_LIST_CLASS}
            >
              <TabsTrigger
                value={CAPTURE_GUEST_PREVIEW_PAGE_TAB.feedback}
                className={CAPTURE_GUEST_PREVIEW_PAGE_TAB_TRIGGER_CLASS}
              >
                {copy.feedbackPageTab}
              </TabsTrigger>
              <TabsTrigger
                value={CAPTURE_GUEST_PREVIEW_PAGE_TAB.thankYou}
                className={CAPTURE_GUEST_PREVIEW_PAGE_TAB_TRIGGER_CLASS}
              >
                {copy.thankYouPageTab}
              </TabsTrigger>
            </TabsList>

            <ToggleGroup
              type="single"
              value={device}
              onValueChange={(value) => {
                if (
                  value === CAPTURE_GUEST_PREVIEW_DEVICE.desktop ||
                  value === CAPTURE_GUEST_PREVIEW_DEVICE.mobile
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
                value={CAPTURE_GUEST_PREVIEW_DEVICE.desktop}
                className={CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS}
              >
                <MonitorIcon data-icon="inline-start" aria-hidden />
                {copy.desktopDevice}
              </ToggleGroupItem>
              <ToggleGroupItem
                value={CAPTURE_GUEST_PREVIEW_DEVICE.mobile}
                className={CAPTURE_GUEST_PREVIEW_DEVICE_ITEM_CLASS}
              >
                <SmartphoneIcon data-icon="inline-start" aria-hidden />
                {copy.mobileDevice}
              </ToggleGroupItem>
            </ToggleGroup>
          </div>

          <div className={CAPTURE_GUEST_PREVIEW_CANVAS_CLASS}>
            <TabsContent
              value={CAPTURE_GUEST_PREVIEW_PAGE_TAB.feedback}
              className="mt-0"
            >
              <PreviewGuestCanvas device={device}>
                <div inert>
                  <GuestFeedbackForm
                    token=""
                    locationName={guestExperience.locationName}
                    address={guestExperience.locationAddress}
                    isSubmitting={false}
                    submitError={null}
                    onSubmit={async () => {}}
                    onRetry={() => {}}
                  />
                </div>
              </PreviewGuestCanvas>
            </TabsContent>
            <TabsContent
              value={CAPTURE_GUEST_PREVIEW_PAGE_TAB.thankYou}
              className="mt-0"
            >
              <PreviewGuestCanvas device={device}>
                <div inert className="flex w-full justify-center pt-10">
                  <GuestFeedbackSuccess
                    locationName={guestExperience.locationName}
                    address={guestExperience.locationAddress}
                  />
                </div>
              </PreviewGuestCanvas>
            </TabsContent>
          </div>
        </Tabs>
      </div>
    </div>
  )
}

import { useEffect, useState } from "react"
import { useNavigate, useOutletContext, useParams } from "react-router-dom"
import { XIcon } from "lucide-react"

import { getCampaignDraftById } from "@/api/dashboardApi"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { GuestPreviewEmailChrome } from "@/components/dashboard/operator/Feedback/GuestPreviewOverlay"
import { OperatorGuestPreviewShell } from "@/components/dashboard/operator/shared/OperatorGuestPreviewShell"
import { Button } from "@/components/ui/button"
import {
  CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS,
  CAPTURE_GUEST_PREVIEW_TITLE_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import {
  GUEST_PREVIEW_CLOSE_LABEL,
  GUEST_PREVIEW_DESKTOP_LABEL,
  GUEST_PREVIEW_DEVICE,
  GUEST_PREVIEW_HEADING,
  GUEST_PREVIEW_MOBILE_LABEL,
  GUEST_PREVIEW_OVERLAY_BODY_CLASS,
  GUEST_PREVIEW_OVERLAY_CLASS,
  type GuestPreviewDevice,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"
import { cn } from "@/lib/utils"

function parseCampaignRouteId(raw: string | undefined): number | null {
  if (raw == null || raw.trim() === "") {
    return null
  }
  const parsed = Number(raw)
  if (!Number.isInteger(parsed) || parsed <= 0) {
    return null
  }
  return parsed
}

function closePreview(navigateBack: () => void): void {
  if (window.history.length > 1) {
    navigateBack()
    return
  }
  window.close()
}

/** Full-page Guest Preview for a campaign message. */
export function CampaignGuestPreviewRoute() {
  const { campaignId: campaignIdParam } = useParams<{ campaignId: string }>()
  const navigate = useNavigate()
  const { mode, selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const campaignId = parseCampaignRouteId(campaignIdParam)
  const [device, setDevice] = useState<GuestPreviewDevice>(
    GUEST_PREVIEW_DEVICE.desktop
  )
  const [loadStatus, setLoadStatus] = useState<"loading" | "loaded" | "error">(
    "loading"
  )
  const [subject, setSubject] = useState("")
  const [message, setMessage] = useState("")
  const [title, setTitle] = useState("Campaign")

  const locationId = selectedLocationId ?? locations[0]?.id ?? 0
  const selectedLocation = locations.find(
    (location) => location.id === selectedLocationId
  )
  const homeFallback = operatorDashboardNavPath(mode, "home", locationId)

  useEffect(() => {
    if (campaignId == null) {
      setLoadStatus("error")
      return
    }

    let cancelled = false
    setLoadStatus("loading")
    void getCampaignDraftById(campaignId)
      .then((response) => {
        if (cancelled) {
          return
        }
        setTitle(response.campaign.name)
        setSubject(response.campaign.messageSubject?.trim() || response.campaign.name)
        setMessage(
          response.campaign.messageBody?.trim()
          || "Campaign message preview is not available."
        )
        setLoadStatus("loaded")
      })
      .catch(() => {
        if (cancelled) {
          return
        }
        setLoadStatus("error")
      })

    return () => {
      cancelled = true
    }
  }, [campaignId])

  const handleClose = () => {
    closePreview(() => {
      navigate(-1)
    })
  }

  if (loadStatus === "loading") {
    return (
      <div
        className="flex min-h-screen items-center justify-center"
        role="status"
        aria-live="polite"
        aria-label="Loading campaign preview"
      >
        <div
          className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
          aria-hidden
        />
      </div>
    )
  }

  if (loadStatus === "error") {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-3 p-6 text-center">
        <p className="text-sm text-destructive">
          Could not load campaign preview. Please try again.
        </p>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            navigate(homeFallback)
          }}
        >
          Back to Home
        </Button>
      </div>
    )
  }

  return (
    <OperatorGuestPreviewShell
      open
      onClose={handleClose}
      titleId="home-campaign-guest-preview-title"
      overlayClassName={cn(GUEST_PREVIEW_OVERLAY_CLASS, "static min-h-screen")}
      bodyClassName={GUEST_PREVIEW_OVERLAY_BODY_CLASS}
      device={device}
      onDeviceChange={setDevice}
      desktopLabel={GUEST_PREVIEW_DESKTOP_LABEL}
      mobileLabel={GUEST_PREVIEW_MOBILE_LABEL}
      lead={
        <h2
          id="home-campaign-guest-preview-title"
          className={cn(CAPTURE_GUEST_PREVIEW_TITLE_CLASS, "font-bold")}
        >
          {GUEST_PREVIEW_HEADING}
        </h2>
      }
      meta={
        <p className="m-0 text-op-sm text-op-card-subtitle-color">{title}</p>
      }
      headerActions={
        <div className={CAPTURE_GUEST_PREVIEW_HEADER_ACTIONS_CLASS}>
          <Button
            type="button"
            variant="op-secondary"
            size="icon-sm"
            aria-label={GUEST_PREVIEW_CLOSE_LABEL}
            onClick={handleClose}
            className="shrink-0 rounded-[2px]"
          >
            <XIcon data-icon="inline-start" aria-hidden />
          </Button>
        </div>
      }
    >
      <GuestPreviewEmailChrome
        brandName={selectedLocation?.locationName ?? null}
        locationName={selectedLocation?.locationName ?? null}
        locationAddress={selectedLocation?.address ?? null}
        subject={subject}
        message={message}
        device={device}
      />
    </OperatorGuestPreviewShell>
  )
}

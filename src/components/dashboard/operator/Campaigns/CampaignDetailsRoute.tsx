import { useEffect, useState } from "react"
import { useNavigate, useOutletContext, useParams } from "react-router-dom"

import { getCampaignDraftById } from "@/api/dashboardApi"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { operatorDashboardNavPath } from "@/lib/operatorHome/operatorDashboardPaths"
import type { CampaignDraftDetail } from "@/types/operatorCampaigns"
import { OPERATOR_HOME_CARD_PADDED_CLASS } from "@/lib/operatorHome/operatorHomeSectionPresentation"

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

function statusLabel(status: string): string {
  if (status === "partially-sent") {
    return "Partially sent"
  }
  if (status.length === 0) {
    return "—"
  }
  return status.charAt(0).toUpperCase() + status.slice(1)
}

/** Thin Campaign detail — name, status, and basic draft fields. */
export function CampaignDetailsRoute() {
  const { campaignId: campaignIdParam } = useParams<{ campaignId: string }>()
  const navigate = useNavigate()
  const { mode, selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const campaignId = parseCampaignRouteId(campaignIdParam)
  const [loadStatus, setLoadStatus] = useState<"loading" | "loaded" | "error">(
    "loading"
  )
  const [campaign, setCampaign] = useState<CampaignDraftDetail | null>(null)

  useEffect(() => {
    if (campaignId == null) {
      setLoadStatus("error")
      setCampaign(null)
      return
    }

    let cancelled = false
    setLoadStatus("loading")
    void getCampaignDraftById(campaignId)
      .then((response) => {
        if (cancelled) {
          return
        }
        setCampaign(response.campaign)
        setLoadStatus("loaded")
      })
      .catch(() => {
        if (cancelled) {
          return
        }
        setCampaign(null)
        setLoadStatus("error")
      })

    return () => {
      cancelled = true
    }
  }, [campaignId])

  const locationId = selectedLocationId ?? locations[0]?.id ?? 0
  const campaignsHref = operatorDashboardNavPath(mode, "campaigns", locationId)

  return (
    <div className="flex flex-col gap-4 p-4 sm:p-6">
      <div className="flex flex-wrap items-center gap-3">
        <Button
          type="button"
          variant="op-tertiary"
          onClick={() => {
            navigate(campaignsHref)
          }}
        >
          Back to campaigns
        </Button>
      </div>

      {loadStatus === "loading" ? (
        <div
          className="flex min-h-48 items-center justify-center"
          role="status"
          aria-live="polite"
          aria-label="Loading campaign"
        >
          <div
            className="size-8 animate-spin rounded-full border-2 border-primary/25 border-t-primary"
            aria-hidden
          />
        </div>
      ) : null}

      {loadStatus === "error" ? (
        <div className={`${OPERATOR_HOME_CARD_PADDED_CLASS} text-center`}>
          <p className="text-sm text-destructive">
            Could not load this campaign. Please try again.
          </p>
          <Button
            type="button"
            variant="link"
            size="link-sm"
            className="mt-3 font-medium underline"
            onClick={() => {
              if (campaignId == null) {
                return
              }
              setLoadStatus("loading")
              void getCampaignDraftById(campaignId)
                .then((response) => {
                  setCampaign(response.campaign)
                  setLoadStatus("loaded")
                })
                .catch(() => {
                  setCampaign(null)
                  setLoadStatus("error")
                })
            }}
          >
            Retry
          </Button>
        </div>
      ) : null}

      {loadStatus === "loaded" && campaign != null ? (
        <section className={OPERATOR_HOME_CARD_PADDED_CLASS}>
          <div className="flex flex-col gap-4">
            <Badge variant="soft">{statusLabel(campaign.status)}</Badge>
            <h1 className="m-0 text-xl font-bold text-op-card-title-color">
              {campaign.name}
            </h1>
            <dl className="m-0 grid gap-3 text-sm sm:grid-cols-2">
              <div>
                <dt className="text-op-card-subtitle-color">Channel</dt>
                <dd className="m-0 font-medium text-op-card-title-color">
                  {campaign.channel?.trim() || "—"}
                </dd>
              </div>
              <div>
                <dt className="text-op-card-subtitle-color">Audience</dt>
                <dd className="m-0 font-medium text-op-card-title-color">
                  {campaign.audienceKey?.trim() || "—"}
                </dd>
              </div>
              <div>
                <dt className="text-op-card-subtitle-color">Offer id</dt>
                <dd className="m-0 font-medium text-op-card-title-color">
                  {campaign.offerId == null ? "—" : String(campaign.offerId)}
                </dd>
              </div>
              <div>
                <dt className="text-op-card-subtitle-color">Updated</dt>
                <dd className="m-0 font-medium text-op-card-title-color">
                  {campaign.updatedAt}
                </dd>
              </div>
            </dl>
            {campaign.messageSubject != null
            && campaign.messageSubject.trim().length > 0 ? (
              <div>
                <p className="m-0 text-sm text-op-card-subtitle-color">Subject</p>
                <p className="m-0 mt-1 font-medium text-op-card-title-color">
                  {campaign.messageSubject}
                </p>
              </div>
            ) : null}
            {campaign.messageBody != null
            && campaign.messageBody.trim().length > 0 ? (
              <div>
                <p className="m-0 text-sm text-op-card-subtitle-color">Message</p>
                <p className="m-0 mt-1 whitespace-pre-wrap font-medium text-op-card-title-color">
                  {campaign.messageBody}
                </p>
              </div>
            ) : null}
          </div>
        </section>
      ) : null}
    </div>
  )
}

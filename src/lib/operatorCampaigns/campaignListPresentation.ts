/**
 * Campaigns list table — Figma Draft row projection (ticket 30 / 3462:61988).
 */

import { CAMPAIGN_CHANNEL_OPTIONS } from "@/lib/operatorCampaigns/campaignChannelPresentation"
import { CAMPAIGN_DETAIL_PREVIEW_COPY } from "@/lib/operatorCampaigns/campaignDetailPreviewPresentation"
import { CAMPAIGN_OFFER_OPTIONS } from "@/lib/operatorCampaigns/campaignOfferPresentation"
import {
  labelForCampaignGoalId,
  type CampaignGoalId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import type { CampaignsListItem } from "@/types/operatorCampaigns"

export const CAMPAIGNS_LIST_TABLE_COPY = {
  campaignColumn: "Campaign",
  statusColumn: "Status",
  locationColumn: "Location",
  channelColumn: "Channel",
  offerColumn: "Offer",
  sendDateColumn: "Send date",
  deliveryColumn: "Delivery",
  engagementColumn: "Engagement",
  redemptionsColumn: "Redemptions",
  actionsColumn: "Actions",
  draftStatusLabel: "Draft",
  continueEditing: "Continue editing",
  preview: CAMPAIGN_DETAIL_PREVIEW_COPY.preview,
  unschedule: "Unschedule",
  pause: "Pause",
  cancel: "Cancel",
  cancelRemaining: "Cancel remaining",
  resume: "Resume",
  retryRemaining: "Retry remaining",
  duplicateAsDraft: "Duplicate / retry as new Draft",
  metricDash: "—",
  updatedPrefix: "Updated",
} as const

export type CampaignRowActionId =
  | "preview"
  | "continue-editing"
  | "unschedule"
  | "pause"
  | "cancel"
  | "cancel-remaining"
  | "resume"
  | "retry-remaining"
  | "duplicate"

export type CampaignRowAction = {
  id: CampaignRowActionId
  label: string
}

/** List ⋮ actions by Campaign status (ticket 30 / PRD matrix). */
export function buildCampaignRowActions(status: string): CampaignRowAction[] {
  const preview: CampaignRowAction = {
    id: "preview",
    label: CAMPAIGNS_LIST_TABLE_COPY.preview,
  }

  switch (status) {
    case "draft":
      return [
        preview,
        {
          id: "continue-editing",
          label: CAMPAIGNS_LIST_TABLE_COPY.continueEditing,
        },
      ]
    case "scheduled":
      return [
        preview,
        {
          id: "unschedule",
          label: CAMPAIGNS_LIST_TABLE_COPY.unschedule,
        },
        { id: "pause", label: CAMPAIGNS_LIST_TABLE_COPY.pause },
        { id: "cancel", label: CAMPAIGNS_LIST_TABLE_COPY.cancel },
      ]
    case "sending":
      return [
        preview,
        { id: "pause", label: CAMPAIGNS_LIST_TABLE_COPY.pause },
        {
          id: "cancel-remaining",
          label: CAMPAIGNS_LIST_TABLE_COPY.cancelRemaining,
        },
      ]
    case "paused":
      return [
        preview,
        { id: "resume", label: CAMPAIGNS_LIST_TABLE_COPY.resume },
        { id: "cancel", label: CAMPAIGNS_LIST_TABLE_COPY.cancel },
      ]
    case "partially-sent":
      return [
        preview,
        {
          id: "retry-remaining",
          label: CAMPAIGNS_LIST_TABLE_COPY.retryRemaining,
        },
      ]
    case "failed":
      return [
        preview,
        {
          id: "duplicate",
          label: CAMPAIGNS_LIST_TABLE_COPY.duplicateAsDraft,
        },
      ]
    case "sent":
    case "cancelled":
      return [preview]
    default:
      return [preview]
  }
}

export type OperatorCampaignsListTableRow = {
  id: number
  name: string
  /** Raw Campaign status — drives ⋮ actions (ticket 27 / 30). */
  status: string
  /** Base64 rowversion for lifecycle API calls (ticket 30). */
  rowVersion: string
  /** Goal label · Updated relative — Figma Campaign subtitle. */
  metaLine: string
  statusLabel: string
  locationName: string
  channelLabel: string | null
  /** Recipient / parts line — null for Draft. */
  channelDetail: string | null
  offerTitle: string
  offerDetail: string | null
  sendDateLabel: string
  deliveryLabel: string
  engagementLabel: string
  redemptionsLabel: string
}

function channelBadgeLabel(channel: string | null): string | null {
  if (channel == null || channel.trim().length === 0) {
    return null
  }
  const match = CAMPAIGN_CHANNEL_OPTIONS.find((option) => option.id === channel)
  return match?.title.toUpperCase() ?? channel.toUpperCase()
}

function offerTitleForStance(offerStance: string | null): string {
  if (offerStance == null || offerStance.trim().length === 0) {
    return CAMPAIGNS_LIST_TABLE_COPY.metricDash
  }
  const match = CAMPAIGN_OFFER_OPTIONS.find((option) => option.id === offerStance)
  return match?.title ?? CAMPAIGNS_LIST_TABLE_COPY.metricDash
}

function metricOrDash(value: string | null): string {
  if (value == null || value.trim().length === 0) {
    return CAMPAIGNS_LIST_TABLE_COPY.metricDash
  }
  return value
}

function statusLabelForItem(status: string): string {
  if (status === "draft") {
    return CAMPAIGNS_LIST_TABLE_COPY.draftStatusLabel
  }
  if (status === "partially-sent") {
    return "Partially sent"
  }
  if (status.length === 0) {
    return CAMPAIGNS_LIST_TABLE_COPY.metricDash
  }
  return status.charAt(0).toUpperCase() + status.slice(1)
}

export function mapCampaignListItemToTableRow(
  item: CampaignsListItem,
  nowMs: number = Date.now()
): OperatorCampaignsListTableRow {
  const goalLabel =
    item.goalId == null
      ? null
      : labelForCampaignGoalId(item.goalId as CampaignGoalId)
  const relative = formatRelativeTime(item.updatedAt, nowMs)
  const updatedSegment =
    relative.length > 0
      ? `${CAMPAIGNS_LIST_TABLE_COPY.updatedPrefix} ${relative}`
      : null
  const metaParts = [goalLabel, updatedSegment].filter(
    (part): part is string => part != null && part.length > 0
  )

  return {
    id: item.id,
    name: item.name,
    status: item.status,
    rowVersion: item.rowVersion,
    metaLine: metaParts.join(" · "),
    statusLabel: statusLabelForItem(item.status),
    locationName: item.locationName,
    channelLabel: channelBadgeLabel(item.channel),
    channelDetail: null,
    offerTitle: offerTitleForStance(item.offerStance),
    offerDetail: null,
    sendDateLabel: metricOrDash(item.sendDate),
    deliveryLabel: metricOrDash(item.delivery),
    // Engagement stays dash until report ingestion (ticket 26 / PRD honesty).
    engagementLabel: CAMPAIGNS_LIST_TABLE_COPY.metricDash,
    redemptionsLabel: metricOrDash(item.redemptions),
  }
}

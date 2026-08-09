/** Campaigns Filter sheet schema — status, channel, location, goal, offer stance,
 * created by, delivery issue, date (Updated / Send date). */

import { CAMPAIGN_OFFER_OPTIONS } from "@/lib/operatorCampaigns/campaignOfferPresentation"
import { CAMPAIGN_GOAL_OPTIONS } from "@/lib/operatorCampaigns/campaignWizardPresentation"
import { DATE_PRESET_LABELS } from "@/lib/operatorFilterSheet"
import type {
  DateAxisId,
  FilterSheetSchema,
  SchemaOption,
} from "@/lib/operatorFilterSheet"

export type CampaignsFilterStatusId =
  | "draft"
  | "scheduled"
  | "sending"
  | "sent"
  | "partially-sent"
  | "paused"
  | "failed"
  | "cancelled"

export type CampaignsFilterChannelId = "email" | "sms"

export type CampaignsFilterDeliveryIssueId = "failed" | "partially-sent"

export const CAMPAIGNS_STATUS_LABELS: Record<CampaignsFilterStatusId, string> = {
  draft: "Draft",
  scheduled: "Scheduled",
  sending: "Sending",
  sent: "Sent",
  "partially-sent": "Partially sent",
  paused: "Paused",
  failed: "Failed",
  cancelled: "Cancelled",
}

export const CAMPAIGNS_CHANNEL_LABELS: Record<CampaignsFilterChannelId, string> =
  {
    email: "Email",
    sms: "SMS",
  }

export const CAMPAIGNS_DELIVERY_ISSUE_LABELS: Record<
  CampaignsFilterDeliveryIssueId,
  string
> = {
  failed: "Failed",
  "partially-sent": "Partially sent",
}

export const CAMPAIGNS_DATE_AXIS_LABELS: Partial<Record<DateAxisId, string>> = {
  updated: "Updated",
  "send-date": "Send date",
}

function toOptions<TId extends string>(
  labels: Record<TId, string>
): SchemaOption[] {
  return Object.entries(labels).map(([id, label]) => ({
    id,
    label: label as string,
  }))
}

export function campaignsFilterSheetSchema(
  catalog: {
    locations?: readonly SchemaOption[]
    createdBy?: readonly SchemaOption[]
  } = {}
): FilterSheetSchema {
  return {
    fields: [
      {
        id: "status",
        kind: "multi-select",
        label: "Status",
        chipKind: "status",
        options: toOptions(CAMPAIGNS_STATUS_LABELS),
      },
      {
        id: "channel",
        kind: "multi-select",
        label: "Channel",
        chipKind: "channel",
        options: toOptions(CAMPAIGNS_CHANNEL_LABELS),
      },
      {
        id: "location",
        kind: "location-scope",
        label: "Location",
        locations: catalog.locations ?? [],
      },
      {
        id: "goal",
        kind: "multi-select",
        label: "Goal",
        chipKind: "goal",
        options: CAMPAIGN_GOAL_OPTIONS.map((goal) => ({
          id: goal.id,
          label: goal.title,
        })),
      },
      {
        id: "offerStance",
        kind: "multi-select",
        label: "Offer stance",
        chipKind: "offerStance",
        options: CAMPAIGN_OFFER_OPTIONS.map((offer) => ({
          id: offer.id,
          label: offer.title,
        })),
      },
      {
        id: "createdBy",
        kind: "multi-select",
        label: "Created by",
        chipKind: "createdBy",
        options: catalog.createdBy ?? [],
      },
      {
        id: "deliveryIssue",
        kind: "multi-select",
        label: "Delivery issue",
        chipKind: "deliveryIssue",
        options: toOptions(CAMPAIGNS_DELIVERY_ISSUE_LABELS),
      },
      {
        id: "date",
        kind: "date",
        label: "Date",
        hasAxis: true,
        axisLabels: CAMPAIGNS_DATE_AXIS_LABELS,
        presetLabels: DATE_PRESET_LABELS,
      },
    ],
  }
}

/** Hide Location when the workspace has a single location. */
export function campaignsFilterSheetSchemaForWorkspace(catalog: {
  locations: readonly SchemaOption[]
  createdBy: readonly SchemaOption[]
  showLocationFilter: boolean
}): FilterSheetSchema {
  const schema = campaignsFilterSheetSchema(catalog)
  if (catalog.showLocationFilter) {
    return schema
  }
  return { fields: schema.fields.filter((field) => field.id !== "location") }
}

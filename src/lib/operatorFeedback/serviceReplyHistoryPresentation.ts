import { formatGuestProfileAbsoluteDateTime } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import type { FeedbackDetailsActivityEventDto } from "@/types/dashboard"

/** Feedback detail drawer: "Service reply history" (WS-E R6). Exact strings. */
export const SERVICE_REPLY_HISTORY_COPY = {
  heading: "Service reply history",
  sentAtLabel: "Sent or requested",
  channelLabel: "Channel",
  maskedRecipientLabel: "Masked recipient",
  statusLabel: "Status",
  sentByLabel: "Sent by",
  empty: "No service replies have been recorded for this feedback.",
  footer:
    "Messages sent through Tummly are shown here. Incoming replies are not shown in this view.",
  statusPending: "Delivery status pending.",
  statusAccepted: "Reply accepted for sending.",
  statusDelivered: "Provider reports delivered.",
  statusFailed:
    "The reply was not confirmed as sent. Check its status before trying again.",
  statusUnavailable:
    "Delivery status is unavailable. This does not mean the message was delivered.",
} as const

export type ServiceReplyDeliveryState = NonNullable<
  FeedbackDetailsActivityEventDto["deliveryState"]
>

export type ServiceReplyStatusTone = "default" | "muted" | "destructive"

export type ServiceReplyHistoryRow = {
  id: number | null
  sentAtLabel: string
  channelLabel: "Email" | "SMS"
  maskedRecipient: string
  statusText: string
  statusTone: ServiceReplyStatusTone
  sentBy: string
}

function statusCopy(
  deliveryState: FeedbackDetailsActivityEventDto["deliveryState"]
): { statusText: string; statusTone: ServiceReplyStatusTone } {
  switch (deliveryState) {
    case "pending":
      return {
        statusText: SERVICE_REPLY_HISTORY_COPY.statusPending,
        statusTone: "muted",
      }
    case "accepted":
      return {
        statusText: SERVICE_REPLY_HISTORY_COPY.statusAccepted,
        statusTone: "default",
      }
    case "delivered":
      return {
        statusText: SERVICE_REPLY_HISTORY_COPY.statusDelivered,
        statusTone: "default",
      }
    case "failed":
      return {
        statusText: SERVICE_REPLY_HISTORY_COPY.statusFailed,
        statusTone: "destructive",
      }
    default:
      // Unavailable, or a state this build does not know. Never claim delivery.
      return {
        statusText: SERVICE_REPLY_HISTORY_COPY.statusUnavailable,
        statusTone: "muted",
      }
  }
}

function sentAtMs(event: FeedbackDetailsActivityEventDto): number {
  return new Date(event.at).getTime()
}

/** Guest reply rows for the drawer, newest first. Other activity kinds are ignored. */
export function serviceReplyHistoryRows(
  events: FeedbackDetailsActivityEventDto[]
): ServiceReplyHistoryRow[] {
  return events
    .filter(
      (event) =>
        event.kind === "guest_response_sent" ||
        event.kind === "guest_response_not_sent"
    )
    .sort((a, b) => {
      const byTime = sentAtMs(b) - sentAtMs(a)
      if (byTime !== 0) {
        return byTime
      }
      return (b.guestResponseId ?? 0) - (a.guestResponseId ?? 0)
    })
    .map((event) => ({
      id: event.guestResponseId ?? null,
      sentAtLabel: formatGuestProfileAbsoluteDateTime(event.at),
      channelLabel: event.channel === "sms" ? "SMS" : "Email",
      maskedRecipient: event.maskedDestination ?? "",
      ...statusCopy(event.deliveryState),
      sentBy: event.actorDisplayName ?? "",
    }))
}

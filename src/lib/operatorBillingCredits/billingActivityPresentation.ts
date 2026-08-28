export type BillingActivityKind =
  | "subscription_created"
  | "subscription_upgraded"
  | "subscription_change_scheduled"
  | "additional_location_added"
  | "additional_location_remove_scheduled"
  | "subscription_cancelled"
  | "subscription_renewed"
  | "topup_purchased"
  | "topup_refunded"
  | "credit_consumed"
  | "credit_expired"
  | "manual_credit_adjusted"
  | "invoice_paid"
  | "credit_note_issued"
  | "payment_method_updated"
  | "soft_lock_entered"
  | "dormant_entered"

export type BillingActivitySnapshot = {
  kind: BillingActivityKind | string
  actorDisplayName?: string | null
  channel?: "email" | "sms" | "ai" | string | null
  qty?: number | null
  campaignName?: string | null
  invoiceNo?: string | null
  creditNoteNo?: string | null
  plan?: string | null
  cadence?: string | null
  scheduledDateLabel?: string | null
  locationName?: string | null
  manualAdjustDirection?: "add" | "remove" | string | null
  consumeSource?: "campaign" | "feedback_recovery" | string | null
}

function formatChannelCredits(
  channel: string | null | undefined,
  qty: number | null | undefined
): string {
  if (channel == null || qty == null) {
    return ""
  }

  const label =
    channel === "email"
      ? "Email credit"
      : channel === "sms"
        ? "SMS credit"
        : channel === "ai"
          ? "AI credit"
          : `${channel} credit`

  const plural =
    channel === "email"
      ? "Email credits"
      : channel === "sms"
        ? "SMS credits"
        : channel === "ai"
          ? "AI credits"
          : `${channel} credits`

  return qty === 1 ? `1 ${label}` : `${qty.toLocaleString("en-GB")} ${plural}`
}

function formatQtyChannel(
  channel: string | null | undefined,
  qty: number | null | undefined
): string {
  if (channel == null || qty == null) {
    return ""
  }

  const plural =
    channel === "email"
      ? "Email credits"
      : channel === "sms"
        ? "SMS credits"
        : channel === "ai"
          ? "AI credits"
          : `${channel} credits`

  const qtyLabel = qty === 1 ? "1" : qty.toLocaleString("en-GB")
  const channelLabel = qty === 1
    ? plural.replace(/ credits$/, " credit")
    : plural

  return `${qtyLabel} ${channelLabel}`
}

function formatPlanAndOrCadence(
  plan: string | null | undefined,
  cadence: string | null | undefined
): string {
  if (plan != null && cadence != null) {
    return `${plan}, ${cadence}`
  }
  if (plan != null) {
    return plan
  }
  if (cadence != null) {
    return cadence
  }
  return ""
}

function londonYmd(date: Date): string {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Europe/London",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(date)
}

function previousYmd(ymd: string): string {
  const [year, month, day] = ymd.split("-").map(Number)
  return new Date(Date.UTC(year, month - 1, day - 1))
    .toISOString()
    .slice(0, 10)
}

function londonTime(date: Date): string {
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "Europe/London",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(date)
}

export function formatBillingActivityOccurredAt(
  iso: string,
  now: Date
): string {
  const occurred = new Date(iso)
  const time = londonTime(occurred)
  const occurredDay = londonYmd(occurred)
  const today = londonYmd(now)
  if (occurredDay === today) {
    return `Today, ${time}`
  }
  if (occurredDay === previousYmd(today)) {
    return `Yesterday, ${time}`
  }
  const datePart = new Intl.DateTimeFormat("en-GB", {
    timeZone: "Europe/London",
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(occurred)
  return `${datePart}, ${time}`
}

export function formatBillingActivityCopy(
  row: BillingActivitySnapshot
): string {
  const actor = row.actorDisplayName ?? ""
  const channel = formatChannelCredits(row.channel, row.qty ?? 0)
  const plan = row.plan ?? ""
  const cadence = row.cadence ?? ""
  const planAndOrCadence = formatPlanAndOrCadence(plan, cadence)
  const date = row.scheduledDateLabel ?? ""

  switch (row.kind) {
    case "subscription_created":
      return `${actor} started ${plan} (${cadence}).`
    case "subscription_upgraded":
      return `${actor} upgraded to ${plan}.`
    case "subscription_change_scheduled":
      return `${actor} scheduled a change to ${planAndOrCadence} on ${date}.`
    case "additional_location_added":
      return `${actor} added an Additional Group Location (${row.locationName ?? ""}).`
    case "additional_location_remove_scheduled":
      return `${actor} scheduled removal of an Additional Group Location (${row.locationName ?? ""}) on ${date}.`
    case "subscription_cancelled":
      return `${actor} scheduled Cancel plan on ${date}.`
    case "subscription_renewed":
      return `${plan} renewed.`
    case "topup_purchased":
      return `${channel} added by ${actor}.`
    case "topup_refunded":
      return `${channel} refunded.`
    case "credit_consumed":
      if (row.consumeSource === "feedback_recovery") {
        return `${formatChannelCredits("sms", row.qty ?? 1)} used by Feedback recovery.`
      }
      return `${channel} used by ${row.campaignName ?? ""}.`
    case "credit_expired": {
      const qtyChannel = formatQtyChannel(row.channel, row.qty ?? 0)
      const channelWords = qtyChannel.split(" ").slice(1).join(" ")
      const qtyLabel = qtyChannel.split(" ")[0]
      return `${qtyLabel} purchased ${channelWords} expired.`
    }
    case "manual_credit_adjusted": {
      const qtyChannel = formatQtyChannel(row.channel, row.qty ?? 0)
      if (row.manualAdjustDirection === "remove") {
        return `Tummly Support removed ${qtyChannel}.`
      }
      return `Tummly Support added ${qtyChannel}.`
    }
    case "invoice_paid":
      return `Invoice ${row.invoiceNo ?? ""} paid.`
    case "credit_note_issued":
      return `Credit note ${row.creditNoteNo ?? ""} issued.`
    case "payment_method_updated":
      return `${actor} updated the payment method.`
    case "soft_lock_entered":
      return "Account entered Soft lock."
    case "dormant_entered":
      return "Account entered Dormant."
    default:
      return ""
  }
}

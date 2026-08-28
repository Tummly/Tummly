import axiosInstance from "@/api/axiosInstance"
import type {
  BillingActivityList,
  BillingAlertRoleFlags,
  BillingContactsSnapshot,
  BillingCreditsPageData,
  BillingPaymentFailureAlertFlags,
  PlanChangeRequest,
  PlanChangeResult,
  UpdateBillingContactsPayload,
} from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import type { CreditsUsageSnapshot } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

type PaymentMethodResponse = BillingCreditsPageData["paymentMethod"] extends infer T
  ? T extends { kind: string }
    ? {
        kind: string
        brand?: string
        last4?: string
        expiryLabel?: string
        walletName?: string
      }
    : never
  : never

type BillingContactsApiSnapshot = {
  billingContactUserId: number
  billingEmail: string | null
  eligibleMembers: Array<{
    userId: number
    fullName: string
    email: string
  }>
  lowCreditAlerts: BillingAlertRoleFlags
  paymentFailureAlerts: BillingPaymentFailureAlertFlags
}

type BillingCreditsPageResponse = {
  actorPermissionRole: string
  actorCanManage: boolean
  actorCanPersistBillingContacts: boolean
  planSubscription: BillingCreditsPageData["planSubscription"]
  paymentMethod: PaymentMethodResponse | null
  invoices: BillingCreditsPageData["invoices"]
  billingContacts: BillingContactsApiSnapshot
}

function mapPaymentMethod(
  raw: PaymentMethodResponse | null | undefined
): BillingCreditsPageData["paymentMethod"] {
  if (raw == null) {
    return null
  }

  if (raw.kind === "wallet") {
    return {
      kind: "wallet",
      walletName: raw.walletName,
    }
  }

  return {
    kind: "card",
    brand: raw.brand,
    last4: raw.last4,
    expiryLabel: raw.expiryLabel,
  }
}

function mapBillingContacts(
  data: BillingContactsApiSnapshot
): BillingContactsSnapshot {
  return {
    billingContactUserId: data.billingContactUserId,
    billingEmail: data.billingEmail ?? "",
    eligibleMembers: (data.eligibleMembers ?? []).map((member) => ({
      userId: member.userId,
      fullName: member.fullName,
      email: member.email,
    })),
    lowCreditAlerts: {
      owner: data.lowCreditAlerts.owner,
      admin: data.lowCreditAlerts.admin,
      billingContact: data.lowCreditAlerts.billingContact,
    },
    paymentFailureAlerts: {
      owner: data.paymentFailureAlerts.owner,
      billingContact: data.paymentFailureAlerts.billingContact,
    },
  }
}

type PlanChangeResponse = {
  outcome: "pay" | "scheduled"
  redirectUrl?: string | null
  scheduledChangeLine?: string | null
}

export async function getBillingCreditsPage(): Promise<BillingCreditsPageData> {
  const { data } = await axiosInstance.get<BillingCreditsPageResponse>(
    "/billing-credits"
  )
  return {
    actorPermissionRole: data.actorPermissionRole,
    actorCanManage: data.actorCanManage,
    actorCanPersistBillingContacts: data.actorCanPersistBillingContacts,
    planSubscription: data.planSubscription,
    paymentMethod: mapPaymentMethod(data.paymentMethod),
    invoices: data.invoices ?? [],
    billingContacts: mapBillingContacts(data.billingContacts),
  }
}

export async function updateBillingContacts(
  payload: UpdateBillingContactsPayload
): Promise<BillingContactsSnapshot> {
  const { data } = await axiosInstance.put<{
    billingContacts: BillingContactsApiSnapshot
  }>("/billing-credits/billing-contacts", {
    billingContactUserId: payload.billingContactUserId,
    billingEmail: payload.billingEmail.trim() === "" ? null : payload.billingEmail.trim(),
    lowCreditAlerts: {
      owner: payload.lowCreditAlerts.owner,
      admin: payload.lowCreditAlerts.admin,
      billingContact: payload.lowCreditAlerts.billingContact,
    },
    paymentFailureAlerts: {
      owner: payload.paymentFailureAlerts.owner,
      billingContact: payload.paymentFailureAlerts.billingContact,
    },
  })
  return mapBillingContacts(data.billingContacts)
}

export async function getBillingCreditsUsage(): Promise<CreditsUsageSnapshot> {
  const { data } = await axiosInstance.get<CreditsUsageSnapshot>(
    "/billing-credits/usage"
  )
  return data
}

export async function getBillingCreditsActivity(params: {
  page: number
  pageSize: number
}): Promise<BillingActivityList> {
  const { data } = await axiosInstance.get<BillingActivityList>(
    "/billing-credits/activity",
    { params }
  )
  return {
    items: (data.items ?? []).map((item) => ({
      id: item.id,
      kind: item.kind,
      occurredAt: item.occurredAt,
      actorDisplayName: item.actorDisplayName,
      channel: item.channel,
      qty: item.qty,
      campaignName: item.campaignName,
      invoiceNo: item.invoiceNo,
      creditNoteNo: item.creditNoteNo,
      plan: item.plan,
      cadence: item.cadence,
      scheduledDateLabel: item.scheduledDateLabel,
      locationName: item.locationName,
      manualAdjustDirection: item.manualAdjustDirection,
      consumeSource: item.consumeSource,
    })),
    totalCount: data.totalCount,
    page: data.page,
    pageSize: data.pageSize,
  }
}

export async function createPaymentMethodUpdateSession(): Promise<{
  redirectUrl: string
}> {
  const { data } = await axiosInstance.post<{ redirectUrl: string }>(
    "/billing-credits/payment-method/update"
  )
  return { redirectUrl: data.redirectUrl }
}

export async function fetchBillingCreditsInvoicePdf(
  invoiceNo: string
): Promise<Blob> {
  const encoded = encodeURIComponent(invoiceNo)
  const { data } = await axiosInstance.get<Blob>(
    `/billing-credits/invoices/${encoded}/pdf`,
    { responseType: "blob" }
  )
  return data
}

export function openInvoicePdfBlob(blob: Blob): void {
  const url = URL.createObjectURL(blob)
  window.open(url, "_blank", "noopener,noreferrer")
  window.setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export function downloadInvoicePdfBlob(blob: Blob, invoiceNo: string): void {
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement("a")
  anchor.href = url
  anchor.download = `${invoiceNo}.pdf`
  anchor.rel = "noopener noreferrer"
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

export async function submitBillingPlanChange(
  request: PlanChangeRequest
): Promise<PlanChangeResult> {
  const { data } = await axiosInstance.post<PlanChangeResponse>(
    "/billing-credits/plan-change",
    {
      targetPlan: request.targetPlan,
      targetCadence: request.targetCadence,
    }
  )
  return {
    outcome: data.outcome,
    redirectUrl: data.redirectUrl,
    scheduledChangeLine: data.scheduledChangeLine,
  }
}

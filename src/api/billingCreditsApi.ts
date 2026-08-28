import axiosInstance from "@/api/axiosInstance"
import type {
  BillingAlertRoleFlags,
  BillingContactsSnapshot,
  BillingCreditsPageData,
  BillingPaymentFailureAlertFlags,
  UpdateBillingContactsPayload,
} from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"

type BillingContactsPageResponse = {
  actorPermissionRole: string
  actorCanManage: boolean
  actorCanPersistBillingContacts: boolean
  planSubscription: BillingCreditsPageData["planSubscription"]
  billingContacts: BillingContactsApiSnapshot
}

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

export async function getBillingCreditsPage(): Promise<BillingCreditsPageData> {
  const { data } = await axiosInstance.get<BillingContactsPageResponse>(
    "/billing-credits"
  )
  return {
    actorPermissionRole: data.actorPermissionRole,
    actorCanManage: data.actorCanManage,
    actorCanPersistBillingContacts: data.actorCanPersistBillingContacts,
    planSubscription: data.planSubscription,
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

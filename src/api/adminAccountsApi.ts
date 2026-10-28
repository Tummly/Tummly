import axiosInstance from "./axiosInstance"

export type AdminAccountListRow = {
  restaurantId: number
  displayName: string
  subscriptionPlan: string
  billingStatus: string
  createdAtUtc: string
  /** Pilot end on a Pilot; current period end otherwise. */
  periodEndUtc: string | null
  activeLocationCount: number
  ownerDisplayName: string | null
}

export type AdminAccountListResponse = {
  success: boolean
  items: AdminAccountListRow[]
  totalCount: number
  skip: number
  take: number
}

export type AdminAccountListParams = {
  search?: string
  plan?: string
  billingStatus?: string
  /** Calendar day, YYYY-MM-DD, inclusive. */
  createdFrom?: string
  /** Calendar day, YYYY-MM-DD, inclusive. */
  createdTo?: string
  skip?: number
  take?: number
}

export type AdminAccountPlanLimit = {
  available: boolean
  cap?: number
  current?: number
  atCap?: boolean
}

export type AdminAccountProfile = {
  identity: {
    restaurantId: number
    displayName: string
    legalName: string | null
    ownerName: string | null
    ownerEmail: string | null
    createdAtUtc: string
    billingAccountId: number
  }
  lifecycle: {
    subscriptionPlan: string
    billingCycle: string | null
    billingStatus: string
    pilotStartedAtUtc: string | null
    pilotPeriodEndUtc: string | null
    currentPeriodStartUtc: string | null
    currentPeriodEndUtc: string | null
    storedAtUtc: string
    pilotEndPassedNotAdvanced: boolean
    scheduledChange: {
      hasScheduledChange: boolean
      targetSubscriptionPlan: string | null
      targetBillingCycle: string | null
      targetExtraLocationCount: number | null
      cancelScheduled: boolean
      effectiveAtUtc: string | null
      cancelAtUtc: string | null
    }
    softLockEnteredAtUtc: string | null
    dormantEnteredAtUtc: string | null
  }
  locations: {
    counts: {
      included: number
      paidExtra: number
      active: number
      total: number
    }
    items: {
      locationId: number
      name: string
      lifecycleStatus: string
      createdAtUtc: string
      assignedManagerCount: number
    }[]
  }
  capacity: {
    seats: AdminAccountPlanLimit
    qrPlacements: AdminAccountPlanLimit
    offers: AdminAccountPlanLimit
    pendingInvites: {
      maskedEmail: string
      sentAtUtc: string
      expiresAtUtc: string
    }[]
  }
  credits: {
    channel: "ai" | "email" | "sms" | string
    available: number
    reserved: number
    includedThisPeriod: number
    prepaidRemaining: number
    nearestPrepaidExpiryUtc: string | null
  }[]
  payments: {
    id: string
    kind: string
    amountPence: number
    currency: string
    isOpen: boolean
    createdAtUtc: string
    providerReference: string | null
  }[]
  invoices: {
    documentNumber: string
    type: "Invoice" | "Credit note" | string
    invoiceDateUtc: string
    grossPence: number
    currency: string
  }[]
  recentAudit: {
    id: string
    occurredAtUtc: string
    action: string
    actorIdentity: string
    targetType: string
    targetId: string
    detailJson: string | null
    succeeded: boolean
  }[]
}

export async function fetchAdminAccounts(
  params: AdminAccountListParams = {}
): Promise<AdminAccountListResponse> {
  const response = await axiosInstance.get<AdminAccountListResponse>(
    "/admin/accounts",
    { params }
  )
  return response.data
}

export async function fetchAdminAccountProfile(
  restaurantId: number
): Promise<AdminAccountProfile> {
  const response = await axiosInstance.get<{
    success: boolean
    profile: AdminAccountProfile
  }>(`/admin/accounts/${restaurantId}`)
  return response.data.profile
}

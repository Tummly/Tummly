import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"

export const BILLING_CREDITS_TAB_IDS = [
  "plan-subscription",
  "credits-usage",
  "payment-invoices",
  "billing-contacts",
  "activity",
] as const

export type BillingCreditsTabId = (typeof BILLING_CREDITS_TAB_IDS)[number]

export type BillingCreditsAccessLevel = "none" | "view" | "manage"

export type ManagePlanSection = "credit-top-ups" | null

const TAB_LABELS: Record<BillingCreditsTabId, string> = {
  "plan-subscription": "Plan & subscription",
  "credits-usage": "Credits & usage",
  "payment-invoices": "Payment & invoices",
  "billing-contacts": "Billing contacts",
  activity: "Activity",
}

export const BILLING_CREDITS_PAGE_COPY = {
  title: "Billing & credits",
  subtitle:
    "Manage your plan, payment method, invoices, credits and add-ons.",
  managePlan: "Manage plan",
  buyCredits: "Buy credits",
  changePlan: "Change plan",
  loadError: "Could not load billing & credits",
  retry: "Retry",
  breadcrumbBillingCredits: "Billing & credits",
  breadcrumbManagePlan: "Manage plan",
  planSubscriptionTitle: "Plan & subscription",
  currentPlan: "Current plan",
  billingStatus: "Billing status",
  renewalDate: "Renewal date",
  emailCredits: "Email credits",
  smsCredits: "SMS credits",
  aiCredits: "AI credits",
  planName: "Plan name",
  billingCycle: "Billing cycle",
  planPrice: "Plan price",
  includedLocations: "Included locations",
  activeLocations: "Active locations",
  includedEmailCredits: "Included Email credits",
  includedSmsCredits: "Included SMS credits",
  includedAiCredits: "Included AI credits",
  starterKit: "Starter kit",
  pricebook: "Pricebook",
  pilotNotice:
    "Pilot does not convert to a paid plan automatically. Credit top-ups are unavailable during Pilot.",
  managePlanPlanCards: "Plan cards",
  managePlanCreditTopUps: "Credit top-ups",
  plusVat: "+ VAT",
  creditsUsageTitle: "Usage & credits",
  creditsUsageSubtitle:
    "Track the credits and usage connected to campaigns, messages, and AI briefs.",
  creditsUsageTableTitle: "Usage this period",
  creditsUsageTableChannel: "Channel",
  creditsUsageTableUsed: "Used this cycle",
  creditsUsageTableIncluded: "Included this period",
  creditsUsageTablePurchased: "Purchased remaining",
  starterKitCardTitle: "Starter kit",
} as const

export function resolveBillingCreditsTabId(
  raw: string | null | undefined
): BillingCreditsTabId {
  if (
    raw != null
    && (BILLING_CREDITS_TAB_IDS as readonly string[]).includes(raw)
  ) {
    return raw as BillingCreditsTabId
  }
  return "plan-subscription"
}

export function billingCreditsTabLabels(): Array<{
  id: BillingCreditsTabId
  label: string
}> {
  return BILLING_CREDITS_TAB_IDS.map((id) => ({
    id,
    label: TAB_LABELS[id],
  }))
}

export function billingCreditsHeaderActions(options: {
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
}): {
  showManagePlan: boolean
  showBuyCredits: boolean
  showChangePlan: boolean
} {
  if (options.accessLevel !== "manage") {
    return {
      showManagePlan: false,
      showBuyCredits: false,
      showChangePlan: false,
    }
  }

  const isOwner = options.permissionRole === "Owner"
  const isBillingAdmin = options.permissionRole === "Billing Admin"
  const isAdminManage =
    options.permissionRole === "Admin" && options.accessLevel === "manage"

  return {
    showManagePlan: isOwner,
    showBuyCredits: isOwner || isBillingAdmin || isAdminManage,
    showChangePlan: isOwner,
  }
}

export function operatorDashboardBillingCreditsPath(
  mode: OperatorDashboardMode,
  locationId: number,
  options?: { tab?: BillingCreditsTabId }
): string {
  const root = mode === "single" ? "/single-dashboard" : "/multi-dashboard"
  const params = new URLSearchParams({ location: String(locationId) })
  const tab = options?.tab ?? "plan-subscription"
  params.set("tab", tab)
  return `${root}/settings/billing-credits?${params.toString()}`
}

export function operatorDashboardBillingCreditsManagePlanPath(
  mode: OperatorDashboardMode,
  locationId: number,
  options?: { section?: "credit-top-ups" }
): string {
  const root = mode === "single" ? "/single-dashboard" : "/multi-dashboard"
  const params = new URLSearchParams({ location: String(locationId) })
  if (options?.section != null) {
    params.set("section", options.section)
  }
  return `${root}/settings/billing-credits/manage-plan?${params.toString()}`
}

export function formatCreditsRemaining(count: number, label: string): string {
  return `${count.toLocaleString("en-GB")} ${label}`
}

export function formatStarterKitState(state: string): string {
  switch (state) {
    case "unused":
      return "Unused"
    case "used":
      return "Used"
    case "pending dispatch":
      return "Pending dispatch"
    default:
      return state
  }
}

export function resolveManagePlanSection(
  raw: string | null | undefined
): ManagePlanSection {
  return raw === "credit-top-ups" ? "credit-top-ups" : null
}

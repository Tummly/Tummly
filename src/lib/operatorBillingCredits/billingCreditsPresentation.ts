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
  currentPlanSubtitle:
    "Review your Tummly plan, included locations and renewal details.",
  billingStatus: "Billing status",
  renewalDate: "Renewal date",
  emailCredits: "Email credits",
  smsCredits: "SMS credits",
  aiCredits: "AI credits",
  qrPacks: "QR packs",
  planName: "Plan name",
  billingCycle: "Billing cycle",
  planPrice: "Plan price",
  includedLocations: "Included locations",
  activeLocations: "Active locations",
  includedEmailCredits: "Included email sends",
  includedSmsCredits: "Included SMS credits",
  includedAiCredits: "Included AI credits",
  qrStarterKit: "QR starter kit",
  qrStarterKitIncluded: "Included once",
  starterKit: "Starter kit",
  pricebook: "Pricebook",
  pilotNotice:
    "Pilot does not convert to a paid plan automatically. Credit top-ups are unavailable during Pilot.",
  topUpPilotNotice:
    "Credit top-ups need a paid plan. Pilot does not convert to a paid plan automatically.",
  managePlanPlanCards: "Plan cards",
  managePlanCreditTopUps: "Credit top-ups",
  plusVat: "+ VAT",
  creditsUsageTitle: "Usage & credits",
  creditsUsageSubtitle:
    "Track the credits and usage connected to campaigns, messages, AI briefs and QR packs.",
  creditsUsageTableUsageType: "Usage type",
  creditsUsageTableThisCycle: "This cycle",
  creditsUsageTableIncluded: "Included",
  creditsUsageTableExtraUsed: "Extra used",
  creditsUsageTableEstimatedCharge: "Estimated charge",
  /** @deprecated Prefer creditsUsageTable* Figma columns. */
  creditsUsageTableTitle: "Usage this period",
  creditsUsageTableChannel: "Channel",
  creditsUsageTableUsed: "Used this cycle",
  creditsUsageTablePurchased: "Purchased remaining",
  starterKitCardTitle: "Starter kit",
  qrPrintPacksTitle: "QR print packs",
  viewUsage: "View usage",
  reorderPrintPack: "Reorder print pack",
  creditTopUpsTitle: "Credit top-ups",
  creditTopUpsSubtitle:
    "Add credits when you need to send more messages, generate more AI help or reorder QR materials.",
  creditTopUpsCurrentBalance: "Current balance",
  qrPrintPacksShop: "Shop",
  qrPrintPacksDetail:
    "Reorder counter cards, table tents, window stickers and other print-ready QR materials.",
  paymentMethodTitle: "Payment method",
  paymentMethodSubtitle:
    "Manage the payment method used for your Tummly subscription, credits and add-ons.",
  noPaymentMethodOnFile: "No payment method on file.",
  updatePaymentMethod: "Update payment method",
  updatePaymentMethodConfirmTitle: "Update payment method",
  updatePaymentMethodConfirmBody:
    "You will open Revolut to save a new payment method. This does not charge you now.",
  continue: "Continue",
  billingContactsTitle: "Billing contacts",
  billingContactsSubtitle:
    "Choose who receives invoices, payment notices and credit warnings.",
  billingContact: "Billing contact",
  billingEmail: "Billing email",
  billingEmailPlaceholder: "Enter your email",
  lowCreditAlerts: "Low-credit alerts",
  paymentFailureAlerts: "Payment failure alerts",
  alertOwner: "Owner",
  alertAdmin: "Admin",
  alertBillingContact: "Billing contact",
  updateBillingContact: "Update billing contact",
  selectUserPlaceholder: "Select user",
  billingContactsSaveSuccess: "Billing contacts updated.",
  billingContactsSaveError:
    "Could not update billing contacts. Please try again.",
  invoicesTitle: "Invoices",
  invoicesSubtitle:
    "View and download invoices for your Tummly subscription, credits and add-ons.",
  noInvoicesYet: "No invoices yet.",
  invoiceDate: "Invoice date",
  invoiceNo: "Invoice no.",
  description: "Description",
  amount: "Amount",
  status: "Status",
  actions: "Actions",
  view: "View",
  download: "Download",
  billingActivityTitle: "Billing activity",
  billingActivitySubtitle:
    "Review recent plan, payment, credit and invoice activity.",
  billingActivityEmpty: "No billing activity yet.",
  viewFullBillingHistory: "View full billing history",
  billingActivitySheetTitle: "Billing activity",
  subscriptionChangesTitle: "Subscription changes",
  subscriptionChangesSubtitle:
    "Need to change your subscription? You can downgrade, cancel or contact support before making changes that affect your account.",
  downgradePlan: "Downgrade plan",
  cancelSubscription: "Cancel subscription",
  contactBillingSupport: "Contact billing support",
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
  options?: { section?: "credit-top-ups"; channel?: "sms" | "email" | "ai" }
): string {
  const root = mode === "single" ? "/single-dashboard" : "/multi-dashboard"
  const params = new URLSearchParams({ location: String(locationId) })
  if (options?.section != null) {
    params.set("section", options.section)
  }
  if (options?.channel != null) {
    params.set("channel", options.channel)
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

/** Plan overview QR packs value — Figma 3762:24014. */
export function formatQrPacksLabel(state: string): string {
  switch (state) {
    case "unused":
      return "Starter kit unused"
    case "used":
      return "Starter kit used"
    case "pending dispatch":
      return "Starter kit pending dispatch"
    default:
      return state
  }
}

/** Plan overview card — Figma 3762:24022. */
export const BILLING_PLAN_OVERVIEW_CARD_CLASS =
  "overflow-clip rounded-op-lg border border-op-card-border bg-op-surface-primary p-6 dark:bg-op-color-gray-992 dark:shadow-none"

/** 20px vertical rhythm between metric rows and dividers. */
export const BILLING_PLAN_METRIC_STACK_CLASS = "flex w-full flex-col gap-5"

/** Two metric pairs per row — 40px column gutter at sm+. */
export const BILLING_PLAN_METRIC_ROW_CLASS =
  "flex w-full flex-col gap-5 sm:flex-row sm:items-center sm:gap-10"

export const BILLING_PLAN_METRIC_PAIR_CLASS =
  "flex min-w-0 flex-1 items-center justify-between gap-4"

export const BILLING_PLAN_METRIC_LABEL_CLASS =
  "m-0 shrink-0 text-base font-semibold leading-normal text-[var(--op-color-gray-550)]"

export const BILLING_PLAN_METRIC_VALUE_CLASS =
  "m-0 text-right text-base font-medium leading-normal text-foreground"

export const BILLING_PLAN_METRIC_DIVIDER_CLASS =
  "m-0 h-px w-full shrink-0 border-0 bg-op-card-border"

/** Plan & subscription tab stack — 20px between cards (Figma 3762:24014). */
export const BILLING_PLAN_TAB_STACK_CLASS = "flex flex-col gap-5"

/** Credits & usage outer card — Figma 5746:96542. */
export const BILLING_CREDITS_USAGE_CARD_CLASS =
  "overflow-clip rounded-op-lg border border-op-card-border bg-op-surface-primary dark:bg-op-color-gray-992 dark:shadow-none"

export const BILLING_CREDITS_USAGE_CARD_HEADER_CLASS =
  "flex flex-col gap-2 p-6"

export const BILLING_CREDITS_USAGE_GRID_CLASS =
  "grid grid-cols-1 sm:grid-cols-2"

export const BILLING_CREDITS_USAGE_CELL_CLASS =
  "flex min-h-0 flex-col items-start justify-between gap-10 border-b border-op-card-border p-6 sm:odd:border-r"

export const BILLING_CREDITS_USAGE_TABLE_WRAP_CLASS = "p-6"

/** Credit top-ups embedded cards — Figma 5746:96612. */
export const BILLING_CREDIT_TOP_UP_CARD_CLASS =
  "flex flex-col items-start justify-between gap-8 rounded-op-md bg-[var(--op-color-gray-990)] p-6 dark:bg-[#202020]"

export const BILLING_CREDITS_CTA_BUTTON_CLASS =
  "w-auto shrink-0 self-start"

export const BILLING_CREDIT_TOP_UP_BALANCE_VALUE_CLASS =
  "m-0 text-lg font-medium leading-normal text-op-card-title-color"

export const BILLING_CREDIT_TOP_UP_BALANCE_CAPTION_CLASS =
  "m-0 text-xs font-normal leading-normal text-[var(--op-color-gray-550)]"

export function resolveManagePlanSection(
  raw: string | null | undefined
): ManagePlanSection {
  return raw === "credit-top-ups" ? "credit-top-ups" : null
}


export const BILLING_CREDITS_SELECT_MENU_CLASS = "z-[130]"

export type PaymentMethodSnapshot = {
  kind: "card" | "wallet"
  brand?: string
  last4?: string
  expiryLabel?: string
  walletName?: string
}

export type InvoiceRowSnapshot = {
  invoiceNo: string
  invoiceDateLabel: string
  description: string
  amountLabel: string
  status: string
  showActions: boolean
}

export function formatPaymentMethodLabel(
  paymentMethod: PaymentMethodSnapshot | null
): string | null {
  if (paymentMethod == null) {
    return null
  }

  if (paymentMethod.kind === "wallet") {
    return paymentMethod.walletName ?? null
  }

  if (
    paymentMethod.brand == null
    || paymentMethod.last4 == null
    || paymentMethod.expiryLabel == null
  ) {
    return null
  }

  return `${paymentMethod.brand} · ···· ${paymentMethod.last4} · ${paymentMethod.expiryLabel}`
}

export function billingCreditsPaymentInvoicesActions(options: {
  accessLevel: BillingCreditsAccessLevel
  isPilot: boolean
  /** Soft lock / Dormant — Pilot shows Update payment method disabled. */
  accountLocked?: boolean
}): {
  showUpdatePaymentMethod: boolean
  updatePaymentMethodDisabled: boolean
} {
  if (options.accessLevel !== "manage") {
    return {
      showUpdatePaymentMethod: false,
      updatePaymentMethodDisabled: false,
    }
  }
  if (options.isPilot) {
    const locked = options.accountLocked === true
    return {
      showUpdatePaymentMethod: locked,
      updatePaymentMethodDisabled: locked,
    }
  }
  return {
    showUpdatePaymentMethod: true,
    updatePaymentMethodDisabled: false,
  }
}

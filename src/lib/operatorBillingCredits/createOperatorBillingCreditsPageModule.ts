import type {
  BillingCreditsAccessLevel,
  BillingCreditsTabId,
  InvoiceRowSnapshot,
  ManagePlanSection,
  PaymentMethodSnapshot,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  BILLING_CREDITS_PAGE_COPY,
  billingCreditsHeaderActions,
  billingCreditsPaymentInvoicesActions,
  billingCreditsTabLabels,
  formatPaymentMethodLabel,
  resolveBillingCreditsTabId,
  resolveManagePlanSection,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"

export type PlanSubscriptionSnapshot = {
  subscriptionPlan: string
  billingStatus: string
  renewalDateLabel: string | null
  emailCreditsRemaining: number
  smsCreditsRemaining: number
  aiCreditsRemaining: number
  billingCycle: string | null
  planPriceNet: string
  includedLocations: number
  activeLocations: number
  includedEmailCreditsLabel: string
  includedSmsCreditsLabel: string
  includedAiCreditsLabel: string
  starterKitState: string
  pricebookId: string
  scheduledChangeLine: string | null
  isPilot: boolean
}

export type BillingCreditsPageData = {
  actorPermissionRole: string
  actorCanManage: boolean
  planSubscription: PlanSubscriptionSnapshot
  paymentMethod: PaymentMethodSnapshot | null
  invoices: InvoiceRowSnapshot[]
}

export type BillingCreditsPageAdapters = {
  getPage: () => Promise<BillingCreditsPageData>
  createPaymentMethodUpdateSession?: () => Promise<{ redirectUrl: string }>
  fetchInvoicePdf?: (invoiceNo: string) => Promise<Blob>
  openInvoicePdf?: (blob: Blob) => void
  downloadInvoicePdf?: (blob: Blob, invoiceNo: string) => void
}

export type BillingCreditsSurface = "tabs" | "manage-plan"

export type BillingCreditsSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error" | "forbidden"
  surface: BillingCreditsSurface
  activeTabId: BillingCreditsTabId
  tabs: Array<{ id: BillingCreditsTabId; label: string }>
  accessLevel: BillingCreditsAccessLevel
  actorPermissionRole: string
  showManagePlan: boolean
  showBuyCredits: boolean
  showChangePlan: boolean
  managePlanSection: ManagePlanSection
  planSubscription: PlanSubscriptionSnapshot | null
  paymentMethodLabel: string | null
  showNoPaymentMethodOnFile: boolean
  showUpdatePaymentMethod: boolean
  showNoInvoicesYet: boolean
  invoices: InvoiceRowSnapshot[]
  updatePaymentMethodConfirmOpen: boolean
  updatePaymentMethodConfirmCopy: {
    title: string
    body: string
    continueLabel: string
    cancelLabel: string
  }
  pendingPaymentMethodRedirectUrl: string | null
  pendingNavigationHref: string | null
  managePlanHref: string | null
  buyCreditsHref: string | null
  breadcrumbHref: string | null
}

export type OperatorBillingCreditsPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => BillingCreditsSnapshot
  load: () => Promise<void>
  setActiveTabFromUrl: (raw: string | null) => void
  requestTabChange: (tabId: BillingCreditsTabId) => void
  setSurface: (surface: BillingCreditsSurface) => void
  setManagePlanSectionFromUrl: (raw: string | null) => void
  setNavigationTargets: (targets: {
    mode: "single" | "multi"
    locationId: number
  }) => void
  openManagePlan: () => void
  openBuyCredits: () => void
  openChangePlan: () => void
  scrollManagePlanToCards: () => void
  consumePendingNavigation: () => string | null
  openUpdatePaymentMethodConfirm: () => void
  dismissUpdatePaymentMethodConfirm: () => void
  confirmUpdatePaymentMethod: () => Promise<void>
  consumePendingPaymentMethodRedirect: () => string | null
  viewInvoicePdf: (invoiceNo: string) => Promise<void>
  downloadInvoicePdf: (invoiceNo: string) => Promise<void>
}

function emptyPlanSnapshot(): PlanSubscriptionSnapshot {
  return {
    subscriptionPlan: "",
    billingStatus: "",
    renewalDateLabel: null,
    emailCreditsRemaining: 0,
    smsCreditsRemaining: 0,
    aiCreditsRemaining: 0,
    billingCycle: null,
    planPriceNet: "",
    includedLocations: 0,
    activeLocations: 0,
    includedEmailCreditsLabel: "",
    includedSmsCreditsLabel: "",
    includedAiCreditsLabel: "",
    starterKitState: "",
    pricebookId: "",
    scheduledChangeLine: null,
    isPilot: false,
  }
}

export function createOperatorBillingCreditsPageModule(
  adapters: BillingCreditsPageAdapters,
  options: {
    initialTabId?: string | null
    initialSurface?: BillingCreditsSurface
    initialManagePlanSection?: string | null
  } = {}
): OperatorBillingCreditsPageModule {
  let data: BillingCreditsPageData | null = null
  let loadStatus: BillingCreditsSnapshot["loadStatus"] = "idle"
  let activeTabId = resolveBillingCreditsTabId(options.initialTabId)
  let surface: BillingCreditsSurface = options.initialSurface ?? "tabs"
  let managePlanSection = resolveManagePlanSection(
    options.initialManagePlanSection
  )
  let updatePaymentMethodConfirmOpen = false
  let pendingPaymentMethodRedirectUrl: string | null = null
  let navMode: "single" | "multi" = "single"
  let locationId = 0
  let pendingNavigationHref: string | null = null
  const listeners = new Set<() => void>()

  const accessLevel = (): BillingCreditsAccessLevel => {
    if (data == null) {
      return "none"
    }
    return data.actorCanManage ? "manage" : "view"
  }

  const headerActions = () =>
    billingCreditsHeaderActions({
      accessLevel: accessLevel(),
      permissionRole: data?.actorPermissionRole ?? "",
    })

  const buildManagePlanHref = (section?: ManagePlanSection) => {
    if (locationId <= 0) {
      return null
    }
    const root =
      navMode === "single" ? "/single-dashboard" : "/multi-dashboard"
    const params = new URLSearchParams({ location: String(locationId) })
    if (section === "credit-top-ups") {
      params.set("section", "credit-top-ups")
    }
    return `${root}/settings/billing-credits/manage-plan?${params.toString()}`
  }

  const buildBillingCreditsHref = (tab?: BillingCreditsTabId) => {
    if (locationId <= 0) {
      return null
    }
    const root =
      navMode === "single" ? "/single-dashboard" : "/multi-dashboard"
    const params = new URLSearchParams({
      location: String(locationId),
      tab: tab ?? activeTabId,
    })
    return `${root}/settings/billing-credits?${params.toString()}`
  }

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const paymentInvoicesActions = () =>
    billingCreditsPaymentInvoicesActions({
      accessLevel: accessLevel(),
      isPilot: data?.planSubscription?.isPilot ?? true,
    })

  const projectSnapshot = (): BillingCreditsSnapshot => {
    const actions = headerActions()
    const paymentActions = paymentInvoicesActions()
    const paymentMethod = data?.paymentMethod ?? null
    const invoices = data?.invoices ?? []
    return {
      loadStatus,
      surface,
      activeTabId: resolveBillingCreditsTabId(activeTabId),
      tabs: billingCreditsTabLabels(),
      accessLevel: accessLevel(),
      actorPermissionRole: data?.actorPermissionRole ?? "",
      showManagePlan: actions.showManagePlan,
      showBuyCredits: actions.showBuyCredits,
      showChangePlan: actions.showChangePlan,
      managePlanSection,
      planSubscription: data?.planSubscription ?? null,
      paymentMethodLabel: formatPaymentMethodLabel(paymentMethod),
      showNoPaymentMethodOnFile: paymentMethod == null,
      showUpdatePaymentMethod: paymentActions.showUpdatePaymentMethod,
      showNoInvoicesYet: invoices.length === 0,
      invoices,
      updatePaymentMethodConfirmOpen,
      updatePaymentMethodConfirmCopy: {
        title: BILLING_CREDITS_PAGE_COPY.updatePaymentMethodConfirmTitle,
        body: BILLING_CREDITS_PAGE_COPY.updatePaymentMethodConfirmBody,
        continueLabel: BILLING_CREDITS_PAGE_COPY.continue,
        cancelLabel: BILLING_CREDITS_PAGE_COPY.cancel,
      },
      pendingPaymentMethodRedirectUrl,
      pendingNavigationHref,
      managePlanHref: buildManagePlanHref(null),
      buyCreditsHref: buildManagePlanHref("credit-top-ups"),
      breadcrumbHref: buildBillingCreditsHref("plan-subscription"),
    }
  }

  let snapshot = projectSnapshot()

  const refreshSnapshot = () => {
    snapshot = projectSnapshot()
    emit()
  }

  const reload = async () => {
    loadStatus = "loading"
    refreshSnapshot()
    try {
      data = await adapters.getPage()
      loadStatus = "loaded"
    } catch (error) {
      const status =
        typeof error === "object"
        && error != null
        && "response" in error
        && typeof (error as { response?: { status?: number } }).response
          ?.status === "number"
          ? (error as { response: { status: number } }).response.status
          : null
      loadStatus = status === 403 ? "forbidden" : "error"
    }
    refreshSnapshot()
  }

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot: () => snapshot,
    load: reload,
    setActiveTabFromUrl: (raw) => {
      activeTabId = resolveBillingCreditsTabId(raw)
      refreshSnapshot()
    },
    requestTabChange: (tabId) => {
      activeTabId = resolveBillingCreditsTabId(tabId)
      refreshSnapshot()
    },
    setSurface: (nextSurface) => {
      surface = nextSurface
      refreshSnapshot()
    },
    setManagePlanSectionFromUrl: (raw) => {
      managePlanSection = resolveManagePlanSection(raw)
      refreshSnapshot()
    },
    setNavigationTargets: (targets) => {
      navMode = targets.mode
      locationId = targets.locationId
      refreshSnapshot()
    },
    openManagePlan: () => {
      const href = buildManagePlanHref(null)
      if (href == null) {
        return
      }
      pendingNavigationHref = href
      refreshSnapshot()
    },
    openBuyCredits: () => {
      const href = buildManagePlanHref("credit-top-ups")
      if (href == null) {
        return
      }
      pendingNavigationHref = href
      refreshSnapshot()
    },
    openChangePlan: () => {
      const href = buildManagePlanHref(null)
      if (href == null) {
        return
      }
      pendingNavigationHref = href
      refreshSnapshot()
    },
    scrollManagePlanToCards: () => {
      managePlanSection = null
      refreshSnapshot()
    },
    consumePendingNavigation: () => {
      const href = pendingNavigationHref
      pendingNavigationHref = null
      refreshSnapshot()
      return href
    },
    openUpdatePaymentMethodConfirm: () => {
      updatePaymentMethodConfirmOpen = true
      refreshSnapshot()
    },
    dismissUpdatePaymentMethodConfirm: () => {
      updatePaymentMethodConfirmOpen = false
      refreshSnapshot()
    },
    confirmUpdatePaymentMethod: async () => {
      if (adapters.createPaymentMethodUpdateSession == null) {
        return
      }
      updatePaymentMethodConfirmOpen = false
      refreshSnapshot()
      const session = await adapters.createPaymentMethodUpdateSession()
      pendingPaymentMethodRedirectUrl = session.redirectUrl
      refreshSnapshot()
    },
    consumePendingPaymentMethodRedirect: () => {
      const href = pendingPaymentMethodRedirectUrl
      pendingPaymentMethodRedirectUrl = null
      refreshSnapshot()
      return href
    },
    viewInvoicePdf: async (invoiceNo) => {
      if (adapters.fetchInvoicePdf == null || adapters.openInvoicePdf == null) {
        return
      }
      const blob = await adapters.fetchInvoicePdf(invoiceNo)
      adapters.openInvoicePdf(blob)
    },
    downloadInvoicePdf: async (invoiceNo) => {
      if (
        adapters.fetchInvoicePdf == null
        || adapters.downloadInvoicePdf == null
      ) {
        return
      }
      const blob = await adapters.fetchInvoicePdf(invoiceNo)
      adapters.downloadInvoicePdf(blob, invoiceNo)
    },
  }
}

export {
  BILLING_CREDITS_TAB_IDS,
  resolveBillingCreditsTabId,
  resolveManagePlanSection,
}

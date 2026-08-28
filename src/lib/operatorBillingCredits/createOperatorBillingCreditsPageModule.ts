import type {
  BillingCreditsAccessLevel,
  BillingCreditsTabId,
  ManagePlanSection,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  billingCreditsHeaderActions,
  billingCreditsTabLabels,
  resolveBillingCreditsTabId,
  resolveManagePlanSection,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  buildCreditChannelCardViewModel,
  buildCreditsUsageTableRows,
  type CreditChannelCardViewModel,
  type CreditChannelId,
  type CreditsUsageSnapshot,
  type CreditsUsageTableRowViewModel,
} from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import {
  buildManagePlanCardViewModels,
  buildPlanChangeConfirmCopy,
  defaultPreviewCadence,
  formatCurrentPlanSummary,
  liveCadenceFromSnapshot,
  resolvePlanChangeKind,
  type BillingCadence,
  type ManagePlanCardViewModel,
  type ManagePlanId,
  type PlanChangeKind,
} from "@/lib/operatorBillingCredits/managePlanPresentation"

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
}

export type PlanChangeRequest = {
  targetPlan: ManagePlanId
  targetCadence: BillingCadence
}

export type PlanChangeResult = {
  outcome: "pay" | "scheduled"
  redirectUrl?: string | null
  scheduledChangeLine?: string | null
}

export type PlanChangeConfirmDialog = {
  open: boolean
  title: string
  body: string
  primaryLabel: string
  busy: boolean
  targetPlanId: ManagePlanId
  changeKind: PlanChangeKind
}

export type BillingCreditsPageAdapters = {
  getPage: () => Promise<BillingCreditsPageData>
  getUsage: () => Promise<CreditsUsageSnapshot>
  submitPlanChange: (request: PlanChangeRequest) => Promise<PlanChangeResult>
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
  creditsUsage: CreditsUsageSnapshot | null
  channelCards: CreditChannelCardViewModel[]
  usageTableRows: CreditsUsageTableRowViewModel[]
  pendingNavigationHref: string | null
  managePlanHref: string | null
  buyCreditsHref: string | null
  breadcrumbHref: string | null
  showPlanCards: boolean
  previewCadence: BillingCadence
  managePlanCards: ManagePlanCardViewModel[]
  currentPlanSummary: string | null
  planChangeConfirm: PlanChangeConfirmDialog | null
  pendingPayRedirectUrl: string | null
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
  openBuyChannelCredits: (channel: CreditChannelId) => void
  openChangePlan: () => void
  scrollManagePlanToCards: () => void
  consumePendingNavigation: () => string | null
  setPreviewCadence: (cadence: BillingCadence) => void
  requestPlanChange: (targetPlanId: ManagePlanId) => void
  cancelPlanChange: () => void
  confirmPlanChange: () => Promise<void>
  consumePendingPayRedirect: () => string | null
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
  let creditsUsage: CreditsUsageSnapshot | null = null
  let loadStatus: BillingCreditsSnapshot["loadStatus"] = "idle"
  let activeTabId = resolveBillingCreditsTabId(options.initialTabId)
  let surface: BillingCreditsSurface = options.initialSurface ?? "tabs"
  let managePlanSection = resolveManagePlanSection(
    options.initialManagePlanSection
  )
  let navMode: "single" | "multi" = "single"
  let locationId = 0
  let pendingNavigationHref: string | null = null
  let previewCadence: BillingCadence = "monthly"
  let planChangeConfirm: PlanChangeConfirmDialog | null = null
  let pendingPayRedirectUrl: string | null = null
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

  const buildManagePlanHref = (
    section?: ManagePlanSection,
    channel?: CreditChannelId
  ) => {
    if (locationId <= 0) {
      return null
    }
    const root =
      navMode === "single" ? "/single-dashboard" : "/multi-dashboard"
    const params = new URLSearchParams({ location: String(locationId) })
    if (section === "credit-top-ups") {
      params.set("section", "credit-top-ups")
    }
    if (channel != null) {
      params.set("channel", channel)
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

  const projectChannelCards = (): CreditChannelCardViewModel[] => {
    if (creditsUsage == null) {
      return []
    }
    return creditsUsage.channels.map((record) =>
      buildCreditChannelCardViewModel(record, {
        accessLevel: accessLevel(),
        permissionRole: data?.actorPermissionRole ?? "",
        isPilot: creditsUsage.isPilot,
      })
    )
  }

  const projectUsageTableRows = (): CreditsUsageTableRowViewModel[] => {
    if (creditsUsage == null) {
      return []
    }
    return buildCreditsUsageTableRows(creditsUsage.channels)
  }

  const projectSnapshot = (): BillingCreditsSnapshot => {
    const actions = headerActions()
    const plan = data?.planSubscription ?? null
    const showPlanCards =
      actions.showManagePlan && managePlanSection == null
    const managePlanCards =
      plan != null && showPlanCards
        ? buildManagePlanCardViewModels({ plan, previewCadence })
        : []

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
      planSubscription: plan,
      creditsUsage,
      channelCards: projectChannelCards(),
      usageTableRows: projectUsageTableRows(),
      pendingNavigationHref,
      managePlanHref: buildManagePlanHref(null),
      buyCreditsHref: buildManagePlanHref("credit-top-ups"),
      breadcrumbHref: buildBillingCreditsHref("plan-subscription"),
      showPlanCards,
      previewCadence,
      managePlanCards,
      currentPlanSummary:
        plan != null ? formatCurrentPlanSummary(plan) : null,
      planChangeConfirm,
      pendingPayRedirectUrl,
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
      const [page, usage] = await Promise.all([
        adapters.getPage(),
        adapters.getUsage(),
      ])
      data = page
      creditsUsage = usage
      previewCadence = defaultPreviewCadence(data.planSubscription)
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
    openBuyChannelCredits: (channel: CreditChannelId) => {
      const href = buildManagePlanHref("credit-top-ups", channel)
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
    setPreviewCadence: (cadence) => {
      previewCadence = cadence
      refreshSnapshot()
    },
    requestPlanChange: (targetPlanId) => {
      const plan = data?.planSubscription
      if (plan == null) {
        return
      }

      const currentPlanId = plan.subscriptionPlan as ManagePlanId
      const liveCadence = liveCadenceFromSnapshot(plan)
      const changeKind = resolvePlanChangeKind({
        currentPlanId,
        targetPlanId,
        liveCadence,
        previewCadence,
      })
      const copy = buildPlanChangeConfirmCopy({
        currentPlanId,
        targetPlanId,
        changeKind,
        previewCadence,
        renewalDateLabel: plan.renewalDateLabel,
      })

      planChangeConfirm = {
        open: true,
        title: copy.title,
        body: copy.body,
        primaryLabel: copy.primaryLabel,
        busy: false,
        targetPlanId,
        changeKind,
      }
      refreshSnapshot()
    },
    cancelPlanChange: () => {
      planChangeConfirm = null
      refreshSnapshot()
    },
    confirmPlanChange: async () => {
      if (planChangeConfirm == null) {
        return
      }

      planChangeConfirm = {
        ...planChangeConfirm,
        busy: true,
      }
      refreshSnapshot()

      try {
        const result = await adapters.submitPlanChange({
          targetPlan: planChangeConfirm.targetPlanId,
          targetCadence: previewCadence,
        })
        planChangeConfirm = null

        if (result.outcome === "pay" && result.redirectUrl != null) {
          pendingPayRedirectUrl = result.redirectUrl
          refreshSnapshot()
          return
        }

        const href = buildBillingCreditsHref("plan-subscription")
        if (href != null) {
          pendingNavigationHref = href
        }
        refreshSnapshot()
      } catch {
        planChangeConfirm = {
          ...planChangeConfirm,
          busy: false,
        }
        refreshSnapshot()
      }
    },
    consumePendingPayRedirect: () => {
      const url = pendingPayRedirectUrl
      pendingPayRedirectUrl = null
      refreshSnapshot()
      return url
    },
  }
}

export {
  BILLING_CREDITS_TAB_IDS,
  resolveBillingCreditsTabId,
  resolveManagePlanSection,
}

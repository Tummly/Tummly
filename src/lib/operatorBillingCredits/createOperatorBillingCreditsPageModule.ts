import type {
  BillingCreditsAccessLevel,
  BillingCreditsTabId,
  ManagePlanSection,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  BILLING_CREDITS_PAGE_COPY,
  billingCreditsHeaderActions,
  billingCreditsTabLabels,
  resolveBillingCreditsTabId,
  resolveManagePlanSection,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"

export type TeamMemberPickerItem = {
  userId: number
  fullName: string
  email: string
}

export type BillingAlertRoleFlags = {
  owner: boolean
  admin: boolean
  billingContact: boolean
}

export type BillingPaymentFailureAlertFlags = {
  owner: boolean
  billingContact: boolean
}

export type BillingContactsSnapshot = {
  billingContactUserId: number
  billingEmail: string
  eligibleMembers: TeamMemberPickerItem[]
  lowCreditAlerts: BillingAlertRoleFlags
  paymentFailureAlerts: BillingPaymentFailureAlertFlags
}

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
  actorCanPersistBillingContacts: boolean
  planSubscription: PlanSubscriptionSnapshot
  billingContacts: BillingContactsSnapshot
}

export type UpdateBillingContactsPayload = {
  billingContactUserId: number
  billingEmail: string
  lowCreditAlerts: BillingAlertRoleFlags
  paymentFailureAlerts: BillingPaymentFailureAlertFlags
}

export type BillingCreditsPageAdapters = {
  getPage: () => Promise<BillingCreditsPageData>
  updateBillingContacts: (
    payload: UpdateBillingContactsPayload
  ) => Promise<BillingContactsSnapshot>
}

export type BillingCreditsSurface = "tabs" | "manage-plan"

type BillingContactsDraft = {
  billingContactUserId: number
  billingEmail: string
  lowCreditAlerts: BillingAlertRoleFlags
  paymentFailureAlerts: BillingPaymentFailureAlertFlags
}

type PendingLeave =
  | { kind: "tab"; tabId: BillingCreditsTabId }
  | { kind: "href"; href: string }

export type BillingCreditsSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error" | "forbidden"
  surface: BillingCreditsSurface
  activeTabId: BillingCreditsTabId
  tabs: Array<{ id: BillingCreditsTabId; label: string }>
  accessLevel: BillingCreditsAccessLevel
  actorPermissionRole: string
  actorCanPersistBillingContacts: boolean
  showManagePlan: boolean
  showBuyCredits: boolean
  showChangePlan: boolean
  managePlanSection: ManagePlanSection
  planSubscription: PlanSubscriptionSnapshot | null
  billingContacts: BillingContactsSnapshot
  isDirty: boolean
  isSaving: boolean
  saveEnabled: boolean
  leaveDirtyOpen: boolean
  pendingNavigationHref: string | null
  managePlanHref: string | null
  buyCreditsHref: string | null
  breadcrumbHref: string | null
  toast: { kind: "success" | "error"; message: string } | null
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
  setBillingContactUserId: (userId: number) => void
  setBillingEmail: (value: string) => void
  setLowCreditAlertOwner: (checked: boolean) => void
  setLowCreditAlertAdmin: (checked: boolean) => void
  setLowCreditAlertBillingContact: (checked: boolean) => void
  setPaymentFailureAlertOwner: (checked: boolean) => void
  setPaymentFailureAlertBillingContact: (checked: boolean) => void
  persistBillingContacts: () => Promise<boolean>
  requestNavigateAway: (href: string) => boolean
  confirmLeaveDirtySave: () => Promise<void>
  confirmLeaveDirtyCancel: () => void
  closeLeaveDirty: () => void
  clearToast: () => void
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

function emptyBillingContacts(): BillingContactsSnapshot {
  return {
    billingContactUserId: 0,
    billingEmail: "",
    eligibleMembers: [],
    lowCreditAlerts: {
      owner: true,
      admin: false,
      billingContact: true,
    },
    paymentFailureAlerts: {
      owner: true,
      billingContact: true,
    },
  }
}

function normalizeBillingContacts(
  details: BillingContactsSnapshot | null | undefined
): BillingContactsSnapshot {
  const base = details ?? emptyBillingContacts()
  return {
    billingContactUserId: base.billingContactUserId,
    billingEmail: base.billingEmail ?? "",
    eligibleMembers: (base.eligibleMembers ?? []).map((member) => ({
      userId: member.userId,
      fullName: member.fullName,
      email: member.email,
    })),
    lowCreditAlerts: {
      owner: base.lowCreditAlerts.owner,
      admin: base.lowCreditAlerts.admin,
      billingContact: base.lowCreditAlerts.billingContact,
    },
    paymentFailureAlerts: {
      owner: base.paymentFailureAlerts.owner,
      billingContact: base.paymentFailureAlerts.billingContact,
    },
  }
}

function draftFromContacts(
  contacts: BillingContactsSnapshot
): BillingContactsDraft {
  return {
    billingContactUserId: contacts.billingContactUserId,
    billingEmail: contacts.billingEmail,
    lowCreditAlerts: { ...contacts.lowCreditAlerts },
    paymentFailureAlerts: { ...contacts.paymentFailureAlerts },
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
  let persistedContacts: BillingContactsSnapshot | null = null
  let billingContactsDraft: BillingContactsDraft = draftFromContacts(
    emptyBillingContacts()
  )
  let loadStatus: BillingCreditsSnapshot["loadStatus"] = "idle"
  let activeTabId = resolveBillingCreditsTabId(options.initialTabId)
  let surface: BillingCreditsSurface = options.initialSurface ?? "tabs"
  let managePlanSection = resolveManagePlanSection(
    options.initialManagePlanSection
  )
  let navMode: "single" | "multi" = "single"
  let locationId = 0
  let pendingNavigationHref: string | null = null
  let pendingLeave: PendingLeave | null = null
  let leaveDirtyOpen = false
  let isSaving = false
  let toast: BillingCreditsSnapshot["toast"] = null
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

  function isBillingContactsDirty(): boolean {
    if (persistedContacts == null) {
      return false
    }
    const saved = normalizeBillingContacts(persistedContacts)
    return (
      billingContactsDraft.billingContactUserId !== saved.billingContactUserId
      || billingContactsDraft.billingEmail !== saved.billingEmail
      || billingContactsDraft.lowCreditAlerts.owner !== saved.lowCreditAlerts.owner
      || billingContactsDraft.lowCreditAlerts.admin !== saved.lowCreditAlerts.admin
      || billingContactsDraft.lowCreditAlerts.billingContact
        !== saved.lowCreditAlerts.billingContact
      || billingContactsDraft.paymentFailureAlerts.owner
        !== saved.paymentFailureAlerts.owner
      || billingContactsDraft.paymentFailureAlerts.billingContact
        !== saved.paymentFailureAlerts.billingContact
    )
  }

  function activeTabDirty(): boolean {
    return activeTabId === "billing-contacts" && isBillingContactsDirty()
  }

  function applyPersistedContacts(contacts: BillingContactsSnapshot): void {
    persistedContacts = normalizeBillingContacts(contacts)
    billingContactsDraft = draftFromContacts(persistedContacts)
    if (data != null) {
      data = {
        ...data,
        billingContacts: persistedContacts,
      }
    }
  }

  const projectSnapshot = (): BillingCreditsSnapshot => {
    const actions = headerActions()
    const dirty = activeTabDirty()
    const eligibleMembers =
      persistedContacts?.eligibleMembers
      ?? data?.billingContacts.eligibleMembers
      ?? []
    return {
      loadStatus,
      surface,
      activeTabId: resolveBillingCreditsTabId(activeTabId),
      tabs: billingCreditsTabLabels(),
      accessLevel: accessLevel(),
      actorPermissionRole: data?.actorPermissionRole ?? "",
      actorCanPersistBillingContacts:
        data?.actorCanPersistBillingContacts ?? false,
      showManagePlan: actions.showManagePlan,
      showBuyCredits: actions.showBuyCredits,
      showChangePlan: actions.showChangePlan,
      managePlanSection,
      planSubscription: data?.planSubscription ?? null,
      billingContacts: {
        ...billingContactsDraft,
        eligibleMembers,
      },
      isDirty: dirty,
      isSaving,
      saveEnabled:
        dirty
        && !isSaving
        && (data?.actorCanPersistBillingContacts ?? false),
      leaveDirtyOpen,
      pendingNavigationHref,
      managePlanHref: buildManagePlanHref(null),
      buyCreditsHref: buildManagePlanHref("credit-top-ups"),
      breadcrumbHref: buildBillingCreditsHref("plan-subscription"),
      toast,
    }
  }

  let snapshot = projectSnapshot()

  const refreshSnapshot = () => {
    snapshot = projectSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  function continuePendingLeave(): void {
    if (pendingLeave == null) {
      return
    }
    const next = pendingLeave
    pendingLeave = null
    if (next.kind === "tab") {
      activeTabId = next.tabId
      return
    }
    pendingNavigationHref = next.href
  }

  async function persistBillingContacts(): Promise<boolean> {
    if (persistedContacts == null || data == null) {
      return false
    }

    const payload: UpdateBillingContactsPayload = {
      billingContactUserId: billingContactsDraft.billingContactUserId,
      billingEmail: billingContactsDraft.billingEmail,
      lowCreditAlerts: { ...billingContactsDraft.lowCreditAlerts },
      paymentFailureAlerts: { ...billingContactsDraft.paymentFailureAlerts },
    }

    isSaving = true
    toast = null
    refreshSnapshot()

    try {
      const result = await adapters.updateBillingContacts(payload)
      applyPersistedContacts(result)
      toast = {
        kind: "success",
        message: BILLING_CREDITS_PAGE_COPY.billingContactsSaveSuccess,
      }
      isSaving = false
      refreshSnapshot()
      return true
    } catch {
      toast = {
        kind: "error",
        message: BILLING_CREDITS_PAGE_COPY.billingContactsSaveError,
      }
      isSaving = false
      refreshSnapshot()
      return false
    }
  }

  function resetDraftFromPersisted(): void {
    if (persistedContacts == null) {
      return
    }
    billingContactsDraft = draftFromContacts(persistedContacts)
  }

  function requestLeave(next: PendingLeave): void {
    if (!activeTabDirty()) {
      if (next.kind === "tab") {
        activeTabId = next.tabId
      } else {
        pendingNavigationHref = next.href
      }
      refreshSnapshot()
      return
    }
    pendingLeave = next
    leaveDirtyOpen = true
    refreshSnapshot()
  }

  const reload = async () => {
    loadStatus = "loading"
    refreshSnapshot()
    try {
      data = await adapters.getPage()
      applyPersistedContacts(data.billingContacts)
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
      const next = resolveBillingCreditsTabId(tabId)
      if (next === activeTabId) {
        return
      }
      requestLeave({ kind: "tab", tabId: next })
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
      requestLeave({ kind: "href", href })
    },
    openBuyCredits: () => {
      const href = buildManagePlanHref("credit-top-ups")
      if (href == null) {
        return
      }
      requestLeave({ kind: "href", href })
    },
    openChangePlan: () => {
      const href = buildManagePlanHref(null)
      if (href == null) {
        return
      }
      requestLeave({ kind: "href", href })
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
    setBillingContactUserId: (userId) => {
      billingContactsDraft = {
        ...billingContactsDraft,
        billingContactUserId: userId,
      }
      refreshSnapshot()
    },
    setBillingEmail: (value) => {
      billingContactsDraft = {
        ...billingContactsDraft,
        billingEmail: value,
      }
      refreshSnapshot()
    },
    setLowCreditAlertOwner: (checked) => {
      billingContactsDraft = {
        ...billingContactsDraft,
        lowCreditAlerts: {
          ...billingContactsDraft.lowCreditAlerts,
          owner: checked,
        },
      }
      refreshSnapshot()
    },
    setLowCreditAlertAdmin: (checked) => {
      billingContactsDraft = {
        ...billingContactsDraft,
        lowCreditAlerts: {
          ...billingContactsDraft.lowCreditAlerts,
          admin: checked,
        },
      }
      refreshSnapshot()
    },
    setLowCreditAlertBillingContact: (checked) => {
      billingContactsDraft = {
        ...billingContactsDraft,
        lowCreditAlerts: {
          ...billingContactsDraft.lowCreditAlerts,
          billingContact: checked,
        },
      }
      refreshSnapshot()
    },
    setPaymentFailureAlertOwner: (checked) => {
      billingContactsDraft = {
        ...billingContactsDraft,
        paymentFailureAlerts: {
          ...billingContactsDraft.paymentFailureAlerts,
          owner: checked,
        },
      }
      refreshSnapshot()
    },
    setPaymentFailureAlertBillingContact: (checked) => {
      billingContactsDraft = {
        ...billingContactsDraft,
        paymentFailureAlerts: {
          ...billingContactsDraft.paymentFailureAlerts,
          billingContact: checked,
        },
      }
      refreshSnapshot()
    },
    persistBillingContacts,
    requestNavigateAway: (href) => {
      if (!activeTabDirty()) {
        return true
      }
      pendingLeave = { kind: "href", href }
      leaveDirtyOpen = true
      refreshSnapshot()
      return false
    },
    confirmLeaveDirtySave: async () => {
      if (!leaveDirtyOpen) {
        return
      }
      leaveDirtyOpen = false
      refreshSnapshot()

      const ok = await persistBillingContacts()
      if (ok) {
        continuePendingLeave()
      } else {
        pendingLeave = null
      }
      refreshSnapshot()
    },
    confirmLeaveDirtyCancel: () => {
      if (!leaveDirtyOpen) {
        return
      }
      leaveDirtyOpen = false
      resetDraftFromPersisted()
      continuePendingLeave()
      refreshSnapshot()
    },
    closeLeaveDirty: () => {
      leaveDirtyOpen = false
      pendingLeave = null
      refreshSnapshot()
    },
    clearToast: () => {
      toast = null
      refreshSnapshot()
    },
  }
}

export {
  BILLING_CREDITS_TAB_IDS,
  resolveBillingCreditsTabId,
  resolveManagePlanSection,
}

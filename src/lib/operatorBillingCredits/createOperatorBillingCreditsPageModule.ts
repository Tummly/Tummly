import {
  formatBillingActivityCopy,
  formatBillingActivityOccurredAt,
  type BillingActivitySnapshot,
} from "@/lib/operatorBillingCredits/billingActivityPresentation"
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
  buildAdditionalGroupLocationViewModel,
  buildCancelPlanConfirmCopy,
  buildExtraLocationRemoveConfirmCopy,
  defaultPreviewCadence,
  formatCurrentPlanSummary,
  isCancelScheduled,
  liveCadenceFromSnapshot,
  resolvePlanChangeKind,
  ADDITIONAL_GROUP_LOCATION_COPY,
  type AdditionalGroupLocationViewModel,
  type BillingCadence,
  type ManagePlanActionConfirmDialog,
  type ManagePlanCardViewModel,
  type ManagePlanId,
  type PlanChangeKind,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  buildCreditTopUpCards,
  buildCreditTopUpConfirmCopy,
  type CreditTopUpCardViewModel,
  type CreditTopUpConfirmViewModel,
} from "@/lib/operatorBillingCredits/creditTopUpPresentation"
import {
  isAccountLockedBillingStatus,
  resolveLockRestorationCause,
} from "@/lib/operatorHome/lockAlertPresentation"

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
  allowSms5000TopUp: boolean
}

export type BillingActivityItem = {
  id: number
  kind: string
  occurredAt: string
  actorDisplayName?: string | null
  channel?: string | null
  qty?: number | null
  campaignName?: string | null
  invoiceNo?: string | null
  creditNoteNo?: string | null
  plan?: string | null
  cadence?: string | null
  scheduledDateLabel?: string | null
  locationName?: string | null
  manualAdjustDirection?: string | null
  consumeSource?: string | null
}

export type BillingActivityList = {
  items: BillingActivityItem[]
  totalCount: number
  page: number
  pageSize: number
}

export type BillingActivityViewRow = {
  id: number
  occurredAtLabel: string
  sentence: string
}

export type BillingCreditsPageData = {
  actorPermissionRole: string
  actorCanManage: boolean
  actorCanPersistBillingContacts: boolean
  planSubscription: PlanSubscriptionSnapshot
  paymentMethod: PaymentMethodSnapshot | null
  invoices: InvoiceRowSnapshot[]
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

export type CreditTopUpRequest = {
  channel: CreditChannelId
  quantity: number
}

export type CreditTopUpConfirmResult = {
  channel: CreditChannelId
  quantity: number
  channelLabel: string
  netLabel: string
  grossLabel: string
  vatLabel: string
}

export type CreditTopUpPayResult = {
  redirectUrl: string
}

export type ExtraLocationChangeResult = PlanChangeResult

export type CancelPlanResult = {
  scheduledChangeLine: string
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

export type UpdateBillingContactsPayload = {
  billingContactUserId: number
  billingEmail: string
  lowCreditAlerts: BillingAlertRoleFlags
  paymentFailureAlerts: BillingPaymentFailureAlertFlags
}

export type BillingCreditsPageAdapters = {
  getPage: () => Promise<BillingCreditsPageData>
  getUsage: () => Promise<CreditsUsageSnapshot>
  getBillingActivity: (params: {
    page: number
    pageSize: number
  }) => Promise<BillingActivityList>
  submitPlanChange: (request: PlanChangeRequest) => Promise<PlanChangeResult>
  addExtraGroupLocation?: () => Promise<ExtraLocationChangeResult>
  removeExtraGroupLocation?: () => Promise<ExtraLocationChangeResult>
  cancelPlan?: () => Promise<CancelPlanResult>
  createPaymentMethodUpdateSession?: () => Promise<{ redirectUrl: string }>
  fetchInvoicePdf?: (invoiceNo: string) => Promise<Blob>
  openInvoicePdf?: (blob: Blob) => void
  downloadInvoicePdf?: (blob: Blob, invoiceNo: string) => void
  updateBillingContacts: (
    payload: UpdateBillingContactsPayload
  ) => Promise<BillingContactsSnapshot>
  confirmCreditTopUp?: (
    request: CreditTopUpRequest
  ) => Promise<CreditTopUpConfirmResult>
  payCreditTopUp?: (request: CreditTopUpRequest) => Promise<CreditTopUpPayResult>
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
  /** Soft lock / Dormant: Buy credits stays visible and disabled. */
  buyCreditsDisabled: boolean
  showChangePlan: boolean
  managePlanSection: ManagePlanSection
  planSubscription: PlanSubscriptionSnapshot | null
  /** Soft lock / Dormant paid-write lock mode for Manage plan CTAs. */
  managePlanLockMode: "none" | "pilot-restore" | "dunning"
  creditsUsage: CreditsUsageSnapshot | null
  channelCards: CreditChannelCardViewModel[]
  usageTableRows: CreditsUsageTableRowViewModel[]
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
  }
  pendingPaymentMethodRedirectUrl: string | null
  billingContacts: BillingContactsSnapshot
  isDirty: boolean
  isSaving: boolean
  saveEnabled: boolean
  leaveDirtyOpen: boolean
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
  topUpCards: CreditTopUpCardViewModel[]
  topUpConfirm: CreditTopUpConfirmViewModel | null
  focusedTopUpChannel: CreditChannelId | null
  showOwnerManagePlanWrites: boolean
  /** Soft lock / Dormant: Owner Manage plan writes stay visible but disabled. */
  ownerManagePlanWritesEnabled: boolean
  additionalGroupLocation: AdditionalGroupLocationViewModel | null
  showCancelPlan: boolean
  extraLocationConfirm: ManagePlanActionConfirmDialog | null
  cancelPlanConfirm: ManagePlanActionConfirmDialog | null
  toast: { kind: "success" | "error"; message: string } | null
  billingActivityPreview: BillingActivityViewRow[]
  billingActivityEmpty: boolean
  billingActivityHistoryOpen: boolean
  billingActivityHistoryRows: BillingActivityViewRow[]
  billingActivityHistoryPage: number
  billingActivityHistoryPageSize: number
  billingActivityHistoryTotalCount: number
  billingActivityHistoryHasNext: boolean
  billingActivityHistoryHasPrevious: boolean
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
  setFocusedTopUpChannelFromUrl: (raw: string | null) => void
  selectTopUpPack: (channel: CreditChannelId, quantity: number) => void
  requestTopUpBuy: (channel: CreditChannelId) => Promise<void>
  cancelTopUpBuy: () => void
  confirmTopUpBuy: () => Promise<void>
  handleTopUpPayReturn: (outcome: "success" | "cancel" | "fail") => void
  shouldAutoOpenCreditTopUps: () => boolean
  requestAddExtraLocation: () => void
  requestRemoveExtraLocation: () => void
  cancelExtraLocationChange: () => void
  confirmExtraLocationChange: () => Promise<void>
  requestCancelPlan: () => void
  cancelCancelPlan: () => void
  confirmCancelPlan: () => Promise<void>
  openUpdatePaymentMethodConfirm: () => void
  dismissUpdatePaymentMethodConfirm: () => void
  confirmUpdatePaymentMethod: () => Promise<void>
  consumePendingPaymentMethodRedirect: () => string | null
  viewInvoicePdf: (invoiceNo: string) => Promise<void>
  downloadInvoicePdf: (invoiceNo: string) => Promise<void>
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
  openBillingActivityHistory: () => Promise<void>
  closeBillingActivityHistory: () => void
  goToNextBillingActivityHistoryPage: () => Promise<void>
  goToPreviousBillingActivityHistoryPage: () => Promise<void>
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
    allowSms5000TopUp: false,
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
    getNow?: () => Date
  } = {}
): OperatorBillingCreditsPageModule {
  const getNow = options.getNow ?? (() => new Date())
  let data: BillingCreditsPageData | null = null
  let creditsUsage: CreditsUsageSnapshot | null = null
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
  let updatePaymentMethodConfirmOpen = false
  let pendingPaymentMethodRedirectUrl: string | null = null
  let navMode: "single" | "multi" = "single"
  let locationId = 0
  let pendingNavigationHref: string | null = null
  let previewCadence: BillingCadence = "monthly"
  let planChangeConfirm: PlanChangeConfirmDialog | null = null
  let pendingPayRedirectUrl: string | null = null
  let focusedTopUpChannel: CreditChannelId | null = null
  let selectedPackByChannel: Partial<Record<CreditChannelId, number>> = {}
  let topUpConfirm: CreditTopUpConfirmViewModel | null = null
  let extraLocationConfirm: ManagePlanActionConfirmDialog | null = null
  let extraLocationConfirmKind: "add" | "remove" | null = null
  let cancelPlanConfirm: ManagePlanActionConfirmDialog | null = null
  let pendingLeave: PendingLeave | null = null
  let leaveDirtyOpen = false
  let isSaving = false
  let toast: BillingCreditsSnapshot["toast"] = null
  let billingActivityPreview: BillingActivityViewRow[] = []
  let billingActivityEmpty = true
  let billingActivityHistoryOpen = false
  let billingActivityHistoryRows: BillingActivityViewRow[] = []
  let billingActivityHistoryPage = 1
  let billingActivityHistoryPageSize = 20
  let billingActivityHistoryTotalCount = 0
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

  const canBuyCreditTopUp = (): boolean => {
    if (accessLevel() !== "manage") {
      return false
    }
    const role = data?.actorPermissionRole ?? ""
    return (
      role === "Owner" || role === "Billing Admin" || role === "Admin"
    )
  }

  const projectTopUpCards = (): CreditTopUpCardViewModel[] => {
    if (creditsUsage == null || data?.planSubscription == null) {
      return []
    }

    const locked = isAccountLockedBillingStatus(
      data.planSubscription.billingStatus
    )

    return buildCreditTopUpCards({
      channels: creditsUsage.channels,
      subscriptionPlan: data.planSubscription.subscriptionPlan,
      allowSms5000TopUp: data.planSubscription.allowSms5000TopUp,
      isPilot: creditsUsage.isPilot,
      canBuy: canBuyCreditTopUp() && !locked,
      selectedPackByChannel,
      focusedChannel: focusedTopUpChannel,
    })
  }

  const resolveManagePlanLockMode = ():
    | "none"
    | "pilot-restore"
    | "dunning" => {
    const plan = data?.planSubscription
    if (plan == null || !isAccountLockedBillingStatus(plan.billingStatus)) {
      return "none"
    }
    const cause = resolveLockRestorationCause({
      billingStatus: plan.billingStatus,
      subscriptionPlan: plan.subscriptionPlan,
      isPilot: plan.isPilot,
    })
    return cause === "pilot" ? "pilot-restore" : "dunning"
  }

  const showOwnerManagePlanWrites = (): boolean =>
    accessLevel() === "manage" && data?.actorPermissionRole === "Owner"

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
    const plan = data?.planSubscription ?? null
    const managePlanLockMode = resolveManagePlanLockMode()
    const buyCreditsDisabled =
      plan != null && isAccountLockedBillingStatus(plan.billingStatus)
    const showPlanCards =
      actions.showManagePlan && managePlanSection == null
    const managePlanCards =
      plan != null && showPlanCards
        ? buildManagePlanCardViewModels({
            plan,
            previewCadence,
            lockMode: managePlanLockMode,
          })
        : []
    const additionalGroupLocation =
      plan != null && showOwnerManagePlanWrites()
        ? buildAdditionalGroupLocationViewModel(plan)
        : null
    const showCancelPlan =
      showOwnerManagePlanWrites()
      && plan != null
      && !plan.isPilot
      && !isCancelScheduled(plan)

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
      buyCreditsDisabled,
      showChangePlan: actions.showChangePlan,
      managePlanSection,
      planSubscription: plan,
      managePlanLockMode,
      creditsUsage,
      channelCards: projectChannelCards(),
      usageTableRows: projectUsageTableRows(),
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
      },
      pendingPaymentMethodRedirectUrl,
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
      showPlanCards,
      previewCadence,
      managePlanCards,
      currentPlanSummary:
        plan != null ? formatCurrentPlanSummary(plan) : null,
      planChangeConfirm,
      pendingPayRedirectUrl,
      topUpCards: projectTopUpCards(),
      topUpConfirm,
      focusedTopUpChannel,
      showOwnerManagePlanWrites: showOwnerManagePlanWrites(),
      ownerManagePlanWritesEnabled: managePlanLockMode === "none",
      additionalGroupLocation,
      showCancelPlan,
      extraLocationConfirm,
      cancelPlanConfirm,
      toast,
      billingActivityPreview,
      billingActivityEmpty,
      billingActivityHistoryOpen,
      billingActivityHistoryRows,
      billingActivityHistoryPage,
      billingActivityHistoryPageSize,
      billingActivityHistoryTotalCount,
      billingActivityHistoryHasNext:
        billingActivityHistoryPage * billingActivityHistoryPageSize
        < billingActivityHistoryTotalCount,
      billingActivityHistoryHasPrevious: billingActivityHistoryPage > 1,
    }
  }

  let snapshot = projectSnapshot()

  const mapActivityRows = (
    items: BillingActivityItem[]
  ): BillingActivityViewRow[] => {
    const now = getNow()
    return items
      .map((item) => {
        const snapshotFields: BillingActivitySnapshot = {
          kind: item.kind,
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
        }
        const sentence = formatBillingActivityCopy(snapshotFields)
        if (sentence === "") {
          return null
        }
        return {
          id: item.id,
          occurredAtLabel: formatBillingActivityOccurredAt(item.occurredAt, now),
          sentence,
        }
      })
      .filter((row): row is BillingActivityViewRow => row != null)
  }

  const loadActivityPreview = async () => {
    const list = await adapters.getBillingActivity({ page: 1, pageSize: 10 })
    billingActivityPreview = mapActivityRows(list.items)
    billingActivityEmpty = list.totalCount === 0
  }

  const loadActivityHistoryPage = async (page: number) => {
    const list = await adapters.getBillingActivity({ page, pageSize: 20 })
    billingActivityHistoryRows = mapActivityRows(list.items)
    billingActivityHistoryPage = list.page
    billingActivityHistoryPageSize = list.pageSize
    billingActivityHistoryTotalCount = list.totalCount
  }

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
      const [page, usage] = await Promise.all([
        adapters.getPage(),
        adapters.getUsage(),
      ])
      data = page
      creditsUsage = usage
      previewCadence = defaultPreviewCadence(data.planSubscription)
      applyPersistedContacts(data.billingContacts)
      await loadActivityPreview()
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
    setFocusedTopUpChannelFromUrl: (raw) => {
      if (raw === "sms" || raw === "email" || raw === "ai") {
        focusedTopUpChannel = raw
      } else {
        focusedTopUpChannel = null
      }
      refreshSnapshot()
    },
    selectTopUpPack: (channel, quantity) => {
      selectedPackByChannel = {
        ...selectedPackByChannel,
        [channel]: quantity,
      }
      focusedTopUpChannel = channel
      refreshSnapshot()
    },
    requestTopUpBuy: async (channel) => {
      const quantity = selectedPackByChannel[channel]
      if (quantity == null || adapters.confirmCreditTopUp == null) {
        return
      }

      topUpConfirm = {
        open: true,
        title: "Confirm credit top-up",
        body: "",
        primaryLabel: "Continue to payment",
        busy: true,
        channel,
        quantity,
      }
      refreshSnapshot()

      try {
        const result = await adapters.confirmCreditTopUp({
          channel,
          quantity,
        })
        const copy = buildCreditTopUpConfirmCopy({
          channelLabel: result.channelLabel,
          quantity: result.quantity,
          netLabel: result.netLabel,
          grossLabel: result.grossLabel,
        })
        topUpConfirm = {
          open: true,
          title: copy.title,
          body: copy.body,
          primaryLabel: copy.primaryLabel,
          busy: false,
          channel,
          quantity,
        }
      } catch {
        topUpConfirm = null
        toast = {
          kind: "error",
          message: "Could not prepare this credit top-up. Please try again.",
        }
      }
      refreshSnapshot()
    },
    cancelTopUpBuy: () => {
      topUpConfirm = null
      refreshSnapshot()
    },
    confirmTopUpBuy: async () => {
      if (topUpConfirm == null || adapters.payCreditTopUp == null) {
        return
      }

      const { channel, quantity } = topUpConfirm
      topUpConfirm = {
        ...topUpConfirm,
        busy: true,
      }
      refreshSnapshot()

      try {
        const result = await adapters.payCreditTopUp({ channel, quantity })
        topUpConfirm = null
        pendingPayRedirectUrl = result.redirectUrl
      } catch {
        topUpConfirm = {
          ...topUpConfirm,
          busy: false,
        }
        toast = {
          kind: "error",
          message: "Could not start payment. Please try again.",
        }
      }
      refreshSnapshot()
    },
    handleTopUpPayReturn: (outcome) => {
      if (outcome === "success") {
        const href = buildBillingCreditsHref("credits-usage")
        if (href != null) {
          pendingNavigationHref = href
        }
        selectedPackByChannel = {}
        topUpConfirm = null
        refreshSnapshot()
        return
      }

      managePlanSection = "credit-top-ups"
      refreshSnapshot()
    },
    shouldAutoOpenCreditTopUps: () =>
      accessLevel() === "manage"
      && managePlanSection == null
      && data?.actorPermissionRole === "Billing Admin",
    requestAddExtraLocation: () => {
      const plan = data?.planSubscription
      if (plan == null || !showOwnerManagePlanWrites()) {
        return
      }

      extraLocationConfirmKind = "add"
      extraLocationConfirm = {
        open: true,
        title: ADDITIONAL_GROUP_LOCATION_COPY.confirmAddTitle,
        body: ADDITIONAL_GROUP_LOCATION_COPY.confirmAddBody,
        primaryLabel: ADDITIONAL_GROUP_LOCATION_COPY.confirmPrimaryPay,
        busy: false,
      }
      refreshSnapshot()
    },
    requestRemoveExtraLocation: () => {
      const plan = data?.planSubscription
      if (plan == null || !showOwnerManagePlanWrites()) {
        return
      }

      const copy = buildExtraLocationRemoveConfirmCopy(plan.renewalDateLabel)
      extraLocationConfirmKind = "remove"
      extraLocationConfirm = {
        open: true,
        title: copy.title,
        body: copy.body,
        primaryLabel: copy.primaryLabel,
        busy: false,
      }
      refreshSnapshot()
    },
    cancelExtraLocationChange: () => {
      extraLocationConfirm = null
      extraLocationConfirmKind = null
      refreshSnapshot()
    },
    confirmExtraLocationChange: async () => {
      if (extraLocationConfirm == null || extraLocationConfirmKind == null) {
        return
      }

      extraLocationConfirm = {
        ...extraLocationConfirm,
        busy: true,
      }
      refreshSnapshot()

      try {
        const result =
          extraLocationConfirmKind === "add"
            ? await adapters.addExtraGroupLocation?.()
            : await adapters.removeExtraGroupLocation?.()

        if (result == null) {
          throw new Error("missing-adapter")
        }

        extraLocationConfirm = null
        extraLocationConfirmKind = null

        if (result.outcome === "pay" && result.redirectUrl != null) {
          pendingPayRedirectUrl = result.redirectUrl
          refreshSnapshot()
          return
        }

        if (data != null && result.scheduledChangeLine != null) {
          data = {
            ...data,
            planSubscription: {
              ...data.planSubscription,
              scheduledChangeLine: result.scheduledChangeLine,
            },
          }
        }

        const href = buildBillingCreditsHref("plan-subscription")
        if (href != null) {
          pendingNavigationHref = href
        }
        refreshSnapshot()
      } catch {
        extraLocationConfirm = {
          ...extraLocationConfirm,
          busy: false,
        }
        refreshSnapshot()
      }
    },
    requestCancelPlan: () => {
      const plan = data?.planSubscription
      if (plan == null || !showOwnerManagePlanWrites()) {
        return
      }

      const copy = buildCancelPlanConfirmCopy(plan.renewalDateLabel)
      cancelPlanConfirm = {
        open: true,
        title: copy.title,
        body: copy.body,
        primaryLabel: copy.primaryLabel,
        busy: false,
      }
      refreshSnapshot()
    },
    cancelCancelPlan: () => {
      cancelPlanConfirm = null
      refreshSnapshot()
    },
    confirmCancelPlan: async () => {
      if (cancelPlanConfirm == null) {
        return
      }

      cancelPlanConfirm = {
        ...cancelPlanConfirm,
        busy: true,
      }
      refreshSnapshot()

      try {
        const result = await adapters.cancelPlan?.()
        if (result == null) {
          throw new Error("missing-adapter")
        }

        cancelPlanConfirm = null

        if (data != null) {
          data = {
            ...data,
            planSubscription: {
              ...data.planSubscription,
              scheduledChangeLine: result.scheduledChangeLine,
            },
          }
        }

        const href = buildBillingCreditsHref("plan-subscription")
        if (href != null) {
          pendingNavigationHref = href
        }
        refreshSnapshot()
      } catch {
        cancelPlanConfirm = {
          ...cancelPlanConfirm,
          busy: false,
        }
        refreshSnapshot()
      }
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
    openBillingActivityHistory: async () => {
      if (billingActivityEmpty) {
        return
      }
      await loadActivityHistoryPage(1)
      billingActivityHistoryOpen = true
      refreshSnapshot()
    },
    closeBillingActivityHistory: () => {
      billingActivityHistoryOpen = false
      refreshSnapshot()
    },
    goToNextBillingActivityHistoryPage: async () => {
      if (
        billingActivityHistoryPage * billingActivityHistoryPageSize
        >= billingActivityHistoryTotalCount
      ) {
        return
      }
      await loadActivityHistoryPage(billingActivityHistoryPage + 1)
      refreshSnapshot()
    },
    goToPreviousBillingActivityHistoryPage: async () => {
      if (billingActivityHistoryPage <= 1) {
        return
      }
      await loadActivityHistoryPage(billingActivityHistoryPage - 1)
      refreshSnapshot()
    },
  }
}

export {
  BILLING_CREDITS_TAB_IDS,
  resolveBillingCreditsTabId,
  resolveManagePlanSection,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
export {
  resolveBillingAlertNotificationCta,
  billingAlertEventKindForCreditThreshold,
} from "@/lib/operatorBillingCredits/billingAlertNotificationPresentation"
export type {
  BillingAlertEventKind,
  BillingAlertNotificationCta,
} from "@/lib/operatorBillingCredits/billingAlertNotificationPresentation"

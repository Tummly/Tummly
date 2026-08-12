import {
  buildOfferDetailsClaimsRowActions,
  buildOfferDetailsDefinitionFields,
  buildOfferDetailsHeaderMenuItems,
  buildOfferDetailsLifecycleEmptyState,
  buildOfferDetailsMetaRows,
  buildOfferDetailsOverviewKpis,
  buildOfferDetailsRedemptionsRowActions,
  buildOfferDetailsVoidRequestsRowActions,
  DEFAULT_OFFER_DETAILS_DATE_RANGE,
  formatOfferDetailsSourceLabel,
  isVisibleOfferDetailsRowAction,
  labelForOfferDetailsDateRange,
  OFFER_DETAILS_CAMPAIGNS_LINKED_COLUMN_LABELS,
  OFFER_DETAILS_CAMPAIGNS_SUB_TAB_IDS,
  OFFER_DETAILS_CAMPAIGNS_SUB_TAB_LABELS,
  OFFER_DETAILS_CLAIMS_COLUMN_LABELS,
  OFFER_DETAILS_COPY,
  OFFER_DETAILS_ISSUANCE_SOURCES_COLUMN_LABELS,
  OFFER_DETAILS_REDEMPTIONS_COLUMN_LABELS,
  OFFER_DETAILS_TAB_IDS,
  OFFER_DETAILS_TAB_LABELS,
  OFFER_DETAILS_VOID_REQUESTS_COLUMN_LABELS,
  offerDetailsClaimsRowActionConfirmCopy,
  offerDetailsHeaderActionConfirmCopy,
  offerDetailsStatusLabel,
  type OfferDetailsCampaignsSubTabId,
  type OfferDetailsClaimsRowActionId,
  type OfferDetailsDateRange,
  type OfferDetailsHeaderActionId,
  type OfferDetailsHeaderMenuItem,
  type OfferDetailsKpi,
  type OfferDetailsLabeledValue,
  type OfferDetailsLifecycleEmptyState,
  type OfferDetailsLifecycleRowAction,
  type OfferDetailsOverviewMetrics,
  type OfferDetailsRedemptionsRowActionId,
  type OfferDetailsTabId,
  type OfferDetailsVoidRequestsRowActionId,
} from "@/lib/operatorOffers/offerDetailsPresentation"
import type {
  VoidRequestCorrectionId,
  VoidRequestReasonId,
} from "@/lib/operatorOffers/voidRequestPresentation"
import type { CatalogOfferDetail } from "@/types/operatorCampaigns"

export const OFFER_DETAILS_LOAD_ERROR_MESSAGE = OFFER_DETAILS_COPY.loadError

export type OfferDetailsWorkspaceLocation = {
  id: number
  locationName: string
}

export type OfferDetailsWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OfferDetailsWorkspaceLocation[]
  offerId: number | null
  /** Optional deep-link tab (e.g. void-requests from Needs attention CTA). */
  initialTabId?: OfferDetailsTabId
}

export type OfferDetailsPendingHeaderAction = {
  actionId: OfferDetailsHeaderActionId
  title: string
  description: string
}

export type OfferDetailsPendingRowAction = {
  tabId: "claims"
  actionId: "resend-offer" | "cancel-claim"
  rowId: string
  title: string
  description: string
}

export type OfferDetailsTabChrome = {
  id: OfferDetailsTabId
  label: string
}

export type OfferDetailsOverviewViewModel = {
  dateRange: OfferDetailsDateRange
  dateRangeLabel: string
  kpis: readonly OfferDetailsKpi[]
  definitionTitle: string
  definitionFields: readonly OfferDetailsLabeledValue[]
  recommendation: {
    title: string
    subtitle: string
    emptyCopy: string
  }
}

export type OfferDetailsClaimRow = {
  id: string
  guestName: string
  guestId: string | null
  claimCode: string
  claimedText: string
  sourceText: string
  locationName: string
  expiryText: string
  statusText: string
  actions: readonly OfferDetailsLifecycleRowAction<OfferDetailsClaimsRowActionId>[]
}

export type OfferDetailsRedemptionRow = {
  id: string
  dateTimeText: string
  guestName: string
  guestId: string | null
  passReferenceText: string
  locationName: string
  staffMemberText: string
  outcomeText: string
  reasonText: string
  offerVersionText: string
  /** Pass id for void create (optional until list API ships). */
  passId?: string
  passCodeMasked?: string
  expiresText?: string
  linkedCampaignText?: string
  offerTitle?: string
  actions: readonly OfferDetailsLifecycleRowAction<OfferDetailsRedemptionsRowActionId>[]
}

export type OfferDetailsLinkedCampaignRow = {
  id: string
  campaignName: string
  statusText: string
  locationName: string
  channelText: string
  audienceText: string
  offerVersionText: string
  passesIssuedText: string
  claimsText: string
  redemptionsText: string
  sendDateText: string
}

export type OfferDetailsIssuanceSourceRow = {
  id: string
  sourceText: string
  pathText: string
  passesIssuedText: string
  lastIssuedText: string
}

export type OfferDetailsVoidRequestRow = {
  id: string
  dateTimeText: string
  requestedByText: string
  guestName: string
  offerPassText: string
  reasonText: string
  locationName: string
  currentStateText: string
  requestedCorrectionText: string
  statusText: string
  /** Optional fields for Review dialogue until list API ships full detail. */
  passId?: string
  passCodeMasked?: string
  expiresText?: string
  linkedCampaignText?: string
  offerTitle?: string
  explanation?: string | null
  reasonId?: VoidRequestReasonId
  correctionId?: VoidRequestCorrectionId
  actions: readonly OfferDetailsLifecycleRowAction<OfferDetailsVoidRequestsRowActionId>[]
}

export type OfferDetailsClaimsTabViewModel = {
  columns: typeof OFFER_DETAILS_CLAIMS_COLUMN_LABELS
  rows: readonly OfferDetailsClaimRow[]
  empty: OfferDetailsLifecycleEmptyState | null
}

export type OfferDetailsRedemptionsTabViewModel = {
  columns: typeof OFFER_DETAILS_REDEMPTIONS_COLUMN_LABELS
  rows: readonly OfferDetailsRedemptionRow[]
  empty: OfferDetailsLifecycleEmptyState | null
}

export type OfferDetailsCampaignsTabViewModel = {
  subTabs: readonly {
    id: OfferDetailsCampaignsSubTabId
    label: string
  }[]
  activeSubTabId: OfferDetailsCampaignsSubTabId
  linked: {
    columns: typeof OFFER_DETAILS_CAMPAIGNS_LINKED_COLUMN_LABELS
    rows: readonly OfferDetailsLinkedCampaignRow[]
    empty: OfferDetailsLifecycleEmptyState | null
  }
  issuanceSources: {
    columns: typeof OFFER_DETAILS_ISSUANCE_SOURCES_COLUMN_LABELS
    rows: readonly OfferDetailsIssuanceSourceRow[]
    empty: OfferDetailsLifecycleEmptyState | null
  }
}

export type OfferDetailsVoidRequestsTabViewModel = {
  columns: typeof OFFER_DETAILS_VOID_REQUESTS_COLUMN_LABELS
  rows: readonly OfferDetailsVoidRequestRow[]
  empty: OfferDetailsLifecycleEmptyState | null
}

export type OfferDetailsViewModel = {
  offerId: number
  locationId: number
  locationName: string
  title: string
  subtitle: string
  status: CatalogOfferDetail["status"]
  statusLabel: string
  breadcrumbOffersLabel: string
  editOfferLabel: string
  openStaffRedeemLabel: string
  moreActionsAriaLabel: string
  headerMenuItems: readonly OfferDetailsHeaderMenuItem[]
  pendingHeaderAction: OfferDetailsPendingHeaderAction | null
  pendingRowAction: OfferDetailsPendingRowAction | null
  metaRows: readonly OfferDetailsLabeledValue[]
  tabs: readonly OfferDetailsTabChrome[]
  activeTabId: OfferDetailsTabId
  overview: OfferDetailsOverviewViewModel
  claims: OfferDetailsClaimsTabViewModel
  redemptions: OfferDetailsRedemptionsTabViewModel
  campaigns: OfferDetailsCampaignsTabViewModel
  voidRequests: OfferDetailsVoidRequestsTabViewModel
}

export type OfferDetailsSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OfferDetailsViewModel | null
  loadError: string | null
}

export type OfferDetailsAdapters = {
  getOffer: (offerId: number) => Promise<CatalogOfferDetail>
  /** Live Overview KPIs — zeros respond to date range when absent. */
  getOfferMetrics?: (
    offerId: number,
    range: OfferDetailsDateRange
  ) => Promise<OfferDetailsOverviewMetrics>
  pauseOffer?: (offerId: number) => Promise<CatalogOfferDetail>
  resumeOffer?: (offerId: number) => Promise<CatalogOfferDetail>
  archiveOffer?: (offerId: number) => Promise<CatalogOfferDetail>
  duplicateOffer?: (offerId: number) => Promise<CatalogOfferDetail>
  /** After Duplicate success — navigate to the new offer Details when wired. */
  onDuplicated?: (newOfferId: number) => void
  /** Optional until Claims list API ships — defaults to honest empty. */
  getClaims?: (offerId: number) => Promise<readonly OfferDetailsClaimRow[]>
  /** Optional until Redemptions list API ships — defaults to honest empty. */
  getRedemptions?: (
    offerId: number
  ) => Promise<readonly OfferDetailsRedemptionRow[]>
  /** Live linked campaigns for Campaigns tab (ticket 41). */
  getLinkedCampaigns?: (
    offerId: number
  ) => Promise<readonly OfferDetailsLinkedCampaignRow[]>
  /** Live issuance sources for Campaigns tab (ticket 41). */
  getIssuanceSources?: (
    offerId: number
  ) => Promise<readonly OfferDetailsIssuanceSourceRow[]>
  /** Live void requests for Void requests tab (ticket 41). */
  getVoidRequests?: (
    offerId: number
  ) => Promise<readonly OfferDetailsVoidRequestRow[]>
}

export type OfferDetailsPageModule = {
  getSnapshot: () => OfferDetailsSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OfferDetailsWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setActiveTab: (tabId: OfferDetailsTabId) => void
  setCampaignsSubTab: (subTabId: OfferDetailsCampaignsSubTabId) => void
  setOverviewDateRange: (range: OfferDetailsDateRange) => Promise<void>
  requestHeaderAction: (actionId: OfferDetailsHeaderActionId) => void
  /** Runs the pending lifecycle write, clears confirm, and refreshes chrome. */
  confirmPendingHeaderAction: () => Promise<void>
  cancelPendingHeaderAction: () => void
  requestClaimsRowAction: (
    rowId: string,
    actionId: OfferDetailsClaimsRowActionId
  ) => void
  /** Clears pending row confirm — no live Resend / Cancel claim APIs yet. */
  confirmPendingRowAction: () => void
  cancelPendingRowAction: () => void
}

type LifecycleLists = {
  claims: readonly OfferDetailsClaimRow[]
  redemptions: readonly OfferDetailsRedemptionRow[]
  linkedCampaigns: readonly OfferDetailsLinkedCampaignRow[]
  issuanceSources: readonly OfferDetailsIssuanceSourceRow[]
  voidRequests: readonly OfferDetailsVoidRequestRow[]
}

type ModuleState = {
  loadStatus: OfferDetailsSnapshot["loadStatus"]
  workspace: OfferDetailsWorkspaceInput | null
  offer: CatalogOfferDetail | null
  locationName: string | null
  viewModel: OfferDetailsViewModel | null
  loadError: string | null
  activeTabId: OfferDetailsTabId
  campaignsSubTabId: OfferDetailsCampaignsSubTabId
  overviewDateRange: OfferDetailsDateRange
  overviewMetrics: OfferDetailsOverviewMetrics
  pendingHeaderAction: OfferDetailsPendingHeaderAction | null
  pendingRowAction: OfferDetailsPendingRowAction | null
  lifecycleLists: LifecycleLists
  loadGeneration: number
}

function emptyMetrics(): OfferDetailsOverviewMetrics {
  return {
    claims: 0,
    redemptions: 0,
    expiredUnused: 0,
    failedAttempts: 0,
  }
}

function emptyLifecycleLists(): LifecycleLists {
  return {
    claims: [],
    redemptions: [],
    linkedCampaigns: [],
    issuanceSources: [],
    voidRequests: [],
  }
}

function withClaimActions(
  rows: readonly OfferDetailsClaimRow[]
): OfferDetailsClaimRow[] {
  const actions = buildOfferDetailsClaimsRowActions().filter(
    isVisibleOfferDetailsRowAction
  )
  return rows.map((row) => ({ ...row, actions }))
}

function withRedemptionActions(
  rows: readonly OfferDetailsRedemptionRow[]
): OfferDetailsRedemptionRow[] {
  return rows.map((row) => {
    const isRedeemed = row.outcomeText.trim().toLowerCase() === "redeemed"
    const actions = buildOfferDetailsRedemptionsRowActions()
      .filter(isVisibleOfferDetailsRowAction)
      .filter((action) => action.id !== "request-void" || isRedeemed)
    return { ...row, actions }
  })
}

function withVoidActions(
  rows: readonly OfferDetailsVoidRequestRow[]
): OfferDetailsVoidRequestRow[] {
  const actions = buildOfferDetailsVoidRequestsRowActions()
  return rows.map((row) => {
    const isPending = row.statusText.trim().toLowerCase() === "pending"
    return {
      ...row,
      actions: isPending ? actions : [],
    }
  })
}

function assembleLifecycleTabs(
  state: ModuleState
): Pick<
  OfferDetailsViewModel,
  "claims" | "redemptions" | "campaigns" | "voidRequests"
> {
  const claimsRows = withClaimActions(state.lifecycleLists.claims)
  const redemptionRows = withRedemptionActions(state.lifecycleLists.redemptions)
  const linkedRows = state.lifecycleLists.linkedCampaigns
  const issuanceRows = state.lifecycleLists.issuanceSources
  const voidRows = withVoidActions(state.lifecycleLists.voidRequests)

  return {
    claims: {
      columns: OFFER_DETAILS_CLAIMS_COLUMN_LABELS,
      rows: claimsRows,
      empty:
        claimsRows.length === 0
          ? buildOfferDetailsLifecycleEmptyState("claims")
          : null,
    },
    redemptions: {
      columns: OFFER_DETAILS_REDEMPTIONS_COLUMN_LABELS,
      rows: redemptionRows,
      empty:
        redemptionRows.length === 0
          ? buildOfferDetailsLifecycleEmptyState("redemptions")
          : null,
    },
    campaigns: {
      subTabs: OFFER_DETAILS_CAMPAIGNS_SUB_TAB_IDS.map((id) => ({
        id,
        label: OFFER_DETAILS_CAMPAIGNS_SUB_TAB_LABELS[id],
      })),
      activeSubTabId: state.campaignsSubTabId,
      linked: {
        columns: OFFER_DETAILS_CAMPAIGNS_LINKED_COLUMN_LABELS,
        rows: linkedRows,
        empty:
          linkedRows.length === 0
            ? buildOfferDetailsLifecycleEmptyState("campaigns-linked")
            : null,
      },
      issuanceSources: {
        columns: OFFER_DETAILS_ISSUANCE_SOURCES_COLUMN_LABELS,
        rows: issuanceRows,
        empty:
          issuanceRows.length === 0
            ? buildOfferDetailsLifecycleEmptyState("campaigns-issuance")
            : null,
      },
    },
    voidRequests: {
      columns: OFFER_DETAILS_VOID_REQUESTS_COLUMN_LABELS,
      rows: voidRows,
      empty:
        voidRows.length === 0
          ? buildOfferDetailsLifecycleEmptyState("void-requests")
          : null,
    },
  }
}

function assembleViewModel(state: ModuleState): OfferDetailsViewModel | null {
  if (state.offer == null || state.locationName == null) {
    return null
  }

  const offer = state.offer
  const lifecycle = assembleLifecycleTabs(state)
  return {
    offerId: offer.id,
    locationId: offer.locationId,
    locationName: state.locationName,
    title: offer.title,
    subtitle: offer.description.trim(),
    status: offer.status,
    statusLabel: offerDetailsStatusLabel(offer.status),
    breadcrumbOffersLabel: OFFER_DETAILS_COPY.breadcrumbOffers,
    editOfferLabel: OFFER_DETAILS_COPY.editOffer,
    openStaffRedeemLabel: OFFER_DETAILS_COPY.openStaffRedeem,
    moreActionsAriaLabel: OFFER_DETAILS_COPY.moreActionsAriaLabel,
    headerMenuItems: buildOfferDetailsHeaderMenuItems(offer.status),
    pendingHeaderAction: state.pendingHeaderAction,
    pendingRowAction: state.pendingRowAction,
    metaRows: buildOfferDetailsMetaRows({
      locationName: state.locationName,
      createdAt: offer.createdAt,
      sourceLabel: formatOfferDetailsSourceLabel(offer.attachKinds),
      createdByLabel: offer.createdByDisplayName,
    }),
    tabs: OFFER_DETAILS_TAB_IDS.map((id) => ({
      id,
      label: OFFER_DETAILS_TAB_LABELS[id],
    })),
    activeTabId: state.activeTabId,
    overview: {
      dateRange: state.overviewDateRange,
      dateRangeLabel: labelForOfferDetailsDateRange(state.overviewDateRange),
      kpis: buildOfferDetailsOverviewKpis(state.overviewMetrics),
      definitionTitle: OFFER_DETAILS_COPY.definitionTitle,
      definitionFields: buildOfferDetailsDefinitionFields({
        offer,
        locationName: state.locationName,
      }),
      recommendation: {
        title: OFFER_DETAILS_COPY.recommendedTitle,
        subtitle: OFFER_DETAILS_COPY.recommendedSubtitle,
        emptyCopy: OFFER_DETAILS_COPY.recommendedEmptyCopy,
      },
    },
    ...lifecycle,
  }
}

/**
 * Offer Details page module — get-by-id chrome, Overview KPIs via getOfferMetrics,
 * header lifecycle writes (Pause / Resume / Archive / Duplicate), lifecycle tab chrome (ticket 24).
 */
export function createOfferDetailsPageModule(
  adapters: OfferDetailsAdapters
): OfferDetailsPageModule {
  let state: ModuleState = {
    loadStatus: "idle",
    workspace: null,
    offer: null,
    locationName: null,
    viewModel: null,
    loadError: null,
    activeTabId: "overview",
    campaignsSubTabId: "linked",
    overviewDateRange: DEFAULT_OFFER_DETAILS_DATE_RANGE,
    overviewMetrics: emptyMetrics(),
    pendingHeaderAction: null,
    pendingRowAction: null,
    lifecycleLists: emptyLifecycleLists(),
    loadGeneration: 0,
  }

  let snapshot: OfferDetailsSnapshot = {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    loadError: state.loadError,
  }

  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = {
      loadStatus: state.loadStatus,
      viewModel: state.viewModel,
      loadError: state.loadError,
    }
    for (const listener of listeners) {
      listener()
    }
  }

  const patchPendingHeader = (
    pending: OfferDetailsPendingHeaderAction | null
  ) => {
    state = {
      ...state,
      pendingHeaderAction: pending,
      viewModel:
        state.viewModel == null
          ? null
          : {
              ...state.viewModel,
              pendingHeaderAction: pending,
            },
    }
    publish()
  }

  const patchPendingRow = (pending: OfferDetailsPendingRowAction | null) => {
    state = {
      ...state,
      pendingRowAction: pending,
      viewModel:
        state.viewModel == null
          ? null
          : {
              ...state.viewModel,
              pendingRowAction: pending,
            },
    }
    publish()
  }

  const loadMetrics = async (
    offerId: number,
    range: OfferDetailsDateRange
  ): Promise<OfferDetailsOverviewMetrics> => {
    if (adapters.getOfferMetrics == null) {
      return emptyMetrics()
    }
    return adapters.getOfferMetrics(offerId, range)
  }

  const loadLifecycleLists = async (
    offerId: number
  ): Promise<LifecycleLists> => {
    const [
      claims,
      redemptions,
      linkedCampaigns,
      issuanceSources,
      voidRequests,
    ] = await Promise.all([
      adapters.getClaims?.(offerId) ?? Promise.resolve([]),
      adapters.getRedemptions?.(offerId) ?? Promise.resolve([]),
      adapters.getLinkedCampaigns?.(offerId) ?? Promise.resolve([]),
      adapters.getIssuanceSources?.(offerId) ?? Promise.resolve([]),
      adapters.getVoidRequests?.(offerId) ?? Promise.resolve([]),
    ])
    return {
      claims,
      redemptions,
      linkedCampaigns,
      issuanceSources,
      voidRequests,
    }
  }

  const loadForWorkspace = async (input: OfferDetailsWorkspaceInput) => {
    const generation = state.loadGeneration + 1
    state = {
      ...state,
      workspace: input,
      loadGeneration: generation,
      loadStatus: "loading",
      loadError: null,
      pendingHeaderAction: null,
      pendingRowAction: null,
    }
    publish()

    if (input.selectedLocationId == null || input.offerId == null) {
      state = {
        ...state,
        loadStatus: "idle",
        offer: null,
        locationName: null,
        viewModel: null,
        loadError: null,
        overviewMetrics: emptyMetrics(),
        lifecycleLists: emptyLifecycleLists(),
      }
      publish()
      return
    }

    const location = input.locations.find(
      (entry) => entry.id === input.selectedLocationId
    )
    if (location == null) {
      state = {
        ...state,
        loadStatus: "error",
        offer: null,
        locationName: null,
        viewModel: null,
        loadError: OFFER_DETAILS_LOAD_ERROR_MESSAGE,
        overviewMetrics: emptyMetrics(),
        lifecycleLists: emptyLifecycleLists(),
      }
      publish()
      return
    }

    try {
      const offer = await adapters.getOffer(input.offerId)
      if (generation !== state.loadGeneration) {
        return
      }

      const [metrics, lifecycleLists] = await Promise.all([
        loadMetrics(offer.id, state.overviewDateRange),
        loadLifecycleLists(offer.id),
      ])
      if (generation !== state.loadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "loaded",
        offer,
        locationName: location.locationName,
        overviewMetrics: metrics,
        lifecycleLists,
        loadError: null,
        pendingHeaderAction: null,
        pendingRowAction: null,
        activeTabId: input.initialTabId ?? "overview",
      }
      state = {
        ...state,
        viewModel: assembleViewModel(state),
      }
      publish()
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "error",
        offer: null,
        locationName: null,
        viewModel: null,
        loadError: OFFER_DETAILS_LOAD_ERROR_MESSAGE,
        overviewMetrics: emptyMetrics(),
        lifecycleLists: emptyLifecycleLists(),
      }
      publish()
    }
  }

  return {
    getSnapshot() {
      return snapshot
    },
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    async syncWorkspace(input) {
      await loadForWorkspace(input)
    },
    async retryLoad() {
      if (state.workspace == null) {
        return
      }
      await loadForWorkspace(state.workspace)
    },
    setActiveTab(tabId) {
      if (state.viewModel == null) {
        return
      }
      state = {
        ...state,
        activeTabId: tabId,
      }
      state = {
        ...state,
        viewModel: assembleViewModel(state),
      }
      publish()
    },
    setCampaignsSubTab(subTabId) {
      if (state.viewModel == null) {
        return
      }
      state = {
        ...state,
        campaignsSubTabId: subTabId,
      }
      state = {
        ...state,
        viewModel: assembleViewModel(state),
      }
      publish()
    },
    async setOverviewDateRange(range) {
      if (state.viewModel == null || state.offer == null) {
        return
      }
      const offerId = state.offer.id
      state = {
        ...state,
        overviewDateRange: range,
      }
      const metrics = await loadMetrics(offerId, range)
      if (state.offer == null) {
        return
      }
      state = {
        ...state,
        overviewMetrics: metrics,
      }
      state = {
        ...state,
        viewModel: assembleViewModel(state),
      }
      publish()
    },
    requestHeaderAction(actionId) {
      if (state.viewModel == null) {
        return
      }
      // Rename opens Edit drawer in the page — no state-change confirm (ticket 10).
      if (actionId === "rename") {
        return
      }
      const allowed = state.viewModel.headerMenuItems.some(
        (item) => item.id === actionId
      )
      if (!allowed) {
        return
      }
      const copy = offerDetailsHeaderActionConfirmCopy(actionId)
      patchPendingHeader({
        actionId,
        title: copy.title,
        description: copy.description,
      })
    },
    async confirmPendingHeaderAction() {
      const pending = state.pendingHeaderAction
      if (pending == null || state.offer == null) {
        return
      }

      const offerId = state.offer.id
      const actionId = pending.actionId
      patchPendingHeader(null)

      let adapter:
        | ((id: number) => Promise<CatalogOfferDetail>)
        | undefined
      switch (actionId) {
        case "pause-issuance":
          adapter = adapters.pauseOffer
          break
        case "resume-issuance":
          adapter = adapters.resumeOffer
          break
        case "archive-offer":
          adapter = adapters.archiveOffer
          break
        case "duplicate":
          adapter = adapters.duplicateOffer
          break
        default:
          return
      }

      if (adapter == null) {
        return
      }

      try {
        const result = await adapter(offerId)
        if (actionId === "duplicate") {
          adapters.onDuplicated?.(result.id)
          return
        }

        state = {
          ...state,
          offer: result,
        }
        state = {
          ...state,
          viewModel: assembleViewModel(state),
        }
        publish()
      } catch {
        // Keep current offer chrome — dialog already closed.
      }
    },
    cancelPendingHeaderAction() {
      patchPendingHeader(null)
    },
    requestClaimsRowAction(rowId, actionId) {
      if (state.viewModel == null) {
        return
      }
      const row = state.viewModel.claims.rows.find((entry) => entry.id === rowId)
      if (row == null) {
        return
      }
      if (actionId !== "resend-offer" && actionId !== "cancel-claim") {
        return
      }
      const copy = offerDetailsClaimsRowActionConfirmCopy(actionId)
      patchPendingRow({
        tabId: "claims",
        actionId,
        rowId,
        title: copy.title,
        description: copy.description,
      })
    },
    confirmPendingRowAction() {
      patchPendingRow(null)
    },
    cancelPendingRowAction() {
      patchPendingRow(null)
    },
  }
}

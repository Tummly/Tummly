import {
  createFeedbackDetailsModule,
  type FeedbackDetailsAdapters,
  type FeedbackDetailsModule,
  type FeedbackDetailsSnapshot,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import { closeExclusiveAssistantDrawer } from "@/lib/operatorAiAssistant/assistantExclusiveOpen"
import {
  buildHomeNeedsAttention,
  type HomeNeedsAttentionProjection,
} from "@/lib/operatorHome/buildHomeNeedsAttention"
import {
  attachLiveCampaignOffer,
  buildLiveOffersSectionCards,
  type OperatorHomeLiveCard,
} from "@/lib/operatorHome/buildLiveOffersSectionCards"
import { createFinishSettingUpAcksModule } from "@/lib/operatorHome/createFinishSettingUpAcksModule"
import { buildOperatorHomeViewModel } from "@/lib/operatorHome/buildHomeViewModel"
import { mapHomeNeedsAttentionSourceFacts } from "@/lib/operatorHome/mapHomeNeedsAttentionSourceFacts"
import {
  NEEDS_ATTENTION_DUPLICATE_DRAFT_ERROR,
  NEEDS_ATTENTION_LOAD_ERROR,
} from "@/lib/operatorHome/operatorHomeSectionPresentation"
import { buildHomeRecommendationRequest } from "@/lib/operatorHome/buildHomeRecommendationRequest"
import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  ChecklistAcksResponse,
  FeedbackDetailsResponse,
  FeedbackInternalNoteItem,
  FeedbackResponse,
  FeedbackSentiment,
  FeedbackWorkflowStatus,
  HomeLatestActivityItem,
  HomeLatestActivityResponse,
  HomePerformanceResponse,
  LocationItem,
  UpdateChecklistAcksRequest,
} from "@/types/dashboard"
import type {
  CampaignDraftResponse,
  CampaignLifecycleActionRequest,
  CampaignLifecycleActionResponse,
  CampaignsListItem,
  CatalogOfferResponse,
  CatalogOffersListItem,
  OpenVoidAttentionOfferApi,
} from "@/types/operatorCampaigns"
import type {
  HomeRecommendation,
  HomeRecommendationRequest,
  HomeRecommendationResponse,
  OperatorHomeChecklistAcks,
  OperatorHomeViewModel,
  WeeklyBriefBody,
  WeeklyBriefGenerateResponse,
  WeeklyBriefGetResponse,
} from "@/types/operatorHome"

export type OperatorHomeWorkspaceInput = {
  locations: LocationItem[]
  selectedLocationId: number | null
}

export type CopySmartGuestLinkResult = "copied" | "failed" | "noop"

export const HOME_RECOMMENDATION_LOAD_ERROR_MESSAGE =
  "Could not load a recommendation. Please try again."

export const HOME_NEEDS_ATTENTION_LOAD_ERROR_MESSAGE =
  NEEDS_ATTENTION_LOAD_ERROR

export const HOME_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE =
  "Could not load your weekly brief. Please try again."

export type OperatorHomeRecommendationStatus =
  | "idle"
  | "loading"
  | "ready"
  | "error"
  | "dismissed"

export type OperatorHomeRecommendationViewModel = {
  status: OperatorHomeRecommendationStatus
  /** Present when status is ready and type is not none. */
  recommendation: HomeRecommendation | null
  /** True when status is ready and type is none. */
  isNone: boolean
  errorMessage: string | null
  errorRetryable: boolean
}

export type OperatorHomeWeeklyBriefStatus =
  | "empty"
  | "loading"
  | "ready"
  | "error"

export type OperatorHomeWeeklyBriefViewModel = {
  status: OperatorHomeWeeklyBriefStatus
  week: string | null
  body: WeeklyBriefBody | null
  errorMessage: string | null
  errorRetryable: boolean
}

export type OperatorHomePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  performanceLoadStatus: "idle" | "loading" | "loaded" | "error"
  liveOffersLoadStatus: "idle" | "loading" | "loaded" | "error"
  liveCards: OperatorHomeLiveCard[]
  liveOffersError: string | null
  liveOffersPauseBusy: boolean
  /** Home Needs attention — not on OperatorHomeViewModel (ticket 02). */
  needsAttentionLoadStatus: "idle" | "loading" | "loaded" | "error"
  needsAttention: HomeNeedsAttentionProjection | null
  needsAttentionError: string | null
  viewModel: OperatorHomeViewModel | null
  previewBusy: boolean
  actionError: string | null
  feedbackDetails: FeedbackDetailsSnapshot
  /** Home Recommended next step — not on OperatorHomeViewModel (ticket 04). */
  recommendation: OperatorHomeRecommendationViewModel
  /** Weekly brief — not on OperatorHomeViewModel (ticket 06). */
  weeklyBrief: OperatorHomeWeeklyBriefViewModel
}

export type ClassificationTerminalSignal = {
  feedbackId: number
  locationId: number
}

export type FeedbackHomeRealtimeHandlers = {
  onClassificationTerminal: (signal: ClassificationTerminalSignal) => void
  onReconnected: () => void
}

export type FeedbackHomeRealtimeSession = {
  stop: () => Promise<void>
}

export type OperatorHomePageAdapters = {
  getFeedback: (locationId: number) => Promise<FeedbackResponse>
  getHomeLatestActivity: (
    locationId: number
  ) => Promise<HomeLatestActivityResponse>
  getHomePerformance: (
    locationId: number,
    from: string,
    to: string
  ) => Promise<HomePerformanceResponse>
  getHomePerformanceDateRange: () => HomePerformanceDateRange
  loadHomeRecommendation: (input: {
    request: HomeRecommendationRequest
  }) => Promise<HomeRecommendationResponse>
  getWeeklyBrief: (locationId: number) => Promise<WeeklyBriefGetResponse>
  generateWeeklyBrief: (
    locationId: number
  ) => Promise<WeeklyBriefGenerateResponse>
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  correctClassification: FeedbackDetailsAdapters["correctClassification"]
  updateDetectedTags: FeedbackDetailsAdapters["updateDetectedTags"]
  setWorkflowStatus: FeedbackDetailsAdapters["setWorkflowStatus"]
  closeOutFeedback: FeedbackDetailsAdapters["closeOutFeedback"]
  createInternalNote: (
    feedbackId: number,
    body: string
  ) => Promise<FeedbackInternalNoteItem>
  updateInternalNote: (
    feedbackId: number,
    noteId: number,
    body: string
  ) => Promise<FeedbackInternalNoteItem>
  deleteInternalNote: (
    feedbackId: number,
    noteId: number
  ) => Promise<{ deletedAt: string; deletedByDisplayName: string }>
  getChecklistAcks: (locationId: number) => Promise<ChecklistAcksResponse>
  setChecklistAcks: (
    locationId: number,
    body: UpdateChecklistAcksRequest
  ) => Promise<ChecklistAcksResponse>
  /** Lightweight “has any catalog offer” probe for setup checklist. */
  hasCreatedOffer: (locationId: number) => Promise<boolean>
  /** Lightweight “has any campaign” probe for setup checklist. */
  hasCreatedCampaign: (locationId: number) => Promise<boolean>
  copyText: (
    text: string
  ) => Promise<{ ok: true } | { ok: false; error: string }>
  openSmartGuestLink: (url: string) => void
  connectRealtime: (
    handlers: FeedbackHomeRealtimeHandlers
  ) => Promise<FeedbackHomeRealtimeSession>
  onPerformanceLoadError?: (message: string) => void
  listLiveOffers: (locationId: number) => Promise<CatalogOffersListItem[]>
  listLiveCampaigns: (locationId: number) => Promise<CampaignsListItem[]>
  getNeedsAttentionFeedback: (locationId: number) => Promise<{
    count: number
    newestSubmittedAt: string | null
  }>
  listNeedsAttentionCampaigns: (
    locationId: number
  ) => Promise<CampaignsListItem[]>
  listNeedsAttentionOffers: (
    locationId: number
  ) => Promise<CatalogOffersListItem[]>
  listOpenVoidAttention: (
    locationId: number
  ) => Promise<OpenVoidAttentionOfferApi[]>
  pauseCampaign: (
    campaignId: number,
    body: CampaignLifecycleActionRequest
  ) => Promise<CampaignLifecycleActionResponse>
  duplicateCampaign: (
    campaignId: number,
    body: CampaignLifecycleActionRequest
  ) => Promise<CampaignLifecycleActionResponse>
  getCampaignDraftById?: (campaignId: number) => Promise<CampaignDraftResponse>
  getCatalogOfferById?: (offerId: number) => Promise<CatalogOfferResponse>
}

export type DuplicateNeedsAttentionCampaignResult =
  | { ok: true; campaignId: number }
  | { ok: false; error: string }

export type OperatorHomePageModule = {
  getSnapshot: () => OperatorHomePageSnapshot
  subscribe: (listener: () => void) => () => void
  connect: () => Promise<void>
  disconnect: () => Promise<void>
  syncWorkspace: (input: OperatorHomeWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  /** Re-load Home using the current Home performance date range from adapters. */
  reloadForHomePerformanceDateRange: () => Promise<void>
  /** Explicit recommendation retry / refresh (bypasses server cache). */
  retryRecommendation: () => Promise<void>
  /** Session hide only — does not write server dismiss/cache. */
  dismissRecommendation: () => void
  /** Retry Weekly brief (GET; generate again only if still missing). */
  retryWeeklyBrief: () => Promise<void>
  retryLiveOffers: () => Promise<void>
  retryNeedsAttention: () => Promise<void>
  pauseLiveCampaign: (campaignId: number) => Promise<boolean>
  duplicateNeedsAttentionCampaign: (
    campaignId: number
  ) => Promise<DuplicateNeedsAttentionCampaignResult>
  previewGuestForm: () => void
  copySmartGuestLink: () => Promise<CopySmartGuestLinkResult>
  openFeedbackDetails: (feedbackId: number) => Promise<void>
  closeFeedbackDetails: () => void
  retryFeedbackDetails: () => Promise<void>
  startClassificationCorrection: () => void
  setClassificationDraftSentiment: (sentiment: FeedbackSentiment) => void
  setClassificationDraftReason: FeedbackDetailsModule["setDraftReason"]
  setClassificationDraftNote: FeedbackDetailsModule["setDraftNote"]
  cancelClassificationCorrection: () => void
  saveClassificationCorrection: () => Promise<void>
  startEditTags: () => void
  stageEditTag: (key: string) => void
  unstageEditTag: (key: string) => void
  setEditTagsSentiment: (sentiment: FeedbackSentiment) => void
  cancelEditTags: () => void
  applyEditTags: () => Promise<void>
  setFeedbackWorkflowStatus: (status: FeedbackWorkflowStatus) => Promise<boolean>
  reopenFeedback: () => Promise<boolean>
  startFeedbackMarkNoActionNeeded: () => boolean
  startFeedbackMarkResolved: () => boolean
  setFeedbackCloseOutReason: FeedbackDetailsModule["setCloseOutReason"]
  setFeedbackCloseOutNoteDraft: FeedbackDetailsModule["setCloseOutNoteDraft"]
  setFeedbackCloseOutAcknowledged: FeedbackDetailsModule["setCloseOutAcknowledged"]
  cancelFeedbackCloseOut: FeedbackDetailsModule["cancelCloseOut"]
  confirmFeedbackCloseOut: () => Promise<boolean>
  setFeedbackInternalNoteDraft: (value: string) => void
  createFeedbackInternalNote: () => Promise<boolean>
  startFeedbackNoteEdit: (noteId: number) => void
  setFeedbackNoteEditDraft: (value: string) => void
  cancelFeedbackNoteEdit: () => void
  saveFeedbackNoteEdit: () => Promise<boolean>
  startFeedbackNoteDelete: (noteId: number) => void
  cancelFeedbackNoteDelete: () => void
  confirmFeedbackNoteDelete: () => Promise<boolean>
}

type HomeState = {
  loadStatus: OperatorHomePageSnapshot["loadStatus"]
  performanceLoadStatus: OperatorHomePageSnapshot["performanceLoadStatus"]
  liveOffersLoadStatus: OperatorHomePageSnapshot["liveOffersLoadStatus"]
  liveCards: OperatorHomeLiveCard[]
  liveOffersError: string | null
  liveOffersPauseBusy: boolean
  needsAttentionLoadStatus: OperatorHomePageSnapshot["needsAttentionLoadStatus"]
  needsAttention: HomeNeedsAttentionProjection | null
  needsAttentionError: string | null
  workspace: OperatorHomeWorkspaceInput | null
  feedback: { total: number; recent: FeedbackResponse["recent"] } | null
  latestActivity: HomeLatestActivityItem[] | null
  feedbackSubmitted: number | null
  feedbackSubmittedPrevious: number | null
  guestsJoined: number | null
  guestsJoinedPrevious: number | null
  qrScans: number | null
  qrScansPrevious: number | null
  hasCreatedOffer: boolean
  hasCreatedCampaign: boolean
  viewModel: OperatorHomeViewModel | null
  actionError: string | null
  loadGeneration: number
  performanceLoadGeneration: number
  liveOffersLoadGeneration: number
  needsAttentionLoadGeneration: number
}

type HomeAction =
  | { type: "workspace_cleared" }
  | {
      type: "workspace_synced"
      workspace: OperatorHomeWorkspaceInput
      viewModel: OperatorHomeViewModel | null
    }
  | {
      type: "workspace_fields_updated"
      workspace: OperatorHomeWorkspaceInput
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "load_started"; generation: number }
  | {
      type: "load_succeeded"
      generation: number
      feedback: { total: number; recent: FeedbackResponse["recent"] }
      latestActivity: HomeLatestActivityItem[]
      feedbackSubmitted: number | null
      feedbackSubmittedPrevious: number | null
      guestsJoined: number | null
      guestsJoinedPrevious: number | null
      qrScans: number | null
      qrScansPrevious: number | null
      hasCreatedOffer: boolean
      hasCreatedCampaign: boolean
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "load_failed"; generation: number }
  | { type: "performance_load_started"; generation: number }
  | {
      type: "performance_load_succeeded"
      generation: number
      feedbackSubmitted: number
      feedbackSubmittedPrevious: number
      guestsJoined: number
      guestsJoinedPrevious: number
      qrScans: number
      qrScansPrevious: number
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "performance_load_failed"; generation: number }
  | {
      type: "live_offers_load_started"
      generation: number
      /** Keep prior cards visible (pause refresh) — do not flip to loading. */
      keepVisible?: boolean
    }
  | {
      type: "live_offers_load_succeeded"
      generation: number
      liveCards: OperatorHomeLiveCard[]
    }
  | {
      type: "live_offers_load_failed"
      generation: number
      error: string
    }
  | {
      type: "live_offers_pause_busy"
      busy: boolean
    }
  | {
      type: "needs_attention_load_started"
      generation: number
    }
  | {
      type: "needs_attention_load_succeeded"
      generation: number
      projection: HomeNeedsAttentionProjection
    }
  | {
      type: "needs_attention_load_failed"
      generation: number
      error: string
    }
  | {
      type: "view_model_updated"
      viewModel: OperatorHomeViewModel | null
    }
  | {
      type: "activity_patched"
      feedback: { total: number; recent: FeedbackResponse["recent"] }
      latestActivity: HomeLatestActivityItem[]
      viewModel: OperatorHomeViewModel | null
    }
  | {
      type: "feedback_patched"
      feedback: { total: number; recent: FeedbackResponse["recent"] }
      latestActivity: HomeLatestActivityItem[] | null
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "action_error"; error: string | null }

function assembleViewModel(
  workspace: OperatorHomeWorkspaceInput,
  checklistAcks: OperatorHomeChecklistAcks,
  feedback: HomeState["feedback"],
  latestActivity: HomeState["latestActivity"],
  feedbackSubmitted: number | null,
  guestsJoined: number | null,
  feedbackSubmittedPrevious: number | null,
  guestsJoinedPrevious: number | null,
  qrScans: number | null,
  qrScansPrevious: number | null,
  dateRangeLabel: string,
  hasCreatedOffer: boolean,
  hasCreatedCampaign: boolean
): OperatorHomeViewModel | null {
  if (workspace.selectedLocationId == null) {
    return null
  }

  return buildOperatorHomeViewModel({
    locations: workspace.locations,
    selectedLocationId: workspace.selectedLocationId,
    feedback,
    latestActivity,
    feedbackSubmitted,
    feedbackSubmittedPrevious,
    guestsJoined,
    guestsJoinedPrevious,
    qrScans,
    qrScansPrevious,
    dateRangeLabel,
    checklistAcks,
    hasCreatedOffer,
    hasCreatedCampaign,
  })
}

function reduce(state: HomeState, action: HomeAction): HomeState {
  switch (action.type) {
    case "workspace_cleared":
      return {
        ...state,
        loadStatus: "idle",
        performanceLoadStatus: "idle",
        liveOffersLoadStatus: "idle",
        liveCards: [],
        liveOffersError: null,
        liveOffersPauseBusy: false,
        needsAttentionLoadStatus: "idle",
        needsAttention: null,
        needsAttentionError: null,
        workspace: null,
        feedback: null,
        latestActivity: null,
        feedbackSubmitted: null,
        feedbackSubmittedPrevious: null,
        guestsJoined: null,
        guestsJoinedPrevious: null,
        qrScans: null,
        qrScansPrevious: null,
        hasCreatedOffer: false,
        hasCreatedCampaign: false,
        viewModel: null,
        actionError: null,
      }
    case "workspace_synced":
      return {
        ...state,
        workspace: action.workspace,
        feedback: null,
        latestActivity: null,
        liveOffersLoadStatus: "idle",
        liveCards: [],
        liveOffersError: null,
        liveOffersPauseBusy: false,
        needsAttentionLoadStatus: "idle",
        needsAttention: null,
        needsAttentionError: null,
        viewModel: action.viewModel,
        actionError: null,
      }
    case "workspace_fields_updated":
      return {
        ...state,
        workspace: action.workspace,
        viewModel: action.viewModel,
      }
    case "load_started":
      return {
        ...state,
        loadStatus: "loading",
        loadGeneration: action.generation,
      }
    case "load_succeeded":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return {
        ...state,
        loadStatus: "loaded",
        feedback: action.feedback,
        latestActivity: action.latestActivity,
        feedbackSubmitted: action.feedbackSubmitted,
        feedbackSubmittedPrevious: action.feedbackSubmittedPrevious,
        guestsJoined: action.guestsJoined,
        guestsJoinedPrevious: action.guestsJoinedPrevious,
        qrScans: action.qrScans,
        qrScansPrevious: action.qrScansPrevious,
        hasCreatedOffer: action.hasCreatedOffer,
        hasCreatedCampaign: action.hasCreatedCampaign,
        viewModel: action.viewModel,
      }
    case "load_failed":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return { ...state, loadStatus: "error" }
    case "performance_load_started":
      return {
        ...state,
        performanceLoadStatus: "loading",
        performanceLoadGeneration: action.generation,
      }
    case "performance_load_succeeded":
      if (action.generation !== state.performanceLoadGeneration) {
        return state
      }
      return {
        ...state,
        performanceLoadStatus: "loaded",
        feedbackSubmitted: action.feedbackSubmitted,
        feedbackSubmittedPrevious: action.feedbackSubmittedPrevious,
        guestsJoined: action.guestsJoined,
        guestsJoinedPrevious: action.guestsJoinedPrevious,
        qrScans: action.qrScans,
        qrScansPrevious: action.qrScansPrevious,
        viewModel: action.viewModel,
      }
    case "performance_load_failed":
      if (action.generation !== state.performanceLoadGeneration) {
        return state
      }
      return { ...state, performanceLoadStatus: "error" }
    case "live_offers_load_started": {
      const keepVisible =
        action.keepVisible === true && state.liveOffersLoadStatus === "loaded"
      return {
        ...state,
        liveOffersLoadStatus: keepVisible ? "loaded" : "loading",
        liveOffersLoadGeneration: action.generation,
        liveOffersError: keepVisible ? state.liveOffersError : null,
        ...(keepVisible ? {} : { liveOffersPauseBusy: false }),
      }
    }
    case "live_offers_load_succeeded":
      if (action.generation !== state.liveOffersLoadGeneration) {
        return state
      }
      return {
        ...state,
        liveOffersLoadStatus: "loaded",
        liveCards: action.liveCards,
        liveOffersError: null,
      }
    case "live_offers_load_failed":
      if (action.generation !== state.liveOffersLoadGeneration) {
        return state
      }
      return {
        ...state,
        liveOffersLoadStatus: "error",
        liveCards: [],
        liveOffersError: action.error,
      }
    case "live_offers_pause_busy":
      return {
        ...state,
        liveOffersPauseBusy: action.busy,
      }
    case "needs_attention_load_started":
      return {
        ...state,
        needsAttentionLoadStatus: "loading",
        needsAttentionLoadGeneration: action.generation,
        needsAttentionError: null,
      }
    case "needs_attention_load_succeeded":
      if (action.generation !== state.needsAttentionLoadGeneration) {
        return state
      }
      return {
        ...state,
        needsAttentionLoadStatus: "loaded",
        needsAttention: action.projection,
        needsAttentionError: null,
      }
    case "needs_attention_load_failed":
      if (action.generation !== state.needsAttentionLoadGeneration) {
        return state
      }
      return {
        ...state,
        needsAttentionLoadStatus: "error",
        needsAttention: null,
        needsAttentionError: action.error,
      }
    case "view_model_updated":
      return {
        ...state,
        viewModel: action.viewModel,
      }
    case "activity_patched":
      return {
        ...state,
        feedback: action.feedback,
        latestActivity: action.latestActivity,
        viewModel: action.viewModel,
      }
    case "feedback_patched":
      return {
        ...state,
        feedback: action.feedback,
        latestActivity: action.latestActivity ?? state.latestActivity,
        viewModel: action.viewModel,
      }
    case "action_error":
      return { ...state, actionError: action.error }
    default:
      return state
  }
}

function idleRecommendation(): OperatorHomeRecommendationViewModel {
  return {
    status: "idle",
    recommendation: null,
    isNone: false,
    errorMessage: null,
    errorRetryable: false,
  }
}

function emptyWeeklyBrief(): OperatorHomeWeeklyBriefViewModel {
  return {
    status: "empty",
    week: null,
    body: null,
    errorMessage: null,
    errorRetryable: false,
  }
}

function mapReadyWeeklyBrief(
  response: Extract<WeeklyBriefGetResponse, { ready: true }>
): OperatorHomeWeeklyBriefViewModel {
  return {
    status: "ready",
    week: response.week,
    body: response.body,
    errorMessage: null,
    errorRetryable: false,
  }
}

function weeklyBriefErrorFrom(
  message: string | null | undefined,
  retryable: boolean
): OperatorHomeWeeklyBriefViewModel {
  return {
    status: "error",
    week: null,
    body: null,
    errorMessage: message?.trim() || HOME_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE,
    errorRetryable: retryable,
  }
}

/**
 * Client soft-cache key — location + Home performance selection identity.
 * Do not use resolved `from`/`to` timestamps: preset windows bind `to` to `now`,
 * so ISO strings change every call even when the operator selection is unchanged.
 */
function recommendationSoftCacheKey(
  locationId: number,
  dateRange: HomePerformanceDateRange
): string {
  if (dateRange.kind === "preset") {
    return `${locationId}:preset:${dateRange.presetId}`
  }
  return `${locationId}:custom:${dateRange.startDate}:${dateRange.endDate}`
}

function mapRecommendationResponse(
  response: HomeRecommendationResponse
): OperatorHomeRecommendationViewModel {
  if (!response.success || response.recommendation == null) {
    return {
      status: "error",
      recommendation: null,
      isNone: false,
      errorMessage:
        response.message ?? HOME_RECOMMENDATION_LOAD_ERROR_MESSAGE,
      errorRetryable: response.retryable !== false,
    }
  }

  const isNone = response.recommendation.type === "none"
  return {
    status: "ready",
    recommendation: isNone ? null : response.recommendation,
    isNone,
    errorMessage: null,
    errorRetryable: false,
  }
}

export function createOperatorHomePageModule(
  adapters: OperatorHomePageAdapters
): OperatorHomePageModule {
  const acks = createFinishSettingUpAcksModule({
    getChecklistAcks: adapters.getChecklistAcks,
    setChecklistAcks: adapters.setChecklistAcks,
  })
  const feedbackDetails = createFeedbackDetailsModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    correctClassification: adapters.correctClassification,
    updateDetectedTags: adapters.updateDetectedTags,
    setWorkflowStatus: adapters.setWorkflowStatus,
    createInternalNote: adapters.createInternalNote,
    updateInternalNote: adapters.updateInternalNote,
    deleteInternalNote: adapters.deleteInternalNote,
    closeOutFeedback: adapters.closeOutFeedback,
  })

  let state: HomeState = {
    loadStatus: "idle",
    performanceLoadStatus: "idle",
    liveOffersLoadStatus: "idle",
    liveCards: [],
    liveOffersError: null,
    liveOffersPauseBusy: false,
    needsAttentionLoadStatus: "idle",
    needsAttention: null,
    needsAttentionError: null,
    workspace: null,
    feedback: null,
    latestActivity: null,
    feedbackSubmitted: null,
    feedbackSubmittedPrevious: null,
    guestsJoined: null,
    guestsJoinedPrevious: null,
    qrScans: null,
    qrScansPrevious: null,
    hasCreatedOffer: false,
    hasCreatedCampaign: false,
    viewModel: null,
    actionError: null,
    loadGeneration: 0,
    performanceLoadGeneration: 0,
    liveOffersLoadGeneration: 0,
    needsAttentionLoadGeneration: 0,
  }

  let snapshot: OperatorHomePageSnapshot = {
    loadStatus: state.loadStatus,
    performanceLoadStatus: state.performanceLoadStatus,
    liveOffersLoadStatus: state.liveOffersLoadStatus,
    liveCards: state.liveCards,
    liveOffersError: state.liveOffersError,
    liveOffersPauseBusy: state.liveOffersPauseBusy,
    needsAttentionLoadStatus: state.needsAttentionLoadStatus,
    needsAttention: state.needsAttention,
    needsAttentionError: state.needsAttentionError,
    viewModel: state.viewModel,
    previewBusy: false,
    actionError: null,
    feedbackDetails: feedbackDetails.getSnapshot(),
    recommendation: idleRecommendation(),
    weeklyBrief: emptyWeeklyBrief(),
  }

  const listeners = new Set<() => void>()
  /** Session-only Not now — survives recommendation reloads until location change. */
  let recommendationDismissedForSession = false
  /**
   * Last ready recommendation for the current soft-cache key.
   * Keeps return visits / remount reloads from flashing an empty loading card
   * when the Home performance window key is unchanged.
   */
  let softCachedRecommendation: {
    cacheKey: string
    viewModel: OperatorHomeRecommendationViewModel
  } | null = null
  let recommendation: OperatorHomeRecommendationViewModel = idleRecommendation()
  let recommendationGeneration = 0
  let needsAttentionDuplicateBusy = false
  let weeklyBrief: OperatorHomeWeeklyBriefViewModel = emptyWeeklyBrief()
  let weeklyBriefGeneration = 0

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const currentAcks = (): OperatorHomeChecklistAcks => {
    const ackSnapshot = acks.getSnapshot()
    return {
      guestFormPreviewed: ackSnapshot.guestFormPreviewed,
      qrPlacementGuideViewed: ackSnapshot.qrPlacementGuideViewed,
      logoUploaded: ackSnapshot.logoUploaded,
    }
  }

  const publish = () => {
    const ackSnapshot = acks.getSnapshot()
    snapshot = {
      loadStatus: state.loadStatus,
      performanceLoadStatus: state.performanceLoadStatus,
      liveOffersLoadStatus: state.liveOffersLoadStatus,
      liveCards: state.liveCards,
      liveOffersError: state.liveOffersError,
      liveOffersPauseBusy: state.liveOffersPauseBusy,
      needsAttentionLoadStatus: state.needsAttentionLoadStatus,
      needsAttention: state.needsAttention,
      needsAttentionError: state.needsAttentionError,
      viewModel: state.viewModel,
      previewBusy: ackSnapshot.acknowledgeBusy,
      actionError: ackSnapshot.acknowledgeError ?? state.actionError,
      feedbackDetails: feedbackDetails.getSnapshot(),
      recommendation,
      weeklyBrief,
    }
    emit()
  }

  const dispatch = (action: HomeAction) => {
    state = reduce(state, action)
    publish()
  }

  const currentDateRangeLabel = () =>
    labelForHomePerformanceDateRange(adapters.getHomePerformanceDateRange())

  const rememberSoftCachedRecommendation = (
    cacheKey: string,
    next: OperatorHomeRecommendationViewModel
  ) => {
    if (next.status !== "ready") {
      return
    }
    softCachedRecommendation = {
      cacheKey,
      viewModel: { ...next },
    }
  }

  const recommendationForSoftLoad = (input: {
    refresh: boolean
    cacheKey: string
  }): OperatorHomeRecommendationViewModel => {
    if (recommendationDismissedForSession) {
      return { ...idleRecommendation(), status: "dismissed" }
    }
    if (
      !input.refresh
      && softCachedRecommendation != null
      && softCachedRecommendation.cacheKey === input.cacheKey
      && softCachedRecommendation.viewModel.status === "ready"
    ) {
      return softCachedRecommendation.viewModel
    }
    return { ...idleRecommendation(), status: "loading" }
  }

  const patchRecommendation = (
    next: OperatorHomeRecommendationViewModel,
    options?: { softCacheKey?: string }
  ) => {
    if (options?.softCacheKey != null) {
      rememberSoftCachedRecommendation(options.softCacheKey, next)
    }
    recommendation = next
    publish()
  }

  const patchWeeklyBrief = (next: OperatorHomeWeeklyBriefViewModel) => {
    weeklyBrief = next
    publish()
  }

  /**
   * GET current closed week; if missing, soft-load while lazy generate runs, then re-GET.
   * Default week from GET is already the closed prior week (Monday started) — always eligible.
   * Keep `empty` through the first GET on a cold load so a ready brief does not flash a spinner.
   */
  const runWeeklyBriefLoad = async (options?: {
    showLoadingImmediately?: boolean
  }) => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const generation = weeklyBriefGeneration + 1
    weeklyBriefGeneration = generation

    if (options?.showLoadingImmediately === true) {
      patchWeeklyBrief({
        ...emptyWeeklyBrief(),
        status: "loading",
      })
    }

    try {
      const first = await adapters.getWeeklyBrief(selectedLocationId)
      if (generation !== weeklyBriefGeneration) {
        return
      }

      if (first.success && first.ready) {
        patchWeeklyBrief(mapReadyWeeklyBrief(first))
        return
      }

      // Missing after closed week is due — soft loading while lazy generate runs.
      patchWeeklyBrief({
        ...emptyWeeklyBrief(),
        status: "loading",
      })

      const generated = await adapters.generateWeeklyBrief(selectedLocationId)
      if (generation !== weeklyBriefGeneration) {
        return
      }

      if (!generated.success) {
        patchWeeklyBrief(
          weeklyBriefErrorFrom(
            generated.message,
            generated.retryable !== false
          )
        )
        return
      }

      const second = await adapters.getWeeklyBrief(selectedLocationId)
      if (generation !== weeklyBriefGeneration) {
        return
      }

      if (second.success && second.ready) {
        patchWeeklyBrief(mapReadyWeeklyBrief(second))
        return
      }

      patchWeeklyBrief(
        weeklyBriefErrorFrom(HOME_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE, true)
      )
    } catch {
      if (generation !== weeklyBriefGeneration) {
        return
      }
      patchWeeklyBrief(
        weeklyBriefErrorFrom(HOME_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE, true)
      )
    }
  }

  const loadWeeklyBrief = () => runWeeklyBriefLoad()
  const retryWeeklyBrief = () =>
    runWeeklyBriefLoad({ showLoadingImmediately: true })

  const loadRecommendation = async (options?: { refresh?: boolean }) => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }
    if (recommendationDismissedForSession) {
      return
    }

    const performanceDateRange = adapters.getHomePerformanceDateRange()
    const request = buildHomeRecommendationRequest({
      locationId: selectedLocationId,
      performanceDateRange,
      refresh: options?.refresh === true,
    })
    const cacheKey = recommendationSoftCacheKey(
      selectedLocationId,
      performanceDateRange
    )
    const generation = recommendationGeneration + 1
    recommendationGeneration = generation
    recommendation = recommendationForSoftLoad({
      refresh: options?.refresh === true,
      cacheKey,
    })
    publish()

    try {
      const response = await adapters.loadHomeRecommendation({ request })
      if (generation !== recommendationGeneration) {
        return
      }
      if (recommendationDismissedForSession) {
        return
      }
      patchRecommendation(mapRecommendationResponse(response), {
        softCacheKey: cacheKey,
      })
    } catch {
      if (generation !== recommendationGeneration) {
        return
      }
      if (recommendationDismissedForSession) {
        return
      }
      patchRecommendation({
        status: "error",
        recommendation: null,
        isNone: false,
        errorMessage: HOME_RECOMMENDATION_LOAD_ERROR_MESSAGE,
        errorRetryable: true,
      })
    }
  }

  const assembleCurrent = (
    workspace: OperatorHomeWorkspaceInput,
    checklistAcks: OperatorHomeChecklistAcks,
    feedback: HomeState["feedback"],
    latestActivity: HomeState["latestActivity"],
    feedbackSubmitted: number | null,
    guestsJoined: number | null,
    feedbackSubmittedPrevious: number | null,
    guestsJoinedPrevious: number | null,
    qrScans: number | null,
    qrScansPrevious: number | null,
    dateRangeLabel: string,
    hasCreatedOffer: boolean = state.hasCreatedOffer,
    hasCreatedCampaign: boolean = state.hasCreatedCampaign
  ) =>
    assembleViewModel(
      workspace,
      checklistAcks,
      feedback,
      latestActivity,
      feedbackSubmitted,
      guestsJoined,
      feedbackSubmittedPrevious,
      guestsJoinedPrevious,
      qrScans,
      qrScansPrevious,
      dateRangeLabel,
      hasCreatedOffer,
      hasCreatedCampaign
    )

  const refreshViewModelFromAcks = () => {
    const workspace = state.workspace
    if (workspace == null) {
      publish()
      return
    }

    dispatch({
      type: "view_model_updated",
      viewModel: assembleCurrent(
        workspace,
        currentAcks(),
        state.feedback,
        state.latestActivity,
        state.feedbackSubmitted,
        state.guestsJoined,
        state.feedbackSubmittedPrevious,
        state.guestsJoinedPrevious,
        state.qrScans,
        state.qrScansPrevious,
        currentDateRangeLabel()
      ),
    })
  }

  acks.subscribe(() => {
    refreshViewModelFromAcks()
  })

  feedbackDetails.subscribe(() => {
    publish()
  })

  const enrichLiveCardsWithCampaignMessages = async (
    cards: OperatorHomeLiveCard[],
    offers: readonly CatalogOffersListItem[]
  ): Promise<OperatorHomeLiveCard[]> => {
    const getDraft = adapters.getCampaignDraftById
    if (getDraft == null) {
      return cards
    }

    const offersById = new Map(offers.map((offer) => [offer.id, offer]))

    return Promise.all(
      cards.map(async (card) => {
        if (card.kind !== "campaign") {
          return card
        }
        try {
          const response = await getDraft(card.id)
          const offerId = response.campaign.offerId
          let attached: Pick<
            CatalogOffersListItem,
            "title" | "description" | "validity" | "expiryDate"
          > | null =
            offerId != null ? offersById.get(offerId) ?? null : null
          if (
            attached == null
            && offerId != null
            && adapters.getCatalogOfferById != null
          ) {
            try {
              const offerResponse = await adapters.getCatalogOfferById(offerId)
              attached = offerResponse.offer
            } catch {
              attached = null
            }
          }
          return attachLiveCampaignOffer(
            {
              ...card,
              messageSubject: response.campaign.messageSubject,
              messageBody: response.campaign.messageBody,
            },
            attached
          )
        } catch {
          return card
        }
      })
    )
  }

  const fetchLiveOffersForSelectedLocation = async (options?: {
    keepVisible?: boolean
  }) => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const keepVisible = options?.keepVisible === true
    const generation = state.liveOffersLoadGeneration + 1
    dispatch({
      type: "live_offers_load_started",
      generation,
      keepVisible,
    })

    try {
      const [offers, campaigns] = await Promise.all([
        adapters.listLiveOffers(selectedLocationId),
        adapters.listLiveCampaigns(selectedLocationId),
      ])

      if (generation !== state.liveOffersLoadGeneration) {
        return
      }

      const cards = buildLiveOffersSectionCards({ campaigns, offers })
      const enrichedCards = await enrichLiveCardsWithCampaignMessages(
        cards,
        offers
      )

      if (generation !== state.liveOffersLoadGeneration) {
        return
      }

      dispatch({
        type: "live_offers_load_succeeded",
        generation,
        liveCards: enrichedCards,
      })
    } catch {
      if (generation !== state.liveOffersLoadGeneration) {
        return
      }
      if (keepVisible && state.liveOffersLoadStatus === "loaded") {
        dispatch({
          type: "action_error",
          error:
            "Could not refresh live offers and campaigns. Please try again.",
        })
        return
      }
      dispatch({
        type: "live_offers_load_failed",
        generation,
        error: "Could not load live offers and campaigns. Please try again.",
      })
    }
  }

  const fetchNeedsAttentionForSelectedLocation = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const location = workspace.locations.find(
      (entry) => entry.id === selectedLocationId
    )
    const locationName = location?.locationName ?? ""
    const generation = state.needsAttentionLoadGeneration + 1
    dispatch({ type: "needs_attention_load_started", generation })

    try {
      const [feedback, campaigns, offers, openVoids] = await Promise.all([
        adapters.getNeedsAttentionFeedback(selectedLocationId),
        adapters.listNeedsAttentionCampaigns(selectedLocationId),
        adapters.listNeedsAttentionOffers(selectedLocationId),
        adapters.listOpenVoidAttention(selectedLocationId),
      ])

      if (generation !== state.needsAttentionLoadGeneration) {
        return
      }

      const facts = mapHomeNeedsAttentionSourceFacts({
        feedback,
        campaigns,
        offers,
        openVoids,
      })
      const projection = buildHomeNeedsAttention({
        locationName,
        feedback: facts.feedback,
        campaigns: facts.campaigns,
        offers: facts.offers,
      })

      dispatch({
        type: "needs_attention_load_succeeded",
        generation,
        projection,
      })
    } catch {
      if (generation !== state.needsAttentionLoadGeneration) {
        return
      }
      dispatch({
        type: "needs_attention_load_failed",
        generation,
        error: HOME_NEEDS_ATTENTION_LOAD_ERROR_MESSAGE,
      })
    }
  }

  const fetchPerformanceForSelectedLocation = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const generation = state.performanceLoadGeneration + 1
    dispatch({ type: "performance_load_started", generation })

    try {
      const performanceWindow = resolveHomePerformanceWindow(
        adapters.getHomePerformanceDateRange()
      )
      const performanceResult = await adapters.getHomePerformance(
        selectedLocationId,
        performanceWindow.from.toISOString(),
        performanceWindow.to.toISOString()
      )

      if (generation !== state.performanceLoadGeneration) {
        return
      }

      dispatch({
        type: "performance_load_succeeded",
        generation,
        feedbackSubmitted: performanceResult.feedbackSubmitted,
        feedbackSubmittedPrevious:
          performanceResult.feedbackSubmittedPrevious,
        guestsJoined: performanceResult.guestsJoined,
        guestsJoinedPrevious: performanceResult.guestsJoinedPrevious,
        qrScans: performanceResult.qrScans,
        qrScansPrevious: performanceResult.qrScansPrevious,
        viewModel: assembleCurrent(
          workspace,
          currentAcks(),
          state.feedback,
          state.latestActivity,
          performanceResult.feedbackSubmitted,
          performanceResult.guestsJoined,
          performanceResult.feedbackSubmittedPrevious,
          performanceResult.guestsJoinedPrevious,
          performanceResult.qrScans,
          performanceResult.qrScansPrevious,
          currentDateRangeLabel()
        ),
      })
    } catch {
      if (generation !== state.performanceLoadGeneration) {
        return
      }
      dispatch({ type: "performance_load_failed", generation })
      adapters.onPerformanceLoadError?.(
        "Could not load performance stats. Please try again."
      )
    }
  }

  const refreshFeedbackInBackground = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (
      workspace == null
      || selectedLocationId == null
      || state.loadStatus !== "loaded"
    ) {
      return
    }

    try {
      const [feedbackResult, latestActivityResult] = await Promise.all([
        adapters.getFeedback(selectedLocationId),
        adapters.getHomeLatestActivity(selectedLocationId),
      ])
      const feedback = {
        total: feedbackResult.total,
        recent: feedbackResult.recent,
      }
      const latestActivity = latestActivityResult.items
      dispatch({
        type: "activity_patched",
        feedback,
        latestActivity,
        viewModel: assembleCurrent(
          workspace,
          currentAcks(),
          feedback,
          latestActivity,
          state.feedbackSubmitted,
          state.guestsJoined,
          state.feedbackSubmittedPrevious,
          state.guestsJoinedPrevious,
          state.qrScans,
          state.qrScansPrevious,
          currentDateRangeLabel()
        ),
      })
    } catch {
      // Keep Latest activity on the last good list when a quiet refresh fails.
    }
  }

  const loadForSelectedLocation = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const generation = state.loadGeneration + 1
    const performanceDateRange = adapters.getHomePerformanceDateRange()
    const recommendationCacheKey = recommendationSoftCacheKey(
      selectedLocationId,
      performanceDateRange
    )
    const nextRecommendation = recommendationForSoftLoad({
      refresh: false,
      cacheKey: recommendationCacheKey,
    })
    recommendationGeneration += 1
    const thisRecommendationGeneration = recommendationGeneration
    recommendation = nextRecommendation
    dispatch({ type: "load_started", generation })
    void fetchLiveOffersForSelectedLocation()
    void fetchNeedsAttentionForSelectedLocation()
    void loadWeeklyBrief()

    let feedback: { total: number; recent: FeedbackResponse["recent"] }
    let latestActivity: HomeLatestActivityItem[]
    let hasCreatedOffer = false
    let hasCreatedCampaign = false

    try {
      const [
        feedbackResult,
        latestActivityResult,
        offerPresence,
        campaignPresence,
      ] = await Promise.all([
        adapters.getFeedback(selectedLocationId),
        adapters.getHomeLatestActivity(selectedLocationId),
        adapters.hasCreatedOffer(selectedLocationId).catch(() => false),
        adapters.hasCreatedCampaign(selectedLocationId).catch(() => false),
      ])
      feedback = {
        total: feedbackResult.total,
        recent: feedbackResult.recent,
      }
      latestActivity = latestActivityResult.items
      hasCreatedOffer = offerPresence
      hasCreatedCampaign = campaignPresence
      await acks.load(selectedLocationId)
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      recommendation = idleRecommendation()
      weeklyBriefGeneration += 1
      weeklyBrief = emptyWeeklyBrief()
      dispatch({ type: "load_failed", generation })
      return
    }

    if (generation !== state.loadGeneration) {
      return
    }

    dispatch({
      type: "load_succeeded",
      generation,
      feedback,
      latestActivity,
      feedbackSubmitted: state.feedbackSubmitted,
      feedbackSubmittedPrevious: state.feedbackSubmittedPrevious,
      guestsJoined: state.guestsJoined,
      guestsJoinedPrevious: state.guestsJoinedPrevious,
      qrScans: state.qrScans,
      qrScansPrevious: state.qrScansPrevious,
      hasCreatedOffer,
      hasCreatedCampaign,
      viewModel: assembleCurrent(
        workspace,
        currentAcks(),
        feedback,
        latestActivity,
        state.feedbackSubmitted,
        state.guestsJoined,
        state.feedbackSubmittedPrevious,
        state.guestsJoinedPrevious,
        state.qrScans,
        state.qrScansPrevious,
        currentDateRangeLabel(),
        hasCreatedOffer,
        hasCreatedCampaign
      ),
    })

    await fetchPerformanceForSelectedLocation()

    if (thisRecommendationGeneration !== recommendationGeneration) {
      return
    }
    if (recommendationDismissedForSession) {
      return
    }

    try {
      const response = await adapters.loadHomeRecommendation({
        request: buildHomeRecommendationRequest({
          locationId: selectedLocationId,
          performanceDateRange,
          refresh: false,
        }),
      })
      if (thisRecommendationGeneration !== recommendationGeneration) {
        return
      }
      if (recommendationDismissedForSession) {
        return
      }
      patchRecommendation(mapRecommendationResponse(response), {
        softCacheKey: recommendationCacheKey,
      })
    } catch {
      if (thisRecommendationGeneration !== recommendationGeneration) {
        return
      }
      if (recommendationDismissedForSession) {
        return
      }
      patchRecommendation({
        status: "error",
        recommendation: null,
        isNone: false,
        errorMessage: HOME_RECOMMENDATION_LOAD_ERROR_MESSAGE,
        errorRetryable: true,
      })
    }
  }

  const refreshOpenFeedbackDetails = () => {
    const details = feedbackDetails.getSnapshot()
    // Do not kick the operator out of an in-progress correction (Cancel/Save exit).
    if (
      details.isOpen
      && details.feedbackId != null
      && !details.correction.isEditing
    ) {
      void feedbackDetails.retry()
    }
  }

  const handleClassificationTerminal = (
    signal: ClassificationTerminalSignal
  ) => {
    const selectedLocationId = state.workspace?.selectedLocationId
    if (
      selectedLocationId == null
      || signal.locationId !== selectedLocationId
    ) {
      return
    }

    void fetchPerformanceForSelectedLocation()
    void refreshFeedbackInBackground()

    const details = feedbackDetails.getSnapshot()
    if (
      details.isOpen
      && details.feedbackId === signal.feedbackId
      && !details.correction.isEditing
    ) {
      void feedbackDetails.retry()
    }
  }

  let realtimeSession: FeedbackHomeRealtimeSession | null = null
  let connectingRealtime = false

  const ensureRealtime = async () => {
    if (realtimeSession != null || connectingRealtime) {
      return
    }

    connectingRealtime = true
    try {
      realtimeSession = await adapters.connectRealtime({
        onClassificationTerminal: handleClassificationTerminal,
        onReconnected: () => {
          void fetchPerformanceForSelectedLocation()
          void refreshFeedbackInBackground()
          refreshOpenFeedbackDetails()
        },
      })
    } finally {
      connectingRealtime = false
    }
  }

  const disconnect = async () => {
    const session = realtimeSession
    realtimeSession = null
    if (session != null) {
      await session.stop()
    }
  }

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    connect: () => ensureRealtime(),
    disconnect,
    syncWorkspace: async (input) => {
      if (input.selectedLocationId == null) {
        recommendationDismissedForSession = false
        softCachedRecommendation = null
        recommendationGeneration += 1
        recommendation = idleRecommendation()
        weeklyBriefGeneration += 1
        weeklyBrief = emptyWeeklyBrief()
        dispatch({ type: "workspace_cleared" })
        acks.reset()
        feedbackDetails.reset()
        return
      }

      const previousLocationId = state.workspace?.selectedLocationId ?? null
      const locationChanged = previousLocationId !== input.selectedLocationId

      if (locationChanged) {
        recommendationDismissedForSession = false
        softCachedRecommendation = null
        recommendationGeneration += 1
        recommendation = idleRecommendation()
        weeklyBriefGeneration += 1
        weeklyBrief = emptyWeeklyBrief()
        feedbackDetails.reset()
        const emptyAcks: OperatorHomeChecklistAcks = {
          guestFormPreviewed: false,
          qrPlacementGuideViewed: false,
          logoUploaded: false,
        }
        const viewModel = assembleCurrent(
          input,
          emptyAcks,
          null,
          null,
          state.feedbackSubmitted,
          state.guestsJoined,
          state.feedbackSubmittedPrevious,
          state.guestsJoinedPrevious,
          state.qrScans,
          state.qrScansPrevious,
          currentDateRangeLabel()
        )
        dispatch({ type: "workspace_synced", workspace: input, viewModel })
        await loadForSelectedLocation()
        return
      }

      // Same Owned location: refresh shell-facing workspace fields only.
      dispatch({
        type: "workspace_fields_updated",
        workspace: input,
        viewModel: assembleCurrent(
          input,
          currentAcks(),
          state.feedback,
          state.latestActivity,
          state.feedbackSubmitted,
          state.guestsJoined,
          state.feedbackSubmittedPrevious,
          state.guestsJoinedPrevious,
          state.qrScans,
          state.qrScansPrevious,
          currentDateRangeLabel()
        ),
      })
    },
    retryLoad: () => loadForSelectedLocation(),
    reloadForHomePerformanceDateRange: async () => {
      const workspace = state.workspace
      if (workspace != null) {
        dispatch({
          type: "view_model_updated",
          viewModel: assembleCurrent(
            workspace,
            currentAcks(),
            state.feedback,
            state.latestActivity,
            state.feedbackSubmitted,
            state.guestsJoined,
            state.feedbackSubmittedPrevious,
            state.guestsJoinedPrevious,
            state.qrScans,
            state.qrScansPrevious,
            currentDateRangeLabel()
          ),
        })
      }
      await fetchPerformanceForSelectedLocation()
      // Window change may invalidate soft-cache key — reload without refresh.
      await loadRecommendation()
    },
    retryRecommendation: () => loadRecommendation({ refresh: true }),
    dismissRecommendation: () => {
      recommendationDismissedForSession = true
      softCachedRecommendation = null
      patchRecommendation({
        ...idleRecommendation(),
        status: "dismissed",
      })
    },
    retryWeeklyBrief: () => retryWeeklyBrief(),
    retryLiveOffers: () => fetchLiveOffersForSelectedLocation(),
    retryNeedsAttention: () => fetchNeedsAttentionForSelectedLocation(),
    pauseLiveCampaign: async (campaignId) => {
      const card = state.liveCards.find(
        (item) => item.kind === "campaign" && item.id === campaignId
      )
      if (card == null || card.kind !== "campaign" || state.liveOffersPauseBusy) {
        return false
      }

      dispatch({ type: "live_offers_pause_busy", busy: true })
      try {
        await adapters.pauseCampaign(campaignId, {
          rowVersion: card.rowVersion,
        })
        await fetchLiveOffersForSelectedLocation({ keepVisible: true })
        return true
      } catch (error) {
        const message =
          error instanceof Error && error.message.trim().length > 0
            ? error.message.trim()
            : "Could not pause this campaign. Please try again."
        dispatch({ type: "action_error", error: message })
        return false
      } finally {
        dispatch({ type: "live_offers_pause_busy", busy: false })
      }
    },
    duplicateNeedsAttentionCampaign: async (campaignId) => {
      const row = state.needsAttention?.allRows.find(
        (item) =>
          item.sourceKind === "campaign" && item.campaignId === campaignId
      )
      if (
        row == null
        || row.sourceKind !== "campaign"
        || needsAttentionDuplicateBusy
      ) {
        return { ok: false, error: NEEDS_ATTENTION_DUPLICATE_DRAFT_ERROR }
      }

      needsAttentionDuplicateBusy = true
      try {
        const response = await adapters.duplicateCampaign(campaignId, {
          rowVersion: row.rowVersion,
        })
        return { ok: true, campaignId: response.campaign.id }
      } catch (error) {
        const message =
          error instanceof Error && error.message.trim().length > 0
            ? error.message.trim()
            : NEEDS_ATTENTION_DUPLICATE_DRAFT_ERROR
        dispatch({ type: "action_error", error: message })
        return { ok: false, error: message }
      } finally {
        needsAttentionDuplicateBusy = false
      }
    },
    previewGuestForm: () => {
      const viewModel = state.viewModel
      if (
        viewModel?.smartGuestLink == null ||
        acks.getSnapshot().acknowledgeBusy
      ) {
        return
      }

      adapters.openSmartGuestLink(viewModel.smartGuestLink)
      acks.acknowledge("guestFormPreviewed")
    },
    copySmartGuestLink: async () => {
      const link = state.viewModel?.smartGuestLink
      if (link == null) {
        return "noop"
      }

      dispatch({ type: "action_error", error: null })
      const result = await adapters.copyText(link)
      if (!result.ok) {
        dispatch({ type: "action_error", error: result.error })
        return "failed"
      }

      return "copied"
    },
    openFeedbackDetails: (feedbackId) => {
      closeExclusiveAssistantDrawer()
      return feedbackDetails.open(feedbackId)
    },
    closeFeedbackDetails: () => {
      feedbackDetails.close()
    },
    retryFeedbackDetails: () => feedbackDetails.retry(),
    startClassificationCorrection: () => {
      feedbackDetails.startCorrection()
    },
    setClassificationDraftSentiment: (sentiment) => {
      feedbackDetails.setDraftSentiment(sentiment)
    },
    setClassificationDraftReason: (reason) => {
      feedbackDetails.setDraftReason(reason)
    },
    setClassificationDraftNote: (note) => {
      feedbackDetails.setDraftNote(note)
    },
    cancelClassificationCorrection: () => {
      feedbackDetails.cancelCorrection()
    },
    saveClassificationCorrection: async () => {
      const before = feedbackDetails.getSnapshot()
      const feedbackId = before.feedbackId
      await feedbackDetails.saveCorrection()
      const after = feedbackDetails.getSnapshot()
      const nextSentiment = after.details?.sentiment
      if (
        feedbackId == null
        || nextSentiment == null
        || after.correction.isEditing
        || state.feedback == null
        || state.workspace == null
      ) {
        return
      }

      const recent = state.feedback.recent.map((item) =>
        item.id === feedbackId
          ? {
              ...item,
              classificationStatus: "Succeeded" as const,
              sentiment: nextSentiment,
            }
          : item
      )
      const feedback = { total: state.feedback.total, recent }
      const latestActivity =
        state.latestActivity?.map((item) =>
          item.kind === "feedback" && item.id === feedbackId
            ? {
                ...item,
                classificationStatus: "Succeeded" as const,
                sentiment: nextSentiment,
              }
            : item
        ) ?? null
      dispatch({
        type: "feedback_patched",
        feedback,
        latestActivity,
        viewModel: assembleCurrent(
          state.workspace,
          currentAcks(),
          feedback,
          latestActivity,
          state.feedbackSubmitted,
          state.guestsJoined,
          state.feedbackSubmittedPrevious,
          state.guestsJoinedPrevious,
          state.qrScans,
          state.qrScansPrevious,
          currentDateRangeLabel()
        ),
      })
    },
    startEditTags: () => {
      feedbackDetails.startEditTags()
    },
    stageEditTag: (key) => {
      feedbackDetails.stageTag(key)
    },
    unstageEditTag: (key) => {
      feedbackDetails.unstageTag(key)
    },
    setEditTagsSentiment: (sentiment) => {
      feedbackDetails.setEditTagsSentiment(sentiment)
    },
    cancelEditTags: () => {
      feedbackDetails.cancelEditTags()
    },
    applyEditTags: () => feedbackDetails.applyEditTags(),
    setFeedbackWorkflowStatus: (status) =>
      feedbackDetails.setWorkflowStatus(status),
    reopenFeedback: () => feedbackDetails.reopen(),
    startFeedbackMarkNoActionNeeded: () => feedbackDetails.startMarkNoActionNeeded(),
    startFeedbackMarkResolved: () => feedbackDetails.startMarkResolved(),
    setFeedbackCloseOutReason: (reason) =>
      feedbackDetails.setCloseOutReason(reason),
    setFeedbackCloseOutNoteDraft: (value) =>
      feedbackDetails.setCloseOutNoteDraft(value),
    setFeedbackCloseOutAcknowledged: (value) =>
      feedbackDetails.setCloseOutAcknowledged(value),
    cancelFeedbackCloseOut: () => feedbackDetails.cancelCloseOut(),
    confirmFeedbackCloseOut: () => feedbackDetails.confirmCloseOut(),
    setFeedbackInternalNoteDraft: (value) => {
      feedbackDetails.setNoteDraft(value)
    },
    createFeedbackInternalNote: () => feedbackDetails.createNote(),
    startFeedbackNoteEdit: (noteId) => {
      feedbackDetails.startEditNote(noteId)
    },
    setFeedbackNoteEditDraft: (value) => {
      feedbackDetails.setNoteEditDraft(value)
    },
    cancelFeedbackNoteEdit: () => {
      feedbackDetails.cancelEditNote()
    },
    saveFeedbackNoteEdit: () => feedbackDetails.saveEditNote(),
    startFeedbackNoteDelete: (noteId) => {
      feedbackDetails.startDeleteNote(noteId)
    },
    cancelFeedbackNoteDelete: () => {
      feedbackDetails.cancelDeleteNote()
    },
    confirmFeedbackNoteDelete: () => feedbackDetails.confirmDeleteNote(),
  }
}

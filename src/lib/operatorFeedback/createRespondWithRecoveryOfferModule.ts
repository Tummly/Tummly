import type {
  ClassificationStatus,
  ContactType,
  FeedbackDetailsResponse,
  FeedbackSentiment,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import {
  deriveStartRecoveryContactCapability,
  type StartRecoveryContactCapability,
} from "@/lib/operatorFeedback/startRecoveryPresentation"
import { mapResponseSetupSummaryChrome } from "@/lib/operatorFeedback/responseSetupPresentation"
import {
  canContinueRespondToGuestMessage,
  defaultRespondToGuestChannel,
  labelForRespondToGuestTone,
  maskRespondToGuestDestination,
  availableRespondToGuestChannels,
  type RespondToGuestChannel,
  type RespondToGuestToneId,
  type RespondToGuestWriteEntry,
} from "@/lib/operatorFeedback/respondToGuestPresentation"
import {
  buildGuestPreviewOfferCoupon,
  buildGuestPreviewSendTestDialog,
  emptyGuestPreviewSendTestSession,
  canOpenGuestPreviewSendTest,
  GUEST_PREVIEW_SEND_TEST_ERROR,
  type GuestPreviewSendTestDialogViewModel,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import type {
  CompleteRecoveryResult,
  PrepareRecoveryDraftMode,
  PrepareRecoveryDraftRewriteTarget,
  PrepareRecoveryDraftResult,
} from "@/lib/operatorFeedback/createRespondToGuestModule"
import { isPrepareRecoveryDraftRewriteMode } from "@/lib/operatorFeedback/createRespondToGuestModule"
import {
  RECOVERY_OFFER_DESCRIPTION_MAX,
  RECOVERY_OFFER_PURPOSE_ID,
  RECOVERY_OFFER_PURPOSE_LABEL,
  RECOVERY_OFFER_STANCE_OPTIONS,
  RECOVERY_EXISTING_OFFER_PICKER_COPY,
  RECOVERY_OFFER_STEP_COPY,
  RECOVERY_OFFER_TITLE_MAX,
  autoTitleForRecoveryOffer,
  canContinueRecoveryOfferAttach,
  canContinueRespondWithRecoveryOfferSetup,
  emptyRespondWithRecoveryOfferDraft,
  furthestRespondWithRecoveryOfferStep,
  labelForRecoveryOfferType,
  toConfirmedRecoveryOfferPayload,
  type ConfirmedRecoveryOfferPayload,
  type RecoveryOfferDetailsDraft,
  type RecoveryOfferPurchaseRequirementId,
  type RecoveryOfferStanceId,
  type RecoveryOfferStanceOptionViewModel,
  type RecoveryOfferTypeId,
  type RecoveryOfferValidityId,
  type RespondWithRecoveryOfferDraft,
  type RespondWithRecoveryOfferWizardStep,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"
import {
  canConfirmCampaignCatalogOfferDetails,
  catalogOfferDetailToDraft,
  emptyCampaignCatalogOfferDetailsDraft,
  mergeCampaignCatalogOfferDraftPatch,
  toCreateCatalogOfferRequestBody,
  type CampaignCatalogOfferDetailsDraft,
  type CreateCatalogOfferRequestBody,
} from "@/lib/operatorOffers/offerCatalogPresentation"
import type {
  ConfirmCatalogOfferWriteResult,
  CreateEditOfferDrawerMode,
} from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import type {
  CatalogOfferDetail,
  CatalogOffersListItem,
  CatalogOffersListQueryParams,
  CatalogOffersListResponse,
} from "@/types/operatorCampaigns"
import {
  CAMPAIGN_EXISTING_OFFER_PICKER_COPY,
  filterExistingOfferPickerItems,
  mapCatalogOfferToExistingPickerCard,
  type CampaignExistingOfferPickerCard,
} from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"
import { emptySelection } from "@/lib/operatorFilterSheet"
import {
  ATTACHABLE_OFFER_STATUS_IDS,
  isAttachableCatalogOfferStatus,
  offersFilterSheetSchema,
} from "@/lib/operatorOffers/offersFilterSheetSchema"
import { buildOffersListQueryParams } from "@/lib/operatorOffers/offersListQueryParams"
import {
  OPERATOR_OFFERS_DEFAULT_SORT_ID,
  OFFERS_PAGE_SIZE,
} from "@/lib/operatorOffers/offersPresentation"

const SEND_ERROR_MESSAGE =
  "Could not send the response and issue the offer. Please try again."
const COMPLETE_ERROR_MESSAGE =
  "Could not mark this recovery resolved. Please try again."
const AI_DRAFT_ERROR_MESSAGE = "We could not prepare a draft."
const OFFER_DESCRIPTION_AI_ERROR_MESSAGE =
  "We could not prepare an offer description."

const EXISTING_OFFER_PICKER_PAGE_SIZE = Math.max(OFFERS_PAGE_SIZE, 100)
const OFFERS_FILTER_SCHEMA = offersFilterSheetSchema()

export type RecoveryExistingOfferPickerLoadStatus =
  | "idle"
  | "loading"
  | "ready"
  | "error"

export type RecoveryExistingOfferPickerViewModel = {
  visible: boolean
  loadStatus: RecoveryExistingOfferPickerLoadStatus
  searchQuery: string
  searchPlaceholder: string
  error: string | null
  retryLabel: string
  cards: CampaignExistingOfferPickerCard[]
  isEmpty: boolean
  emptyHelper: string | null
  createNewOfferLabel: string | null
  selectLabel: string
  viewDetailsLabel: string
  viewDetailsEnabled: boolean
}

export type RecoveryOfferIssuedActivityEvent = {
  kind: "recovery_offer_issued"
  at: string
  actorDisplayName: string | null
  offerType: RecoveryOfferTypeId
  title: string
  validity: RecoveryOfferValidityId
  expiryAt: string | null
  redemptionCode: string
}

export type SendAndIssueRecoveryOfferRequest = {
  feedbackId: number
  channel: RespondToGuestChannel
  subject: string | null
  body: string
  intent: "respond_with_recovery_offer"
  purpose: typeof RECOVERY_OFFER_PURPOSE_ID
  tone: RespondToGuestToneId
  includeNotes: string | null
  offer: ConfirmedRecoveryOfferPayload
}

export type SendAndIssueRecoveryOfferResult = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  guestResponseActivityEvent: {
    kind: "guest_response_sent"
    at: string
    actorDisplayName: string | null
    channel: RespondToGuestChannel
    maskedDestination: string
  }
  recoveryOfferActivityEvent: RecoveryOfferIssuedActivityEvent
  issuedOffer: {
    title: string
    redemptionCode: string
    expiryAt: string | null
    validity: RecoveryOfferValidityId
  }
}

export type PrepareRecoveryOfferDraftRequest = {
  feedbackId: number
  channel: RespondToGuestChannel
  purpose: typeof RECOVERY_OFFER_PURPOSE_ID
  tone: RespondToGuestToneId
  includeNotes: string | null
  mode: PrepareRecoveryDraftMode
  currentBody: string | null
  currentSubject: string | null
  confirmedOffer: ConfirmedRecoveryOfferPayload | null
}

export type RespondWithRecoveryOfferAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  getRecoveryOfferAttach: (feedbackId: number) => Promise<number | null>
  setRecoveryOfferAttach: (
    feedbackId: number,
    offerId: number | null
  ) => Promise<void>
  /** Host workspace location for catalog create / Existing picker. */
  getLocationId?: () => number | null
  /** List Active catalog offers for Existing attach (ticket 03). */
  listCatalogOffers?: (
    params: CatalogOffersListQueryParams
  ) => Promise<CatalogOffersListResponse>
  /** Create Active Offers catalog definition (ticket 04). */
  createOffer?: (
    body: CreateCatalogOfferRequestBody
  ) => Promise<CatalogOfferDetail>
  /** Load catalog definition for Edit attached offer. */
  getOffer?: (offerId: number) => Promise<CatalogOfferDetail>
  /** Update catalog definition on Edit. */
  updateOffer?: (
    offerId: number,
    body: CreateCatalogOfferRequestBody
  ) => Promise<CatalogOfferDetail>
  sendAndIssueRecoveryOffer: (
    request: SendAndIssueRecoveryOfferRequest
  ) => Promise<SendAndIssueRecoveryOfferResult>
  sendGuestPreviewTest: (request: {
    feedbackId: number
    subject: string
    body: string
    toEmail: string
    offer?: {
      title: string
      description: string
      expiryLabel: string
    } | null
  }) => Promise<void>
  getOperatorAccountEmail?: () => string | null | Promise<string | null>
  completeRecovery: (
    feedbackId: number,
    intent:
      | "respond_to_guest"
      | "record_internal_action_only"
      | "respond_with_recovery_offer"
  ) => Promise<CompleteRecoveryResult>
  prepareRecoveryDraft: (
    request: PrepareRecoveryOfferDraftRequest,
    signal?: AbortSignal
  ) => Promise<PrepareRecoveryDraftResult>
}

export type RespondWithRecoveryOfferSummary = {
  guestName: string
  contactCapability: StartRecoveryContactCapability
  feedbackComment: string
  locationName: string
  classificationStatus: ClassificationStatus
  classificationSentiment: FeedbackSentiment | null
  contactLabel: string
  issueTagLabels: string[] | null
  purposeLabel: string
  toneLabel: string | null
  offerTitle: string | null
  offerTypeLabel: string | null
}

export type RespondWithRecoveryOfferAiDraftStatus = "idle" | "running" | "failed"

export type RespondWithRecoveryOfferSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  feedbackId: number | null
  step: RespondWithRecoveryOfferWizardStep
  headerSubtitle: string | null
  summary: RespondWithRecoveryOfferSummary | null
  availableChannels: RespondToGuestChannel[]
  channel: RespondToGuestChannel | null
  purpose: typeof RECOVERY_OFFER_PURPOSE_ID
  purposeLabel: typeof RECOVERY_OFFER_PURPOSE_LABEL
  tone: RespondToGuestToneId | null
  includeNotes: string
  offer: RecoveryOfferDetailsDraft
  /** Durable catalog Recovery offer attach. */
  offerId: number | null
  offerStanceId: RecoveryOfferStanceId | null
  offerStanceOptions: RecoveryOfferStanceOptionViewModel[]
  attachedOfferTitle: string | null
  attachedOfferStatus: CatalogOfferDetail["status"] | null
  locationId: number | null
  existingOfferPicker: RecoveryExistingOfferPickerViewModel | null
  createPanelOpen: boolean
  createOfferDrawerMode: CreateEditOfferDrawerMode
  createOfferDraft: CampaignCatalogOfferDetailsDraft
  createOfferStatus: "idle" | "saving" | "error"
  createOfferError: string | null
  canConfirmCreateOffer: boolean
  locationSubtitle: string
  canContinueSetup: boolean
  canContinueOffer: boolean
  subject: string
  message: string
  maskedDestination: string | null
  canContinueWrite: boolean
  writeEntry: RespondToGuestWriteEntry
  aiDraftStatus: RespondWithRecoveryOfferAiDraftStatus
  aiDraftMode: PrepareRecoveryDraftMode | null
  preparingOverlayOpen: boolean
  actionsLocked: boolean
  aiDraftError: string | null
  aiDraftRetryable: boolean
  offerDescriptionAiStatus: RespondWithRecoveryOfferAiDraftStatus
  offerDescriptionAiError: string | null
  /** Successful AI prepare/rewrite/offer-description count for this open session. */
  aiActionCount: number
  /** Location chrome for Guest preview — from Feedback details. */
  locationName: string | null
  locationAddress: string | null
  /** Full-screen Guest preview overlay open on Review. */
  guestPreviewOpen: boolean
  sendConfirmOpen: boolean
  sendStatus: "idle" | "saving" | "error"
  sendError: string | null
  sendTestStatus: "idle" | "sending" | "success" | "error"
  sendTestError: string | null
  sendTest: GuestPreviewSendTestDialogViewModel | null
  completeStatus: "idle" | "saving" | "error"
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  issuedOffer: SendAndIssueRecoveryOfferResult["issuedOffer"] | null
  openedFromDraftAction: boolean
}

export type RespondWithRecoveryOfferBackResult = "return-to-shell" | "stayed"

export type RespondWithRecoveryOfferModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => RespondWithRecoveryOfferSnapshot
  open: (
    feedbackId: number,
    preloadedDetails?: FeedbackDetailsResponse
  ) => Promise<void>
  openFromDraftAction: (input: {
    feedbackId: number
    channel: RespondToGuestChannel
    tone: RespondToGuestToneId
    includeNotes: string
    subject: string
    message: string
    offerId: number
  }) => Promise<void>
  saveAndExit: () => void
  close: () => void
  back: () => RespondWithRecoveryOfferBackResult
  setChannel: (channel: RespondToGuestChannel) => void
  setTone: (tone: RespondToGuestToneId) => void
  setIncludeNotes: (value: string) => void
  continueSetup: () => void
  /** Set or clear durable catalog Recovery offer attach (persists immediately). */
  setOfferId: (offerId: number | null) => Promise<void>
  setOfferStanceId: (stanceId: RecoveryOfferStanceId) => void
  setExistingOfferSearch: (query: string) => void
  selectExistingOffer: (offerId: number) => void
  retryExistingOfferPicker: () => Promise<void>
  openCreateOfferPanel: () => void
  closeCreateOfferPanel: () => void
  patchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  confirmCreateOffer: () => Promise<ConfirmCatalogOfferWriteResult>
  editAttachedOffer: () => Promise<void>
  setOfferType: (offerType: RecoveryOfferTypeId) => void
  setDiscountPercentage: (value: string) => void
  setDiscountAmount: (value: string) => void
  setFreeItemText: (value: string) => void
  setPurchaseRequirement: (
    value: RecoveryOfferPurchaseRequirementId
  ) => void
  setMinimumSpend: (value: string) => void
  setAdditionalExclusions: (value: string) => void
  setReplacementItemText: (value: string) => void
  setOfferTitle: (value: string) => void
  setOfferDescription: (value: string) => void
  setOfferValidity: (value: RecoveryOfferValidityId) => void
  setExpiryDate: (value: string) => void
  setStaffInstructions: (value: string) => void
  prepareOfferDescription: () => Promise<void>
  continueOffer: () => void
  editOffer: () => void
  writeManually: () => void
  prepareDraft: () => Promise<void>
  rewriteDraft: (target: PrepareRecoveryDraftRewriteTarget) => Promise<void>
  retryAiDraft: () => Promise<void>
  dismissPreparingOverlay: () => void
  setSubject: (value: string) => void
  setMessage: (value: string) => void
  continueWrite: () => void
  /** Review → Guest response editor (no in-place edit on Review). */
  editText: () => void
  openGuestPreview: () => void
  closeGuestPreview: () => void
  openSendTestDialog: () => Promise<void>
  closeSendTestDialog: () => void
  setSendTestEmail: (value: string) => void
  confirmSendTest: () => Promise<void>
  openSendConfirm: () => void
  cancelSendConfirm: () => void
  confirmSend: () => Promise<void>
  keepInProgress: () => void
  markResolved: () => Promise<void>
}

type SessionState = {
  isOpen: boolean
  loadStatus: RespondWithRecoveryOfferSnapshot["loadStatus"]
  loadError: string | null
  loadGeneration: number
  feedbackId: number | null
  locationId: number | null
  step: RespondWithRecoveryOfferWizardStep
  headerSubtitle: string | null
  summary: RespondWithRecoveryOfferSummary | null
  contactType: ContactType | null
  guestContact: string
  contactCapability: StartRecoveryContactCapability | null
  availableChannels: RespondToGuestChannel[]
  draft: RespondWithRecoveryOfferDraft
  offerStanceId: RecoveryOfferStanceId | null
  attachedOfferTitle: string | null
  /** Catalog status for the attached Offer — Continue needs Active. */
  attachedOfferStatus: CatalogOfferDetail["status"] | null
  existingOfferPickerVisible: boolean
  existingOfferPickerLoadStatus: RecoveryExistingOfferPickerLoadStatus
  existingOfferPickerError: string | null
  existingOfferPickerSearchQuery: string
  existingOfferPickerItems: CatalogOffersListItem[]
  createOfferPanelOpen: boolean
  createOfferDrawerMode: CreateEditOfferDrawerMode
  createOfferDraft: CampaignCatalogOfferDetailsDraft
  createOfferStatus: "idle" | "saving" | "error"
  createOfferError: string | null
  maskedDestination: string | null
  aiDraftStatus: RespondWithRecoveryOfferAiDraftStatus
  aiDraftMode: PrepareRecoveryDraftMode | null
  preparingOverlayOpen: boolean
  aiDraftError: string | null
  aiDraftRetryable: boolean
  aiDraftGeneration: number
  offerDescriptionAiStatus: RespondWithRecoveryOfferAiDraftStatus
  offerDescriptionAiError: string | null
  aiActionCount: number
  locationName: string | null
  locationAddress: string | null
  guestPreviewOpen: boolean
  sendConfirmOpen: boolean
  sendStatus: RespondWithRecoveryOfferSnapshot["sendStatus"]
  sendError: string | null
  sendTestDialogOpen: boolean
  sendTestEmail: string
  sendTestStatus: RespondWithRecoveryOfferSnapshot["sendTestStatus"]
  sendTestError: string | null
  completeStatus: RespondWithRecoveryOfferSnapshot["completeStatus"]
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  issuedOffer: SendAndIssueRecoveryOfferResult["issuedOffer"] | null
  openedFromDraftAction: boolean
}

function emptySession(): SessionState {
  return {
    isOpen: false,
    loadStatus: "idle",
    loadError: null,
    loadGeneration: 0,
    feedbackId: null,
    locationId: null,
    step: "setup",
    headerSubtitle: null,
    summary: null,
    contactType: null,
    guestContact: "",
    contactCapability: null,
    availableChannels: [],
    draft: emptyRespondWithRecoveryOfferDraft(),
    offerStanceId: null,
    attachedOfferTitle: null,
    attachedOfferStatus: null,
    existingOfferPickerVisible: false,
    existingOfferPickerLoadStatus: "idle",
    existingOfferPickerError: null,
    existingOfferPickerSearchQuery: "",
    existingOfferPickerItems: [],
    createOfferPanelOpen: false,
    createOfferDrawerMode: "create",
    createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
    createOfferStatus: "idle",
    createOfferError: null,
    maskedDestination: null,
    aiDraftStatus: "idle",
    aiDraftMode: null,
    preparingOverlayOpen: false,
    aiDraftError: null,
    aiDraftRetryable: true,
    aiDraftGeneration: 0,
    offerDescriptionAiStatus: "idle",
    offerDescriptionAiError: null,
    aiActionCount: 0,
    locationName: null,
    locationAddress: null,
    guestPreviewOpen: false,
    sendConfirmOpen: false,
    sendStatus: "idle",
    sendError: null,
    ...emptyGuestPreviewSendTestSession(),
    completeStatus: "idle",
    completeError: null,
    workflowStatus: null,
    issuedOffer: null,
    openedFromDraftAction: false,
  }
}

function parseWorkflowStatus(
  value: FeedbackWorkflowStatus | undefined
): FeedbackWorkflowStatus {
  if (value === "in_progress" || value === "resolved") {
    return value
  }
  return "new"
}

function buildHeaderSubtitle(
  feedbackId: number,
  locationName: string,
  qrSource: string | null | undefined
): string {
  const reference = `FDB-${String(feedbackId).padStart(6, "0")}`
  const location = locationName.trim() || "Location"
  const touchpoint = qrSource?.trim() || "QR"
  return `${reference} · ${location} · ${touchpoint}`
}

function withAutoTitle(
  offer: RecoveryOfferDetailsDraft
): RecoveryOfferDetailsDraft {
  if (offer.titleTouched) {
    return offer
  }
  return {
    ...offer,
    title: autoTitleForRecoveryOffer(offer),
  }
}

function cloneDraft(
  draft: RespondWithRecoveryOfferDraft
): RespondWithRecoveryOfferDraft {
  return {
    ...draft,
    offer: { ...draft.offer },
  }
}

function projectSummary(
  state: SessionState
): RespondWithRecoveryOfferSummary | null {
  if (state.summary == null || state.contactCapability == null) {
    return state.summary
  }
  return {
    ...state.summary,
    purposeLabel: RECOVERY_OFFER_PURPOSE_LABEL,
    toneLabel: labelForRespondToGuestTone(state.draft.tone),
    offerTitle:
      state.attachedOfferTitle?.trim()
      || state.draft.offer.title.trim()
      || null,
    offerTypeLabel: labelForRecoveryOfferType(state.draft.offer.offerType),
  }
}

function catalogDetailToRecoveryOfferDraft(
  offer: CatalogOfferDetail
): RecoveryOfferDetailsDraft {
  const catalogDraft = catalogOfferDetailToDraft(offer)
  return {
    offerType: catalogDraft.offerType,
    discountPercentage: catalogDraft.discountPercentage,
    discountAmount: catalogDraft.discountAmount,
    freeItemText: catalogDraft.freeItemText,
    purchaseRequirement: catalogDraft.purchaseRequirement,
    minimumSpend: catalogDraft.minimumSpend,
    additionalExclusions: catalogDraft.additionalExclusions,
    replacementItemText: catalogDraft.replacementItemText,
    title: catalogDraft.title,
    titleTouched: true,
    description: catalogDraft.description,
    validity: catalogDraft.validity,
    expiryDate: catalogDraft.expiryDate,
    staffInstructions: catalogDraft.staffInstructions,
    offerComplete: false,
  }
}

function clearExistingOfferPicker(state: SessionState): SessionState {
  return {
    ...state,
    existingOfferPickerVisible: false,
    existingOfferPickerLoadStatus: "idle",
    existingOfferPickerError: null,
    existingOfferPickerSearchQuery: "",
    existingOfferPickerItems: [],
  }
}

function buildExistingOfferPickerQueryParams(
  locationId: number
): CatalogOffersListQueryParams {
  const filters = emptySelection(OFFERS_FILTER_SCHEMA)
  filters.status = {
    kind: "multi-select",
    ids: [...ATTACHABLE_OFFER_STATUS_IDS],
  }
  return buildOffersListQueryParams({
    locationId,
    view: "all",
    q: "",
    sort: OPERATOR_OFFERS_DEFAULT_SORT_ID,
    page: 1,
    pageSize: EXISTING_OFFER_PICKER_PAGE_SIZE,
    filters,
  })
}

function buildExistingOfferPickerViewModel(
  state: SessionState
): RecoveryExistingOfferPickerViewModel | null {
  if (
    state.step !== "offer"
    || state.offerStanceId !== "existing-offer"
    || !state.existingOfferPickerVisible
  ) {
    return null
  }

  const filtered = filterExistingOfferPickerItems(
    state.existingOfferPickerItems,
    state.existingOfferPickerSearchQuery
  )
  const cards = filtered.map(mapCatalogOfferToExistingPickerCard)
  const catalogEmpty =
    state.existingOfferPickerLoadStatus === "ready"
    && state.existingOfferPickerItems.length === 0
  const searchMiss =
    state.existingOfferPickerLoadStatus === "ready"
    && state.existingOfferPickerItems.length > 0
    && cards.length === 0

  return {
    visible: true,
    loadStatus: state.existingOfferPickerLoadStatus,
    searchQuery: state.existingOfferPickerSearchQuery,
    searchPlaceholder: RECOVERY_EXISTING_OFFER_PICKER_COPY.searchPlaceholder,
    error:
      state.existingOfferPickerLoadStatus === "error"
        ? (state.existingOfferPickerError
          ?? CAMPAIGN_EXISTING_OFFER_PICKER_COPY.loadError)
        : null,
    retryLabel: RECOVERY_EXISTING_OFFER_PICKER_COPY.retryLabel,
    cards,
    isEmpty: catalogEmpty || searchMiss,
    emptyHelper: catalogEmpty
      ? RECOVERY_EXISTING_OFFER_PICKER_COPY.emptyHelper
      : searchMiss
        ? RECOVERY_EXISTING_OFFER_PICKER_COPY.searchMissHelper
        : null,
    createNewOfferLabel: null,
    selectLabel: RECOVERY_EXISTING_OFFER_PICKER_COPY.selectLabel,
    viewDetailsLabel: RECOVERY_EXISTING_OFFER_PICKER_COPY.viewDetailsLabel,
    viewDetailsEnabled: RECOVERY_EXISTING_OFFER_PICKER_COPY.viewDetailsEnabled,
  }
}

function toSnapshot(state: SessionState): RespondWithRecoveryOfferSnapshot {
  const draft = state.draft
  const actionsLocked =
    state.aiDraftStatus === "running"
    || state.offerDescriptionAiStatus === "running"
  return {
    isOpen: state.isOpen,
    loadStatus: state.loadStatus,
    loadError: state.loadError,
    feedbackId: state.feedbackId,
    step: state.step,
    headerSubtitle: state.headerSubtitle,
    summary: projectSummary(state),
    availableChannels: state.availableChannels,
    channel: draft.channel,
    purpose: RECOVERY_OFFER_PURPOSE_ID,
    purposeLabel: RECOVERY_OFFER_PURPOSE_LABEL,
    tone: draft.tone,
    includeNotes: draft.includeNotes,
    offer: draft.offer,
    offerId: draft.offerId,
    offerStanceId: state.offerStanceId,
    offerStanceOptions: RECOVERY_OFFER_STANCE_OPTIONS.map((option) => ({
      ...option,
      selected: state.offerStanceId === option.id,
    })),
    attachedOfferTitle: state.attachedOfferTitle,
    attachedOfferStatus: state.attachedOfferStatus,
    locationId: state.locationId,
    existingOfferPicker: buildExistingOfferPickerViewModel(state),
    createPanelOpen: state.createOfferPanelOpen,
    createOfferDrawerMode: state.createOfferDrawerMode,
    createOfferDraft: state.createOfferDraft,
    createOfferStatus: state.createOfferStatus,
    createOfferError: state.createOfferError,
    canConfirmCreateOffer: canConfirmCampaignCatalogOfferDetails(
      state.createOfferDraft
    ),
    locationSubtitle: state.locationName ?? "",
    canContinueSetup: canContinueRespondWithRecoveryOfferSetup(draft),
    canContinueOffer: canContinueRecoveryOfferAttach({
      offerId: draft.offerId,
      attachedOfferStatus: state.attachedOfferStatus,
    }),
    subject: draft.subject,
    message: draft.message,
    maskedDestination: state.maskedDestination,
    canContinueWrite:
      draft.writeEntry === "editor"
      && !actionsLocked
      && canContinueRespondToGuestMessage({
        channel: draft.channel,
        subject: draft.subject,
        message: draft.message,
      }),
    writeEntry: draft.writeEntry,
    aiDraftStatus: state.aiDraftStatus,
    aiDraftMode: state.aiDraftMode,
    preparingOverlayOpen: state.preparingOverlayOpen,
    actionsLocked,
    aiDraftError: state.aiDraftError,
    aiDraftRetryable: state.aiDraftRetryable,
    offerDescriptionAiStatus: state.offerDescriptionAiStatus,
    offerDescriptionAiError: state.offerDescriptionAiError,
    aiActionCount: state.aiActionCount,
    locationName: state.locationName,
    locationAddress: state.locationAddress,
    guestPreviewOpen: state.guestPreviewOpen,
    sendConfirmOpen: state.sendConfirmOpen,
    sendStatus: state.sendStatus,
    sendError: state.sendError,
    sendTestStatus: state.sendTestStatus,
    sendTestError: state.sendTestError,
    sendTest: buildGuestPreviewSendTestDialog({
      wizardOpen: state.isOpen,
      channel: draft.channel,
      dialogOpen: state.sendTestDialogOpen,
      email: state.sendTestEmail,
      status: state.sendTestStatus,
      error: state.sendTestError,
    }),
    completeStatus: state.completeStatus,
    completeError: state.completeError,
    workflowStatus: state.workflowStatus,
    issuedOffer: state.issuedOffer,
    openedFromDraftAction: state.openedFromDraftAction,
  }
}

/**
 * Respond with a recovery offer — setup → offer → Guest response →
 * review → Send and issue offer → success. Intent-scoped drafts.
 */
export function createRespondWithRecoveryOfferModule(
  adapters: RespondWithRecoveryOfferAdapters
): RespondWithRecoveryOfferModule {
  let state = emptySession()
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()
  const draftsByFeedbackId = new Map<number, RespondWithRecoveryOfferDraft>()
  let aiAbortController: AbortController | null = null
  let existingOfferPickerLoadGeneration = 0

  const publish = () => {
    snapshot = toSnapshot(state)
    for (const listener of listeners) {
      listener()
    }
  }

  const loadExistingOfferPicker = async () => {
    if (
      !state.isOpen
      || state.step !== "offer"
      || state.offerStanceId !== "existing-offer"
      || !state.existingOfferPickerVisible
      || state.locationId == null
      || adapters.listCatalogOffers == null
    ) {
      if (
        state.existingOfferPickerVisible
        && state.offerStanceId === "existing-offer"
        && (state.locationId == null || adapters.listCatalogOffers == null)
      ) {
        state = {
          ...state,
          existingOfferPickerLoadStatus: "error",
          existingOfferPickerError: RECOVERY_EXISTING_OFFER_PICKER_COPY.loadError,
          existingOfferPickerItems: [],
        }
        publish()
      }
      return
    }

    const locationId = state.locationId
    const generation = ++existingOfferPickerLoadGeneration
    state = {
      ...state,
      existingOfferPickerLoadStatus: "loading",
      existingOfferPickerError: null,
    }
    publish()

    try {
      const response = await adapters.listCatalogOffers(
        buildExistingOfferPickerQueryParams(locationId)
      )
      if (generation !== existingOfferPickerLoadGeneration) {
        return
      }
      if (!state.isOpen || state.step !== "offer") {
        return
      }
      const items = (response.items ?? []).filter(
        (item) => isAttachableCatalogOfferStatus(item.status)
      )
      state = {
        ...state,
        existingOfferPickerLoadStatus: "ready",
        existingOfferPickerError: null,
        existingOfferPickerItems: items,
      }
      publish()
    } catch {
      if (generation !== existingOfferPickerLoadGeneration) {
        return
      }
      state = {
        ...state,
        existingOfferPickerLoadStatus: "error",
        existingOfferPickerError: RECOVERY_EXISTING_OFFER_PICKER_COPY.loadError,
        existingOfferPickerItems: [],
      }
      publish()
    }
  }

  const persistDraftIfComposable = () => {
    if (state.feedbackId == null || state.step === "success") {
      return
    }
    draftsByFeedbackId.set(state.feedbackId, cloneDraft(state.draft))
  }

  const closeSession = () => {
    if (aiAbortController != null) {
      aiAbortController.abort()
      aiAbortController = null
    }
    state = {
      ...emptySession(),
      loadGeneration: state.loadGeneration,
      aiDraftGeneration: state.aiDraftGeneration,
    }
    publish()
  }

  const applyDraftDefaults = (
    response: FeedbackDetailsResponse,
    existing: RespondWithRecoveryOfferDraft | undefined
  ): RespondWithRecoveryOfferDraft => {
    const capability = deriveStartRecoveryContactCapability(
      response.contactType,
      response.guestContact
    )
    if (existing != null) {
      return cloneDraft(existing)
    }
    const draft = emptyRespondWithRecoveryOfferDraft()
    draft.channel = defaultRespondToGuestChannel(capability)
    return draft
  }

  const clearAiDraftUi = () => {
    state = {
      ...state,
      aiDraftStatus: "idle",
      aiDraftMode: null,
      preparingOverlayOpen: false,
      aiDraftError: null,
      aiDraftRetryable: true,
    }
  }

  const patchOffer = (
    patch: Partial<RecoveryOfferDetailsDraft>,
    options?: { autoTitle?: boolean }
  ) => {
    if (state.step !== "offer") {
      return
    }
    let offer: RecoveryOfferDetailsDraft = {
      ...state.draft.offer,
      ...patch,
      offerComplete: false,
    }
    if (options?.autoTitle !== false) {
      offer = withAutoTitle(offer)
    }
    state = {
      ...state,
      draft: { ...state.draft, offer },
    }
    publish()
  }

  const runAiDraft = async (mode: PrepareRecoveryDraftMode) => {
    if (
      state.step !== "write"
      || state.feedbackId == null
      || state.draft.channel == null
      || state.draft.tone == null
      || state.aiDraftStatus === "running"
    ) {
      return
    }

    const confirmedOffer = toConfirmedRecoveryOfferPayload(state.draft.offer)
    if (confirmedOffer == null) {
      return
    }

    const feedbackId = state.feedbackId
    const channel = state.draft.channel
    const tone = state.draft.tone
    const includeNotes =
      state.draft.includeNotes.trim() === ""
        ? null
        : state.draft.includeNotes.trim()
    const priorSubject = state.draft.subject
    const priorMessage = state.draft.message
    const generation = ++state.aiDraftGeneration

    if (aiAbortController != null) {
      aiAbortController.abort()
    }
    const controller = new AbortController()
    aiAbortController = controller

    const isRewrite = isPrepareRecoveryDraftRewriteMode(mode)

    state = {
      ...state,
      aiDraftStatus: "running",
      aiDraftMode: mode,
      preparingOverlayOpen: mode === "prepare",
      aiDraftError: null,
      aiDraftRetryable: true,
    }
    publish()

    const request: PrepareRecoveryOfferDraftRequest = {
      feedbackId,
      channel,
      purpose: RECOVERY_OFFER_PURPOSE_ID,
      tone,
      includeNotes,
      mode,
      currentBody: isRewrite ? priorMessage : null,
      currentSubject: isRewrite && channel === "email" ? priorSubject : null,
      confirmedOffer,
    }

    try {
      const result = await adapters.prepareRecoveryDraft(
        request,
        controller.signal
      )

      if (
        generation !== state.aiDraftGeneration
        || controller.signal.aborted
      ) {
        return
      }

      if (result.status === "succeeded") {
        let nextSubject = priorSubject
        let nextMessage = priorMessage
        if (mode === "prepare") {
          nextSubject =
            channel === "email" ? (result.subject ?? "").trim() : ""
          nextMessage = result.body
        } else if (mode === "rewrite_subject") {
          nextSubject =
            channel === "email" ? (result.subject ?? "").trim() : ""
          nextMessage = priorMessage
        } else {
          nextSubject = priorSubject
          nextMessage = result.body
        }
        state = {
          ...state,
          draft: {
            ...state.draft,
            subject: nextSubject,
            message: nextMessage,
            writeEntry: "editor",
            messageComplete: false,
          },
          aiDraftStatus: "idle",
          aiDraftMode: null,
          preparingOverlayOpen: false,
          aiDraftError: null,
          aiDraftRetryable: true,
          aiActionCount: state.aiActionCount + 1,
        }
        publish()
        return
      }

      state = {
        ...state,
        draft: {
          ...state.draft,
          subject: mode === "prepare" ? "" : priorSubject,
          message: mode === "prepare" ? "" : priorMessage,
        },
        aiDraftStatus: "failed",
        preparingOverlayOpen: false,
        aiDraftError: AI_DRAFT_ERROR_MESSAGE,
        aiDraftRetryable: result.retryable,
      }
      publish()
    } catch (error) {
      if (
        generation !== state.aiDraftGeneration
        || controller.signal.aborted
        || (error instanceof DOMException && error.name === "AbortError")
      ) {
        return
      }

      state = {
        ...state,
        draft: {
          ...state.draft,
          subject: mode === "prepare" ? "" : priorSubject,
          message: mode === "prepare" ? "" : priorMessage,
        },
        aiDraftStatus: "failed",
        preparingOverlayOpen: false,
        aiDraftError: AI_DRAFT_ERROR_MESSAGE,
        aiDraftRetryable: true,
      }
      publish()
    } finally {
      if (aiAbortController === controller) {
        aiAbortController = null
      }
    }
  }

  const api: RespondWithRecoveryOfferModule = {
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot() {
      return snapshot
    },
    async open(feedbackId, preloadedDetails) {
      const generation = ++state.loadGeneration
      const existingDraft = draftsByFeedbackId.get(feedbackId)
      const locationId = adapters.getLocationId?.() ?? null

      if (aiAbortController != null) {
        aiAbortController.abort()
        aiAbortController = null
      }

      const applyLoaded = (
        response: FeedbackDetailsResponse,
        attachedOfferId?: number | null
      ) => {
        const capability = deriveStartRecoveryContactCapability(
          response.contactType,
          response.guestContact
        )
        let draft = applyDraftDefaults(response, existingDraft)
        if (attachedOfferId !== undefined) {
          draft = { ...draft, offerId: attachedOfferId }
        }
        const step = furthestRespondWithRecoveryOfferStep(draft)
        const maskedDestination = maskRespondToGuestDestination(
          response.contactType,
          response.guestContact
        )

        state = {
          ...state,
          loadStatus: "loaded",
          loadError: null,
          feedbackId: response.id,
          locationId,
          step,
          headerSubtitle: buildHeaderSubtitle(
            response.id,
            response.locationName,
            response.qrSource
          ),
          summary: {
            guestName: response.guestName,
            contactCapability: capability,
            feedbackComment: response.comment,
            locationName: response.locationName,
            ...mapResponseSetupSummaryChrome(response, capability),
            purposeLabel: RECOVERY_OFFER_PURPOSE_LABEL,
            toneLabel: labelForRespondToGuestTone(draft.tone),
            offerTitle: draft.offer.title.trim() || null,
            offerTypeLabel: labelForRecoveryOfferType(draft.offer.offerType),
          },
          contactType: response.contactType,
          guestContact: response.guestContact,
          contactCapability: capability,
          availableChannels: availableRespondToGuestChannels(capability),
          draft,
          offerStanceId:
            draft.offerId != null ? "create-and-select" : state.offerStanceId,
          attachedOfferTitle:
            draft.offerId != null && draft.offer.title.trim() !== ""
              ? draft.offer.title.trim()
              : state.attachedOfferTitle,
          maskedDestination,
          locationName: response.locationName,
          locationAddress: response.address,
          workflowStatus: parseWorkflowStatus(response.workflowStatus),
        }
      }

      const hydrateOfferAttach = async (feedbackIdForAttach: number) => {
        try {
          const attachedOfferId = await adapters.getRecoveryOfferAttach(
            feedbackIdForAttach
          )
          if (generation !== state.loadGeneration) {
            return
          }
          state = {
            ...state,
            draft: { ...state.draft, offerId: attachedOfferId },
            offerStanceId:
              attachedOfferId != null
                ? "create-and-select"
                : state.offerStanceId,
            attachedOfferStatus:
              attachedOfferId == null ? null : state.attachedOfferStatus,
            attachedOfferTitle:
              attachedOfferId == null ? null : state.attachedOfferTitle,
          }
          publish()

          if (attachedOfferId != null && adapters.getOffer != null) {
            try {
              const offer = await adapters.getOffer(attachedOfferId)
              if (
                generation !== state.loadGeneration
                || state.draft.offerId !== attachedOfferId
              ) {
                return
              }
              const recoveryDraft = catalogDetailToRecoveryOfferDraft(offer)
              state = {
                ...state,
                attachedOfferTitle: offer.title,
                attachedOfferStatus: offer.status,
                createOfferDraft: catalogOfferDetailToDraft(offer),
                draft: {
                  ...state.draft,
                  offer: {
                    ...recoveryDraft,
                    offerComplete: state.draft.offer.offerComplete,
                  },
                },
              }
              publish()
            } catch {
              // Keep OfferId; unknown status blocks Continue until Active.
              state = {
                ...state,
                attachedOfferStatus: null,
              }
              publish()
            }
          } else if (attachedOfferId != null) {
            state = {
              ...state,
              attachedOfferStatus: null,
            }
            publish()
          }
        } catch {
          // Keep draft.offerId when attach load fails.
        }
      }

      if (
        preloadedDetails != null
        && preloadedDetails.id === feedbackId
      ) {
        state = {
          ...emptySession(),
          loadGeneration: generation,
          aiDraftGeneration: state.aiDraftGeneration,
          isOpen: true,
          feedbackId,
          locationId,
        }
        applyLoaded(preloadedDetails)
        publish()
        await hydrateOfferAttach(feedbackId)
        return
      }

      state = {
        ...emptySession(),
        loadGeneration: generation,
        aiDraftGeneration: state.aiDraftGeneration,
        isOpen: true,
        loadStatus: "loading",
        feedbackId,
        locationId,
      }
      publish()

      try {
        const response = await adapters.getFeedbackDetails(feedbackId)
        if (generation !== state.loadGeneration) {
          return
        }
        applyLoaded(response)
        publish()
        await hydrateOfferAttach(feedbackId)
      } catch {
        if (generation !== state.loadGeneration) {
          return
        }
        state = {
          ...state,
          loadStatus: "error",
          loadError: "Could not load recovery. Please try again.",
        }
        publish()
      }
    },
    async openFromDraftAction(input) {
      draftsByFeedbackId.set(input.feedbackId, {
        channel: input.channel,
        tone: input.tone,
        includeNotes: input.includeNotes,
        subject: input.subject,
        message: input.message,
        setupComplete: true,
        messageComplete: true,
        writeEntry: "editor",
        offerId: input.offerId,
        offer: {
          ...emptyRespondWithRecoveryOfferDraft().offer,
          offerComplete: true,
        },
      })
      await api.open(input.feedbackId)
      if (
        state.feedbackId === input.feedbackId
        && state.loadStatus === "loaded"
      ) {
        state = {
          ...state,
          step: "review",
          openedFromDraftAction: true,
          draft: {
            ...state.draft,
            offerId: input.offerId,
            setupComplete: true,
            messageComplete: true,
            writeEntry: "editor",
            offer: {
              ...state.draft.offer,
              offerComplete: true,
            },
          },
        }
        publish()
      }
    },
    saveAndExit() {
      if (state.aiDraftStatus === "running") {
        return
      }
      const feedbackId = state.feedbackId
      const offerId = state.draft.offerId
      persistDraftIfComposable()
      closeSession()
      if (feedbackId != null) {
        void adapters.setRecoveryOfferAttach(feedbackId, offerId)
      }
    },
    close() {
      closeSession()
    },
    back() {
      if (state.aiDraftStatus === "running") {
        return "stayed"
      }
      if (state.step === "setup") {
        persistDraftIfComposable()
        closeSession()
        return "return-to-shell"
      }
      if (state.step === "offer") {
        state = {
          ...state,
          step: "setup",
          offerDescriptionAiStatus: "idle",
          offerDescriptionAiError: null,
        }
        publish()
        return "stayed"
      }
      if (state.step === "write") {
        state = {
          ...state,
          step: "offer",
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
        }
        clearAiDraftUi()
        publish()
        return "stayed"
      }
      if (state.step === "review") {
        state = {
          ...state,
          step: "write",
          draft: { ...state.draft, writeEntry: "editor" },
          guestPreviewOpen: false,
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
        }
        publish()
        return "stayed"
      }
      return "stayed"
    },
    setChannel(channel) {
      if (state.step !== "setup" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, channel, setupComplete: false },
      }
      publish()
    },
    setTone(tone) {
      if (state.step !== "setup" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, tone, setupComplete: false },
      }
      publish()
    },
    setIncludeNotes(value) {
      if (state.step !== "setup" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, includeNotes: value },
      }
      publish()
    },
    continueSetup() {
      if (
        state.step !== "setup"
        || state.aiDraftStatus === "running"
        || !canContinueRespondWithRecoveryOfferSetup(state.draft)
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, setupComplete: true },
        step: "offer",
      }
      publish()
    },
    async setOfferId(offerId) {
      if (state.feedbackId == null || state.aiDraftStatus === "running") {
        return
      }
      const feedbackId = state.feedbackId
      state = {
        ...state,
        draft: { ...state.draft, offerId },
        offerStanceId: offerId != null ? "create-and-select" : state.offerStanceId,
        attachedOfferTitle: offerId == null ? null : state.attachedOfferTitle,
        attachedOfferStatus: offerId == null ? null : state.attachedOfferStatus,
      }
      publish()
      await adapters.setRecoveryOfferAttach(feedbackId, offerId)
      if (offerId != null && adapters.getOffer != null) {
        try {
          const offer = await adapters.getOffer(offerId)
          if (!state.isOpen || state.draft.offerId !== offerId) {
            return
          }
          state = {
            ...state,
            attachedOfferTitle: offer.title,
            attachedOfferStatus: offer.status,
            createOfferDraft: catalogOfferDetailToDraft(offer),
            draft: {
              ...state.draft,
              offer: catalogDetailToRecoveryOfferDraft(offer),
            },
          }
          publish()
        } catch {
          state = {
            ...state,
            attachedOfferStatus: null,
          }
          publish()
        }
      }
    },
    setOfferStanceId(stanceId) {
      if (state.step !== "offer" || state.aiDraftStatus === "running") {
        return
      }
      const option = RECOVERY_OFFER_STANCE_OPTIONS.find(
        (item) => item.id === stanceId
      )
      if (option == null || option.disabled) {
        return
      }
      if (stanceId === "create-and-select") {
        existingOfferPickerLoadGeneration += 1
        state = clearExistingOfferPicker({
          ...state,
          offerStanceId: stanceId,
          createOfferPanelOpen: true,
          createOfferDrawerMode: "create",
          createOfferDraft:
            state.draft.offerId != null
              ? state.createOfferDraft
              : emptyCampaignCatalogOfferDetailsDraft(),
          createOfferStatus: "idle",
          createOfferError: null,
        })
        publish()
        return
      }
      if (stanceId === "existing-offer") {
        state = {
          ...state,
          offerStanceId: stanceId,
          createOfferPanelOpen: false,
          createOfferStatus: "idle",
          createOfferError: null,
          existingOfferPickerVisible: true,
          existingOfferPickerSearchQuery: "",
          existingOfferPickerLoadStatus: "idle",
          existingOfferPickerError: null,
          existingOfferPickerItems: [],
        }
        publish()
        void loadExistingOfferPicker()
      }
    },
    setExistingOfferSearch(query) {
      if (
        state.step !== "offer"
        || state.offerStanceId !== "existing-offer"
        || !state.existingOfferPickerVisible
      ) {
        return
      }
      state = {
        ...state,
        existingOfferPickerSearchQuery: query,
      }
      publish()
    },
    selectExistingOffer(offerId) {
      if (
        state.step !== "offer"
        || state.offerStanceId !== "existing-offer"
        || !state.existingOfferPickerVisible
        || state.feedbackId == null
        || state.aiDraftStatus === "running"
      ) {
        return
      }
      const item = state.existingOfferPickerItems.find(
        (offer) => offer.id === offerId
      )
      if (item == null) {
        return
      }
      existingOfferPickerLoadGeneration += 1
      state = clearExistingOfferPicker({
        ...state,
        offerStanceId: "existing-offer",
        attachedOfferTitle: item.title,
        attachedOfferStatus: item.status,
        createOfferPanelOpen: false,
        draft: {
          ...state.draft,
          offerId: item.id,
          offer: {
            ...state.draft.offer,
            title: item.title,
            titleTouched: true,
            offerComplete: false,
          },
        },
      })
      publish()
      void (async () => {
        if (state.feedbackId == null) {
          return
        }
        await adapters.setRecoveryOfferAttach(state.feedbackId, item.id)
        if (adapters.getOffer != null) {
          try {
            const offer = await adapters.getOffer(item.id)
            if (!state.isOpen || state.draft.offerId !== item.id) {
              return
            }
            state = {
              ...state,
              attachedOfferTitle: offer.title,
              attachedOfferStatus: offer.status,
              createOfferDraft: catalogOfferDetailToDraft(offer),
              draft: {
                ...state.draft,
                offer: catalogDetailToRecoveryOfferDraft(offer),
              },
            }
            publish()
          } catch {
            // Keep list title/status from Select.
          }
        }
      })()
    },
    async retryExistingOfferPicker() {
      if (
        state.step !== "offer"
        || state.offerStanceId !== "existing-offer"
        || !state.existingOfferPickerVisible
      ) {
        return
      }
      await loadExistingOfferPicker()
    },
    openCreateOfferPanel() {
      if (state.step !== "offer" || state.aiDraftStatus === "running") {
        return
      }
      existingOfferPickerLoadGeneration += 1
      state = clearExistingOfferPicker({
        ...state,
        offerStanceId: "create-and-select",
        createOfferPanelOpen: true,
        createOfferDrawerMode: "create",
        createOfferStatus: "idle",
        createOfferError: null,
      })
      publish()
    },
    closeCreateOfferPanel() {
      if (!state.isOpen) {
        return
      }
      state = {
        ...state,
        createOfferPanelOpen: false,
        createOfferStatus: "idle",
        createOfferError: null,
      }
      publish()
    },
    patchCreateOfferDraft(patch) {
      if (
        state.step !== "offer"
        || !state.createOfferPanelOpen
        || state.aiDraftStatus === "running"
      ) {
        return
      }
      state = {
        ...state,
        createOfferDraft: mergeCampaignCatalogOfferDraftPatch(
          state.createOfferDraft,
          patch
        ),
        createOfferError: null,
      }
      publish()
    },
    async confirmCreateOffer() {
      if (
        !state.isOpen
        || state.step !== "offer"
        || state.feedbackId == null
        || state.locationId == null
        || !state.createOfferPanelOpen
        || state.createOfferStatus === "saving"
        || state.aiDraftStatus === "running"
      ) {
        return "noop"
      }

      if (state.createOfferDrawerMode === "edit") {
        if (
          adapters.updateOffer == null
          || state.draft.offerId == null
          || !canConfirmCampaignCatalogOfferDetails(state.createOfferDraft)
        ) {
          return "noop"
        }
        const body = toCreateCatalogOfferRequestBody({
          locationId: state.locationId,
          draft: state.createOfferDraft,
        })
        if (body == null) {
          return "noop"
        }
        const offerId = state.draft.offerId
        const feedbackId = state.feedbackId
        state = {
          ...state,
          createOfferStatus: "saving",
          createOfferError: null,
        }
        publish()
        try {
          const offer = await adapters.updateOffer(offerId, body)
          await adapters.setRecoveryOfferAttach(feedbackId, offer.id)
          if (
            !state.isOpen
            || state.feedbackId !== feedbackId
            || state.draft.offerId !== offerId
          ) {
            return "noop"
          }
          const recoveryDraft = catalogDetailToRecoveryOfferDraft(offer)
          state = {
            ...state,
            attachedOfferTitle: offer.title,
            attachedOfferStatus: offer.status,
            createOfferDraft: catalogOfferDetailToDraft(offer),
            createOfferPanelOpen: false,
            createOfferStatus: "idle",
            createOfferError: null,
            draft: {
              ...state.draft,
              offerId: offer.id,
              offer: recoveryDraft,
            },
          }
          publish()
          return "updated"
        } catch {
          state = {
            ...state,
            createOfferStatus: "error",
            createOfferError: RECOVERY_OFFER_STEP_COPY.createOfferError,
          }
          publish()
          return "error"
        }
      }

      if (adapters.createOffer == null) {
        return "noop"
      }

      const body = toCreateCatalogOfferRequestBody({
        locationId: state.locationId,
        draft: state.createOfferDraft,
      })
      if (body == null) {
        return "noop"
      }

      const feedbackId = state.feedbackId
      state = {
        ...state,
        createOfferStatus: "saving",
        createOfferError: null,
      }
      publish()

      try {
        const offer = await adapters.createOffer(body)
        await adapters.setRecoveryOfferAttach(feedbackId, offer.id)
        if (!state.isOpen || state.feedbackId !== feedbackId) {
          return "noop"
        }
        const recoveryDraft = catalogDetailToRecoveryOfferDraft(offer)
        state = {
          ...state,
          offerStanceId: "create-and-select",
          attachedOfferTitle: offer.title,
          attachedOfferStatus: offer.status,
          createOfferDraft: catalogOfferDetailToDraft(offer),
          createOfferPanelOpen: false,
          createOfferStatus: "idle",
          createOfferError: null,
          draft: {
            ...state.draft,
            offerId: offer.id,
            offer: recoveryDraft,
          },
        }
        publish()
        return "created"
      } catch {
        state = {
          ...state,
          createOfferStatus: "error",
          createOfferError: RECOVERY_OFFER_STEP_COPY.createOfferError,
        }
        publish()
        return "error"
      }
    },
    async editAttachedOffer() {
      if (state.step !== "offer" || state.aiDraftStatus === "running") {
        return
      }
      if (state.draft.offerId == null) {
        return
      }
      if (state.offerStanceId === "existing-offer") {
        existingOfferPickerLoadGeneration += 1
        state = {
          ...state,
          createOfferPanelOpen: false,
          existingOfferPickerVisible: true,
          existingOfferPickerSearchQuery: "",
          existingOfferPickerLoadStatus: "idle",
          existingOfferPickerError: null,
          existingOfferPickerItems: [],
        }
        publish()
        await loadExistingOfferPicker()
        return
      }
      const offerId = state.draft.offerId
      existingOfferPickerLoadGeneration += 1
      state = clearExistingOfferPicker({
        ...state,
        offerStanceId: "create-and-select",
        createOfferPanelOpen: true,
        createOfferDrawerMode: "edit",
        createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
        createOfferStatus: "idle",
        createOfferError: null,
      })
      publish()
      if (adapters.getOffer == null) {
        return
      }
      try {
        const offer = await adapters.getOffer(offerId)
        if (!state.isOpen || state.draft.offerId !== offerId) {
          return
        }
        state = {
          ...state,
          attachedOfferTitle: offer.title,
          attachedOfferStatus: offer.status,
          createOfferDraft: catalogOfferDetailToDraft(offer),
        }
        publish()
      } catch {
        state = {
          ...state,
          createOfferError: RECOVERY_OFFER_STEP_COPY.createOfferError,
        }
        publish()
      }
    },
    setOfferType(offerType) {
      patchOffer({
        offerType,
        titleTouched: false,
      })
    },
    setDiscountPercentage(value) {
      patchOffer({ discountPercentage: value })
    },
    setDiscountAmount(value) {
      patchOffer({ discountAmount: value })
    },
    setFreeItemText(value) {
      patchOffer({ freeItemText: value })
    },
    setPurchaseRequirement(value) {
      patchOffer({ purchaseRequirement: value })
    },
    setMinimumSpend(value) {
      patchOffer({ minimumSpend: value })
    },
    setAdditionalExclusions(value) {
      patchOffer({ additionalExclusions: value }, { autoTitle: false })
    },
    setReplacementItemText(value) {
      patchOffer({ replacementItemText: value })
    },
    setOfferTitle(value) {
      patchOffer(
        {
          title: value.slice(0, RECOVERY_OFFER_TITLE_MAX),
          titleTouched: true,
        },
        { autoTitle: false }
      )
    },
    setOfferDescription(value) {
      patchOffer(
        {
          description: value.slice(0, RECOVERY_OFFER_DESCRIPTION_MAX),
        },
        { autoTitle: false }
      )
    },
    setOfferValidity(value) {
      patchOffer({ validity: value }, { autoTitle: false })
    },
    setExpiryDate(value) {
      patchOffer({ expiryDate: value }, { autoTitle: false })
    },
    setStaffInstructions(value) {
      patchOffer({ staffInstructions: value }, { autoTitle: false })
    },
    async prepareOfferDescription() {
      if (
        state.step !== "offer"
        || state.feedbackId == null
        || state.draft.channel == null
        || state.draft.tone == null
        || state.offerDescriptionAiStatus === "running"
      ) {
        return
      }

      const offerForDraft = withAutoTitle(state.draft.offer)
      const confirmedOffer = toConfirmedRecoveryOfferPayload({
        ...offerForDraft,
        description:
          offerForDraft.description.trim() === ""
            ? "placeholder"
            : offerForDraft.description,
      })
      // Allow AI when description empty: temporarily satisfy validator.
      const offerPayload: ConfirmedRecoveryOfferPayload | null =
        confirmedOffer
        ?? (offerForDraft.offerType != null
          ? {
              offerType: offerForDraft.offerType,
              title: offerForDraft.title.trim() || autoTitleForRecoveryOffer(offerForDraft),
              description: offerForDraft.description.trim() || offerForDraft.title.trim() || "Recovery offer",
              validity: offerForDraft.validity,
              expiryDate:
                offerForDraft.validity === "choose_expiry_date"
                  ? offerForDraft.expiryDate.trim() || null
                  : null,
              discountPercentage: null,
              discountAmount: null,
              freeItemText: offerForDraft.freeItemText.trim() || null,
              purchaseRequirement: offerForDraft.purchaseRequirement,
              minimumSpend: null,
              additionalExclusions:
                offerForDraft.additionalExclusions.trim() || null,
              replacementItemText:
                offerForDraft.replacementItemText.trim() || null,
              staffInstructions:
                offerForDraft.staffInstructions.trim() || null,
            }
          : null)

      if (offerPayload == null) {
        return
      }

      const feedbackId = state.feedbackId
      const channel = state.draft.channel
      const tone = state.draft.tone
      const generation = ++state.aiDraftGeneration

      if (aiAbortController != null) {
        aiAbortController.abort()
      }
      const controller = new AbortController()
      aiAbortController = controller

      state = {
        ...state,
        offerDescriptionAiStatus: "running",
        offerDescriptionAiError: null,
      }
      publish()

      try {
        const result = await adapters.prepareRecoveryDraft(
          {
            feedbackId,
            channel,
            purpose: RECOVERY_OFFER_PURPOSE_ID,
            tone,
            includeNotes:
              "Write a short guest-facing offer description only (max 240 characters).",
            mode: "prepare",
            currentBody: null,
            currentSubject: null,
            confirmedOffer: offerPayload,
          },
          controller.signal
        )

        if (
          generation !== state.aiDraftGeneration
          || controller.signal.aborted
        ) {
          return
        }

        if (result.status === "succeeded") {
          state = {
            ...state,
            draft: {
              ...state.draft,
              offer: {
                ...state.draft.offer,
                description: result.body
                  .trim()
                  .slice(0, RECOVERY_OFFER_DESCRIPTION_MAX),
                offerComplete: false,
              },
            },
            offerDescriptionAiStatus: "idle",
            offerDescriptionAiError: null,
            aiActionCount: state.aiActionCount + 1,
          }
          publish()
          return
        }

        state = {
          ...state,
          offerDescriptionAiStatus: "failed",
          offerDescriptionAiError: OFFER_DESCRIPTION_AI_ERROR_MESSAGE,
        }
        publish()
      } catch (error) {
        if (
          generation !== state.aiDraftGeneration
          || controller.signal.aborted
          || (error instanceof DOMException && error.name === "AbortError")
        ) {
          return
        }
        state = {
          ...state,
          offerDescriptionAiStatus: "failed",
          offerDescriptionAiError: OFFER_DESCRIPTION_AI_ERROR_MESSAGE,
        }
        publish()
      } finally {
        if (aiAbortController === controller) {
          aiAbortController = null
        }
      }
    },
    continueOffer() {
      if (
        state.step !== "offer"
        || !canContinueRecoveryOfferAttach({
          offerId: state.draft.offerId,
          attachedOfferStatus: state.attachedOfferStatus,
        })
      ) {
        return
      }
      state = {
        ...state,
        draft: {
          ...state.draft,
          offer: { ...state.draft.offer, offerComplete: true },
          writeEntry:
            state.draft.writeEntry === "editor" ? "editor" : "chooser",
        },
        step: "write",
        offerDescriptionAiStatus: "idle",
        offerDescriptionAiError: null,
        createOfferPanelOpen: false,
      }
      clearAiDraftUi()
      publish()
    },
    editOffer() {
      if (state.step !== "write" && state.step !== "review") {
        return
      }
      if (state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        step: "offer",
        draft: {
          ...state.draft,
          offer: { ...state.draft.offer, offerComplete: false },
          messageComplete: false,
        },
        guestPreviewOpen: false,
        sendConfirmOpen: false,
        sendStatus: "idle",
        sendError: null,
      }
      clearAiDraftUi()
      publish()
    },
    writeManually() {
      if (state.step !== "write") {
        return
      }
      if (state.aiDraftStatus === "running") {
        state.aiDraftGeneration += 1
        if (aiAbortController != null) {
          aiAbortController.abort()
          aiAbortController = null
        }
      }
      state = {
        ...state,
        draft: { ...state.draft, writeEntry: "editor" },
        aiDraftStatus: "idle",
        aiDraftMode: null,
        preparingOverlayOpen: false,
        aiDraftError: null,
        aiDraftRetryable: true,
      }
      publish()
    },
    async prepareDraft() {
      await runAiDraft("prepare")
    },
    async rewriteDraft(target) {
      if (state.draft.writeEntry !== "editor") {
        return
      }
      if (target === "subject") {
        if (state.draft.channel !== "email") {
          return
        }
        await runAiDraft("rewrite_subject")
        return
      }
      await runAiDraft("rewrite_message")
    },
    async retryAiDraft() {
      if (
        state.aiDraftStatus !== "failed"
        || !state.aiDraftRetryable
        || state.aiDraftMode == null
      ) {
        return
      }
      await runAiDraft(state.aiDraftMode)
    },
    dismissPreparingOverlay() {
      if (!state.preparingOverlayOpen) {
        return
      }
      state = {
        ...state,
        preparingOverlayOpen: false,
      }
      publish()
    },
    setSubject(value) {
      if (
        state.step !== "write"
        || state.draft.writeEntry !== "editor"
        || state.aiDraftStatus === "running"
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, subject: value, messageComplete: false },
        aiDraftStatus: "idle",
        aiDraftError: null,
      }
      publish()
    },
    setMessage(value) {
      if (
        state.step !== "write"
        || state.draft.writeEntry !== "editor"
        || state.aiDraftStatus === "running"
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, message: value, messageComplete: false },
        aiDraftStatus: "idle",
        aiDraftError: null,
      }
      publish()
    },
    continueWrite() {
      if (
        state.step !== "write"
        || state.draft.writeEntry !== "editor"
        || state.aiDraftStatus === "running"
        || !canContinueRespondToGuestMessage({
          channel: state.draft.channel,
          subject: state.draft.subject,
          message: state.draft.message,
        })
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, messageComplete: true },
        step: "review",
      }
      clearAiDraftUi()
      publish()
    },
    editText() {
      if (state.step !== "review") {
        return
      }
      state = {
        ...state,
        step: "write",
        draft: { ...state.draft, writeEntry: "editor", messageComplete: false },
        guestPreviewOpen: false,
        sendConfirmOpen: false,
        sendStatus: "idle",
        sendError: null,
      }
      publish()
    },
    openGuestPreview() {
      if (state.step !== "review") {
        return
      }
      state = {
        ...state,
        guestPreviewOpen: true,
      }
      publish()
    },
    closeGuestPreview() {
      if (!state.guestPreviewOpen) {
        return
      }
      state = {
        ...state,
        guestPreviewOpen: false,
      }
      publish()
    },
    async openSendTestDialog() {
      if (
        !canOpenGuestPreviewSendTest({
          feedbackId: state.feedbackId,
          channel: state.draft.channel,
          step: state.step,
          sendTestStatus: state.sendTestStatus,
          aiDraftStatus: state.aiDraftStatus,
        })
      ) {
        return
      }

      let email = ""
      if (adapters.getOperatorAccountEmail != null) {
        try {
          email = (await adapters.getOperatorAccountEmail()) ?? ""
        } catch {
          email = ""
        }
      }

      if (
        !canOpenGuestPreviewSendTest({
          feedbackId: state.feedbackId,
          channel: state.draft.channel,
          step: state.step,
          sendTestStatus: state.sendTestStatus,
          aiDraftStatus: state.aiDraftStatus,
        })
      ) {
        return
      }

      state = {
        ...state,
        sendTestDialogOpen: true,
        sendTestEmail: email,
        sendTestStatus: "idle",
        sendTestError: null,
      }
      publish()
    },
    closeSendTestDialog() {
      if (!state.sendTestDialogOpen || state.sendTestStatus === "sending") {
        return
      }
      state = {
        ...state,
        ...emptyGuestPreviewSendTestSession(),
      }
      publish()
    },
    setSendTestEmail(value) {
      if (!state.sendTestDialogOpen) {
        return
      }
      const clearingError = state.sendTestStatus === "error"
      state = {
        ...state,
        sendTestEmail: value,
        sendTestStatus: clearingError ? "idle" : state.sendTestStatus,
        sendTestError: clearingError ? null : state.sendTestError,
      }
      publish()
    },
    async confirmSendTest() {
      if (
        state.feedbackId == null
        || state.draft.channel !== "email"
        || state.step !== "review"
        || !state.sendTestDialogOpen
        || state.sendTestStatus === "sending"
        || state.aiDraftStatus === "running"
      ) {
        return
      }

      const toEmail = state.sendTestEmail.trim()
      const subject = state.draft.subject.trim()
      const body = state.draft.message.trim()
      if (toEmail === "" || subject === "" || body === "") {
        state = {
          ...state,
          sendTestStatus: "error",
          sendTestError: GUEST_PREVIEW_SEND_TEST_ERROR,
        }
        publish()
        return
      }

      const feedbackId = state.feedbackId
      state = {
        ...state,
        sendTestStatus: "sending",
        sendTestError: null,
      }
      publish()

      try {
        const coupon = buildGuestPreviewOfferCoupon(
          toConfirmedRecoveryOfferPayload(state.draft.offer)
        )
        await adapters.sendGuestPreviewTest({
          feedbackId,
          subject,
          body,
          toEmail,
          offer:
            coupon == null
              ? null
              : {
                  title: coupon.title,
                  description: coupon.description,
                  expiryLabel: coupon.expiryLabel,
                },
        })
        state = {
          ...state,
          sendTestDialogOpen: false,
          sendTestStatus: "success",
          sendTestError: null,
        }
        publish()
      } catch {
        state = {
          ...state,
          sendTestStatus: "error",
          sendTestError: GUEST_PREVIEW_SEND_TEST_ERROR,
        }
        publish()
      }
    },
    openSendConfirm() {
      if (state.step !== "review" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        sendConfirmOpen: true,
        sendStatus: "idle",
        sendError: null,
      }
      publish()
    },
    cancelSendConfirm() {
      state = {
        ...state,
        sendConfirmOpen: false,
        sendStatus: "idle",
        sendError: null,
      }
      publish()
    },
    async confirmSend() {
      const offer = toConfirmedRecoveryOfferPayload(state.draft.offer)
      if (
        state.feedbackId == null
        || state.draft.channel == null
        || state.draft.tone == null
        || offer == null
        || (state.step !== "review" && !state.sendConfirmOpen)
      ) {
        return
      }

      const feedbackId = state.feedbackId
      const channel = state.draft.channel
      const tone = state.draft.tone
      const subject =
        channel === "email" ? state.draft.subject.trim() : null
      const body = state.draft.message.trim()
      const includeNotes =
        state.draft.includeNotes.trim() === ""
          ? null
          : state.draft.includeNotes.trim()

      if (state.step === "review" && !state.sendConfirmOpen) {
        state = { ...state, sendConfirmOpen: true }
      }

      state = {
        ...state,
        sendConfirmOpen: true,
        sendStatus: "saving",
        sendError: null,
      }
      publish()

      const request: SendAndIssueRecoveryOfferRequest = {
        feedbackId,
        channel,
        subject,
        body,
        intent: "respond_with_recovery_offer",
        purpose: RECOVERY_OFFER_PURPOSE_ID,
        tone,
        includeNotes,
        offer,
      }

      try {
        const result = await adapters.sendAndIssueRecoveryOffer(request)
        draftsByFeedbackId.delete(feedbackId)
        state = {
          ...state,
          step: "success",
          guestPreviewOpen: false,
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
          workflowStatus: result.workflowStatus,
          issuedOffer: result.issuedOffer,
          draft: emptyRespondWithRecoveryOfferDraft(),
        }
        publish()
      } catch {
        state = {
          ...state,
          step: "review",
          sendConfirmOpen: true,
          sendStatus: "error",
          sendError: SEND_ERROR_MESSAGE,
        }
        publish()
      }
    },
    keepInProgress() {
      if (state.step !== "success") {
        return
      }
      closeSession()
    },
    async markResolved() {
      if (state.step !== "success" || state.feedbackId == null) {
        return
      }
      const feedbackId = state.feedbackId
      state = {
        ...state,
        completeStatus: "saving",
        completeError: null,
      }
      publish()

      try {
        await adapters.completeRecovery(
          feedbackId,
          "respond_with_recovery_offer"
        )
        closeSession()
      } catch {
        state = {
          ...state,
          completeStatus: "error",
          completeError: COMPLETE_ERROR_MESSAGE,
        }
        publish()
      }
    },
  }
  return api
}

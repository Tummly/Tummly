import { isCampaignBillingReserveUnavailableError } from "@/lib/operatorCampaigns/campaignBillingReserveUnavailableError"
import {
  CAMPAIGN_COMMIT_COPY,
  campaignCommitConfirmCopy,
  campaignCommitSuccessChrome,
  campaignReviewPrimaryActionLabel,
} from "@/lib/operatorCampaigns/campaignCommitPresentation"
import { CampaignDraftHttp409Error } from "@/lib/operatorCampaigns/campaignDraftHttp409Error"
import {
  CAMPAIGN_AUDIENCE_COPY,
  CAMPAIGN_AUDIENCE_OPTIONS,
  errorCampaignAudienceEligibilityBreakdown,
  evaluableCampaignAudienceIds,
  formatAudienceMatchedEligibleLabel,
  isCampaignAudienceUnevaluable,
  resolveAudienceCardCounts,
  unavailableCampaignAudienceEligibilityBreakdown,
  type CampaignAudienceCountSource,
  type CampaignAudienceEligibilityBreakdown,
  type CampaignAudienceId,
  type CampaignAudienceSmartGroupCountsInput,
} from "@/lib/operatorCampaigns/campaignAudiencePresentation"
import {
  CAMPAIGN_CHANNEL_COPY,
  CAMPAIGN_CHANNEL_OPTIONS,
  buildCampaignChannelUsageSummary,
  defaultCampaignChannelId,
  resolveCampaignChannelShortfall,
  type CampaignChannelEstimateMode,
  type CampaignChannelId,
  type CampaignChannelShortfall,
  type CampaignChannelUsageRow,
} from "@/lib/operatorCampaigns/campaignChannelPresentation"
import {
  CAMPAIGN_MESSAGE_AI_DRAFT_ERROR,
  CAMPAIGN_MESSAGE_COPY,
  CAMPAIGN_MESSAGE_DRAFT_DEFAULT_TONE,
  isCampaignMessageDraftRewriteMode,
  type CampaignMessageDraftMode,
  type CampaignMessageDraftRewriteTarget,
  type CampaignMessageWriteEntry,
} from "@/lib/operatorCampaigns/campaignMessagePresentation"
import {
  CAMPAIGN_EXISTING_OFFER_PICKER_COPY,
  filterExistingOfferPickerItems,
  mapCatalogOfferToExistingPickerCard,
  type CampaignExistingOfferPickerCard,
} from "@/lib/operatorCampaigns/campaignExistingOfferPickerPresentation"
import {
  CAMPAIGN_OFFER_COPY,
  CAMPAIGN_OFFER_OPTIONS,
  defaultCampaignOfferStanceId,
  type CampaignOfferStanceId,
} from "@/lib/operatorCampaigns/campaignOfferPresentation"
import type {
  ConfirmCatalogOfferWriteResult,
  CreateEditOfferDrawerMode,
} from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import { CREATE_EDIT_OFFER_DRAWER_COPY } from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import {
  canConfirmCampaignCatalogOfferDetails,
  catalogOfferDetailToDraft,
  emptyCampaignCatalogOfferDetailsDraft,
  isDirtyBenefitOrValidity,
  mergeCampaignCatalogOfferDraftPatch,
  shouldConfirmEditOfferSave,
  toCreateCatalogOfferRequestBody,
  type CampaignCatalogOfferDetailsDraft,
  type CreateCatalogOfferRequestBody,
} from "@/lib/operatorOffers/offerCatalogPresentation"
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
import {
  CAMPAIGN_REVIEW_COPY,
  CAMPAIGN_REVIEW_SECTIONS,
  type CampaignReviewSectionId,
} from "@/lib/operatorCampaigns/campaignReviewPresentation"
import {
  CAMPAIGN_SCHEDULE_COPY,
  CAMPAIGN_SCHEDULE_OPTIONS,
  CAMPAIGN_SCHEDULE_TIME_OPTIONS,
  canContinueCampaignSchedule,
  campaignScheduleFieldErrors,
  campaignScheduledAtUtcIso,
  defaultCampaignScheduleModeId,
  defaultCampaignScheduleTimeZone,
  labelForCampaignScheduleModeId,
  type CampaignScheduleModeId,
} from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import {
  CAMPAIGN_SEND_TEST_COPY,
  CAMPAIGN_SEND_TEST_SAMPLE_OFFER,
} from "@/lib/operatorCampaigns/campaignSendTestPresentation"
import {
  buildGuestPreviewOfferCoupon,
  GUEST_PREVIEW_OFFER_COPY_LABEL,
  GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
  type GuestPreviewOfferCouponView,
} from "@/lib/operatorFeedback/guestPreviewPresentation"
import {
  CAMPAIGN_GOAL_OPTIONS,
  CAMPAIGN_WIZARD_COPY,
  CAMPAIGN_WIZARD_NUMBERED_STEPS,
  formatCampaignWizardHeaderSubtitle,
  labelForCampaignGoalId,
  type CampaignGoalId,
  type CampaignGoalOption,
  type CampaignWizardContinueEditingStep,
  type CampaignWizardStepId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import { mapCampaignTemplateSuggestions } from "@/lib/operatorCampaigns/mapCampaignTemplateSuggestions"
import {
  maybeConsumeDirectAiOnUsableDraft,
  resolveCampaignAiPrepareGate,
  resolveCampaignMessagingUsage,
  type CampaignBillingBalancesPayload,
  type CampaignMessagingChromeAccess,
  type CampaignMessagingUsageCutover,
  type ConsumeDirectAiInput,
} from "@/lib/operatorCampaigns/campaignMessagingBalances"
import {
  resolveBillingReserveUnavailableCopy,
  resolveCampaignsMessagingLockHelper,
  type CampaignsMessagingBalancesFixture,
  type CampaignsMessagingChromeAction,
  type CampaignsMessagingLockCause,
} from "@/lib/operatorCampaigns/campaignsMessagingCreditChrome"
import {
  MESSAGING_USAGE_FIXTURE,
  type MessagingUsageFixture,
} from "@/lib/operatorCampaigns/messagingUsageFixtures"

/** @deprecated Prefer CampaignsMessagingBalancesFixture — re-exported alias. */
export type { MessagingUsageFixture }
import type { RecoverySuccessChrome } from "@/lib/operatorFeedback/recoverySuccessPresentation"
import type {
  CampaignDraftDetail,
  CampaignRecommendationDraftPrefill,
  CampaignScheduleCommitDetail,
  CampaignSendTestRequest,
  CampaignTemplateDetail,
  CatalogOfferDetail,
  CatalogOffersListItem,
  CatalogOffersListQueryParams,
  CatalogOffersListResponse,
  CommitCampaignScheduleRequest,
  CreateCampaignDraftRequest,
  PatchCampaignDraftRequest,
} from "@/types/operatorCampaigns"

const EXISTING_OFFER_PICKER_PAGE_SIZE = Math.max(OFFERS_PAGE_SIZE, 100)
const OFFERS_FILTER_SCHEMA = offersFilterSheetSchema()

export type CampaignWizardOpenBlankInput = {
  locationId: number
  locationName: string
  locationAddress?: string | null
}

export type CampaignWizardOpenFromTemplateInput = {
  locationId: number
  locationName: string
  locationAddress?: string | null
  template: CampaignTemplateDetail
}

export type CampaignWizardOpenFromDraftInput = {
  locationName: string
  locationAddress?: string | null
  draft: CampaignDraftDetail
  /** Assistant Change audience / Add Offer / later send-schedule land. */
  startStep?: CampaignWizardContinueEditingStep
  scheduleMode?: CampaignScheduleModeId
  dateLocal?: string
  timeLocal?: string
}

export type CampaignWizardOpenFromRecommendationInput = {
  locationId: number
  locationName: string
  locationAddress?: string | null
  draftPrefill: CampaignRecommendationDraftPrefill
}

export type PrepareCampaignMessageDraftRequest = {
  locationId: number
  channel: CampaignChannelId
  goalId: CampaignGoalId
  audienceKey: string
  offerStance: CampaignOfferStanceId
  campaignName: string | null
  tone: string
  includeNotes: string | null
  mode: CampaignMessageDraftMode
  currentBody: string | null
  currentSubject: string | null
}

export type PrepareCampaignMessageDraftResult =
  | {
      status: "succeeded"
      body: string
      subject: string | null
      channel: CampaignChannelId
    }
  | {
      status: "failed"
      retryable: boolean
    }

export type CampaignWizardAdapters = {
  getNow?: () => Date
  /** Live Smart Group aggregates for Audience — omitted until step loads. */
  loadSmartGroupCounts?: (input: {
    locationId: number
  }) => Promise<CampaignAudienceSmartGroupCountsInput>
  /** Live Campaign eligibility for one audience + location (ticket 21). */
  loadAudienceEligibility?: (input: {
    locationId: number
    audienceKey: CampaignAudienceId
  }) => Promise<CampaignAudienceEligibilityBreakdown>
  /** Persist Draft on Save / Save and exit (ticket 29). */
  createDraft?: (
    body: CreateCampaignDraftRequest
  ) => Promise<CampaignDraftDetail>
  updateDraft?: (
    id: number,
    body: PatchCampaignDraftRequest
  ) => Promise<CampaignDraftDetail>
  /** Create Active Offers catalog definition (ticket 22). */
  createOffer?: (
    body: CreateCatalogOfferRequestBody
  ) => Promise<CatalogOfferDetail>
  /** Update catalog definition on Edit (ticket 31). */
  updateOffer?: (
    offerId: number,
    body: CreateCatalogOfferRequestBody
  ) => Promise<CatalogOfferDetail>
  /** Load catalog definition for attached OfferId (resume / Edit). */
  getOffer?: (offerId: number) => Promise<CatalogOfferDetail>
  /** Browse Active/attachable catalog Offers for Existing stance (ticket 30). */
  listCatalogOffers?: (
    params: CatalogOffersListQueryParams
  ) => Promise<CatalogOffersListResponse>
  /** Live Campaign message-draft AI (ticket 33). */
  prepareMessageDraft?: (
    request: PrepareCampaignMessageDraftRequest,
    signal?: AbortSignal
  ) => Promise<PrepareCampaignMessageDraftResult>
  /**
   * Billing balances (+ plan) for Channel meters + Soft-lock / AI gates (ticket 25).
   * Omitted → fixtures + display-only AI debit.
   */
  loadMessagingBalances?: () => Promise<CampaignBillingBalancesPayload>
  /** Billing ConsumeDirect — 1 AI on usable prepare/rewrite after live cutover. */
  consumeDirectAi?: (input: ConsumeDirectAiInput) => Promise<void>
  /** Prefill Send test email dialog with the signed-in operator account email. */
  getOperatorAccountEmail?: () => string | null | Promise<string | null>
  /** Campaign send test — transactional Resend; no credit burn. */
  sendCampaignTest?: (request: CampaignSendTestRequest) => Promise<void>
  /**
   * Schedule / send commit — freeze + Billing Reserve (ticket 26).
   * Omitted → confirm stays hard-blocked until Billing Reserve is live.
   */
  commitCampaign?: (input: {
    campaignId: number
    body: CommitCampaignScheduleRequest
  }) => Promise<CampaignScheduleCommitDetail>
  /**
   * True when Billing Reserve adapter IsLive is true.
   * Controls stub vs live unexpected-503 copy (ticket 23). Default false.
   */
  billingReserveLive?: boolean
  /**
   * Billing chrome access for Soft-lock helper + overview / shortfall CTAs (ticket 23).
   * Omit: treat as Owner + manage so Account-owner chrome stays visible until
   * Billing page / `/auth/me` fields are live. Explicit view/none still hides writes.
   * Live balances `chromeAccess` overrides this when present.
   */
  messagingChromeAccess?: CampaignMessagingChromeAccess
}

export type CampaignWizardGoalCardViewModel = CampaignGoalOption & {
  selected: boolean
}

export type CampaignAudienceOptionViewModel = {
  id: Exclude<CampaignAudienceId, "saved-group">
  title: string
  description: string
  recommended: boolean
  selected: boolean
  unevaluable: boolean
  matched: number | null
  currentlyEligible: number | null
  countLabel: string
  countSource: CampaignAudienceCountSource
}

export type CampaignAudienceViewModel = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  selectedAudienceId: CampaignAudienceId
  options: CampaignAudienceOptionViewModel[]
  eligibilityBreakdown: CampaignAudienceEligibilityBreakdown
}

export type CampaignChannelOptionViewModel = {
  id: CampaignChannelId
  title: string
  description: string
  selected: boolean
}

export type CampaignChannelViewModel = {
  selectedChannelId: CampaignChannelId
  stepHeading: string
  stepDescription: string
  options: CampaignChannelOptionViewModel[]
  usageSummary: {
    title: string
    audienceLine: string
    rows: CampaignChannelUsageRow[]
  }
  /**
   * Shared overview / Channel balances source when ready.
   * null after live cutover load-failed (no fixture fallback).
   */
  messagingFixture: CampaignsMessagingBalancesFixture | null
  messagingBalancesStatus: "ready" | "load-failed"
  messagingBalancesError: string | null
  channelShortfall: CampaignChannelShortfall | null
}

export type CampaignOfferOptionViewModel = {
  id: CampaignOfferStanceId
  title: string
  description: string
  selected: boolean
  disabled: boolean
}

export type CampaignExistingOfferPickerLoadStatus =
  | "idle"
  | "loading"
  | "ready"
  | "error"

export type CampaignExistingOfferPickerViewModel = {
  visible: boolean
  loadStatus: CampaignExistingOfferPickerLoadStatus
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

export type CampaignOfferViewModel = {
  selectedStanceId: CampaignOfferStanceId
  attachedOfferId: number | null
  attachedOfferTitle: string | null
  createPanelOpen: boolean
  createOfferDrawerMode: CreateEditOfferDrawerMode
  createOfferDraft: CampaignCatalogOfferDetailsDraft
  createOfferStatus: "idle" | "saving" | "error"
  createOfferError: string | null
  canConfirmCreateOffer: boolean
  /** Always false once catalog update API is live (ticket 31). */
  createOfferSaveGated: boolean
  pendingEditOfferSave: {
    title: string
    description: string
  } | null
  locationSubtitle: string
  stepHeading: string
  stepDescription: string
  options: CampaignOfferOptionViewModel[]
  /** Inline Existing picker under stance cards (ticket 30). */
  existingOfferPicker: CampaignExistingOfferPickerViewModel | null
  usageSummary: {
    title: string
    audienceLine: string
    rows: CampaignChannelUsageRow[]
  }
  /** Same shared overview fixture as Channel (ticket 19 / 24). */
  messagingFixture: CampaignsMessagingBalancesFixture | null
  channelShortfall: CampaignChannelShortfall | null
}

export type CampaignMessageViewModel = {
  writeEntry: CampaignMessageWriteEntry
  subject: string
  body: string
  channelId: CampaignChannelId
  showSubject: boolean
  /** True when `prepareMessageDraft` adapter is wired (ticket 33). */
  prepareAiLive: boolean
  /** Soft-lock / AI=0 / balances-failed gate after live cutover (ticket 25). */
  aiPrepareAllowed: boolean
  aiPrepareBlockReason: string | null
  guestPreviewOpen: boolean
  /** Email channel only — SMS Send test stays unavailable. */
  sendTestAvailable: boolean
  aiDraftStatus: "idle" | "running" | "failed"
  aiDraftMode: CampaignMessageDraftMode | null
  aiDraftError: string | null
  aiDraftRetryable: boolean
  preparingOverlayOpen: boolean
  /** Successful AI prepares/rewrites this session (ledger debit is separate). */
  aiActionCount: number
  stepHeading: string
  stepDescription: string
  locationName: string
  locationAddress: string | null
  usageSummary: {
    title: string
    audienceLine: string
    rows: CampaignChannelUsageRow[]
  }
  messagingFixture: CampaignsMessagingBalancesFixture | null
  channelShortfall: CampaignChannelShortfall | null
}

export type CampaignScheduleOptionViewModel = {
  id: CampaignScheduleModeId
  title: string
  description: string
  selected: boolean
}

export type CampaignScheduleViewModel = {
  selectedModeId: CampaignScheduleModeId
  stepHeading: string
  stepDescription: string
  options: CampaignScheduleOptionViewModel[]
  dateLocal: string
  timeLocal: string
  dateError: string | null
  timeError: string | null
  showDatetimeFields: boolean
  timeOptions: readonly string[]
  usageSummary: {
    title: string
    audienceLine: string
    rows: CampaignChannelUsageRow[]
  }
  messagingFixture: CampaignsMessagingBalancesFixture | null
  channelShortfall: CampaignChannelShortfall | null
}

export type CampaignReviewSectionRow = {
  label: string
  value: string
}

export type CampaignReviewSectionViewModel = {
  id: CampaignReviewSectionId
  title: string
  rows: CampaignReviewSectionRow[]
}

export type CampaignReviewGuestPreviewViewModel = {
  channelId: CampaignChannelId
  subject: string
  body: string
  locationName: string
  locationAddress: string | null
  guestPreviewOpen: boolean
  /** Email channel only — SMS Send test stays unavailable. */
  sendTestAvailable: boolean
  /**
   * Figma offer block when Offer stance is not "No offer" — sample code chrome
   * (matches Send test / template Preview). Null when no offer.
   */
  offerCoupon: GuestPreviewOfferCouponView | null
}

export type CampaignSendTestDialogViewModel = {
  isOpen: boolean
  email: string
  status: "idle" | "sending" | "success" | "error"
  error: string | null
  canSubmit: boolean
}

export type CampaignCommitConfirmViewModel = {
  open: boolean
  busy: boolean
  title: string
  description: string
  confirmLabel: string
  cancelLabel: string
  confirmBusyLabel: string
  error: string | null
}

export type CampaignReviewViewModel = {
  stepHeading: string
  stepDescription: string
  /** True when Schedule / send commit gates pass (Billing Reserve adapter required). */
  sendAvailable: boolean
  /** Honest reason when `sendAvailable` is false; null when send is allowed. */
  sendBlockedReason: string | null
  /**
   * Shortfall / hard-stop chrome — Buy + Change plan when channel credits are short.
   * Null when unlocked or Soft-lock is the blocking reason.
   */
  channelShortfall: CampaignChannelShortfall | null
  sections: CampaignReviewSectionViewModel[]
  guestPreview: CampaignReviewGuestPreviewViewModel
}

export type CampaignWizardSnapshot = {
  isOpen: boolean
  locationId: number | null
  locationName: string | null
  templateId: string | null
  /** Server Draft id after first successful Save; null while client-only. */
  draftId: number | null
  saveStatus: "idle" | "saving" | "saved" | "error"
  saveError: string | null
  lastSavedAt: Date | null
  stepId: CampaignWizardStepId
  goalId: CampaignGoalId | null
  goals: CampaignWizardGoalCardViewModel[]
  pageTitle: string
  headerSubtitle: string
  stepHeading: string | null
  stepDescription: string | null
  showNumberedStepper: boolean
  /** Always the 1–6 strip model — UI shows it only when `showNumberedStepper`. */
  numberedSteps: typeof CAMPAIGN_WIZARD_NUMBERED_STEPS
  /** Index into `numberedSteps` when the numbered strip is shown. */
  activeNumberedStepIndex: number
  canContinue: boolean
  /** Footer primary label — Review switches by schedule mode. */
  primaryActionLabel: string
  placeholderBody: string | null
  audience: CampaignAudienceViewModel | null
  channel: CampaignChannelViewModel | null
  offer: CampaignOfferViewModel | null
  message: CampaignMessageViewModel | null
  schedule: CampaignScheduleViewModel | null
  review: CampaignReviewViewModel | null
  /** Null when wizard closed or Send test unavailable (SMS / no adapter). */
  sendTest: CampaignSendTestDialogViewModel | null
  /** Confirm dialog for Schedule / send commit (ticket 26). */
  commitConfirm: CampaignCommitConfirmViewModel | null
  /** Recovery-pattern success chrome after commit; null mid-flow. */
  success: RecoverySuccessChrome | null
  /**
   * Soft lock / Dormant restoration helper next to disabled Schedule/Send.
   * Null when unlocked or the actor cannot restore.
   */
  lockHelper: CampaignsMessagingChromeAction | null
  footerLayout: "wizard" | "end"
}

export type CampaignWizardModule = {
  getSnapshot: () => CampaignWizardSnapshot
  subscribe: (listener: () => void) => () => void
  openBlankCreate: (input: CampaignWizardOpenBlankInput) => void
  /**
   * Use template — Goal + suggestion defaults applied; opens at Audience.
   * No server Draft until explicit Save (ticket 29).
   */
  openFromTemplate: (
    input: CampaignWizardOpenFromTemplateInput
  ) => Promise<void>
  /**
   * Continue editing — hydrate from get-by-id Draft (ticket 30).
   * Opens at Goal / Audience / Schedule from persisted fields.
   */
  openFromDraft: (input: CampaignWizardOpenFromDraftInput) => Promise<void>
  /**
   * Review campaign draft from Recommended next step — AI prefill, no server Draft
   * until explicit Save (ticket 32).
   */
  openFromRecommendation: (
    input: CampaignWizardOpenFromRecommendationInput
  ) => Promise<void>
  close: () => void
  /** Persist editable fields; keep wizard open. */
  save: () => Promise<void>
  /** Persist editable fields then close. Close without Save creates no row. */
  saveAndExit: () => Promise<void>
  setGoalId: (goalId: CampaignGoalId) => void
  setAudienceId: (audienceId: CampaignAudienceId) => void
  setChannelId: (channelId: CampaignChannelId) => void
  setOfferStanceId: (stanceId: CampaignOfferStanceId) => void
  openCreateOfferPanel: () => void
  closeCreateOfferPanel: () => void
  editAttachedOffer: () => Promise<void>
  patchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  confirmCreateOffer: () => Promise<ConfirmCatalogOfferWriteResult>
  setExistingOfferSearch: (query: string) => void
  selectExistingOffer: (offerId: number) => void
  retryExistingOfferPicker: () => Promise<void>
  /** Empty-picker CTA — switch to create-new-offer + open Create drawer. */
  createNewOfferFromExistingPicker: () => void
  confirmPendingEditOfferSave: () => Promise<ConfirmCatalogOfferWriteResult>
  cancelPendingEditOfferSave: () => void
  setScheduleModeId: (modeId: CampaignScheduleModeId) => void
  setScheduleDateLocal: (value: string) => void
  setScheduleTimeLocal: (value: string) => void
  writeManually: () => void
  prepareDraft: () => Promise<void>
  rewriteDraft: (target: CampaignMessageDraftRewriteTarget) => Promise<void>
  retryAiDraft: () => Promise<void>
  dismissPreparingOverlay: () => void
  /** Retry live Billing balances after Channel/Message load-failed (ticket 25). */
  retryMessagingBalances: () => Promise<void>
  setSubject: (value: string) => void
  setMessage: (value: string) => void
  openGuestPreview: () => void
  closeGuestPreview: () => void
  /** From Review Guest preview Edit text — returns to Message editor. */
  editMessageFromReview: () => void
  /** Open Send test email dialog from Guest preview (email channel only). */
  openSendTestDialog: () => Promise<void>
  closeSendTestDialog: () => void
  setSendTestEmail: (value: string) => void
  confirmSendTest: () => Promise<void>
  /** Review primary — open Schedule / send confirm when commit gates pass. */
  openCommitConfirm: () => void
  cancelCommitConfirm: () => void
  /** Persist draft if needed, then commit schedule / send. */
  confirmCommit: () => Promise<void>
  /** Alias for confirmCommit (tests / callers). */
  scheduleCommit: () => Promise<void>
  /** Close wizard from success chrome. */
  dismissSuccess: () => void
  continue: () => Promise<void>
  back: () => void
}

type WizardState = {
  isOpen: boolean
  locationId: number | null
  locationName: string | null
  locationAddress: string | null
  templateId: string | null
  templateVersion: number | null
  draftId: number | null
  draftRowVersion: string | null
  saveStatus: CampaignWizardSnapshot["saveStatus"]
  saveError: string | null
  lastSavedAt: Date | null
  stepId: CampaignWizardStepId
  goalId: CampaignGoalId | null
  openedAt: Date | null
  audienceId: CampaignAudienceId
  audienceLoadStatus: CampaignAudienceViewModel["loadStatus"]
  liveCounts: CampaignAudienceSmartGroupCountsInput | null
  eligibilityByAudienceId: Partial<
    Record<CampaignAudienceId, CampaignAudienceEligibilityBreakdown>
  >
  channelId: CampaignChannelId
  offerStanceId: CampaignOfferStanceId
  attachedOfferId: number | null
  attachedOfferTitle: string | null
  createOfferPanelOpen: boolean
  createOfferDrawerMode: CreateEditOfferDrawerMode
  createOfferDraft: CampaignCatalogOfferDetailsDraft
  createOfferStatus: CampaignOfferViewModel["createOfferStatus"]
  createOfferError: string | null
  existingOfferPickerVisible: boolean
  existingOfferPickerLoadStatus: CampaignExistingOfferPickerLoadStatus
  existingOfferPickerError: string | null
  existingOfferPickerSearchQuery: string
  existingOfferPickerItems: CatalogOffersListItem[]
  editBaselineDraft: CampaignCatalogOfferDetailsDraft | null
  editIssueCount: number
  pendingEditOfferSave: CampaignOfferViewModel["pendingEditOfferSave"]
  scheduleModeId: CampaignScheduleModeId
  scheduleDateLocal: string
  scheduleTimeLocal: string
  scheduleTimeZone: string
  messageWriteEntry: CampaignMessageWriteEntry
  messageSubject: string
  messageBody: string
  /** Client-only draft title — recommendation campaignName or blank until Save. */
  draftName: string | null
  guestPreviewOpen: boolean
  sendTestDialogOpen: boolean
  sendTestEmail: string
  sendTestStatus: CampaignSendTestDialogViewModel["status"]
  sendTestError: string | null
  /** Operator account email for Review Sender + Send test prefill. */
  operatorSenderEmail: string | null
  commitConfirmOpen: boolean
  commitStatus: "idle" | "saving" | "error"
  commitError: string | null
  commitSuccess: {
    modeId: CampaignScheduleModeId
    campaignName: string
    scheduledAtUtc: string | null
    committedAt: Date
  } | null
  aiDraftStatus: "idle" | "running" | "failed"
  aiDraftMode: CampaignMessageDraftMode | null
  aiDraftError: string | null
  aiDraftRetryable: boolean
  preparingOverlayOpen: boolean
  aiDraftGeneration: number
  aiActionCount: number
  messagingCutover: CampaignMessagingUsageCutover
  messagingBalancesStatus: "ready" | "load-failed"
  messagingBalancesError: string | null
  messagingFixture: CampaignsMessagingBalancesFixture
  aiAvailable: number | null
  softLocked: boolean
  isPilot: boolean
  lockCause: CampaignsMessagingLockCause | null
  messagingChromeAccess: CampaignMessagingChromeAccess
}

const NUMBERED_STEP_ORDER: readonly CampaignWizardStepId[] =
  CAMPAIGN_WIZARD_NUMBERED_STEPS.map((step) => step.id)

const DEFAULT_AUDIENCE_ID: CampaignAudienceId = "all-eligible-guests"

const CAMPAIGN_DRAFT_SAVE_ERROR_MESSAGE =
  "Could not save this campaign draft. Try again."

function emptyState(
  messagingChromeAccess: CampaignMessagingChromeAccess = {
    accessLevel: "manage",
    permissionRole: "Owner",
  }
): WizardState {
  return {
    isOpen: false,
    locationId: null,
    locationName: null,
    locationAddress: null,
    templateId: null,
    templateVersion: null,
    draftId: null,
    draftRowVersion: null,
    saveStatus: "idle",
    saveError: null,
    lastSavedAt: null,
    stepId: "goal",
    goalId: null,
    openedAt: null,
    audienceId: DEFAULT_AUDIENCE_ID,
    audienceLoadStatus: "idle",
    liveCounts: null,
    eligibilityByAudienceId: {},
    channelId: defaultCampaignChannelId(),
    offerStanceId: defaultCampaignOfferStanceId(),
    attachedOfferId: null,
    attachedOfferTitle: null,
    createOfferPanelOpen: false,
    createOfferDrawerMode: "create",
    createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
    createOfferStatus: "idle",
    createOfferError: null,
    existingOfferPickerVisible: false,
    existingOfferPickerLoadStatus: "idle",
    existingOfferPickerError: null,
    existingOfferPickerSearchQuery: "",
    existingOfferPickerItems: [],
    editBaselineDraft: null,
    editIssueCount: 0,
    pendingEditOfferSave: null,
    scheduleModeId: defaultCampaignScheduleModeId(),
    scheduleDateLocal: "",
    scheduleTimeLocal: "",
    scheduleTimeZone: defaultCampaignScheduleTimeZone(),
    messageWriteEntry: "chooser",
    messageSubject: "",
    messageBody: "",
    draftName: null,
    guestPreviewOpen: false,
    sendTestDialogOpen: false,
    sendTestEmail: "",
    sendTestStatus: "idle",
    sendTestError: null,
    operatorSenderEmail: null,
    commitConfirmOpen: false,
    commitStatus: "idle",
    commitError: null,
    commitSuccess: null,
    aiDraftStatus: "idle",
    aiDraftMode: null,
    aiDraftError: null,
    aiDraftRetryable: true,
    preparingOverlayOpen: false,
    aiDraftGeneration: 0,
    aiActionCount: 0,
    messagingCutover: "fixtures",
    messagingBalancesStatus: "ready",
    messagingBalancesError: null,
    messagingFixture: MESSAGING_USAGE_FIXTURE,
    aiAvailable: null,
    softLocked: false,
    isPilot: MESSAGING_USAGE_FIXTURE.isPilot,
    lockCause: null,
    messagingChromeAccess,
  }
}

function normalizeLocationAddress(
  value: string | null | undefined
): string | null {
  if (value == null) {
    return null
  }
  const trimmed = value.trim()
  return trimmed.length > 0 ? trimmed : null
}

function isSendTestAvailable(
  state: WizardState,
  sendCampaignTestWired: boolean
): boolean {
  return state.channelId === "email" && sendCampaignTestWired
}

function buildSendTestViewModel(
  state: WizardState,
  sendTestAvailable: boolean
): CampaignSendTestDialogViewModel | null {
  if (!state.isOpen || !sendTestAvailable) {
    return null
  }
  const email = state.sendTestEmail.trim()
  return {
    isOpen: state.sendTestDialogOpen,
    email: state.sendTestEmail,
    status: state.sendTestStatus,
    error: state.sendTestError,
    canSubmit: email.length > 0 && state.sendTestStatus !== "sending",
  }
}

function clearSendTestDialog(state: WizardState): WizardState {
  return {
    ...state,
    sendTestDialogOpen: false,
    sendTestEmail: "",
    sendTestStatus: "idle",
    sendTestError: null,
  }
}

function buildDraftFields(state: WizardState): {
  goalId: string | null
  templateId: string | null
  templateVersion: number | null
  audienceKey: string
  channel: string
  offerStance: string
  offerId: number | null
  messageSubject: string | null
  messageBody: string | null
} {
  const messageSubject = state.messageSubject.trim()
  const messageBody = state.messageBody.trim()
  const offerStance = state.offerStanceId
  const offerId =
    offerStance === "no-offer" ? null : state.attachedOfferId
  return {
    goalId: state.goalId,
    templateId: state.templateId,
    templateVersion:
      state.templateId == null ? null : state.templateVersion,
    audienceKey: state.audienceId,
    channel: state.channelId,
    offerStance,
    offerId,
    messageSubject: messageSubject.length > 0 ? messageSubject : null,
    messageBody: messageBody.length > 0 ? messageBody : null,
  }
}

function placeholderForStep(stepId: CampaignWizardStepId): string | null {
  switch (stepId) {
    case "audience":
    case "channel":
    case "offer":
    case "message":
    case "schedule":
    case "review":
    case "goal":
    case "success":
      return null
  }
}

function buildAudienceViewModel(
  state: WizardState
): CampaignAudienceViewModel | null {
  if (state.stepId !== "audience") {
    return null
  }

  const options: CampaignAudienceOptionViewModel[] =
    CAMPAIGN_AUDIENCE_OPTIONS.map((option) => {
      const counts = resolveAudienceCardCounts({
        option,
        liveCounts: state.liveCounts,
        eligibilityByAudienceId: state.eligibilityByAudienceId,
      })
      let countLabel: string
      if (counts.countSource === "unavailable") {
        countLabel = CAMPAIGN_AUDIENCE_COPY.countsUnavailableLabel
      } else {
        countLabel = formatAudienceMatchedEligibleLabel(
          counts.matched,
          counts.currentlyEligible
        )
      }
      return {
        id: option.id,
        title: option.title,
        description: option.description,
        recommended: option.recommended,
        selected: state.audienceId === option.id,
        unevaluable: option.unevaluable,
        matched: counts.matched,
        currentlyEligible: counts.currentlyEligible,
        countLabel,
        countSource: counts.countSource,
      }
    })

  let eligibilityBreakdown: CampaignAudienceEligibilityBreakdown
  if (isCampaignAudienceUnevaluable(state.audienceId)) {
    eligibilityBreakdown = unavailableCampaignAudienceEligibilityBreakdown()
  } else {
    eligibilityBreakdown =
      state.eligibilityByAudienceId[state.audienceId]
      ?? (state.audienceLoadStatus === "error"
        ? errorCampaignAudienceEligibilityBreakdown()
        : unavailableCampaignAudienceEligibilityBreakdown())
  }

  return {
    loadStatus: state.audienceLoadStatus,
    selectedAudienceId: state.audienceId,
    options,
    eligibilityBreakdown,
  }
}

function channelUsageEligibility(
  state: WizardState
): CampaignAudienceEligibilityBreakdown {
  return (
    state.eligibilityByAudienceId[state.audienceId]
    ?? unavailableCampaignAudienceEligibilityBreakdown()
  )
}

function channelEstimateMode(state: WizardState): CampaignChannelEstimateMode {
  if (state.stepId === "channel" || state.stepId === "offer") {
    return "floor"
  }
  if (
    (state.stepId === "message"
      || state.stepId === "schedule"
      || state.stepId === "review")
    && state.channelId === "sms"
    && state.messageBody.trim().length > 0
  ) {
    return "exact"
  }
  return "floor"
}

function buildChannelUsageSummary(state: WizardState) {
  return buildCampaignChannelUsageSummary({
    channelId: state.channelId,
    locationName: state.locationName ?? "",
    eligibility: channelUsageEligibility(state),
    fixture: state.messagingFixture,
    estimateMode: channelEstimateMode(state),
  })
}

function resolveChannelShortfallForState(
  state: WizardState
): CampaignChannelShortfall | null {
  if (state.messagingBalancesStatus !== "ready") {
    return null
  }
  const eligibility = channelUsageEligibility(state)
  const channelEligible =
    state.channelId === "email"
      ? eligibility.emailEligible
      : eligibility.smsEligible
  return resolveCampaignChannelShortfall({
    channelId: state.channelId,
    channelEligible,
    fixture: state.messagingFixture,
    estimateMode: channelEstimateMode(state),
    accessLevel: state.messagingChromeAccess.accessLevel,
    permissionRole: state.messagingChromeAccess.permissionRole,
  })
}

function buildChannelViewModel(
  state: WizardState
): CampaignChannelViewModel | null {
  if (state.stepId !== "channel") {
    return null
  }

  const balancesReady = state.messagingBalancesStatus === "ready"
  const fixture = balancesReady ? state.messagingFixture : null
  const usage = balancesReady
    ? buildChannelUsageSummary(state)
    : {
        audienceLine: state.messagingBalancesError ?? "",
        rows: [] as CampaignChannelUsageRow[],
      }

  return {
    selectedChannelId: state.channelId,
    stepHeading: CAMPAIGN_CHANNEL_COPY.stepHeading,
    stepDescription: CAMPAIGN_CHANNEL_COPY.stepDescription,
    options: CAMPAIGN_CHANNEL_OPTIONS.map((option) => ({
      ...option,
      selected: state.channelId === option.id,
    })),
    usageSummary: {
      title: CAMPAIGN_CHANNEL_COPY.usageTitle,
      audienceLine: usage.audienceLine,
      rows: usage.rows,
    },
    messagingFixture: fixture,
    messagingBalancesStatus: state.messagingBalancesStatus,
    messagingBalancesError: state.messagingBalancesError,
    channelShortfall: resolveChannelShortfallForState(state),
  }
}

function clearExistingOfferPicker(state: WizardState): WizardState {
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
  state: WizardState
): CampaignExistingOfferPickerViewModel | null {
  if (
    state.stepId !== "offer"
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
    searchPlaceholder: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.searchPlaceholder,
    error:
      state.existingOfferPickerLoadStatus === "error"
        ? (state.existingOfferPickerError
          ?? CAMPAIGN_EXISTING_OFFER_PICKER_COPY.loadError)
        : null,
    retryLabel: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.retryLabel,
    cards,
    isEmpty: catalogEmpty || searchMiss,
    emptyHelper: catalogEmpty
      ? CAMPAIGN_EXISTING_OFFER_PICKER_COPY.emptyHelper
      : searchMiss
        ? CAMPAIGN_EXISTING_OFFER_PICKER_COPY.searchMissHelper
        : null,
    createNewOfferLabel: catalogEmpty
      ? CAMPAIGN_EXISTING_OFFER_PICKER_COPY.createNewOfferLabel
      : null,
    selectLabel: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.selectLabel,
    viewDetailsLabel: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.viewDetailsLabel,
    viewDetailsEnabled: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.viewDetailsEnabled,
  }
}

function buildOfferViewModel(
  state: WizardState
): CampaignOfferViewModel | null {
  if (state.stepId !== "offer") {
    return null
  }

  const balancesReady = state.messagingBalancesStatus === "ready"
  const usage = balancesReady
    ? buildChannelUsageSummary(state)
    : {
        audienceLine: state.messagingBalancesError ?? "",
        rows: [] as CampaignChannelUsageRow[],
      }

  return {
    selectedStanceId: state.offerStanceId,
    attachedOfferId: state.attachedOfferId,
    attachedOfferTitle: state.attachedOfferTitle,
    createPanelOpen: state.createOfferPanelOpen,
    createOfferDrawerMode: state.createOfferDrawerMode,
    createOfferDraft: state.createOfferDraft,
    createOfferStatus: state.createOfferStatus,
    createOfferError: state.createOfferError,
    createOfferSaveGated: false,
    canConfirmCreateOffer: canConfirmCampaignCatalogOfferDetails(
      state.createOfferDraft
    ),
    pendingEditOfferSave: state.pendingEditOfferSave,
    locationSubtitle: state.locationName ?? "",
    stepHeading: CAMPAIGN_OFFER_COPY.stepHeading,
    stepDescription: CAMPAIGN_OFFER_COPY.stepDescription,
    options: CAMPAIGN_OFFER_OPTIONS.map((option) => ({
      ...option,
      selected: state.offerStanceId === option.id,
    })),
    existingOfferPicker: buildExistingOfferPickerViewModel(state),
    usageSummary: {
      title: CAMPAIGN_OFFER_COPY.usageTitle,
      audienceLine: usage.audienceLine,
      rows: usage.rows,
    },
    messagingFixture: balancesReady ? state.messagingFixture : null,
    channelShortfall: resolveChannelShortfallForState(state),
  }
}

function buildMessageViewModel(
  state: WizardState,
  prepareAiLive: boolean,
  sendTestAvailable: boolean
): CampaignMessageViewModel | null {
  if (state.stepId !== "message") {
    return null
  }

  const balancesReady = state.messagingBalancesStatus === "ready"
  const usage = balancesReady
    ? buildChannelUsageSummary(state)
    : {
        audienceLine: state.messagingBalancesError ?? "",
        rows: [] as CampaignChannelUsageRow[],
      }
  const prepareGate = resolveCampaignAiPrepareGate({
    cutover: state.messagingCutover,
    softLocked: state.softLocked,
    aiAvailable: state.aiAvailable,
    balancesStatus: state.messagingBalancesStatus,
  })

  return {
    writeEntry: state.messageWriteEntry,
    subject: state.messageSubject,
    body: state.messageBody,
    channelId: state.channelId,
    showSubject: state.channelId === "email",
    prepareAiLive,
    aiPrepareAllowed: prepareGate.allowed,
    aiPrepareBlockReason: prepareGate.blockReason,
    guestPreviewOpen: state.guestPreviewOpen,
    sendTestAvailable,
    aiDraftStatus: state.aiDraftStatus,
    aiDraftMode: state.aiDraftMode,
    aiDraftError: state.aiDraftError,
    aiDraftRetryable: state.aiDraftRetryable,
    preparingOverlayOpen: state.preparingOverlayOpen,
    aiActionCount: state.aiActionCount,
    stepHeading: CAMPAIGN_MESSAGE_COPY.stepHeading,
    stepDescription: CAMPAIGN_MESSAGE_COPY.stepDescription,
    locationName: state.locationName ?? "",
    locationAddress: state.locationAddress,
    usageSummary: {
      title: CAMPAIGN_MESSAGE_COPY.usageTitle,
      audienceLine: usage.audienceLine,
      rows: usage.rows,
    },
    messagingFixture: balancesReady ? state.messagingFixture : null,
    channelShortfall: resolveChannelShortfallForState(state),
  }
}

function buildScheduleViewModel(
  state: WizardState,
  now: Date
): CampaignScheduleViewModel | null {
  if (state.stepId !== "schedule") {
    return null
  }

  const balancesReady = state.messagingBalancesStatus === "ready"
  const usage = balancesReady
    ? buildChannelUsageSummary(state)
    : {
        audienceLine: state.messagingBalancesError ?? "",
        rows: [] as CampaignChannelUsageRow[],
      }
  const fieldErrors = campaignScheduleFieldErrors({
    modeId: state.scheduleModeId,
    dateLocal: state.scheduleDateLocal,
    timeLocal: state.scheduleTimeLocal,
    now,
  })

  return {
    selectedModeId: state.scheduleModeId,
    stepHeading: CAMPAIGN_SCHEDULE_COPY.stepHeading,
    stepDescription: CAMPAIGN_SCHEDULE_COPY.stepDescription,
    options: CAMPAIGN_SCHEDULE_OPTIONS.map((option) => ({
      ...option,
      selected: state.scheduleModeId === option.id,
    })),
    dateLocal: state.scheduleDateLocal,
    timeLocal: state.scheduleTimeLocal,
    dateError: fieldErrors.dateError,
    timeError: fieldErrors.timeError,
    showDatetimeFields: state.scheduleModeId === "schedule-later",
    timeOptions: CAMPAIGN_SCHEDULE_TIME_OPTIONS,
    usageSummary: {
      title: CAMPAIGN_SCHEDULE_COPY.usageTitle,
      audienceLine: usage.audienceLine,
      rows: usage.rows,
    },
    messagingFixture: balancesReady ? state.messagingFixture : null,
    channelShortfall: resolveChannelShortfallForState(state),
  }
}

function audienceTitleForId(audienceId: CampaignAudienceId): string {
  return (
    CAMPAIGN_AUDIENCE_OPTIONS.find((option) => option.id === audienceId)
      ?.title ?? CAMPAIGN_REVIEW_COPY.emptyValue
  )
}

function channelTitleForId(channelId: CampaignChannelId): string {
  return (
    CAMPAIGN_CHANNEL_OPTIONS.find((option) => option.id === channelId)?.title
    ?? CAMPAIGN_REVIEW_COPY.emptyValue
  )
}

function offerTitleForId(stanceId: CampaignOfferStanceId): string {
  return (
    CAMPAIGN_OFFER_OPTIONS.find((option) => option.id === stanceId)?.title
    ?? CAMPAIGN_REVIEW_COPY.emptyValue
  )
}

/** Guest preview offer chrome for Review — sample code, never an issued code. */
function buildReviewGuestPreviewOfferCoupon(
  state: WizardState
): GuestPreviewOfferCouponView | null {
  if (state.offerStanceId === "no-offer") {
    return null
  }

  const draftTitle = state.createOfferDraft.title.trim()
  const attachedTitle = state.attachedOfferTitle?.trim() ?? ""
  const title =
    draftTitle
    || attachedTitle
    || CAMPAIGN_SEND_TEST_SAMPLE_OFFER.title
  const description =
    state.createOfferDraft.description.trim()
    || CAMPAIGN_SEND_TEST_SAMPLE_OFFER.description

  const fromDraft = buildGuestPreviewOfferCoupon({
    title,
    description,
    validity: state.createOfferDraft.validity,
    expiryDate: state.createOfferDraft.expiryDate,
  })
  if (fromDraft != null) {
    return fromDraft
  }

  return {
    title: CAMPAIGN_SEND_TEST_SAMPLE_OFFER.title,
    description: CAMPAIGN_SEND_TEST_SAMPLE_OFFER.description,
    redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
    expiryLabel: CAMPAIGN_SEND_TEST_SAMPLE_OFFER.expiryLabel,
    copyLabel: GUEST_PREVIEW_OFFER_COPY_LABEL,
  }
}

function sendBlockedReason(
  state: WizardState,
  commitCampaignWired: boolean,
  billingReserveLive: boolean,
  now: Date
): string | null {
  if (canCommitCampaign(state, commitCampaignWired, now)) {
    return null
  }
  if (!commitCampaignWired) {
    return resolveBillingReserveUnavailableCopy({ billingReserveLive })
  }
  if (state.softLocked) {
    return CAMPAIGN_COMMIT_COPY.softLocked
  }
  if (isChannelHardStopped(state)) {
    return CAMPAIGN_COMMIT_COPY.channelHardStop
  }
  if (selectedChannelEligibleCount(state) < 1) {
    return CAMPAIGN_COMMIT_COPY.zeroEligible
  }
  return CAMPAIGN_COMMIT_COPY.commitNotReady
}

function buildReviewViewModel(
  state: WizardState,
  sendTestAvailable: boolean,
  sendAvailable: boolean,
  sendBlockedReason: string | null
): CampaignReviewViewModel | null {
  if (state.stepId !== "review") {
    return null
  }

  const balancesReady = state.messagingBalancesStatus === "ready"
  const usage = balancesReady
    ? buildChannelUsageSummary(state)
    : {
        audienceLine: state.messagingBalancesError ?? "",
        rows: [] as CampaignChannelUsageRow[],
      }

  const goalLabel =
    labelForCampaignGoalId(state.goalId) ?? CAMPAIGN_REVIEW_COPY.emptyValue
  const locationLabel = state.locationName ?? CAMPAIGN_REVIEW_COPY.emptyValue
  const senderValue =
    state.operatorSenderEmail != null
    && state.operatorSenderEmail.trim().length > 0
      ? state.operatorSenderEmail.trim()
      : CAMPAIGN_REVIEW_COPY.emptyValue
  const subjectValue =
    state.channelId === "email"
      ? state.messageSubject.trim() || CAMPAIGN_REVIEW_COPY.emptyValue
      : null
  const messageValue =
    state.messageBody.trim() || CAMPAIGN_REVIEW_COPY.emptyValue

  const sectionRows: Record<
    CampaignReviewSectionId,
    CampaignReviewSectionRow[]
  > = {
    campaign: [
      { label: CAMPAIGN_REVIEW_COPY.goalLabel, value: goalLabel },
      { label: CAMPAIGN_REVIEW_COPY.locationLabel, value: locationLabel },
    ],
    audience: [
      {
        label: CAMPAIGN_REVIEW_COPY.audienceLabel,
        value: audienceTitleForId(state.audienceId),
      },
    ],
    channel: [
      {
        label: CAMPAIGN_REVIEW_COPY.channelLabel,
        value: channelTitleForId(state.channelId),
      },
      {
        label: CAMPAIGN_REVIEW_COPY.senderLabel,
        value: senderValue,
      },
    ],
    message: [
      ...(subjectValue != null
        ? [
            {
              label: CAMPAIGN_REVIEW_COPY.subjectLabel,
              value: subjectValue,
            },
          ]
        : []),
      {
        label: CAMPAIGN_REVIEW_COPY.messageLabel,
        value: messageValue,
      },
    ],
    offer: [
      {
        label: CAMPAIGN_REVIEW_COPY.offerLabel,
        value: offerTitleForId(state.offerStanceId),
      },
    ],
    schedule: [
      {
        label: CAMPAIGN_REVIEW_COPY.scheduleLabel,
        value: labelForCampaignScheduleModeId(state.scheduleModeId),
      },
    ],
    usage: usage.rows,
  }

  return {
    stepHeading: CAMPAIGN_REVIEW_COPY.stepHeading,
    stepDescription: CAMPAIGN_REVIEW_COPY.stepDescription,
    sendAvailable,
    sendBlockedReason,
    channelShortfall:
      !state.softLocked && isChannelHardStopped(state)
        ? resolveChannelShortfallForState(state)
        : null,
    sections: CAMPAIGN_REVIEW_SECTIONS.map((section) => ({
      id: section.id,
      title: section.title,
      rows: sectionRows[section.id],
    })),
    guestPreview: {
      channelId: state.channelId,
      subject: state.messageSubject,
      body: state.messageBody,
      locationName: state.locationName ?? "",
      locationAddress: state.locationAddress,
      guestPreviewOpen: state.guestPreviewOpen,
      sendTestAvailable,
      offerCoupon: buildReviewGuestPreviewOfferCoupon(state),
    },
  }
}

function selectedChannelEligibleCount(state: WizardState): number {
  const breakdown = state.eligibilityByAudienceId[state.audienceId]
  if (breakdown == null || breakdown.source !== "live") {
    return 0
  }
  if (state.channelId === "email") {
    return breakdown.emailEligible ?? 0
  }
  return breakdown.smsEligible ?? 0
}

function isChannelHardStopped(state: WizardState): boolean {
  if (state.messagingCutover !== "live") {
    return false
  }
  if (state.messagingBalancesStatus !== "ready") {
    return false
  }
  const eligible = selectedChannelEligibleCount(state)
  if (state.channelId === "email") {
    const remaining = state.messagingFixture.email.combinedRemaining
    return remaining === 0 || remaining < eligible
  }
  const remaining = state.messagingFixture.sms.combinedRemaining
  return remaining === 0 || remaining < eligible
}

function canCommitCampaign(
  state: WizardState,
  commitCampaignWired: boolean,
  now: Date
): boolean {
  if (!commitCampaignWired) {
    return false
  }
  if (state.softLocked) {
    return false
  }
  if (isChannelHardStopped(state)) {
    return false
  }
  if (selectedChannelEligibleCount(state) < 1) {
    return false
  }
  if (!messageCanContinue(state)) {
    return false
  }
  if (
    !canContinueCampaignSchedule({
      modeId: state.scheduleModeId,
      dateLocal: state.scheduleDateLocal,
      timeLocal: state.scheduleTimeLocal,
      now,
    })
  ) {
    return false
  }
  // Draft must be creatable / updatable so confirm can persist before commit.
  if (state.draftId == null) {
    return true
  }
  return state.draftRowVersion != null && state.draftRowVersion.length > 0
}

function buildCommitConfirmViewModel(
  state: WizardState
): CampaignCommitConfirmViewModel | null {
  if (!state.isOpen || state.stepId === "success") {
    return null
  }
  const copy = campaignCommitConfirmCopy({ modeId: state.scheduleModeId })
  return {
    open: state.commitConfirmOpen,
    busy: state.commitStatus === "saving",
    title: copy.title,
    description: copy.description,
    confirmLabel: copy.confirmLabel,
    cancelLabel: copy.cancelLabel,
    confirmBusyLabel: CAMPAIGN_COMMIT_COPY.confirmBusyLabel,
    error: state.commitError,
  }
}

function buildSuccessViewModel(
  state: WizardState
): RecoverySuccessChrome | null {
  if (state.stepId !== "success" || state.commitSuccess == null) {
    return null
  }
  return campaignCommitSuccessChrome(state.commitSuccess)
}

function audienceCanContinue(state: WizardState): boolean {
  if (isCampaignAudienceUnevaluable(state.audienceId)) {
    return false
  }
  if (state.audienceLoadStatus !== "loaded") {
    return false
  }
  const breakdown = state.eligibilityByAudienceId[state.audienceId]
  if (breakdown == null || breakdown.source !== "live") {
    return false
  }
  return (breakdown.currentlyEligible ?? 0) >= 1
}

function audienceCanPersist(state: WizardState): boolean {
  return state.audienceId !== "saved-group"
}

function messageCanContinue(state: WizardState): boolean {
  if (state.messageWriteEntry !== "editor") {
    return false
  }
  if (state.messageBody.trim().length === 0) {
    return false
  }
  if (state.channelId === "email" && state.messageSubject.trim().length === 0) {
    return false
  }
  return true
}

function offerCanContinue(state: WizardState): boolean {
  if (state.offerStanceId === "no-offer") {
    return true
  }
  if (
    state.offerStanceId === "existing-offer"
    || state.offerStanceId === "create-new-offer"
  ) {
    return state.attachedOfferId != null
  }
  return false
}

function toSnapshot(
  state: WizardState,
  getNow: () => Date,
  prepareAiLive: boolean,
  sendTestAvailable: boolean,
  commitCampaignWired: boolean,
  billingReserveLive: boolean
): CampaignWizardSnapshot {
  const now = getNow()
  const openedAt = state.openedAt ?? now
  const locationName = state.locationName ?? ""
  const isSuccess = state.stepId === "success"
  const showNumberedStepper =
    state.stepId !== "goal" && state.stepId !== "success"
  const activeNumberedStepIndex = Math.max(
    0,
    NUMBERED_STEP_ORDER.indexOf(state.stepId)
  )
  const isAudience = state.stepId === "audience"
  const isGoal = state.stepId === "goal"
  const isChannel = state.stepId === "channel"
  const isOffer = state.stepId === "offer"
  const isMessage = state.stepId === "message"
  const isSchedule = state.stepId === "schedule"
  const isReview = state.stepId === "review"
  const canCommit = canCommitCampaign(state, commitCampaignWired, now)
  const blockedReason = sendBlockedReason(
    state,
    commitCampaignWired,
    billingReserveLive,
    now
  )
  const lockHelper = resolveCampaignsMessagingLockHelper({
    softLocked: state.softLocked,
    lockCause: state.lockCause,
    accessLevel: state.messagingChromeAccess.accessLevel,
    permissionRole: state.messagingChromeAccess.permissionRole,
  })

  let canContinue = false
  if (isGoal) {
    canContinue = state.goalId != null
  } else if (isAudience) {
    canContinue = audienceCanContinue(state)
  } else if (isChannel) {
    canContinue = true
  } else if (isOffer) {
    canContinue = offerCanContinue(state)
  } else if (isSchedule) {
    canContinue = canContinueCampaignSchedule({
      modeId: state.scheduleModeId,
      dateLocal: state.scheduleDateLocal,
      timeLocal: state.scheduleTimeLocal,
      now,
    })
  } else if (isMessage) {
    canContinue = messageCanContinue(state)
  } else if (isReview) {
    canContinue = canCommit
  } else {
    canContinue = false
  }

  const success = buildSuccessViewModel(state)

  return {
    isOpen: state.isOpen,
    locationId: state.locationId,
    locationName: state.locationName,
    templateId: state.templateId,
    draftId: state.draftId,
    saveStatus: state.saveStatus,
    saveError: state.saveError,
    lastSavedAt: state.lastSavedAt,
    stepId: state.stepId,
    goalId: state.goalId,
    goals: CAMPAIGN_GOAL_OPTIONS.map((goal) => ({
      ...goal,
      selected: state.goalId === goal.id,
    })),
    pageTitle:
      isSuccess && success != null
        ? success.title
        : CAMPAIGN_WIZARD_COPY.pageTitle,
    headerSubtitle:
      isSuccess && success != null
        ? success.subtitle
        : formatCampaignWizardHeaderSubtitle({
            goalId: state.goalId,
            locationName,
            now: openedAt,
          }),
    stepHeading: isSuccess
      ? null
      : isGoal
        ? CAMPAIGN_WIZARD_COPY.goalStepHeading
        : null,
    stepDescription: isSuccess
      ? null
      : isGoal
        ? CAMPAIGN_WIZARD_COPY.goalStepDescription
        : null,
    showNumberedStepper,
    numberedSteps: CAMPAIGN_WIZARD_NUMBERED_STEPS,
    activeNumberedStepIndex,
    canContinue,
    primaryActionLabel: isReview
      ? campaignReviewPrimaryActionLabel(state.scheduleModeId)
      : CAMPAIGN_WIZARD_COPY.continue,
    placeholderBody: placeholderForStep(state.stepId),
    audience: buildAudienceViewModel(state),
    channel: buildChannelViewModel(state),
    offer: buildOfferViewModel(state),
    message: buildMessageViewModel(state, prepareAiLive, sendTestAvailable),
    schedule: buildScheduleViewModel(state, now),
    review: buildReviewViewModel(
      state,
      sendTestAvailable,
      canCommit,
      blockedReason
    ),
    sendTest: buildSendTestViewModel(state, sendTestAvailable),
    commitConfirm: buildCommitConfirmViewModel(state),
    success,
    lockHelper,
    footerLayout: isSuccess ? "end" : "wizard",
  }
}

/**
 * Campaign create wizard — blank Create opens at Goal with no template.
 * Close / dismiss never persists a server Campaign Draft (ticket 22 / 29).
 * Audience (ticket 21): live Smart Group counts + Campaign eligibility service.
 * Channel (ticket 24 / 25): Email/SMS + shared messaging balances (fixtures until Billing).
 * Offer (tickets 25 + 22 + 18 + 30): No offer clears attach; Create a new offer
 * via shared drawer; Existing offer inline Active catalog picker.
 * Message (tickets 26 + 33 + 25): Write manually / live AI prepare-rewrite + ConsumeDirect when live.
 * Send test (ticket 24): transactional Resend test email from Message + Review Guest preview.
 * Schedule + Review (ticket 26 / polish 06): commit when `commitCampaign` is
 * wired; omitted or Soft-lock / shortfall / zero eligible surfaces sendBlockedReason.
 */
export function createCampaignWizardModule(
  adapters: CampaignWizardAdapters = {}
): CampaignWizardModule {
  const getNow = adapters.getNow ?? (() => new Date())
  const prepareAiLive = adapters.prepareMessageDraft != null
  const sendCampaignTestWired = adapters.sendCampaignTest != null
  const commitCampaignWired = adapters.commitCampaign != null
  const billingReserveLive = adapters.billingReserveLive === true
  const defaultChromeAccess: CampaignMessagingChromeAccess =
    adapters.messagingChromeAccess
    ?? { accessLevel: "manage", permissionRole: "Owner" }
  let state = emptyState(defaultChromeAccess)
  let snapshot = toSnapshot(
    state,
    getNow,
    prepareAiLive,
    isSendTestAvailable(state, sendCampaignTestWired),
    commitCampaignWired,
    billingReserveLive
  )
  const listeners = new Set<() => void>()
  let audienceLoadGeneration = 0
  let existingOfferPickerLoadGeneration = 0
  let messagingBalancesGeneration = 0
  let aiAbortController: AbortController | null = null

  const publish = () => {
    snapshot = toSnapshot(
      state,
      getNow,
      prepareAiLive,
      isSendTestAvailable(state, sendCampaignTestWired),
      commitCampaignWired,
      billingReserveLive
    )
    for (const listener of listeners) {
      listener()
    }
  }

  const hydrateAttachedOffer = async (offerId: number) => {
    if (adapters.getOffer == null) {
      return
    }
    try {
      const offer = await adapters.getOffer(offerId)
      if (!state.isOpen || state.attachedOfferId !== offerId) {
        return
      }
      const draft = catalogOfferDetailToDraft(offer)
      state = {
        ...state,
        attachedOfferTitle: offer.title,
        createOfferDraft: draft,
        editBaselineDraft:
          state.createOfferDrawerMode === "edit" ? draft : state.editBaselineDraft,
        editIssueCount:
          state.createOfferDrawerMode === "edit"
            ? offer.issueCount
            : state.editIssueCount,
      }
      publish()
    } catch {
      // Keep OfferId attach; title/summary stay empty until Edit or next load.
    }
  }

  const loadExistingOfferPicker = async () => {
    if (
      !state.isOpen
      || state.stepId !== "offer"
      || state.offerStanceId !== "existing-offer"
      || !state.existingOfferPickerVisible
      || state.locationId == null
    ) {
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

    const listCatalogOffers = adapters.listCatalogOffers
    if (listCatalogOffers == null) {
      if (generation !== existingOfferPickerLoadGeneration) {
        return
      }
      state = {
        ...state,
        existingOfferPickerLoadStatus: "error",
        existingOfferPickerError: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.loadError,
        existingOfferPickerItems: [],
      }
      publish()
      return
    }

    try {
      const response = await listCatalogOffers(
        buildExistingOfferPickerQueryParams(locationId)
      )
      if (generation !== existingOfferPickerLoadGeneration) {
        return
      }
      if (!state.isOpen || state.stepId !== "offer") {
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
        existingOfferPickerError: CAMPAIGN_EXISTING_OFFER_PICKER_COPY.loadError,
        existingOfferPickerItems: [],
      }
      publish()
    }
  }

  const applyFixturesMessaging = () => {
    const resolved = resolveCampaignMessagingUsage({
      cutover: "fixtures",
      access: state.messagingChromeAccess,
    })
    if (resolved.status !== "ready") {
      return
    }
    state = {
      ...state,
      messagingCutover: "fixtures",
      messagingBalancesStatus: "ready",
      messagingBalancesError: null,
      messagingFixture: resolved.fixture,
      aiAvailable: resolved.aiAvailable,
      softLocked: resolved.softLocked,
      isPilot: resolved.isPilot,
      lockCause: resolved.lockCause,
    }
  }

  const refreshOperatorSenderEmail = async () => {
    if (adapters.getOperatorAccountEmail == null) {
      return
    }
    try {
      const email = (await adapters.getOperatorAccountEmail()) ?? ""
      if (!state.isOpen) {
        return
      }
      const trimmed = email.trim()
      state = {
        ...state,
        operatorSenderEmail: trimmed.length > 0 ? trimmed : null,
      }
      publish()
    } catch {
      // Keep honest "—" on Review when identity load fails.
    }
  }

  const refreshMessagingBalances = async () => {
    const loadMessagingBalances = adapters.loadMessagingBalances
    if (loadMessagingBalances == null) {
      applyFixturesMessaging()
      return
    }

    const generation = ++messagingBalancesGeneration
    try {
      const balances = await loadMessagingBalances()
      if (generation !== messagingBalancesGeneration) {
        return
      }
      const resolved = resolveCampaignMessagingUsage({
        cutover: "live",
        balances,
        access: state.messagingChromeAccess,
      })
      if (resolved.status !== "ready") {
        return
      }
      state = {
        ...state,
        messagingCutover: "live",
        messagingBalancesStatus: "ready",
        messagingBalancesError: null,
        messagingFixture: resolved.fixture,
        aiAvailable: resolved.aiAvailable,
        softLocked: resolved.softLocked,
        isPilot: resolved.isPilot,
        lockCause: resolved.lockCause,
        messagingChromeAccess:
          balances.chromeAccess ?? state.messagingChromeAccess,
      }
      publish()
    } catch {
      if (generation !== messagingBalancesGeneration) {
        return
      }
      const resolved = resolveCampaignMessagingUsage({
        cutover: "live",
        failed: true,
      })
      if (resolved.status !== "load-failed") {
        return
      }
      state = {
        ...state,
        messagingCutover: "live",
        messagingBalancesStatus: "load-failed",
        messagingBalancesError: resolved.errorMessage,
        // No silent fixture fallback after live cutover failure.
        messagingFixture: MESSAGING_USAGE_FIXTURE,
        aiAvailable: null,
        softLocked: false,
        isPilot: false,
        lockCause: null,
      }
      publish()
    }
  }

  const closeWithoutPersist = () => {
    audienceLoadGeneration += 1
    messagingBalancesGeneration += 1
    if (aiAbortController != null) {
      aiAbortController.abort()
      aiAbortController = null
    }
    state = emptyState(defaultChromeAccess)
    publish()
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

  const runAiDraft = async (mode: CampaignMessageDraftMode) => {
    if (
      !state.isOpen
      || state.stepId !== "message"
      || state.locationId == null
      || state.goalId == null
      || adapters.prepareMessageDraft == null
      || state.aiDraftStatus === "running"
    ) {
      return
    }

    const prepareGate = resolveCampaignAiPrepareGate({
      cutover: state.messagingCutover,
      softLocked: state.softLocked,
      aiAvailable: state.aiAvailable,
      balancesStatus: state.messagingBalancesStatus,
    })
    if (!prepareGate.allowed) {
      state = {
        ...state,
        aiDraftStatus: "failed",
        aiDraftMode: mode,
        preparingOverlayOpen: false,
        aiDraftError: prepareGate.blockReason,
        aiDraftRetryable: false,
      }
      publish()
      return
    }

    const locationId = state.locationId
    const channel = state.channelId
    const goalId = state.goalId
    const audienceKey = state.audienceId
    const offerStance = state.offerStanceId
    const campaignName =
      state.draftName != null && state.draftName.trim() !== ""
        ? state.draftName.trim()
        : null
    const priorSubject = state.messageSubject
    const priorMessage = state.messageBody
    const generation = ++state.aiDraftGeneration

    if (aiAbortController != null) {
      aiAbortController.abort()
    }
    const controller = new AbortController()
    aiAbortController = controller

    const isRewrite = isCampaignMessageDraftRewriteMode(mode)

    state = {
      ...state,
      aiDraftStatus: "running",
      aiDraftMode: mode,
      preparingOverlayOpen: mode === "prepare",
      aiDraftError: null,
      aiDraftRetryable: true,
      guestPreviewOpen: false,
    }
    publish()

    const request: PrepareCampaignMessageDraftRequest = {
      locationId,
      channel,
      goalId,
      audienceKey,
      offerStance,
      campaignName,
      tone: CAMPAIGN_MESSAGE_DRAFT_DEFAULT_TONE,
      includeNotes: null,
      mode,
      currentBody: isRewrite ? priorMessage : null,
      currentSubject: isRewrite && channel === "email" ? priorSubject : null,
    }

    try {
      const result = await adapters.prepareMessageDraft(
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
        let debitOutcome: "debited" | "skipped" = "skipped"
        try {
          debitOutcome = await maybeConsumeDirectAiOnUsableDraft({
            cutover: state.messagingCutover,
            usableSuccess: true,
            locationId,
            consumeDirectAi: adapters.consumeDirectAi,
          })
        } catch {
          // Usable draft still applies; Billing retry is out of Campaigns.
        }

        if (
          generation !== state.aiDraftGeneration
          || controller.signal.aborted
        ) {
          return
        }

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
          messageWriteEntry: "editor",
          messageSubject: nextSubject,
          messageBody: nextMessage,
          aiDraftStatus: "idle",
          aiDraftMode: null,
          preparingOverlayOpen: false,
          aiDraftError: null,
          aiDraftRetryable: true,
          aiActionCount: state.aiActionCount + 1,
          aiAvailable:
            debitOutcome === "debited" && state.aiAvailable != null
              ? Math.max(0, state.aiAvailable - 1)
              : state.aiAvailable,
        }
        publish()
        return
      }

      state = {
        ...state,
        messageSubject: mode === "prepare" ? "" : priorSubject,
        messageBody: mode === "prepare" ? "" : priorMessage,
        aiDraftStatus: "failed",
        preparingOverlayOpen: false,
        aiDraftError: CAMPAIGN_MESSAGE_AI_DRAFT_ERROR,
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
        messageSubject: mode === "prepare" ? "" : priorSubject,
        messageBody: mode === "prepare" ? "" : priorMessage,
        aiDraftStatus: "failed",
        preparingOverlayOpen: false,
        aiDraftError: CAMPAIGN_MESSAGE_AI_DRAFT_ERROR,
        aiDraftRetryable: true,
      }
      publish()
    } finally {
      if (aiAbortController === controller) {
        aiAbortController = null
      }
    }
  }

  const persistDraft = async (): Promise<boolean> => {
    if (!state.isOpen || state.locationId == null) {
      return false
    }

    // Create needs a name source — goal label or template title (ticket 12 / 29).
    if (
      state.draftId == null
      && state.goalId == null
      && state.templateId == null
    ) {
      state = {
        ...state,
        saveStatus: "error",
        saveError: CAMPAIGN_DRAFT_SAVE_ERROR_MESSAGE,
      }
      publish()
      return false
    }

    const locationId = state.locationId
    const draftId = state.draftId
    const draftRowVersion = state.draftRowVersion
    if (!audienceCanPersist(state)) {
      state = {
        ...state,
        saveStatus: "error",
        saveError: CAMPAIGN_DRAFT_SAVE_ERROR_MESSAGE,
      }
      publish()
      return false
    }
    const fields = buildDraftFields(state)
    const isCreate = draftId == null

    if (isCreate && adapters.createDraft == null) {
      return false
    }
    if (!isCreate && adapters.updateDraft == null) {
      return false
    }

    state = {
      ...state,
      saveStatus: "saving",
      saveError: null,
    }
    publish()

    try {
      const saved = isCreate
        ? await adapters.createDraft!({
            locationId,
            ...(state.draftName != null && state.draftName.trim().length > 0
              ? { name: state.draftName.trim() }
              : {}),
            goalId: fields.goalId,
            templateId: fields.templateId,
            templateVersion: fields.templateVersion,
            audienceKey: fields.audienceKey,
            channel: fields.channel,
            offerStance: fields.offerStance,
            offerId: fields.offerId,
            messageSubject: fields.messageSubject,
            messageBody: fields.messageBody,
          })
        : await adapters.updateDraft!(draftId, {
            rowVersion: draftRowVersion ?? "",
            goalId: fields.goalId,
            templateId: fields.templateId,
            templateVersion: fields.templateVersion,
            audienceKey: fields.audienceKey,
            channel: fields.channel,
            offerStance: fields.offerStance,
            offerId: fields.offerId,
            messageSubject: fields.messageSubject,
            messageBody: fields.messageBody,
          })

      state = {
        ...state,
        draftId: saved.id,
        draftRowVersion: saved.rowVersion,
        templateId: saved.templateId,
        templateVersion: saved.templateVersion,
        saveStatus: "saved",
        saveError: null,
        lastSavedAt: getNow(),
      }
      publish()
      return true
    } catch (error) {
      const saveError =
        error instanceof CampaignDraftHttp409Error
          ? error.message
          : CAMPAIGN_DRAFT_SAVE_ERROR_MESSAGE
      state = {
        ...state,
        saveStatus: "error",
        saveError,
      }
      publish()
      return false
    }
  }

  const loadAudienceCounts = async () => {
    const locationId = state.locationId
    const loadSmartGroupCounts = adapters.loadSmartGroupCounts
    const loadAudienceEligibility = adapters.loadAudienceEligibility

    if (locationId == null) {
      state = {
        ...state,
        audienceLoadStatus: "loaded",
        liveCounts: null,
        eligibilityByAudienceId: {},
      }
      publish()
      return
    }

    const generation = ++audienceLoadGeneration
    state = {
      ...state,
      audienceLoadStatus: "loading",
      liveCounts: null,
      eligibilityByAudienceId: {},
    }
    publish()

    try {
      const evaluableIds = evaluableCampaignAudienceIds()
      const smartGroupPromise =
        loadSmartGroupCounts != null
          ? loadSmartGroupCounts({ locationId })
          : Promise.resolve(null)

      const eligibilitySettled =
        loadAudienceEligibility == null
          ? Promise.resolve(
              evaluableIds.map(() => ({
                status: "rejected" as const,
                reason: new Error("Campaign eligibility adapter missing."),
              }))
            )
          : Promise.allSettled(
              evaluableIds.map((audienceKey) =>
                loadAudienceEligibility({ locationId, audienceKey })
              )
            )

      const [liveCounts, eligibilityResults] = await Promise.all([
        smartGroupPromise,
        eligibilitySettled,
      ])

      if (generation !== audienceLoadGeneration) {
        return
      }

      const eligibilityByAudienceId: Partial<
        Record<CampaignAudienceId, CampaignAudienceEligibilityBreakdown>
      > = {}

      for (let index = 0; index < evaluableIds.length; index += 1) {
        const audienceKey = evaluableIds[index]!
        const result = eligibilityResults[index]
        if (result == null || result.status === "rejected") {
          eligibilityByAudienceId[audienceKey] =
            errorCampaignAudienceEligibilityBreakdown()
          continue
        }
        eligibilityByAudienceId[audienceKey] = result.value
      }

      for (const option of CAMPAIGN_AUDIENCE_OPTIONS) {
        if (option.unevaluable) {
          eligibilityByAudienceId[option.id] =
            unavailableCampaignAudienceEligibilityBreakdown()
        }
      }

      state = {
        ...state,
        audienceLoadStatus: "loaded",
        liveCounts,
        eligibilityByAudienceId,
      }
      publish()
    } catch {
      if (generation !== audienceLoadGeneration) {
        return
      }
      state = {
        ...state,
        audienceLoadStatus: "error",
        liveCounts: null,
        eligibilityByAudienceId: {},
      }
      publish()
    }
  }

  const runConfirmCommit = async () => {
    if (
      !state.isOpen
      || state.stepId !== "review"
      || !state.commitConfirmOpen
      || state.commitStatus === "saving"
      || adapters.commitCampaign == null
      || !canCommitCampaign(state, commitCampaignWired, getNow())
    ) {
      return
    }

    state = {
      ...state,
      commitStatus: "saving",
      commitError: null,
    }
    publish()

    const saved = await persistDraft()
    if (!saved || state.draftId == null || state.draftRowVersion == null) {
      state = {
        ...state,
        commitStatus: "error",
        commitError:
          state.saveError ?? CAMPAIGN_COMMIT_COPY.reserveFailedDefault,
      }
      publish()
      return
    }

    const modeId = state.scheduleModeId
    const scheduledAtUtc =
      modeId === "schedule-later"
        ? campaignScheduledAtUtcIso({
            dateLocal: state.scheduleDateLocal,
            timeLocal: state.scheduleTimeLocal,
          })
        : null

    if (modeId === "schedule-later" && scheduledAtUtc == null) {
      state = {
        ...state,
        commitStatus: "error",
        commitError: CAMPAIGN_SCHEDULE_COPY.datetimeRequired,
      }
      publish()
      return
    }

    const body: CommitCampaignScheduleRequest = {
      rowVersion: state.draftRowVersion,
      scheduleMode: modeId,
      scheduleTimeZone: state.scheduleTimeZone,
      ...(modeId === "schedule-later"
        ? { scheduledAtUtc }
        : { scheduledAtUtc: null }),
    }

    const campaignId = state.draftId
    const campaignName =
      state.draftName != null && state.draftName.trim().length > 0
        ? state.draftName.trim()
        : labelForCampaignGoalId(state.goalId) ?? "Campaign"

    try {
      const committed = await adapters.commitCampaign({
        campaignId,
        body,
      })
      const committedAt = getNow()
      state = {
        ...state,
        draftId: committed.id,
        draftRowVersion: committed.rowVersion,
        commitConfirmOpen: false,
        commitStatus: "idle",
        commitError: null,
        commitSuccess: {
          modeId,
          campaignName: committed.name || campaignName,
          scheduledAtUtc: committed.scheduledAtUtc,
          committedAt,
        },
        stepId: "success",
      }
      publish()
    } catch (error) {
      let commitError: string = CAMPAIGN_COMMIT_COPY.reserveFailedDefault
      if (isCampaignBillingReserveUnavailableError(error)) {
        commitError = resolveBillingReserveUnavailableCopy({
          billingReserveLive,
        })
      } else if (error instanceof Error && error.message.trim().length > 0) {
        commitError = error.message.trim()
      }
      state = {
        ...state,
        commitStatus: "error",
        commitError,
      }
      publish()
    }
  }

  async function executeUpdateOffer(): Promise<ConfirmCatalogOfferWriteResult> {
    if (
      !state.isOpen
      || state.stepId !== "offer"
      || state.locationId == null
      || !state.createOfferPanelOpen
      || state.createOfferDrawerMode !== "edit"
      || adapters.updateOffer == null
      || state.attachedOfferId == null
      || state.createOfferStatus === "saving"
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

    const offerId = state.attachedOfferId
    state = {
      ...state,
      createOfferStatus: "saving",
      createOfferError: null,
    }
    publish()

    try {
      const offer = await adapters.updateOffer(offerId, body)
      state = {
        ...state,
        offerStanceId: "create-new-offer",
        attachedOfferId: offer.id,
        attachedOfferTitle: offer.title,
        createOfferPanelOpen: false,
        createOfferStatus: "idle",
        createOfferError: null,
        editBaselineDraft: null,
        editIssueCount: 0,
        pendingEditOfferSave: null,
      }
      publish()
      return "updated"
    } catch {
      state = {
        ...state,
        createOfferStatus: "error",
        createOfferError: CREATE_EDIT_OFFER_DRAWER_COPY.updateOfferError,
      }
      publish()
      return "error"
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
    openBlankCreate(input) {
      audienceLoadGeneration += 1
      state = {
        ...emptyState(defaultChromeAccess),
        isOpen: true,
        locationId: input.locationId,
        locationName: input.locationName,
        locationAddress: normalizeLocationAddress(input.locationAddress),
        stepId: "goal",
        openedAt: getNow(),
      }
      publish()
      void refreshMessagingBalances()
      void refreshOperatorSenderEmail()
    },
    async openFromTemplate(input) {
      audienceLoadGeneration += 1
      const suggestions = mapCampaignTemplateSuggestions(
        input.template.suggestions
      )
      state = {
        ...emptyState(defaultChromeAccess),
        isOpen: true,
        locationId: input.locationId,
        locationName: input.locationName,
        locationAddress: normalizeLocationAddress(input.locationAddress),
        templateId: input.template.id,
        templateVersion: input.template.version,
        stepId: "audience",
        goalId: suggestions.goalId,
        openedAt: getNow(),
        audienceId: suggestions.audienceId,
        channelId: suggestions.channelId,
        offerStanceId: suggestions.offerStanceId,
      }
      publish()
      await Promise.all([
        loadAudienceCounts(),
        refreshMessagingBalances(),
        refreshOperatorSenderEmail(),
      ])
    },
    async openFromDraft(input) {
      audienceLoadGeneration += 1
      const draft = input.draft
      const resolved = mapCampaignTemplateSuggestions({
        goalId: draft.goalId ?? "custom-campaign",
        audienceKey: draft.audienceKey ?? "all-eligible-guests",
        channel: draft.channel ?? defaultCampaignChannelId(),
        offerStance: draft.offerStance ?? defaultCampaignOfferStanceId(),
      })
      const goalId =
        draft.goalId == null ? null : resolved.goalId
      const hasMessageContent =
        (draft.messageBody?.trim().length ?? 0) > 0
        || (draft.messageSubject?.trim().length ?? 0) > 0
      // Resume at first incomplete step from persisted fields (schedule is not stored).
      // Assistant Change audience / Add Offer may force Audience or Offer.
      const forceNoOfferLand =
        input.startStep === "offer" && draft.offerId == null
      const stepId: CampaignWizardStepId =
        input.startStep === "audience"
          ? "audience"
          : input.startStep === "offer"
            ? "offer"
            : input.startStep === "schedule"
              ? "schedule"
              : input.startStep === "review"
                ? "review"
                : goalId == null
                  ? "goal"
                  : hasMessageContent
                    ? "schedule"
                    : "audience"

      const offerStanceId = forceNoOfferLand
        ? "no-offer"
        : resolved.offerStanceId
      const attachedOfferId = forceNoOfferLand ? null : draft.offerId

      state = {
        ...emptyState(defaultChromeAccess),
        isOpen: true,
        locationId: draft.locationId,
        locationName: input.locationName,
        locationAddress: normalizeLocationAddress(input.locationAddress),
        templateId: draft.templateId,
        templateVersion: draft.templateVersion,
        draftId: draft.id,
        draftRowVersion: draft.rowVersion,
        stepId,
        goalId,
        openedAt: getNow(),
        audienceId: resolved.audienceId,
        channelId: resolved.channelId,
        offerStanceId,
        attachedOfferId,
        attachedOfferTitle: null,
        createOfferPanelOpen: false,
        messageWriteEntry: hasMessageContent ? "editor" : "chooser",
        messageSubject: draft.messageSubject ?? "",
        messageBody: draft.messageBody ?? "",
        scheduleModeId: input.scheduleMode
          ?? (input.startStep === "schedule"
            ? "schedule-later"
            : defaultCampaignScheduleModeId()),
        scheduleDateLocal: input.dateLocal ?? "",
        scheduleTimeLocal: input.timeLocal ?? "",
        lastSavedAt: getNow(),
        saveStatus: "saved",
      }
      publish()
      if (!forceNoOfferLand && draft.offerId != null) {
        await hydrateAttachedOffer(draft.offerId)
      }
      if (stepId === "audience" || stepId === "offer") {
        await Promise.all([
          loadAudienceCounts(),
          refreshMessagingBalances(),
          refreshOperatorSenderEmail(),
        ])
      } else {
        await Promise.all([
          refreshMessagingBalances(),
          refreshOperatorSenderEmail(),
        ])
      }
    },
    async openFromRecommendation(input) {
      audienceLoadGeneration += 1
      const prefill = input.draftPrefill
      const resolved = mapCampaignTemplateSuggestions({
        goalId: prefill.goalId,
        audienceKey: prefill.audienceKey,
        channel: prefill.channel,
        offerStance: prefill.offerStance,
      })
      const hasMessageContent =
        prefill.messageBody.trim().length > 0
        || (prefill.messageSubject?.trim().length ?? 0) > 0
      const draftName = prefill.campaignName.trim()

      state = {
        ...emptyState(defaultChromeAccess),
        isOpen: true,
        locationId: input.locationId,
        locationName: input.locationName,
        locationAddress: normalizeLocationAddress(input.locationAddress),
        stepId: "audience",
        goalId: resolved.goalId,
        openedAt: getNow(),
        audienceId: resolved.audienceId,
        channelId: resolved.channelId,
        offerStanceId: resolved.offerStanceId,
        messageWriteEntry: hasMessageContent ? "editor" : "chooser",
        messageSubject: prefill.messageSubject ?? "",
        messageBody: prefill.messageBody,
        draftName: draftName.length > 0 ? draftName : null,
      }
      publish()
      await Promise.all([
        loadAudienceCounts(),
        refreshMessagingBalances(),
        refreshOperatorSenderEmail(),
      ])
    },
    close() {
      closeWithoutPersist()
    },
    async save() {
      await persistDraft()
    },
    async saveAndExit() {
      if (adapters.createDraft == null && state.draftId == null) {
        // No persist adapter — exit without creating a server Draft.
        closeWithoutPersist()
        return
      }
      const persisted = await persistDraft()
      if (persisted) {
        closeWithoutPersist()
      }
    },
    setGoalId(goalId) {
      if (!state.isOpen || state.stepId !== "goal") {
        return
      }
      state = { ...state, goalId }
      publish()
    },
    setAudienceId(audienceId) {
      if (!state.isOpen || state.stepId !== "audience") {
        return
      }
      state = {
        ...state,
        audienceId,
      }
      publish()
    },
    setChannelId(channelId) {
      if (!state.isOpen || state.stepId !== "channel") {
        return
      }
      state = { ...state, channelId }
      publish()
    },
    setOfferStanceId(stanceId) {
      if (!state.isOpen || state.stepId !== "offer") {
        return
      }
      const option = CAMPAIGN_OFFER_OPTIONS.find((item) => item.id === stanceId)
      if (option == null || option.disabled) {
        return
      }
      if (stanceId === "no-offer") {
        existingOfferPickerLoadGeneration += 1
        state = clearExistingOfferPicker({
          ...state,
          offerStanceId: stanceId,
          attachedOfferId: null,
          attachedOfferTitle: null,
          createOfferPanelOpen: false,
          createOfferStatus: "idle",
          createOfferError: null,
        })
        publish()
        return
      }
      if (stanceId === "create-new-offer") {
        existingOfferPickerLoadGeneration += 1
        state = clearExistingOfferPicker({
          ...state,
          offerStanceId: stanceId,
          createOfferPanelOpen: true,
          createOfferDrawerMode: "create",
          createOfferDraft:
            state.attachedOfferId != null
              ? state.createOfferDraft
              : emptyCampaignCatalogOfferDetailsDraft(),
          createOfferStatus: "idle",
          createOfferError: null,
        })
        publish()
        return
      }
      // existing-offer — open (or re-open) inline picker; keep attach until Select.
      state = {
        ...state,
        offerStanceId: stanceId,
        existingOfferPickerVisible: true,
        existingOfferPickerSearchQuery: "",
        createOfferPanelOpen: false,
        createOfferStatus: "idle",
        createOfferError: null,
      }
      publish()
      void loadExistingOfferPicker()
    },
    openCreateOfferPanel() {
      if (!state.isOpen || state.stepId !== "offer") {
        return
      }
      existingOfferPickerLoadGeneration += 1
      state = clearExistingOfferPicker({
        ...state,
        offerStanceId: "create-new-offer",
        createOfferPanelOpen: true,
        createOfferDrawerMode: "create",
        createOfferStatus: "idle",
        createOfferError: null,
        editBaselineDraft: null,
        editIssueCount: 0,
        pendingEditOfferSave: null,
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
        pendingEditOfferSave: null,
      }
      publish()
    },
    async editAttachedOffer() {
      if (!state.isOpen || state.stepId !== "offer") {
        return
      }
      if (state.attachedOfferId == null) {
        return
      }
      const offerId = state.attachedOfferId
      existingOfferPickerLoadGeneration += 1
      state = clearExistingOfferPicker({
        ...state,
        offerStanceId: "create-new-offer",
        createOfferPanelOpen: true,
        createOfferDrawerMode: "edit",
        createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
        createOfferStatus: "idle",
        createOfferError: null,
        editBaselineDraft: null,
        editIssueCount: 0,
        pendingEditOfferSave: null,
      })
      publish()
      await hydrateAttachedOffer(offerId)
    },
    patchCreateOfferDraft(patch) {
      if (!state.isOpen || state.stepId !== "offer") {
        return
      }
      if (!state.createOfferPanelOpen) {
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
        || state.stepId !== "offer"
        || state.locationId == null
        || !state.createOfferPanelOpen
        || state.createOfferStatus === "saving"
        || state.pendingEditOfferSave != null
      ) {
        return "noop"
      }

      if (state.createOfferDrawerMode === "edit") {
        if (
          adapters.updateOffer == null
          || state.attachedOfferId == null
          || !canConfirmCampaignCatalogOfferDetails(state.createOfferDraft)
        ) {
          return "noop"
        }

        const dirtyBenefitOrValidity =
          state.editBaselineDraft != null
          && isDirtyBenefitOrValidity(
            state.editBaselineDraft,
            state.createOfferDraft
          )
        if (
          shouldConfirmEditOfferSave({
            issueCount: state.editIssueCount,
            dirtyBenefitOrValidity,
          })
        ) {
          state = {
            ...state,
            pendingEditOfferSave: {
              title: CREATE_EDIT_OFFER_DRAWER_COPY.editSaveConfirmTitle,
              description:
                CREATE_EDIT_OFFER_DRAWER_COPY.editSaveConfirmDescription,
            },
          }
          publish()
          return "awaiting-edit-confirm"
        }

        return await executeUpdateOffer()
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

      state = {
        ...state,
        createOfferStatus: "saving",
        createOfferError: null,
      }
      publish()

      try {
        const offer = await adapters.createOffer(body)
        existingOfferPickerLoadGeneration += 1
        state = clearExistingOfferPicker({
          ...state,
          offerStanceId: "create-new-offer",
          attachedOfferId: offer.id,
          attachedOfferTitle: offer.title,
          createOfferPanelOpen: false,
          createOfferStatus: "idle",
          createOfferError: null,
          editBaselineDraft: null,
          editIssueCount: 0,
          pendingEditOfferSave: null,
        })
        publish()
        return "created"
      } catch {
        state = {
          ...state,
          createOfferStatus: "error",
          createOfferError: CAMPAIGN_OFFER_COPY.createOfferError,
        }
        publish()
        return "error"
      }
    },
    setExistingOfferSearch(query) {
      if (
        !state.isOpen
        || state.stepId !== "offer"
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
        !state.isOpen
        || state.stepId !== "offer"
        || state.offerStanceId !== "existing-offer"
        || !state.existingOfferPickerVisible
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
        attachedOfferId: item.id,
        attachedOfferTitle: item.title,
      })
      publish()
    },
    async retryExistingOfferPicker() {
      if (
        !state.isOpen
        || state.stepId !== "offer"
        || state.offerStanceId !== "existing-offer"
        || !state.existingOfferPickerVisible
      ) {
        return
      }
      await loadExistingOfferPicker()
    },
    createNewOfferFromExistingPicker() {
      if (
        !state.isOpen
        || state.stepId !== "offer"
        || state.offerStanceId !== "existing-offer"
      ) {
        return
      }
      existingOfferPickerLoadGeneration += 1
      state = clearExistingOfferPicker({
        ...state,
        offerStanceId: "create-new-offer",
        createOfferPanelOpen: true,
        createOfferDrawerMode: "create",
        createOfferStatus: "idle",
        createOfferError: null,
      })
      publish()
    },
    async confirmPendingEditOfferSave() {
      if (state.pendingEditOfferSave == null) {
        return "noop"
      }
      state = {
        ...state,
        pendingEditOfferSave: null,
      }
      publish()
      return await executeUpdateOffer()
    },
    cancelPendingEditOfferSave() {
      if (state.pendingEditOfferSave == null) {
        return
      }
      state = {
        ...state,
        pendingEditOfferSave: null,
      }
      publish()
    },
    setScheduleModeId(modeId) {
      if (!state.isOpen || state.stepId !== "schedule") {
        return
      }
      state = { ...state, scheduleModeId: modeId }
      publish()
    },
    setScheduleDateLocal(value) {
      if (!state.isOpen || state.stepId !== "schedule") {
        return
      }
      state = { ...state, scheduleDateLocal: value }
      publish()
    },
    setScheduleTimeLocal(value) {
      if (!state.isOpen || state.stepId !== "schedule") {
        return
      }
      state = { ...state, scheduleTimeLocal: value }
      publish()
    },
    writeManually() {
      if (!state.isOpen || state.stepId !== "message") {
        return
      }
      if (aiAbortController != null) {
        aiAbortController.abort()
        aiAbortController = null
      }
      state = clearSendTestDialog({
        ...state,
        messageWriteEntry: "editor",
        guestPreviewOpen: false,
        aiDraftGeneration: state.aiDraftGeneration + 1,
      })
      clearAiDraftUi()
      publish()
    },
    async prepareDraft() {
      await runAiDraft("prepare")
    },
    async rewriteDraft(target) {
      await runAiDraft(
        target === "subject" ? "rewrite_subject" : "rewrite_message"
      )
    },
    async retryAiDraft() {
      const mode = state.aiDraftMode
      if (mode == null || state.aiDraftStatus !== "failed") {
        return
      }
      await runAiDraft(mode)
    },
    dismissPreparingOverlay() {
      if (!state.isOpen || state.stepId !== "message") {
        return
      }
      if (!state.preparingOverlayOpen) {
        return
      }
      state = { ...state, preparingOverlayOpen: false }
      publish()
    },
    async retryMessagingBalances() {
      if (!state.isOpen) {
        return
      }
      await refreshMessagingBalances()
    },
    setSubject(value) {
      if (!state.isOpen || state.stepId !== "message") {
        return
      }
      if (state.messageWriteEntry !== "editor") {
        return
      }
      state = { ...state, messageSubject: value }
      publish()
    },
    setMessage(value) {
      if (!state.isOpen || state.stepId !== "message") {
        return
      }
      if (state.messageWriteEntry !== "editor") {
        return
      }
      state = { ...state, messageBody: value }
      publish()
    },
    openGuestPreview() {
      if (!state.isOpen) {
        return
      }
      if (state.stepId === "message") {
        if (state.messageWriteEntry !== "editor") {
          return
        }
        state = { ...state, guestPreviewOpen: true }
        publish()
        return
      }
      if (state.stepId === "review") {
        state = { ...state, guestPreviewOpen: true }
        publish()
      }
    },
    closeGuestPreview() {
      if (!state.isOpen) {
        return
      }
      if (state.stepId !== "message" && state.stepId !== "review") {
        return
      }
      state = clearSendTestDialog({
        ...state,
        guestPreviewOpen: false,
      })
      publish()
    },
    editMessageFromReview() {
      if (!state.isOpen || state.stepId !== "review") {
        return
      }
      state = clearSendTestDialog({
        ...state,
        stepId: "message",
        messageWriteEntry: "editor",
        guestPreviewOpen: false,
      })
      publish()
    },
    async openSendTestDialog() {
      // Send test is on the Review/Message rail and the full Guest preview.
      // Do not require guestPreviewOpen — the rail button must open the dialog.
      if (
        !state.isOpen
        || !isSendTestAvailable(state, sendCampaignTestWired)
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
        !state.isOpen
        || !isSendTestAvailable(state, sendCampaignTestWired)
      ) {
        return
      }

      const trimmed = email.trim()
      state = {
        ...state,
        sendTestDialogOpen: true,
        sendTestEmail: email,
        sendTestStatus: "idle",
        sendTestError: null,
        operatorSenderEmail:
          trimmed.length > 0 ? trimmed : state.operatorSenderEmail,
      }
      publish()
    },
    closeSendTestDialog() {
      if (!state.isOpen || !state.sendTestDialogOpen) {
        return
      }
      state = clearSendTestDialog(state)
      publish()
    },
    setSendTestEmail(value) {
      if (!state.isOpen || !state.sendTestDialogOpen) {
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
        !state.isOpen
        || !state.sendTestDialogOpen
        || state.locationId == null
        || adapters.sendCampaignTest == null
        || !isSendTestAvailable(state, sendCampaignTestWired)
        || state.sendTestStatus === "sending"
      ) {
        return
      }

      const toEmail = state.sendTestEmail.trim()
      const subject = state.messageSubject.trim()
      const body = state.messageBody.trim()
      if (toEmail.length === 0 || subject.length === 0 || body.length === 0) {
        state = {
          ...state,
          sendTestStatus: "error",
          sendTestError: CAMPAIGN_SEND_TEST_COPY.errorMessage,
        }
        publish()
        return
      }

      const locationId = state.locationId
      const offerStanceId = state.offerStanceId
      state = {
        ...state,
        sendTestStatus: "sending",
        sendTestError: null,
      }
      publish()

      const request: CampaignSendTestRequest = {
        locationId,
        toEmail,
        subject,
        body,
        ...(offerStanceId !== "no-offer"
          ? { offer: { ...CAMPAIGN_SEND_TEST_SAMPLE_OFFER } }
          : {}),
      }

      try {
        await adapters.sendCampaignTest(request)
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
          sendTestError: CAMPAIGN_SEND_TEST_COPY.errorMessage,
        }
        publish()
      }
    },
    openCommitConfirm() {
      if (
        !state.isOpen
        || state.stepId !== "review"
        || !canCommitCampaign(state, commitCampaignWired, getNow())
      ) {
        return
      }
      state = {
        ...state,
        commitConfirmOpen: true,
        commitStatus: "idle",
        commitError: null,
      }
      publish()
    },
    cancelCommitConfirm() {
      if (!state.isOpen || !state.commitConfirmOpen) {
        return
      }
      if (state.commitStatus === "saving") {
        return
      }
      state = {
        ...state,
        commitConfirmOpen: false,
        commitStatus: "idle",
        commitError: null,
      }
      publish()
    },
    async confirmCommit() {
      await runConfirmCommit()
    },
    async scheduleCommit() {
      await runConfirmCommit()
    },
    dismissSuccess() {
      if (!state.isOpen || state.stepId !== "success") {
        return
      }
      closeWithoutPersist()
    },
    async continue() {
      if (!state.isOpen) {
        return
      }
      if (state.stepId === "success") {
        return
      }
      if (state.stepId === "goal") {
        if (state.goalId == null) {
          return
        }
        state = {
          ...state,
          stepId: "audience",
          audienceId: DEFAULT_AUDIENCE_ID,
          audienceLoadStatus: "idle",
          liveCounts: null,
          eligibilityByAudienceId: {},
        }
        publish()
        await loadAudienceCounts()
        return
      }
      if (state.stepId === "audience") {
        if (!audienceCanContinue(state)) {
          return
        }
      }
      if (state.stepId === "offer") {
        if (!offerCanContinue(state)) {
          return
        }
      }
      if (state.stepId === "message") {
        if (!messageCanContinue(state)) {
          return
        }
        state = clearSendTestDialog({
          ...state,
          guestPreviewOpen: false,
        })
      }
      if (state.stepId === "schedule") {
        if (
          !canContinueCampaignSchedule({
            modeId: state.scheduleModeId,
            dateLocal: state.scheduleDateLocal,
            timeLocal: state.scheduleTimeLocal,
            now: getNow(),
          })
        ) {
          return
        }
      }
      if (state.stepId === "review") {
        if (!canCommitCampaign(state, commitCampaignWired, getNow())) {
          return
        }
        state = {
          ...state,
          commitConfirmOpen: true,
          commitStatus: "idle",
          commitError: null,
        }
        publish()
        return
      }
      const index = NUMBERED_STEP_ORDER.indexOf(state.stepId)
      if (index < 0 || index >= NUMBERED_STEP_ORDER.length - 1) {
        return
      }
      state = { ...state, stepId: NUMBERED_STEP_ORDER[index + 1]! }
      publish()
    },
    back() {
      if (!state.isOpen) {
        return
      }
      if (state.stepId === "success") {
        closeWithoutPersist()
        return
      }
      if (state.stepId === "goal") {
        closeWithoutPersist()
        return
      }
      if (state.stepId === "message" || state.stepId === "review") {
        state = clearSendTestDialog({
          ...state,
          guestPreviewOpen: false,
          commitConfirmOpen: false,
          commitStatus: "idle",
          commitError: null,
        })
      }
      const index = NUMBERED_STEP_ORDER.indexOf(state.stepId)
      if (index <= 0) {
        state = {
          ...state,
          stepId: "goal",
          audienceLoadStatus: "idle",
          liveCounts: null,
          eligibilityByAudienceId: {},
        }
        publish()
        return
      }
      state = { ...state, stepId: NUMBERED_STEP_ORDER[index - 1]! }
      publish()
    },
  }
}

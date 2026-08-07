import {
  CAMPAIGN_AUDIENCE_COPY,
  CAMPAIGN_AUDIENCE_OPTIONS,
  formatAudienceMatchedEligibleLabel,
  mockCampaignAudienceEligibilityBreakdown,
  resolveAudienceCardCounts,
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
  resolveCampaignChannelSmsShortfall,
  type CampaignChannelId,
  type CampaignChannelSmsShortfall,
  type CampaignChannelUsageRow,
} from "@/lib/operatorCampaigns/campaignChannelPresentation"
import {
  CAMPAIGN_MESSAGE_COPY,
  type CampaignMessageWriteEntry,
} from "@/lib/operatorCampaigns/campaignMessagePresentation"
import {
  CAMPAIGN_OFFER_COPY,
  CAMPAIGN_OFFER_OPTIONS,
  defaultCampaignOfferStanceId,
  type CampaignOfferStanceId,
} from "@/lib/operatorCampaigns/campaignOfferPresentation"
import {
  CAMPAIGN_REVIEW_COPY,
  CAMPAIGN_REVIEW_SECTIONS,
  type CampaignReviewSectionId,
} from "@/lib/operatorCampaigns/campaignReviewPresentation"
import {
  CAMPAIGN_SCHEDULE_COPY,
  CAMPAIGN_SCHEDULE_OPTIONS,
  defaultCampaignScheduleModeId,
  labelForCampaignScheduleModeId,
  type CampaignScheduleModeId,
} from "@/lib/operatorCampaigns/campaignSchedulePresentation"
import {
  CAMPAIGN_GOAL_OPTIONS,
  CAMPAIGN_WIZARD_COPY,
  CAMPAIGN_WIZARD_NUMBERED_STEPS,
  formatCampaignWizardHeaderSubtitle,
  labelForCampaignGoalId,
  type CampaignGoalId,
  type CampaignGoalOption,
  type CampaignWizardStepId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import { mapCampaignTemplateSuggestions } from "@/lib/operatorCampaigns/mapCampaignTemplateSuggestions"
import {
  MESSAGING_USAGE_FIXTURE,
  type MessagingUsageFixture,
} from "@/lib/operatorCampaigns/messagingUsageFixtures"
import type {
  CampaignDraftDetail,
  CampaignTemplateDetail,
  CreateCampaignDraftRequest,
  PatchCampaignDraftRequest,
} from "@/types/operatorCampaigns"

export type CampaignWizardOpenBlankInput = {
  locationId: number
  locationName: string
}

export type CampaignWizardOpenFromTemplateInput = {
  locationId: number
  locationName: string
  template: CampaignTemplateDetail
}

export type CampaignWizardOpenFromDraftInput = {
  locationName: string
  draft: CampaignDraftDetail
}

export type CampaignWizardAdapters = {
  getNow?: () => Date
  /** Live Smart Group aggregates for Audience — omitted until step loads. */
  loadSmartGroupCounts?: (input: {
    locationId: number
  }) => Promise<CampaignAudienceSmartGroupCountsInput>
  /** Persist Draft on Save / Save and exit (ticket 29). */
  createDraft?: (
    body: CreateCampaignDraftRequest
  ) => Promise<CampaignDraftDetail>
  updateDraft?: (
    id: number,
    body: PatchCampaignDraftRequest
  ) => Promise<CampaignDraftDetail>
}

export type CampaignWizardGoalCardViewModel = CampaignGoalOption & {
  selected: boolean
}

export type CampaignAudienceOptionViewModel = {
  id: CampaignAudienceId
  title: string
  description: string
  recommended: boolean
  selected: boolean
  deferredOfferGroup: boolean
  matched: number
  currentlyEligible: number
  countLabel: string
  countSource: CampaignAudienceCountSource
}

export type CampaignAudienceViewModel = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  selectedAudienceId: CampaignAudienceId
  savedGroupId: string | null
  options: CampaignAudienceOptionViewModel[]
  eligibilityBreakdown: CampaignAudienceEligibilityBreakdown
  showSavedGroupPicker: boolean
  savedGroupOptions: readonly { value: string; label: string }[]
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
  /** Raw shared overview fixture — Channel must not invent a second source. */
  messagingFixture: MessagingUsageFixture
  smsShortfall: CampaignChannelSmsShortfall | null
}

export type CampaignOfferOptionViewModel = {
  id: CampaignOfferStanceId
  title: string
  description: string
  selected: boolean
}

export type CampaignOfferViewModel = {
  selectedStanceId: CampaignOfferStanceId
  /** Always null in slice 1 — no live catalog / Offers CRUD. */
  attachedOfferId: string | null
  stepHeading: string
  stepDescription: string
  options: CampaignOfferOptionViewModel[]
  usageSummary: {
    title: string
    audienceLine: string
    rows: CampaignChannelUsageRow[]
  }
  /** Same shared overview fixture as Channel (ticket 19 / 24). */
  messagingFixture: MessagingUsageFixture
}

export type CampaignMessageViewModel = {
  writeEntry: CampaignMessageWriteEntry
  subject: string
  body: string
  channelId: CampaignChannelId
  showSubject: boolean
  /** False until ticket 33 wires live prepare / rewrite. */
  prepareAiLive: boolean
  guestPreviewOpen: boolean
  /** Always false in ticket 26 — no send-test path. */
  sendTestAvailable: boolean
  stepHeading: string
  stepDescription: string
  locationName: string
  usageSummary: {
    title: string
    audienceLine: string
    rows: CampaignChannelUsageRow[]
  }
  messagingFixture: MessagingUsageFixture
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
  usageSummary: {
    title: string
    audienceLine: string
    rows: CampaignChannelUsageRow[]
  }
  messagingFixture: MessagingUsageFixture
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
  guestPreviewOpen: boolean
  /** Always false in ticket 27 — no send-test path. */
  sendTestAvailable: boolean
}

export type CampaignReviewViewModel = {
  stepHeading: string
  stepDescription: string
  /** Always false — Review cannot send / approve / schedule-commit. */
  sendAvailable: boolean
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
  /** Footer primary label — Review shows Send chrome but stays disabled. */
  primaryActionLabel: string
  placeholderBody: string | null
  audience: CampaignAudienceViewModel | null
  channel: CampaignChannelViewModel | null
  offer: CampaignOfferViewModel | null
  message: CampaignMessageViewModel | null
  schedule: CampaignScheduleViewModel | null
  review: CampaignReviewViewModel | null
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
   * Opens at Goal when goalId is missing; otherwise Audience.
   */
  openFromDraft: (input: CampaignWizardOpenFromDraftInput) => Promise<void>
  close: () => void
  /** Persist editable fields; keep wizard open. */
  save: () => Promise<void>
  /** Persist editable fields then close. Close without Save creates no row. */
  saveAndExit: () => Promise<void>
  setGoalId: (goalId: CampaignGoalId) => void
  setAudienceId: (audienceId: CampaignAudienceId) => void
  setSavedGroupId: (savedGroupId: string | null) => void
  setChannelId: (channelId: CampaignChannelId) => void
  setOfferStanceId: (stanceId: CampaignOfferStanceId) => void
  setScheduleModeId: (modeId: CampaignScheduleModeId) => void
  writeManually: () => void
  /** Explicit no-op until ticket 33 — does not call a live endpoint. */
  prepareDraftStub: () => void
  setSubject: (value: string) => void
  setMessage: (value: string) => void
  openGuestPreview: () => void
  closeGuestPreview: () => void
  /** From Review Guest preview Edit text — returns to Message editor. */
  editMessageFromReview: () => void
  continue: () => Promise<void>
  back: () => void
}

type WizardState = {
  isOpen: boolean
  locationId: number | null
  locationName: string | null
  templateId: string | null
  templateVersion: number | null
  draftId: number | null
  draftRowVersion: number | null
  saveStatus: CampaignWizardSnapshot["saveStatus"]
  saveError: string | null
  lastSavedAt: Date | null
  stepId: CampaignWizardStepId
  goalId: CampaignGoalId | null
  openedAt: Date | null
  audienceId: CampaignAudienceId
  savedGroupId: string | null
  audienceLoadStatus: CampaignAudienceViewModel["loadStatus"]
  liveCounts: CampaignAudienceSmartGroupCountsInput | null
  channelId: CampaignChannelId
  offerStanceId: CampaignOfferStanceId
  scheduleModeId: CampaignScheduleModeId
  messageWriteEntry: CampaignMessageWriteEntry
  messageSubject: string
  messageBody: string
  guestPreviewOpen: boolean
}

const NUMBERED_STEP_ORDER: readonly CampaignWizardStepId[] =
  CAMPAIGN_WIZARD_NUMBERED_STEPS.map((step) => step.id)

const DEFAULT_AUDIENCE_ID: CampaignAudienceId = "all-eligible-guests"

const CAMPAIGN_DRAFT_SAVE_ERROR_MESSAGE =
  "Could not save this campaign draft. Try again."

function emptyState(): WizardState {
  return {
    isOpen: false,
    locationId: null,
    locationName: null,
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
    savedGroupId: null,
    audienceLoadStatus: "idle",
    liveCounts: null,
    channelId: defaultCampaignChannelId(),
    offerStanceId: defaultCampaignOfferStanceId(),
    scheduleModeId: defaultCampaignScheduleModeId(),
    messageWriteEntry: "chooser",
    messageSubject: "",
    messageBody: "",
    guestPreviewOpen: false,
  }
}

function buildDraftFields(state: WizardState): {
  goalId: string | null
  templateId: string | null
  templateVersion: number | null
  audienceKey: string
  channel: string
  offerStance: string
  messageSubject: string | null
  messageBody: string | null
} {
  const messageSubject = state.messageSubject.trim()
  const messageBody = state.messageBody.trim()
  return {
    goalId: state.goalId,
    templateId: state.templateId,
    templateVersion:
      state.templateId == null ? null : state.templateVersion,
    audienceKey: state.audienceId,
    channel: state.channelId,
    offerStance: state.offerStanceId,
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
      })
      return {
        id: option.id,
        title: option.title,
        description: option.description,
        recommended: option.recommended,
        selected: state.audienceId === option.id,
        deferredOfferGroup: option.deferredOfferGroup,
        matched: counts.matched,
        currentlyEligible: counts.currentlyEligible,
        countLabel: formatAudienceMatchedEligibleLabel(
          counts.matched,
          counts.currentlyEligible
        ),
        countSource: counts.countSource,
      }
    })

  return {
    loadStatus: state.audienceLoadStatus,
    selectedAudienceId: state.audienceId,
    savedGroupId: state.savedGroupId,
    options,
    eligibilityBreakdown: mockCampaignAudienceEligibilityBreakdown(),
    showSavedGroupPicker: state.audienceId === "saved-group",
    savedGroupOptions: CAMPAIGN_AUDIENCE_COPY.mockSavedGroupOptions,
  }
}

function buildChannelViewModel(
  state: WizardState
): CampaignChannelViewModel | null {
  if (state.stepId !== "channel") {
    return null
  }

  const usage = buildCampaignChannelUsageSummary({
    channelId: state.channelId,
    audienceId: state.audienceId,
    fixture: MESSAGING_USAGE_FIXTURE,
  })

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
    messagingFixture: MESSAGING_USAGE_FIXTURE,
    smsShortfall: resolveCampaignChannelSmsShortfall({
      channelId: state.channelId,
      fixture: MESSAGING_USAGE_FIXTURE,
    }),
  }
}

function buildOfferViewModel(
  state: WizardState
): CampaignOfferViewModel | null {
  if (state.stepId !== "offer") {
    return null
  }

  const usage = buildCampaignChannelUsageSummary({
    channelId: state.channelId,
    audienceId: state.audienceId,
    fixture: MESSAGING_USAGE_FIXTURE,
  })

  return {
    selectedStanceId: state.offerStanceId,
    attachedOfferId: null,
    stepHeading: CAMPAIGN_OFFER_COPY.stepHeading,
    stepDescription: CAMPAIGN_OFFER_COPY.stepDescription,
    options: CAMPAIGN_OFFER_OPTIONS.map((option) => ({
      ...option,
      selected: state.offerStanceId === option.id,
    })),
    usageSummary: {
      title: CAMPAIGN_OFFER_COPY.usageTitle,
      audienceLine: usage.audienceLine,
      rows: usage.rows,
    },
    messagingFixture: MESSAGING_USAGE_FIXTURE,
  }
}

function buildMessageViewModel(
  state: WizardState
): CampaignMessageViewModel | null {
  if (state.stepId !== "message") {
    return null
  }

  const usage = buildCampaignChannelUsageSummary({
    channelId: state.channelId,
    audienceId: state.audienceId,
    fixture: MESSAGING_USAGE_FIXTURE,
  })

  return {
    writeEntry: state.messageWriteEntry,
    subject: state.messageSubject,
    body: state.messageBody,
    channelId: state.channelId,
    showSubject: state.channelId === "email",
    prepareAiLive: false,
    guestPreviewOpen: state.guestPreviewOpen,
    sendTestAvailable: false,
    stepHeading: CAMPAIGN_MESSAGE_COPY.stepHeading,
    stepDescription: CAMPAIGN_MESSAGE_COPY.stepDescription,
    locationName: state.locationName ?? "",
    usageSummary: {
      title: CAMPAIGN_MESSAGE_COPY.usageTitle,
      audienceLine: usage.audienceLine,
      rows: usage.rows,
    },
    messagingFixture: MESSAGING_USAGE_FIXTURE,
  }
}

function buildScheduleViewModel(
  state: WizardState
): CampaignScheduleViewModel | null {
  if (state.stepId !== "schedule") {
    return null
  }

  const usage = buildCampaignChannelUsageSummary({
    channelId: state.channelId,
    audienceId: state.audienceId,
    fixture: MESSAGING_USAGE_FIXTURE,
  })

  return {
    selectedModeId: state.scheduleModeId,
    stepHeading: CAMPAIGN_SCHEDULE_COPY.stepHeading,
    stepDescription: CAMPAIGN_SCHEDULE_COPY.stepDescription,
    options: CAMPAIGN_SCHEDULE_OPTIONS.map((option) => ({
      ...option,
      selected: state.scheduleModeId === option.id,
    })),
    usageSummary: {
      title: CAMPAIGN_SCHEDULE_COPY.usageTitle,
      audienceLine: usage.audienceLine,
      rows: usage.rows,
    },
    messagingFixture: MESSAGING_USAGE_FIXTURE,
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

function buildReviewViewModel(
  state: WizardState
): CampaignReviewViewModel | null {
  if (state.stepId !== "review") {
    return null
  }

  const usage = buildCampaignChannelUsageSummary({
    channelId: state.channelId,
    audienceId: state.audienceId,
    fixture: MESSAGING_USAGE_FIXTURE,
  })

  const goalLabel =
    labelForCampaignGoalId(state.goalId) ?? CAMPAIGN_REVIEW_COPY.emptyValue
  const locationLabel = state.locationName ?? CAMPAIGN_REVIEW_COPY.emptyValue
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
        value: CAMPAIGN_REVIEW_COPY.emptyValue,
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
    sendAvailable: false,
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
      guestPreviewOpen: state.guestPreviewOpen,
      sendTestAvailable: false,
    },
  }
}

function audienceCanContinue(state: WizardState): boolean {
  if (state.audienceId === "saved-group") {
    return state.savedGroupId != null && state.savedGroupId.length > 0
  }
  return true
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

function toSnapshot(
  state: WizardState,
  getNow: () => Date
): CampaignWizardSnapshot {
  const now = state.openedAt ?? getNow()
  const locationName = state.locationName ?? ""
  const showNumberedStepper = state.stepId !== "goal"
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

  let canContinue = false
  if (isGoal) {
    canContinue = state.goalId != null
  } else if (isAudience) {
    canContinue = audienceCanContinue(state)
  } else if (isChannel || isOffer || isSchedule) {
    canContinue = true
  } else if (isMessage) {
    canContinue = messageCanContinue(state)
  } else {
    canContinue = false
  }

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
    pageTitle: CAMPAIGN_WIZARD_COPY.pageTitle,
    headerSubtitle: formatCampaignWizardHeaderSubtitle({
      goalId: state.goalId,
      locationName,
      now,
    }),
    stepHeading: isGoal ? CAMPAIGN_WIZARD_COPY.goalStepHeading : null,
    stepDescription: isGoal ? CAMPAIGN_WIZARD_COPY.goalStepDescription : null,
    showNumberedStepper,
    numberedSteps: CAMPAIGN_WIZARD_NUMBERED_STEPS,
    activeNumberedStepIndex,
    canContinue,
    primaryActionLabel: isReview
      ? CAMPAIGN_REVIEW_COPY.primaryActionLabel
      : CAMPAIGN_WIZARD_COPY.continue,
    placeholderBody: placeholderForStep(state.stepId),
    audience: buildAudienceViewModel(state),
    channel: buildChannelViewModel(state),
    offer: buildOfferViewModel(state),
    message: buildMessageViewModel(state),
    schedule: buildScheduleViewModel(state),
    review: buildReviewViewModel(state),
  }
}

/**
 * Campaign create wizard — blank Create opens at Goal with no template.
 * Close / dismiss never persists a server Campaign Draft (ticket 22 / 29).
 * Audience (ticket 23): live Smart Group counts + mocked eligibility breakdown.
 * Channel (ticket 24): Email/SMS + shared messaging usage fixtures (no balance API).
 * Offer (ticket 25): stance only — No offer + shell select path; no live catalog.
 * Message (ticket 26): Write manually + Guest preview (Send test off); AI prepare stub until 33.
 * Schedule + Review (ticket 27): timing chrome + summary only — no send / schedule-commit.
 */
export function createCampaignWizardModule(
  adapters: CampaignWizardAdapters = {}
): CampaignWizardModule {
  const getNow = adapters.getNow ?? (() => new Date())
  let state = emptyState()
  let snapshot = toSnapshot(state, getNow)
  const listeners = new Set<() => void>()
  let audienceLoadGeneration = 0

  const publish = () => {
    snapshot = toSnapshot(state, getNow)
    for (const listener of listeners) {
      listener()
    }
  }

  const closeWithoutPersist = () => {
    audienceLoadGeneration += 1
    state = emptyState()
    publish()
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
            goalId: fields.goalId,
            templateId: fields.templateId,
            templateVersion: fields.templateVersion,
            audienceKey: fields.audienceKey,
            channel: fields.channel,
            offerStance: fields.offerStance,
            messageSubject: fields.messageSubject,
            messageBody: fields.messageBody,
          })
        : await adapters.updateDraft!(draftId, {
            rowVersion: draftRowVersion ?? 1,
            goalId: fields.goalId,
            templateId: fields.templateId,
            templateVersion: fields.templateVersion,
            audienceKey: fields.audienceKey,
            channel: fields.channel,
            offerStance: fields.offerStance,
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
    } catch {
      state = {
        ...state,
        saveStatus: "error",
        saveError: CAMPAIGN_DRAFT_SAVE_ERROR_MESSAGE,
      }
      publish()
      return false
    }
  }

  const loadAudienceCounts = async () => {
    const locationId = state.locationId
    const loadSmartGroupCounts = adapters.loadSmartGroupCounts
    if (locationId == null || loadSmartGroupCounts == null) {
      state = {
        ...state,
        audienceLoadStatus: "loaded",
        liveCounts: null,
      }
      publish()
      return
    }

    const generation = ++audienceLoadGeneration
    state = {
      ...state,
      audienceLoadStatus: "loading",
      liveCounts: null,
    }
    publish()

    try {
      const liveCounts = await loadSmartGroupCounts({ locationId })
      if (generation !== audienceLoadGeneration) {
        return
      }
      state = {
        ...state,
        audienceLoadStatus: "loaded",
        liveCounts,
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
    openBlankCreate(input) {
      audienceLoadGeneration += 1
      state = {
        ...emptyState(),
        isOpen: true,
        locationId: input.locationId,
        locationName: input.locationName,
        stepId: "goal",
        openedAt: getNow(),
      }
      publish()
    },
    async openFromTemplate(input) {
      audienceLoadGeneration += 1
      const suggestions = mapCampaignTemplateSuggestions(
        input.template.suggestions
      )
      state = {
        ...emptyState(),
        isOpen: true,
        locationId: input.locationId,
        locationName: input.locationName,
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
      await loadAudienceCounts()
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

      state = {
        ...emptyState(),
        isOpen: true,
        locationId: draft.locationId,
        locationName: input.locationName,
        templateId: draft.templateId,
        templateVersion: draft.templateVersion,
        draftId: draft.id,
        draftRowVersion: draft.rowVersion,
        stepId: goalId == null ? "goal" : "audience",
        goalId,
        openedAt: getNow(),
        audienceId: resolved.audienceId,
        channelId: resolved.channelId,
        offerStanceId: resolved.offerStanceId,
        messageWriteEntry: hasMessageContent ? "editor" : "chooser",
        messageSubject: draft.messageSubject ?? "",
        messageBody: draft.messageBody ?? "",
        lastSavedAt: getNow(),
        saveStatus: "saved",
      }
      publish()
      if (goalId != null) {
        await loadAudienceCounts()
      }
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
        savedGroupId:
          audienceId === "saved-group" ? state.savedGroupId : null,
      }
      publish()
    },
    setSavedGroupId(savedGroupId) {
      if (!state.isOpen || state.stepId !== "audience") {
        return
      }
      if (state.audienceId !== "saved-group") {
        return
      }
      state = { ...state, savedGroupId }
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
      state = { ...state, offerStanceId: stanceId }
      publish()
    },
    setScheduleModeId(modeId) {
      if (!state.isOpen || state.stepId !== "schedule") {
        return
      }
      state = { ...state, scheduleModeId: modeId }
      publish()
    },
    writeManually() {
      if (!state.isOpen || state.stepId !== "message") {
        return
      }
      state = {
        ...state,
        messageWriteEntry: "editor",
        guestPreviewOpen: false,
      }
      publish()
    },
    prepareDraftStub() {
      // Ticket 33 wires live prepare. Explicit no-op — no network call.
      if (!state.isOpen || state.stepId !== "message") {
        return
      }
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
      state = { ...state, guestPreviewOpen: false }
      publish()
    },
    editMessageFromReview() {
      if (!state.isOpen || state.stepId !== "review") {
        return
      }
      state = {
        ...state,
        stepId: "message",
        messageWriteEntry: "editor",
        guestPreviewOpen: false,
      }
      publish()
    },
    async continue() {
      if (!state.isOpen) {
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
          savedGroupId: null,
          audienceLoadStatus: "idle",
          liveCounts: null,
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
      if (state.stepId === "message") {
        if (!messageCanContinue(state)) {
          return
        }
        state = { ...state, guestPreviewOpen: false }
      }
      if (state.stepId === "review") {
        // Ticket 27 — Review cannot send / schedule-commit.
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
      if (state.stepId === "goal") {
        closeWithoutPersist()
        return
      }
      if (state.stepId === "message" || state.stepId === "review") {
        state = { ...state, guestPreviewOpen: false }
      }
      const index = NUMBERED_STEP_ORDER.indexOf(state.stepId)
      if (index <= 0) {
        state = {
          ...state,
          stepId: "goal",
          audienceLoadStatus: "idle",
          liveCounts: null,
        }
        publish()
        return
      }
      state = { ...state, stepId: NUMBERED_STEP_ORDER[index - 1]! }
      publish()
    },
  }
}

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
  CAMPAIGN_OFFER_COPY,
  CAMPAIGN_OFFER_OPTIONS,
  defaultCampaignOfferStanceId,
  type CampaignOfferStanceId,
} from "@/lib/operatorCampaigns/campaignOfferPresentation"
import {
  CAMPAIGN_GOAL_OPTIONS,
  CAMPAIGN_WIZARD_COPY,
  CAMPAIGN_WIZARD_NUMBERED_STEPS,
  formatCampaignWizardHeaderSubtitle,
  type CampaignGoalId,
  type CampaignGoalOption,
  type CampaignWizardStepId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"
import {
  MESSAGING_USAGE_FIXTURE,
  type MessagingUsageFixture,
} from "@/lib/operatorCampaigns/messagingUsageFixtures"

export type CampaignWizardOpenBlankInput = {
  locationId: number
  locationName: string
}

export type CampaignWizardAdapters = {
  getNow?: () => Date
  /** Live Smart Group aggregates for Audience — omitted until step loads. */
  loadSmartGroupCounts?: (input: {
    locationId: number
  }) => Promise<CampaignAudienceSmartGroupCountsInput>
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

export type CampaignWizardSnapshot = {
  isOpen: boolean
  locationId: number | null
  locationName: string | null
  templateId: string | null
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
  placeholderBody: string | null
  audience: CampaignAudienceViewModel | null
  channel: CampaignChannelViewModel | null
  offer: CampaignOfferViewModel | null
}

export type CampaignWizardModule = {
  getSnapshot: () => CampaignWizardSnapshot
  subscribe: (listener: () => void) => () => void
  openBlankCreate: (input: CampaignWizardOpenBlankInput) => void
  close: () => void
  /** Close without persist — Save path lands in ticket 29. */
  saveAndExit: () => void
  setGoalId: (goalId: CampaignGoalId) => void
  setAudienceId: (audienceId: CampaignAudienceId) => void
  setSavedGroupId: (savedGroupId: string | null) => void
  setChannelId: (channelId: CampaignChannelId) => void
  setOfferStanceId: (stanceId: CampaignOfferStanceId) => void
  continue: () => Promise<void>
  back: () => void
}

type WizardState = {
  isOpen: boolean
  locationId: number | null
  locationName: string | null
  templateId: string | null
  stepId: CampaignWizardStepId
  goalId: CampaignGoalId | null
  openedAt: Date | null
  audienceId: CampaignAudienceId
  savedGroupId: string | null
  audienceLoadStatus: CampaignAudienceViewModel["loadStatus"]
  liveCounts: CampaignAudienceSmartGroupCountsInput | null
  channelId: CampaignChannelId
  offerStanceId: CampaignOfferStanceId
}

const NUMBERED_STEP_ORDER: readonly CampaignWizardStepId[] =
  CAMPAIGN_WIZARD_NUMBERED_STEPS.map((step) => step.id)

const DEFAULT_AUDIENCE_ID: CampaignAudienceId = "all-eligible-guests"

function emptyState(): WizardState {
  return {
    isOpen: false,
    locationId: null,
    locationName: null,
    templateId: null,
    stepId: "goal",
    goalId: null,
    openedAt: null,
    audienceId: DEFAULT_AUDIENCE_ID,
    savedGroupId: null,
    audienceLoadStatus: "idle",
    liveCounts: null,
    channelId: defaultCampaignChannelId(),
    offerStanceId: defaultCampaignOfferStanceId(),
  }
}

function placeholderForStep(stepId: CampaignWizardStepId): string | null {
  switch (stepId) {
    case "audience":
    case "channel":
    case "offer":
      return null
    case "message":
      return CAMPAIGN_WIZARD_COPY.placeholderMessage
    case "schedule":
      return CAMPAIGN_WIZARD_COPY.placeholderSchedule
    case "review":
      return CAMPAIGN_WIZARD_COPY.placeholderReview
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

function audienceCanContinue(state: WizardState): boolean {
  if (state.audienceId === "saved-group") {
    return state.savedGroupId != null && state.savedGroupId.length > 0
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

  let canContinue = false
  if (isGoal) {
    canContinue = state.goalId != null
  } else if (isAudience) {
    canContinue = audienceCanContinue(state)
  } else if (isChannel || isOffer) {
    canContinue = true
  } else {
    canContinue = state.stepId !== "review"
  }

  return {
    isOpen: state.isOpen,
    locationId: state.locationId,
    locationName: state.locationName,
    templateId: state.templateId,
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
    placeholderBody: placeholderForStep(state.stepId),
    audience: buildAudienceViewModel(state),
    channel: buildChannelViewModel(state),
    offer: buildOfferViewModel(state),
  }
}

/**
 * Campaign create wizard — blank Create opens at Goal with no template.
 * Close / dismiss never persists a server Campaign Draft (ticket 22 / 29).
 * Audience (ticket 23): live Smart Group counts + mocked eligibility breakdown.
 * Channel (ticket 24): Email/SMS + shared messaging usage fixtures (no balance API).
 * Offer (ticket 25): stance only — No offer + shell select path; no live catalog.
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
        isOpen: true,
        locationId: input.locationId,
        locationName: input.locationName,
        templateId: null,
        stepId: "goal",
        goalId: null,
        openedAt: getNow(),
        audienceId: DEFAULT_AUDIENCE_ID,
        savedGroupId: null,
        audienceLoadStatus: "idle",
        liveCounts: null,
        channelId: defaultCampaignChannelId(),
        offerStanceId: defaultCampaignOfferStanceId(),
      }
      publish()
    },
    close() {
      closeWithoutPersist()
    },
    saveAndExit() {
      // Ticket 29 wires Draft persist. Until then, exit without a server Draft.
      closeWithoutPersist()
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
      if (state.stepId === "review") {
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

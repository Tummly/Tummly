import {
  CAMPAIGN_GOAL_OPTIONS,
  CAMPAIGN_WIZARD_COPY,
  CAMPAIGN_WIZARD_NUMBERED_STEPS,
  formatCampaignWizardHeaderSubtitle,
  type CampaignGoalId,
  type CampaignGoalOption,
  type CampaignWizardStepId,
} from "@/lib/operatorCampaigns/campaignWizardPresentation"

export type CampaignWizardOpenBlankInput = {
  locationId: number
  locationName: string
}

export type CampaignWizardAdapters = {
  getNow?: () => Date
}

export type CampaignWizardGoalCardViewModel = CampaignGoalOption & {
  selected: boolean
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
}

export type CampaignWizardModule = {
  getSnapshot: () => CampaignWizardSnapshot
  subscribe: (listener: () => void) => () => void
  openBlankCreate: (input: CampaignWizardOpenBlankInput) => void
  close: () => void
  /** Close without persist — Save path lands in ticket 29. */
  saveAndExit: () => void
  setGoalId: (goalId: CampaignGoalId) => void
  continue: () => void
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
}

const NUMBERED_STEP_ORDER: readonly CampaignWizardStepId[] =
  CAMPAIGN_WIZARD_NUMBERED_STEPS.map((step) => step.id)

function emptyState(): WizardState {
  return {
    isOpen: false,
    locationId: null,
    locationName: null,
    templateId: null,
    stepId: "goal",
    goalId: null,
    openedAt: null,
  }
}

function placeholderForStep(stepId: CampaignWizardStepId): string | null {
  switch (stepId) {
    case "audience":
      return CAMPAIGN_WIZARD_COPY.placeholderAudience
    case "channel":
      return CAMPAIGN_WIZARD_COPY.placeholderChannel
    case "offer":
      return CAMPAIGN_WIZARD_COPY.placeholderOffer
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
    stepHeading:
      state.stepId === "goal" ? CAMPAIGN_WIZARD_COPY.goalStepHeading : null,
    stepDescription:
      state.stepId === "goal"
        ? CAMPAIGN_WIZARD_COPY.goalStepDescription
        : null,
    showNumberedStepper,
    numberedSteps: CAMPAIGN_WIZARD_NUMBERED_STEPS,
    activeNumberedStepIndex,
    canContinue:
      state.stepId === "goal"
        ? state.goalId != null
        : state.stepId !== "review",
    placeholderBody: placeholderForStep(state.stepId),
  }
}

/**
 * Campaign create wizard — blank Create opens at Goal with no template.
 * Close / dismiss never persists a server Campaign Draft (ticket 22 / 29).
 */
export function createCampaignWizardModule(
  adapters: CampaignWizardAdapters = {}
): CampaignWizardModule {
  const getNow = adapters.getNow ?? (() => new Date())
  let state = emptyState()
  let snapshot = toSnapshot(state, getNow)
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = toSnapshot(state, getNow)
    for (const listener of listeners) {
      listener()
    }
  }

  const closeWithoutPersist = () => {
    state = emptyState()
    publish()
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
      state = {
        isOpen: true,
        locationId: input.locationId,
        locationName: input.locationName,
        templateId: null,
        stepId: "goal",
        goalId: null,
        openedAt: getNow(),
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
    continue() {
      if (!state.isOpen) {
        return
      }
      if (state.stepId === "goal") {
        if (state.goalId == null) {
          return
        }
        state = { ...state, stepId: "audience" }
        publish()
        return
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
        state = { ...state, stepId: "goal" }
        publish()
        return
      }
      state = { ...state, stepId: NUMBERED_STEP_ORDER[index - 1]! }
      publish()
    },
  }
}

import type {
  OperatorHomeSetupStep,
  OperatorHomeSetupStepId,
  OperatorHomeSetupStepStatus,
} from "@/types/operatorHome"

/** First four checklist rows gate “ready for guests”; later rows stay optional. */
export const REQUIRED_SETUP_STEP_IDS = [
  "account-ready",
  "upload-logo",
  "guest-form",
  "first-response",
] as const satisfies readonly OperatorHomeSetupStepId[]

export const REQUIRED_SETUP_STEP_COUNT = REQUIRED_SETUP_STEP_IDS.length

const requiredSetupStepIdSet = new Set<OperatorHomeSetupStepId>(
  REQUIRED_SETUP_STEP_IDS
)

export function countCompleteSetupSteps(steps: OperatorHomeSetupStep[]): {
  completeCount: number
  totalSteps: number
} {
  const requiredCompleteCount = steps.filter(
    (step) =>
      requiredSetupStepIdSet.has(step.id) && step.status === "complete"
  ).length

  return {
    completeCount: requiredCompleteCount,
    totalSteps: REQUIRED_SETUP_STEP_COUNT,
  }
}

export function shouldShowSetupStatusMarker(
  status: OperatorHomeSetupStepStatus
): boolean {
  return status === "complete" || status === "partial"
}

export const hasSetupStepTintedRow = shouldShowSetupStatusMarker

/** Incomplete rows spread text and CTAs to opposite edges; partial rows keep CTAs adjacent. */
export function shouldSpreadSetupStepActions(
  status: OperatorHomeSetupStepStatus,
  actionCount: number
): boolean {
  return status === "incomplete" && actionCount > 0
}

const SECONDARY_ACTION_IDS = new Set(["upload-logo", "preview-guest-form"])

export function resolveSetupActionButtonVariant(
  actionId: string
): "op-secondary" | "op-tertiary" {
  return SECONDARY_ACTION_IDS.has(actionId)
    ? "op-secondary"
    : "op-tertiary"
}

type SetupStepIllustrationCrop = {
  width: `${number}%`
  height: `${number}%`
  left: `${number}%`
  top: `${number}%`
}

export type SetupStepIllustrationConfig = {
  height: number
  crop: SetupStepIllustrationCrop | "cover"
}

/** Figma dark checklist — per-step illustration frame + overflow crop (node 3238:28363). */
export const SETUP_STEP_ILLUSTRATION: Record<
  OperatorHomeSetupStepId,
  SetupStepIllustrationConfig
> = {
  "account-ready": {
    height: 35,
    crop: {
      width: "154.26%",
      height: "162.5%",
      left: "-34.59%",
      top: "-31.25%",
    },
  },
  "upload-logo": {
    height: 35,
    crop: "cover",
  },
  "guest-form": {
    height: 37,
    crop: {
      width: "147.37%",
      height: "144.83%",
      left: "-23.68%",
      top: "-22.41%",
    },
  },
  "first-response": {
    height: 43,
    crop: {
      width: "193.06%",
      height: "168.72%",
      left: "-48.75%",
      top: "-34.67%",
    },
  },
  "qr-placement": {
    height: 47,
    crop: {
      width: "160%",
      height: "126%",
      left: "-44.29%",
      top: "-13%",
    },
  },
  "first-offer": {
    height: 42,
    crop: {
      width: "182.61%",
      height: "158.49%",
      left: "-46.74%",
      top: "-35.22%",
    },
  },
  "first-campaign": {
    height: 43,
    crop: {
      width: "173.2%",
      height: "146.51%",
      left: "-35.05%",
      top: "-23.84%",
    },
  },
}

export function getSetupStepIllustration(
  stepId: OperatorHomeSetupStepId
): SetupStepIllustrationConfig {
  return SETUP_STEP_ILLUSTRATION[stepId]
}

/** Figma step copy — 16/24 title, 14/17 description, 8px gap (node 3238:28148). */
export const SETUP_STEP_COPY_GAP_CLASS = "gap-[8px]"
export const SETUP_STEP_TITLE_CLASS =
  "m-0 text-base font-semibold leading-6 tracking-[-0.4px] text-foreground"
export const SETUP_STEP_DESCRIPTION_CLASS =
  "m-0 text-op-sm leading-[17px] text-op-card-subtitle-color"

/** PRD §4.2 — stepped section padding and responsive step layout. */
export const SETUP_CHECKLIST_SECTION_CLASS =
  "rounded-op-lg border border-op-card-border bg-op-card-background p-4 shadow-[0px_1px_1.5px_rgba(19,29,43,0.04),0px_8px_12px_rgba(19,29,43,0.08)] sm:p-5 md:p-6 dark:shadow-none"

export const SETUP_CHECKLIST_ACCORDION_TRIGGER_CLASS =
  "cursor-pointer items-center gap-4 py-0 hover:no-underline **:data-[slot=accordion-trigger-icon]:ml-0 **:data-[slot=accordion-trigger-icon]:hidden"

export const SETUP_CHECKLIST_ACCORDION_CONTROL_CLASS =
  "flex size-11 shrink-0 cursor-pointer items-center justify-center rounded-op-sm bg-op-button-collapse-background text-foreground hover:bg-op-button-collapse-hover md:size-[42px]"

export const SETUP_CHECKLIST_STEP_CLASS =
  "relative flex items-start py-4 pr-4 pl-4 sm:items-center sm:py-5 sm:pr-5 sm:pl-[30px]"

export const SETUP_CHECKLIST_STEP_BODY_CLASS =
  "flex min-w-0 flex-1 items-start sm:items-center"

export const SETUP_CHECKLIST_STEP_CONTENT_CLASS =
  "flex min-w-0 flex-1 flex-col gap-4 pl-3 sm:flex-row sm:items-center sm:gap-0"

export const SETUP_CHECKLIST_STEP_ACTIONS_CLASS =
  "flex w-full shrink-0 flex-wrap items-center gap-[18px] sm:w-auto"

export const SETUP_CHECKLIST_STEP_ACTIONS_SPREAD_CLASS = "sm:justify-between"

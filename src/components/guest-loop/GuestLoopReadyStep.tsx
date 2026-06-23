import type { ReactNode } from "react"
import { CheckIcon, Loader2Icon } from "lucide-react"
import {
  AnimatePresence,
  motion,
  useReducedMotion,
  type Transition,
} from "framer-motion"

import { cn } from "@/lib/utils"
import {
  PROVISIONING_PHASE_MIN_MS,
  type ProvisioningPhaseSnapshot,
  type ProvisioningPhaseStatus,
} from "@/lib/runProvisioningPhases"

import { GuestLoopStepButton } from "./GuestLoopStepButton"
import { GuestLoopStepFooter } from "./GuestLoopStepFooter"
import { GuestLoopStepHeader } from "./GuestLoopStepHeader"
import {
  GUEST_LOOP_SINGLE_STEPS,
  type GuestLoopProgressStep,
} from "./guestLoopSteps"

type ProvisioningPhaseConfig = {
  title: string
  description: string
  readyLabel: string
}

const PROVISIONING_PHASES: readonly ProvisioningPhaseConfig[] = [
  {
    title: "Creating your Smart Guest Link",
    description: "Your location link and QR code are being generated.",
    readyLabel: "Smart Guest Link ready",
  },
  {
    title: "Preparing your private feedback form",
    description:
      "We're applying the starter form with private feedback, guest details and consent wording included.",
    readyLabel: "Guest form ready",
  },
  {
    title: "Preparing your starter QR materials",
    description:
      "We're generating starter QR materials for your selected touchpoints.",
    readyLabel: "Starter QR materials ready",
  },
] as const

const phaseSpring: Transition = {
  type: "spring",
  stiffness: 420,
  damping: 34,
  mass: 0.85,
}

const phaseFade: Transition = {
  duration: 0.22,
  ease: [0.25, 0.1, 0.25, 1],
}

function usePhaseMotion(reduced: boolean) {
  return {
    spring: reduced ? { duration: 0 } : phaseSpring,
    fade: reduced ? { duration: 0 } : phaseFade,
  }
}

type GuestLoopReadyStepProps = {
  activeStep: number
  steps?: readonly GuestLoopProgressStep[]
  phaseStatuses: ProvisioningPhaseSnapshot
  isWorkspaceReady: boolean
  provisioningError: string | null
  isProvisioningActive: boolean
  onOpenWorkspace: () => void
  onRetry: () => void
}

function PhaseSuccessIcon({
  reduced,
  transition,
}: {
  reduced: boolean
  transition: Transition
}) {
  return (
    <motion.span
      initial={reduced ? false : { scale: 0.72, opacity: 0 }}
      animate={{ scale: 1, opacity: 1 }}
      transition={transition}
      className="flex size-[18px] shrink-0 items-center justify-center rounded-full bg-primary"
      aria-hidden
    >
      <CheckIcon className="size-3 text-white" strokeWidth={3} />
    </motion.span>
  )
}

function PhaseLoadingIcon() {
  return (
    <Loader2Icon
      className="size-[18px] shrink-0 animate-spin text-primary"
      aria-hidden
    />
  )
}

function PhaseIdleIcon() {
  return (
    <span
      className="flex size-[18px] shrink-0 items-center justify-center rounded-full border-2 border-[#dfdfdf] bg-white"
      aria-hidden
    />
  )
}

function PhaseIconSlot({
  reduced,
  children,
}: {
  reduced: boolean
  children: ReactNode
}) {
  const { fade } = usePhaseMotion(reduced)

  return (
    <motion.div
      initial={reduced ? false : { opacity: 0, scale: 0.9 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={reduced ? undefined : { opacity: 0, scale: 0.9 }}
      transition={fade}
    >
      {children}
    </motion.div>
  )
}

const PHASE_HORIZONTAL_PADDING = "px-4"

function PhaseProgressBar() {
  const prefersReducedMotion = useReducedMotion()
  const duration = prefersReducedMotion ? 0 : PROVISIONING_PHASE_MIN_MS / 1000

  return (
    <div className="h-1.5 w-full overflow-hidden rounded-full bg-white">
      <motion.div
        className="h-full rounded-full bg-primary"
        initial={{ width: "0%" }}
        animate={{ width: "100%" }}
        transition={{ duration, ease: "easeOut" }}
      />
    </div>
  )
}

function PhaseConnector({ isComplete }: { isComplete: boolean }) {
  return (
    <div
      aria-hidden
      className={cn(
        "h-[30px] w-0.5 shrink-0 transition-colors duration-350 ease-out",
        isComplete ? "bg-primary" : "bg-[#dfdfdf]"
      )}
    />
  )
}

function ProvisioningPhaseItem({
  title,
  description,
  readyLabel,
  status,
}: ProvisioningPhaseConfig & { status: ProvisioningPhaseStatus }) {
  const prefersReducedMotion = useReducedMotion()
  const reduced = prefersReducedMotion ?? false
  const { spring, fade } = usePhaseMotion(reduced)
  const isActive = status === "loading"
  const isPending = status === "idle"
  const isComplete = status === "success"
  const textColor = isPending ? "#868686" : "#232323"

  return (
    <motion.div
      initial={false}
      animate={{
        backgroundColor: isActive ? "#f1f1f1" : "rgba(0,0,0,0)",
        borderRadius: isActive ? 8 : 0,
      }}
      transition={spring}
      className={cn(
        "flex w-full flex-col overflow-hidden",
        PHASE_HORIZONTAL_PADDING,
        isActive ? "gap-5 py-5" : "py-0"
      )}
    >
      <div className="flex flex-col gap-3">
        <AnimatePresence mode="wait" initial={false}>
          {isComplete ? (
            <motion.div
              key="ready-row"
              initial={reduced ? false : { opacity: 0, y: -10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={reduced ? undefined : { opacity: 0, y: -8 }}
              transition={spring}
              className="flex items-center gap-3"
            >
              <PhaseSuccessIcon reduced={reduced} transition={spring} />
              <motion.span
                initial={reduced ? false : { opacity: 0, x: -6 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ ...spring, delay: reduced ? 0 : 0.05 }}
                className="text-sm font-semibold leading-normal tracking-[-0.28px] text-primary"
              >
                {readyLabel}
              </motion.span>
            </motion.div>
          ) : (
            <PhaseIconSlot
              key={isActive ? "loading-icon" : "idle-icon"}
              reduced={reduced}
            >
              {isActive ? <PhaseLoadingIcon /> : <PhaseIdleIcon />}
            </PhaseIconSlot>
          )}
        </AnimatePresence>

        <div className="flex flex-col gap-2.5">
          <motion.h3
            animate={{
              color: textColor,
              fontWeight: isPending ? 500 : 700,
            }}
            transition={fade}
            className="m-0 text-xl leading-normal tracking-[-0.4px]"
          >
            {title}
          </motion.h3>
          <motion.p
            animate={{ color: textColor }}
            transition={fade}
            className="m-0 text-sm leading-[18px] tracking-[-0.28px]"
          >
            {description}
          </motion.p>
        </div>
      </div>

      {isActive ? (
        <div className="h-1.5 shrink-0">
          <PhaseProgressBar />
        </div>
      ) : null}
    </motion.div>
  )
}

export function GuestLoopReadyStep({
  activeStep,
  steps = GUEST_LOOP_SINGLE_STEPS,
  phaseStatuses,
  isWorkspaceReady,
  provisioningError,
  isProvisioningActive,
  onOpenWorkspace,
  onRetry,
}: GuestLoopReadyStepProps) {
  const phaseStatusList = [
    phaseStatuses.phase1,
    phaseStatuses.phase2,
    phaseStatuses.phase3,
  ] as const
  const prefersReducedMotion = useReducedMotion()
  const { fade } = usePhaseMotion(prefersReducedMotion ?? false)

  const headerDescription = provisioningError
    ? "We couldn't finish setting up your Guest Loop. You can retry or go back to check your restaurant details."
    : "We're preparing the core setup for this location. You can review and adjust everything once you open your workspace."

  return (
    <div className="flex w-full flex-col gap-8 sm:gap-10">
      <GuestLoopStepHeader
        title="Setting up your first Guest Loop"
        description={
          <AnimatePresence mode="wait" initial={false}>
            <motion.p
              key={headerDescription}
              initial={prefersReducedMotion ? false : { opacity: 0, y: 8 }}
              animate={{ opacity: 1, y: 0 }}
              exit={prefersReducedMotion ? undefined : { opacity: 0, y: -6 }}
              transition={fade}
              className="m-0"
            >
              {headerDescription}
            </motion.p>
          </AnimatePresence>
        }
      />

      <div className="flex flex-col">
        {PROVISIONING_PHASES.map((phase, index) => {
          const status = phaseStatusList[index]
          const showConnector = index < PROVISIONING_PHASES.length - 1

          return (
            <div key={phase.title}>
              <ProvisioningPhaseItem {...phase} status={status} />
              {showConnector ? (
                <div className={cn(PHASE_HORIZONTAL_PADDING, "py-3.5")}>
                  <div className="flex w-[18px] justify-center">
                    <PhaseConnector isComplete={status === "success"} />
                  </div>
                </div>
              ) : null}
            </div>
          )
        })}
      </div>

      {provisioningError ? (
        <div
          className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700"
          role="alert"
        >
          {provisioningError}
        </div>
      ) : null}

      <GuestLoopStepFooter
        steps={steps}
        activeStep={activeStep}
        markActiveStepComplete={isWorkspaceReady && !provisioningError}
      >
        {provisioningError ? (
          <GuestLoopStepButton
            enabled={!isProvisioningActive}
            onClick={onRetry}
          >
            Retry
          </GuestLoopStepButton>
        ) : (
          <GuestLoopStepButton
            enabled={isWorkspaceReady}
            onClick={onOpenWorkspace}
          >
            Open workspace
          </GuestLoopStepButton>
        )}
      </GuestLoopStepFooter>
    </div>
  )
}

export type ProvisioningPhaseStatus = "idle" | "loading" | "success"

export const PROVISIONING_PHASE_MIN_MS = 2500

function sleep(ms: number) {
  return new Promise<void>((resolve) => {
    window.setTimeout(resolve, ms)
  })
}

export interface ProvisioningPhaseSnapshot {
  phase1: ProvisioningPhaseStatus
  phase2: ProvisioningPhaseStatus
  phase3: ProvisioningPhaseStatus
}

export async function runProvisioningPhases(
  runSetup: () => Promise<void>,
  onUpdate: (snapshot: ProvisioningPhaseSnapshot) => void,
  runPhase3: () => Promise<void> = async () => {}
): Promise<{ success: true } | { success: false; message: string }> {
  onUpdate({ phase1: "loading", phase2: "idle", phase3: "idle" })

  const phaseOneStartedAt = Date.now()

  try {
    await runSetup()

    const phaseOneElapsed = Date.now() - phaseOneStartedAt
    await sleep(Math.max(0, PROVISIONING_PHASE_MIN_MS - phaseOneElapsed))

    onUpdate({ phase1: "success", phase2: "loading", phase3: "idle" })
    await sleep(PROVISIONING_PHASE_MIN_MS)

    onUpdate({ phase1: "success", phase2: "success", phase3: "loading" })
    await runPhase3()
    await sleep(PROVISIONING_PHASE_MIN_MS)

    onUpdate({ phase1: "success", phase2: "success", phase3: "success" })

    return { success: true }
  } catch (error: unknown) {
    onUpdate({ phase1: "idle", phase2: "idle", phase3: "idle" })

    const message =
      error instanceof Error && error.message.trim()
        ? error.message.trim()
        : "Something went wrong during onboarding processing."

    return { success: false, message }
  }
}

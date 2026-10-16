/** Client flag: product demo tour chrome + forced light theme on the real dashboard. */

export const DEMO_TOUR_SESSION_KEY = "tummly-product-demo-tour"

export type DemoTourSessionState = {
  stepIndex: number
  exploreOpen: boolean
  phase: "tour" | "exit"
}

export function isDemoTourActive(): boolean {
  if (typeof sessionStorage === "undefined") {
    return false
  }
  return sessionStorage.getItem(DEMO_TOUR_SESSION_KEY) != null
}

export function readDemoTourSession(): DemoTourSessionState | null {
  if (typeof sessionStorage === "undefined") {
    return null
  }
  const raw = sessionStorage.getItem(DEMO_TOUR_SESSION_KEY)
  if (!raw) {
    return null
  }
  try {
    const parsed = JSON.parse(raw) as Partial<DemoTourSessionState>
    if (
      parsed.phase !== "tour" &&
      parsed.phase !== "exit"
    ) {
      return null
    }
    return {
      stepIndex:
        typeof parsed.stepIndex === "number" && parsed.stepIndex >= 0
          ? parsed.stepIndex
          : 0,
      exploreOpen: Boolean(parsed.exploreOpen),
      phase: parsed.phase,
    }
  } catch {
    return null
  }
}

function notifyDemoTourChange(): void {
  if (typeof window === "undefined") {
    return
  }
  window.dispatchEvent(new Event("tummly-demo-tour-change"))
}

export function writeDemoTourSession(state: DemoTourSessionState): void {
  if (typeof sessionStorage === "undefined") {
    return
  }
  sessionStorage.setItem(DEMO_TOUR_SESSION_KEY, JSON.stringify(state))
  notifyDemoTourChange()
}

export function startDemoTourSession(stepIndex = 0): void {
  writeDemoTourSession({
    stepIndex,
    exploreOpen: true,
    phase: "tour",
  })
}

export function clearDemoTourSession(): void {
  if (typeof sessionStorage === "undefined") {
    return
  }
  sessionStorage.removeItem(DEMO_TOUR_SESSION_KEY)
  notifyDemoTourChange()
}

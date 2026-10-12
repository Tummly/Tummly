import { aiAssistantVideoUrl } from "@/lib/marketingMedia"

let warmed = false

/**
 * Prefetch Guest Loop + hero videos so the AI Assistant page media is ready
 * after scroll. Uses `prefetch` (not high-priority preload).
 */
export function warmAiAssistantMedia(): void {
  if (warmed || typeof document === "undefined") {
    return
  }

  warmed = true

  for (const key of ["guestLoop", "heroMotion"] as const) {
    const link = document.createElement("link")
    link.rel = "prefetch"
    link.as = "video"
    link.type = "video/mp4"
    link.href = aiAssistantVideoUrl(key)
    document.head.appendChild(link)
  }
}

/** Idle-schedule {@link warmAiAssistantMedia} so it does not compete with LCP. */
export function scheduleWarmAiAssistantMedia(): () => void {
  if (typeof window === "undefined") {
    return () => {}
  }

  const idleId =
    "requestIdleCallback" in window
      ? window.requestIdleCallback(() => warmAiAssistantMedia())
      : window.setTimeout(() => warmAiAssistantMedia(), 1)

  return () => {
    if ("cancelIdleCallback" in window && typeof idleId === "number") {
      window.cancelIdleCallback(idleId)
    } else {
      window.clearTimeout(idleId)
    }
  }
}

/**
 * Public marketing media on Azure Blob (`marketing` container).
 * Override per env with `VITE_MARKETING_MEDIA_BASE_URL` (no trailing slash).
 *
 * Publish / refresh image variants (from repo root):
 *   npm run marketing-media:publish
 * Generate only (no upload):
 *   npm run marketing-media:generate
 */
const DEFAULT_MARKETING_MEDIA_BASE =
  "https://sttummlyqavfavue.blob.core.windows.net/marketing"

export function marketingMediaBaseUrl(): string {
  const fromEnv = import.meta.env.VITE_MARKETING_MEDIA_BASE_URL?.trim()
  if (fromEnv != null && fromEnv !== "") {
    return fromEnv.replace(/\/$/, "")
  }
  return DEFAULT_MARKETING_MEDIA_BASE
}

/** Build an absolute URL for a blob under the marketing container. */
export function marketingMediaUrl(path: string): string {
  const clean = path.replace(/^\/+/, "")
  return `${marketingMediaBaseUrl()}/${clean}`
}

/** Looping Sign-up CTA background video (`marketing/cta/signup-cta.mp4`). */
export const SIGN_UP_CTA_VIDEO_PATH = "cta/signup-cta.mp4"

export function signUpCtaVideoUrl(): string {
  return marketingMediaUrl(SIGN_UP_CTA_VIDEO_PATH)
}

/** AI Assistant marketing page videos (`marketing/ai-assistant/*`). */
export const AI_ASSISTANT_VIDEO_PATHS = {
  guestLoop: "ai-assistant/guest-loop.mp4",
  heroMotion: "ai-assistant/hero-motion.mp4",
  stepAsk: "ai-assistant/step-ask.mp4",
  stepContext: "ai-assistant/step-context.mp4",
  stepPermissions: "ai-assistant/step-context-check.mp4",
  stepDraft: "ai-assistant/step-draft.mp4",
  stepReview: "ai-assistant/step-review.mp4",
} as const

export type AiAssistantVideoKey = keyof typeof AI_ASSISTANT_VIDEO_PATHS

export function aiAssistantVideoUrl(key: AiAssistantVideoKey): string {
  return marketingMediaUrl(AI_ASSISTANT_VIDEO_PATHS[key])
}

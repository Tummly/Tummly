/**
 * Public marketing media on Azure Blob (`marketing` container).
 * Override per env with `VITE_MARKETING_MEDIA_BASE_URL` (no trailing slash).
 *
 * Publish / refresh image variants (from repo root):
 *   npm run marketing-media:publish
 *   npm run marketing-media:publish -- --new              # ids missing from meta
 *   npm run marketing-media:publish -- --only product     # id or prefix
 * Generate only (no upload):
 *   npm run marketing-media:generate
 *
 * 2× re-export checklist:
 *   .scratch/marketing-media-sources/IMAGE-QUALITY-REEPORT.md
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

/** Looping Sign-up CTA background video (`marketing/cta/signup-cta-feedback-form.mp4`). */
export const SIGN_UP_CTA_VIDEO_PATH = "cta/signup-cta-feedback-form.mp4"

export function signUpCtaVideoUrl(): string {
  return marketingMediaUrl(SIGN_UP_CTA_VIDEO_PATH)
}

/**
 * Home hero campaign results loop, 1920×1080 web copy of the 4K master.
 * The older `home/hero-campaign.mp4` stays on Blob for cached pages.
 */
export const HOME_HERO_VIDEO_PATH = "home/hero-campaign-results-loop-1080.mp4"

export function homeHeroVideoUrl(): string {
  return marketingMediaUrl(HOME_HERO_VIDEO_PATH)
}

/**
 * For Groups “ONE CONNECTED GUEST LOOP” product card loops
 * (`marketing/for-groups/product-e1dfdb-*.mp4` — beige plate masters, no re-encode).
 */
export const FOR_GROUPS_PRODUCT_VIDEO_PATHS = {
  "hear-from-guests": "for-groups/product-e1dfdb-hear-from-guests.mp4",
  "guest-relationships": "for-groups/product-e1dfdb-guest-relationships.mp4",
  "reasons-to-return": "for-groups/product-e1dfdb-reasons-to-return.mp4",
  "whats-working": "for-groups/product-e1dfdb-whats-working.mp4",
} as const

export type ForGroupsProductVideoKey = keyof typeof FOR_GROUPS_PRODUCT_VIDEO_PATHS

export function forGroupsProductVideoUrl(key: ForGroupsProductVideoKey): string {
  return marketingMediaUrl(FOR_GROUPS_PRODUCT_VIDEO_PATHS[key])
}

/**
 * Home platform card loops: the same For Groups loops, re-encoded to 864×1080
 * (`*-864.mp4`). The 2160×2700 masters decode about 6.5× slower and made the
 * three cards stutter; the cards draw them at about 400×500px.
 */
export const HOME_PLATFORM_VIDEO_PATHS = {
  "guest-relationships": "for-groups/product-e1dfdb-guest-relationships-864.mp4",
  "reasons-to-return": "for-groups/product-e1dfdb-reasons-to-return-864.mp4",
  "whats-working": "for-groups/product-e1dfdb-whats-working-864.mp4",
} as const

export type HomePlatformVideoKey = keyof typeof HOME_PLATFORM_VIDEO_PATHS

export function homePlatformVideoUrl(key: HomePlatformVideoKey): string {
  return marketingMediaUrl(HOME_PLATFORM_VIDEO_PATHS[key])
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

/** About “restaurant reality” card motion (`marketing/about/reality-*.mp4`). */
export const ABOUT_REALITY_VIDEO_PATHS = {
  return: "about/reality-return.mp4",
  relationships: "about/reality-relationships.mp4",
  // Composited on white — source WebMs keep alpha; H.264 MP4 cannot.
  together: "about/reality-together-on-white.mp4",
  visibility: "about/reality-visibility-on-white.mp4",
} as const

export type AboutRealityVideoKey = keyof typeof ABOUT_REALITY_VIDEO_PATHS

export function aboutRealityVideoUrl(key: AboutRealityVideoKey): string {
  return marketingMediaUrl(ABOUT_REALITY_VIDEO_PATHS[key])
}

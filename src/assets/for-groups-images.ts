/**
 * For Groups local plates as responsive webp (vite-imagetools). Widths follow the
 * displayed size: hero 1548px, AI plate 720px, sign-up plate 1668px, plus 2x.
 */
import heroSrc from "./images/marketing/for-groups/hero.webp?w=1548&format=webp"
import heroSrcSet from "./images/marketing/for-groups/hero.webp?w=750;1100;1548;2400&format=webp&as=srcset"
import aiSrc from "./images/marketing/for-groups/ai-assistant.webp?w=720&format=webp"
import aiSrcSet from "./images/marketing/for-groups/ai-assistant.webp?w=640;1024;1440;2160&format=webp&as=srcset"
import posterHear from "./images/marketing/for-groups/guest-loop-hear-from-guests-poster.webp?w=800&format=webp"
import posterGuest from "./images/marketing/for-groups/guest-loop-guest-relationships-poster.webp?w=800&format=webp"
import posterReasons from "./images/marketing/for-groups/guest-loop-reasons-to-return-poster.webp?w=800&format=webp"
import posterWorking from "./images/marketing/for-groups/guest-loop-whats-working-poster.webp?w=800&format=webp"
import signUpSrc from "./images/marketing/for-groups/sign-up-bg.webp?w=1668&format=webp"
import signUpSrcSet from "./images/marketing/for-groups/sign-up-bg.webp?w=750;1200;1668;2160&format=webp&as=srcset"

export const forGroupsHeroImage = {
  src: heroSrc,
  srcSet: heroSrcSet,
  width: 3096,
  height: 1526,
} as const

export const forGroupsAiImage = {
  src: aiSrc,
  srcSet: aiSrcSet,
  width: 2880,
  height: 2600,
} as const

export const forGroupsSignUpImage = {
  src: signUpSrc,
  srcSet: signUpSrcSet,
  width: 2160,
  height: 2160,
} as const

/**
 * Keep the Guest Loop working card stills (Figma `6405:36122`), shown as the
 * video `poster` and under reduced motion. 800×1000 (4:5) for the 496px card at
 * 2x; a `poster` takes one URL, so there is no srcset. Each still is the Figma
 * image moved down to the clip's UI-card position so it lines up with the video.
 */
export const forGroupsGuestLoopPosters = {
  "hear-from-guests": posterHear,
  "guest-relationships": posterGuest,
  "reasons-to-return": posterReasons,
  "whats-working": posterWorking,
} as const

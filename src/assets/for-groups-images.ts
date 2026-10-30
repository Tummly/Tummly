/**
 * For Groups local plates as responsive webp (vite-imagetools). Widths follow the
 * displayed size: hero 1548px, AI plate 720px, sign-up plate 1668px, plus 2x.
 */
import heroSrc from "./images/marketing/for-groups/hero.webp?w=1548&format=webp"
import heroSrcSet from "./images/marketing/for-groups/hero.webp?w=750;1100;1548;2400&format=webp&as=srcset"
import aiSrc from "./images/marketing/for-groups/ai-assistant.webp?w=720&format=webp"
import aiSrcSet from "./images/marketing/for-groups/ai-assistant.webp?w=640;1024;1440;2160&format=webp&as=srcset"
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

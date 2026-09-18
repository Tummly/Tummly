/**
 * Barrel re-exports for critical LCP pictures.
 * Prefer importing from `@/assets/critical-images/<name>` in route-critical
 * code so Vite does not pull sibling transforms into the module graph.
 */
export { authSignInPanelPicture } from "./critical-images/auth-sign-in-panel"
export { heroBgPicture, heroBgMobilePicture } from "./critical-images/hero"
export { helpCenterBgPicture } from "./critical-images/help-center-bg"

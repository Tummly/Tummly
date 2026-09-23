import { authSignInPanelPicture } from "@/assets/critical-images/auth-sign-in-panel"

const AUTH_HERO_IMAGE_SIZES = "(min-width: 1024px) 41.3vw, 0px"

let prefetched = false

/** Warm auth Sign-in panel AVIFs before navigating to `/login`. */
export function prefetchAuthImages(): void {
  if (prefetched || typeof document === "undefined") {
    return
  }

  prefetched = true

  const avifSrcset = authSignInPanelPicture.sources.avif

  if (avifSrcset) {
    // Intentional hover/focus → use preload + high so the panel is ready on arrival.
    const link = document.createElement("link")
    link.rel = "preload"
    link.as = "image"
    link.type = "image/avif"
    link.setAttribute("imagesrcset", avifSrcset)
    link.setAttribute("imagesizes", AUTH_HERO_IMAGE_SIZES)
    link.fetchPriority = "high"
    document.head.appendChild(link)
  }
}

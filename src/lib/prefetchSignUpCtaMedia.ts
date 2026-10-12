import { signupCtaBgPicture } from "@/assets/marketing-images"
import { PANORAMIC_BG_IMAGE_SIZES } from "@/lib/imagePresets"
import { signUpCtaVideoUrl } from "@/lib/marketingMedia"

let warmed = false

/**
 * Prefetch Sign-up CTA poster AVIFs + MP4 so the bottom CTA is ready after
 * scroll. Uses `prefetch` (not high-priority preload) — the video is ~11MB.
 */
export function warmSignUpCtaMedia(): void {
  if (warmed || typeof document === "undefined") {
    return
  }

  warmed = true

  const avifSrcset = signupCtaBgPicture.sources.avif
  if (avifSrcset) {
    const imageLink = document.createElement("link")
    imageLink.rel = "prefetch"
    imageLink.as = "image"
    imageLink.type = "image/avif"
    imageLink.setAttribute("imagesrcset", avifSrcset)
    imageLink.setAttribute("imagesizes", PANORAMIC_BG_IMAGE_SIZES)
    document.head.appendChild(imageLink)
  }

  const videoLink = document.createElement("link")
  videoLink.rel = "prefetch"
  videoLink.as = "video"
  videoLink.type = "video/mp4"
  videoLink.href = signUpCtaVideoUrl()
  document.head.appendChild(videoLink)
}

/** Idle-schedule {@link warmSignUpCtaMedia} so it does not compete with LCP. */
export function scheduleWarmSignUpCtaMedia(): () => void {
  if (typeof window === "undefined") {
    return () => {}
  }

  const idleId =
    "requestIdleCallback" in window
      ? window.requestIdleCallback(() => warmSignUpCtaMedia())
      : window.setTimeout(() => warmSignUpCtaMedia(), 1)

  return () => {
    if ("cancelIdleCallback" in window && typeof idleId === "number") {
      window.cancelIdleCallback(idleId)
    } else {
      window.clearTimeout(idleId)
    }
  }
}

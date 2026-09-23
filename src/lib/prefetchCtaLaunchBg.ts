import { ctaLaunchBgPicture } from "@/assets/marketing-images"
import { PANORAMIC_BG_IMAGE_SIZES } from "@/lib/imagePresets"

let warmed = false

type WarmCtaLaunchBgOptions = {
  /** FAQ page has no image LCP — raise priority so the CTA is ready before scroll. */
  priority?: boolean
}

/** Warm CTA / FAQ sign-up panoramic AVIFs before the section scrolls into view. */
export function warmCtaLaunchBg(
  options: WarmCtaLaunchBgOptions = {},
): void {
  if (warmed || typeof document === "undefined") {
    return
  }

  warmed = true

  const avifSrcset = ctaLaunchBgPicture.sources.avif
  if (!avifSrcset) {
    return
  }

  const link = document.createElement("link")
  link.rel = options.priority === true ? "preload" : "prefetch"
  link.as = "image"
  link.type = "image/avif"
  link.setAttribute("imagesrcset", avifSrcset)
  link.setAttribute("imagesizes", PANORAMIC_BG_IMAGE_SIZES)
  if (options.priority === true) {
    link.fetchPriority = "high"
  }
  document.head.appendChild(link)
}

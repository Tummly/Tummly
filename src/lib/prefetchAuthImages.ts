import { authSignInPanelPicture } from "@/assets/critical-images"

const AUTH_HERO_IMAGE_SIZES = "(min-width: 1024px) 41.3vw, 0px"

let prefetched = false

function appendImagePrefetch(
  attributes: Record<string, string | undefined>,
): void {
  const link = document.createElement("link")

  for (const [name, value] of Object.entries(attributes)) {
    if (value) {
      link.setAttribute(name, value)
    }
  }

  document.head.appendChild(link)
}

/** Warm auth Sign-in panel AVIFs before navigating to `/login`. */
export function prefetchAuthImages(): void {
  if (prefetched || typeof document === "undefined") {
    return
  }

  prefetched = true

  const avifSrcset = authSignInPanelPicture.sources.avif

  if (avifSrcset) {
    appendImagePrefetch({
      rel: "prefetch",
      as: "image",
      type: "image/avif",
      imagesrcset: avifSrcset,
      imagesizes: AUTH_HERO_IMAGE_SIZES,
    })
  }
}

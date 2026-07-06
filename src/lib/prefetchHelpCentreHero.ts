import { helpCenterBgPicture } from "@/assets/critical-images";
import { FULL_BLEED_IMAGE_SIZES } from "@/lib/imagePresets";

let prefetched = false;

function appendImagePrefetch(
  attributes: Record<string, string | undefined>,
): void {
  const link = document.createElement("link");

  for (const [name, value] of Object.entries(attributes)) {
    if (value) {
      link.setAttribute(name, value);
    }
  }

  document.head.appendChild(link);
}

/** Warm Help Centre hero AVIFs before navigating to `/help-center`. */
export function prefetchHelpCentreHero(): void {
  if (prefetched || typeof document === "undefined") {
    return;
  }

  prefetched = true;

  const avifSrcset = helpCenterBgPicture.sources.avif;

  if (avifSrcset) {
    appendImagePrefetch({
      rel: "prefetch",
      as: "image",
      type: "image/avif",
      imagesrcset: avifSrcset,
      imagesizes: FULL_BLEED_IMAGE_SIZES,
    });
  }
}

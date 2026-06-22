import {
  authHeroFrame267Picture,
  authHeroFrame268Picture,
} from "@/assets/critical-images";
import authHeroLogo from "@/assets/images/auth-hero-logo.png";

const AUTH_HERO_IMAGE_SIZES = "(min-width: 1024px) 45.38vw, 0px";

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

/** Warm auth hero AVIFs and logo before navigating to `/login`. */
export function prefetchAuthImages(): void {
  if (prefetched || typeof document === "undefined") {
    return;
  }

  prefetched = true;

  const avifSrcsets = [
    authHeroFrame267Picture.sources.avif,
    authHeroFrame268Picture.sources.avif,
  ];

  for (const srcset of avifSrcsets) {
    if (!srcset) {
      continue;
    }

    appendImagePrefetch({
      rel: "prefetch",
      as: "image",
      type: "image/avif",
      imagesrcset: srcset,
      imagesizes: AUTH_HERO_IMAGE_SIZES,
    });
  }

  appendImagePrefetch({
    rel: "prefetch",
    as: "image",
    href: authHeroLogo,
  });
}

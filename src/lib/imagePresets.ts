/** Shared imagetools query suffix — quality 100, AVIF + WebP + lossless fallback. */
export const IMAGE_QUALITY = "quality=100" as const;

/** Below-fold card / carousel images (~392–436px wide at lg). */
export const CARD_WIDTHS = "640;800;1024" as const;

/** Full-bleed section backgrounds. */
export const HERO_WIDTHS = "640;1024;1536;1920" as const;

/** Wide panoramic full-bleed backgrounds (includes native width when ≤ 2362px). */
export const PANORAMIC_BG_WIDTHS = "640;1024;1536;1920;2362" as const;

/** Repeating decorative strips. */
export const STRIP_WIDTHS = "512;768;1024" as const;

export const CARD_IMAGE_SIZES =
  "(min-width: 1280px) 30vw, (min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw" as const;

export const GRID_CARD_IMAGE_SIZES =
  "(min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw" as const;

export const FULL_BLEED_IMAGE_SIZES = "100vw" as const;

/**
 * Panoramic backgrounds in fixed-height sections (e.g. CTA launch at 688px).
 * Below ~1726px viewport, object-cover is height-limited so the bitmap renders
 * wider than the viewport — `100vw` undershoots and the browser picks a soft srcset candidate.
 */
export const PANORAMIC_BG_IMAGE_SIZES =
  "(min-width: 1726px) 100vw, 1726px" as const;

/** Shared imagetools query suffix — quality 100, AVIF + WebP + lossless fallback. */
export const IMAGE_QUALITY = "quality=100" as const;

/** Below-fold card / carousel images (~392–436px wide at lg). */
export const CARD_WIDTHS = "640;800;1024" as const;

/** Full-bleed section backgrounds. */
export const HERO_WIDTHS = "640;1024;1536;1920" as const;

/** Repeating decorative strips. */
export const STRIP_WIDTHS = "512;768;1024" as const;

export const CARD_IMAGE_SIZES =
  "(min-width: 1280px) 30vw, (min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw" as const;

export const GRID_CARD_IMAGE_SIZES =
  "(min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw" as const;

export const FULL_BLEED_IMAGE_SIZES = "100vw" as const;

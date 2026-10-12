/**
 * Marketing / auth / help-centre images to publish to Azure Blob.
 * Widths + formats mirror current Vite imagetools queries.
 * Excludes: operator, shop, guest-feedback, unused placeholders.
 */

/** @typedef {"avif" | "webp" | "png" | "jpg"} Format */

/**
 * @typedef {object} PictureAsset
 * @property {string} id
 * @property {string} source relative to repo root
 * @property {string} blobDir under marketing container (no trailing slash)
 * @property {string} baseName file stem without width/format
 * @property {number[]} widths
 * @property {Format[]} formats
 * @property {number} quality 1–100
 * @property {"png" | "jpg"} fallbackFormat
 */

/**
 * @typedef {object} SimpleAsset
 * @property {string} id
 * @property {string} source
 * @property {string} blobDir
 * @property {string} baseName
 * @property {number[]} widths single or few widths for plain <img> / email
 * @property {Format[]} formats
 * @property {number} quality
 * @property {"png" | "jpg" | "webp"} primaryFormat URL used by app
 */

/** @type {PictureAsset[]} */
export const PICTURE_ASSETS = [
  // Home carousels
  ...[1, 2].map((n) => ({
    id: `carousel/c1-${n}`,
    source: `src/assets/images/crousal/c1-${n}.png`,
    blobDir: "images/carousel",
    baseName: `c1-${n}`,
    widths: [640, 800, 1024],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    fallbackFormat: /** @type {"png"} */ ("png"),
  })),
  ...[3, 4].map((n) => ({
    id: `carousel/c1-${n}`,
    source: `src/assets/images/crousal/c1-${n}.jpg`,
    blobDir: "images/carousel",
    baseName: `c1-${n}`,
    widths: [640, 800, 1024],
    formats: /** @type {Format[]} */ (["avif", "webp", "jpg"]),
    quality: 100,
    fallbackFormat: /** @type {"jpg"} */ ("jpg"),
  })),
  ...[1, 2, 3, 4, 5, 6, 7, 8].map((n) => ({
    id: `carousel/c2-${n}`,
    source: `src/assets/images/crousal/c2-${n}.png`,
    blobDir: "images/carousel",
    baseName: `c2-${n}`,
    widths: [640, 800, 1024],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    fallbackFormat: /** @type {"png"} */ ("png"),
  })),
  // Why Tummly
  ...[1, 2].map((n) => ({
    id: `why-tummly/why-tummly-${n}`,
    source: `src/assets/images/why-tummly/why-tummly-${n}.png`,
    blobDir: "images/why-tummly",
    baseName: `why-tummly-${n}`,
    widths: [640, 800, 1024],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    fallbackFormat: /** @type {"png"} */ ("png"),
  })),
  {
    id: "why-tummly/why-tummly-3",
    source: "src/assets/images/why-tummly/why-tummly-3.jpg",
    blobDir: "images/why-tummly",
    baseName: "why-tummly-3",
    widths: [640, 800, 1024],
    formats: ["avif", "webp", "jpg"],
    quality: 100,
    fallbackFormat: "jpg",
  },
  // How it works
  ...[1, 2, 3].map((n) => ({
    id: `how-it-works/how-it-works-${n}`,
    source: `src/assets/images/how-it-works/how-it-works-${n}.png`,
    blobDir: "images/how-it-works",
    baseName: `how-it-works-${n}`,
    widths: [640, 800, 1024],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    fallbackFormat: /** @type {"png"} */ ("png"),
  })),
  // Heroes / CTA
  {
    id: "hero/hero-bg",
    source: "src/assets/images/hero-bg.png",
    blobDir: "images/hero",
    baseName: "hero-bg",
    widths: [640, 1024, 1536, 1920],
    formats: ["avif", "webp", "png"],
    quality: 100,
    fallbackFormat: "png",
  },
  {
    id: "hero/hero-bg-mobile",
    source: "src/assets/images/hero-bg-mobile.png",
    blobDir: "images/hero",
    baseName: "hero-bg-mobile",
    widths: [393, 786],
    formats: ["avif", "webp", "png"],
    quality: 100,
    fallbackFormat: "png",
  },
  {
    id: "cta/cta-launch-bg",
    source: "src/assets/images/cta-launch-bg.png",
    blobDir: "images/cta",
    baseName: "cta-launch-bg",
    widths: [640, 1024, 1536, 1920, 2362],
    formats: ["avif", "webp", "png"],
    quality: 100,
    fallbackFormat: "png",
  },
  {
    id: "cta/signup-cta-bg",
    source: "src/assets/images/marketing/signup-cta-bg.png",
    blobDir: "images/cta",
    baseName: "signup-cta-bg",
    widths: [640, 1024, 1536, 1920],
    formats: ["avif", "webp", "png"],
    quality: 85,
    fallbackFormat: "png",
  },
  // Auth / help centre
  {
    id: "auth/auth-sign-in-panel",
    source: "src/assets/images/auth-sign-in-panel.jpg",
    blobDir: "images/auth",
    baseName: "auth-sign-in-panel",
    widths: [714, 1071, 1428],
    formats: ["avif", "webp", "jpg"],
    quality: 100,
    fallbackFormat: "jpg",
  },
  {
    id: "help-centre/help-center-bg",
    source: "src/assets/images/help-center-bg.jpg",
    blobDir: "images/help-centre",
    baseName: "help-center-bg",
    widths: [393, 640, 1024, 1536, 1920],
    formats: ["avif", "webp", "jpg"],
    quality: 85,
    fallbackFormat: "jpg",
  },
]

/** Plain <img> / email assets — one primary URL + optional extra formats. */
/** @type {SimpleAsset[]} */
export const SIMPLE_ASSETS = [
  {
    id: "pricing/qr-featured-guest-cards",
    source: "src/assets/images/marketing/pricing/qr-featured-guest-cards.png",
    blobDir: "images/pricing",
    baseName: "qr-featured-guest-cards",
    widths: [640, 1024],
    formats: ["avif", "webp", "png"],
    quality: 85,
    primaryFormat: "webp",
  },
  {
    id: "pricing/qr-peek-01",
    source: "src/assets/images/marketing/pricing/qr-peek-01.png",
    blobDir: "images/pricing",
    baseName: "qr-peek-01",
    widths: [640, 1024],
    formats: ["avif", "webp", "png"],
    quality: 85,
    primaryFormat: "webp",
  },
  {
    id: "pricing/qr-peek-02",
    source: "src/assets/images/marketing/pricing/qr-peek-02.jpg",
    blobDir: "images/pricing",
    baseName: "qr-peek-02",
    widths: [640, 1024],
    formats: ["avif", "webp", "jpg"],
    quality: 85,
    primaryFormat: "webp",
  },
  {
    id: "pricing/qr-peek-04",
    source: "src/assets/images/marketing/pricing/qr-peek-04.jpg",
    blobDir: "images/pricing",
    baseName: "qr-peek-04",
    widths: [640, 1024],
    formats: ["avif", "webp", "jpg"],
    quality: 85,
    primaryFormat: "webp",
  },
  {
    id: "pricing/qr-peek-05",
    source: "src/assets/images/marketing/pricing/qr-peek-05.jpg",
    blobDir: "images/pricing",
    baseName: "qr-peek-05",
    widths: [640, 1024],
    formats: ["avif", "webp", "jpg"],
    quality: 85,
    primaryFormat: "webp",
  },
  {
    id: "pricing/qr-peek-06",
    source: "src/assets/images/marketing/pricing/qr-peek-06.png",
    blobDir: "images/pricing",
    baseName: "qr-peek-06",
    widths: [640, 1024],
    formats: ["avif", "webp", "png"],
    quality: 85,
    primaryFormat: "webp",
  },
  {
    id: "signup/signup-modal-backdrop",
    source: "src/assets/images/signup-modal-backdrop.png",
    blobDir: "images/signup",
    baseName: "signup-modal-backdrop",
    widths: [1024, 1536],
    formats: ["avif", "webp", "png"],
    quality: 85,
    primaryFormat: "webp",
  },
  {
    id: "auth/auth-headline-brush",
    source: "src/assets/images/auth-headline-brush.png",
    blobDir: "images/auth",
    baseName: "auth-headline-brush",
    widths: [256],
    formats: ["png"],
    quality: 100,
    primaryFormat: "png",
  },
  {
    id: "auth/auth-hero-logo",
    source: "src/assets/images/auth-hero-logo.png",
    blobDir: "images/auth",
    baseName: "auth-hero-logo",
    widths: [256],
    formats: ["png"],
    quality: 100,
    primaryFormat: "png",
  },
]

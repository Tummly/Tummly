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
  // Product page UI mocks — sources live in gitignored scratch; runtime via Blob.
  // Hero collage (Figma `5838:27265`) — baseName distinct from retired
  // `hero-dashboard` so immutable blob URLs cache-bust after visual swaps.
  {
    id: "product/hero-collage",
    source: ".scratch/marketing-media-sources/product/hero-dashboard.png",
    blobDir: "images/product",
    baseName: "hero-collage",
    widths: [766],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 85,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  ...[
    "feedback-phone",
    "feedback-recovery",
    "guests",
    "campaigns",
    "reports",
    "ai-assistant",
  ].map((name) => ({
    id: `product/${name}`,
    source: `.scratch/marketing-media-sources/product/${name}.png`,
    blobDir: "images/product",
    baseName: name,
    widths: [766],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 85,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  })),
  // Product Capture scroll-hijack media (Figma `6106:17567` … `6106:18055`)
  ...[
    "capture-guest-cards",
    "capture-counter-display",
    "capture-tray-liner",
    "capture-table-tent",
    "capture-table-talker",
    "capture-window-sticker",
    "capture-receipt-label",
    "capture-delivery-bag-seal",
    "capture-package-label",
    "capture-drink-cup-seal",
    "capture-pizza-box-label",
    "capture-poster-club",
  ].map((name) => ({
    id: `product/${name}`,
    source: `.scratch/marketing-media-sources/product/${name}.png`,
    blobDir: "images/product",
    baseName: name,
    widths: [750],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 85,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  })),
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

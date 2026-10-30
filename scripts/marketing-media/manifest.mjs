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
  // Product page UI mock — source lives in gitignored scratch; runtime via Blob.
  // Product UI mocks display at max 766 CSS px → need ≥1532 master for 2×.
  // Until masters are replaced, publish skips 1532 and keeps 766 (see checklist).
  {
    id: "product/feedback-phone",
    source: ".scratch/marketing-media-sources/product/feedback-phone.png",
    blobDir: "images/product",
    baseName: "feedback-phone",
    widths: [766, 1532],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // Product Capture scroll-hijack media (Figma `6106:17567` … `6106:18055`)
  // Sources are often ~1100–1448; request 1500 so publish emits native max.
  // `-v2` sources are the Figma frame crops (750×661); a new baseName keeps
  // the immutable Blob cache from serving the old photos.
  ...[
    "capture-guest-cards-v2",
    "capture-counter-display-v2",
    "capture-tray-liner-v2",
    "capture-table-tent",
    "capture-table-talker-v2",
    "capture-window-sticker-v2",
    "capture-receipt-label-v2",
    "capture-delivery-bag-seal-v2",
    "capture-package-label-v2",
    "capture-drink-cup-seal-v2",
    "capture-pizza-box-label-v2",
    "capture-poster-club-v2",
  ].map((name) => ({
    id: `product/${name.replace(/-v\d+$/, "")}`,
    source: `.scratch/marketing-media-sources/product/${name}.png`,
    blobDir: "images/product",
    baseName: name,
    widths: [750, 1500],
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
  // About hero band (Figma `5673:24165`) — full-bleed ~1668 CSS px.
  // Master exported @3× (5004); emit through 3336 (2×) for reliable encode.
  {
    id: "about/hero-band",
    source: ".scratch/marketing-media-sources/about/hero-band-master.png",
    blobDir: "images/about",
    baseName: "hero-band",
    widths: [834, 1668, 3336],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 95,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // About Industries cards (Figma `6367:30260`) — card ~374 CSS px, but the
  // absolute photo frame is ~1.5–1.7× card width (~585–640 CSS px). Cover
  // 1× / 2× of that frame (clamped to native master).
  ...["cafes", "takeaway", "casual", "groups"].map((name) => ({
    id: `about/industry-${name}`,
    source: `.scratch/marketing-media-sources/about/industry-${name}.png`,
    blobDir: "images/about",
    baseName: `industry-${name}`,
    widths: [640, 1280, 1672],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 95,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  })),
  // About Principles icons (Figma `5673:24211`) — display ~98 CSS px.
  // Masters @4×; publish through ~4× for sharp DPR.
  ...["heard", "listening", "trust", "return", "location"].map((name) => ({
    id: `about/principle-${name}`,
    source: `.scratch/marketing-media-sources/about/principle-${name}.png`,
    blobDir: "images/about",
    baseName: `principle-${name}`,
    widths: [98, 196, 392],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    primaryFormat: /** @type {"png"} */ ("png"),
  })),
  // About Reality illustrations (Figma `5673:24194`) — display ~250 CSS px.
  // Masters @4× (~900); publish through native width for 3×+ DPR.
  ...["return", "relationships", "together", "visibility"].map((name) => ({
    id: `about/reality-${name}`,
    source: `.scratch/marketing-media-sources/about/reality-${name}.png`,
    blobDir: "images/about",
    baseName: `reality-${name}`,
    widths: [250, 500, 900],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    primaryFormat: /** @type {"png"} */ ("png"),
  })),
  // How it works Guest Form phone mockups (Figma `5887:20625` … `5880:19826`)
  // Display ~392 CSS px → 784 master for 2×. quality 90 for UI chrome.
  ...[
    "guest-form-step-01",
    "guest-form-step-02",
    "guest-form-step-03",
    "guest-form-step-04",
    "guest-form-step-05",
  ].map((name) => ({
    id: `how-it-works/${name}`,
    source: `.scratch/marketing-media-sources/how-it-works/${name}.png`,
    blobDir: "images/how-it-works",
    baseName: name,
    widths: [392, 784],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  })),
  // QR materials hero product frame (Figma `5661:19346`) — display 671 CSS px.
  {
    id: "qr-materials/hero-product",
    source: "src/assets/images/marketing/qr-materials/hero-product.png",
    blobDir: "images/qr-materials",
    baseName: "hero-product",
    widths: [671, 1342],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // Pilot hero lifestyle + green glow (Figma `6426:17174` / page hero `5661:19874`).
  // Display 739 CSS px → 1478 master for 2×.
  {
    id: "pilot/hero",
    source: ".scratch/marketing-media-sources/pilot/hero.png",
    blobDir: "images/pilot",
    baseName: "hero",
    widths: [739, 1478],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // Pilot “Why Tummly” card illustrations (Figma `6543:18855`).
  {
    id: "pilot/why-hear",
    source: ".scratch/marketing-media-sources/pilot/why-hear.png",
    blobDir: "images/pilot",
    baseName: "why-hear",
    widths: [300, 600],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  {
    id: "pilot/why-relationships",
    source: ".scratch/marketing-media-sources/pilot/why-relationships.png",
    blobDir: "images/pilot",
    baseName: "why-relationships",
    widths: [326, 651],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  {
    id: "pilot/why-return",
    source: ".scratch/marketing-media-sources/pilot/why-return.png",
    blobDir: "images/pilot",
    baseName: "why-return",
    widths: [266, 532],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // Pilot Getting Started composite (stairs + step copy) — Frame 5971 / Figma `6535:18445`.
  // Display ~753 CSS px → 1506 master for 2×.
  {
    id: "pilot/getting-started-steps",
    source: ".scratch/marketing-media-sources/pilot/getting-started-steps.png",
    blobDir: "images/pilot",
    baseName: "getting-started-steps",
    widths: [753, 1506],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // Pilot bottom split CTA frame (Figma `6464:35739`) — lifestyle + smoke strip
  // baked at 3× (2160×1950). Display 720×650 CSS → widths cover 1× / 2× / 3×.
  {
    id: "pilot/sign-up-cta",
    source: ".scratch/marketing-media-sources/pilot/sign-up-cta.png",
    blobDir: "images/pilot",
    baseName: "sign-up-cta-frame",
    widths: [720, 1440, 2160],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 95,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // Pilot “See Tummly in Action” tab mockups (Figma `6495:18145` … `6495:18254`).
  // Display 750 CSS px → 1500 master for 2× (sources are 2160²).
  ...[
    "in-action-hear",
    "in-action-connected",
    "in-action-opportunity",
    "in-action-working",
  ].map((name) => ({
    id: `pilot/${name}`,
    source: `.scratch/marketing-media-sources/pilot/${name}.png`,
    blobDir: "images/pilot",
    baseName: name,
    widths: [750, 1500],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 90,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  })),
  // For Groups hero photo lives in src/assets/images/marketing/for-groups/hero.webp.
  // For Groups Group-wide visibility (Figma `5661:20731`) — display 720 CSS px; 4× master.
  {
    id: "for-groups/feature-reporting",
    source: ".scratch/marketing-media-sources/for-groups/feature-reporting.png",
    blobDir: "images/for-groups",
    baseName: "feature-reporting",
    widths: [720, 1440, 2880],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // For Groups Location management (Figma `5661:21096`) — display 720 CSS px; 4× master.
  {
    id: "for-groups/feature-location-management",
    source:
      ".scratch/marketing-media-sources/for-groups/feature-location-management.png",
    blobDir: "images/for-groups",
    baseName: "feature-location-management",
    widths: [720, 1440, 2880],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // For Groups Team Access (Figma `5661:21425`) — display 720 CSS px; 4× master.
  {
    id: "for-groups/feature-teams",
    source: ".scratch/marketing-media-sources/for-groups/feature-teams.png",
    blobDir: "images/for-groups",
    baseName: "feature-teams",
    widths: [720, 1440, 2880],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // For Groups Offers & Campaigns (Figma `5661:21798`) — display 720 CSS px; 4× master.
  {
    id: "for-groups/feature-campaigns",
    source: ".scratch/marketing-media-sources/for-groups/feature-campaigns.png",
    blobDir: "images/for-groups",
    baseName: "feature-campaigns",
    widths: [720, 1440, 2880],
    formats: /** @type {Format[]} */ (["avif", "webp", "png"]),
    quality: 100,
    primaryFormat: /** @type {"webp"} */ ("webp"),
  },
  // For Groups AI assistant and Sign up CTA plates live in src/assets/images/marketing/for-groups.
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

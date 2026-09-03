/**
 * Shop surface fills — Figma Guest-Loop Shop.
 *
 * Page / sidenav chrome `#141414` / `--op-shell-chrome`.
 * Recommended parent `#141414` + border `#262626`.
 * Nested recommendation panel `#202020` / `--op-color-gray-990`.
 * Catalog product card (primary) body `#171717`, image well `#212121`.
 * Catalog product card (secondary) body + image `#202020` — “You may also need”.
 * Recommendation product card (horizontal) body `#171717`, image well `#212121`.
 * Checkout review / section cards `#171717`; nested options `#202020` / selected `#7c7c7c` border.
 */

/** Main pane behind Shop content — same fill as header / sidenav chrome. */
export const SHOP_PAGE_BACKGROUND_CLASS = "bg-op-shell-chrome"

/**
 * Recommended-for outer panel — Figma `4378:43973`.
 * `#fff` / shell chrome light, `#141414` dark; border `#262626`.
 */
export const SHOP_RECOMMENDATION_PARENT_CLASS =
  "flex flex-col gap-5 overflow-hidden rounded-op-lg border border-op-border-default bg-op-shell-chrome py-[25px]"

/** Header strip inside the parent (title + AI icon). */
export const SHOP_RECOMMENDATION_HEADER_CLASS =
  "flex flex-col items-start gap-2 px-6"

/**
 * Inner `#202020` panel holding Based on / product cards / summary.
 */
export const SHOP_RECOMMENDATION_INNER_CLASS =
  "mx-6 flex flex-col overflow-hidden rounded-[4px] bg-op-color-gray-60 dark:bg-[var(--op-color-gray-990)]"

/** Based-on bar — bottom border `#262626` / 4px. */
export const SHOP_RECOMMENDATION_BASED_ON_CLASS =
  "flex flex-col gap-[22px] border-b-4 border-op-border-default p-5 sm:flex-row sm:items-start sm:justify-between"

/** Tag chips — content width only; Figma tag fill `rgba(57,57,57,0.3)`. */
export const SHOP_RECOMMENDATION_TAG_CLASS =
  "inline-flex w-fit shrink-0 items-center justify-center rounded-[2px] border-0 bg-[rgba(57,57,57,0.12)] px-1.5 py-1 text-xs font-medium whitespace-nowrap text-op-text-primary dark:bg-[rgba(57,57,57,0.3)]"

/**
 * Recommendation product card — Figma `4384:47538` (562×241).
 * Horizontal; body `#171717`; image column 228×241; content 334×241; radius 6px.
 */
export const SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS =
  "group flex h-[241px] w-[562px] shrink-0 cursor-pointer items-stretch overflow-hidden rounded-op-lg border-0 bg-op-color-gray-60 dark:bg-[var(--op-color-gray-1000)]"

/** Image column — 228px; fill matches card `#171717` (product art carries its own well). */
export const SHOP_PRODUCT_CARD_RECOMMENDATION_IMAGE_CLASS =
  "relative h-full w-[228px] shrink-0 overflow-hidden bg-op-color-gray-60 dark:bg-[var(--op-color-gray-1000)]"

/** Content column — 334×241, 20px padding, 20px section gap. */
export const SHOP_PRODUCT_CARD_RECOMMENDATION_BODY_CLASS =
  "flex h-full w-[334px] shrink-0 flex-col gap-5 p-5"

/**
 * Catalog product card — Figma `4346:22831` (primary) / `4415:53168` (secondary).
 * No outer border; radius 6px (`--op-radius-lg`).
 */
export const SHOP_PRODUCT_CARD_PRIMARY_CLASS =
  "group flex w-full cursor-pointer flex-col justify-between overflow-hidden rounded-op-lg border-0 bg-op-color-gray-60 transition-all dark:bg-[var(--op-color-gray-1000)]"

/** Primary image well — Figma `#212121` / `--op-color-gray-985`. */
export const SHOP_PRODUCT_CARD_PRIMARY_IMAGE_CLASS =
  "relative flex h-[232px] w-full shrink-0 items-center justify-center overflow-hidden bg-op-color-gray-85 dark:bg-[var(--op-color-gray-985)]"

export const SHOP_PRODUCT_CARD_SECONDARY_CLASS =
  "group flex w-full cursor-pointer flex-col justify-between overflow-hidden rounded-op-lg border-0 bg-op-color-gray-60 transition-all dark:bg-[var(--op-color-gray-990)]"

/** Secondary image well — same fill as secondary card body `#202020`. */
export const SHOP_PRODUCT_CARD_SECONDARY_IMAGE_CLASS =
  "relative flex h-[232px] w-full shrink-0 items-center justify-center overflow-hidden bg-op-color-gray-60 dark:bg-[var(--op-color-gray-990)]"

/**
 * Product detail config cards (Prepared for / quantity / order summary) —
 * `#f5f5f5` / `#202020`; radius 6px.
 */
export const SHOP_PRODUCT_DETAIL_CARD_CLASS =
  "rounded-op-lg border border-op-border-default bg-op-color-gray-60 dark:bg-[var(--op-color-gray-990)]"

/** Spec / info row divider — Figma specs list, not a card chrome. */
export const SHOP_PRODUCT_SPEC_DIVIDER_CLASS = "h-px w-full bg-op-border-default"

/**
 * Checkout — Figma `4450:55084`.
 * Review material panel `#171717`, no border, radius 6px.
 */
export const SHOP_CHECKOUT_REVIEW_CARD_CLASS =
  "flex w-full flex-col gap-6 rounded-op-lg border-0 bg-op-color-gray-60 p-5 dark:bg-[var(--op-color-gray-1000)]"

/**
 * Checkout section card (Delivery details / Payment) —
 * `#171717` + border `#262626`, radius 6px.
 */
export const SHOP_CHECKOUT_SECTION_CARD_CLASS =
  "flex w-full flex-col gap-5 rounded-op-lg border border-op-border-default bg-op-color-gray-60 p-5 dark:bg-[var(--op-color-gray-1000)]"

/** Delivery icon well — circular `#202020` + `#262626`. */
export const SHOP_CHECKOUT_ICON_WELL_CLASS =
  "flex size-[52px] shrink-0 items-center justify-center rounded-full border border-op-border-default bg-op-color-gray-60 p-3 text-op-text-primary dark:bg-[var(--op-color-gray-990)]"

/**
 * Unselected address / delivery method row —
 * `#202020` + border `#262626`, radius 4px.
 */
export const SHOP_CHECKOUT_OPTION_DEFAULT_CLASS =
  "flex w-full items-center justify-between rounded-[4px] border border-op-border-default bg-op-color-gray-60 px-[18px] py-4 transition-colors dark:bg-[var(--op-color-gray-990)]"

/**
 * Selected address / delivery method row —
 * `#171717` + border `#7c7c7c`, radius 4px.
 */
export const SHOP_CHECKOUT_OPTION_SELECTED_CLASS =
  "flex w-full items-center justify-between rounded-[4px] border border-[var(--op-color-gray-550)] bg-op-color-gray-60 px-[18px] py-4 transition-colors dark:bg-[var(--op-color-gray-1000)]"

/** Checkout text fields — same Operator field chrome as Locations / Capture. */
export const SHOP_CHECKOUT_INPUT_CLASS =
  "h-[50px] rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none placeholder:text-op-input-placeholder md:text-sm dark:bg-transparent"

/** Checkout textarea — same tokens as input, taller. */
export const SHOP_CHECKOUT_TEXTAREA_CLASS =
  "min-h-[88px] rounded border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary shadow-none placeholder:text-op-input-placeholder md:text-sm dark:bg-transparent"

/** @deprecated Use {@link SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS}. */
export const SHOP_RECOMMENDATION_CARD_CLASS =
  SHOP_PRODUCT_CARD_RECOMMENDATION_CLASS

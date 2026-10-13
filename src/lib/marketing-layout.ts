import { cn } from "@/lib/utils";

/** Page chrome / gutters behind marketing header and footer (Figma Home). */
export const marketingChromeBackground = "bg-[#CBCBCB]";

/**
 * Cap the marketing shell at 1080p (1920). Smaller viewports stay full-bleed
 * with gutters; only wider viewports centre at this fixed width.
 */
export const marketingShellMaxWidthClass = "mx-auto w-full max-w-[1920px]";

/** Outer padding around the sticky header (Figma Header `5700:25213`: 30 / 20). */
export const marketingHeaderChromePadding = "px-[30px] py-5";

/**
 * Sticky / scroll-margin offset under the marketing header
 * (chrome `py-5` + nav row ≈ 86px — Figma Header `5700:25213`).
 */
export const marketingHeaderStickyTopClass = "top-[86px]";
export const marketingHeaderScrollMarginClass = "scroll-mt-[86px]";

/**
 * Horizontal gutters for marketing page body (Home, legal, Help Centre, etc.).
 * Matches the header chrome side inset (Figma Header: 30px).
 */
export const marketingBodyChromePadding = "px-[30px]";

/**
 * Inner L/R padding for marketing page heroes and footer links (Figma: 60px).
 */
export const marketingChromeContentInset = "px-[60px]";

/** 25px horizontal inset below `lg`; desktop padding unchanged at `lg+`. */
export const marketingSectionInset = "px-[25px] lg:px-16 xl:px-45";

export const marketingSectionInsetLeft =
  "pl-[25px] lg:pl-16 xl:pl-45";

export const marketingSectionInsetRight =
  "pr-[25px] lg:pr-16 xl:pr-45";

/** Shared vertical section padding for marketing homepage blocks. */
export const marketingSectionPadding = "py-12 sm:py-16 lg:py-22.5";

/** Section H2: *headline* Jakarta; 34px below `lg`; fluid scale at `lg+`. Figma line-height 1. */
export const marketingSectionHeading =
  "font-jakarta text-[34px] font-bold leading-none text-[#232323] lg:text-[clamp(1.75rem,4vw,2.625rem)]";

/** Section body: *functional* Helvetica Neue; 16px / 22px below `lg`; desktop scale at `lg+`. */
export const marketingSectionBody =
  "font-sans text-base font-medium leading-[22px] text-[#232323] lg:text-[17px] lg:leading-6.5 lg:text-lg";

/** Hero H1 — *headline* Plus Jakarta Sans. Figma line-height 1. */
export const marketingHeroHeading =
  "font-jakarta text-[36px] font-bold leading-none lg:text-[46px]";

/** Hero body — *functional* Helvetica Neue. */
export const marketingHeroBody =
  "font-sans text-base font-medium leading-[22px] lg:text-[18px] lg:leading-[24px]";

export function marketingSectionShell(...extra: Array<string | undefined>) {
  return cn(
    "mx-auto flex w-full flex-col gap-12 sm:gap-14 lg:gap-15",
    marketingSectionInset,
    marketingSectionPadding,
    ...extra,
  );
}

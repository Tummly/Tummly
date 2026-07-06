import { marketingSectionInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

/**
 * Figma hub column at 1728px: 180px inset, 920px content centred in the remaining
 * 1368px (x=404). Padding and max-width must be on separate elements so border-box
 * does not shrink the 920px column.
 */
export const HELP_CENTRE_HUB_CONTENT_WIDTH = "max-w-[920px]"

/** Figma article detail column width (node 2404:6187). */
export const HELP_CENTRE_ARTICLE_WIDTH = "max-w-[750px]"

/** Figma form width */
export const HELP_CENTRE_FORM_WIDTH = "max-w-[560px]"

/** Outer hub section — horizontal inset + vertical padding only. */
export const helpCentreHubSectionShell = cn(
  "w-full",
  marketingSectionInset
)

/** Inner hub column — 920px max, centred inside the inset shell. */
export const helpCentreHubSectionInner = cn(
  "mx-auto w-full",
  HELP_CENTRE_HUB_CONTENT_WIDTH
)

/**
 * Figma article page at 1728px: 180px inset, 750px column left-aligned in the
 * padded area (x=180). Padding and max-width must be on separate elements.
 */
export const helpCentreArticleSectionShell = cn(
  "w-full",
  marketingSectionInset
)

/** Inner article column — 750px max, left-aligned inside the inset shell. */
export const helpCentreArticleSectionInner = cn(
  "w-full",
  HELP_CENTRE_ARTICLE_WIDTH
)

/** Related-articles band — full width inside horizontal inset (no 750px cap). */
export const helpCentreRelatedSectionInner = "w-full"

export const helpCentreSectionPadding = "py-[70px] lg:py-[110px]"

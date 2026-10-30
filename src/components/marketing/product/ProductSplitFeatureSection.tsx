import type { LucideIcon } from "lucide-react"
import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PRODUCT_BAND_HEADING,
  PRODUCT_SECTION_BODY,
  PRODUCT_SECTION_HEADING,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
  type ProductFeaturePoint,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

/** Figma-exported point glyph (prefer over Lucide when the mark is custom). */
export type FeaturePointIconImg = {
  src: string
  width: number
  height: number
}

function FeaturePoint({
  point,
  Icon,
  iconImg,
  bodyClassName,
}: {
  point: ProductFeaturePoint
  Icon?: LucideIcon
  iconImg?: FeaturePointIconImg
  bodyClassName?: string
}) {
  const copy = (
    <>
      <h3 className="m-0 font-jakarta text-base font-bold leading-[normal]">
        {point.title}
      </h3>
      <p
        className={cn(
          "m-0 max-w-[641px] font-sans text-sm font-normal leading-5",
          bodyClassName,
        )}
      >
        {point.body}
      </p>
    </>
  )

  if (iconImg != null) {
    return (
      <div className="flex items-baseline gap-3 text-[#141414]">
        <img
          src={iconImg.src}
          alt=""
          width={iconImg.width}
          height={iconImg.height}
          className="shrink-0"
          aria-hidden
        />
        <div className="flex min-w-0 flex-col gap-1.5">{copy}</div>
      </div>
    )
  }

  if (Icon == null) {
    return <div className="flex flex-col gap-2 text-[#141414]">{copy}</div>
  }

  return (
    <div className="flex items-start gap-3 text-[#141414]">
      <Icon
        className="mt-0.5 size-[18px] shrink-0 text-[#141414]"
        strokeWidth={1.75}
        aria-hidden
      />
      <div className="flex min-w-0 flex-col gap-1.5">{copy}</div>
    </div>
  )
}

export type ProductSplitFeatureSectionProps = {
  headingId: string
  eyebrow?: string
  title: string
  body: string
  /** Feature list under the intro. Omit or pass [] when the band has none. */
  points?: readonly ProductFeaturePoint[]
  /** Optional Lucide icon per point id. */
  pointIcons?: Partial<Record<string, LucideIcon>>
  /** Optional Figma SVG per point id (wins over `pointIcons` when both set). */
  pointIconImgs?: Partial<Record<string, FeaturePointIconImg>>
  imageSrc: string
  imageAlt: string
  imageWidth: number
  imageHeight: number
  /** Optional srcset when Blob publishes 1x + 2x widths. */
  imageSrcSet?: string
  /**
   * When true, media sits on the left on desktop (Campaigns).
   * Mobile still shows copy first.
   */
  mediaFirst?: boolean
  /**
   * For Groups bands (Figma 720×650 media, copy 640px, gap 120px): keep a
   * fixed column gap — do not stretch with justify-between.
   */
  balanceToMedia?: boolean
  /** Optional secondary CTA under the feature list. */
  cta?: {
    label: string
    to: string
  }
  /** Optional section background — default white. */
  className?: string
  /** Merged onto each point body (e.g. For Groups 14/18 leading). */
  pointBodyClassName?: string
  /** Merged onto the intro body (e.g. a narrower max width). */
  bodyClassName?: string
  /**
   * For Groups only: at `xl` the copy column is a fixed 560px high and spaces
   * intro, list and button with `justify-between` (Figma bands 04, 06, 07, 08).
   * Without it the column keeps its natural height with a 116px intro gap (band 05).
   */
  fixedCopyHeight?: boolean
  /** For Groups only: gap between the list and the CTA button. Default 54px. */
  ctaGapClassName?: string
  /** Merged onto the padded inner row (e.g. a different vertical pad). */
  contentClassName?: string
}

/**
 * Shared Product page layout: copy + feature list and media plate.
 * Default: copy left, media right. `mediaFirst` swaps columns on desktop.
 */
export function ProductSplitFeatureSection({
  headingId,
  eyebrow,
  title,
  body,
  points = [],
  pointIcons,
  pointIconImgs,
  imageSrc,
  imageAlt,
  imageWidth,
  imageHeight,
  imageSrcSet,
  mediaFirst = false,
  balanceToMedia = false,
  cta,
  className,
  pointBodyClassName,
  bodyClassName,
  fixedCopyHeight = false,
  ctaGapClassName,
  contentClassName,
}: ProductSplitFeatureSectionProps) {
  const hasPoints = points.length > 0
  const hasFooter = hasPoints || cta != null

  const copyBlock = (
    <div
      className={cn(
        "flex w-full flex-col gap-10",
        balanceToMedia
          ? // For Groups: 640px copy at Figma width (49.5% below it). At xl either a fixed
            // 560px column (space-between) or the natural height with a 116px intro gap.
            cn(
              "max-w-[640px] lg:w-[49.5%] lg:shrink-0 lg:gap-14",
              fixedCopyHeight
                ? "xl:h-[560px] xl:justify-between xl:gap-0"
                : "xl:gap-[116px]",
            )
          : "max-w-[712px] lg:min-w-0 lg:flex-1 lg:self-stretch lg:justify-between lg:gap-[72px] xl:gap-[116px]",
        mediaFirst ? "order-1 lg:order-2" : null,
      )}
    >
      <header
        className={cn(
          "flex w-full flex-col",
          balanceToMedia
            ? "max-w-[640px] gap-[22px]"
            : "max-w-[692px] gap-[18px]",
        )}
      >
        {balanceToMedia ? (
          <div className="flex w-full flex-col gap-3.5">
            {eyebrow != null && eyebrow !== "" ? (
              <p className="m-0 font-sans text-sm font-medium uppercase leading-[normal] tracking-[1.12px] text-black">
                {eyebrow}
              </p>
            ) : null}
            <h2
              id={headingId}
              className={cn(
                "m-0 max-w-[567px] text-balance xl:text-wrap xl:whitespace-pre-line",
                PRODUCT_BAND_HEADING,
                "lg:leading-[45px]",
              )}
            >
              {title}
            </h2>
          </div>
        ) : (
          <>
            {eyebrow != null && eyebrow !== "" ? (
              <p className="m-0 font-sans text-sm font-medium uppercase leading-[normal] tracking-[1.12px] text-black">
                {eyebrow}
              </p>
            ) : null}
            <h2
              id={headingId}
              className={cn(
                "m-0 max-w-[710px] whitespace-pre-line",
                PRODUCT_SECTION_HEADING,
              )}
            >
              {title}
            </h2>
          </>
        )}
        <p
          className={cn(
            "m-0",
            balanceToMedia ? "max-w-[627px]" : "max-w-[692px]",
            PRODUCT_SECTION_BODY,
            bodyClassName,
          )}
        >
          {body}
        </p>
      </header>

          {hasFooter ? (
        <div
          className={cn(
            "flex w-full flex-col",
            balanceToMedia
              ? cta != null
                ? (ctaGapClassName ?? "gap-[54px]")
                : "gap-4"
              : "gap-[26px]",
          )}
        >
          {hasPoints ? (
            <ul className="m-0 flex w-full list-none flex-col gap-4 p-0">
              {points.map((point, index) => (
                <li key={point.id} className="flex flex-col gap-4">
                  <FeaturePoint
                    point={point}
                    Icon={pointIcons?.[point.id]}
                    iconImg={pointIconImgs?.[point.id]}
                    bodyClassName={pointBodyClassName}
                  />
                  {index < points.length - 1 ? (
                    <hr className="m-0 w-full border-[#e0e0e0]" />
                  ) : null}
                </li>
              ))}
            </ul>
          ) : null}

          {cta != null ? (
            <MarketingButton asChild variant="secondary" className="w-fit">
              <Link to={cta.to}>{cta.label}</Link>
            </MarketingButton>
          ) : null}
        </div>
      ) : null}
    </div>
  )

  const mediaBlock = (
    <div
      className={cn(
        "w-full shrink-0 overflow-hidden rounded-[12px]",
        balanceToMedia
          ? "lg:aspect-[720/650] lg:w-auto lg:min-w-0 lg:max-w-[720px] lg:flex-1 lg:shrink lg:self-center"
          : // Desktop: stretch to the copy column height so media matches text.
            "lg:flex lg:w-[min(100%,766px)] lg:max-w-[766px] lg:self-stretch",
        mediaFirst ? "order-2 lg:order-1" : null,
      )}
    >
      <img
        src={imageSrc}
        srcSet={imageSrcSet}
        sizes={
          balanceToMedia
            ? "(min-width: 1024px) 720px, 100vw"
            : "(min-width: 1024px) 766px, 100vw"
        }
        alt={imageAlt}
        width={imageWidth}
        height={imageHeight}
        className={cn(
          "block h-auto w-full",
          balanceToMedia
            ? "lg:size-full lg:object-cover lg:object-center"
            : "lg:h-full lg:min-h-full lg:w-full lg:object-cover lg:object-center",
        )}
        decoding="async"
      />
    </div>
  )

  return (
    <section
      className={cn("w-full bg-white", className)}
      aria-labelledby={headingId}
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row",
          balanceToMedia
            ? // Figma For Groups: 720 media + 120 gap + 640 copy at 1668px; both columns
              // and the gap scale down below that. Copy-left bands pack to the right edge.
              cn(
                "lg:items-center lg:gap-[clamp(32px,3.5vw,48px)] xl:gap-[clamp(48px,6.95vw,120px)]",
                mediaFirst ? null : "lg:justify-end",
              )
            : "lg:items-stretch lg:justify-between lg:gap-[70px]",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
          contentClassName,
        )}
      >
        {mediaFirst ? (
          <>
            {mediaBlock}
            {copyBlock}
          </>
        ) : (
          <>
            {copyBlock}
            {mediaBlock}
          </>
        )}
      </div>
    </section>
  )
}

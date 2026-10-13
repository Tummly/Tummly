import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PRODUCT_SECTION_BODY,
  PRODUCT_SECTION_HEADING,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
  type ProductFeaturePoint,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

function FeaturePoint({ point }: { point: ProductFeaturePoint }) {
  return (
    <div className="flex flex-col gap-2 text-[#141414]">
      <h3 className="m-0 font-jakarta text-base font-bold leading-normal">
        {point.title}
      </h3>
      <p className="m-0 max-w-[641px] font-sans text-sm font-normal leading-5">
        {point.body}
      </p>
    </div>
  )
}

export type ProductSplitFeatureSectionProps = {
  headingId: string
  eyebrow?: string
  title: string
  body: string
  points: readonly ProductFeaturePoint[]
  imageSrc: string
  imageAlt: string
  imageWidth: number
  imageHeight: number
  /**
   * When true, media sits on the left on desktop (Campaigns).
   * Mobile still shows copy first.
   */
  mediaFirst?: boolean
  /** Optional secondary CTA under the feature list. */
  cta?: {
    label: string
    to: string
  }
  /** Optional section background — default white. */
  className?: string
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
  points,
  imageSrc,
  imageAlt,
  imageWidth,
  imageHeight,
  mediaFirst = false,
  cta,
  className,
}: ProductSplitFeatureSectionProps) {
  const copyBlock = (
    <div
      className={cn(
        "flex w-full max-w-[712px] flex-col gap-10 lg:min-w-0 lg:flex-1 lg:self-stretch lg:justify-between lg:gap-[72px] xl:gap-[116px]",
        mediaFirst ? "order-1 lg:order-2" : null,
      )}
    >
      <header className="flex w-full max-w-[692px] flex-col gap-[18px]">
        {eyebrow != null && eyebrow !== "" ? (
          <p className="m-0 font-sans text-base font-medium uppercase leading-normal text-black">
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
        <p className={cn("m-0 max-w-[692px]", PRODUCT_SECTION_BODY)}>
          {body}
        </p>
      </header>

      <div className="flex w-full flex-col gap-[26px]">
        <ul className="m-0 flex w-full list-none flex-col gap-4 p-0">
          {points.map((point, index) => (
            <li key={point.id} className="flex flex-col gap-4">
              <FeaturePoint point={point} />
              {index < points.length - 1 ? (
                <hr className="m-0 w-full border-[#e0e0e0]" />
              ) : null}
            </li>
          ))}
        </ul>

        {cta != null ? (
          <MarketingButton asChild variant="secondary" className="w-fit">
            <Link to={cta.to}>{cta.label}</Link>
          </MarketingButton>
        ) : null}
      </div>
    </div>
  )

  const mediaBlock = (
    <div
      className={cn(
        "w-full shrink-0 overflow-hidden rounded-[12px] lg:w-[min(100%,766px)] lg:max-w-[766px]",
        mediaFirst ? "order-2 lg:order-1" : null,
      )}
    >
      <img
        src={imageSrc}
        alt={imageAlt}
        width={imageWidth}
        height={imageHeight}
        className="block h-auto w-full"
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
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-start lg:justify-between lg:gap-[70px]",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
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

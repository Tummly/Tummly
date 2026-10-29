import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PRODUCT_BAND_BODY,
  PRODUCT_BAND_HEADING,
  PRODUCT_EYEBROW,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
  type ProductFeaturePoint,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

export type ProductFeatureBandMedia = {
  src: string
  srcSet?: string
  alt: string
  width: number
  height: number
  /**
   * Figma crop of the 758×650 plate (`left` / `top` / `width` / `height` in %).
   * Omit to fill the plate with `object-cover`.
   */
  imageClassName?: string
  /** Plate surface behind the image. Default `bg-[#e9e9e9]`. */
  plateClassName?: string
}

export type ProductFeatureBandProps = {
  headingId: string
  eyebrow: string
  title: string
  body: string
  points: readonly ProductFeaturePoint[]
  /** Point glyph (24px Figma export) per point `id`. */
  pointIcons: Readonly<Record<string, string>>
  media: ProductFeatureBandMedia
  /** Media on the left on desktop. Copy stays first on mobile. */
  mediaFirst?: boolean
  cta?: { label: string; to: string }
}

/**
 * Product page split band — Figma `5661:22688` … `5661:23627`.
 * 640px copy column (intro top, point list bottom, 580px tall) and a 758×650
 * media plate, 120px apart on desktop.
 */
export function ProductFeatureBand({
  headingId,
  eyebrow,
  title,
  body,
  points,
  pointIcons,
  media,
  mediaFirst = false,
  cta,
}: ProductFeatureBandProps) {
  return (
    <section className="w-full bg-white" aria-labelledby={headingId}>
      <div
        className={cn(
          "flex w-full flex-col items-start gap-10 lg:flex-row lg:items-center lg:gap-12 xl:gap-[120px]",
          mediaFirst ? "lg:justify-start" : "lg:justify-end",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <div
          className={cn(
            "flex w-full flex-col gap-10 lg:min-h-[580px] lg:min-w-0 lg:max-w-[640px] lg:flex-1 lg:justify-between",
            mediaFirst ? "order-1 lg:order-2" : null,
          )}
        >
          <header className="flex w-full flex-col gap-[22px]">
            <div className="flex w-full flex-col gap-3.5">
              <p className={PRODUCT_EYEBROW}>{eyebrow}</p>
              <h2
                id={headingId}
                className={cn(PRODUCT_BAND_HEADING, "max-w-[470px]")}
              >
                {title}
              </h2>
            </div>
            <p className={cn(PRODUCT_BAND_BODY, "max-w-[610px]")}>{body}</p>
          </header>

          <div className="flex w-full flex-col gap-9">
            <ul className="m-0 flex w-full list-none flex-col gap-4 p-0">
              {points.map((point, index) => (
                <li key={point.id} className="flex flex-col gap-4">
                  <div className="flex items-start gap-3 text-[#141414]">
                    <img
                      src={pointIcons[point.id]}
                      alt=""
                      width={24}
                      height={24}
                      className="size-6 shrink-0"
                      aria-hidden
                    />
                    <div className="flex min-w-0 flex-col gap-2">
                      <h3 className="m-0 font-jakarta text-base font-bold leading-[normal]">
                        {point.title}
                      </h3>
                      <p className="m-0 font-sans text-sm font-normal leading-5">
                        {point.body}
                      </p>
                    </div>
                  </div>
                  {index < points.length - 1 ? (
                    <hr className="m-0 w-full border-[#c6c6c6]" />
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

        <div
          className={cn(
            "relative aspect-[758/650] w-full shrink-0 overflow-hidden rounded-[12px] lg:w-auto lg:max-w-[758px] lg:flex-1",
            media.plateClassName ?? "bg-[#e9e9e9]",
            mediaFirst ? "order-2 lg:order-1" : null,
          )}
        >
          <img
            src={media.src}
            srcSet={media.srcSet}
            sizes="(min-width: 1024px) 758px, 100vw"
            alt={media.alt}
            width={media.width}
            height={media.height}
            className={cn(
              "absolute max-w-none",
              media.imageClassName ?? "inset-0 size-full object-cover",
            )}
            decoding="async"
            loading="lazy"
          />
        </div>
      </div>
    </section>
  )
}

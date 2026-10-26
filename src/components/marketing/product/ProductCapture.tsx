import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { ProductCaptureMaterialsPanel } from "@/components/marketing/product/ProductCaptureMaterialsPanel"
import {
  PRODUCT_CAPTURE,
  PRODUCT_SECTION_BODY,
  PRODUCT_SECTION_HEADING,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

/** Figma Product Capture section `5661:22668` — sticky QR materials panel. */
export function ProductCapture() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="product-capture-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col gap-[46px] lg:gap-[70px]",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <header className="flex max-w-[548px] flex-col gap-[18px]">
            <p className="m-0 font-sans text-base font-medium uppercase leading-[normal] text-black">
              {PRODUCT_CAPTURE.eyebrow}
            </p>
            <h2
              id="product-capture-heading"
              className={cn("m-0", PRODUCT_SECTION_HEADING)}
            >
              {PRODUCT_CAPTURE.title}
            </h2>
            <p className={cn("m-0 max-w-[467px]", PRODUCT_SECTION_BODY)}>
              {PRODUCT_CAPTURE.body}
            </p>
          </header>
          <MarketingButton asChild variant="secondary" className="w-fit shrink-0">
            <Link to={PRODUCT_CAPTURE.ctaTo}>{PRODUCT_CAPTURE.ctaLabel}</Link>
          </MarketingButton>
        </div>

        <ProductCaptureMaterialsPanel />
      </div>
    </section>
  )
}

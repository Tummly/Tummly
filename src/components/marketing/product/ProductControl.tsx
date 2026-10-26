import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  PRODUCT_CONTROL,
  PRODUCT_SECTION_BODY,
  PRODUCT_SECTION_HEADING,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
  type ProductFeaturePoint,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

function ControlPoint({ point }: { point: ProductFeaturePoint }) {
  return (
    <div className="flex w-full flex-col gap-2 text-[#141414]">
      <h3 className="m-0 font-jakarta text-lg font-bold leading-[normal]">
        {point.title}
      </h3>
      <p className="m-0 font-sans text-sm font-normal leading-5">{point.body}</p>
    </div>
  )
}

/** Figma Product “Built around restaurant control” `5661:24177`. */
export function ProductControl() {
  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="product-control-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px] lg:gap-[66px]",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[692px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-[normal] text-black">
            {PRODUCT_CONTROL.eyebrow}
          </p>
          <h2
            id="product-control-heading"
            className={cn("m-0 max-w-[676px]", PRODUCT_SECTION_HEADING)}
          >
            {PRODUCT_CONTROL.title}
          </h2>
          <p className={cn("m-0 max-w-[692px]", PRODUCT_SECTION_BODY)}>
            {PRODUCT_CONTROL.body}
          </p>
        </header>

        <div className="flex w-full flex-col gap-[26px]">
          <div className="grid w-full grid-cols-1 gap-[26px] lg:grid-cols-2">
            {PRODUCT_CONTROL.columns.map((column) => (
              <div
                key={column[0]?.id ?? column.map((p) => p.id).join("-")}
                className="flex min-w-0 flex-col gap-4"
              >
                {column.map((point, pointIndex) => (
                  <div key={point.id} className="flex flex-col gap-4">
                    <ControlPoint point={point} />
                    {pointIndex < column.length - 1 ? (
                      <hr className="m-0 w-full border-[#e0e0e0]" />
                    ) : null}
                  </div>
                ))}
              </div>
            ))}
          </div>

          <MarketingButton asChild variant="secondary" className="w-fit">
            <Link to={PRODUCT_CONTROL.ctaTo}>{PRODUCT_CONTROL.ctaLabel}</Link>
          </MarketingButton>
        </div>
      </div>
    </section>
  )
}

import { Link } from "react-router-dom"
import { ArrowRightIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { PRICING_QR_MATERIALS, PRICING_SECTION_INSET } from "@/content/marketing/pricingPage"
import {
  marketingHeroBody,
  marketingSectionHeading,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const ctaButtonClass =
  "h-auto min-h-11 gap-1.5 rounded-[4px] border border-[#4e4e4e] bg-transparent px-[19px] py-[13px] text-sm font-medium text-[#141414] shadow-none hover:bg-[#141414]/5"

/** Figma Physical QR materials (`5145:8715`). */
export function PricingQrMaterials() {
  return (
    <section className="w-full bg-white">
      <div
        className={cn(
          "mx-auto flex w-full max-w-[1668px] flex-col gap-[46px] py-[70px]",
          PRICING_SECTION_INSET,
        )}
      >
        <div className="flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
          <div className="flex max-w-[578px] flex-col gap-[18px]">
            <h2
              className={cn(
                "m-0 font-medium text-black",
                marketingSectionHeading,
                "lg:text-[46px]",
              )}
            >
              {PRICING_QR_MATERIALS.title}
            </h2>
            {PRICING_QR_MATERIALS.paragraphs.map((paragraph) => (
              <p
                key={paragraph}
                className={cn(
                  "m-0 font-normal text-[#141414]",
                  marketingHeroBody,
                )}
              >
                {paragraph}
              </p>
            ))}
          </div>
          <Button asChild className={cn(ctaButtonClass, "w-fit")}>
            <Link to={PRICING_QR_MATERIALS.ctaTo}>
              {PRICING_QR_MATERIALS.ctaLabel}
              <ArrowRightIcon className="size-3.5" aria-hidden />
            </Link>
          </Button>
        </div>

        <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
          {PRICING_QR_MATERIALS.products.map((product) => (
            <article
              key={product.id}
              className="overflow-hidden rounded-[12px] bg-[#f0f0f0]"
            >
              <img
                src={product.image}
                alt={product.imageAlt}
                width={509}
                height={578}
                className="block h-auto w-full object-cover"
              />
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

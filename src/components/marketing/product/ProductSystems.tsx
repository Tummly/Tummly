import {
  PRODUCT_SECTION_BODY,
  PRODUCT_SECTION_HEADING,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
  PRODUCT_SYSTEMS,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

/** Figma Product “Built to work with your restaurant” `5661:24151`. */
export function ProductSystems() {
  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="product-systems-heading"
    >
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px] lg:gap-[66px]",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[747px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-normal text-black">
            {PRODUCT_SYSTEMS.eyebrow}
          </p>
          <h2
            id="product-systems-heading"
            className={cn("m-0 max-w-[597px]", PRODUCT_SECTION_HEADING)}
          >
            {PRODUCT_SYSTEMS.title}
          </h2>
          <p className={cn("m-0 max-w-[747px]", PRODUCT_SECTION_BODY)}>
            {PRODUCT_SYSTEMS.body}
          </p>
        </header>

        <div className="grid w-full grid-cols-1 gap-5 md:grid-cols-3 md:gap-10">
          {PRODUCT_SYSTEMS.cards.map((card) => (
            <article
              key={card.id}
              className="flex min-w-0 flex-col gap-4 bg-white p-[26px]"
            >
              <h3 className="m-0 font-jakarta text-base font-bold leading-6 text-[#141414]">
                {card.title}
              </h3>
              <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
                {card.body}
              </p>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

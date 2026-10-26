import {
  PRODUCT_GUEST_LOOP,
  PRODUCT_SECTION_BODY,
  PRODUCT_SECTION_HEADING,
  PRODUCT_SECTION_INSET,
  PRODUCT_SECTION_Y,
  type ProductGuestLoopStep,
} from "@/content/marketing/productPage"
import { cn } from "@/lib/utils"

function GuestLoopStepCard({ step }: { step: ProductGuestLoopStep }) {
  return (
    <article className="flex min-w-0 flex-1 flex-col gap-4 bg-white p-[26px]">
      <h3 className="m-0 font-jakarta text-base font-bold leading-[normal] text-[#141414]">
        {step.title}
      </h3>
      <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
        {step.body}
      </p>
    </article>
  )
}

/** Figma Product Guest Loop overview `5661:22650`. */
export function ProductGuestLoop() {
  return (
    <section className="w-full bg-[#f0f0f0]" aria-labelledby="product-guest-loop-heading">
      <div
        className={cn(
          "flex w-full flex-col items-start gap-[46px] lg:gap-[70px]",
          PRODUCT_SECTION_INSET,
          PRODUCT_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[688px] flex-col gap-[18px]">
          <p className="m-0 font-sans text-base font-medium uppercase leading-[normal] text-black">
            {PRODUCT_GUEST_LOOP.eyebrow}
          </p>
          <h2
            id="product-guest-loop-heading"
            className={cn("m-0 max-w-[674px]", PRODUCT_SECTION_HEADING)}
          >
            {PRODUCT_GUEST_LOOP.title}
          </h2>
          <p className={cn("m-0 max-w-[688px]", PRODUCT_SECTION_BODY)}>
            {PRODUCT_GUEST_LOOP.body}
          </p>
        </header>

        {/*
          Desktop: five equal cards in one row (Figma gap 20).
          Below lg: horizontal scroll so the strip stays one row.
        */}
        <div className="-mx-5 w-[calc(100%+2.5rem)] overflow-x-auto px-5 lg:mx-0 lg:w-full lg:overflow-visible lg:px-0">
          <div className="flex min-w-[960px] gap-5 lg:min-w-0">
            {PRODUCT_GUEST_LOOP.steps.map((step) => (
              <GuestLoopStepCard key={step.id} step={step} />
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}

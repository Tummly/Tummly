import captureIllustration from "@/assets/images/marketing/pricing/guest-loop/capture.png"
import engageIllustration from "@/assets/images/marketing/pricing/guest-loop/engage.png"
import listenIllustration from "@/assets/images/marketing/pricing/guest-loop/listen.png"
import understandIllustration from "@/assets/images/marketing/pricing/guest-loop/understand.png"
import {
  PRICING_GUEST_LOOP,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
  type PricingGuestLoopCardId,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

const CARD_IMAGES = {
  capture: captureIllustration,
  listen: listenIllustration,
  engage: engageIllustration,
  understand: understandIllustration,
} as const satisfies Record<PricingGuestLoopCardId, string>

/**
 * Figma Guest Loop cards `5661:14067` — below UK restaurant sectors.
 */
export function PricingGuestLoop() {
  return (
    <section
      aria-labelledby="pricing-guest-loop-heading"
      className="w-full bg-[#f0f0f0]"
    >
      <div
        className={cn(
          "flex w-full flex-col items-center gap-[60px]",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[566px] flex-col items-center gap-[18px] text-center">
          <h2
            id="pricing-guest-loop-heading"
            className="m-0 max-w-[496px] font-jakarta text-[36px] font-[500] leading-[normal] text-black"
          >
            {PRICING_GUEST_LOOP.title}
          </h2>
          {/* Figma `5661:14072`: 16/22 at every width. */}
          <p className="m-0 font-sans text-base leading-[22px] font-normal text-[#141414]">
            {PRICING_GUEST_LOOP.body}
          </p>
        </div>

        <ul className="m-0 grid w-full list-none grid-cols-1 gap-5 p-0 sm:grid-cols-2 xl:grid-cols-4">
          {PRICING_GUEST_LOOP.cards.map((card) => (
            <li key={card.id} className="min-w-0">
              <article className="flex h-full min-h-[356px] flex-col items-center justify-between overflow-hidden rounded-lg bg-[#e2e2e2] px-[26px] py-9">
                <img
                  src={CARD_IMAGES[card.id]}
                  alt={card.imageAlt}
                  width={200}
                  height={200}
                  className="size-[200px] shrink-0 object-contain"
                  decoding="async"
                />
                <div className="flex w-full flex-col items-center gap-2.5 text-center">
                  <h3 className="m-0 font-jakarta text-xl font-semibold leading-6 text-[#141414]">
                    {card.title}
                  </h3>
                  <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
                    {card.body}
                  </p>
                </div>
              </article>
            </li>
          ))}
        </ul>
      </div>
    </section>
  )
}

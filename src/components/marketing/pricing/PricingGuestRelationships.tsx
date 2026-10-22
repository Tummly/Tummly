import guestRelationshipsIcon from "@/assets/svg/marketing/pricing/guest-relationships/guest-relationships.svg"
import privateFeedbackIcon from "@/assets/svg/marketing/pricing/guest-relationships/private-feedback.svg"
import qrGuestLinksIcon from "@/assets/svg/marketing/pricing/guest-relationships/qr-guest-links.svg"
import reportsBriefsIcon from "@/assets/svg/marketing/pricing/guest-relationships/reports-briefs.svg"
import {
  PRICING_GUEST_RELATIONSHIPS,
  PRICING_SECTION_INSET,
  PRICING_SECTION_Y,
  type PricingGuestRelationshipsCardId,
} from "@/content/marketing/pricingPage"
import { cn } from "@/lib/utils"

const CARD_ICONS = {
  "qr-guest-links": qrGuestLinksIcon,
  "private-feedback": privateFeedbackIcon,
  "guest-relationships": guestRelationshipsIcon,
  "reports-briefs": reportsBriefsIcon,
} as const satisfies Record<PricingGuestRelationshipsCardId, string>

/** Figma Built for better guest relationships `6728:13979`. */
export function PricingGuestRelationships() {
  return (
    <section
      aria-labelledby="pricing-guest-relationships-heading"
      className="w-full bg-[#e4ede7]"
    >
      <div
        className={cn(
          "flex w-full flex-col items-center gap-[70px]",
          PRICING_SECTION_INSET,
          PRICING_SECTION_Y,
        )}
      >
        <div className="flex w-full max-w-[592px] flex-col items-center gap-[22px] text-center">
          <h2
            id="pricing-guest-relationships-heading"
            className="m-0 max-w-[579px] font-jakarta text-[36px] font-semibold leading-normal text-black"
          >
            {PRICING_GUEST_RELATIONSHIPS.title}
          </h2>
          <p className="m-0 max-w-[559px] font-sans text-base font-normal leading-5 text-[#141414]">
            {PRICING_GUEST_RELATIONSHIPS.body}
          </p>
        </div>

        <ul className="m-0 grid w-full list-none grid-cols-1 gap-5 p-0 sm:grid-cols-2 xl:grid-cols-4">
          {PRICING_GUEST_RELATIONSHIPS.cards.map((card) => (
            <li key={card.id} className="min-w-0">
              <article className="flex h-full flex-col items-center gap-7 rounded-[12px] bg-[#d3dcd6] p-[26px]">
                <img
                  src={CARD_ICONS[card.id]}
                  alt=""
                  width={51}
                  height={51}
                  className="size-[50.667px] shrink-0"
                  aria-hidden
                />
                <div className="flex w-full flex-col items-center gap-2.5 text-center text-[#141414]">
                  <h3 className="m-0 font-jakarta text-lg font-bold leading-[30px]">
                    {card.title}
                  </h3>
                  <p className="m-0 font-sans text-sm font-normal leading-5">
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

import { Link } from "react-router-dom"

import { forGroupsGuestLoopPosters } from "@/assets/for-groups-images"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLoopVideo } from "@/components/marketing/MarketingLoopVideo"
import {
  FOR_GROUPS_PRODUCT,
  FOR_GROUPS_SECTION_BODY,
  FOR_GROUPS_SECTION_HEADING,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import { forGroupsProductVideoUrl } from "@/lib/marketingMedia"
import { cn } from "@/lib/utils"

/**
 * Figma For Groups Keep the Guest Loop working `6405:36122` / cards `6405:36126`.
 * Centre copy + four equal cards (visual fills the card, copy sits at the
 * bottom) + CTA. The loop videos keep the Figma still as poster (and as the
 * reduced-motion view) and sit `videoShiftPct` higher so the UI card starts 19%
 * from the top, as in Figma.
 */
export function ForGroupsProduct() {
  return (
    <section
      className="w-full bg-[#f2f0ec]"
      aria-labelledby="for-groups-product-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-[46px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <header className="flex w-full flex-col items-center gap-6 text-center text-[#141414]">
          <h2
            id="for-groups-product-heading"
            className={cn("m-0 w-full max-w-[660px]", FOR_GROUPS_SECTION_HEADING)}
          >
            {FOR_GROUPS_PRODUCT.title}
          </h2>
          <p className={cn("m-0 max-w-[616px]", FOR_GROUPS_SECTION_BODY)}>
            {FOR_GROUPS_PRODUCT.body}
          </p>
        </header>

        <ul className="m-0 grid w-full list-none grid-cols-1 gap-[26px] p-0 sm:grid-cols-2 xl:grid-cols-4">
          {FOR_GROUPS_PRODUCT.cards.map((card) => (
            <li key={card.id} className="flex min-w-0">
              <article
                className="relative flex h-[496px] w-full flex-col items-center justify-end overflow-hidden rounded-[12px] pb-10"
                style={{ backgroundColor: FOR_GROUPS_PRODUCT.cardBackground }}
              >
                <MarketingLoopVideo
                  src={forGroupsProductVideoUrl(card.id)}
                  poster={forGroupsGuestLoopPosters[card.id]}
                  className="absolute inset-0 size-full bg-[#e1dfdb]"
                  videoClassName="bg-[#e1dfdb] object-cover object-center"
                  style={{ transform: `translateY(-${card.videoShiftPct}%)` }}
                />
                <div className="relative flex w-full shrink-0 flex-col items-center gap-3.5 px-2.5 text-center text-[#141414]">
                  <h3 className="m-0 font-jakarta text-xl font-bold leading-[normal]">
                    {card.title}
                  </h3>
                  <p
                    className="m-0 font-sans text-sm font-normal leading-5"
                    style={{ maxWidth: card.bodyWidth }}
                  >
                    {card.body}
                  </p>
                </div>
              </article>
            </li>
          ))}
        </ul>

        <MarketingButton
          asChild
          variant="secondary"
          className="w-full sm:w-[210px]"
        >
          <Link to={FOR_GROUPS_PRODUCT.ctaTo}>
            {FOR_GROUPS_PRODUCT.ctaLabel}
          </Link>
        </MarketingButton>
      </div>
    </section>
  )
}

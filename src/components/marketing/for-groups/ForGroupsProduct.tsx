import { Link } from "react-router-dom"

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
 * Figma For Groups Product `6405:36122` / cards `6405:36126`.
 * Centre copy + four equal cards (video above copy) + CTA.
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
        <header className="flex w-full max-w-[638px] flex-col items-center gap-6 text-center text-[#141414]">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className="m-0 font-sans text-sm font-medium leading-4 tracking-[1.12px]">
              {FOR_GROUPS_PRODUCT.eyebrow}
            </p>
            <h2
              id="for-groups-product-heading"
              className={cn("m-0 w-full", FOR_GROUPS_SECTION_HEADING)}
            >
              {FOR_GROUPS_PRODUCT.title}
            </h2>
          </div>
          <p className={cn("m-0 max-w-[616px]", FOR_GROUPS_SECTION_BODY)}>
            {FOR_GROUPS_PRODUCT.body}
          </p>
        </header>

        <ul className="m-0 grid w-full list-none grid-cols-1 gap-[26px] p-0 sm:grid-cols-2 xl:grid-cols-4">
          {FOR_GROUPS_PRODUCT.cards.map((card) => (
            <li key={card.id} className="flex min-w-0">
              <article
                className="flex h-[496px] w-full flex-col items-center overflow-hidden rounded-[12px] pb-10"
                style={{ backgroundColor: FOR_GROUPS_PRODUCT.cardBackground }}
              >
                <MarketingLoopVideo
                  src={forGroupsProductVideoUrl(card.id)}
                  className="min-h-0 w-full flex-1 bg-[#e1dfdb]"
                  videoClassName="bg-[#e1dfdb] object-cover object-center"
                />
                <div className="flex w-full shrink-0 flex-col items-center gap-3.5 px-2.5 text-center text-[#141414]">
                  <h3 className="m-0 font-jakarta text-xl font-bold leading-[normal]">
                    {card.title}
                  </h3>
                  <p className="m-0 max-w-[323px] font-sans text-sm font-normal leading-5">
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

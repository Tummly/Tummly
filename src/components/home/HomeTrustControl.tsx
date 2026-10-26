import { Link } from "react-router-dom"

import badgeCyberEssentials from "@/assets/images/marketing/home/trust-badges/cyber-essentials.png"
import badgeCyberEssentialsPlus from "@/assets/images/marketing/home/trust-badges/cyber-essentials-plus.png"
import badgeDataProtection from "@/assets/images/marketing/home/trust-badges/data-protection.png"
import badgeIcoRegistered from "@/assets/images/marketing/home/trust-badges/ico-registered.png"
import badgeIso27001 from "@/assets/images/marketing/home/trust-badges/iso-27001.png"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion"
import {
  HOME_TRUST_CONTROL,
  type HomeTrustBadge,
} from "@/content/marketing/homePage"
import { publicHomeTrustBadges } from "@/content/marketing/publicTrustClaims"
import { cn } from "@/lib/utils"

const BADGE_IMAGES = {
  "ico-registered": badgeIcoRegistered,
  "data-protection": badgeDataProtection,
  "cyber-essentials": badgeCyberEssentials,
  "cyber-essentials-plus": badgeCyberEssentialsPlus,
  "iso-27001": badgeIso27001,
} as const satisfies Record<HomeTrustBadge["id"], string>

const accordionTriggerClassName = cn(
  "items-start gap-[30px] rounded-none border-0 py-0 hover:no-underline hover:cursor-pointer",
  "focus-visible:border-0 focus-visible:ring-2 focus-visible:ring-[#141414]/40 focus-visible:ring-offset-2",
  "**:data-[slot=accordion-trigger-icon]:ml-0 **:data-[slot=accordion-trigger-icon]:size-4",
  "**:data-[slot=accordion-trigger-icon]:shrink-0 **:data-[slot=accordion-trigger-icon]:text-[#141414]",
)

function TrustBadgeCard({ badge }: { badge: HomeTrustBadge }) {
  return (
    <div className="flex w-full flex-col items-center gap-2.5 rounded-[10px] border border-[#b8b8b8] px-[30px] py-[22px] lg:min-w-[200px] lg:flex-1">
      <img
        src={BADGE_IMAGES[badge.id]}
        alt=""
        width={64}
        height={64}
        className="block size-16 object-contain"
        aria-hidden
        decoding="async"
      />
      <p className="m-0 text-center font-sans text-sm font-medium leading-5 text-black">
        {badge.label}
      </p>
    </div>
  )
}

/**
 * Home Trust & control — desktop Figma `4942:19219`, mobile `6241:18218`.
 * Accordion + compliance badges; CTA to Trust & privacy.
 */
export function HomeTrustControl() {
  const defaultOpen = HOME_TRUST_CONTROL.items[0]?.id
  const badges = publicHomeTrustBadges()

  return (
    <section
      className="w-full bg-[#f0f0f0]"
      aria-labelledby="home-trust-control-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col gap-[60px] py-[70px]",
          // Figma mobile `6241:18218`: 24px. Desktop keep 60px chrome inset.
          "px-6 lg:px-[60px]",
        )}
      >
        <div className="flex w-full flex-col gap-[76px] lg:flex-row lg:items-start lg:justify-between lg:gap-12">
          <div className="flex max-w-[548px] flex-col gap-[46px]">
            <header className="flex flex-col gap-3.5 text-[#141414]">
              <p className="m-0 font-jakarta text-xs font-medium leading-[14px] lg:text-sm lg:leading-4">
                {HOME_TRUST_CONTROL.eyebrow}
              </p>
              <h2
                id="home-trust-control-heading"
                className="m-0 font-jakarta text-[28px] font-medium leading-[38px] lg:text-[36px] lg:leading-[48px]"
              >
                {HOME_TRUST_CONTROL.title}
              </h2>
            </header>
            <MarketingButton variant="secondary" asChild className="w-fit">
              <Link to={HOME_TRUST_CONTROL.ctaTo}>
                {HOME_TRUST_CONTROL.ctaLabel}
              </Link>
            </MarketingButton>
          </div>

          <Accordion
            type="single"
            collapsible
            defaultValue={defaultOpen}
            className="w-full max-w-[792px] shrink-0 gap-[30px] lg:w-[min(100%,792px)]"
          >
            {HOME_TRUST_CONTROL.items.map((item) => (
              <AccordionItem
                key={item.id}
                value={item.id}
                className="border-0 border-b border-[#7c7c7c] pb-5"
              >
                <AccordionTrigger className={accordionTriggerClassName}>
                  <span className="m-0 min-w-0 flex-1 text-left font-jakarta text-xl font-medium leading-[25px] text-[#141414] lg:text-2xl lg:leading-[normal]">
                    {item.title}
                  </span>
                </AccordionTrigger>
                <AccordionContent className="pt-[18px] pb-0">
                  <p className="m-0 font-sans text-sm font-normal leading-[18px] text-[#141414] lg:text-base lg:leading-[normal]">
                    {item.body}
                  </p>
                </AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>
        </div>

        {/* Mobile: full-width vertical stack (Figma `6241:18218`). Desktop: row. */}
        {badges.length > 0 ? (
          <ul className="m-0 flex w-full list-none flex-col gap-4 p-0 lg:flex-row lg:items-stretch">
            {badges.map((badge) => (
              <li key={badge.id} className="flex w-full min-w-0 lg:flex-1">
                <TrustBadgeCard badge={badge} />
              </li>
            ))}
          </ul>
        ) : null}
      </div>
    </section>
  )
}

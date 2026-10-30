import { Link } from "react-router-dom"

import { MarketingAskAiIcons } from "@/components/marketing/MarketingAskAiIcons"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { MarketingNavLink } from "@/components/marketing/MarketingNavLink"
import { TrustIndicatorTile } from "@/components/marketing/trust/TrustIndicatorTile"
import {
  MARKETING_FOOTER_BOTTOM_LINKS,
  MARKETING_FOOTER_COMPANY,
  MARKETING_FOOTER_COMPANY_DISCLOSURE,
  MARKETING_FOOTER_FHH_URL,
  MARKETING_FOOTER_LEGAL,
  MARKETING_FOOTER_PRODUCT,
  MARKETING_FOOTER_RESTAURANTS,
  MARKETING_FOOTER_SOCIAL,
  type MarketingNavItem,
} from "@/constants/marketingNav"
import { visibleTrustIndicators } from "@/content/marketing/publicTrustClaims"
import { usePublicTrustIndicators } from "@/hooks/usePublicTrustIndicators"
import type { PublicTrustIndicator } from "@/lib/marketing/publicTrustIndicators"
import { buildSignupPath } from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

/** Figma link Item is 18px tall (14px text, "normal" line height). */
const footerLinkClass = "text-sm font-normal leading-[18px] text-[#141414]"
const footerHeadingClass =
  "m-0 font-jakarta text-base font-bold leading-5 text-[#141414]"
/** Figma: 16px on mobile, 18px from `md`. */
const askAiHeadingClass = cn(
  footerHeadingClass,
  "md:whitespace-nowrap md:text-lg md:leading-[23px]",
)

/**
 * DOM order is the mobile 2-column pairing (Product|Restaurants,
 * Company|Social, Company & Legal). From `md`, `md:order-*` restores the 3-column rows
 * (Product|Restaurants|Social, Company|Company & Legal|Ask AI).
 */
const FOOTER_LINK_COLUMNS = [
  {
    id: "product",
    title: "Product",
    items: MARKETING_FOOTER_PRODUCT,
    className: "md:order-1",
  },
  {
    id: "restaurants",
    title: "Restaurants",
    items: MARKETING_FOOTER_RESTAURANTS,
    className: "md:order-2",
  },
  {
    id: "company",
    title: "Company",
    items: MARKETING_FOOTER_COMPANY,
    className: "md:order-4",
  },
  {
    id: "social",
    title: "Find us on social",
    items: MARKETING_FOOTER_SOCIAL,
    className: "md:order-3",
  },
  {
    id: "legal",
    title: "Company & Legal",
    items: MARKETING_FOOTER_LEGAL,
    className: "col-span-2 md:order-5 md:col-span-1",
  },
] as const

function FooterLinkColumn({
  title,
  items,
  className,
}: {
  title: string
  items: readonly MarketingNavItem[]
  className?: string
}) {
  return (
    <div className={cn("flex min-w-0 flex-col gap-5 md:w-[178px]", className)}>
      <p className={footerHeadingClass}>{title}</p>
      <nav aria-label={title} className="flex flex-col items-start gap-4.5">
        {items.map((item) => (
          <MarketingNavLink
            key={item.id}
            label={item.label}
            href={item.href}
            className={cn(footerLinkClass, "md:whitespace-nowrap")}
          />
        ))}
      </nav>
    </div>
  )
}

function AskAiBlock({ className }: { className?: string }) {
  return (
    <div className={cn("flex-col gap-5 md:w-[178px]", className)}>
      <p className={askAiHeadingClass}>Ask AI about Tummly</p>
      <MarketingAskAiIcons />
    </div>
  )
}

/**
 * "Built with trust in mind" grid: 5 columns × 3 rows from `sm`, 3 columns from
 * 360px, 2 columns below that. The DOM order is the reading order. The caller
 * renders this only when there are indicators to show.
 */
export function TrustIndicatorGrid({ items }: { items: readonly PublicTrustIndicator[] }) {
  return (
    <div className="flex flex-col gap-7.5">
      <p className={footerHeadingClass}>Built with trust in mind</p>
      <ul
        aria-label="Trust indicators"
        className="m-0 grid max-w-[530px] list-none grid-cols-2 gap-x-3.5 gap-y-2 p-0 min-[360px]:grid-cols-3 sm:grid-cols-5"
      >
        {items.map((indicator) => (
          <li key={indicator.itemNumber} className="flex">
            <TrustIndicatorTile indicator={indicator} variant="footer" />
          </li>
        ))}
      </ul>
    </div>
  )
}

/** The one green Pilot call to action in the footer. */
export function FooterPilotButton() {
  return (
    <MarketingButton asChild variant="primary">
      <Link to={buildSignupPath({ plan: "Pilot" })}>Start 30-day Pilot</Link>
    </MarketingButton>
  )
}

/** Figma marketing Footer: desktop `5839:73487`, mobile `6243:19686`. */
export default function Footer() {
  const trust = usePublicTrustIndicators()
  const trustIndicators = visibleTrustIndicators(trust)

  return (
    <footer className="w-full pt-5">
      <div className="flex w-full flex-col overflow-hidden rounded-tl-[8px] rounded-tr-[8px] px-6 pb-[60px] pt-10 sm:px-10 xl:px-[60px] xl:pb-[70px]">
        <div className="flex w-full flex-col gap-[110px] xl:gap-[70px]">
          {/* Top: brand | link columns, trust indicators under the brand on desktop */}
          <div
            className={cn(
              "flex w-full flex-col gap-[70px]",
              "xl:grid xl:grid-cols-[minmax(0,1fr)_auto] xl:grid-rows-[auto_1fr] xl:items-start xl:gap-x-[70px] xl:gap-y-8",
            )}
          >
            <div className="flex flex-col gap-[50px] xl:col-start-1 xl:row-start-1 xl:gap-9">
              <Link
                to="/"
                className="block h-[26px] w-fit shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30 xl:h-[44px]"
              >
                <MarketingLogo
                  width={236}
                  height={58}
                  className="h-[35px] w-[141px] max-w-none xl:h-[58px] xl:w-[236px]"
                />
              </Link>

              <div className="flex flex-col gap-8 xl:gap-9">
                <p className="m-0 max-w-[431px] font-jakarta text-lg font-semibold leading-[23px] text-[#141414] md:text-xl md:leading-7">
                  Turn everyday orders and visits into guest relationships you
                  can build on.
                </p>

                <div>
                  <FooterPilotButton />
                </div>
              </div>

              <AskAiBlock className="flex md:hidden" />
            </div>

            <div
              className={cn(
                "grid w-full grid-cols-2 gap-y-[70px]",
                "md:w-fit md:grid-cols-[repeat(3,178px)] md:gap-x-10 xl:col-start-2 xl:row-span-2 xl:row-start-1 2xl:gap-x-[100px] 2xl:pr-[30px]",
              )}
            >
              {FOOTER_LINK_COLUMNS.map((column) => (
                <FooterLinkColumn
                  key={column.id}
                  title={column.title}
                  items={column.items}
                  className={column.className}
                />
              ))}

              <AskAiBlock className="hidden md:order-6 md:flex" />
            </div>

            {trustIndicators.length > 0 ? (
              <div className="flex flex-col gap-8 xl:col-start-1 xl:row-start-2">
                <div
                  className="hidden h-px w-full max-w-[525px] bg-[#aaaaaa] xl:block"
                  aria-hidden
                />
                <TrustIndicatorGrid items={trustIndicators} />
              </div>
            ) : null}
          </div>

          {/* Bottom: FHH · rule · copyright + disclosure | legal links */}
          <div className="flex w-full flex-col gap-7">
            <p className="m-0 w-full text-center font-jakarta text-xs font-medium leading-[22px] text-black md:text-sm">
              Proud to be a company of{" "}
              <a
                href={MARKETING_FOOTER_FHH_URL}
                target="_blank"
                rel="noopener noreferrer"
                className="underline underline-offset-2"
              >
                FHH.group
              </a>
            </p>

            <div className="h-px w-full bg-[#969696]" aria-hidden />

            <div className="flex w-full flex-col items-start gap-[30px] md:flex-row md:items-center md:justify-between md:gap-10">
              <div className="flex flex-col gap-3 text-[#141414]">
                <p className="m-0 text-xs font-normal leading-[18px]">
                  {MARKETING_FOOTER_COMPANY_DISCLOSURE.map((line) => (
                    <span key={line} className="block">
                      {line}
                    </span>
                  ))}
                </p>
                <p className="m-0 text-sm font-normal leading-[normal]">
                  © 2026 Tummly.com Ltd. All rights reserved.
                </p>
              </div>

              <nav
                aria-label="Legal"
                className="flex flex-wrap items-center gap-x-[22px] gap-y-2 md:shrink-0 md:flex-nowrap"
              >
                {MARKETING_FOOTER_BOTTOM_LINKS.map((item) => (
                  <MarketingNavLink
                    key={item.id}
                    label={item.label}
                    href={item.href}
                    className={footerLinkClass}
                  />
                ))}
              </nav>
            </div>
          </div>
        </div>
      </div>
    </footer>
  )
}

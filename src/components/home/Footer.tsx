import { Link } from "react-router-dom"

import { MarketingAskAiIcons } from "@/components/marketing/MarketingAskAiIcons"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { MarketingNavLink } from "@/components/marketing/MarketingNavLink"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  MARKETING_DEMO_PATH,
  MARKETING_FOOTER_BOTTOM_LINKS,
  MARKETING_FOOTER_COMPANY,
  MARKETING_FOOTER_FHH_URL,
  MARKETING_FOOTER_PRODUCT,
  MARKETING_FOOTER_RESTAURANTS,
  MARKETING_FOOTER_SOCIAL,
  MARKETING_FOOTER_TRUST,
  MARKETING_FOOTER_TRUST_CREDENTIALS,
  type MarketingNavItem,
} from "@/constants/marketingNav"
import { cn } from "@/lib/utils"

const footerLinkClass = "text-sm font-normal leading-normal text-[#141414]"
const footerHeadingClass =
  "m-0 font-jakarta text-base font-bold leading-none text-[#141414]"

/** Mobile 2-col pairing: Product|Restaurants, Company|Trust, Social|Built. */
const FOOTER_LINK_COLUMNS = [
  { id: "product", title: "Product", items: MARKETING_FOOTER_PRODUCT },
  {
    id: "restaurants",
    title: "Restaurants",
    items: MARKETING_FOOTER_RESTAURANTS,
  },
  {
    id: "company",
    title: "Company & Legal",
    items: MARKETING_FOOTER_COMPANY,
  },
  { id: "trust", title: "Trust", items: MARKETING_FOOTER_TRUST },
  {
    id: "social",
    title: "Find us on social",
    items: MARKETING_FOOTER_SOCIAL,
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
    <div className={cn("flex min-w-0 flex-col gap-5 lg:w-[178px]", className)}>
      <p className={footerHeadingClass}>{title}</p>
      <nav aria-label={title} className="flex flex-col gap-4.5">
        {items.map((item) => (
          <MarketingNavLink
            key={item.id}
            label={item.label}
            href={item.href}
            className={footerLinkClass}
          />
        ))}
      </nav>
    </div>
  )
}

/** Figma marketing Footer (`5661:14511`). */
export default function Footer() {
  return (
    <footer className="w-full pt-5">
      <div className="flex w-full flex-col overflow-hidden rounded-tl-[8px] rounded-tr-[8px] px-6.25 pb-12.5 pt-10 sm:px-10 lg:px-[70px] lg:pb-20 lg:pt-10">
        <div className="flex w-full flex-col gap-[70px] lg:gap-[110px]">
          {/* Top: brand + Ask AI | link columns + trust credentials */}
          <div className="flex w-full flex-col gap-12 lg:flex-row lg:items-start lg:gap-[70px]">
            <div className="flex w-full flex-col gap-[50px] lg:min-w-0 lg:flex-1 lg:max-w-[669px]">
              <Link
                to="/"
                className="shrink-0 rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30"
              >
                <MarketingLogo
                  width={141}
                  height={34}
                  className="h-[26px] max-w-[min(141px,50vw)]"
                />
              </Link>

              <div className="flex max-w-[496px] flex-col gap-8">
                <div className="flex flex-col gap-3.5 text-black">
                  <p className="m-0 font-jakarta text-xl font-semibold leading-none">
                    Turn guest feedback into repeat visits.
                  </p>
                  <p className="m-0 font-sans text-base font-normal leading-6">
                    Collect private feedback, recover guests, build stronger
                    guest relationships and understand what to do next.
                  </p>
                </div>

                <div className="flex flex-col gap-5 sm:flex-row sm:items-center">
                  <MarketingButton asChild variant="primary">
                    <RequestTrialLink planIntent={{ plan: "Pilot" }}>
                      Start 30-day Pilot
                    </RequestTrialLink>
                  </MarketingButton>
                  <MarketingButton asChild variant="secondary">
                    <Link to={MARKETING_DEMO_PATH}>Try demo</Link>
                  </MarketingButton>
                </div>
              </div>

              <div className="flex flex-col gap-5">
                <p className={footerHeadingClass}>Ask AI about Tummly</p>
                <MarketingAskAiIcons />
              </div>
            </div>

            <div
              className={cn(
                "grid w-full grid-cols-2 gap-x-8 gap-y-10 sm:gap-x-10 sm:gap-y-12",
                "lg:w-auto lg:shrink-0 lg:grid-cols-3 lg:gap-x-[100px] lg:gap-y-[70px] lg:pr-[30px]",
              )}
            >
              {FOOTER_LINK_COLUMNS.map((column) => (
                <FooterLinkColumn
                  key={column.id}
                  title={column.title}
                  items={column.items}
                />
              ))}

              <div className="flex min-w-0 flex-col gap-5 lg:w-[223px]">
                <p className={footerHeadingClass}>Built with trust in mind</p>
                <div className="flex flex-col gap-8">
                  {MARKETING_FOOTER_TRUST_CREDENTIALS.map((credential) => (
                    <div
                      key={credential.id}
                      className="flex flex-col items-start gap-2.5"
                    >
                      <p className="m-0 font-jakarta text-sm font-bold leading-normal text-[#141414]">
                        {credential.title}
                      </p>
                      <MarketingNavLink
                        label={credential.linkLabel}
                        href={credential.href}
                        className={footerLinkClass}
                      />
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Bottom: FHH · rule · copyright + legal */}
          <div className="flex w-full flex-col gap-7">
            <p className="m-0 w-full text-center font-jakarta text-sm font-medium leading-[22px] text-black">
              Proud to be part of{" "}
              <a
                href={MARKETING_FOOTER_FHH_URL}
                target="_blank"
                rel="noopener noreferrer"
                className="underline underline-offset-2"
              >
                FHH.group
              </a>
              .
            </p>

            <div className="h-px w-full bg-[#141414]/20" aria-hidden />

            <div className="flex w-full flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <p className="m-0 text-sm font-normal leading-normal text-[#141414]">
                © 2026 Tummly.com Ltd. All rights reserved.
              </p>

              <nav
                aria-label="Legal"
                className="flex flex-wrap items-center gap-x-[22px] gap-y-2"
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

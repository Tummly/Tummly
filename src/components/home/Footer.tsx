import { Link } from "react-router-dom"

import { CookieSettingsTrigger } from "@/components/common/CookieSettingsDialog"
import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { MarketingNavLink } from "@/components/marketing/MarketingNavLink"
import { MARKETING_FOOTER_COLUMNS } from "@/constants/marketingNav"
import {
  marketingChromeBackground,
  marketingChromeContentInset,
} from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const footerLinkClass = "text-sm text-[#141414]"
const footerHeadingClass =
  "m-0 text-base font-medium leading-normal text-[#141414]"

export default function Footer() {
  return (
    <footer className={cn("w-full", marketingChromeBackground)}>
      <div
        className={cn(
          "flex w-full flex-col gap-10 rounded-tl-[8px] rounded-tr-[8px] pb-10 pt-[60px] lg:gap-0",
          marketingChromeContentInset,
        )}
      >
        <div className="flex flex-col gap-10 lg:flex-row lg:items-stretch lg:justify-between lg:gap-12">
          <div className="flex flex-col justify-between gap-8 lg:min-h-[251px]">
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
            <p className="m-0 text-sm text-[#141414]">
              © 2026 Limited. All rights reserved.
            </p>
          </div>

          <div className="grid grid-cols-2 gap-x-8 gap-y-10 sm:grid-cols-3 lg:flex lg:flex-wrap lg:gap-x-[60px] xl:gap-x-[120px]">
            {MARKETING_FOOTER_COLUMNS.map((column) => (
              <div
                key={column.id}
                className="flex min-w-0 flex-col gap-5"
              >
                <p className={footerHeadingClass}>{column.title}</p>
                <nav
                  aria-label={column.title}
                  className="flex flex-col gap-[18px]"
                >
                  {column.items.map((item) => {
                    if (
                      column.id === "trust"
                      && item.id === "cookies"
                    ) {
                      return (
                        <div
                          key={item.id}
                          className="flex flex-col gap-2"
                        >
                          <MarketingNavLink
                            label={item.label}
                            href={item.href}
                            className={footerLinkClass}
                          />
                          <CookieSettingsTrigger
                            className={cn(
                              footerLinkClass,
                              "rounded-sm text-left no-underline hover:opacity-70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30",
                            )}
                          >
                            Cookie Settings
                          </CookieSettingsTrigger>
                        </div>
                      )
                    }

                    return (
                      <MarketingNavLink
                        key={item.id}
                        label={item.label}
                        href={item.href}
                        className={footerLinkClass}
                      />
                    )
                  })}
                </nav>
              </div>
            ))}
          </div>
        </div>
      </div>
    </footer>
  )
}

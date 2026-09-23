import { Link } from "react-router-dom"

import { MarketingLogo } from "@/components/marketing/MarketingLogo"
import { MarketingNavLink } from "@/components/marketing/MarketingNavLink"
import { MARKETING_FOOTER_COLUMNS } from "@/constants/marketingNav"
import { cn } from "@/lib/utils"

const footerLinkClass = "text-sm font-normal text-[#141414]"
const footerHeadingClass =
  "m-0 text-base font-medium leading-normal text-[#141414]"

/** Figma marketing Footer (`4116:13302` / FAQ `4974:26425`). */
export default function Footer() {
  return (
    <footer className="w-full pt-5">
      <div className="flex w-full flex-col justify-center overflow-hidden rounded-tl-[8px] rounded-tr-[8px] bg-[#f0f0f0] px-6.25 py-10 sm:px-10 lg:min-h-[311px] lg:px-15 lg:py-15">
        <div className="flex w-full flex-col gap-10 lg:flex-row lg:items-stretch lg:gap-[70px]">
          <div className="flex flex-col justify-between gap-8 lg:min-h-[211px] lg:flex-1">
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
            <p className="m-0 text-sm font-normal leading-normal text-[#141414]">
              © 2026 Tummly.com Limited. All rights reserved.
            </p>
          </div>

          <div
            className={cn(
              "grid grid-cols-2 gap-x-8 gap-y-10 sm:grid-cols-3",
              "lg:flex lg:shrink-0 lg:flex-nowrap lg:gap-x-10 xl:gap-x-20 2xl:gap-x-40",
            )}
          >
            {MARKETING_FOOTER_COLUMNS.map((column) => (
              <div
                key={column.id}
                className="flex min-w-0 flex-col gap-5"
              >
                <p className={footerHeadingClass}>{column.title}</p>
                <nav
                  aria-label={column.title}
                  className="flex flex-col gap-4.5"
                >
                  {column.items.map((item) => (
                    <MarketingNavLink
                      key={item.id}
                      label={item.label}
                      href={item.href}
                      className={footerLinkClass}
                    />
                  ))}
                </nav>
              </div>
            ))}
          </div>
        </div>
      </div>
    </footer>
  )
}

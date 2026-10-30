import { Building2, FileSearch, MapPin } from "lucide-react"
import type { LucideIcon } from "lucide-react"
import { Link } from "react-router-dom"

import { AboutIconBadge } from "@/components/marketing/about/AboutIconBadge"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  ABOUT_COMPANY,
  ABOUT_SECTION_HEADING,
  ABOUT_SECTION_INSET,
  type AboutCompanyCard,
} from "@/content/marketing/aboutPage"
import { cn } from "@/lib/utils"

const COMPANY_ICONS = {
  company: Building2,
  office: MapPin,
  enquiries: FileSearch,
} as const satisfies Record<AboutCompanyCard["id"], LucideIcon>

function CompanyCard({ card }: { card: AboutCompanyCard }) {
  const isEnquiries = card.id === "enquiries"

  return (
    <article className="flex w-full flex-col items-start gap-4 rounded-[4px] bg-white p-[26px] text-[#141414]">
      <AboutIconBadge icon={COMPANY_ICONS[card.id]} glyphClassName="size-6" />
      <div
        className={cn(
          "flex w-full flex-col items-start",
          isEnquiries ? "gap-3" : "gap-3.5",
        )}
      >
        <h3 className="m-0 w-full font-jakarta text-xl font-semibold leading-6">
          {card.title}
        </h3>
        <p className="m-0 w-full font-sans text-base font-normal leading-5">
          {card.body}
        </p>
        {isEnquiries ? (
          <Link
            to={HELP_CENTRE_CONTACT_URL}
            className="font-sans text-base font-normal leading-5 underline underline-offset-2"
          >
            {ABOUT_COMPANY.contactLabel}
          </Link>
        ) : null}
      </div>
    </article>
  )
}

/**
 * Figma The company behind Tummly `5673:24231`.
 * Grey panel with three white cards; bottom radii close the About white card
 * before the sign-up CTA.
 */
export function AboutCompany() {
  return (
    <section
      className={cn(
        "w-full overflow-hidden rounded-bl-[12px] rounded-br-[12px] bg-[#f0f0f0] py-12.5 lg:py-[70px]",
        ABOUT_SECTION_INSET,
      )}
    >
      <div className="flex w-full flex-col items-center gap-10 lg:gap-[70px]">
        <h2
          className={cn(
            "m-0 w-full max-w-[590px] text-center",
            ABOUT_SECTION_HEADING,
            "text-[#0e0e0e]",
          )}
        >
          {ABOUT_COMPANY.title}
        </h2>

        <div className="grid w-full grid-cols-1 items-stretch gap-3 lg:grid-cols-3">
          {ABOUT_COMPANY.cards.map((card) => (
            <CompanyCard key={card.id} card={card} />
          ))}
        </div>
      </div>
    </section>
  )
}

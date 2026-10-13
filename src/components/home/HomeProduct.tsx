import { Link } from "react-router-dom"

import iconAiAssistant from "@/assets/images/marketing/home/product-icons/ai-assistant.svg"
import iconCampaigns from "@/assets/images/marketing/home/product-icons/campaigns.svg"
import iconCapture from "@/assets/images/marketing/home/product-icons/capture.svg"
import iconFeedback from "@/assets/images/marketing/home/product-icons/feedback.svg"
import iconGuests from "@/assets/images/marketing/home/product-icons/guests.svg"
import iconOffers from "@/assets/images/marketing/home/product-icons/offers.svg"
import iconReports from "@/assets/images/marketing/home/product-icons/reports.svg"
import iconWeeklyBrief from "@/assets/images/marketing/home/product-icons/weekly-brief.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  HOME_PRODUCT,
  type HomeProductFeature,
} from "@/content/marketing/homePage"
import { marketingChromeContentInset } from "@/lib/marketing-layout"
import { cn } from "@/lib/utils"

const FEATURE_ICONS = {
  capture: iconCapture,
  feedback: iconFeedback,
  guests: iconGuests,
  offers: iconOffers,
  campaigns: iconCampaigns,
  reports: iconReports,
  "weekly-brief": iconWeeklyBrief,
  "ai-assistant": iconAiAssistant,
} as const satisfies Record<HomeProductFeature["id"], string>

/** Left column then right column order matches Figma `4902:19791`. */
const LEFT_IDS = ["capture", "feedback", "guests", "offers"] as const
const RIGHT_IDS = [
  "campaigns",
  "reports",
  "weekly-brief",
  "ai-assistant",
] as const

function featureById(id: HomeProductFeature["id"]) {
  return HOME_PRODUCT.features.find((feature) => feature.id === id)!
}

function FeatureCard({ feature }: { feature: HomeProductFeature }) {
  const iconSrc = FEATURE_ICONS[feature.id]
  const rotateIcon = feature.id === "ai-assistant"

  return (
    <article className="flex w-full flex-col gap-[22px] border-b border-[#bebebe] pb-5">
      <img
        src={iconSrc}
        alt=""
        width={20}
        height={20}
        className={cn("block size-5 shrink-0", rotateIcon && "rotate-180")}
        aria-hidden
        decoding="async"
      />
      <div className="flex w-full flex-col gap-[7px] text-[#141414]">
        <h3 className="m-0 font-jakarta text-xl font-medium leading-normal">
          {feature.title}
        </h3>
        <p className="m-0 font-sans text-base font-normal leading-normal">
          {feature.body}
        </p>
      </div>
    </article>
  )
}

/**
 * Home Product features — Figma `4902:19791`.
 * Two-column feature grid with Figma icons; CTA to Product.
 */
export function HomeProduct() {
  return (
    <section
      className="w-full bg-white"
      aria-labelledby="home-product-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-start gap-[46px] py-12.5 lg:py-[70px]",
          marketingChromeContentInset,
        )}
      >
        <header className="flex max-w-[548px] flex-col gap-3.5 text-[#141414]">
          <p className="m-0 font-jakarta text-sm font-medium leading-4">
            {HOME_PRODUCT.eyebrow}
          </p>
          <h2
            id="home-product-heading"
            className="m-0 font-jakarta text-[28px] font-medium leading-9 lg:text-[36px] lg:leading-[48px]"
          >
            {HOME_PRODUCT.title}
          </h2>
        </header>

        <div className="flex w-full flex-col gap-[26px] lg:flex-row lg:items-start">
          <div className="flex min-w-0 flex-1 flex-col gap-[26px]">
            {LEFT_IDS.map((id) => (
              <FeatureCard key={id} feature={featureById(id)} />
            ))}
          </div>
          <div className="flex min-w-0 flex-1 flex-col gap-[26px]">
            {RIGHT_IDS.map((id) => (
              <FeatureCard key={id} feature={featureById(id)} />
            ))}
          </div>
        </div>

        <MarketingButton variant="secondary" asChild className="w-fit">
          <Link to={HOME_PRODUCT.ctaTo}>{HOME_PRODUCT.ctaLabel}</Link>
        </MarketingButton>
      </div>
    </section>
  )
}

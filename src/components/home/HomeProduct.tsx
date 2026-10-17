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

/** Left column then right column order matches Figma desktop `4902:19791`. */
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

function FeatureCard({
  feature,
  showDivider = true,
  layout = "stack",
}: {
  feature: HomeProductFeature
  showDivider?: boolean
  /** `scroll` = mobile horizontal strip (Figma `6241:18118`). */
  layout?: "stack" | "scroll"
}) {
  const iconSrc = FEATURE_ICONS[feature.id]
  const rotateIcon = feature.id === "ai-assistant"
  const isScroll = layout === "scroll"

  return (
    <article
      className={cn(
        "flex flex-col gap-[22px] pb-5",
        isScroll
          ? "w-[190px] shrink-0"
          : "w-full",
        showDivider &&
          (isScroll
            ? "border-r border-[#bebebe]"
            : "border-b border-[#bebebe]"),
      )}
    >
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
        <h3
          className={cn(
            "m-0 font-jakarta font-medium",
            isScroll
              ? "text-lg leading-[23px]"
              : "text-lg leading-[23px] lg:text-xl lg:leading-normal",
          )}
        >
          {feature.title}
        </h3>
        <p
          className={cn(
            "m-0 font-sans font-normal",
            isScroll
              ? "text-sm leading-[18px]"
              : "text-sm leading-[18px] lg:text-base lg:leading-normal",
          )}
        >
          {feature.body}
        </p>
      </div>
    </article>
  )
}

/**
 * Home Product features — desktop Figma `4902:19791`, mobile `6241:18118`.
 * Mobile: horizontal scroll strip (24px inset). Desktop: two-column grid.
 */
export function HomeProduct() {
  const features = HOME_PRODUCT.features

  return (
    <section
      className="w-full bg-white"
      aria-labelledby="home-product-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-start gap-10 py-[70px] lg:gap-[46px]",
          // Figma mobile `6241:18118`: 24px. Desktop keep 60px chrome inset.
          "px-6 lg:px-[60px]",
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

        {/* Mobile — horizontal scroll (Figma `6241:18118`). */}
        <div className="-mx-6 w-[calc(100%+3rem)] overflow-x-auto px-6 lg:hidden">
          <div className="flex w-max gap-[26px] pb-1">
            {features.map((feature, index) => (
              <FeatureCard
                key={feature.id}
                feature={feature}
                layout="scroll"
                showDivider={index < features.length - 1}
              />
            ))}
          </div>
        </div>

        {/* Desktop — two columns. */}
        <div className="hidden w-full lg:flex lg:flex-row lg:items-start lg:gap-[26px]">
          <div className="flex min-w-0 flex-1 flex-col gap-[26px]">
            {LEFT_IDS.map((id, index) => (
              <FeatureCard
                key={id}
                feature={featureById(id)}
                showDivider={index < LEFT_IDS.length - 1}
              />
            ))}
          </div>
          <div className="flex min-w-0 flex-1 flex-col gap-[26px]">
            {RIGHT_IDS.map((id, index) => (
              <FeatureCard
                key={id}
                feature={featureById(id)}
                showDivider={index < RIGHT_IDS.length - 1}
              />
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

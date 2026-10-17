import { Link } from "react-router-dom"
import { MailIcon, MessageSquareIcon } from "lucide-react"

import aiAssistantGlyph from "@/assets/svg/marketing/ai-assistant-glyph.svg"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import {
  FOR_GROUPS_PRICING,
  FOR_GROUPS_SECTION_BODY,
  FOR_GROUPS_SECTION_HEADING,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import {
  PRICING_PLAN_FEATURE_LABEL_CLASS,
  PRICING_PLAN_FEATURE_ROW_CLASS,
  PRICING_PLAN_FEATURE_VALUE_CLASS,
  PRICING_PLAN_PRICE_AMOUNT_CLASS,
  PRICING_PLAN_PRICE_SUBLINE_CLASS,
  PRICING_PLAN_PRICE_SUFFIX_CLASS,
  type PricingFeatureRow,
} from "@/content/marketing/pricingPage"
import type { PlanFeatureIcon } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

function FeatureIcon({ icon }: { icon: PlanFeatureIcon }) {
  switch (icon) {
    case "email":
      return <MailIcon className="size-4 shrink-0" aria-hidden />
    case "sms":
      return <MessageSquareIcon className="size-4 shrink-0" aria-hidden />
    case "ai":
      return (
        <img
          src={aiAssistantGlyph}
          alt=""
          width={16}
          height={16}
          className="size-4 shrink-0 rotate-180"
          aria-hidden
        />
      )
  }
}

function PlanFeatureRow({ row }: { row: PricingFeatureRow }) {
  return (
    <div
      className={cn(
        PRICING_PLAN_FEATURE_ROW_CLASS,
        "max-lg:flex-wrap max-lg:whitespace-normal",
      )}
    >
      <div className="flex min-w-0 items-center gap-3">
        {row.icon != null ? <FeatureIcon icon={row.icon} /> : null}
        <p
          className={cn(
            PRICING_PLAN_FEATURE_LABEL_CLASS,
            "max-lg:whitespace-normal",
          )}
        >
          {row.label}
        </p>
      </div>
      <p
        className={cn(
          PRICING_PLAN_FEATURE_VALUE_CLASS,
          "max-lg:whitespace-normal max-lg:shrink",
        )}
      >
        {row.value}
      </p>
    </div>
  )
}

/**
 * Figma For Groups pricing `5661:22463`.
 * Group plan detail + green growth panel; reuses Pricing feature-row tokens.
 */
export function ForGroupsPricing() {
  const { growthPanel } = FOR_GROUPS_PRICING

  return (
    <section
      className="w-full overflow-hidden rounded-bl-[8px] rounded-br-[8px] bg-[#f0f0f0]"
      aria-labelledby="for-groups-pricing-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-start gap-[46px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[728px] flex-col gap-[18px]">
          <h2
            id="for-groups-pricing-heading"
            className={cn("m-0 text-black", FOR_GROUPS_SECTION_HEADING)}
          >
            {FOR_GROUPS_PRICING.title}
          </h2>
          <p className={cn("m-0 max-w-[612px]", FOR_GROUPS_SECTION_BODY)}>
            {FOR_GROUPS_PRICING.body}
          </p>
        </header>

        <div className="flex w-full flex-col gap-[46px] lg:flex-row lg:items-stretch">
          <div className="flex min-w-0 flex-1 flex-col gap-10 py-6">
            <div className="flex flex-col gap-10">
              <div className="flex flex-col gap-3">
                <h3 className="m-0 font-jakarta text-[30px] font-normal leading-none text-[#141414]">
                  {FOR_GROUPS_PRICING.planName}
                </h3>
                <p className="m-0 max-w-[702px] font-sans text-base font-normal leading-[21px] text-[#141414]">
                  {FOR_GROUPS_PRICING.planDescription}
                </p>
              </div>

              <div className="flex flex-col gap-5">
                <div className="flex flex-col gap-2">
                  <p className="m-0 text-[#141414]">
                    <span className={PRICING_PLAN_PRICE_AMOUNT_CLASS}>
                      {FOR_GROUPS_PRICING.priceAmount}
                    </span>
                    <span className={PRICING_PLAN_PRICE_SUFFIX_CLASS}>
                      {FOR_GROUPS_PRICING.priceSuffix}
                    </span>
                  </p>
                  <p
                    className={cn(
                      PRICING_PLAN_PRICE_SUBLINE_CLASS,
                      "min-h-0",
                    )}
                  >
                    {FOR_GROUPS_PRICING.priceSubline}
                  </p>
                </div>

                <MarketingButton
                  asChild
                  variant="secondary"
                  className="w-full sm:w-[158px]"
                >
                  <RequestTrialLink
                    planIntent={{ plan: "Group", cadence: "monthly" }}
                  >
                    {FOR_GROUPS_PRICING.ctaLabel}
                  </RequestTrialLink>
                </MarketingButton>
              </div>
            </div>

            <div className="flex flex-col gap-5.5">
              {FOR_GROUPS_PRICING.coreFeatures.map((row) => (
                <PlanFeatureRow key={row.label} row={row} />
              ))}
              <hr className="m-0 border-[#e0e0e0]" />
              {FOR_GROUPS_PRICING.allowanceFeatures.map((row) => (
                <PlanFeatureRow
                  key={`${row.label}-${row.value}`}
                  row={row}
                />
              ))}
            </div>
          </div>

          <aside className="flex min-w-0 flex-1 flex-col gap-8 self-stretch bg-[#dfebe3] px-8 py-7">
            <div className="flex max-w-[548px] flex-col gap-[18px] text-black">
              <h3 className="m-0 font-jakarta text-[28px] font-normal leading-none">
                {growthPanel.title}
              </h3>
              <p className="m-0 font-sans text-base font-normal leading-normal">
                {growthPanel.body}
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-5">
              <MarketingButton asChild variant="primary" className="w-[158px]">
                <RequestTrialLink
                  planIntent={{ plan: "Group", cadence: "monthly" }}
                >
                  {growthPanel.primaryLabel}
                </RequestTrialLink>
              </MarketingButton>
              <MarketingButton asChild variant="secondary">
                <Link to={growthPanel.secondaryTo}>
                  {growthPanel.secondaryLabel}
                </Link>
              </MarketingButton>
            </div>
          </aside>
        </div>

        <MarketingButton asChild variant="secondary" className="w-fit">
          <Link to={FOR_GROUPS_PRICING.compareTo}>
            {FOR_GROUPS_PRICING.compareLabel}
          </Link>
        </MarketingButton>
      </div>
    </section>
  )
}

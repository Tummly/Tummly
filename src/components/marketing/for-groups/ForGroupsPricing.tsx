import { MinusIcon, PlusIcon, MailIcon, MessageSquareIcon } from "lucide-react"
import { useState } from "react"
import { Link } from "react-router-dom"

import aiAssistantGlyph from "@/assets/svg/marketing/ai-assistant-glyph.svg"
import { CustomPlanQuoteStatus } from "@/components/marketing/custom-plan/CustomPlanSummaryCard"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { PublicPlanPriceFallback } from "@/components/marketing/PublicPlanPricesStatus"
import { RequestTrialLink } from "@/components/navigation/RequestTrialLink"
import { Skeleton } from "@/components/ui/skeleton"
import {
  FOR_GROUPS_PRICING,
  FOR_GROUPS_SECTION_BODY,
  FOR_GROUPS_SECTION_HEADING,
  FOR_GROUPS_SECTION_INSET,
  FOR_GROUPS_SECTION_Y,
} from "@/content/marketing/forGroupsPage"
import {
  PRICING_CADENCE_COPY,
  PRICING_PLAN_FEATURE_LABEL_CLASS,
  PRICING_PLAN_FEATURE_ROW_CLASS,
  PRICING_PLAN_FEATURE_VALUE_CLASS,
  PRICING_PLAN_PRICE_AMOUNT_CLASS,
  PRICING_PLAN_PRICE_SUBLINE_CLASS,
  PRICING_PLAN_PRICE_SUFFIX_CLASS,
  type PricingFeatureRow,
} from "@/content/marketing/pricingPage"
import {
  formatPenceGbp,
  lastQuote,
  quoteView,
} from "@/lib/marketingCustomPlan/customPlanQuoteState"
import { buildForGroupsSignupIntent } from "@/lib/marketingCustomPlan/forGroupsSignupIntent"
import { useServerCustomPlanQuote } from "@/lib/marketingCustomPlan/useServerCustomPlanQuote"
import { usePublicPlanPrices } from "@/lib/marketingCustomPlan/usePublicPlanPrices"
import type {
  BillingCadence,
  PlanFeatureIcon,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
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
        // Figma `5661:22463`: 14px text on an 18.2px line.
        "leading-[18.228px] max-lg:flex-wrap max-lg:whitespace-normal",
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
 * Group plan column + Locations calculator. Prices come from the server quote.
 */
export function ForGroupsPricing() {
  const { calculator } = FOR_GROUPS_PRICING
  const [locationCount, setLocationCount] = useState<number>(
    FOR_GROUPS_PRICING.includedLocations,
  )
  const [cadence, setCadence] = useState<BillingCadence>("monthly")
  const { status: pricesStatus, prices } = usePublicPlanPrices()
  const { state, check, retry } = useServerCustomPlanQuote({
    cadence,
    basePlan: "group",
    locations: locationCount,
    packs: {},
  })
  const shown = lastQuote(state)
  // A quote for the other cadence must not show with this cadence's suffix.
  const shownMatchesCadence = shown != null && shown.cadence === cadence
  const view = quoteView({ check, state, changed: false })
  // Above the included Locations, Continue carries the configured plan and quote hash.
  const continueIntent = buildForGroupsSignupIntent({
    locations: locationCount,
    includedLocations: FOR_GROUPS_PRICING.includedLocations,
    cadence,
    quote: state.status === "ready" ? state.quote : null,
  })
  const groupPrice = prices.Group
  const totalSuffix = cadence === "annual" ? "/ per year" : "/ per month"

  return (
    <section
      className="w-full overflow-hidden bg-[#f0f0f0]"
      aria-labelledby="for-groups-pricing-heading"
    >
      <div
        className={cn(
          "mx-auto flex w-full flex-col items-center gap-[46px] lg:gap-[70px]",
          FOR_GROUPS_SECTION_INSET,
          FOR_GROUPS_SECTION_Y,
        )}
      >
        <header className="flex w-full max-w-[656px] flex-col items-center gap-[22px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5">
            <p className="m-0 font-sans text-sm font-medium uppercase leading-[normal] text-black">
              {FOR_GROUPS_PRICING.eyebrow}
            </p>
            <h2
              id="for-groups-pricing-heading"
              className={cn(
                "m-0 max-w-[464px] text-black",
                FOR_GROUPS_SECTION_HEADING,
              )}
            >
              {FOR_GROUPS_PRICING.title}
            </h2>
          </div>
          <p
            className={cn(
              "m-0 max-w-[604px]",
              FOR_GROUPS_SECTION_BODY,
              "lg:leading-[26px]",
            )}
          >
            {FOR_GROUPS_PRICING.body}
          </p>
        </header>

        <div className="flex w-full flex-col items-start gap-[46px] lg:flex-row">
          <div className="flex w-full min-w-0 flex-1 flex-col gap-10 py-6">
            <div className="flex flex-col gap-3">
              <h3 className="m-0 font-jakarta text-[30px] font-semibold leading-[37.8px] text-[#141414]">
                {FOR_GROUPS_PRICING.planName}
              </h3>
              <p className="m-0 max-w-[526px] font-sans text-base font-normal leading-[21px] text-[#141414]">
                {FOR_GROUPS_PRICING.planDescription}
              </p>
            </div>

            {/* Figma `5661:22463`: 40px under the title, 20px between price and rows. */}
            <div className="flex flex-col gap-5">
              <div className="flex flex-col gap-5">
                <div className="flex flex-col gap-2">
                  {groupPrice != null ? (
                    <p className="m-0 min-h-[47px] text-[#141414]">
                      <span className={PRICING_PLAN_PRICE_AMOUNT_CLASS}>
                        {groupPrice.monthly}
                      </span>
                      <span
                        className={cn(
                          PRICING_PLAN_PRICE_SUFFIX_CLASS,
                          "whitespace-nowrap",
                        )}
                      >
                        {FOR_GROUPS_PRICING.priceSuffix}
                      </span>
                    </p>
                  ) : (
                    <PublicPlanPriceFallback status={pricesStatus} />
                  )}
                  {groupPrice != null ? (
                    <p
                      className={cn(
                        PRICING_PLAN_PRICE_SUBLINE_CLASS,
                        "min-h-0 leading-[18.228px]",
                      )}
                    >
                      {`${groupPrice.annual}/year`}
                    </p>
                  ) : null}
                </div>

                <div className="flex w-full flex-col gap-4">
                  <MarketingButton asChild variant="primary" className="w-full">
                    <RequestTrialLink
                      planIntent={{ plan: "Group", cadence: "monthly" }}
                    >
                      {FOR_GROUPS_PRICING.ctaLabel}
                    </RequestTrialLink>
                  </MarketingButton>
                  <MarketingButton asChild variant="secondary" className="w-full">
                    <Link to={FOR_GROUPS_PRICING.compareTo}>
                      {FOR_GROUPS_PRICING.compareLabel}
                    </Link>
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
          </div>

          <aside className="flex w-full min-w-0 flex-1 flex-col overflow-hidden rounded-[12px]">
            <div className="flex flex-col gap-8 rounded-tl-[8px] rounded-tr-[8px] bg-[#dfebe3] px-5 py-7 sm:px-8">
              <div className="flex max-w-[548px] flex-col gap-[18px] text-black">
                <p className="m-0 font-sans text-sm font-medium uppercase leading-[normal]">
                  {calculator.eyebrow}
                </p>
                <div className="flex flex-col gap-3.5">
                  <h3 className="m-0 font-jakarta text-[28px] font-bold leading-[normal]">
                    {calculator.title}
                  </h3>
                  <p className="m-0 max-w-[486px] font-sans text-base font-normal leading-[21px]">
                    {calculator.body}
                  </p>
                </div>
              </div>

              <div className="flex w-full flex-col items-start gap-5 rounded-[12px] border border-[#e5e5e5] bg-[#f4f4f4] p-5 sm:flex-row sm:items-center sm:justify-between sm:gap-4 sm:p-[30px] lg:max-[1320px]:flex-col lg:max-[1320px]:items-start">
                <div className="flex min-w-0 flex-col gap-2">
                  <p className="m-0 font-sans text-[22px] font-normal leading-none text-[#141414] sm:text-[26px]">
                    {calculator.locationLabel}
                  </p>
                  <p className="m-0 font-sans text-sm font-medium leading-[normal] text-[#7c7c7c]">
                    {calculator.locationHint}
                  </p>
                </div>
                <div className="flex shrink-0 items-center gap-8">
                  <button
                    type="button"
                    aria-label="Decrease Locations"
                    disabled={locationCount <= FOR_GROUPS_PRICING.minLocations}
                    className="inline-flex size-7 cursor-pointer items-center justify-center rounded text-[#141414] transition-opacity hover:opacity-70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30 disabled:cursor-not-allowed disabled:opacity-30"
                    onClick={() => {
                      setLocationCount((count) =>
                        Math.max(FOR_GROUPS_PRICING.minLocations, count - 1),
                      )
                    }}
                  >
                    <MinusIcon className="size-7" strokeWidth={1.5} aria-hidden />
                  </button>
                  <p
                    className="m-0 min-w-[1.5ch] text-center font-sans text-[32px] font-normal leading-none text-black"
                    aria-live="polite"
                  >
                    {locationCount}
                  </p>
                  <button
                    type="button"
                    aria-label="Increase Locations"
                    disabled={locationCount >= FOR_GROUPS_PRICING.maxLocations}
                    className="inline-flex size-7 cursor-pointer items-center justify-center rounded text-[#141414] transition-opacity hover:opacity-70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30 disabled:cursor-not-allowed disabled:opacity-30"
                    onClick={() => {
                      setLocationCount((count) =>
                        Math.min(FOR_GROUPS_PRICING.maxLocations, count + 1),
                      )
                    }}
                  >
                    <PlusIcon className="size-7" strokeWidth={1.5} aria-hidden />
                  </button>
                </div>
              </div>
            </div>

            <div className="flex flex-col gap-8 bg-[#e5e5e5] px-5 py-7 sm:px-8">
              <div
                className="flex w-full items-center gap-2.5 rounded-[10px] border border-[rgba(38,38,38,0.21)] bg-[#f4f4f4] p-3"
                role="group"
                aria-label="Billing cadence"
              >
                <button
                  type="button"
                  aria-pressed={cadence === "monthly"}
                  className={cn(
                    "inline-flex h-[41px] flex-1 cursor-pointer items-center justify-center rounded-[6px] px-3 text-base font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30",
                    cadence === "monthly"
                      ? "bg-[#dfebe3] text-[#141414]"
                      : "text-[#7c7c7c] hover:bg-[#efefef] hover:text-[#141414]",
                  )}
                  onClick={() => {
                    setCadence("monthly")
                  }}
                >
                  {PRICING_CADENCE_COPY.monthly}
                </button>
                <button
                  type="button"
                  aria-pressed={cadence === "annual"}
                  className={cn(
                    "inline-flex h-[41px] flex-1 cursor-pointer items-center justify-center gap-2.5 rounded-[6px] px-3 text-base font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#141414]/30",
                    cadence === "annual"
                      ? "bg-[#dfebe3] text-[#141414]"
                      : "text-[#7c7c7c] hover:bg-[#efefef] hover:text-[#141414]",
                  )}
                  onClick={() => {
                    setCadence("annual")
                  }}
                >
                  {PRICING_CADENCE_COPY.annual}
                  <span className="rounded-full bg-[#14a74a] px-2.5 py-2 text-[10px] whitespace-nowrap font-medium leading-none text-white">
                    {PRICING_CADENCE_COPY.saveBadge}
                  </span>
                </button>
              </div>

              <div className="flex w-full flex-col gap-7 text-black">
                <p className="m-0 font-jakarta text-base font-medium uppercase leading-none">
                  {calculator.estimateHeading}
                </p>
                <div className="flex flex-col gap-[18px] font-sans text-base font-normal">
                  {shown?.lines.map((line) => (
                    <div
                      key={line.key}
                      className="flex items-center justify-between gap-3"
                    >
                      <p className="m-0 leading-[18px]">{line.label}</p>
                      <p className="m-0 leading-[18px]">
                        {formatPenceGbp(line.netPence)}
                      </p>
                    </div>
                  ))}
                  {view.message != null ? (
                    <CustomPlanQuoteStatus
                      message={view.message}
                      showRetry={view.showRetry}
                      onRetry={retry}
                      showContact={view.showContact}
                    />
                  ) : null}
                </div>
              </div>

              <hr className="m-0 w-full border-[#cfcfcf]" />

              <div className="flex flex-col gap-2">
                <div className="flex items-center justify-between gap-4">
                  <p className="m-0 font-sans text-[22px] font-normal leading-none text-black">
                    {calculator.estimatedTotalLabel}
                  </p>
                  {shown != null && shownMatchesCadence ? (
                    <p className="m-0 text-[#141414]">
                      <span className={PRICING_PLAN_PRICE_AMOUNT_CLASS}>
                        {formatPenceGbp(shown.periodNetPence)}
                      </span>
                      <span
                        className={cn(
                          PRICING_PLAN_PRICE_SUFFIX_CLASS,
                          "whitespace-nowrap",
                        )}
                      >
                        {totalSuffix}
                      </span>
                    </p>
                  ) : state.status === "failed" ? null : (
                    <Skeleton className="h-9 w-28" aria-hidden />
                  )}
                </div>
                {view.taxText != null ? (
                  <p className="m-0 font-sans text-sm font-normal leading-[normal] text-black">
                    {view.taxText}
                  </p>
                ) : null}
              </div>

              <div className="flex w-full flex-col gap-[22px] sm:flex-row sm:items-stretch">
                {continueIntent != null && view.continueEnabled ? (
                  <MarketingButton
                    asChild
                    variant="dark"
                    className="h-[54px] min-h-[54px] w-full flex-1"
                  >
                    <RequestTrialLink planIntent={continueIntent}>
                      {calculator.continueLabel}
                    </RequestTrialLink>
                  </MarketingButton>
                ) : (
                  <MarketingButton
                    variant="dark"
                    className="h-[54px] min-h-[54px] w-full flex-1"
                    disabled
                  >
                    {calculator.continueLabel}
                  </MarketingButton>
                )}
                <MarketingButton
                  asChild
                  variant="secondary"
                  className="h-[54px] min-h-[54px] w-full flex-1"
                >
                  <Link to={calculator.contactTo}>
                    {calculator.contactLabel}
                  </Link>
                </MarketingButton>
              </div>
            </div>
          </aside>
        </div>
      </div>
    </section>
  )
}

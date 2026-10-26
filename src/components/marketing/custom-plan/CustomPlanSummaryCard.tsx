import { Link } from "react-router-dom"

import { MarketingButton } from "@/components/marketing/MarketingButton"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { PRICING_CADENCE_COPY } from "@/content/marketing/pricingPage"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import type { CustomPlanV2Estimate } from "@/lib/marketingCustomPlan/customPlanEstimate"
import { formatPenceGbp } from "@/lib/marketingCustomPlan/customPlanQuoteState"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

function formatCount(n: number): string {
  return n.toLocaleString("en-GB")
}

export type CustomPlanSummaryCardProps = {
  estimate: CustomPlanV2Estimate
  cadence: BillingCadence
  onCadenceChange?: (cadence: BillingCadence) => void
  planDescription?: string
  continueLabel?: string
  onContinue?: () => void
  continueHref?: string
  /** Blocks checkout while catalog allowances are missing. */
  continueDisabled?: boolean
  contactLabel?: string
  contactHref?: string
  showCadenceToggle?: boolean
  showActions?: boolean
  checkoutNote?: string
  badgeLabel?: string
  className?: string
}

export function CustomPlanSummaryCard({
  estimate,
  cadence,
  onCadenceChange,
  planDescription,
  continueLabel = "Continue",
  onContinue,
  continueHref,
  continueDisabled = false,
  contactLabel = "Contact us",
  contactHref = HELP_CENTRE_CONTACT_URL,
  showCadenceToggle = true,
  showActions = true,
  checkoutNote = "No payment is taken until your subscription details are confirmed at checkout.",
  badgeLabel = "Selected",
  className,
}: CustomPlanSummaryCardProps) {
  const totalSuffix =
    cadence === "annual" ? "/ per year" : "/ per month"
  const planName =
    estimate.basePlan.charAt(0).toUpperCase() + estimate.basePlan.slice(1)
  const q = estimate.quantities

  return (
    <aside
      className={cn(
        "flex w-full max-w-[581px] flex-col gap-[26px] pt-6",
        className,
      )}
    >
      {showCadenceToggle && onCadenceChange != null ? (
        <div
          className="flex w-full items-center gap-2.5 rounded-[10px] border border-[rgba(38,38,38,0.21)] bg-[#f4f4f4] p-2"
          role="group"
          aria-label="Billing cadence"
        >
          <Button
            type="button"
            variant="ghost"
            aria-pressed={cadence === "monthly"}
            className={cn(
              "h-[41px] flex-1 rounded-[6px] px-3 text-base font-medium shadow-none",
              cadence === "monthly"
                ? "bg-[#dfebe3] text-[#141414] hover:bg-[#dfebe3]"
                : "text-[#7c7c7c] hover:bg-[#efefef] hover:text-[#141414]",
            )}
            onClick={() => onCadenceChange("monthly")}
          >
            {PRICING_CADENCE_COPY.monthly}
          </Button>
          <Button
            type="button"
            variant="ghost"
            aria-pressed={cadence === "annual"}
            className={cn(
              "h-[41px] flex-1 gap-2.5 rounded-[6px] px-3 text-base font-medium shadow-none",
              cadence === "annual"
                ? "bg-[#dfebe3] text-[#141414] hover:bg-[#dfebe3]"
                : "text-[#7c7c7c] hover:bg-[#efefef] hover:text-[#141414]",
            )}
            onClick={() => onCadenceChange("annual")}
          >
            {PRICING_CADENCE_COPY.annual}
            <Badge className="rounded-full border-0 bg-[#14a74a] px-2.5 py-2 text-[10px] font-medium leading-none text-white hover:bg-[#14a74a]">
              {PRICING_CADENCE_COPY.saveBadge}
            </Badge>
          </Button>
        </div>
      ) : null}

      <div className="flex w-full flex-col gap-[26px]">
        <div className="overflow-hidden rounded-xl">
          <div className="flex flex-col gap-8 rounded-t-lg bg-[#dfebe3] p-7">
            <div className="flex items-center justify-between gap-3">
              <p className="m-0 font-jakarta text-base font-medium uppercase text-black">
                Your estimated plan
              </p>
              <Badge className="rounded-[20px] border-0 bg-[#b6d1bf] px-3 py-1.5 font-jakarta text-xs font-semibold text-black hover:bg-[#b6d1bf]">
                {badgeLabel}
              </Badge>
            </div>
            <div className="flex flex-col gap-2 text-black">
              <p className="m-0 font-jakarta text-[34px] font-bold leading-none">
                {planName}
              </p>
              {planDescription != null && planDescription !== "" ? (
                <p className="m-0 max-w-[379px] font-jakarta text-sm font-normal">
                  {planDescription}
                </p>
              ) : null}
            </div>
            <div className="flex flex-col gap-3.5">
              <div className="flex flex-wrap gap-x-10 gap-y-3 font-jakarta text-xs font-semibold">
                <div className="flex flex-col gap-1.5">
                  <span className="text-[#707070]">Locations</span>
                  <span className="text-black">{formatCount(q.locations)}</span>
                </div>
                <div className="flex flex-col gap-1.5">
                  <span className="text-[#707070]">Team users</span>
                  <span className="text-black">{formatCount(q.teamUsers)}</span>
                </div>
                <div className="flex flex-col gap-1.5">
                  <span className="text-[#707070]">Active QR</span>
                  <span className="text-black">
                    {formatCount(q.qrPlacements)} per Location
                    {q.qrPlacementsAccountExtra > 0
                      ? ` + ${formatCount(q.qrPlacementsAccountExtra)} account`
                      : ""}
                  </span>
                </div>
                <div className="flex flex-col gap-1.5">
                  <span className="text-[#707070]">Active Offers</span>
                  <span className="text-black">
                    {formatCount(q.offers)} account-wide
                  </span>
                </div>
              </div>
              <hr className="m-0 border-[#c9c9c9]" />
              <div className="flex flex-wrap gap-x-10 gap-y-3 font-jakarta text-xs font-semibold">
                <div className="flex flex-col gap-1.5">
                  <span className="text-[#707070]">Email</span>
                  <span className="text-black">
                    {formatCount(q.emailCredits)}/mo
                  </span>
                </div>
                <div className="flex flex-col gap-1.5">
                  <span className="text-[#707070]">SMS</span>
                  <span className="text-black">
                    {formatCount(q.smsCredits)}/mo
                  </span>
                </div>
                <div className="flex flex-col gap-1.5">
                  <span className="text-[#707070]">AI</span>
                  <span className="text-black">
                    {formatCount(q.aiCredits)}/mo
                  </span>
                </div>
              </div>
            </div>
          </div>

          {estimate.lines.length > 0 ? (
            <div className="bg-[#efefef] p-7">
              <ul className="m-0 flex list-none flex-col gap-[18px] p-0 font-sans text-base text-black">
                {estimate.lines.map((line) => (
                  <li
                    key={line.key}
                    className="flex items-center justify-between gap-3"
                  >
                    <span className="leading-[18px]">{line.label}</span>
                    <span className="leading-[18px]">
                      {formatPenceGbp(line.netPence)}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}

          <div className="flex flex-col gap-8 bg-[#e5e5e5] p-7">
            <div className="flex items-center justify-between gap-4">
              <p className="m-0 font-sans text-lg text-black">Estimated total</p>
              <p className="m-0 text-[#141414]">
                <span className="font-sans text-[36px] font-normal leading-none">
                  {formatPenceGbp(estimate.periodNetPence)}
                </span>
                <span className="font-sans text-sm font-normal">
                  {totalSuffix}
                </span>
              </p>
            </div>
            {showActions ? (
              <div className="flex w-full flex-col gap-[22px] sm:flex-row sm:items-stretch">
                {continueHref != null && !continueDisabled ? (
                  <MarketingButton
                    asChild
                    variant="dark"
                    className="w-full flex-1"
                  >
                    <Link to={continueHref}>{continueLabel}</Link>
                  </MarketingButton>
                ) : (
                  <MarketingButton
                    type="button"
                    variant="dark"
                    className="w-full flex-1"
                    disabled={continueDisabled}
                    onClick={onContinue}
                  >
                    {continueLabel}
                  </MarketingButton>
                )}
                <MarketingButton
                  asChild
                  variant="secondary"
                  className="w-full flex-1"
                >
                  <Link to={contactHref}>{contactLabel}</Link>
                </MarketingButton>
              </div>
            ) : null}
          </div>
        </div>

        {checkoutNote != null && checkoutNote !== "" ? (
          <p className="m-0 font-sans text-sm font-normal leading-5 text-[#676767]">
            {checkoutNote}
          </p>
        ) : null}
      </div>
    </aside>
  )
}

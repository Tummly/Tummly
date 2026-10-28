import { Link } from "react-router-dom"

import type { CustomPlanQuoteResult } from "@/api/adminCommercialCatalogApi"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Skeleton } from "@/components/ui/skeleton"
import { PRICING_CADENCE_COPY } from "@/content/marketing/pricingPage"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  formatQrWorkspacePool,
  quoteCadenceSuffix,
  quotePlanName,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import { formatPenceGbp } from "@/lib/marketingCustomPlan/customPlanQuoteState"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

function formatCount(n: number): string {
  return n.toLocaleString("en-GB")
}

/** Quote status: the message, then the over-limit help and Contact us link, then Try again. */
export function CustomPlanQuoteStatus({
  message,
  showRetry = false,
  onRetry,
  showContact = false,
  contactHref = CUSTOM_PLAN_QUOTE_COPY.contactUsHref,
}: {
  message: string
  showRetry?: boolean
  onRetry?: () => void
  showContact?: boolean
  contactHref?: string
}) {
  return (
    <div className="flex flex-col items-start gap-3">
      <p
        role="status"
        className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]"
      >
        {message}
      </p>
      {showContact ? (
        <>
          <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
            {CUSTOM_PLAN_QUOTE_COPY.locationOverLimitHelp}
          </p>
          <MarketingButton asChild variant="dark" className="w-fit">
            <Link to={contactHref}>
              {CUSTOM_PLAN_QUOTE_COPY.contactUs}
            </Link>
          </MarketingButton>
        </>
      ) : null}
      {showRetry && onRetry != null ? (
        <MarketingButton type="button" variant="secondary" onClick={onRetry}>
          {CUSTOM_PLAN_QUOTE_COPY.retry}
        </MarketingButton>
      ) : null}
    </div>
  )
}

/**
 * Full server quote for the price confirmation: period, net, VAT, gross, what is
 * due now, the recurring amount and when the plan starts. No browser arithmetic.
 */
export function CustomPlanQuoteBreakdown({
  quote,
}: {
  quote: CustomPlanQuoteResult
}) {
  const copy = CUSTOM_PLAN_QUOTE_COPY.breakdown
  const annual = quote.cadence === "annual"
  const rows: { label: string; value: string }[] = [
    {
      label: copy.billing,
      value: annual ? PRICING_CADENCE_COPY.annual : PRICING_CADENCE_COPY.monthly,
    },
    { label: copy.net, value: formatPenceGbp(quote.periodNetPence) },
    {
      label: copy.vat,
      value: formatPenceGbp(quote.vatAmountPence),
    },
    { label: copy.gross, value: formatPenceGbp(quote.grossTotalPence) },
    { label: copy.dueNow, value: formatPenceGbp(quote.dueNowGrossPence) },
    {
      label: annual ? copy.recurringAnnual : copy.recurringMonthly,
      value: formatPenceGbp(quote.recurringGrossPence),
    },
    {
      label: copy.effective,
      value:
        quote.effectiveTiming === "on_payment_confirmed"
          ? copy.effectiveOnPayment
          : quote.effectiveTiming,
    },
  ]
  return (
    <dl
      aria-label="Quote breakdown"
      className="m-0 flex flex-col gap-3 font-sans text-sm text-[#141414]"
    >
      {rows.map((row) => (
        <div
          key={row.label}
          className="flex items-center justify-between gap-3"
        >
          <dt className="m-0 font-normal">{row.label}</dt>
          <dd className="m-0 font-medium">{row.value}</dd>
        </div>
      ))}
    </dl>
  )
}

export type CustomPlanSummaryCardProps = {
  quote: CustomPlanQuoteResult
  cadence: BillingCadence
  onCadenceChange?: (cadence: BillingCadence) => void
  planDescription?: string
  continueLabel?: string
  onContinue?: () => void
  continueHref?: string
  /** Blocks checkout until the server quote is ready for the current inputs. */
  continueDisabled?: boolean
  contactLabel?: string
  contactHref?: string
  showCadenceToggle?: boolean
  showActions?: boolean
  /** Show VAT, gross total, due now, recurring amount and start. For the price confirmation. */
  showBreakdown?: boolean
  checkoutNote?: string
  /** Quote status under the total: loading, failed or changed (doc 08). */
  statusMessage?: string | null
  showRetry?: boolean
  onRetry?: () => void
  showContact?: boolean
  /** Doc 08 VAT line, passed only when the quote carries VAT. */
  taxText?: string | null
  badgeLabel?: string
  /**
   * Sticky use: cap the card at the viewport height so the cadence toggle and
   * the total stay in view; the plan details scroll inside the card.
   */
  fitViewport?: boolean
  className?: string
}

export function CustomPlanSummaryCard({
  quote,
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
  showBreakdown = false,
  checkoutNote = CUSTOM_PLAN_QUOTE_COPY.noPaymentTaken,
  statusMessage = null,
  showRetry = false,
  onRetry,
  showContact = false,
  taxText = null,
  badgeLabel = "Selected",
  fitViewport = false,
  className,
}: CustomPlanSummaryCardProps) {
  // The suffix follows the quote the amount came from, not the toggle.
  const totalSuffix = quoteCadenceSuffix(quote.cadence)
  const planName = quotePlanName(quote)
  const q = quote.quantities

  return (
    <aside
      className={cn(
        "flex w-full max-w-[581px] flex-col gap-[26px] pt-6",
        fitViewport && "lg:max-h-[calc(100dvh-106px)]",
        className,
      )}
    >
      {showCadenceToggle && onCadenceChange != null ? (
        <div
          className="flex w-full shrink-0 items-center gap-2.5 rounded-[10px] border border-[rgba(38,38,38,0.21)] bg-[#f4f4f4] p-2"
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

      <div
        className={cn(
          "flex w-full flex-col gap-[26px]",
          fitViewport && "lg:min-h-0",
        )}
      >
        <div
          className={cn(
            "overflow-hidden rounded-xl",
            fitViewport && "lg:flex lg:min-h-0 lg:flex-col",
          )}
        >
          <div
            className={cn(
              fitViewport &&
                "lg:min-h-0 lg:overflow-y-auto lg:overscroll-contain",
            )}
          >
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
                      {formatQrWorkspacePool(quote.workspaceQrPool)}
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

            {quote.lines.length > 0 ? (
              <div className="bg-[#efefef] p-7">
                <ul className="m-0 flex list-none flex-col gap-[18px] p-0 font-sans text-base text-black">
                  {quote.lines.map((line) => (
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
          </div>

          <div className="flex shrink-0 flex-col gap-8 bg-[#e5e5e5] p-7">
            <div className="flex items-center justify-between gap-4">
              <p className="m-0 font-sans text-lg text-black">Estimated total</p>
              <p className="m-0 text-[#141414]">
                <span className="font-sans text-[36px] font-normal leading-none">
                  {formatPenceGbp(quote.periodNetPence)}
                </span>
                <span className="font-sans text-sm font-normal">
                  {totalSuffix}
                </span>
              </p>
            </div>
            {showBreakdown ? <CustomPlanQuoteBreakdown quote={quote} /> : null}
            {statusMessage != null && statusMessage !== "" ? (
              <CustomPlanQuoteStatus
                message={statusMessage}
                showRetry={showRetry}
                onRetry={onRetry}
                showContact={showContact}
                contactHref={contactHref}
              />
            ) : null}
            {taxText != null && taxText !== "" ? (
              <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
                {taxText}
              </p>
            ) : null}
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
          <p className="m-0 shrink-0 font-sans text-sm font-normal leading-5 text-[#676767]">
            {checkoutNote}
          </p>
        ) : null}
      </div>
    </aside>
  )
}

/**
 * Shown in place of the summary card until a quote exists: the loading line
 * with a Skeleton, or the failed line with Try again.
 */
export function CustomPlanQuotePlaceholder({
  loading,
  message,
  showRetry = false,
  onRetry,
  showContact = false,
  className,
}: {
  loading: boolean
  message: string | null
  showRetry?: boolean
  onRetry?: () => void
  showContact?: boolean
  className?: string
}) {
  return (
    <aside
      className={cn(
        "flex w-full max-w-[581px] flex-col gap-[26px] pt-6",
        className,
      )}
    >
      {loading ? <Skeleton className="h-[320px] w-full rounded-xl" /> : null}
      {message != null && message !== "" ? (
        <CustomPlanQuoteStatus
          message={message}
          showRetry={showRetry}
          onRetry={onRetry}
          showContact={showContact}
        />
      ) : null}
    </aside>
  )
}

/**
 * Over the online limit the server gives no price: the message, the help line
 * and a full-width Contact us take the place of the total and Continue.
 */
export function CustomPlanContactCard({
  message,
  contactHref,
  className,
}: {
  message: string
  contactHref: string
  className?: string
}) {
  return (
    <aside
      className={cn(
        "flex w-full max-w-[581px] flex-col gap-[26px] pt-6",
        className,
      )}
    >
      <div className="flex flex-col gap-8 rounded-xl bg-[#e5e5e5] p-7">
        <div className="flex flex-col gap-3">
          <p
            role="status"
            className="m-0 font-sans text-lg font-normal leading-6 text-black"
          >
            {message}
          </p>
          <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
            {CUSTOM_PLAN_QUOTE_COPY.locationOverLimitHelp}
          </p>
        </div>
        <MarketingButton asChild variant="dark" className="w-full">
          <Link to={contactHref}>{CUSTOM_PLAN_QUOTE_COPY.contactUs}</Link>
        </MarketingButton>
      </div>
    </aside>
  )
}

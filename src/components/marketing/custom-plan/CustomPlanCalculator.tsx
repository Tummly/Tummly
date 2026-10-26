import { useEffect, useState } from "react"
import { Link } from "react-router-dom"

import {
  fetchPublicCustomUnitRates,
  type CustomPlanQuoteResult,
  type PublicCatalogPackMetaDto,
} from "@/api/adminCommercialCatalogApi"
import qrPlacementsCardTop from "@/assets/images/marketing/custom-plan/qr-placements-card-top.png"
import mapPinIcon from "@/assets/svg/marketing/custom-plan/map-pin.svg"
import creditsTipIcon from "@/assets/svg/marketing/pricing/usage/credits-badge-icon.svg"
import { CustomPlanStepper } from "@/components/marketing/custom-plan/CustomPlanStepper"
import {
  CustomPlanQuotePlaceholder,
  CustomPlanSummaryCard,
} from "@/components/marketing/custom-plan/CustomPlanSummaryCard"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import { CUSTOM_PLAN_STEPS } from "@/content/marketing/customPlanPage"
import {
  CUSTOM_PLAN_MAX_LOCATIONS,
  CUSTOM_PLAN_MAX_PACK_COUNT,
  CUSTOM_PLAN_PACKS,
  EMPTY_PACK_COUNTS,
  basePlanRank,
  basePlanToChosenPlan,
  chosenPlanToBasePlan,
  minBasePlanForLocations,
  normalizePackCounts,
  quotePackLineAmount,
  quotePlanName,
  type CustomPlanBasePlanKey,
  type CustomPlanPackCounts,
  type CustomPlanPackKey,
  type CustomPlanV2ConfigInput,
  type PublicCatalogPlanSummary,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import {
  formatPenceGbp,
  lastQuote,
  quoteView,
} from "@/lib/marketingCustomPlan/customPlanQuoteState"
import { useServerCustomPlanQuote } from "@/lib/marketingCustomPlan/useServerCustomPlanQuote"
import { toPublicCatalogPlanSummaries } from "@/lib/marketingCustomPlan/publicCatalogPlans"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

function StepBadge({ label }: { label: string }) {
  return (
    <span className="flex size-11 shrink-0 items-center justify-center rounded-full border border-[#e0e5eb] font-jakarta text-base font-semibold text-[#141414]">
      {label}
    </span>
  )
}

/** Pack size, plus the server pack price when the catalogue has one. */
function packHint(
  monthlyPackPence: number | null,
  unitLabel: string,
  packSize: number,
): string {
  const size = `${packSize.toLocaleString("en-GB")} ${unitLabel} per pack`
  if (monthlyPackPence == null) {
    return size
  }
  return `${size} · ${formatPenceGbp(monthlyPackPence)}/month`
}

/** "Included in Starter: 3 · Your total: 8 users" under a pack row. */
function IncludedLine({
  planName,
  included,
  total,
  unit,
}: {
  planName: string
  included: string
  total: string
  unit: string
}) {
  return (
    <p className="m-0 font-sans text-sm font-normal text-[#141414]">
      Included in {planName}: <span className="font-medium">{included}</span>
      {" · "}Your total:{" "}
      <span className="font-medium">
        {total} {unit}
      </span>
    </p>
  )
}

function formatCount(n: number): string {
  return Math.max(0, n).toLocaleString("en-GB")
}

export type CustomPlanCalculatorContinue = {
  quote: CustomPlanQuoteResult
  config: CustomPlanV2ConfigInput
}

/** Only a ready server quote can continue. Its hash travels with the config. */
function toContinuePayload(
  quote: CustomPlanQuoteResult,
): CustomPlanCalculatorContinue | null {
  const basePlan = chosenPlanToBasePlan(quote.basePlan)
  if (basePlan == null) {
    return null
  }
  return {
    quote,
    config: {
      version: 2,
      cadence: quote.cadence === "annual" ? "annual" : "monthly",
      basePlan,
      locations: quote.locations,
      packs: normalizePackCounts(quote.packs),
      quoteHash: quote.quoteHash,
    },
  }
}

export type CustomPlanCalculatorProps = {
  /** Preselected package (signup / restored intent). */
  initialBasePlan?: CustomPlanBasePlanKey | null
  initialLocations?: number
  initialPacks?: Partial<CustomPlanPackCounts>
  initialCadence?: BillingCadence
  headingTitle?: string
  headingBody?: string
  continueLabel?: string
  /** Marketing page passes an href builder; signup uses onContinue. */
  continueHrefFor?: (payload: CustomPlanCalculatorContinue) => string
  onContinue?: (payload: CustomPlanCalculatorContinue) => void
  className?: string
  summaryClassName?: string
}

export function CustomPlanCalculator({
  initialBasePlan,
  initialLocations = 1,
  initialPacks,
  initialCadence = "monthly",
  headingTitle,
  headingBody,
  continueLabel = "Continue",
  continueHrefFor,
  onContinue,
  className,
  summaryClassName,
}: CustomPlanCalculatorProps) {
  const [plans, setPlans] = useState<PublicCatalogPlanSummary[]>([])
  const [packMeta, setPackMeta] = useState<PublicCatalogPackMetaDto[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [locations, setLocations] = useState(() =>
    Math.max(1, initialLocations),
  )
  const [requestedBasePlan] = useState<CustomPlanBasePlanKey | null>(
    initialBasePlan ?? null,
  )
  const [packCounts, setPackCounts] = useState<CustomPlanPackCounts>(() => ({
    ...EMPTY_PACK_COUNTS,
    ...initialPacks,
  }))
  const [cadence, setCadence] = useState<BillingCadence>(initialCadence)

  useEffect(() => {
    void fetchPublicCustomUnitRates()
      .then((data) => {
        const loadedPlans = toPublicCatalogPlanSummaries(data.plans)
        if (loadedPlans.length === 0) {
          // Older API without plan rows: never show zero base allowances.
          setLoadError(
            "Plan allowances are not available right now. Please try again later.",
          )
        }
        setPlans(loadedPlans)
        setPackMeta(data.packs)
        // Opened from a plan card: start at that plan's included Locations so
        // the selected package shows first (Location count sets the base).
        if (initialBasePlan != null) {
          setLocations((current) => {
            if (minBasePlanForLocations(current, loadedPlans) === initialBasePlan) {
              return current
            }
            const row = loadedPlans.find((plan) => plan.planKey === initialBasePlan)
            return row != null ? Math.max(1, row.includedLocations) : current
          })
        }
      })
      .catch(() => {
        setLoadError("Plan rates are not available yet.")
      })
  }, [initialBasePlan])

  const { state, check, retry } = useServerCustomPlanQuote({
    cadence,
    basePlan: requestedBasePlan,
    locations,
    packs: packCounts,
  })
  // The last quote stays on screen while a newer one loads or fails.
  const quote = lastQuote(state)
  const view = quoteView({ check, state, changed: false })
  const contactSales = view.showContact
  const readyQuote = state.status === "ready" ? state.quote : null
  const continuePayload =
    readyQuote != null && view.continueEnabled
      ? toContinuePayload(readyQuote)
      : null

  function setPackCount(packKey: CustomPlanPackKey, value: number) {
    setPackCounts((prev) => ({
      ...prev,
      [packKey]: Math.max(0, Math.min(CUSTOM_PLAN_MAX_PACK_COUNT, value)),
    }))
  }

  function handleLocationsChange(next: number) {
    setLocations(next)
  }

  const steps = CUSTOM_PLAN_STEPS
  const shownBasePlan = quote != null ? chosenPlanToBasePlan(quote.basePlan) : requestedBasePlan
  const planDisplayName =
    quote != null
      ? quotePlanName(quote)
      : basePlanToChosenPlan(requestedBasePlan ?? "starter")
  const defaultHeadingTitle =
    headingTitle ?? `Make ${planDisplayName} work for your restaurants.`
  const defaultHeadingBody =
    headingBody ??
    `Your ${planDisplayName} allowances are already included. Add more capacity only where your restaurants need it.`

  const basePlanRow =
    shownBasePlan != null
      ? plans.find((plan) => plan.planKey === shownBasePlan)
      : undefined
  const baseIncluded = {
    teamUsers: basePlanRow?.includedTeamMembers ?? 0,
    offers: basePlanRow?.activeOffersAccount ?? 0,
    qrPerLocation: basePlanRow?.activeQrPlacementsPerLocation ?? 0,
    email: basePlanRow?.creditsMonthlyEmail ?? 0,
    sms: basePlanRow?.creditsMonthlySms ?? 0,
    ai: basePlanRow?.creditsMonthlyAi ?? 0,
  }
  const q = quote?.quantities ?? null
  /** Server totals; a dash while no quote has arrived. */
  function total(pick: (quantities: NonNullable<typeof q>) => number): string {
    return q != null ? formatCount(pick(q)) : "—"
  }

  /** Catalogue pack price (monthly, net pence); null when the catalogue has none. */
  function packMonthlyPrice(packKey: CustomPlanPackKey): number | null {
    return packMeta.find((meta) => meta.packKey === packKey)?.monthlyNetPence ?? null
  }

  const teamPack = CUSTOM_PLAN_PACKS.find((p) => p.packKey === "team_users_5")!
  const offerPack = CUSTOM_PLAN_PACKS.find((p) => p.packKey === "offers_5")!
  const qrPack = CUSTOM_PLAN_PACKS.find((p) => p.packKey === "qr_10")!
  const teamPrice = packMonthlyPrice("team_users_5")
  const offerPrice = packMonthlyPrice("offers_5")
  const qrPrice = packMonthlyPrice("qr_10")
  // QR cost comes from the ready quote's pack line only; no browser arithmetic.
  const qrExtraCost = quotePackLineAmount(readyQuote, "qr_10")

  const continueHref =
    !contactSales && continuePayload != null && continueHrefFor != null
      ? continueHrefFor(continuePayload)
      : undefined

  /** The Location message sits under the stepper while the count is invalid or over the limit. */
  const locationMessage =
    check !== "ok" || contactSales ? view.message : null
  /** Quote status sits in the summary card once the count is valid. */
  const summaryStatus = check === "ok" && !contactSales ? view.message : null

  return (
    <div
      className={cn(
        "mx-auto flex w-full max-w-[1548px] flex-col items-start justify-center gap-[46px] lg:flex-row",
        className,
      )}
    >
      <div className="relative isolate flex w-full max-w-[660px] flex-col gap-10 py-6 lg:gap-[60px]">
        <header className="flex flex-col gap-3">
          <p className="m-0 font-sans text-sm font-medium uppercase tracking-wide text-[#141414]">
            Customise your subscription
          </p>
          <h2 className="m-0 font-jakarta text-[28px] font-semibold leading-tight text-[#141414] lg:text-[34px]">
            {defaultHeadingTitle}
          </h2>
          <p className="m-0 font-sans text-base font-normal leading-[22px] text-[#141414]">
            {defaultHeadingBody}
          </p>
        </header>

        {loadError ? (
          <p className="m-0 text-sm text-red-600">{loadError}</p>
        ) : null}

        <section className="flex shrink-0 flex-col gap-8">
          <div className="flex flex-col gap-3">
            <div className="flex items-center gap-4">
              <StepBadge label={steps.locations.step} />
              <h3 className="m-0 font-jakarta text-xl font-semibold text-[#141414]">
                {steps.locations.title}
              </h3>
            </div>
            <p className="m-0 font-sans text-base font-normal leading-5 text-[#141414]">
              {steps.locations.body}
            </p>
          </div>
          <div className="flex items-center justify-between gap-4 rounded-xl border border-[#cacaca] p-[18px]">
            <div className="flex min-w-0 items-center gap-5">
              <img
                src={mapPinIcon}
                alt=""
                aria-hidden
                className="h-4 w-5 shrink-0 object-contain"
              />
              <p className="m-0 font-sans text-base font-medium text-[#141414]">
                {steps.locations.locationLabel}
              </p>
            </div>
            <CustomPlanStepper
              value={Math.min(locations, CUSTOM_PLAN_MAX_LOCATIONS + 1)}
              onChange={handleLocationsChange}
              min={1}
              max={CUSTOM_PLAN_MAX_LOCATIONS + 1}
              label={steps.locations.locationLabel}
            />
          </div>
          {locationMessage != null ? (
            <p role="status" className="m-0 font-sans text-sm font-normal text-[#141414]">
              {locationMessage}
            </p>
          ) : null}
        </section>

        {quote?.basePlanChanged === true && shownBasePlan != null ? (
          <div
            className="flex flex-col gap-2 rounded-xl border border-[#f0a35e] bg-[#fff4e8] px-5 py-4"
            role="status"
          >
            <p className="m-0 font-jakarta text-base font-semibold text-[#141414]">
              Your selected plan has changed
            </p>
            <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
              {planDisplayName} fits your {quote?.locations} restaurant
              Location
              {quote?.locations === 1 ? "" : "s"}
              {requestedBasePlan != null &&
              basePlanRank(shownBasePlan) > basePlanRank(requestedBasePlan)
                ? " and includes higher allowances for team users, active Offers and monthly credits."
                : ". Add packs below if you need more team users, active Offers, QR placements or monthly credits."}{" "}
              Your subscription price has been updated.
            </p>
          </div>
        ) : null}

        {contactSales ? (
          <div className="flex flex-col gap-4 rounded-xl border border-[#cacaca] bg-[#f4f4f4] p-6">
            <p className="m-0 font-sans text-sm text-[#141414]">
              {CUSTOM_PLAN_QUOTE_COPY.locationOverLimitHelp}
            </p>
            <MarketingButton asChild variant="dark" className="w-fit">
              <Link to={CUSTOM_PLAN_QUOTE_COPY.contactUsHref}>
                {CUSTOM_PLAN_QUOTE_COPY.contactUs}
              </Link>
            </MarketingButton>
          </div>
        ) : (
          <div className="flex flex-col gap-[60px]">
            <section className="flex flex-col gap-8">
              <div className="flex items-center gap-4">
                <StepBadge label={steps.capacity.step} />
                <h3 className="m-0 font-jakarta text-xl font-semibold text-[#141414]">
                  {steps.capacity.title}
                </h3>
              </div>

              <div className="flex flex-col overflow-hidden rounded-xl border border-[#cacaca]">
                <div className="flex flex-col gap-2.5 border-b border-[#cacaca] bg-[#e7f2eb] p-5 text-[#141414]">
                  <p className="m-0 font-jakarta text-base font-bold leading-4">
                    {steps.capacity.includedTitle}
                  </p>
                  <p className="m-0 font-jakarta text-sm font-normal leading-5">
                    {steps.capacity.includedBody}
                  </p>
                </div>
                <div className="flex flex-col gap-[18px] p-5">
                  <div className="flex items-center justify-between gap-4 rounded-xl border border-[#cacaca] p-[18px]">
                    <div className="flex min-w-0 flex-col gap-1.5">
                      <p className="m-0 font-sans text-base font-medium text-[#141414]">
                        {teamPack.title}
                      </p>
                      <p className="m-0 font-sans text-sm font-normal text-[#676767]">
                        {packHint(teamPrice, "users", teamPack.packSize)}
                      </p>
                      {basePlanRow != null ? (
                        <IncludedLine
                          planName={planDisplayName}
                          included={formatCount(baseIncluded.teamUsers)}
                          total={total((quantities) => quantities.teamUsers)}
                          unit="users"
                        />
                      ) : null}
                    </div>
                    <CustomPlanStepper
                      value={packCounts.team_users_5}
                      onChange={(n) => setPackCount("team_users_5", n)}
                      label="Additional team user packs"
                      min={0}
                      max={CUSTOM_PLAN_MAX_PACK_COUNT}
                    />
                  </div>
                  <div className="flex items-center justify-between gap-4 rounded-xl border border-[#cacaca] p-[18px]">
                    <div className="flex min-w-0 flex-col gap-1.5">
                      <p className="m-0 font-sans text-base font-medium text-[#141414]">
                        {offerPack.title}
                      </p>
                      <p className="m-0 font-sans text-sm font-normal text-[#676767]">
                        {packHint(offerPrice, "Offers", offerPack.packSize)}
                      </p>
                      {basePlanRow != null ? (
                        <IncludedLine
                          planName={planDisplayName}
                          included={formatCount(baseIncluded.offers)}
                          total={total((quantities) => quantities.offers)}
                          unit="active Offers"
                        />
                      ) : null}
                    </div>
                    <CustomPlanStepper
                      value={packCounts.offers_5}
                      onChange={(n) => setPackCount("offers_5", n)}
                      label="Additional active Offer packs"
                      min={0}
                      max={CUSTOM_PLAN_MAX_PACK_COUNT}
                    />
                  </div>
                  <div className="relative flex flex-col gap-[22px] overflow-hidden rounded-xl border border-[#cacaca] px-[18px] pt-[26px] pb-[18px]">
                    <div
                      aria-hidden
                      className="pointer-events-none absolute top-[-59px] left-1/2 h-[65px] w-[739px] -translate-x-1/2"
                    >
                      <img
                        src={qrPlacementsCardTop}
                        alt=""
                        className="absolute inset-0 size-full object-cover"
                        decoding="async"
                      />
                    </div>
                    <div className="relative z-[1] flex flex-col gap-1.5">
                      <p className="m-0 font-sans text-base font-medium text-[#141414]">
                        {steps.capacity.qrTitle}
                      </p>
                      <p className="m-0 max-w-[449px] font-sans text-sm font-normal leading-[18px] text-[#676767]">
                        {steps.capacity.qrBody}
                      </p>
                      {basePlanRow != null ? (
                        <IncludedLine
                          planName={planDisplayName}
                          included={`${formatCount(baseIncluded.qrPerLocation)} per Location`}
                          total={
                            q != null
                              ? `${formatCount(q.qrPlacements)} per Location${
                                  q.qrPlacementsAccountExtra > 0
                                    ? ` + ${formatCount(q.qrPlacementsAccountExtra)} shared`
                                    : ""
                                }`
                              : "—"
                          }
                          unit="placements"
                        />
                      ) : null}
                    </div>
                    <div className="relative z-[1] flex flex-col gap-4">
                      <div className="flex flex-col gap-[22px] rounded-xl border border-[#c9c9c9] bg-[#efefef] p-5">
                        <div className="flex flex-wrap items-center justify-between gap-3">
                          <div className="flex min-w-0 flex-col gap-1.5">
                            <p className="m-0 font-sans text-sm font-medium text-[#141414]">
                              {qrPack.title}
                            </p>
                            <p className="m-0 font-sans text-xs font-normal text-[#676767]">
                              {packHint(qrPrice, "placements", qrPack.packSize)}
                            </p>
                          </div>
                          <CustomPlanStepper
                            value={packCounts.qr_10}
                            onChange={(n) => setPackCount("qr_10", n)}
                            label="Extra QR placement packs"
                            min={0}
                            max={CUSTOM_PLAN_MAX_PACK_COUNT}
                          />
                        </div>
                        <hr className="m-0 border-[#c9c9c9]" />
                        <div className="flex items-center justify-between gap-2 text-[#141414]">
                          <p className="m-0 font-sans text-xs font-medium">
                            {steps.capacity.qrExtraCostLabel}
                          </p>
                          <p className="m-0 font-sans text-[22px] font-normal leading-none">
                            {qrExtraCost != null
                              ? `${formatPenceGbp(qrExtraCost.netPence)}${qrExtraCost.suffix}`
                              : "—"}
                          </p>
                        </div>
                      </div>
                      <p className="m-0 max-w-[405px] font-sans text-[10px] font-normal leading-3 text-[#141414]">
                        {steps.capacity.qrFootnote}
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </section>

            <section className="flex flex-col gap-8">
              <div className="flex flex-col gap-[18px]">
                <div className="flex items-center gap-4">
                  <StepBadge label={steps.credits.step} />
                  <h3 className="m-0 font-jakarta text-xl font-semibold text-[#141414]">
                    {steps.credits.title}
                  </h3>
                </div>
                <p className="m-0 max-w-[471px] font-jakarta text-sm font-normal leading-[18px] text-[#141414]">
                  {steps.credits.body}
                </p>
              </div>
              <div className="flex flex-col gap-[18px]">
                {steps.credits.channels.map((channel) => {
                  const packs = CUSTOM_PLAN_PACKS.filter(
                    (pack) => pack.channel === channel.channel,
                  )
                  const channelIncluded =
                    channel.channel === "email"
                      ? baseIncluded.email
                      : channel.channel === "sms"
                        ? baseIncluded.sms
                        : baseIncluded.ai
                  const channelTotal =
                    q == null
                      ? null
                      : channel.channel === "email"
                        ? q.emailCredits
                        : channel.channel === "sms"
                          ? q.smsCredits
                          : q.aiCredits
                  return (
                    <div
                      key={channel.channel}
                      className="flex flex-col overflow-hidden rounded-xl border border-[#cacaca]"
                    >
                      <div className="flex items-center justify-between gap-3 border-b border-[#cacaca] bg-[#e7f2eb] p-5">
                        <p className="m-0 flex-1 font-jakarta text-base font-bold leading-4 text-[#141414]">
                          {channel.title}
                        </p>
                        <div className="flex shrink-0 items-center gap-1 rounded-[30px] bg-[#d1dbd5] px-2 py-1.5">
                          <img
                            src={creditsTipIcon}
                            alt=""
                            aria-hidden
                            className="size-3 object-contain"
                          />
                          <p className="m-0 font-jakarta text-xs font-medium text-[#262626]">
                            {channel.tip}
                          </p>
                        </div>
                      </div>
                      <div className="flex flex-col gap-[18px] px-5 pt-5 pb-5">
                        {basePlanRow != null ? (
                          <IncludedLine
                            planName={planDisplayName}
                            included={`${formatCount(channelIncluded)}/month`}
                            total={
                              channelTotal != null
                                ? `${formatCount(channelTotal)}/month`
                                : "—"
                            }
                            unit="credits"
                          />
                        ) : null}
                        {packs.map((pack) => {
                          const packPrice = packMonthlyPrice(pack.packKey)
                          return (
                            <div
                              key={pack.packKey}
                              className="flex items-center justify-between gap-4 rounded-xl border border-[#cacaca] p-[18px]"
                            >
                              <div className="flex min-w-0 flex-col gap-1.5">
                                <p className="m-0 font-sans text-base font-medium text-[#141414]">
                                  {pack.title}
                                </p>
                                <p className="m-0 font-sans text-sm font-normal text-[#676767]">
                                  {packPrice != null
                                    ? `${formatPenceGbp(packPrice)}${steps.credits.packPriceSuffix}`
                                    : ""}
                                </p>
                              </div>
                              <CustomPlanStepper
                                value={packCounts[pack.packKey]}
                                onChange={(n) => setPackCount(pack.packKey, n)}
                                label={`${pack.title} packs`}
                                min={0}
                                max={CUSTOM_PLAN_MAX_PACK_COUNT}
                              />
                            </div>
                          )
                        })}
                      </div>
                    </div>
                  )
                })}
              </div>
            </section>
          </div>
        )}
      </div>

      {!contactSales && quote != null ? (
        <CustomPlanSummaryCard
          // Sticky only beside the long calculator column.
          className={cn("lg:sticky lg:top-0", summaryClassName)}
          quote={quote}
          cadence={cadence}
          onCadenceChange={setCadence}
          continueLabel={continueLabel}
          continueHref={continueHref}
          continueDisabled={!view.continueEnabled}
          onContinue={
            onContinue != null && continuePayload != null
              ? () => onContinue(continuePayload)
              : undefined
          }
          statusMessage={summaryStatus}
          showRetry={view.showRetry}
          onRetry={retry}
          taxText={view.taxText}
          planDescription={`Manage your restaurants with ${planDisplayName} features and shared allowances.`}
        />
      ) : null}
      {!contactSales && quote == null ? (
        <CustomPlanQuotePlaceholder
          className={cn("lg:sticky lg:top-0", summaryClassName)}
          loading={state.status === "loading"}
          message={view.message}
          showRetry={view.showRetry}
          onRetry={retry}
        />
      ) : null}
    </div>
  )
}

import { useEffect, useMemo, useState } from "react"
import { Link } from "react-router-dom"

import {
  fetchPublicCustomUnitRates,
  type AdminCommercialCustomUnitRate,
  type PublicExtraLocationSummaryDto,
} from "@/api/adminCommercialCatalogApi"
import qrPlacementsCardTop from "@/assets/images/marketing/custom-plan/qr-placements-card-top.png"
import mapPinIcon from "@/assets/svg/marketing/custom-plan/map-pin.svg"
import creditsTipIcon from "@/assets/svg/marketing/pricing/usage/credits-badge-icon.svg"
import { CustomPlanStepper } from "@/components/marketing/custom-plan/CustomPlanStepper"
import { CustomPlanSummaryCard } from "@/components/marketing/custom-plan/CustomPlanSummaryCard"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import { CUSTOM_PLAN_STEPS } from "@/content/marketing/customPlanPage"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  CUSTOM_PLAN_MAX_LOCATIONS,
  CUSTOM_PLAN_MAX_PACK_COUNT,
  CUSTOM_PLAN_PACKS,
  EMPTY_PACK_COUNTS,
  basePlanRank,
  estimateCustomPlanV2,
  minBasePlanForLocations,
  packMonthlyNetPence,
  type CustomPlanBasePlanKey,
  type CustomPlanPackCounts,
  type CustomPlanPackKey,
  type CustomPlanV2ConfigInput,
  type CustomPlanV2Estimate,
  type PublicCatalogPlanSummary,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import { formatPenceGbp } from "@/lib/marketingCustomPlan/customPlanQuoteState"
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

function packHint(
  monthlyPackPence: number,
  unitLabel: string,
  packSize: number,
  fallback: string,
): string {
  if (monthlyPackPence <= 0) {
    return fallback
  }
  return `${packSize.toLocaleString("en-GB")} ${unitLabel} per pack · ${formatPenceGbp(monthlyPackPence)}/month`
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
  estimate: CustomPlanV2Estimate
  config: CustomPlanV2ConfigInput
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
  const [rates, setRates] = useState<AdminCommercialCustomUnitRate[]>([])
  const [plans, setPlans] = useState<PublicCatalogPlanSummary[]>([])
  const [extraLocation, setExtraLocation] =
    useState<PublicExtraLocationSummaryDto | null>(null)
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
        setRates(data.unitRates)
        setPlans(loadedPlans)
        setExtraLocation(data.extraLocation)
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

  const estimate = useMemo(
    () =>
      estimateCustomPlanV2({
        cadence,
        requestedBasePlan,
        locations,
        packs: packCounts,
        plans,
        rates,
        extraLocation,
      }),
    [
      cadence,
      requestedBasePlan,
      locations,
      packCounts,
      plans,
      rates,
      extraLocation,
    ],
  )

  const continuePayload = useMemo<CustomPlanCalculatorContinue>(
    () => ({
      estimate,
      config: {
        version: 2,
        cadence,
        basePlan: estimate.basePlan,
        locations: estimate.locations,
        packs: estimate.packs,
      },
    }),
    [estimate, cadence],
  )

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
  const contactSales = locations > CUSTOM_PLAN_MAX_LOCATIONS
  const planDisplayName =
    estimate.basePlan.charAt(0).toUpperCase() + estimate.basePlan.slice(1)
  const defaultHeadingTitle =
    headingTitle ?? `Make ${planDisplayName} work for your restaurants.`
  const defaultHeadingBody =
    headingBody ??
    `Your ${planDisplayName} allowances are already included. Add more capacity only where your restaurants need it.`

  const catalogReady = plans.length > 0 && rates.length > 0
  const basePlanRow = plans.find((plan) => plan.planKey === estimate.basePlan)
  const baseIncluded = {
    teamUsers: basePlanRow?.includedTeamMembers ?? 0,
    offers: basePlanRow?.activeOffersAccount ?? 0,
    qrPerLocation: basePlanRow?.activeQrPlacementsPerLocation ?? 0,
    email: basePlanRow?.creditsMonthlyEmail ?? 0,
    sms: basePlanRow?.creditsMonthlySms ?? 0,
    ai: basePlanRow?.creditsMonthlyAi ?? 0,
  }
  const q = estimate.quantities

  const teamPack = CUSTOM_PLAN_PACKS.find((p) => p.packKey === "team_users_5")!
  const offerPack = CUSTOM_PLAN_PACKS.find((p) => p.packKey === "offers_5")!
  const qrPack = CUSTOM_PLAN_PACKS.find((p) => p.packKey === "qr_10")!
  const teamPrice = packMonthlyNetPence(teamPack, rates)
  const offerPrice = packMonthlyNetPence(offerPack, rates)
  const qrPrice = packMonthlyNetPence(qrPack, rates)
  const qrExtraNetPence = qrPrice * packCounts.qr_10

  const continueHref =
    !contactSales && catalogReady && continueHrefFor != null
      ? continueHrefFor(continuePayload)
      : undefined

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
        </section>

        {estimate.basePlanChanged ? (
          <div
            className="flex flex-col gap-2 rounded-xl border border-[#f0a35e] bg-[#fff4e8] px-5 py-4"
            role="status"
          >
            <p className="m-0 font-jakarta text-base font-semibold text-[#141414]">
              Your selected plan has changed
            </p>
            <p className="m-0 font-sans text-sm font-normal leading-5 text-[#141414]">
              {planDisplayName} fits your {estimate.locations} restaurant
              Location
              {estimate.locations === 1 ? "" : "s"}
              {requestedBasePlan != null &&
              basePlanRank(estimate.basePlan) > basePlanRank(requestedBasePlan)
                ? " and includes higher allowances for team users, active Offers and monthly credits."
                : ". Add packs below if you need more team users, active Offers, QR placements or monthly credits."}{" "}
              Your subscription price has been updated.
            </p>
          </div>
        ) : null}

        {contactSales ? (
          <div className="flex flex-col gap-4 rounded-xl border border-[#cacaca] bg-[#f4f4f4] p-6">
            <p className="m-0 font-jakarta text-lg font-semibold text-[#141414]">
              Need more than {CUSTOM_PLAN_MAX_LOCATIONS} Locations?
            </p>
            <p className="m-0 font-sans text-sm text-[#141414]">
              Contact us for a tailored plan. Self-serve checkout supports up to{" "}
              {CUSTOM_PLAN_MAX_LOCATIONS} Locations.
            </p>
            <MarketingButton asChild variant="dark" className="w-fit">
              <Link to={HELP_CENTRE_CONTACT_URL}>Contact us</Link>
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
                        {packHint(
                          teamPrice,
                          "users",
                          teamPack.packSize,
                          steps.capacity.teamUsersHintFallback,
                        )}
                      </p>
                      {basePlanRow != null ? (
                        <IncludedLine
                          planName={planDisplayName}
                          included={formatCount(baseIncluded.teamUsers)}
                          total={formatCount(q.teamUsers)}
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
                        {packHint(
                          offerPrice,
                          "Offers",
                          offerPack.packSize,
                          steps.capacity.offersHintFallback,
                        )}
                      </p>
                      {basePlanRow != null ? (
                        <IncludedLine
                          planName={planDisplayName}
                          included={formatCount(baseIncluded.offers)}
                          total={formatCount(q.offers)}
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
                          total={`${formatCount(q.qrPlacements)} per Location${
                            q.qrPlacementsAccountExtra > 0
                              ? ` + ${formatCount(q.qrPlacementsAccountExtra)} shared`
                              : ""
                          }`}
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
                              {packHint(
                                qrPrice,
                                "placements",
                                qrPack.packSize,
                                steps.capacity.qrPackHintFallback,
                              )}
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
                            {formatPenceGbp(qrExtraNetPence)}/mo
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
                    channel.channel === "email"
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
                            total={`${formatCount(channelTotal)}/month`}
                            unit="credits"
                          />
                        ) : null}
                        {packs.map((pack) => {
                          const packPrice = packMonthlyNetPence(pack, rates)
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
                                  {formatPenceGbp(packPrice)}
                                  {steps.credits.packPriceSuffix}
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

      {!contactSales ? (
        <CustomPlanSummaryCard
          // Sticky only beside the long calculator column.
          className={cn("lg:sticky lg:top-0", summaryClassName)}
          estimate={estimate}
          cadence={cadence}
          onCadenceChange={setCadence}
          continueLabel={continueLabel}
          continueHref={continueHref}
          continueDisabled={!catalogReady}
          onContinue={
            onContinue != null
              ? () => onContinue(continuePayload)
              : undefined
          }
          planDescription={`Manage your restaurants with ${planDisplayName} features and shared allowances.`}
        />
      ) : null}
    </div>
  )
}

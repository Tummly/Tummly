import { useEffect, useMemo, useState } from "react"
import { Link } from "react-router-dom"
import { Minus, Plus } from "lucide-react"

import {
  fetchPublicCustomUnitRates,
  type AdminCommercialCustomUnitRate,
} from "@/api/adminCommercialCatalogApi"
import emptyStateIllustration from "@/assets/images/marketing/custom-plan/empty-state-illustration.png"
import qrPlacementsCardTop from "@/assets/images/marketing/custom-plan/qr-placements-card-top.png"
import mapPinIcon from "@/assets/svg/marketing/custom-plan/map-pin.svg"
import creditsTipIcon from "@/assets/svg/marketing/pricing/usage/credits-badge-icon.svg"
import Footer from "@/components/home/Footer"
import { CustomPlanFaqs } from "@/components/marketing/custom-plan/CustomPlanFaqs"
import { MarketingButton } from "@/components/marketing/MarketingButton"
import {
  CUSTOM_PLAN_HERO,
  CUSTOM_PLAN_SECTION_INSET,
  CUSTOM_PLAN_SECTION_Y,
  CUSTOM_PLAN_STEPS,
} from "@/content/marketing/customPlanPage"
import { PRICING_CADENCE_COPY } from "@/content/marketing/pricingPage"
import {
  creditPackMonthlyNetPence,
  creditsFromPackCounts,
  CUSTOM_PLAN_CREDIT_PACKS,
  CUSTOM_PLAN_MAX_LOCATIONS,
  EMPTY_CREDIT_PACK_COUNTS,
  EMPTY_CUSTOM_PLAN_QUANTITIES,
  encodeCustomPlanIntent,
  estimateCustomPlanTotalPence,
  formatPenceGbp,
  lineNetPence,
  type CustomPlanCreditPackCounts,
  type CustomPlanQuantities,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { buildSignupPath } from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

function Stepper({
  value,
  onChange,
  min,
  max,
  disabled,
}: {
  value: number
  onChange: (n: number) => void
  min: number
  max: number
  disabled?: boolean
}) {
  return (
    <div className="flex shrink-0 items-center overflow-hidden rounded-lg border border-[#e5e5e5] bg-white">
      <button
        type="button"
        className="border-r border-[#e5e5e5] px-2.5 py-1.5 disabled:opacity-40"
        disabled={disabled || value <= min}
        onClick={() => onChange(Math.max(min, value - 1))}
        aria-label="Decrease"
      >
        <Minus className="size-4" />
      </button>
      <span className="flex w-[61px] items-center justify-center px-4 py-1.5 text-center font-sans text-sm text-black">
        {value.toLocaleString("en-GB")}
      </span>
      <button
        type="button"
        className="border-l border-[#e5e5e5] px-2.5 py-1.5 disabled:opacity-40"
        disabled={disabled || value >= max}
        onClick={() => onChange(Math.min(max, value + 1))}
        aria-label="Increase"
      >
        <Plus className="size-4" />
      </button>
    </div>
  )
}

function StepBadge({ label }: { label: string }) {
  return (
    <span className="flex size-11 shrink-0 items-center justify-center rounded-full border border-[#e0e5eb] font-jakarta text-base font-semibold text-[#141414]">
      {label}
    </span>
  )
}

/** Stepper in pack units; stores natural quantity (= packs × unitQuantity). */
function PackAllowanceRow({
  title,
  hint,
  naturalValue,
  unitQuantity,
  onNaturalChange,
  maxPacks,
  disabled,
}: {
  title: string
  hint: string
  naturalValue: number
  unitQuantity: number
  onNaturalChange: (n: number) => void
  maxPacks: number
  disabled?: boolean
}) {
  const unit = Math.max(1, unitQuantity)
  const packs = Math.floor(naturalValue / unit)
  return (
    <div className="flex items-center justify-between gap-4 rounded-xl border border-[#cacaca] p-[18px]">
      <div className="flex min-w-0 flex-col gap-1.5">
        <p className="m-0 font-sans text-base font-medium text-[#141414]">
          {title}
        </p>
        <p className="m-0 font-sans text-sm font-normal text-[#676767]">
          {hint}
        </p>
      </div>
      <Stepper
        value={packs}
        onChange={(nextPacks) => onNaturalChange(nextPacks * unit)}
        min={0}
        max={maxPacks}
        disabled={disabled}
      />
    </div>
  )
}

function rateByKey(
  rates: AdminCommercialCustomUnitRate[],
  rateKey: string,
): AdminCommercialCustomUnitRate | undefined {
  return rates.find((rate) => rate.rateKey === rateKey)
}

function packHint(
  rate: AdminCommercialCustomUnitRate | undefined,
  unitLabel: string,
  fallback: string,
): string {
  if (rate == null || rate.unitQuantity <= 0) {
    return fallback
  }
  return `${rate.unitQuantity.toLocaleString("en-GB")} ${unitLabel} per pack · ${formatPenceGbp(rate.monthlyNetPence)}/month`
}

function formatCount(n: number): string {
  return n.toLocaleString("en-GB")
}

/** Figma custom-plan empty `6657:37873` + full calculator `6657:37518` + FAQs `6661:39170`. */
export default function CustomPlanPage() {
  const [rates, setRates] = useState<AdminCommercialCustomUnitRate[]>([])
  const [catalogPublicId, setCatalogPublicId] = useState("")
  const [loadError, setLoadError] = useState<string | null>(null)
  const [capacity, setCapacity] = useState({
    locations: EMPTY_CUSTOM_PLAN_QUANTITIES.locations,
    teamUsers: EMPTY_CUSTOM_PLAN_QUANTITIES.teamUsers,
    qrPlacements: EMPTY_CUSTOM_PLAN_QUANTITIES.qrPlacements,
    offers: EMPTY_CUSTOM_PLAN_QUANTITIES.offers,
  })
  const [packCounts, setPackCounts] = useState<CustomPlanCreditPackCounts>(
    EMPTY_CREDIT_PACK_COUNTS,
  )
  const [cadence, setCadence] = useState<BillingCadence>("monthly")

  useEffect(() => {
    void fetchPublicCustomUnitRates()
      .then((data) => {
        setRates(data.unitRates)
        setCatalogPublicId(data.catalogPublicId)
      })
      .catch(() => {
        setLoadError("Custom plan rates are not available yet.")
      })
  }, [])

  const creditTotals = useMemo(
    () => creditsFromPackCounts(packCounts),
    [packCounts],
  )

  const quantities: CustomPlanQuantities = useMemo(
    () => ({
      ...capacity,
      ...creditTotals,
    }),
    [capacity, creditTotals],
  )

  const locked = quantities.locations < 1
  const estimate = useMemo(
    () => estimateCustomPlanTotalPence(rates, quantities, cadence),
    [rates, quantities, cadence],
  )

  const qrRate = rateByKey(rates, "qr_placement")
  const teamRate = rateByKey(rates, "team_user")
  const offerRate = rateByKey(rates, "offer")
  const qrExtraNetPence =
    qrRate == null
      ? 0
      : lineNetPence(qrRate, quantities.qrPlacements, "monthly")

  function setCapacityQty<K extends keyof typeof capacity>(
    key: K,
    value: number,
  ) {
    setCapacity((prev) => ({ ...prev, [key]: value }))
  }

  function setPackCount(packId: string, value: number) {
    setPackCounts((prev) => ({ ...prev, [packId]: value }))
  }

  const continueHref = useMemo(() => {
    if (locked || !catalogPublicId) {
      return "#"
    }
    const intent = encodeCustomPlanIntent(
      quantities,
      cadence,
      catalogPublicId,
      rates,
    )
    const base = buildSignupPath({ plan: "Custom", cadence })
    const join = base.includes("?") ? "&" : "?"
    return `${base}${join}customIntent=${encodeURIComponent(intent)}`
  }, [locked, catalogPublicId, quantities, cadence, rates])

  const steps = CUSTOM_PLAN_STEPS
  const totalSuffix =
    cadence === "annual"
      ? steps.estimate.annualSuffix
      : steps.estimate.monthlySuffix

  return (
    <>
      <main
        className={cn(
          "flex w-full flex-col items-center overflow-hidden rounded-tl-[12px] rounded-tr-[12px] bg-white",
          CUSTOM_PLAN_SECTION_INSET,
          CUSTOM_PLAN_SECTION_Y,
          "gap-[70px]",
        )}
      >
        <header className="flex w-full max-w-[656px] flex-col items-center gap-[22px] text-center">
          <div className="flex w-full flex-col items-center gap-3.5 text-black">
            <p className="m-0 font-sans text-sm font-medium uppercase leading-normal">
              {CUSTOM_PLAN_HERO.eyebrow}
            </p>
            <h1 className="m-0 font-jakarta text-[34px] font-[500] leading-[40px] lg:text-[46px] lg:leading-[52px]">
              {CUSTOM_PLAN_HERO.title}
            </h1>
          </div>
          <p className="m-0 max-w-[596px] font-sans text-base font-normal leading-[22px] text-[#141414] lg:text-lg lg:leading-[26px]">
            {CUSTOM_PLAN_HERO.body}
          </p>
        </header>

        {loadError ? (
          <p className="text-center text-sm text-red-600">{loadError}</p>
        ) : null}

        <div className="flex w-full max-w-[1548px] flex-col items-start justify-center gap-[46px] lg:flex-row">
          <div
            className={cn(
              "relative isolate flex w-full max-w-[660px] flex-col gap-10 py-6 lg:gap-[60px]",
              locked && "h-[775px] overflow-hidden",
            )}
          >
            <section className="flex shrink-0 flex-col gap-8">
              <div className="flex flex-col gap-3">
                <div className="flex items-center gap-4">
                  <StepBadge label={steps.locations.step} />
                  <h2 className="m-0 font-jakarta text-xl font-semibold text-[#141414]">
                    {steps.locations.title}
                  </h2>
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
                <Stepper
                  value={quantities.locations}
                  onChange={(n) => setCapacityQty("locations", n)}
                  min={0}
                  max={CUSTOM_PLAN_MAX_LOCATIONS}
                />
              </div>
            </section>

            <div
              className={cn(
                "relative min-h-0 flex-1",
                locked && "pointer-events-none select-none",
              )}
            >
              <div
                className={cn(
                  "flex flex-col gap-[60px]",
                  locked && "blur-[1.5px]",
                )}
                aria-disabled={locked}
              >
                <section className="flex flex-col gap-8">
                  <div className="flex items-center gap-4">
                    <StepBadge label={steps.capacity.step} />
                    <h2 className="m-0 font-jakarta text-xl font-semibold text-[#141414]">
                      {steps.capacity.title}
                    </h2>
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
                      <PackAllowanceRow
                        title={steps.capacity.teamUsersTitle}
                        hint={packHint(
                          teamRate,
                          "users",
                          steps.capacity.teamUsersHintFallback,
                        )}
                        naturalValue={quantities.teamUsers}
                        unitQuantity={teamRate?.unitQuantity ?? 5}
                        onNaturalChange={(n) => setCapacityQty("teamUsers", n)}
                        maxPacks={1000}
                        disabled={locked}
                      />
                      <PackAllowanceRow
                        title={steps.capacity.offersTitle}
                        hint={packHint(
                          offerRate,
                          "Offers",
                          steps.capacity.offersHintFallback,
                        )}
                        naturalValue={quantities.offers}
                        unitQuantity={offerRate?.unitQuantity ?? 5}
                        onNaturalChange={(n) => setCapacityQty("offers", n)}
                        maxPacks={1000}
                        disabled={locked}
                      />
                      <div className="relative flex flex-col gap-[22px] overflow-hidden rounded-xl border border-[#cacaca] px-[18px] pt-[26px] pb-[18px]">
                        {/* Figma `6699:13689` — green organic fill clipped to the card top. */}
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
                        </div>
                        <div className="relative z-[1] flex flex-col gap-4">
                          <div className="flex flex-col gap-[22px] rounded-xl border border-[#c9c9c9] bg-[#efefef] p-5">
                            <div className="flex flex-wrap items-center justify-between gap-3">
                              <div className="flex min-w-0 flex-col gap-1.5">
                                <p className="m-0 font-sans text-sm font-medium text-[#141414]">
                                  {steps.capacity.qrPackTitle}
                                </p>
                                <p className="m-0 font-sans text-xs font-normal text-[#676767]">
                                  {packHint(
                                    qrRate,
                                    "placements",
                                    steps.capacity.qrPackHintFallback,
                                  )}
                                </p>
                              </div>
                              <Stepper
                                value={Math.floor(
                                  quantities.qrPlacements /
                                    Math.max(1, qrRate?.unitQuantity ?? 10),
                                )}
                                onChange={(packs) =>
                                  setCapacityQty(
                                    "qrPlacements",
                                    packs *
                                      Math.max(1, qrRate?.unitQuantity ?? 10),
                                  )
                                }
                                min={0}
                                max={5000}
                                disabled={locked}
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
                      <h2 className="m-0 font-jakarta text-xl font-semibold text-[#141414]">
                        {steps.credits.title}
                      </h2>
                    </div>
                    <p className="m-0 max-w-[471px] font-jakarta text-sm font-normal leading-[18px] text-[#141414]">
                      {steps.credits.body}
                    </p>
                  </div>
                  <div className="flex flex-col gap-[18px]">
                    {steps.credits.channels.map((channel) => {
                      const packs = CUSTOM_PLAN_CREDIT_PACKS.filter(
                        (pack) => pack.channel === channel.channel,
                      )
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
                            {packs.map((pack) => {
                              const packPrice = creditPackMonthlyNetPence(
                                pack,
                                rates,
                              )
                              return (
                                <div
                                  key={pack.id}
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
                                  <Stepper
                                    value={packCounts[pack.id] ?? 0}
                                    onChange={(n) => setPackCount(pack.id, n)}
                                    min={0}
                                    max={500}
                                    disabled={locked}
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

              {locked ? (
                <div
                  className="pointer-events-none absolute inset-0 z-50"
                  aria-live="polite"
                >
                  <div
                    aria-hidden
                    className="absolute inset-x-0 bottom-0 h-full bg-gradient-to-b from-transparent from-[48%] to-white to-[76%]"
                  />
                  <div className="absolute top-1/2 left-1/2 z-50 flex -translate-x-1/2 -translate-y-1/2 items-center justify-center rounded-[30px] border border-[#e5e5e5] bg-white px-5 py-[15px] shadow-[0_6px_6.5px_rgba(0,0,0,0.05),0_23px_11.5px_rgba(0,0,0,0.05),0_53px_16px_rgba(0,0,0,0.03)]">
                    <p className="m-0 whitespace-nowrap font-sans text-base font-normal leading-normal text-black">
                      {steps.capacity.lockHint}
                    </p>
                  </div>
                </div>
              ) : null}
            </div>
          </div>

          {locked ? (
            <aside className="w-full max-w-[610px] overflow-hidden rounded-xl">
              <img
                src={emptyStateIllustration}
                alt={steps.estimate.emptyIllustrationAlt}
                width={610}
                height={660}
                className="h-auto w-full object-cover"
                decoding="async"
              />
            </aside>
          ) : (
            <aside className="sticky top-0 flex w-full max-w-[581px] flex-col gap-[26px] pt-6">
              <div
                className="flex w-full items-center gap-2.5 rounded-[10px] border border-[rgba(38,38,38,0.21)] bg-[#f4f4f4] p-2"
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
                  onClick={() => setCadence("monthly")}
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
                  onClick={() => setCadence("annual")}
                >
                  {PRICING_CADENCE_COPY.annual}
                  <span className="rounded-full bg-[#14a74a] px-2.5 py-2 text-[10px] font-medium leading-none text-white">
                    {PRICING_CADENCE_COPY.saveBadge}
                  </span>
                </button>
              </div>

              <div className="flex w-full flex-col gap-[26px]">
                <div className="overflow-hidden rounded-xl">
                  <div className="flex flex-col gap-8 rounded-t-lg bg-[#dfebe3] p-7">
                    <div className="flex items-center justify-between gap-3">
                      <p className="m-0 font-jakarta text-base font-medium uppercase text-black">
                        {steps.estimate.heading}
                      </p>
                      <span className="rounded-[20px] bg-[#b6d1bf] px-3 py-1.5 font-jakarta text-xs font-semibold text-black">
                        {steps.estimate.recommendedBadge}
                      </span>
                    </div>
                    <div className="flex flex-col gap-2 text-black">
                      <p className="m-0 font-jakarta text-[34px] font-bold leading-none">
                        {steps.estimate.planName}
                      </p>
                      <p className="m-0 max-w-[379px] font-jakarta text-sm font-normal">
                        {steps.estimate.planDescription}
                      </p>
                    </div>
                    <div className="flex flex-col gap-3.5">
                      <div className="flex flex-wrap gap-x-10 gap-y-3 font-jakarta text-xs font-semibold">
                        <div className="flex flex-col gap-1.5">
                          <span className="text-[#707070]">
                            {steps.estimate.locationsLabel}
                          </span>
                          <span className="text-black">
                            {formatCount(quantities.locations)}
                          </span>
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <span className="text-[#707070]">
                            {steps.estimate.teamUsersLabel}
                          </span>
                          <span className="text-black">
                            {formatCount(quantities.teamUsers)}
                          </span>
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <span className="text-[#707070]">
                            {steps.estimate.activeQrLabel}
                          </span>
                          <span className="text-black">
                            {formatCount(quantities.qrPlacements)}
                            {steps.estimate.activeQrSuffix}
                          </span>
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <span className="text-[#707070]">
                            {steps.estimate.activeOffersLabel}
                          </span>
                          <span className="text-black">
                            {formatCount(quantities.offers)}
                            {steps.estimate.activeOffersSuffix}
                          </span>
                        </div>
                      </div>
                      <hr className="m-0 border-[#c9c9c9]" />
                      <div className="flex flex-wrap gap-x-10 gap-y-3 font-jakarta text-xs font-semibold">
                        <div className="flex flex-col gap-1.5">
                          <span className="text-[#707070]">
                            {steps.estimate.emailLabel}
                          </span>
                          <span className="text-black">
                            {formatCount(quantities.emailCredits)}
                            {steps.estimate.perMonthSuffix}
                          </span>
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <span className="text-[#707070]">
                            {steps.estimate.smsLabel}
                          </span>
                          <span className="text-black">
                            {formatCount(quantities.smsCredits)}
                            {steps.estimate.perMonthSuffix}
                          </span>
                        </div>
                        <div className="flex flex-col gap-1.5">
                          <span className="text-[#707070]">
                            {steps.estimate.aiLabel}
                          </span>
                          <span className="text-black">
                            {formatCount(quantities.aiCredits)}
                            {steps.estimate.perMonthSuffix}
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
                            key={line.rateKey}
                            className="flex items-center justify-between gap-3"
                          >
                            <span className="leading-[18px]">
                              {line.label} · {formatCount(line.quantity)}
                            </span>
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
                      <p className="m-0 font-sans text-lg text-black">
                        {steps.estimate.estimatedTotalLabel}
                      </p>
                      <p className="m-0 text-[#141414]">
                        <span className="font-sans text-[36px] font-normal leading-none">
                          {formatPenceGbp(estimate.displayNetPence)}
                        </span>
                        <span className="font-sans text-sm font-normal">
                          {totalSuffix}
                        </span>
                      </p>
                    </div>
                    <div className="flex w-full flex-col gap-[22px] sm:flex-row sm:items-stretch">
                      <MarketingButton
                        asChild
                        variant="dark"
                        className="w-full flex-1"
                      >
                        <Link to={continueHref}>
                          {steps.estimate.continueLabel}
                        </Link>
                      </MarketingButton>
                      <MarketingButton
                        asChild
                        variant="secondary"
                        className="w-full flex-1"
                      >
                        <Link to={steps.estimate.contactTo}>
                          {steps.estimate.contactLabel}
                        </Link>
                      </MarketingButton>
                    </div>
                  </div>
                </div>

                <p className="m-0 font-sans text-sm font-normal leading-5 text-[#676767]">
                  {steps.estimate.checkoutNote}
                </p>
              </div>
            </aside>
          )}
        </div>
      </main>
      <CustomPlanFaqs />
      <Footer />
    </>
  )
}

import { useEffect, useMemo, useState } from "react"

import {
  fetchPublicCustomUnitRates,
  quotePublicCustomPlan,
  type CustomPlanQuoteResult,
} from "@/api/adminCommercialCatalogApi"
import { GuestLoopShellFooter } from "@/components/guest-loop/GuestLoopShell"
import { CustomPlanSummaryCard } from "@/components/marketing/custom-plan/CustomPlanSummaryCard"
import { Button } from "@/components/ui/button"
import {
  chosenPlanToBasePlan,
  EMPTY_PACK_COUNTS,
  estimateCustomPlanV2,
  normalizePackCounts,
  type CustomPlanBasePlanKey,
  type CustomPlanV2Estimate,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import { toPublicCatalogPlanSummaries } from "@/lib/marketingCustomPlan/publicCatalogPlans"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  activeCustomConfig,
  readSignupPlanIntent,
  saveSignupCustomPlanConfig,
  saveSignupPlanIntent,
} from "@/lib/signupPlanIntent"

type SignupReviewPlanStepProps = {
  onBack: () => void
  onConfirm: () => void | Promise<void>
  isSubmitting?: boolean
}

function quoteToEstimate(
  quote: CustomPlanQuoteResult,
  requestedBasePlan: CustomPlanBasePlanKey | null,
): CustomPlanV2Estimate {
  const basePlan =
    chosenPlanToBasePlan(quote.basePlan) ?? requestedBasePlan ?? "starter"
  return {
    contactSales: false,
    basePlan,
    requestedBasePlan,
    basePlanChanged: quote.basePlanChanged,
    locations: quote.locations,
    additionalLocations: 0,
    packs: normalizePackCounts(quote.packs),
    quantities: {
      locations: quote.quantities.locations,
      teamUsers: quote.quantities.teamUsers,
      qrPlacements: quote.quantities.qrPlacements,
      qrPlacementsAccountExtra: quote.quantities.qrPlacementsAccountExtra,
      offers: quote.quantities.offers,
      emailCredits: quote.quantities.emailCredits,
      smsCredits: quote.quantities.smsCredits,
      aiCredits: quote.quantities.aiCredits,
    },
    monthlyNetPence: quote.monthlyNetPence,
    periodNetPence: quote.periodNetPence,
    lines: quote.lines.map((line) => ({
      key: line.key,
      label: line.label,
      quantity: line.quantity,
      netPence: line.netPence,
    })),
    storedAsPlan:
      quote.storedAsPlan === "Starter" ||
      quote.storedAsPlan === "Growth" ||
      quote.storedAsPlan === "Group"
        ? quote.storedAsPlan
        : "Custom",
  }
}

/** Review reuses the calculator summary card (no separate Figma frame). */
export function SignupReviewPlanStep({
  onBack,
  onConfirm,
  isSubmitting = false,
}: SignupReviewPlanStepProps) {
  // Read once: a fresh parse each render would change `packs` identity and
  // re-run the quote effect in a loop.
  const [intent] = useState(() => readSignupPlanIntent())
  const config = activeCustomConfig(intent)
  const cadence: BillingCadence =
    config?.cadence ?? intent?.cadence ?? "monthly"
  const basePlan: CustomPlanBasePlanKey =
    config?.basePlan ?? chosenPlanToBasePlan(intent?.plan) ?? "starter"
  const [packs] = useState(() => config?.packs ?? EMPTY_PACK_COUNTS)
  // Plain card checkout: quote at the plan's included Locations so the
  // Location-count rule keeps the selected package.
  const [plainLocations, setPlainLocations] = useState<number | null>(null)
  const locations = config?.locations ?? plainLocations

  const [estimate, setEstimate] = useState<CustomPlanV2Estimate | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [cadenceState, setCadenceState] = useState<BillingCadence>(cadence)

  useEffect(() => {
    if (config != null) return
    let cancelled = false
    void fetchPublicCustomUnitRates()
      .then((catalog) => {
        if (cancelled) return
        const row = catalog.plans.find((plan) => plan.planKey === basePlan)
        setPlainLocations(Math.max(1, row?.includedLocations ?? 1))
      })
      .catch(() => {
        if (!cancelled) setPlainLocations(1)
      })
    return () => {
      cancelled = true
    }
  }, [config, basePlan])

  const requestBody = useMemo(
    () => ({
      cadence: cadenceState,
      basePlan,
      locations: locations ?? 1,
      packs: Object.fromEntries(
        Object.entries(normalizePackCounts(packs)).filter(([, n]) => n > 0),
      ),
    }),
    [cadenceState, basePlan, locations, packs],
  )

  useEffect(() => {
    if (locations == null) return
    let cancelled = false

    void (async () => {
      try {
        const quote = await quotePublicCustomPlan(requestBody)
        if (cancelled) return
        const next = quoteToEstimate(quote, basePlan)
        setEstimate(next)
        setError(null)
        saveSignupCustomPlanConfig({
          basePlan: next.basePlan,
          cadence: cadenceState,
          locations: next.locations,
          packs: next.packs,
          asCustom: next.storedAsPlan === "Custom",
        })
      } catch {
        // Server quote may not be live yet — fall back to client estimate.
        try {
          const catalog = await fetchPublicCustomUnitRates()
          if (cancelled) return
          const local = estimateCustomPlanV2({
            cadence: cadenceState,
            requestedBasePlan: basePlan,
            locations: locations ?? 1,
            packs,
            plans: toPublicCatalogPlanSummaries(catalog.plans),
            rates: catalog.unitRates,
            extraLocation: catalog.extraLocation,
          })
          setEstimate(local)
          setError(null)
          saveSignupCustomPlanConfig({
            basePlan: local.basePlan,
            cadence: cadenceState,
            locations: local.locations,
            packs: local.packs,
            asCustom: local.storedAsPlan === "Custom",
          })
        } catch {
          if (!cancelled) {
            setEstimate(null)
            setError("Could not load your plan price. Please try again.")
          }
        }
      }
    })()

    return () => {
      cancelled = true
    }
  }, [requestBody, basePlan, cadenceState, locations, packs])

  function handleCadenceChange(next: BillingCadence) {
    setCadenceState(next)
    const current = readSignupPlanIntent()
    if (current?.plan === "Pilot") return
    saveSignupPlanIntent({
      ...current,
      plan: current?.plan ?? "Custom",
      cadence: next,
      config:
        current?.config != null
          ? { ...current.config, cadence: next }
          : {
              version: 2,
              cadence: next,
              basePlan,
              locations: locations ?? 1,
              packs,
            },
    })
  }

  return (
    <div className="mx-auto flex w-full max-w-[581px] flex-col gap-10">
      <header className="flex flex-col gap-3 text-[#141414]">
        <h1 className="m-0 font-jakarta text-[36px] font-medium leading-normal">
          Review your subscription
        </h1>
        <p className="m-0 max-w-[640px] font-sans text-base leading-[22px]">
          Confirm your plan, included allowances, additional capacity, billing
          period and price before you continue to payment.
        </p>
      </header>

      {error != null ? (
        <p className="m-0 text-sm text-red-600">{error}</p>
      ) : null}

      {estimate == null && error == null ? (
        <p className="m-0 font-sans text-sm text-[#676767]">
          Calculating your price…
        </p>
      ) : null}

      {estimate != null ? (
        <CustomPlanSummaryCard
          estimate={estimate}
          cadence={cadenceState}
          onCadenceChange={handleCadenceChange}
          showActions={false}
          checkoutNote="VAT is added at checkout when applicable. No payment is taken until you confirm on the payment page."
          planDescription={`You are checking out with ${estimate.basePlan.charAt(0).toUpperCase()}${estimate.basePlan.slice(1)}.`}
          className="max-w-[581px] pt-0"
        />
      ) : null}

      <GuestLoopShellFooter>
        <div className="flex w-full items-center justify-between gap-4">
          <Button
            type="button"
            variant="outline"
            onClick={onBack}
            disabled={isSubmitting}
            className="h-auto w-[124px] rounded-[4px] border-[#4e4e4e] bg-transparent px-[19px] py-[13px] font-sans text-sm font-medium leading-5 text-[#141414] shadow-none"
          >
            Back
          </Button>
          <Button
            type="button"
            disabled={isSubmitting || estimate == null}
            onClick={() => void onConfirm()}
            className="h-auto min-w-[180px] rounded-[4px] bg-[#141414] px-[18px] py-3 font-sans text-sm font-medium leading-5 text-white shadow-none hover:bg-[#141414]/90"
          >
            {isSubmitting ? "Please wait..." : "Continue to payment"}
          </Button>
        </div>
      </GuestLoopShellFooter>
    </div>
  )
}

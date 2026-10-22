import { useEffect, useMemo, useState } from "react"
import { Check, ChevronDown } from "lucide-react"

import {
  fetchPublicCustomUnitRates,
  type PublicCatalogPlanSummaryDto,
} from "@/api/adminCommercialCatalogApi"
import customisePlanPanel from "@/assets/svg/marketing/custom-plan/customise-plan-panel.svg"
import { GuestLoopShellFooter } from "@/components/guest-loop/GuestLoopShell"
import { SignupComparePlansDialog } from "@/components/signup/SignupComparePlansDialog"
import { SignupPlanPrice } from "@/components/signup/SignupPlanPrice"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { PRICING_CADENCE_COPY } from "@/content/marketing/pricingPage"
import {
  SIGNUP_PLAN_CARD_IDS,
  SIGNUP_PLAN_DESCRIPTIONS,
  SIGNUP_PLAN_STEP_COPY,
  type SignupPlanCardId,
} from "@/content/marketing/signupComparePlans"
import { packsHaveAny, normalizePackCounts } from "@/lib/marketingCustomPlan/customPlanEstimate"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import { signupPlanPrices } from "@/lib/signupPlanPrices"
import {
  readSignupPlanIntent,
  saveSignupPlanIntent,
  type SignupPlanIntent,
} from "@/lib/signupPlanIntent"
import { cn } from "@/lib/utils"

type PlanCardId = SignupPlanCardId

type SignupChoosePlanStepProps = {
  onBack: () => void
  onCheckout: (
    plan: Exclude<PlanCardId, "Pilot">,
    cadence: BillingCadence,
  ) => void
  onCustomise: (
    plan: Exclude<PlanCardId, "Pilot">,
    cadence: BillingCadence,
  ) => void
  onPilotContinue: () => void
  isSubmitting?: boolean
}

const outlineButtonClass =
  "h-auto w-full rounded-[4px] border-[#4e4e4e] bg-transparent px-[19px] py-[13px] font-sans text-sm font-medium leading-5 text-[#141414] shadow-none hover:bg-[#f0f0f0]"

/** Figma Choose plan `6775:39425`. */
export function SignupChoosePlanStep({
  onBack,
  onCheckout,
  onCustomise,
  onPilotContinue,
  isSubmitting = false,
}: SignupChoosePlanStepProps) {
  const existing = readSignupPlanIntent()
  const preselected: PlanCardId =
    existing?.plan === "Custom" && existing.config != null
      ? ((existing.config.basePlan.charAt(0).toUpperCase() +
          existing.config.basePlan.slice(1)) as PlanCardId)
      : existing?.plan === "Pilot" ||
          existing?.plan === "Starter" ||
          existing?.plan === "Growth" ||
          existing?.plan === "Group"
        ? existing.plan
        : "Growth"

  const [selected, setSelected] = useState<PlanCardId>(preselected)
  const [cadence, setCadence] = useState<BillingCadence>(
    existing?.cadence === "annual" ? "annual" : "monthly",
  )
  const [catalogPlans, setCatalogPlans] = useState<
    PublicCatalogPlanSummaryDto[]
  >([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [compareOpen, setCompareOpen] = useState(false)

  useEffect(() => {
    void fetchPublicCustomUnitRates()
      .then((data) => {
        setCatalogPlans(data.plans)
      })
      .catch(() => {
        setLoadError("Plan prices are not available yet.")
      })
  }, [])

  const prices = useMemo(
    () => signupPlanPrices(cadence, catalogPlans),
    [cadence, catalogPlans],
  )

  function persistSelection(plan: PlanCardId, nextCadence: BillingCadence) {
    const intent: SignupPlanIntent =
      plan === "Pilot"
        ? { plan: "Pilot" }
        : {
            plan,
            cadence: nextCadence,
            // Keep the saved config while comparing cards; it applies again
            // when the user returns to its base plan card.
            ...(existing?.config != null
              ? {
                  config: {
                    ...existing.config,
                    cadence: nextCadence,
                  },
                }
              : {}),
          }
    saveSignupPlanIntent(intent)
  }

  function handleSelect(plan: PlanCardId) {
    setSelected(plan)
    persistSelection(plan, cadence)
  }

  function handleCadence(next: BillingCadence) {
    setCadence(next)
    persistSelection(selected, next)
  }

  function handleCheckout() {
    persistSelection(selected, cadence)
    if (selected === "Pilot") {
      onPilotContinue()
      return
    }
    onCheckout(selected, cadence)
  }

  function handleCustomise() {
    if (selected === "Pilot") return
    persistSelection(selected, cadence)
    onCustomise(selected, cadence)
  }

  const hasRestoredCustom =
    existing?.config != null &&
    selected !== "Pilot" &&
    existing.config.basePlan === selected.toLowerCase() &&
    (packsHaveAny(normalizePackCounts(existing.config.packs)) ||
      existing.config.locations > 1)

  return (
    <div className="flex w-full flex-col items-center gap-[50px]">
      <div className="flex w-full flex-col gap-6 lg:flex-row lg:items-start lg:justify-center lg:gap-[50px]">
        <header className="flex min-w-0 flex-1 flex-col gap-3 text-[#141414]">
          <h1 className="m-0 font-jakarta text-[28px] font-semibold leading-normal lg:text-[36px]">
            {SIGNUP_PLAN_STEP_COPY.title}
          </h1>
          <p className="m-0 font-sans text-base leading-[22px]">
            {SIGNUP_PLAN_STEP_COPY.body}
          </p>
        </header>

        <div
          className="flex w-fit shrink-0 items-center gap-2.5 rounded-[6px] border border-[rgba(38,38,38,0.21)] p-3"
          role="group"
          aria-label="Billing cadence"
        >
          <Button
            type="button"
            variant="ghost"
            aria-pressed={cadence === "monthly"}
            className={cn(
              "h-[41px] w-[79px] rounded p-3 font-sans text-sm font-medium shadow-none",
              cadence === "monthly"
                ? "bg-[#202020] text-white hover:bg-[#2a2a2a] hover:text-white"
                : "text-[#7c7c7c] hover:bg-[#efefef] hover:text-[#141414]",
            )}
            onClick={() => handleCadence("monthly")}
          >
            {PRICING_CADENCE_COPY.monthly}
          </Button>
          <Button
            type="button"
            variant="ghost"
            aria-pressed={cadence === "annual"}
            className={cn(
              "h-[41px] gap-2.5 rounded p-3 font-sans text-sm font-medium shadow-none",
              cadence === "annual"
                ? "bg-[#202020] text-white hover:bg-[#2a2a2a] hover:text-white"
                : "text-[#7c7c7c] hover:bg-[#efefef] hover:text-[#141414]",
            )}
            onClick={() => handleCadence("annual")}
          >
            {PRICING_CADENCE_COPY.annual}
            <Badge className="rounded-[34px] border-0 bg-[#14a74a] px-2.5 py-2 font-sans text-[10px] font-medium leading-none text-white hover:bg-[#14a74a]">
              {PRICING_CADENCE_COPY.saveBadge}
            </Badge>
          </Button>
        </div>
      </div>

      {loadError ? (
        <p className="m-0 w-full text-sm text-red-600">{loadError}</p>
      ) : null}

      <div className="grid w-full grid-cols-1 gap-4 md:grid-cols-2 xl:flex xl:items-stretch xl:gap-0">
        {SIGNUP_PLAN_CARD_IDS.map((planId) => {
          const isSelected = selected === planId
          const isPilot = planId === "Pilot"
          return (
            <article
              key={planId}
              aria-current={isSelected ? "true" : undefined}
              className={cn(
                "flex min-w-0 flex-1 flex-col justify-between gap-10 rounded-[6px] border px-[26px] py-6 xl:min-h-[325px]",
                isSelected
                  ? "border-[#14a74a] bg-[rgba(20,167,74,0.05)]"
                  : "border-transparent",
                isPilot &&
                  !isSelected &&
                  "xl:rounded-none xl:border-y-0 xl:border-l-0 xl:border-r-[#cacaca] xl:pl-0",
              )}
            >
              <div className="flex flex-col gap-3 text-[#141414]">
                <h2 className="m-0 font-jakarta text-[30px] font-medium leading-normal">
                  {planId}
                </h2>
                <p className="m-0 max-w-[300px] font-sans text-sm leading-[19px]">
                  {SIGNUP_PLAN_DESCRIPTIONS[planId]}
                </p>
              </div>

              <div className="flex flex-col gap-5">
                <SignupPlanPrice price={prices[planId]} />
                {isSelected ? (
                  <Button
                    type="button"
                    className="h-auto w-full gap-1.5 rounded-[4px] bg-[#171717] px-[18px] py-3 font-sans text-sm font-medium leading-[22px] text-white shadow-none hover:bg-[#171717]/90"
                    onClick={() => handleSelect(planId)}
                  >
                    <Check className="size-3.5" strokeWidth={1.5} aria-hidden />
                    {SIGNUP_PLAN_STEP_COPY.selected}
                  </Button>
                ) : (
                  <Button
                    type="button"
                    variant="outline"
                    className={outlineButtonClass}
                    onClick={() => handleSelect(planId)}
                  >
                    {isPilot
                      ? SIGNUP_PLAN_STEP_COPY.startPilot
                      : `Choose ${planId}`}
                  </Button>
                )}
              </div>
            </article>
          )
        })}
      </div>

      <Button
        type="button"
        variant="ghost"
        className="h-auto gap-1.5 px-0 py-0 font-sans text-base font-normal text-[#141414] hover:bg-transparent hover:underline"
        onClick={() => setCompareOpen(true)}
      >
        {SIGNUP_PLAN_STEP_COPY.seeFeatures}
        <ChevronDown className="size-3.5" aria-hidden />
      </Button>

      {selected !== "Pilot" ? (
        <section className="flex w-full flex-col overflow-hidden rounded-xl bg-[#f1f1f1] md:flex-row md:gap-8">
          <img
            src={customisePlanPanel}
            alt=""
            aria-hidden
            width={314}
            height={211}
            className="h-[211px] w-full shrink-0 object-cover md:w-[314px]"
          />
          <div className="flex flex-col items-start gap-8 px-6 py-[30px] md:px-0 md:pr-8">
            <div className="flex flex-col gap-3 text-black">
              <h2 className="m-0 font-jakarta text-xl font-bold leading-6">
                {SIGNUP_PLAN_STEP_COPY.customiseTitle}
              </h2>
              <p className="m-0 max-w-[743px] font-sans text-sm leading-[19px]">
                {hasRestoredCustom
                  ? `Your customised ${selected} plan is saved. Check out uses it, or open Customise to change Locations, packs or credits.`
                  : SIGNUP_PLAN_STEP_COPY.customiseBody}
              </p>
            </div>
            <Button
              type="button"
              variant="outline"
              className="h-auto w-fit rounded-[4px] border-[#4e4e4e] bg-transparent px-[19px] py-[13px] font-sans text-xs font-normal leading-[19px] text-black shadow-none hover:bg-white"
              onClick={handleCustomise}
            >
              {hasRestoredCustom
                ? "Review your customised plan"
                : SIGNUP_PLAN_STEP_COPY.customiseCta}
            </Button>
          </div>
        </section>
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
            {SIGNUP_PLAN_STEP_COPY.back}
          </Button>
          <Button
            type="button"
            onClick={handleCheckout}
            disabled={isSubmitting}
            className="h-auto min-w-[139px] rounded-[4px] bg-[#141414] px-[18px] py-3 font-sans text-sm font-medium leading-5 text-white shadow-none hover:bg-[#141414]/90"
          >
            {isSubmitting
              ? "Please wait..."
              : selected === "Pilot"
                ? SIGNUP_PLAN_STEP_COPY.startPilot
                : SIGNUP_PLAN_STEP_COPY.checkout}
          </Button>
        </div>
      </GuestLoopShellFooter>

      <SignupComparePlansDialog
        open={compareOpen}
        onOpenChange={setCompareOpen}
        prices={prices}
        selected={selected}
        onChoose={handleSelect}
      />
    </div>
  )
}

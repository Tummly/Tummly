import { useEffect, useRef, useState } from "react"

import { fetchPublicCustomUnitRates } from "@/api/adminCommercialCatalogApi"
import { GuestLoopShellFooter } from "@/components/guest-loop/GuestLoopShell"
import {
  CustomPlanQuotePlaceholder,
  CustomPlanSummaryCard,
} from "@/components/marketing/custom-plan/CustomPlanSummaryCard"
import { Button } from "@/components/ui/button"
import { CUSTOM_PLAN_QUOTE_COPY } from "@/content/marketing/customPlanQuoteCopy"
import {
  chosenPlanToBasePlan,
  EMPTY_PACK_COUNTS,
  normalizePackCounts,
  quotePlanName,
  storedPlanFromQuote,
  type CustomPlanBasePlanKey,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import {
  lastQuote,
  quoteHashChanged,
  quoteView,
} from "@/lib/marketingCustomPlan/customPlanQuoteState"
import { useServerCustomPlanQuote } from "@/lib/marketingCustomPlan/useServerCustomPlanQuote"
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
  /** Set after the server rejected the saved quote hash (quote_changed). */
  quoteChanged: boolean
}

/** Review reuses the calculator summary card (no separate Figma frame). */
export function SignupReviewPlanStep({
  onBack,
  onConfirm,
  isSubmitting = false,
  quoteChanged,
}: SignupReviewPlanStepProps) {
  // Read once: a fresh parse each render would change identities and re-run the quote.
  const [intent] = useState(() => readSignupPlanIntent())
  const config = activeCustomConfig(intent)
  // No intent, or Pilot: nothing to review. Custom without a restorable config: restore failed.
  const selection: "ok" | "no_selection" | "restore_failed" =
    intent == null || intent.plan === "Pilot"
      ? "no_selection"
      : intent.plan === "Custom" && config == null
        ? "restore_failed"
        : "ok"
  const selectable = selection === "ok"
  const basePlan: CustomPlanBasePlanKey | null = selectable
    ? (config?.basePlan ?? chosenPlanToBasePlan(intent?.plan))
    : null
  const [packs] = useState(() => config?.packs ?? EMPTY_PACK_COUNTS)
  const [cadenceState, setCadenceState] = useState<BillingCadence>(
    config?.cadence ?? intent?.cadence ?? "monthly",
  )

  // Plain card checkout: quote at the plan's included Locations so the
  // Location-count rule keeps the selected package.
  const [plainLocations, setPlainLocations] = useState<number | null>(null)
  const [plainFailed, setPlainFailed] = useState(false)
  const [plainAttempt, setPlainAttempt] = useState(0)

  useEffect(() => {
    if (config != null || !selectable || basePlan == null) return
    let cancelled = false
    void fetchPublicCustomUnitRates()
      .then((catalog) => {
        if (cancelled) return
        const row = catalog.plans.find((plan) => plan.planKey === basePlan)
        if (row == null) {
          setPlainFailed(true)
          return
        }
        setPlainLocations(Math.max(1, row.includedLocations))
      })
      .catch(() => {
        if (!cancelled) setPlainFailed(true)
      })
    return () => {
      cancelled = true
    }
  }, [config, basePlan, selectable, plainAttempt])

  function retryPlainCatalogue() {
    setPlainFailed(false)
    setPlainAttempt((n) => n + 1)
  }

  const locations = config != null ? config.locations : plainLocations
  const { state, check, retry } = useServerCustomPlanQuote({
    cadence: cadenceState,
    basePlan,
    locations,
    packs,
  })

  // A rejected hash means the price moved: re-quote so the new total shows.
  useEffect(() => {
    if (quoteChanged) retry()
  }, [quoteChanged, retry])

  // The first ready quote can differ from the hash saved earlier. Show the changed state then.
  const firstReadyChecked = useRef(false)
  const [restoredHashChanged, setRestoredHashChanged] = useState(false)

  // Every ready quote becomes the saved config, with its hash, so Continue sends the hash on screen.
  useEffect(() => {
    if (state.status !== "ready") return
    const next = state.quote
    if (!firstReadyChecked.current) {
      firstReadyChecked.current = true
      if (quoteHashChanged(config?.quoteHash, next.quoteHash)) {
        setRestoredHashChanged(true)
      }
    }
    const nextBasePlan = chosenPlanToBasePlan(next.basePlan)
    if (nextBasePlan == null) return
    saveSignupCustomPlanConfig({
      basePlan: nextBasePlan,
      cadence: next.cadence === "annual" ? "annual" : "monthly",
      locations: next.locations,
      packs: normalizePackCounts(next.packs),
      asCustom: storedPlanFromQuote(next.storedAsPlan) === "Custom",
      quoteHash: next.quoteHash,
    })
  }, [state])

  const quote = lastQuote(state)
  const waitingForCatalogue = selectable && config == null && plainLocations == null
  const view = waitingForCatalogue
    ? null
    : quoteView({
        check,
        state,
        changed: quoteChanged || restoredHashChanged,
      })
  const continueEnabled = selectable && view?.continueEnabled === true

  function handleCadenceChange(next: BillingCadence) {
    setCadenceState(next)
    const current = readSignupPlanIntent()
    if (current == null || current.plan === "Pilot") return
    saveSignupPlanIntent({
      ...current,
      cadence: next,
      ...(current.config != null
        ? { config: { ...current.config, cadence: next } }
        : {}),
    })
  }

  const selectionMessage =
    selection === "no_selection"
      ? CUSTOM_PLAN_QUOTE_COPY.noSelection
      : selection === "restore_failed"
        ? CUSTOM_PLAN_QUOTE_COPY.restoreFailed
        : null

  // Before the first quote, the placeholder shows the catalogue or quote status.
  const placeholderMessage = waitingForCatalogue
    ? plainFailed
      ? CUSTOM_PLAN_QUOTE_COPY.failed
      : CUSTOM_PLAN_QUOTE_COPY.loading
    : (view?.message ?? null)
  const placeholderRetry = waitingForCatalogue ? retryPlainCatalogue : retry

  return (
    <div className="mx-auto flex w-full max-w-[581px] flex-col gap-10">
      <header className="flex flex-col gap-3 text-[#141414]">
        <h1 className="m-0 font-jakarta text-[36px] font-medium leading-[normal]">
          Review your subscription
        </h1>
        <p className="m-0 max-w-[640px] font-sans text-base leading-[22px]">
          Confirm your plan, included allowances, additional capacity, billing
          period and price before you continue to payment.
        </p>
      </header>

      {selectionMessage != null ? (
        <p role="status" className="m-0 font-sans text-sm text-[#141414]">
          {selectionMessage}
        </p>
      ) : null}

      {selectable && quote != null ? (
        <CustomPlanSummaryCard
          quote={quote}
          cadence={cadenceState}
          onCadenceChange={handleCadenceChange}
          showActions={false}
          statusMessage={view?.message ?? null}
          showRetry={view?.showRetry ?? false}
          onRetry={retry}
          showContact={view?.showContact ?? false}
          taxText={view?.taxText ?? null}
          checkoutNote={CUSTOM_PLAN_QUOTE_COPY.noPaymentTaken}
          planDescription={`You are checking out with ${quotePlanName(quote)}.`}
          className="max-w-[581px] pt-0"
        />
      ) : null}

      {selectable && quote == null ? (
        <CustomPlanQuotePlaceholder
          loading={waitingForCatalogue ? !plainFailed : state.status === "loading"}
          message={placeholderMessage}
          showRetry={waitingForCatalogue ? plainFailed : (view?.showRetry ?? false)}
          onRetry={placeholderRetry}
          showContact={view?.showContact ?? false}
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
            disabled={isSubmitting || !continueEnabled}
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

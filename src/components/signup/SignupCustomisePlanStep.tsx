import { GuestLoopShellFooter } from "@/components/guest-loop/GuestLoopShell"
import { CustomPlanCalculator } from "@/components/marketing/custom-plan/CustomPlanCalculator"
import { Button } from "@/components/ui/button"
import { capacityRetiredNotice } from "@/lib/marketingCustomPlan/customPlanQuoteState"
import {
  chosenPlanToBasePlan,
  storedPlanFromQuote,
  type CustomPlanBasePlanKey,
} from "@/lib/marketingCustomPlan/customPlanEstimate"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  readSignupPlanIntent,
  saveSignupCustomPlanConfig,
} from "@/lib/signupPlanIntent"

type SignupCustomisePlanStepProps = {
  onBack: () => void
  onContinueToReview: () => void
}

/** Figma Customise calculator `6775:40023` / plan-changed `6777:40772`. */
export function SignupCustomisePlanStep({
  onBack,
  onContinueToReview,
}: SignupCustomisePlanStepProps) {
  const intent = readSignupPlanIntent()
  const basePlan: CustomPlanBasePlanKey =
    intent?.config?.basePlan ??
    chosenPlanToBasePlan(intent?.plan) ??
    "growth"
  const cadence: BillingCadence =
    intent?.config?.cadence ?? intent?.cadence ?? "monthly"
  const locations = intent?.config?.locations ?? 1
  const packs = intent?.config?.packs
  const retiredNotice = capacityRetiredNotice(intent)

  return (
    <div className="flex w-full flex-col gap-8">
      {retiredNotice != null ? (
        <p role="status" className="m-0 font-sans text-sm text-[#141414]">
          {retiredNotice}
        </p>
      ) : null}
      <CustomPlanCalculator
        key={`${basePlan}-${locations}-${cadence}`}
        initialBasePlan={basePlan}
        initialLocations={locations}
        initialPacks={packs}
        initialCadence={cadence}
        continueLabel="Review subscription"
        onContinue={({ quote, config }) => {
          saveSignupCustomPlanConfig({
            basePlan: config.basePlan,
            cadence: config.cadence,
            locations: config.locations,
            packs: config.packs,
            asCustom: storedPlanFromQuote(quote.storedAsPlan) === "Custom",
            quoteHash: quote.quoteHash,
          })
          onContinueToReview()
        }}
      />
      <GuestLoopShellFooter>
        <Button
          type="button"
          variant="outline"
          onClick={onBack}
          className="h-auto min-w-[124px] rounded-[4px] border-[#4e4e4e] bg-transparent px-[19px] py-[13px] font-sans text-sm font-medium leading-5 text-[#141414] shadow-none"
        >
          Back to plans
        </Button>
      </GuestLoopShellFooter>
    </div>
  )
}

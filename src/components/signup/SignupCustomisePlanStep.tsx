import { GuestLoopShellFooter } from "@/components/guest-loop/GuestLoopShell"
import { CustomPlanCalculator } from "@/components/marketing/custom-plan/CustomPlanCalculator"
import { Button } from "@/components/ui/button"
import {
  chosenPlanToBasePlan,
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

  return (
    <div className="flex w-full flex-col gap-8">
      <CustomPlanCalculator
        key={`${basePlan}-${locations}-${cadence}`}
        initialBasePlan={basePlan}
        initialLocations={locations}
        initialPacks={packs}
        initialCadence={cadence}
        continueLabel="Review subscription"
        onContinue={({ estimate, config }) => {
          saveSignupCustomPlanConfig({
            basePlan: estimate.basePlan,
            cadence: config.cadence,
            locations: config.locations,
            packs: config.packs,
            asCustom: estimate.storedAsPlan === "Custom",
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

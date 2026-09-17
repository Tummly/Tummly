import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  MANAGE_PLAN_CARD_TITLE_CLASS,
  MANAGE_PLAN_CATALOG,
  MANAGE_PLAN_COMPARISON_ROWS,
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_IDS,
  MANAGE_PLAN_PRICE_AMOUNT_CLASS,
  MANAGE_PLAN_PRICE_SUFFIX_CLASS,
  type BillingCadence,
  type ManagePlanId,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

type SignupPlanFeaturesDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  cadence: BillingCadence
  onSelectPlan: (planId: ManagePlanId) => void
  busyPlanId: ManagePlanId | null
  disabled?: boolean
}

function planCtaLabel(planId: ManagePlanId) {
  if (planId === "Pilot") return MANAGE_PLAN_COPY.startPilot
  return MANAGE_PLAN_COPY.choosePlan(planId)
}

export function SignupPlanFeaturesDialog({
  open,
  onOpenChange,
  cadence,
  onSelectPlan,
  busyPlanId,
  disabled = false,
}: SignupPlanFeaturesDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="max-h-[min(90dvh,920px)] w-full max-w-[calc(100%-2rem)] gap-0 overflow-hidden bg-white p-0 sm:max-w-[1100px]"
        showCloseButton
      >
        <DialogHeader className="border-b border-border px-6 py-6 pr-14 sm:px-8">
          <DialogTitle className="text-[28px] font-semibold tracking-[-0.56px] text-[#232323]">
            Compare plans
          </DialogTitle>
          <DialogDescription className="max-w-none text-sm text-muted-foreground">
            Pick Pilot for free, or choose a paid plan. Prices exclude VAT.
          </DialogDescription>
        </DialogHeader>

        <div className="overflow-auto">
          <div className="min-w-240">
            <div className="grid grid-cols-[180px_repeat(4,minmax(0,1fr))] gap-x-4 border-b border-border bg-[#f5f5f5] px-6 pb-6 pt-6 sm:px-8">
              <div className="flex items-end pb-1">
                <p className="text-base font-medium text-foreground">
                  {MANAGE_PLAN_COPY.comparisonFeature}
                </p>
              </div>
              {MANAGE_PLAN_IDS.map((planId) => {
                const entry = MANAGE_PLAN_CATALOG[planId]
                const priceAmount =
                  cadence === "annual"
                    ? entry.annualPriceNet
                    : entry.monthlyPriceNet
                const priceSuffix =
                  planId === "Pilot"
                    ? "/ for 30 days"
                    : cadence === "annual"
                      ? "/ year"
                      : "/ month"
                const priceSubline =
                  planId === "Pilot"
                    ? (entry.priceSublinePilot ?? "No card required")
                    : cadence === "monthly"
                      ? `${entry.annualPriceNet}/year`
                      : entry.annualSaveLabel || null
                const isBusy = busyPlanId === planId

                return (
                  <div key={planId} className="flex flex-col gap-5">
                    <div className="flex flex-col gap-3">
                      <div className="flex items-center justify-between gap-2">
                        <h3
                          className={cn(
                            MANAGE_PLAN_CARD_TITLE_CLASS,
                            "text-[22px] sm:text-[25px]"
                          )}
                        >
                          {planId}
                        </h3>
                        {entry.isMostPopular ? (
                          <span className="rounded bg-primary/18 px-2.5 py-1.5 text-[10px] font-medium text-primary sm:text-xs">
                            {MANAGE_PLAN_COPY.mostPopular}
                          </span>
                        ) : null}
                      </div>
                      <div className="flex flex-col gap-1.5">
                        <p className="m-0 flex flex-wrap items-baseline gap-x-1 text-foreground">
                          <span
                            className={cn(
                              MANAGE_PLAN_PRICE_AMOUNT_CLASS,
                              "text-[28px] sm:text-[34px]"
                            )}
                          >
                            {priceAmount}
                          </span>
                          <span className={MANAGE_PLAN_PRICE_SUFFIX_CLASS}>
                            {priceSuffix}
                          </span>
                        </p>
                        <p
                          className={cn(
                            "m-0 min-h-5 text-xs font-medium text-muted-foreground sm:text-sm",
                            priceSubline == null && "invisible"
                          )}
                          aria-hidden={priceSubline == null}
                        >
                          {priceSubline ?? "\u00a0"}
                        </p>
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant={
                        entry.isMostPopular ? "op-primary" : "op-tertiary"
                      }
                      className="h-11 w-full"
                      disabled={disabled || busyPlanId != null}
                      onClick={() => {
                        onSelectPlan(planId)
                      }}
                    >
                      {isBusy ? "Please wait..." : planCtaLabel(planId)}
                    </Button>
                  </div>
                )
              })}
            </div>

            <div className="px-6 py-2 sm:px-8">
              {MANAGE_PLAN_COMPARISON_ROWS.map((row) => (
                <div
                  key={row.label}
                  className="grid grid-cols-[180px_repeat(4,minmax(0,1fr))] gap-x-4 border-b border-border/60 py-4 last:border-b-0"
                >
                  <p className="text-sm font-medium text-foreground">
                    {row.label}
                  </p>
                  {MANAGE_PLAN_IDS.map((planId) => {
                    const value = row.values[planId]
                    const isDash = value === "—"
                    return (
                      <p
                        key={planId}
                        className={cn(
                          "text-sm font-normal",
                          isDash
                            ? "text-muted-foreground"
                            : "text-foreground"
                        )}
                      >
                        {value}
                      </p>
                    )
                  })}
                </div>
              ))}
            </div>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}

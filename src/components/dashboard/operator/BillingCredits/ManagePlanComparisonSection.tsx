import { Button } from "@/components/ui/button"
import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import {
  ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  MANAGE_PLAN_CARD_TITLE_CLASS,
  MANAGE_PLAN_COMPARISON_ROWS,
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_IDS,
  MANAGE_PLAN_PRICE_AMOUNT_CLASS,
  MANAGE_PLAN_PRICE_SUFFIX_CLASS,
  type ManagePlanCardViewModel,
  type ManagePlanId,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

function ComparisonPlanHeader({
  card,
  onSelect,
}: {
  card: ManagePlanCardViewModel
  onSelect: (planId: ManagePlanId) => void
}) {
  const cta = card.cta

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3">
        <div className="flex items-center justify-between gap-2">
          <h3 className={cn(MANAGE_PLAN_CARD_TITLE_CLASS, "text-[25px]")}>
            {card.id}
          </h3>
          {card.isMostPopular ? (
            <span className="rounded bg-primary/18 px-3 py-2 text-xs font-medium text-primary">
              {MANAGE_PLAN_COPY.mostPopular}
            </span>
          ) : null}
        </div>
        <div className="flex flex-col gap-2">
          <p className="flex flex-wrap items-baseline gap-x-1 text-foreground">
            <span className={cn(MANAGE_PLAN_PRICE_AMOUNT_CLASS, "text-[34px]")}>
              {card.priceAmount}
            </span>
            <span className={MANAGE_PLAN_PRICE_SUFFIX_CLASS}>
              {card.priceSuffix}
            </span>
          </p>
          {card.priceSubline != null ? (
            <p className="text-sm font-medium text-muted-foreground">
              {card.priceSubline}
            </p>
          ) : (
            <p className="invisible min-h-5 text-sm font-medium" aria-hidden>
              &nbsp;
            </p>
          )}
        </div>
      </div>
      <Button
        type="button"
        variant={
          cta.kind === "current"
            ? "op-secondary"
            : card.isMostPopular
              ? "op-primary"
              : "op-tertiary"
        }
        className="h-11 w-full"
        disabled={cta.disabled}
        onClick={() => {
          if (cta.kind === "action") {
            onSelect(card.id)
          }
        }}
      >
        {cta.label}
      </Button>
    </div>
  )
}

export function ManagePlanComparisonSection({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  if (!snap.showPlanCards || snap.managePlanCards.length === 0) {
    return null
  }

  const cardsById = Object.fromEntries(
    snap.managePlanCards.map((card) => [card.id, card])
  ) as Record<ManagePlanId, ManagePlanCardViewModel>

  return (
    <section className={cn(ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS, "gap-0 p-0")}>
      <div className="overflow-x-auto">
        <div className="min-w-240">
          <div className="grid grid-cols-[224px_repeat(4,minmax(0,1fr))] gap-x-5 border-b border-border px-6 pb-6 pt-6">
            <div className="flex items-end pb-1">
              <p className="text-lg font-medium text-foreground">
                {MANAGE_PLAN_COPY.comparisonFeature}
              </p>
            </div>
            {MANAGE_PLAN_IDS.map((planId) => (
              <ComparisonPlanHeader
                key={planId}
                card={cardsById[planId]}
                onSelect={(id) => {
                  pageModule.requestPlanChange(id)
                }}
              />
            ))}
          </div>

          <div className="px-6 py-2">
            {MANAGE_PLAN_COMPARISON_ROWS.map((row) => (
              <div
                key={row.label}
                className="grid grid-cols-[224px_repeat(4,minmax(0,1fr))] gap-x-5 border-b border-border/60 py-4 last:border-b-0"
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
    </section>
  )
}

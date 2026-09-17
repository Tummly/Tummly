import { ChevronDownIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import {
  MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS,
  MANAGE_PLAN_CADENCE_ITEM_CLASS,
  MANAGE_PLAN_CADENCE_SAVE_BADGE_CLASS,
  MANAGE_PLAN_CADENCE_SHELL_CLASS,
  MANAGE_PLAN_CARD_DESCRIPTION_CLASS,
  MANAGE_PLAN_CARD_POPULAR_CLASS,
  MANAGE_PLAN_CARD_TITLE_CLASS,
  MANAGE_PLAN_CATALOG,
  MANAGE_PLAN_COMPLETE_LIST_TRIGGER_CLASS,
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_PRICE_AMOUNT_CLASS,
  MANAGE_PLAN_PRICE_SUFFIX_CLASS,
  type BillingCadence,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  mapSignupCardToPlanId,
  type SignupPlanCard,
} from "@/lib/signupPlanMap"
import { cn } from "@/lib/utils"

const CARD_COPY: Record<
  SignupPlanCard,
  { title: string; cta: string; highlight: boolean }
> = {
  essential: {
    title: "Essential",
    cta: "Start with Essential",
    highlight: false,
  },
  pro: {
    title: "Pro",
    cta: "Choose Pro",
    highlight: true,
  },
}

type SignupChoosePlanCardsProps = {
  cadence: BillingCadence
  onCadenceChange: (cadence: BillingCadence) => void
  onSelectCard: (card: SignupPlanCard) => void
  onOpenFeatures: () => void
  busyCard: SignupPlanCard | null
  disabled?: boolean
}

export function SignupChoosePlanCards({
  cadence,
  onCadenceChange,
  onSelectCard,
  onOpenFeatures,
  busyCard,
  disabled = false,
}: SignupChoosePlanCardsProps) {
  return (
    <div className="flex w-full flex-col gap-9">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div
          className={MANAGE_PLAN_CADENCE_SHELL_CLASS}
          role="group"
          aria-label="Billing cadence"
        >
          <button
            type="button"
            disabled={disabled}
            aria-pressed={cadence === "monthly"}
            className={cn(
              MANAGE_PLAN_CADENCE_ITEM_CLASS,
              "min-w-20",
              cadence === "monthly" && MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS
            )}
            onClick={() => {
              onCadenceChange("monthly")
            }}
          >
            {MANAGE_PLAN_COPY.cadenceMonthly}
          </button>
          <button
            type="button"
            disabled={disabled}
            aria-pressed={cadence === "annual"}
            className={cn(
              MANAGE_PLAN_CADENCE_ITEM_CLASS,
              cadence === "annual" && MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS
            )}
            onClick={() => {
              onCadenceChange("annual")
            }}
          >
            {MANAGE_PLAN_COPY.cadenceAnnual}
            <span className={MANAGE_PLAN_CADENCE_SAVE_BADGE_CLASS}>
              {MANAGE_PLAN_COPY.cadenceAnnualSave}
            </span>
          </button>
        </div>

        <p className="m-0 text-sm font-normal text-muted-foreground">
          {MANAGE_PLAN_COPY.vatNotice}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-6 md:grid-cols-2 md:gap-0 md:divide-x md:divide-[#cacaca]">
        {(["essential", "pro"] as const).map((card) => {
          const planId = mapSignupCardToPlanId(card)
          const entry = MANAGE_PLAN_CATALOG[planId]
          const copy = CARD_COPY[card]
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
              ? (entry.priceSublinePilot ??
                "No payment card required. No automatic paid renewal.")
              : cadence === "monthly"
                ? `${entry.annualPriceNet}/year`
                : entry.annualSaveLabel || null
          const isBusy = busyCard === card

          return (
            <article
              key={card}
              className={cn(
                "flex flex-col gap-10 px-0 py-6 md:px-6.5",
                copy.highlight && MANAGE_PLAN_CARD_POPULAR_CLASS,
                copy.highlight && "md:rounded-[6px]"
              )}
            >
              <div className="flex flex-col gap-3">
                <div className="flex items-center justify-between gap-3">
                  <h2 className={MANAGE_PLAN_CARD_TITLE_CLASS}>{copy.title}</h2>
                  {copy.highlight ? (
                    <span className="shrink-0 rounded bg-primary/18 px-3 py-2 text-xs font-medium text-primary">
                      {MANAGE_PLAN_COPY.mostPopular}
                    </span>
                  ) : null}
                </div>
                <p className={MANAGE_PLAN_CARD_DESCRIPTION_CLASS}>
                  {entry.description}
                </p>
              </div>

              <div className="flex flex-col gap-5">
                <div className="flex flex-col gap-2">
                  <p className="m-0 text-foreground">
                    <span className={MANAGE_PLAN_PRICE_AMOUNT_CLASS}>
                      {priceAmount}
                    </span>
                    <span className={MANAGE_PLAN_PRICE_SUFFIX_CLASS}>
                      {priceSuffix}
                    </span>
                  </p>
                  <p
                    className={cn(
                      "m-0 min-h-5 text-sm font-medium text-muted-foreground",
                      priceSubline == null && "invisible"
                    )}
                    aria-hidden={priceSubline == null}
                  >
                    {priceSubline ?? "\u00a0"}
                  </p>
                </div>

                <Button
                  type="button"
                  variant={copy.highlight ? "op-primary" : "op-tertiary"}
                  className="h-11 w-full shrink-0"
                  disabled={disabled || busyCard != null}
                  onClick={() => {
                    onSelectCard(card)
                  }}
                >
                  {isBusy ? "Please wait..." : copy.cta}
                </Button>
              </div>
            </article>
          )
        })}
      </div>

      <div className="flex justify-center pt-2">
        <button
          type="button"
          className={MANAGE_PLAN_COMPLETE_LIST_TRIGGER_CLASS}
          disabled={disabled || busyCard != null}
          onClick={onOpenFeatures}
        >
          See full features list
          <ChevronDownIcon className="size-3.5 shrink-0" aria-hidden />
        </button>
      </div>
    </div>
  )
}

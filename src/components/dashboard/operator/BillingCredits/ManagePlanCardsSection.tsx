import { useState } from "react"
import { ChevronDownIcon, CoinsIcon, MailIcon, MessageSquareIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { ManagePlanComparisonSection } from "@/components/dashboard/operator/BillingCredits/ManagePlanComparisonSection"
import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import { ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  MANAGE_PLAN_BODY_STACK_CLASS,
  MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS,
  MANAGE_PLAN_CADENCE_ITEM_CLASS,
  MANAGE_PLAN_CADENCE_SAVE_BADGE_CLASS,
  MANAGE_PLAN_CADENCE_SHELL_CLASS,
  MANAGE_PLAN_CARD_CLASS,
  MANAGE_PLAN_CARD_DESCRIPTION_CLASS,
  MANAGE_PLAN_CARD_POPULAR_CLASS,
  MANAGE_PLAN_CARD_TITLE_CLASS,
  MANAGE_PLAN_CARDS_GRID_CLASS,
  MANAGE_PLAN_COMPLETE_LIST_TRIGGER_CLASS,
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_CURRENT_PLAN_CARD_CLASS,
  MANAGE_PLAN_CURRENT_PLAN_NAME_CLASS,
  MANAGE_PLAN_FEATURE_LABEL_CLASS,
  MANAGE_PLAN_FEATURE_ROW_CLASS,
  MANAGE_PLAN_FEATURE_VALUE_CLASS,
  MANAGE_PLAN_PRICE_AMOUNT_CLASS,
  MANAGE_PLAN_PRICE_SUFFIX_CLASS,
  type ManagePlanCardViewModel,
  type ManagePlanFeatureRow,
  type ManagePlanId,
  type PlanFeatureIcon,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import { cn } from "@/lib/utils"

function FeatureIcon({ icon }: { icon: PlanFeatureIcon }) {
  switch (icon) {
    case "email":
      return <MailIcon className="size-4 shrink-0" aria-hidden />
    case "sms":
      return <MessageSquareIcon className="size-4 shrink-0" aria-hidden />
    case "ai":
      return <CoinsIcon className="size-4 shrink-0" aria-hidden />
  }
}

function PlanFeatureRow({ row }: { row: ManagePlanFeatureRow }) {
  return (
    <div className={MANAGE_PLAN_FEATURE_ROW_CLASS}>
      <div className="flex min-w-0 items-center gap-3">
        {row.icon != null ? <FeatureIcon icon={row.icon} /> : null}
        <p className={MANAGE_PLAN_FEATURE_LABEL_CLASS}>{row.label}</p>
      </div>
      <p className={MANAGE_PLAN_FEATURE_VALUE_CLASS}>{row.value}</p>
    </div>
  )
}

function PlanCard({
  card,
  onSelect,
}: {
  card: ManagePlanCardViewModel
  onSelect: (planId: ManagePlanId) => void
}) {
  const cta = card.cta

  return (
    <article
      className={
        card.isMostPopular
          ? MANAGE_PLAN_CARD_POPULAR_CLASS
          : MANAGE_PLAN_CARD_CLASS
      }
    >
      <div className="flex flex-col gap-3">
        <div className="flex items-center justify-between gap-3">
          <h3 className={MANAGE_PLAN_CARD_TITLE_CLASS}>{card.id}</h3>
          {card.isMostPopular ? (
            <span className="shrink-0 rounded bg-primary/18 px-3 py-2 text-xs font-medium text-primary">
              {MANAGE_PLAN_COPY.mostPopular}
            </span>
          ) : null}
        </div>
        <p className={MANAGE_PLAN_CARD_DESCRIPTION_CLASS}>{card.description}</p>
      </div>

        <div className="flex flex-col gap-5">
          <div className="flex flex-col gap-2">
            <p className="m-0 text-foreground">
              <span className={MANAGE_PLAN_PRICE_AMOUNT_CLASS}>
                {card.priceAmount}
              </span>
              <span className={MANAGE_PLAN_PRICE_SUFFIX_CLASS}>
                {card.priceSuffix}
              </span>
            </p>
            <p
              className={cn(
                "m-0 min-h-5 text-sm font-medium text-muted-foreground",
                card.priceSubline == null && "invisible"
              )}
              aria-hidden={card.priceSubline == null}
            >
              {card.priceSubline ?? "\u00a0"}
            </p>
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
            className="h-11 w-full shrink-0"
            disabled={cta.disabled}
            onClick={() => {
              if (cta.kind === "action") {
                onSelect(card.id)
              }
            }}
          >
            {cta.label}
          </Button>

        <div className="flex flex-col gap-5.5">
          {card.coreFeatures.map((row) => (
            <PlanFeatureRow key={row.label} row={row} />
          ))}
          <hr className="border-border" />
          {card.allowanceFeatures.map((row) => (
            <PlanFeatureRow
              key={`${row.label}-${row.value}`}
              row={row}
            />
          ))}
        </div>
      </div>
    </article>
  )
}

export function ManagePlanCardsSection({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  const [completeFeaturesOpen, setCompleteFeaturesOpen] = useState(false)

  if (!snap.showPlanCards) {
    return null
  }

  return (
    <div className={MANAGE_PLAN_BODY_STACK_CLASS}>
      {snap.currentPlanSummary != null ? (
        <section className={MANAGE_PLAN_CURRENT_PLAN_CARD_CLASS}>
          <div className="flex flex-col gap-2">
            <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
              {MANAGE_PLAN_COPY.currentPlanHeading}
            </p>
            <h2 className={MANAGE_PLAN_CURRENT_PLAN_NAME_CLASS}>
              {snap.planSubscription?.subscriptionPlan}
            </h2>
            <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
              {snap.currentPlanSummary}
            </p>
          </div>
        </section>
      ) : null}

      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div
          className={MANAGE_PLAN_CADENCE_SHELL_CLASS}
          role="group"
          aria-label="Billing cadence preview"
        >
          <button
            type="button"
            disabled={snap.managePlanLockMode !== "none"}
            aria-pressed={snap.previewCadence === "monthly"}
            className={cn(
              MANAGE_PLAN_CADENCE_ITEM_CLASS,
              "min-w-20",
              snap.previewCadence === "monthly"
                && MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS
            )}
            onClick={() => {
              pageModule.setPreviewCadence("monthly")
            }}
          >
            {MANAGE_PLAN_COPY.cadenceMonthly}
          </button>
          <button
            type="button"
            disabled={snap.managePlanLockMode !== "none"}
            aria-pressed={snap.previewCadence === "annual"}
            className={cn(
              MANAGE_PLAN_CADENCE_ITEM_CLASS,
              snap.previewCadence === "annual"
                && MANAGE_PLAN_CADENCE_ITEM_ACTIVE_CLASS
            )}
            onClick={() => {
              pageModule.setPreviewCadence("annual")
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

      <div className={MANAGE_PLAN_CARDS_GRID_CLASS}>
        {snap.managePlanCards.map((card) => (
          <PlanCard
            key={card.id}
            card={card}
            onSelect={(planId) => {
              pageModule.requestPlanChange(planId)
            }}
          />
        ))}
      </div>

      {completeFeaturesOpen ? (
        <ManagePlanComparisonSection snap={snap} pageModule={pageModule} />
      ) : null}

      <div className="flex justify-center pt-2">
        <button
          type="button"
          className={MANAGE_PLAN_COMPLETE_LIST_TRIGGER_CLASS}
          aria-expanded={completeFeaturesOpen}
          onClick={() => {
            setCompleteFeaturesOpen((open) => !open)
          }}
        >
          {MANAGE_PLAN_COPY.completeFeaturesList}
          <ChevronDownIcon
            className={cn(
              "size-3.5 shrink-0 transition-transform",
              completeFeaturesOpen && "rotate-180"
            )}
            aria-hidden
          />
        </button>
      </div>

      {snap.planChangeConfirm?.open ? (
        <AccountWorkspaceConfirmDialog
          open
          title={snap.planChangeConfirm.title}
          body={snap.planChangeConfirm.body}
          primaryLabel={snap.planChangeConfirm.primaryLabel}
          busy={snap.planChangeConfirm.busy}
          onOpenChange={(open) => {
            if (!open) {
              pageModule.cancelPlanChange()
            }
          }}
          onPrimary={() => {
            void pageModule.confirmPlanChange()
          }}
          onCancel={() => {
            pageModule.cancelPlanChange()
          }}
        />
      ) : null}
    </div>
  )
}

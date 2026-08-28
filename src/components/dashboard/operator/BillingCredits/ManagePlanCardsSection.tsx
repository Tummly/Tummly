import { Button } from "@/components/ui/button"
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group"
import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import {
  MANAGE_PLAN_COPY,
  type ManagePlanCardViewModel,
  type ManagePlanId,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  GUESTS_DETAIL_FIELD_CLASS,
  GUESTS_DETAIL_FIELD_LABEL_CLASS,
  GUESTS_DETAIL_FIELD_VALUE_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

function PlanFeatureRow({ label, value }: { label: string; value: string }) {
  return (
    <div className={GUESTS_DETAIL_FIELD_CLASS}>
      <p className={GUESTS_DETAIL_FIELD_LABEL_CLASS}>{label}</p>
      <p className={GUESTS_DETAIL_FIELD_VALUE_CLASS}>{value}</p>
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
    <article className={cn(GUESTS_SECTION_CLASS, "flex h-full flex-col gap-6")}>
      <div className="flex flex-col gap-2">
        <h3 className="text-2xl font-bold text-foreground">{card.id}</h3>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{card.description}</p>
      </div>

      <div className="flex flex-col gap-2">
        <p className="text-3xl font-bold text-foreground">{card.priceHeadline}</p>
        {card.annualSaveLabel != null ? (
          <p className="text-sm font-medium text-muted-foreground">
            {card.annualSaveLabel}
          </p>
        ) : null}
      </div>

      <Button
        type="button"
        variant={cta.kind === "current" ? "op-secondary" : "op-primary"}
        className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
        disabled={cta.disabled}
        onClick={() => {
          if (cta.kind === "action") {
            onSelect(card.id)
          }
        }}
      >
        {cta.label}
      </Button>

      <div className="flex flex-col gap-3 border-t border-border pt-4">
        <PlanFeatureRow label="Email credits" value={card.emailCreditsLabel} />
        <PlanFeatureRow label="SMS credits" value={card.smsCreditsLabel} />
        <PlanFeatureRow label="AI credits" value={card.aiCreditsLabel} />
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
  if (!snap.showPlanCards) {
    return null
  }

  return (
    <>
      {snap.currentPlanSummary != null ? (
        <section className={GUESTS_SECTION_CLASS}>
          <p className="text-sm font-medium text-muted-foreground">
            {MANAGE_PLAN_COPY.currentPlanHeading}
          </p>
          <p className="text-2xl font-bold text-foreground">
            {snap.planSubscription?.subscriptionPlan}
          </p>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {snap.currentPlanSummary}
          </p>
        </section>
      ) : null}

      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <ToggleGroup
          type="single"
          variant="outline"
          spacing={0}
          value={snap.previewCadence}
          onValueChange={(value) => {
            if (value === "monthly" || value === "annual") {
              pageModule.setPreviewCadence(value)
            }
          }}
          aria-label="Billing cadence preview"
        >
          <ToggleGroupItem value="monthly" className="min-w-28">
            {MANAGE_PLAN_COPY.cadenceMonthly}
          </ToggleGroupItem>
          <ToggleGroupItem value="annual" className="min-w-28">
            {MANAGE_PLAN_COPY.cadenceAnnual}
          </ToggleGroupItem>
        </ToggleGroup>

        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
          {MANAGE_PLAN_COPY.vatNotice}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-4">
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
    </>
  )
}

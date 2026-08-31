import { useEffect, useSyncExternalStore, type ReactNode } from "react"
import {
  ChevronRightIcon,
  CoinsIcon,
  MailIcon,
  MessageSquareIcon,
  PackageIcon,
} from "lucide-react"
import { Link, useNavigate, useSearchParams } from "react-router-dom"

import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import { ManagePlanCardsSection } from "@/components/dashboard/operator/BillingCredits/ManagePlanCardsSection"
import { ManagePlanFaqSection } from "@/components/dashboard/operator/BillingCredits/ManagePlanFaqSection"
import { CreditTopUpsSection } from "@/components/dashboard/operator/BillingCredits/CreditTopUpsSection"
import { CancelSubscriptionDialog } from "@/components/dashboard/operator/BillingCredits/CancelSubscriptionDialog"
import {
  ManagePlanAdditionalGroupLocationSection,
} from "@/components/dashboard/operator/BillingCredits/ManagePlanGroupActionsSection"
import { PaymentInvoicesTable } from "@/components/dashboard/operator/BillingCredits/PaymentInvoicesTable"
import { UpdatePaymentMethodConfirmDialog } from "@/components/dashboard/operator/BillingCredits/UpdatePaymentMethodConfirmDialog"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { Spinner } from "@/components/ui/spinner"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { HELP_CENTRE_CONTACT_URL } from "@/config/support"
import {
  BILLING_CREDITS_PAGE_COPY as copy,
  BILLING_CREDITS_CTA_BUTTON_CLASS,
  BILLING_CREDITS_USAGE_CARD_CLASS,
  BILLING_CREDITS_USAGE_CARD_HEADER_CLASS,
  BILLING_CREDITS_USAGE_CELL_CLASS,
  BILLING_CREDITS_USAGE_GRID_CLASS,
  BILLING_CREDITS_USAGE_TABLE_WRAP_CLASS,
  BILLING_PLAN_METRIC_DIVIDER_CLASS,
  BILLING_PLAN_METRIC_LABEL_CLASS,
  BILLING_PLAN_METRIC_PAIR_CLASS,
  BILLING_PLAN_METRIC_ROW_CLASS,
  BILLING_PLAN_METRIC_STACK_CLASS,
  BILLING_PLAN_METRIC_VALUE_CLASS,
  BILLING_PLAN_OVERVIEW_CARD_CLASS,
  BILLING_PLAN_TAB_STACK_CLASS,
  formatCreditsRemaining,
  formatQrPacksLabel,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { BillingCreditsTabId } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  MANAGE_PLAN_BREADCRUMB_CLASS,
  MANAGE_PLAN_BREADCRUMB_CURRENT_CLASS,
  MANAGE_PLAN_COPY,
  MANAGE_PLAN_PAGE_STACK_CLASS,
  MANAGE_PLAN_SECTION_HEADING_CLASS,
  buildPlanRenewalDateMetric,
  isCancelScheduled,
} from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS,
  ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
  ACCOUNT_WORKSPACE_FULL_BLEED_X,
  ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS,
  ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS,
  ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS,
  ACCOUNT_WORKSPACE_PAGE_COPY,
  ACCOUNT_WORKSPACE_PAGE_STACK_CLASS,
  ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS,
  ACCOUNT_WORKSPACE_SELECT_MENU_CLASS,
  ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
  ACCOUNT_WORKSPACE_SHELL_PAD_X,
  ACCOUNT_WORKSPACE_TAB_BODY_CLASS,
  ACCOUNT_WORKSPACE_TAB_LIST_CLASS,
  ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
  ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import type { BillingActivityViewRow } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import type {
  CreditChannelCardViewModel,
  CreditChannelId,
} from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import {
  CAMPAIGNS_MESSAGING_USAGE_METER_FILL_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_METER_ROW_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_METER_TRACK_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import {
  BROWSER_BACK_HREF,
  registerLeaveDirtyGuard,
} from "@/lib/operatorNavigation/leaveDirtyGuard"
import {
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

function PlanMetricPair({
  label,
  value,
}: {
  label: string
  value: ReactNode
}) {
  return (
    <div className={BILLING_PLAN_METRIC_PAIR_CLASS}>
      <p className={BILLING_PLAN_METRIC_LABEL_CLASS}>{label}</p>
      {typeof value === "string" ? (
        <p className={BILLING_PLAN_METRIC_VALUE_CLASS}>{value}</p>
      ) : (
        value
      )}
    </div>
  )
}

function PlanSubscriptionBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  if (snap.loadStatus === "idle" || snap.loadStatus === "loading") {
    return (
      <div className="flex justify-center py-16" role="status">
        <Spinner />
      </div>
    )
  }

  if (snap.loadStatus === "error") {
    return (
      <div className={GUESTS_SECTION_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.loadError}</h2>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void pageModule.load()
          }}
        >
          {copy.retry}
        </Button>
      </div>
    )
  }

  const plan = snap.planSubscription
  if (plan == null) {
    return null
  }

  const planRenewalDateMetric = buildPlanRenewalDateMetric(plan, {
    renewalDate: copy.renewalDate,
    cancelDate: copy.cancelDate,
  })
  const billingCycleLabel = plan.billingCycle ?? "—"
  const planPrice = plan.isPilot
    ? plan.planPriceNet
    : `${plan.planPriceNet} ${copy.plusVat}`

  return (
    <div className={BILLING_PLAN_TAB_STACK_CLASS}>
      <section className={BILLING_PLAN_OVERVIEW_CARD_CLASS}>
        <div className={BILLING_PLAN_METRIC_STACK_CLASS}>
          <div className={BILLING_PLAN_METRIC_ROW_CLASS}>
            <PlanMetricPair
              label={copy.currentPlan}
              value={plan.subscriptionPlan}
            />
            <PlanMetricPair
              label={copy.smsCredits}
              value={formatCreditsRemaining(
                plan.smsCreditsRemaining,
                "remaining"
              )}
            />
          </div>
          <hr className={BILLING_PLAN_METRIC_DIVIDER_CLASS} />
          <div className={BILLING_PLAN_METRIC_ROW_CLASS}>
            <PlanMetricPair
              label={copy.billingStatus}
              value={
                <Badge variant="soft">{plan.billingStatus}</Badge>
              }
            />
            <PlanMetricPair
              label={copy.aiCredits}
              value={formatCreditsRemaining(
                plan.aiCreditsRemaining,
                "remaining"
              )}
            />
          </div>
          <hr className={BILLING_PLAN_METRIC_DIVIDER_CLASS} />
          <div className={BILLING_PLAN_METRIC_ROW_CLASS}>
            <PlanMetricPair
              label={planRenewalDateMetric.label}
              value={planRenewalDateMetric.value}
            />
            <PlanMetricPair
              label={copy.qrPacks}
              value={formatQrPacksLabel(plan.starterKitState)}
            />
          </div>
        </div>

        {plan.scheduledChangeLine != null && !isCancelScheduled(plan) ? (
          <p className={cn(ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS, "mt-5")}>
            {plan.scheduledChangeLine}
          </p>
        ) : null}

        {plan.isPilot ? (
          <p className={cn(ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS, "mt-5")}>
            {copy.pilotNotice}
          </p>
        ) : null}
      </section>

      <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.currentPlan}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.currentPlanSubtitle}
          </p>
        </div>

        <div className={BILLING_PLAN_METRIC_STACK_CLASS}>
          <div className={BILLING_PLAN_METRIC_ROW_CLASS}>
            <PlanMetricPair
              label={copy.planName}
              value={plan.subscriptionPlan}
            />
            <PlanMetricPair
              label={copy.includedEmailCredits}
              value={plan.includedEmailCreditsLabel}
            />
          </div>
          <hr className={BILLING_PLAN_METRIC_DIVIDER_CLASS} />
          <div className={BILLING_PLAN_METRIC_ROW_CLASS}>
            <PlanMetricPair
              label={copy.billingCycle}
              value={billingCycleLabel}
            />
            <PlanMetricPair
              label={copy.includedSmsCredits}
              value={plan.includedSmsCreditsLabel}
            />
          </div>
          <hr className={BILLING_PLAN_METRIC_DIVIDER_CLASS} />
          <div className={BILLING_PLAN_METRIC_ROW_CLASS}>
            <PlanMetricPair label={copy.planPrice} value={planPrice} />
            <PlanMetricPair
              label={copy.includedAiCredits}
              value={plan.includedAiCreditsLabel}
            />
          </div>
          <hr className={BILLING_PLAN_METRIC_DIVIDER_CLASS} />
          <div className={BILLING_PLAN_METRIC_ROW_CLASS}>
            <PlanMetricPair
              label={copy.includedLocations}
              value={String(plan.includedLocations)}
            />
            <PlanMetricPair
              label={copy.qrStarterKit}
              value={copy.qrStarterKitIncluded}
            />
          </div>
          <hr className={BILLING_PLAN_METRIC_DIVIDER_CLASS} />
          <div className={BILLING_PLAN_METRIC_ROW_CLASS}>
            <PlanMetricPair
              label={copy.activeLocations}
              value={String(plan.activeLocations)}
            />
            <PlanMetricPair
              label={planRenewalDateMetric.label}
              value={planRenewalDateMetric.value}
            />
          </div>
        </div>

        {snap.showChangePlan ? (
          <div>
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              onClick={() => {
                pageModule.openChangePlan()
              }}
            >
              {copy.changePlan}
            </Button>
          </div>
        ) : null}
      </section>
    </div>
  )
}

function UsageMeter({
  fillRatio,
  maxLabel,
}: {
  fillRatio: number
  maxLabel: string
}) {
  const clamped = Math.min(1, Math.max(0, fillRatio))

  return (
    <div className={CAMPAIGNS_MESSAGING_USAGE_METER_ROW_CLASS}>
      <div
        className={CAMPAIGNS_MESSAGING_USAGE_METER_TRACK_CLASS}
        role="meter"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(clamped * 100)}
      >
        <div
          className={CAMPAIGNS_MESSAGING_USAGE_METER_FILL_CLASS}
          style={{ width: `${clamped * 100}%` }}
        />
      </div>
      <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>{maxLabel}</p>
    </div>
  )
}

function channelIcon(channel: CreditChannelCardViewModel["channel"]) {
  switch (channel) {
    case "email":
      return MailIcon
    case "sms":
      return MessageSquareIcon
    case "ai":
      return CoinsIcon
  }
}

function CreditChannelCard({
  card,
  onBuy,
  onChangePlan,
  onViewUsage,
}: {
  card: CreditChannelCardViewModel
  onBuy: () => void
  onChangePlan: () => void
  onViewUsage: () => void
}) {
  const Icon = channelIcon(card.channel)

  return (
    <div className={BILLING_CREDITS_USAGE_CELL_CLASS}>
      <div className="flex w-full flex-col gap-3">
        <div className="flex flex-col gap-[18px]">
          <Icon className="size-5 text-op-card-title-color" aria-hidden />
          <div className="flex flex-col gap-1">
            <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
              {card.title}
            </p>
            <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
              {card.headline}
            </p>
          </div>
        </div>
        <UsageMeter fillRatio={card.fillRatio} maxLabel={card.meterMaxLabel} />
      </div>
      <div className="flex flex-wrap items-center gap-2">
        {card.showViewUsage ? (
          <Button
            type="button"
            variant="op-secondary"
            className={cn(
              GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
              BILLING_CREDITS_CTA_BUTTON_CLASS
            )}
            onClick={onViewUsage}
          >
            {copy.viewUsage}
          </Button>
        ) : null}
        {card.showBuy ? (
          <Button
            type="button"
            variant="op-secondary"
            className={cn(
              GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
              BILLING_CREDITS_CTA_BUTTON_CLASS
            )}
            onClick={onBuy}
          >
            {card.buyLabel}
          </Button>
        ) : null}
        {card.showChangePlan ? (
          <Button
            type="button"
            variant="op-link"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={onChangePlan}
          >
            {copy.changePlan}
          </Button>
        ) : null}
      </div>
    </div>
  )
}

function QrPrintPacksUsageCell({
  starterKitState,
  onReorder,
}: {
  starterKitState: string
  onReorder: () => void
}) {
  return (
    <div className={BILLING_CREDITS_USAGE_CELL_CLASS}>
      <div className="flex w-full flex-col gap-3">
        <div className="flex flex-col gap-[18px]">
          <PackageIcon
            className="size-5 text-op-card-title-color"
            aria-hidden
          />
          <div className="flex flex-col gap-1">
            <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
              {copy.qrPrintPacksTitle}
            </p>
            <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
              {formatQrPacksLabel(starterKitState)}
            </p>
          </div>
        </div>
      </div>
      <Button
        type="button"
        variant="op-secondary"
        className={cn(
          GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
          BILLING_CREDITS_CTA_BUTTON_CLASS
        )}
        onClick={onReorder}
      >
        {copy.reorderPrintPack}
      </Button>
    </div>
  )
}

function CreditsUsageBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  if (snap.loadStatus === "idle" || snap.loadStatus === "loading") {
    return (
      <div className="flex justify-center py-16" role="status">
        <Spinner />
      </div>
    )
  }

  if (snap.loadStatus === "error") {
    return (
      <div className={GUESTS_SECTION_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.loadError}</h2>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void pageModule.load()
          }}
        >
          {copy.retry}
        </Button>
      </div>
    )
  }

  const usage = snap.creditsUsage
  if (usage == null) {
    return null
  }

  const orderedCards = (["sms", "email", "ai"] as const)
    .map((channel) => snap.channelCards.find((card) => card.channel === channel))
    .filter((card): card is CreditChannelCardViewModel => card != null)

  const scrollToUsageTable = () => {
    document
      .getElementById("credits-usage-table")
      ?.scrollIntoView({ behavior: "smooth", block: "start" })
  }

  const scrollToCreditTopUps = (channel?: CreditChannelId) => {
    if (channel != null) {
      pageModule.setFocusedTopUpChannelFromUrl(channel)
    }
    const targetId =
      channel != null ? `credit-top-up-${channel}` : "credit-top-ups"
    document
      .getElementById(targetId)
      ?.scrollIntoView({ behavior: "smooth", block: "start" })
  }

  return (
    <div className={BILLING_PLAN_TAB_STACK_CLASS}>
      <section className={BILLING_CREDITS_USAGE_CARD_CLASS}>
        <div className={BILLING_CREDITS_USAGE_CARD_HEADER_CLASS}>
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.creditsUsageTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.creditsUsageSubtitle}
          </p>
        </div>

        <hr className={BILLING_PLAN_METRIC_DIVIDER_CLASS} />

        <div className={BILLING_CREDITS_USAGE_GRID_CLASS}>
          {orderedCards.map((card) => (
            <CreditChannelCard
              key={card.channel}
              card={card}
              onBuy={() => {
                scrollToCreditTopUps(card.channel)
              }}
              onChangePlan={() => {
                pageModule.openChangePlan()
              }}
              onViewUsage={scrollToUsageTable}
            />
          ))}
          <QrPrintPacksUsageCell
            starterKitState={usage.starterKitState}
            onReorder={() => {
              scrollToCreditTopUps()
            }}
          />
        </div>

        <div
          id="credits-usage-table"
          className={BILLING_CREDITS_USAGE_TABLE_WRAP_CLASS}
        >
          <div className={GUESTS_TABLE_FRAME_CLASS}>
            <Table className={GUESTS_TABLE_CLASS}>
              <TableHeader className="[&_tr]:border-0">
                <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    {copy.creditsUsageTableUsageType}
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    {copy.creditsUsageTableThisCycle}
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    {copy.creditsUsageTableIncluded}
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    {copy.creditsUsageTableExtraUsed}
                  </TableHead>
                  <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                    {copy.creditsUsageTableEstimatedCharge}
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {snap.usageTableRows.map((row) => (
                  <TableRow
                    key={row.channelLabel}
                    className={GUESTS_TABLE_BODY_ROW_CLASS}
                  >
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className="text-sm font-semibold leading-[19px] text-foreground">
                        {row.channelLabel}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className="text-sm font-normal leading-[19px] text-foreground">
                        {row.usedThisCycle}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className="text-sm font-normal leading-[19px] text-foreground">
                        {row.includedThisPeriod}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className="text-sm font-normal leading-[19px] text-foreground">
                        {row.extraUsed}
                      </span>
                    </TableCell>
                    <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      <span className="text-sm font-normal leading-[19px] text-foreground">
                        {row.estimatedCharge}
                      </span>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </div>
      </section>

      <section
        id="credit-top-ups"
        className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}
      >
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.creditTopUpsTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.creditTopUpsSubtitle}
          </p>
        </div>
        <CreditTopUpsSection snap={snap} pageModule={pageModule} />
      </section>
    </div>
  )
}

function PaymentInvoicesBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  if (snap.loadStatus === "idle" || snap.loadStatus === "loading") {
    return (
      <div className="flex justify-center py-16" role="status">
        <Spinner />
      </div>
    )
  }

  if (snap.loadStatus === "error") {
    return (
      <div className={GUESTS_SECTION_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.loadError}</h2>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void pageModule.load()
          }}
        >
          {copy.retry}
        </Button>
      </div>
    )
  }

  return (
    <div className={BILLING_PLAN_TAB_STACK_CLASS}>
      <section
        id="update-payment-method"
        className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}
      >
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.paymentMethodTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.paymentMethodSubtitle}
          </p>
        </div>

        {snap.showNoPaymentMethodOnFile ? (
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.noPaymentMethodOnFile}
          </p>
        ) : null}

        {snap.showUpdatePaymentMethod ? (
          <Button
            type="button"
            variant="op-tertiary"
            className={BILLING_CREDITS_CTA_BUTTON_CLASS}
            disabled={snap.updatePaymentMethodDisabled}
            onClick={() => {
              pageModule.openUpdatePaymentMethodConfirm()
            }}
          >
            {copy.updatePaymentMethod}
          </Button>
        ) : null}
      </section>

      <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.invoicesTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.invoicesSubtitle}
          </p>
        </div>

        {snap.showNoInvoicesYet ? (
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.noInvoicesYet}
          </p>
        ) : (
          <PaymentInvoicesTable
            rows={snap.invoices}
            onView={(invoiceNo) => {
              void pageModule.viewInvoicePdf(invoiceNo)
            }}
            onDownload={(invoiceNo) => {
              void pageModule.downloadInvoicePdf(invoiceNo)
            }}
          />
        )}
      </section>

      <UpdatePaymentMethodConfirmDialog
        open={snap.updatePaymentMethodConfirmOpen}
        title={snap.updatePaymentMethodConfirmCopy.title}
        body={snap.updatePaymentMethodConfirmCopy.body}
        continueLabel={snap.updatePaymentMethodConfirmCopy.continueLabel}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.dismissUpdatePaymentMethodConfirm()
          }
        }}
        onContinue={() => {
          void pageModule.confirmUpdatePaymentMethod()
        }}
        onCancel={() => {
          pageModule.dismissUpdatePaymentMethodConfirm()
        }}
      />
    </div>
  )
}

function BillingActivityRows({
  rows,
  className,
}: {
  rows: BillingActivityViewRow[]
  className?: string
}) {
  return (
    <ul
      className={cn(
        "m-0 flex list-none flex-col gap-[22px] p-0",
        className
      )}
    >
      {rows.map((row, index) => (
        <li
          key={row.id}
          className={
            index === 0
              ? "flex flex-col gap-2"
              : "flex flex-col gap-2 border-t border-op-border-default pt-[22px]"
          }
        >
          <p className="m-0 text-sm font-medium leading-[19px] text-foreground">
            {row.occurredAtLabel}
          </p>
          <p className="m-0 text-sm font-medium leading-[19px] text-op-card-subtitle-color">
            {row.sentence}
          </p>
        </li>
      ))}
    </ul>
  )
}

function BillingActivityBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  if (snap.loadStatus === "idle" || snap.loadStatus === "loading") {
    return (
      <div className="flex justify-center py-16" role="status">
        <Spinner />
      </div>
    )
  }

  if (snap.loadStatus === "error") {
    return (
      <div className={GUESTS_SECTION_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.loadError}</h2>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void pageModule.load()
          }}
        >
          {copy.retry}
        </Button>
      </div>
    )
  }

  return (
    <div className={BILLING_PLAN_TAB_STACK_CLASS}>
      <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.billingActivityTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.billingActivitySubtitle}
          </p>
        </div>

        {snap.billingActivityEmpty ? (
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.billingActivityEmpty}
          </p>
        ) : (
          <>
            <BillingActivityRows rows={snap.billingActivityPreview} />
            <Button
              type="button"
              variant="op-secondary"
              className={BILLING_CREDITS_CTA_BUTTON_CLASS}
              onClick={() => {
                void pageModule.openBillingActivityHistory()
              }}
            >
              {copy.viewFullBillingHistory}
            </Button>
          </>
        )}
      </section>

      <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.subscriptionChangesTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.subscriptionChangesSubtitle}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3.5">
          <Button
            type="button"
            variant="op-secondary"
            className={BILLING_CREDITS_CTA_BUTTON_CLASS}
            onClick={() => {
              pageModule.openManagePlan()
            }}
          >
            {copy.downgradePlan}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={BILLING_CREDITS_CTA_BUTTON_CLASS}
            disabled={!snap.showCancelPlan}
            onClick={() => {
              pageModule.requestCancelPlan()
            }}
          >
            {copy.cancelSubscription}
          </Button>
          <Button
            type="button"
            variant="op-tertiary"
            className={BILLING_CREDITS_CTA_BUTTON_CLASS}
            asChild
          >
            <Link to={HELP_CENTRE_CONTACT_URL}>
              {copy.contactBillingSupport}
            </Link>
          </Button>
        </div>
      </section>

      <Sheet
        open={snap.billingActivityHistoryOpen}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeBillingActivityHistory()
          }
        }}
      >
        <SheetContent
          side="right"
          className="w-full sm:max-w-lg"
          showCloseButton
        >
          <SheetHeader>
            <SheetTitle className="text-xl font-bold">
              {copy.billingActivitySheetTitle}
            </SheetTitle>
          </SheetHeader>
          <BillingActivityRows
            rows={snap.billingActivityHistoryRows}
            className="overflow-y-auto p-4"
          />
          {snap.billingActivityHistoryHasPrevious
          || snap.billingActivityHistoryHasNext ? (
            <div className="flex gap-3 p-4">
              <Button
                type="button"
                variant="op-tertiary"
                disabled={!snap.billingActivityHistoryHasPrevious}
                onClick={() => {
                  void pageModule.goToPreviousBillingActivityHistoryPage()
                }}
              >
                Previous
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                disabled={!snap.billingActivityHistoryHasNext}
                onClick={() => {
                  void pageModule.goToNextBillingActivityHistoryPage()
                }}
              >
                Next
              </Button>
            </div>
          ) : null}
        </SheetContent>
      </Sheet>
    </div>
  )
}

const BILLING_FIELD_STACK_CLASS = "flex w-full max-w-[510px] flex-col gap-2"
const BILLING_FORM_STACK_CLASS = "flex w-full max-w-[510px] flex-col gap-7"
const BILLING_ALERT_STACK_CLASS = "flex flex-col gap-3.5"

function BillingContactMemberSelect({
  id,
  label,
  value,
  members,
  disabled = false,
  onValueChange,
}: {
  id: string
  label: string
  value: number
  members: ReadonlyArray<{ userId: number; fullName: string; email: string }>
  disabled?: boolean
  onValueChange?: (userId: number) => void
}) {
  const selected = members.find((member) => member.userId === value)
  const selectValue = value > 0 ? String(value) : undefined

  return (
    <div className={BILLING_FIELD_STACK_CLASS}>
      <label htmlFor={id} className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}>
        {label}
      </label>
      <Select
        value={selectValue}
        disabled={disabled}
        onValueChange={(next) => {
          const parsed = Number(next)
          if (!Number.isFinite(parsed)) {
            return
          }
          onValueChange?.(parsed)
        }}
      >
        <SelectTrigger
          id={id}
          className={ACCOUNT_WORKSPACE_SELECT_TRIGGER_CLASS}
        >
          <SelectValue placeholder={copy.selectUserPlaceholder}>
            {selected?.fullName ?? copy.selectUserPlaceholder}
          </SelectValue>
        </SelectTrigger>
        <SelectContent
          position="popper"
          align="start"
          className={ACCOUNT_WORKSPACE_SELECT_MENU_CLASS}
        >
          {members.map((member) => (
            <SelectItem
              key={member.userId}
              value={String(member.userId)}
              className={ACCOUNT_WORKSPACE_SELECT_ITEM_CLASS}
            >
              <span className="flex flex-col gap-0.5 text-left">
                <span>{member.fullName}</span>
                <span className="text-xs font-normal text-muted-foreground">
                  {member.email}
                </span>
              </span>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  )
}

function BillingContactsBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
}) {
  if (snap.loadStatus === "idle" || snap.loadStatus === "loading") {
    return (
      <div className="flex justify-center py-16" role="status">
        <Spinner />
      </div>
    )
  }

  if (snap.loadStatus === "error") {
    return (
      <div className={GUESTS_SECTION_CLASS}>
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.loadError}</h2>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void pageModule.load()
          }}
        >
          {copy.retry}
        </Button>
      </div>
    )
  }

  const contacts = snap.billingContacts
  const readOnly = !snap.actorCanPersistBillingContacts

  return (
    <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
      <div className="flex flex-col gap-2">
        <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
          {copy.billingContactsTitle}
        </h2>
        <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
          {copy.billingContactsSubtitle}
        </p>
      </div>

      <div className={BILLING_FORM_STACK_CLASS}>
        <BillingContactMemberSelect
          id="billing-contact-user"
          label={copy.billingContact}
          value={contacts.billingContactUserId}
          members={contacts.eligibleMembers}
          disabled={readOnly}
          onValueChange={(userId) => {
            pageModule.setBillingContactUserId(userId)
          }}
        />

        <hr aria-hidden className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

        <div className={BILLING_FIELD_STACK_CLASS}>
          <label
            htmlFor="billing-email"
            className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}
          >
            {copy.billingEmail}
          </label>
          <Input
            id="billing-email"
            type="email"
            value={contacts.billingEmail}
            placeholder={copy.billingEmailPlaceholder}
            disabled={readOnly}
            className={ACCOUNT_WORKSPACE_TEXT_INPUT_CLASS}
            onChange={(event) => {
              pageModule.setBillingEmail(event.target.value)
            }}
          />
        </div>

        <hr aria-hidden className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

        <div className={BILLING_ALERT_STACK_CLASS}>
          <p className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}>
            {copy.lowCreditAlerts}
          </p>
          <div className={BILLING_ALERT_STACK_CLASS}>
            <CheckboxLabel
              checked={contacts.lowCreditAlerts.owner}
              disabled={readOnly}
              onCheckedChange={(checked) => {
                pageModule.setLowCreditAlertOwner(checked)
              }}
            >
              {copy.alertOwner}
            </CheckboxLabel>
            <CheckboxLabel
              checked={contacts.lowCreditAlerts.admin}
              disabled={readOnly}
              onCheckedChange={(checked) => {
                pageModule.setLowCreditAlertAdmin(checked)
              }}
            >
              {copy.alertAdmin}
            </CheckboxLabel>
            <CheckboxLabel
              checked={contacts.lowCreditAlerts.billingContact}
              disabled={readOnly}
              onCheckedChange={(checked) => {
                pageModule.setLowCreditAlertBillingContact(checked)
              }}
            >
              {copy.alertBillingContact}
            </CheckboxLabel>
          </div>
        </div>

        <hr aria-hidden className={ACCOUNT_WORKSPACE_IDENTITY_DIVIDER_CLASS} />

        <div className={BILLING_ALERT_STACK_CLASS}>
          <p className={ACCOUNT_WORKSPACE_FIELD_LABEL_CLASS}>
            {copy.paymentFailureAlerts}
          </p>
          <div className={BILLING_ALERT_STACK_CLASS}>
            <CheckboxLabel
              checked={contacts.paymentFailureAlerts.owner}
              disabled={readOnly}
              onCheckedChange={(checked) => {
                pageModule.setPaymentFailureAlertOwner(checked)
              }}
            >
              {copy.alertOwner}
            </CheckboxLabel>
            <CheckboxLabel
              checked={contacts.paymentFailureAlerts.billingContact}
              disabled={readOnly}
              onCheckedChange={(checked) => {
                pageModule.setPaymentFailureAlertBillingContact(checked)
              }}
            >
              {copy.alertBillingContact}
            </CheckboxLabel>
          </div>
        </div>
      </div>

      {snap.actorCanPersistBillingContacts ? (
        <Button
          type="button"
          variant="op-secondary"
          className={BILLING_CREDITS_CTA_BUTTON_CLASS}
          disabled={!snap.saveEnabled}
          onClick={() => {
            void pageModule.persistBillingContacts()
          }}
        >
          {copy.updateBillingContact}
        </Button>
      ) : null}
    </section>
  )
}

function BillingCreditsHeaderActions({
  snap,
  pageModule,
  onScrollToPlanCards,
}: {
  snap: ReturnType<
    ReturnType<typeof useBillingCreditsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useBillingCreditsPageModuleApi>
  onScrollToPlanCards?: () => void
}) {
  if (!snap.showManagePlan && !snap.showBuyCredits) {
    return null
  }

  return (
    <div className="flex flex-wrap items-center gap-3">
      {snap.showManagePlan ? (
        <Button
          type="button"
          variant="op-primary"
          className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
          onClick={() => {
            if (snap.surface === "manage-plan") {
              pageModule.scrollManagePlanToCards()
              onScrollToPlanCards?.()
              return
            }
            pageModule.openManagePlan()
          }}
        >
          {copy.managePlan}
        </Button>
      ) : null}
      {snap.showBuyCredits ? (
        <Button
          type="button"
          variant="op-secondary"
          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
          disabled={snap.buyCreditsDisabled}
          onClick={() => {
            if (
              snap.surface === "tabs"
              && snap.activeTabId === "credits-usage"
            ) {
              document
                .getElementById("credit-top-ups")
                ?.scrollIntoView({ behavior: "smooth", block: "start" })
              return
            }
            pageModule.openBuyCredits()
          }}
        >
          {copy.buyCredits}
        </Button>
      ) : null}
    </div>
  )
}

export function BillingCreditsPage() {
  const pageModule = useBillingCreditsPageModuleApi()
  const snap = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()

  useEffect(() => {
    const current = searchParams.get("tab")
    if (current === snap.activeTabId) {
      return
    }
    const next = new URLSearchParams(searchParams)
    next.set("tab", snap.activeTabId)
    setSearchParams(next, { replace: true })
  }, [snap.activeTabId, searchParams, setSearchParams])

  useEffect(() => {
    if (snap.pendingNavigationHref == null) {
      return
    }
    const href = pageModule.consumePendingNavigation()
    if (href == null) {
      return
    }
    if (href === BROWSER_BACK_HREF) {
      navigate(-1)
      return
    }
    navigate(href)
  }, [snap.pendingNavigationHref, pageModule, navigate])

  useEffect(() => {
    registerLeaveDirtyGuard({
      isBlocked: () => pageModule.getSnapshot().isDirty,
      requestLeave: (href) => pageModule.requestNavigateAway(href),
    })
    return () => {
      registerLeaveDirtyGuard(null)
    }
  }, [pageModule])

  useEffect(() => {
    if (snap.loadStatus !== "loaded") {
      return
    }
    if (window.location.hash !== "#update-payment-method") {
      return
    }
    document
      .getElementById("update-payment-method")
      ?.scrollIntoView({ behavior: "smooth", block: "start" })
  }, [snap.loadStatus, snap.activeTabId])

  useEffect(() => {
    if (!snap.isDirty) {
      return
    }

    const onPopState = () => {
      if (!pageModule.getSnapshot().isDirty) {
        return
      }
      window.history.pushState(null, "", window.location.href)
      pageModule.requestNavigateAway(BROWSER_BACK_HREF)
    }

    window.history.pushState(null, "", window.location.href)
    window.addEventListener("popstate", onPopState)
    return () => {
      window.removeEventListener("popstate", onPopState)
    }
  }, [snap.isDirty, pageModule])

  return (
    <div className={ACCOUNT_WORKSPACE_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
        </div>
        <BillingCreditsHeaderActions snap={snap} pageModule={pageModule} />
      </div>

      <Tabs
        value={snap.activeTabId}
        onValueChange={(value) => {
          pageModule.requestTabChange(value as BillingCreditsTabId)
        }}
        className="flex min-h-0 flex-1 flex-col gap-0"
      >
        <div
          className={cn(
            ACCOUNT_WORKSPACE_FULL_BLEED_X,
            ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
            "shrink-0"
          )}
        >
          <div className={ACCOUNT_WORKSPACE_SHELL_PAD_X}>
            <TabsList
              variant="line"
              className={ACCOUNT_WORKSPACE_TAB_LIST_CLASS}
            >
              {snap.tabs.map((tab) => (
                <TabsTrigger
                  key={tab.id}
                  value={tab.id}
                  className={ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS}
                >
                  {tab.label}
                </TabsTrigger>
              ))}
            </TabsList>
          </div>
        </div>

        <div
          className={cn(
            ACCOUNT_WORKSPACE_FULL_BLEED_X,
            ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
            ACCOUNT_WORKSPACE_SHELL_PAD_X,
            ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
            ACCOUNT_WORKSPACE_TAB_BODY_CLASS
          )}
        >
          <TabsContent value="plan-subscription" className="mt-0">
            <PlanSubscriptionBody snap={snap} pageModule={pageModule} />
          </TabsContent>
          <TabsContent value="credits-usage" className="mt-0">
            <CreditsUsageBody snap={snap} pageModule={pageModule} />
          </TabsContent>
          <TabsContent value="payment-invoices" className="mt-0">
            <PaymentInvoicesBody snap={snap} pageModule={pageModule} />
          </TabsContent>
          <TabsContent value="billing-contacts" className="mt-0">
            <BillingContactsBody snap={snap} pageModule={pageModule} />
          </TabsContent>
          <TabsContent value="activity" className="mt-0">
            <BillingActivityBody snap={snap} pageModule={pageModule} />
          </TabsContent>
        </div>
      </Tabs>

      <AccountWorkspaceConfirmDialog
        open={snap.leaveDirtyOpen}
        title={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtyTitle}
        body={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtyBody}
        primaryLabel={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtySave}
        busy={snap.isSaving}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeLeaveDirty()
          }
        }}
        onPrimary={() => {
          void pageModule.confirmLeaveDirtySave()
        }}
        onCancel={() => {
          pageModule.confirmLeaveDirtyCancel()
        }}
      />

      <CancelSubscriptionDialog
        cancelPlanConfirm={snap.cancelPlanConfirm}
        pageModule={pageModule}
      />
    </div>
  )
}

export function ManagePlanPage() {
  const pageModule = useBillingCreditsPageModuleApi()
  const snap = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  const scrollToPlanCards = () => {
    const next = new URLSearchParams(searchParams)
    next.delete("section")
    navigate(
      { pathname: ".", search: next.toString() === "" ? "" : `?${next.toString()}` },
      { replace: true }
    )
    document.getElementById("plan-cards")?.scrollIntoView({ behavior: "smooth" })
  }

  useEffect(() => {
    if (snap.pendingNavigationHref == null) {
      return
    }
    const href = pageModule.consumePendingNavigation()
    if (href == null) {
      return
    }
    navigate(href)
  }, [snap.pendingNavigationHref, pageModule, navigate])

  useEffect(() => {
    if (snap.pendingPayRedirectUrl == null) {
      return
    }
    const url = pageModule.consumePendingPayRedirect()
    if (url == null) {
      return
    }
    window.location.assign(url)
  }, [snap.pendingPayRedirectUrl, pageModule])

  useEffect(() => {
    if (snap.focusedTopUpChannel == null) {
      return
    }
    document
      .getElementById(`credit-top-up-${snap.focusedTopUpChannel}`)
      ?.scrollIntoView({ behavior: "smooth", block: "start" })
  }, [snap.focusedTopUpChannel, snap.loadStatus])

  useEffect(() => {
    if (snap.pendingPaymentMethodRedirectUrl == null) {
      return
    }
    const href = pageModule.consumePendingPaymentMethodRedirect()
    if (href == null) {
      return
    }
    window.location.assign(href)
  }, [snap.pendingPaymentMethodRedirectUrl, pageModule])

  useEffect(() => {
    if (
      snap.loadStatus !== "loaded"
      || !pageModule.shouldAutoOpenCreditTopUps()
    ) {
      return
    }
    pageModule.openBuyCredits()
  }, [
    snap.loadStatus,
    snap.accessLevel,
    snap.actorPermissionRole,
    snap.managePlanSection,
    pageModule,
  ])

  useEffect(() => {
    if (snap.loadStatus !== "loaded") {
      return
    }
    if (window.location.hash !== "#plan-cards") {
      return
    }
    document
      .getElementById("plan-cards")
      ?.scrollIntoView({ behavior: "smooth", block: "start" })
  }, [snap.loadStatus, snap.showPlanCards])

  return (
    <div className={MANAGE_PLAN_PAGE_STACK_CLASS}>
      <nav
        aria-label="Breadcrumb"
        className={MANAGE_PLAN_BREADCRUMB_CLASS}
      >
        {snap.breadcrumbHref != null ? (
          <Link
            to={snap.breadcrumbHref}
            className="text-foreground hover:underline"
          >
            {copy.breadcrumbBillingCredits}
          </Link>
        ) : (
          <span className="text-foreground">{copy.breadcrumbBillingCredits}</span>
        )}
        <ChevronRightIcon
          className="size-4 shrink-0 text-muted-foreground"
          aria-hidden
        />
        <span className={MANAGE_PLAN_BREADCRUMB_CURRENT_CLASS}>
          {copy.breadcrumbManagePlan}
        </span>
      </nav>

      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>
            {MANAGE_PLAN_COPY.pageTitle}
          </h1>
          <p className={ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS}>
            {MANAGE_PLAN_COPY.pageSubtitle}
          </p>
        </div>
        <BillingCreditsHeaderActions
          snap={snap}
          pageModule={pageModule}
          onScrollToPlanCards={scrollToPlanCards}
        />
      </div>

      {snap.loadStatus === "idle" || snap.loadStatus === "loading" ? (
        <div className="flex justify-center py-16" role="status">
          <Spinner />
        </div>
      ) : snap.loadStatus === "error" ? (
        <div className={GUESTS_SECTION_CLASS}>
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.loadError}</h2>
          <Button
            type="button"
            variant="op-secondary"
            onClick={() => {
              void pageModule.load()
            }}
          >
            {copy.retry}
          </Button>
        </div>
      ) : (
        <div className="flex flex-col gap-15">
          <div id="plan-cards">
            <ManagePlanCardsSection snap={snap} pageModule={pageModule} />
          </div>

          {snap.additionalGroupLocation != null ? (
            <ManagePlanAdditionalGroupLocationSection
              viewModel={snap.additionalGroupLocation}
              showActions={snap.showOwnerManagePlanWrites}
              actionsEnabled={snap.ownerManagePlanWritesEnabled}
              pageModule={pageModule}
              extraLocationConfirm={snap.extraLocationConfirm}
            />
          ) : null}

          <section
            id="credit-top-ups"
            className="flex flex-col gap-15"
          >
            <h2 className={cn(MANAGE_PLAN_SECTION_HEADING_CLASS, "max-w-[640px]")}>
              {MANAGE_PLAN_COPY.capacityHeading}
            </h2>
            <CreditTopUpsSection snap={snap} pageModule={pageModule} />
          </section>

          <ManagePlanFaqSection />
        </div>
      )}
    </div>
  )
}

import { useEffect, useSyncExternalStore } from "react"
import {
  BotIcon,
  ChevronRightIcon,
  MailIcon,
  MessageSquareIcon,
  PackageIcon,
} from "lucide-react"
import { Link, useNavigate, useSearchParams } from "react-router-dom"

import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import { ManagePlanCardsSection } from "@/components/dashboard/operator/BillingCredits/ManagePlanCardsSection"
import { CreditTopUpsSection } from "@/components/dashboard/operator/BillingCredits/CreditTopUpsSection"
import {
  ManagePlanAdditionalGroupLocationSection,
  ManagePlanCancelPlanControl,
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
import { Separator } from "@/components/ui/separator"
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
import {
  BILLING_CREDITS_PAGE_COPY as copy,
  BILLING_CREDITS_SELECT_MENU_CLASS,
  formatCreditsRemaining,
  formatStarterKitState,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { BillingCreditsTabId } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import { ACCOUNT_WORKSPACE_PAGE_COPY } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import type { BillingActivityViewRow } from "@/lib/operatorBillingCredits/createOperatorBillingCreditsPageModule"
import type { CreditChannelCardViewModel } from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import {
  CAMPAIGNS_MESSAGING_USAGE_METER_FILL_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_METER_ROW_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_METER_TRACK_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS,
  CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import {
  BROWSER_BACK_HREF,
  registerLeaveDirtyGuard,
} from "@/lib/operatorNavigation/leaveDirtyGuard"
import {
  GUESTS_DETAIL_FIELD_CLASS,
  GUESTS_DETAIL_FIELD_LABEL_CLASS,
  GUESTS_DETAIL_FIELD_VALUE_CLASS,
  GUESTS_DETAIL_ROWS_STACK_CLASS,
  GUESTS_KPI_CARD_CLASS,
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"

function SummaryMetric({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className={GUESTS_KPI_CARD_CLASS}>
      <p className="text-xs font-medium text-muted-foreground">{label}</p>
      <p className="text-lg font-semibold text-foreground">{value}</p>
    </div>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div className={GUESTS_DETAIL_FIELD_CLASS}>
      <p className={GUESTS_DETAIL_FIELD_LABEL_CLASS}>{label}</p>
      <p className={GUESTS_DETAIL_FIELD_VALUE_CLASS}>{value}</p>
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

  const renewalLabel =
    plan.renewalDateLabel ?? "—"
  const billingCycleLabel = plan.billingCycle ?? "—"
  const planPrice =
    plan.isPilot
      ? plan.planPriceNet
      : `${plan.planPriceNet} ${copy.plusVat}`

  return (
    <div className="flex flex-col gap-6">
      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex flex-col gap-2">
            <h2 className={GUESTS_SECTION_TITLE_CLASS}>
              {copy.planSubscriptionTitle}
            </h2>
          </div>
          {snap.showChangePlan ? (
            <Button
              type="button"
              variant="op-secondary"
              className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
              onClick={() => {
                pageModule.openChangePlan()
              }}
            >
              {copy.changePlan}
            </Button>
          ) : null}
        </div>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
          <SummaryMetric label={copy.currentPlan} value={plan.subscriptionPlan} />
          <SummaryMetric label={copy.billingStatus} value={plan.billingStatus} />
          <SummaryMetric label={copy.renewalDate} value={renewalLabel} />
          <SummaryMetric
            label={copy.emailCredits}
            value={formatCreditsRemaining(
              plan.emailCreditsRemaining,
              "remaining"
            )}
          />
          <SummaryMetric
            label={copy.smsCredits}
            value={formatCreditsRemaining(
              plan.smsCreditsRemaining,
              "remaining"
            )}
          />
          <SummaryMetric
            label={copy.aiCredits}
            value={formatCreditsRemaining(
              plan.aiCreditsRemaining,
              "remaining"
            )}
          />
        </div>

        {plan.scheduledChangeLine != null ? (
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {plan.scheduledChangeLine}
          </p>
        ) : null}

        {plan.isPilot ? (
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.pilotNotice}</p>
        ) : null}
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <h3 className={GUESTS_SECTION_TITLE_CLASS}>{copy.currentPlan}</h3>
        <div className={GUESTS_DETAIL_ROWS_STACK_CLASS}>
          <DetailRow label={copy.planName} value={plan.subscriptionPlan} />
          <DetailRow label={copy.billingCycle} value={billingCycleLabel} />
          <DetailRow label={copy.planPrice} value={planPrice} />
          <DetailRow
            label={copy.includedLocations}
            value={String(plan.includedLocations)}
          />
          <DetailRow
            label={copy.activeLocations}
            value={String(plan.activeLocations)}
          />
          <DetailRow
            label={copy.includedEmailCredits}
            value={plan.includedEmailCreditsLabel}
          />
          <DetailRow
            label={copy.includedSmsCredits}
            value={plan.includedSmsCreditsLabel}
          />
          <DetailRow
            label={copy.includedAiCredits}
            value={plan.includedAiCreditsLabel}
          />
          <DetailRow
            label={copy.starterKit}
            value={formatStarterKitState(plan.starterKitState)}
          />
          <DetailRow label={copy.renewalDate} value={renewalLabel} />
        </div>
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
      return BotIcon
  }
}

function CreditChannelCard({
  card,
  onBuy,
  onChangePlan,
}: {
  card: CreditChannelCardViewModel
  onBuy: () => void
  onChangePlan: () => void
}) {
  const Icon = channelIcon(card.channel)

  return (
    <div className={CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS}>
      <div className="flex flex-col gap-3">
        <Icon className="size-5 text-op-card-title-color" aria-hidden />
        <div className="flex flex-col gap-1">
          <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
            {card.title}
          </p>
          <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
            {card.headline}
          </p>
          <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
            {card.subline}
          </p>
        </div>
        {card.purchasedLine != null ? (
          <p className="m-0 text-xs font-normal leading-normal text-op-card-subtitle-color">
            {card.purchasedLine}
          </p>
        ) : null}
      </div>
      <UsageMeter fillRatio={card.fillRatio} maxLabel={card.meterMaxLabel} />
      <div className="flex flex-wrap items-center gap-2">
        {card.showBuy ? (
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
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

  return (
    <div className="flex flex-col gap-6">
      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.creditsUsageTitle}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {copy.creditsUsageSubtitle}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-3 xl:grid-cols-2">
          {snap.channelCards.map((card) => (
            <CreditChannelCard
              key={card.channel}
              card={card}
              onBuy={() => {
                pageModule.openBuyChannelCredits(card.channel)
              }}
              onChangePlan={() => {
                pageModule.openChangePlan()
              }}
            />
          ))}

          <div className={CAMPAIGNS_MESSAGING_USAGE_TILE_CLASS}>
            <div className="flex flex-col gap-3">
              <PackageIcon
                className="size-5 text-op-card-title-color"
                aria-hidden
              />
              <div className="flex flex-col gap-1">
                <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_TITLE_CLASS}>
                  {copy.starterKitCardTitle}
                </p>
                <p className={CAMPAIGNS_MESSAGING_USAGE_TILE_BODY_CLASS}>
                  {formatStarterKitState(usage.starterKitState)}
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-2">
          <h3 className={GUESTS_SECTION_TITLE_CLASS}>
            {copy.creditsUsageTableTitle}
          </h3>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{usage.periodLabel}</p>
        </div>

        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{copy.creditsUsageTableChannel}</TableHead>
              <TableHead>{copy.creditsUsageTableUsed}</TableHead>
              <TableHead>{copy.creditsUsageTableIncluded}</TableHead>
              <TableHead>{copy.creditsUsageTablePurchased}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {snap.usageTableRows.map((row) => (
              <TableRow key={row.channelLabel}>
                <TableCell>{row.channelLabel}</TableCell>
                <TableCell>{row.usedThisCycle}</TableCell>
                <TableCell>{row.includedThisPeriod}</TableCell>
                <TableCell>{row.purchasedRemaining}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
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
    <div className="flex flex-col gap-6">
      <section id="update-payment-method" className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.paymentMethodTitle}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {copy.paymentMethodSubtitle}
          </p>
        </div>

        {snap.showNoPaymentMethodOnFile ? (
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {copy.noPaymentMethodOnFile}
          </p>
        ) : (
          <p className={GUESTS_DETAIL_FIELD_VALUE_CLASS}>
            {snap.paymentMethodLabel}
          </p>
        )}

        {snap.showUpdatePaymentMethod ? (
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            disabled={snap.updatePaymentMethodDisabled}
            onClick={() => {
              pageModule.openUpdatePaymentMethodConfirm()
            }}
          >
            {copy.updatePaymentMethod}
          </Button>
        ) : null}
      </section>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.invoicesTitle}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {copy.invoicesSubtitle}
          </p>
        </div>

        {snap.showNoInvoicesYet ? (
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.noInvoicesYet}</p>
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
          <p className="m-0 text-sm font-medium text-foreground">
            {row.occurredAtLabel}
          </p>
          <p className="m-0 text-sm font-medium text-op-card-subtitle-color">
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
    <>
      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.billingActivityTitle}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {copy.billingActivitySubtitle}
          </p>
        </div>

        {snap.billingActivityEmpty ? (
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
            {copy.billingActivityEmpty}
          </p>
        ) : (
          <>
            <BillingActivityRows rows={snap.billingActivityPreview} />
            <Button
              type="button"
              variant="op-secondary"
              className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
              onClick={() => {
                void pageModule.openBillingActivityHistory()
              }}
            >
              {copy.viewFullBillingHistory}
            </Button>
          </>
        )}
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
    </>
  )
}

const BILLING_FIELD_STACK_CLASS = "flex flex-col gap-2"
const BILLING_FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-foreground"
const BILLING_FORM_STACK_CLASS = "flex max-w-[510px] flex-col gap-7"

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
      <label htmlFor={id} className={BILLING_FIELD_LABEL_CLASS}>
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
        <SelectTrigger id={id} className="h-auto min-h-8 w-full py-3">
          <SelectValue placeholder={copy.selectUserPlaceholder}>
            {selected?.fullName ?? copy.selectUserPlaceholder}
          </SelectValue>
        </SelectTrigger>
        <SelectContent
          position="popper"
          align="start"
          className={BILLING_CREDITS_SELECT_MENU_CLASS}
        >
          {members.map((member) => (
            <SelectItem key={member.userId} value={String(member.userId)}>
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
    <section className={GUESTS_SECTION_CLASS}>
      <div className="flex flex-col gap-2">
        <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.billingContactsTitle}</h2>
        <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
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

        <Separator />

        <div className={BILLING_FIELD_STACK_CLASS}>
          <label htmlFor="billing-email" className={BILLING_FIELD_LABEL_CLASS}>
            {copy.billingEmail}
          </label>
          <Input
            id="billing-email"
            type="email"
            value={contacts.billingEmail}
            placeholder={copy.billingEmailPlaceholder}
            disabled={readOnly}
            className="h-auto py-3"
            onChange={(event) => {
              pageModule.setBillingEmail(event.target.value)
            }}
          />
        </div>

        <Separator />

        <div className="flex flex-col gap-3.5">
          <p className={BILLING_FIELD_LABEL_CLASS}>{copy.lowCreditAlerts}</p>
          <div className="flex flex-col gap-3.5">
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

        <Separator />

        <div className="flex flex-col gap-3.5">
          <p className={BILLING_FIELD_LABEL_CLASS}>{copy.paymentFailureAlerts}</p>
          <div className="flex flex-col gap-3.5">
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
          className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
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
    <div className="flex flex-wrap items-center gap-2">
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
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
        </div>
        <BillingCreditsHeaderActions snap={snap} pageModule={pageModule} />
      </div>

      <Tabs
        value={snap.activeTabId}
        onValueChange={(value) => {
          pageModule.requestTabChange(value as BillingCreditsTabId)
        }}
      >
        <TabsList variant="line" className="w-full justify-start">
          {snap.tabs.map((tab) => (
            <TabsTrigger key={tab.id} value={tab.id}>
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>

        <TabsContent value="plan-subscription">
          <PlanSubscriptionBody snap={snap} pageModule={pageModule} />
        </TabsContent>
        <TabsContent value="credits-usage">
          <CreditsUsageBody snap={snap} pageModule={pageModule} />
        </TabsContent>
        <TabsContent value="payment-invoices">
          <PaymentInvoicesBody snap={snap} pageModule={pageModule} />
        </TabsContent>
        <TabsContent value="billing-contacts">
          <BillingContactsBody snap={snap} pageModule={pageModule} />
        </TabsContent>
        <TabsContent value="activity">
          <BillingActivityBody snap={snap} pageModule={pageModule} />
        </TabsContent>
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
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <nav
        aria-label="Breadcrumb"
        className="flex items-center gap-1 text-sm font-medium text-muted-foreground"
      >
        {snap.breadcrumbHref != null ? (
          <Link
            to={snap.breadcrumbHref}
            className="text-foreground hover:underline"
          >
            {copy.breadcrumbBillingCredits}
          </Link>
        ) : (
          <span>{copy.breadcrumbBillingCredits}</span>
        )}
        <ChevronRightIcon className="size-4" aria-hidden />
        <span className="text-foreground">{copy.breadcrumbManagePlan}</span>
      </nav>

      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
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
        <div className="flex flex-col gap-8">
          <section
            id="plan-cards"
            className={cn(GUESTS_SECTION_CLASS, {
              "ring-2 ring-primary/20": snap.managePlanSection == null,
            })}
          >
            <h2 className={GUESTS_SECTION_TITLE_CLASS}>
              {copy.managePlanPlanCards}
            </h2>
            <ManagePlanCardsSection snap={snap} pageModule={pageModule} />
            <ManagePlanCancelPlanControl
              showCancelPlan={snap.showCancelPlan}
              cancelEnabled={snap.ownerManagePlanWritesEnabled}
              pageModule={pageModule}
              cancelPlanConfirm={snap.cancelPlanConfirm}
            />
          </section>
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
            className={cn(GUESTS_SECTION_CLASS, {
              "ring-2 ring-primary/20":
                snap.managePlanSection === "credit-top-ups",
            })}
          >
            <div className="flex items-center gap-2">
              <h2 className={GUESTS_SECTION_TITLE_CLASS}>
                {copy.managePlanCreditTopUps}
              </h2>
              {snap.managePlanSection === "credit-top-ups" ? (
                <Badge variant="secondary">Selected</Badge>
              ) : null}
            </div>
            <CreditTopUpsSection snap={snap} pageModule={pageModule} />
          </section>
        </div>
      )}
    </div>
  )
}

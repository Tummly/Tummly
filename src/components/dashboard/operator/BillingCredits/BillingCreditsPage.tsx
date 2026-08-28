import { useEffect, useSyncExternalStore } from "react"
import { ChevronRightIcon } from "lucide-react"
import { Link, useNavigate, useSearchParams } from "react-router-dom"

import { useBillingCreditsPageModuleApi } from "@/components/dashboard/operator/BillingCredits/utils/billingCreditsPageModuleContext"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Spinner } from "@/components/ui/spinner"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  BILLING_CREDITS_PAGE_COPY as copy,
  formatCreditsRemaining,
  formatStarterKitState,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { BillingCreditsTabId } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
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

function EmptyTabBody({ label }: { label: string }) {
  return (
    <section className={GUESTS_SECTION_CLASS}>
      <h2 className={GUESTS_SECTION_TITLE_CLASS}>{label}</h2>
      <p className={GUESTS_SECTION_SUBTITLE_CLASS}>Coming soon.</p>
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
    navigate(href)
  }, [snap.pendingNavigationHref, pageModule, navigate])

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
          <EmptyTabBody label="Credits & usage" />
        </TabsContent>
        <TabsContent value="payment-invoices">
          <EmptyTabBody label="Payment & invoices" />
        </TabsContent>
        <TabsContent value="billing-contacts">
          <EmptyTabBody label="Billing contacts" />
        </TabsContent>
        <TabsContent value="activity">
          <EmptyTabBody label="Activity" />
        </TabsContent>
      </Tabs>
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
    if (
      snap.loadStatus !== "loaded"
      || snap.accessLevel !== "manage"
      || snap.managePlanSection != null
    ) {
      return
    }
    if (
      snap.actorPermissionRole !== "Billing Admin"
      && snap.actorPermissionRole !== "Admin"
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
            <p className={GUESTS_SECTION_SUBTITLE_CLASS}>Coming soon.</p>
          </section>
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
            <p className={GUESTS_SECTION_SUBTITLE_CLASS}>Coming soon.</p>
          </section>
        </div>
      )}
    </div>
  )
}

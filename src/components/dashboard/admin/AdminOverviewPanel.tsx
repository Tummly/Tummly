import { useEffect, useState } from "react"
import axios from "axios"
import { Link } from "react-router-dom"

import { getAdminEnvironment, getAdminOverview } from "@/api/adminApi"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Skeleton } from "@/components/ui/skeleton"
import { STAFF_DASHBOARD_SUPPORT_URL } from "@/config/staffDashboard"
import {
  adminOverviewAsOfText,
  buildAdminOverviewCards,
  buildAdminProviderReadinessRows,
  hasUnavailableAdminOverviewCounts,
  type AdminOverviewCard,
} from "@/lib/admin/adminOverviewPresentation"
import {
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import type { AdminEnvironment, AdminOverview } from "@/types/adminOps"

type OverviewTab = "orders" | "materials" | "pilots" | "customers" | "audit"

type OverviewState =
  | { status: "loading" }
  | { status: "error"; message: string }
  | {
      status: "ready"
      overview: AdminOverview
      environment: AdminEnvironment | null
    }

const LOADING_STATE: OverviewState = { status: "loading" }

function overviewErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error) && error.response?.status === 403) {
    return "You do not have permission to view the overview."
  }
  return "Could not load the overview. Try again."
}

async function fetchOverviewState(): Promise<OverviewState> {
  try {
    const [overview, environment] = await Promise.all([
      getAdminOverview(),
      getAdminEnvironment().catch(() => null),
    ])
    return { status: "ready", overview, environment }
  } catch (error) {
    return { status: "error", message: overviewErrorMessage(error) }
  }
}

export function AdminOverviewPanel({
  onOpenTab,
}: {
  onOpenTab: (tab: OverviewTab) => void
}) {
  const [state, setState] = useState<OverviewState>(LOADING_STATE)
  const [reloadCount, setReloadCount] = useState(0)

  useEffect(() => {
    let active = true
    void fetchOverviewState().then((next) => {
      if (active) {
        setState(next)
      }
    })

    return () => {
      active = false
    }
  }, [reloadCount])

  const refresh = () => {
    setState(LOADING_STATE)
    setReloadCount((count) => count + 1)
  }

  return (
    <section className={GUESTS_PAGE_STACK_CLASS} aria-labelledby="admin-overview-title">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h2 id="admin-overview-title" className={GUESTS_PAGE_TITLE_CLASS}>
            Overview
          </h2>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>
            Operational queues for Tummly staff. Counts only, with no guest details.
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={refresh} disabled={state.status === "loading"}>
          Refresh
        </Button>
      </div>

      {state.status === "loading" && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" aria-busy="true">
          {Array.from({ length: 7 }, (_, index) => (
            <Skeleton key={index} className="h-32 w-full rounded-xl" />
          ))}
        </div>
      )}

      {state.status === "error" && (
        <Alert variant="destructive">
          <AlertTitle>Overview not available</AlertTitle>
          <AlertDescription>{state.message}</AlertDescription>
        </Alert>
      )}

      {state.status === "ready" && (
        <ReadyOverview
          overview={state.overview}
          environment={state.environment}
          onOpenTab={onOpenTab}
        />
      )}
    </section>
  )
}

function ReadyOverview({
  overview,
  environment,
  onOpenTab,
}: {
  overview: AdminOverview
  environment: AdminEnvironment | null
  onOpenTab: (tab: OverviewTab) => void
}) {
  const cards = buildAdminOverviewCards(overview, STAFF_DASHBOARD_SUPPORT_URL)

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <p className="text-sm text-muted-foreground" data-testid="overview-freshness">
        {adminOverviewAsOfText(overview.generatedAtUtc)}
        {" · "}
        {environment
          ? `Environment ${environment.environmentName} · Commit ${environment.commitSha}`
          : "Environment not available"}
      </p>

      {hasUnavailableAdminOverviewCounts(overview) && (
        <Alert>
          <AlertTitle>Some counts could not be loaded</AlertTitle>
          <AlertDescription>
            Unavailable counts are not zero. Refresh to try again.
          </AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {cards.map((card) => (
          <OverviewCard key={card.id} card={card} onOpenTab={onOpenTab} />
        ))}
      </div>

      {environment ? (
        <section className="flex flex-col gap-2" aria-label="Provider readiness">
          <h3 className="text-sm font-medium text-foreground">Provider readiness</h3>
          <ul className="flex flex-col gap-2">
            {buildAdminProviderReadinessRows(environment).map((row) => (
              <li key={row.id} className="flex flex-wrap items-center gap-2 text-sm">
                <span className="text-foreground">{row.label}</span>
                <Badge variant={row.configured ? "positive" : "negative"}>
                  {row.statusText}
                </Badge>
                {row.detailText ? (
                  <span className="text-muted-foreground">{row.detailText}</span>
                ) : null}
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </div>
  )
}

function OverviewCard({
  card,
  onOpenTab,
}: {
  card: AdminOverviewCard
  onOpenTab: (tab: OverviewTab) => void
}) {
  const target = card.target

  return (
    <Card size="sm">
      <CardHeader>
        <CardDescription>{card.title}</CardDescription>
        <CardTitle
          className={
            card.available
              ? "font-heading text-3xl text-foreground"
              : "font-sans text-base text-muted-foreground"
          }
        >
          {card.valueText}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-2">
        <p className="text-sm text-muted-foreground">
          {card.available ? card.description : "This count could not be loaded."}
        </p>
        {card.available && card.detailText && (
          <p className="text-sm text-foreground">{card.detailText}</p>
        )}
        {target?.kind === "tab" && (
          <Button
            variant="link"
            className="w-fit px-0"
            onClick={() => onOpenTab(target.tab)}
          >
            {card.linkLabel}
          </Button>
        )}
        {target?.kind === "route" && (
          <Button asChild variant="link" className="w-fit px-0">
            <Link to={target.to}>{card.linkLabel}</Link>
          </Button>
        )}
      </CardContent>
    </Card>
  )
}

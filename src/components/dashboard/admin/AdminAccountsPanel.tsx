import { useEffect, useMemo, useState } from "react"
import axios from "axios"

import {
  fetchAdminAccounts,
  type AdminAccountListRow,
} from "@/api/adminAccountsApi"
import { AdminAccountProfileDrawer } from "@/components/dashboard/admin/AdminAccountProfileDrawer"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import {
  ACCOUNT_BILLING_STATUS_OPTIONS,
  ACCOUNT_PLAN_OPTIONS,
  ALL_FILTER_VALUE,
  type AdminAccountFilters,
  billingStatusBadgeVariant,
  buildAccountListParams,
  formatAccountDate,
  hasNextPage,
  hasPreviousPage,
  pageSummary,
  validateCreatedRange,
} from "@/lib/admin/adminAccountsPresentation"

const PAGE_SIZE = 50

const EMPTY_FILTERS: AdminAccountFilters = {
  search: "",
  plan: ALL_FILTER_VALUE,
  billingStatus: ALL_FILTER_VALUE,
  createdFrom: "",
  createdTo: "",
}

type AccountsResult = {
  key: string
  rows: AdminAccountListRow[]
  totalCount: number
  error: string | null
}

function accountsErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error) && error.response?.status === 403) {
    return "You do not have access to Customers."
  }
  return "Could not load Customers."
}

export function AdminAccountsPanel() {
  const [filters, setFilters] = useState<AdminAccountFilters>(EMPTY_FILTERS)
  const [searchDraft, setSearchDraft] = useState("")
  const [skip, setSkip] = useState(0)
  const [result, setResult] = useState<AccountsResult | null>(null)
  const [reloadKey, setReloadKey] = useState(0)
  const [selectedRestaurantId, setSelectedRestaurantId] = useState<number | null>(
    null
  )
  const [drawerOpen, setDrawerOpen] = useState(false)

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setFilters((current) =>
        current.search === searchDraft.trim()
          ? current
          : { ...current, search: searchDraft.trim() }
      )
      setSkip(0)
    }, 300)
    return () => window.clearTimeout(timer)
  }, [searchDraft])

  const rangeError = validateCreatedRange(filters.createdFrom, filters.createdTo)

  const listParams = useMemo(
    () => buildAccountListParams(filters, skip, PAGE_SIZE),
    [filters, skip]
  )

  const requestKey = useMemo(
    () => JSON.stringify([listParams, reloadKey]),
    [listParams, reloadKey]
  )

  useEffect(() => {
    if (rangeError) return

    let cancelled = false
    fetchAdminAccounts(listParams)
      .then((response) => {
        if (!cancelled) {
          setResult({
            key: requestKey,
            rows: response.items,
            totalCount: response.totalCount,
            error: null,
          })
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setResult({
            key: requestKey,
            rows: [],
            totalCount: 0,
            error: accountsErrorMessage(error),
          })
        }
      })

    return () => {
      cancelled = true
    }
  }, [listParams, rangeError, requestKey])

  // A result belongs to one request; until it lands the list shows loading.
  const current = result?.key === requestKey ? result : null
  const loading = !rangeError && current == null
  const rows = current?.rows ?? []
  const totalCount = current?.totalCount ?? 0
  const loadError = current?.error ?? null

  const updateFilter = <K extends keyof AdminAccountFilters>(
    key: K,
    value: AdminAccountFilters[K]
  ) => {
    setFilters((current) => ({ ...current, [key]: value }))
    setSkip(0)
  }

  const openProfile = (restaurantId: number) => {
    setSelectedRestaurantId(restaurantId)
    setDrawerOpen(true)
  }

  return (
    <section className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h2 className="font-heading text-xl font-bold text-foreground">Customers</h2>
        <p className="text-sm text-op-text-primary">
          Read-only directory of customer Workspaces. Search by Workspace ID or name.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-5">
        <Input
          aria-label="Search by Workspace ID or name"
          placeholder="Workspace ID or name"
          value={searchDraft}
          onChange={(event) => setSearchDraft(event.target.value)}
        />
        <Select
          value={filters.plan}
          onValueChange={(value) => updateFilter("plan", value)}
        >
          <SelectTrigger aria-label="Plan filter">
            <SelectValue placeholder="All plans" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_FILTER_VALUE}>All plans</SelectItem>
            {ACCOUNT_PLAN_OPTIONS.map((plan) => (
              <SelectItem key={plan} value={plan}>
                {plan}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Select
          value={filters.billingStatus}
          onValueChange={(value) => updateFilter("billingStatus", value)}
        >
          <SelectTrigger aria-label="Billing status filter">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_FILTER_VALUE}>All statuses</SelectItem>
            {ACCOUNT_BILLING_STATUS_OPTIONS.map((status) => (
              <SelectItem key={status} value={status}>
                {status}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          type="date"
          aria-label="Created from"
          value={filters.createdFrom}
          onChange={(event) => updateFilter("createdFrom", event.target.value)}
        />
        <Input
          type="date"
          aria-label="Created to"
          value={filters.createdTo}
          onChange={(event) => updateFilter("createdTo", event.target.value)}
        />
      </div>

      {rangeError ? (
        <Alert variant="destructive">
          <AlertTitle>Check the date range</AlertTitle>
          <AlertDescription>{rangeError}</AlertDescription>
        </Alert>
      ) : null}

      {loadError && !loading ? (
        <Alert variant="destructive">
          <AlertTitle>Customers unavailable</AlertTitle>
          <AlertDescription className="flex flex-wrap items-center gap-3">
            {loadError}
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setReloadKey((key) => key + 1)}
            >
              Try again
            </Button>
          </AlertDescription>
        </Alert>
      ) : null}

      {loading && !rangeError ? (
        <div className="flex flex-col gap-2" aria-busy="true" aria-label="Loading Customers">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      ) : !loadError && !rangeError ? (
        <>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Workspace</TableHead>
                <TableHead>Owner</TableHead>
                <TableHead>Plan</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Active locations</TableHead>
                <TableHead>Period end</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} className="text-center text-op-text-primary">
                    No accounts match these filters.
                  </TableCell>
                </TableRow>
              ) : (
                rows.map((row) => (
                  <TableRow key={row.restaurantId}>
                    <TableCell>
                      <div className="flex flex-col items-start gap-0.5">
                        <Button
                          type="button"
                          variant="link"
                          className="h-auto p-0 font-medium"
                          onClick={() => openProfile(row.restaurantId)}
                        >
                          {row.displayName}
                        </Button>
                        <span className="text-xs text-op-text-primary">
                          ID {row.restaurantId}
                        </span>
                      </div>
                    </TableCell>
                    <TableCell>{row.ownerDisplayName ?? "—"}</TableCell>
                    <TableCell>{row.subscriptionPlan}</TableCell>
                    <TableCell>
                      <Badge variant={billingStatusBadgeVariant(row.billingStatus)}>
                        {row.billingStatus}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-right">{row.activeLocationCount}</TableCell>
                    <TableCell>{formatAccountDate(row.periodEndUtc)}</TableCell>
                    <TableCell>{formatAccountDate(row.createdAtUtc)}</TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>

          <div className="flex flex-wrap items-center justify-between gap-3">
            <span className="text-sm text-op-text-primary">
              {pageSummary(skip, rows.length, totalCount)}
            </span>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={!hasPreviousPage(skip)}
                onClick={() => setSkip((current) => Math.max(0, current - PAGE_SIZE))}
              >
                Previous
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={!hasNextPage(skip, PAGE_SIZE, totalCount)}
                onClick={() => setSkip((current) => current + PAGE_SIZE)}
              >
                Next
              </Button>
            </div>
          </div>
        </>
      ) : null}

      <AdminAccountProfileDrawer
        restaurantId={selectedRestaurantId}
        open={drawerOpen}
        onOpenChange={setDrawerOpen}
      />
    </section>
  )
}

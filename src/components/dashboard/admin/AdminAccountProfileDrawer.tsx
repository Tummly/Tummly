import type { ReactNode } from "react"
import { useEffect, useState } from "react"
import axios from "axios"

import {
  fetchAdminAccountProfile,
  type AdminAccountProfile,
} from "@/api/adminAccountsApi"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { Skeleton } from "@/components/ui/skeleton"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { formatAdminShopGbpFromPence } from "@/lib/adminShopOrderFulfilment"
import {
  billingStatusBadgeVariant,
  capacityLabel,
  formatAccountDate,
  locationStatusBadgeVariant,
  scheduledChangeLabel,
} from "@/lib/admin/adminAccountsPresentation"
import {
  OPERATOR_RIGHT_DRAWER_BODY_CLASS,
  OPERATOR_RIGHT_DRAWER_CONTENT_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import { XIcon } from "lucide-react"

type AdminAccountProfileDrawerProps = {
  restaurantId: number | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

type LoadOutcome =
  | { kind: "ready"; profile: AdminAccountProfile }
  | { kind: "forbidden" }
  | { kind: "failed" }

/** A finished load, tagged with the Workspace and attempt it answers. */
type LoadResult = LoadOutcome & { restaurantId: number; attempt: number }

function loadOutcomeFromError(error: unknown): LoadOutcome {
  if (axios.isAxiosError(error) && error.response?.status === 403) {
    return { kind: "forbidden" }
  }
  return { kind: "failed" }
}

function ProfileSection({
  title,
  children,
}: {
  title: string
  children: ReactNode
}) {
  return (
    <section className="flex flex-col gap-3">
      <h3 className="font-heading text-base font-bold text-foreground">{title}</h3>
      {children}
    </section>
  )
}

function Field({ label, value }: { label: string; value: ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-4 text-sm">
      <dt className="text-op-text-primary">{label}</dt>
      <dd className="text-right font-medium text-foreground">{value}</dd>
    </div>
  )
}

function EmptyNote({ children }: { children: ReactNode }) {
  return <p className="text-sm text-op-text-primary">{children}</p>
}

export function AdminAccountProfileDrawer({
  restaurantId,
  open,
  onOpenChange,
}: AdminAccountProfileDrawerProps) {
  const [result, setResult] = useState<LoadResult | null>(null)
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    if (!open || restaurantId == null) return

    let cancelled = false
    fetchAdminAccountProfile(restaurantId)
      .then((profile) => {
        if (!cancelled) {
          setResult({ kind: "ready", profile, restaurantId, attempt })
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setResult({ ...loadOutcomeFromError(error), restaurantId, attempt })
        }
      })

    return () => {
      cancelled = true
    }
  }, [open, restaurantId, attempt])

  // Ignore a result left over from another Workspace or an earlier attempt.
  const current =
    result != null && result.restaurantId === restaurantId && result.attempt === attempt
      ? result
      : null
  const profile = current?.kind === "ready" ? current.profile : null
  const loading = open && restaurantId != null && current == null

  return (
    <Drawer open={open} onOpenChange={onOpenChange} direction="right">
      <DrawerContent className={OPERATOR_RIGHT_DRAWER_CONTENT_CLASS}>
        <div className="flex min-h-0 flex-1 flex-col">
          <div className="flex shrink-0 items-start justify-between gap-4 px-6 pt-8 pb-6">
            <div className="flex min-w-0 flex-col gap-1">
              <DrawerTitle className="text-2xl font-bold text-foreground">
                {profile
                  ? profile.identity.displayName
                  : "Customer account"}
              </DrawerTitle>
              <DrawerDescription className="text-sm text-op-text-primary">
                {profile
                  ? `Workspace ${profile.identity.restaurantId} · read-only`
                  : "Read-only account profile"}
              </DrawerDescription>
            </div>
            <DrawerClose asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-[42px] shrink-0 rounded-[2px] bg-muted hover:bg-muted/80"
                aria-label="Close account profile"
              >
                <XIcon className="size-[18px]" aria-hidden />
              </Button>
            </DrawerClose>
          </div>

          <div className={OPERATOR_RIGHT_DRAWER_BODY_CLASS}>
            <div className="flex flex-col gap-6 px-6 pb-10">
              {loading ? (
                <div className="flex flex-col gap-2" aria-busy="true" aria-label="Loading account profile">
                  <Skeleton className="h-8 w-full" />
                  <Skeleton className="h-24 w-full" />
                  <Skeleton className="h-24 w-full" />
                </div>
              ) : null}

              {current?.kind === "forbidden" ? (
                <Alert variant="destructive">
                  <AlertTitle>No access</AlertTitle>
                  <AlertDescription>You do not have access to this account.</AlertDescription>
                </Alert>
              ) : null}

              {current?.kind === "failed" ? (
                <Alert variant="destructive">
                  <AlertTitle>Profile unavailable</AlertTitle>
                  <AlertDescription className="flex flex-wrap items-center gap-3">
                    Could not load this account.
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => setAttempt((count) => count + 1)}
                    >
                      Try again
                    </Button>
                  </AlertDescription>
                </Alert>
              ) : null}

              {profile ? <ProfileBody profile={profile} /> : null}
            </div>
          </div>
        </div>
      </DrawerContent>
    </Drawer>
  )
}

function ProfileBody({ profile }: { profile: AdminAccountProfile }) {
  const { identity, lifecycle, locations, capacity, credits, payments, invoices, recentAudit } =
    profile
  const scheduled = scheduledChangeLabel(lifecycle.scheduledChange)

  return (
    <>
      <ProfileSection title="Identity">
        <dl className="flex flex-col gap-2">
          <Field label="Display name" value={identity.displayName} />
          <Field label="Legal name" value={identity.legalName ?? "—"} />
          <Field label="Account owner" value={identity.ownerName ?? "—"} />
          <Field label="Owner email" value={identity.ownerEmail ?? "—"} />
          <Field label="Created" value={formatAccountDate(identity.createdAtUtc)} />
          <Field label="Billing account" value={`#${identity.billingAccountId}`} />
        </dl>
      </ProfileSection>

      <Separator />

      <ProfileSection title="Lifecycle">
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant={billingStatusBadgeVariant(lifecycle.billingStatus)}>
            {lifecycle.billingStatus}
          </Badge>
          <span className="text-sm text-foreground">
            {lifecycle.subscriptionPlan}
            {lifecycle.billingCycle ? ` · ${lifecycle.billingCycle}` : ""}
          </span>
        </div>
        <dl className="flex flex-col gap-2">
          <Field label="Pilot started" value={formatAccountDate(lifecycle.pilotStartedAtUtc)} />
          <Field label="Pilot ends" value={formatAccountDate(lifecycle.pilotPeriodEndUtc)} />
          <Field
            label="Current period start"
            value={formatAccountDate(lifecycle.currentPeriodStartUtc)}
          />
          <Field
            label="Current period end"
            value={formatAccountDate(lifecycle.currentPeriodEndUtc)}
          />
          <Field
            label="Soft lock entered"
            value={formatAccountDate(lifecycle.softLockEnteredAtUtc)}
          />
          <Field
            label="Dormant entered"
            value={formatAccountDate(lifecycle.dormantEnteredAtUtc)}
          />
        </dl>
        {scheduled ? <EmptyNote>{scheduled}</EmptyNote> : null}
      </ProfileSection>

      <Separator />

      <ProfileSection title="Locations">
        <dl className="grid grid-cols-2 gap-2">
          <Field label="Included" value={locations.counts.included} />
          <Field label="Paid extra" value={locations.counts.paidExtra} />
          <Field label="Active" value={locations.counts.active} />
          <Field label="Total" value={locations.counts.total} />
        </dl>
        {locations.items.length === 0 ? (
          <EmptyNote>No Locations yet.</EmptyNote>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Location</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="text-right">Managers</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {locations.items.map((location) => (
                <TableRow key={location.locationId}>
                  <TableCell>
                    <span className="font-medium">{location.name}</span>
                    <span className="block text-xs text-op-text-primary">
                      ID {location.locationId}
                    </span>
                  </TableCell>
                  <TableCell>
                    <Badge variant={locationStatusBadgeVariant(location.lifecycleStatus)}>
                      {location.lifecycleStatus}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-right">{location.assignedManagerCount}</TableCell>
                  <TableCell>{formatAccountDate(location.createdAtUtc)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </ProfileSection>

      <Separator />

      <ProfileSection title="Capacity">
        <dl className="flex flex-col gap-2">
          <Field label="Team seats" value={capacityLabel(capacity.seats)} />
          <Field label="QR placements" value={capacityLabel(capacity.qrPlacements)} />
          <Field label="Active offers" value={capacityLabel(capacity.offers)} />
        </dl>
        <h4 className="text-sm font-medium text-foreground">Pending invitations</h4>
        {capacity.pendingInvites.length === 0 ? (
          <EmptyNote>No pending invitations.</EmptyNote>
        ) : (
          <ul className="flex flex-col gap-2">
            {capacity.pendingInvites.map((invite) => (
              <li
                key={`${invite.maskedEmail}-${invite.sentAtUtc}`}
                className="flex flex-wrap items-baseline justify-between gap-2 text-sm"
              >
                <span className="font-medium text-foreground">{invite.maskedEmail}</span>
                <span className="text-op-text-primary">
                  Sent {formatAccountDate(invite.sentAtUtc)} · expires{" "}
                  {formatAccountDate(invite.expiresAtUtc)}
                </span>
              </li>
            ))}
          </ul>
        )}
      </ProfileSection>

      <Separator />

      <ProfileSection title="Credits">
        {credits.length === 0 ? (
          <EmptyNote>No credit balances.</EmptyNote>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Channel</TableHead>
                <TableHead className="text-right">Available</TableHead>
                <TableHead className="text-right">Reserved</TableHead>
                <TableHead className="text-right">Included</TableHead>
                <TableHead className="text-right">Prepaid</TableHead>
                <TableHead>Nearest expiry</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {credits.map((channel) => (
                <TableRow key={channel.channel}>
                  <TableCell className="font-medium uppercase">{channel.channel}</TableCell>
                  <TableCell className="text-right">{channel.available}</TableCell>
                  <TableCell className="text-right">{channel.reserved}</TableCell>
                  <TableCell className="text-right">{channel.includedThisPeriod}</TableCell>
                  <TableCell className="text-right">{channel.prepaidRemaining}</TableCell>
                  <TableCell>{formatAccountDate(channel.nearestPrepaidExpiryUtc)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </ProfileSection>

      <Separator />

      <ProfileSection title="Payments">
        {payments.length === 0 ? (
          <EmptyNote>No payment intents.</EmptyNote>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Kind</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                <TableHead>State</TableHead>
                <TableHead>Reference</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {payments.map((payment) => (
                <TableRow key={payment.id}>
                  <TableCell>{payment.kind}</TableCell>
                  <TableCell className="text-right">
                    {formatAdminShopGbpFromPence(payment.amountPence)}
                  </TableCell>
                  <TableCell>{payment.isOpen ? "Open" : "Closed"}</TableCell>
                  <TableCell className="font-mono text-xs">
                    {payment.providerReference ?? "—"}
                  </TableCell>
                  <TableCell>{formatAccountDate(payment.createdAtUtc)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </ProfileSection>

      <Separator />

      <ProfileSection title="Invoices and credit notes">
        {invoices.length === 0 ? (
          <EmptyNote>No invoices or credit notes.</EmptyNote>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Number</TableHead>
                <TableHead>Type</TableHead>
                <TableHead>Date</TableHead>
                <TableHead className="text-right">Total</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoices.map((invoice) => (
                <TableRow key={invoice.documentNumber}>
                  <TableCell className="font-medium">{invoice.documentNumber}</TableCell>
                  <TableCell>{invoice.type}</TableCell>
                  <TableCell>{formatAccountDate(invoice.invoiceDateUtc)}</TableCell>
                  <TableCell className="text-right">
                    {formatAdminShopGbpFromPence(invoice.grossPence)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </ProfileSection>

      <Separator />

      <ProfileSection title="Recent audit">
        {recentAudit.length === 0 ? (
          <EmptyNote>No audit events for this account.</EmptyNote>
        ) : (
          <ul className="flex flex-col gap-2">
            {recentAudit.map((event) => (
              <li key={event.id} className="flex flex-col gap-0.5 text-sm">
                <span className="font-medium text-foreground">
                  {event.action}
                  {event.succeeded ? "" : " (failed)"}
                </span>
                <span className="text-op-text-primary">
                  {formatAccountDate(event.occurredAtUtc)} · {event.actorIdentity}
                </span>
              </li>
            ))}
          </ul>
        )}
      </ProfileSection>
    </>
  )
}

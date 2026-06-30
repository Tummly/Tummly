import type { ReactNode } from "react"

import {
  AccountTypeBadge,
  hasCreatedOperatorAccount,
  TrialRequestStatusBadge,
} from "@/components/dashboard/admin/adminTrialRequestStatus"
import { TrialRequestActionsMenu } from "@/components/dashboard/admin/TrialRequestActionsMenu"
import {
  Drawer,
  DrawerContent,
  DrawerDescription,
  DrawerHeader,
  DrawerTitle,
} from "@/components/ui/drawer"
import { Separator } from "@/components/ui/separator"
import {
  formatAdminBoolean,
  formatAdminDate,
  formatAdminText,
  formatTrialRequestCategory,
  formatTrialRequestGoal,
  formatTrialRequestLocations,
  formatTrialRequestRole,
} from "@/lib/adminTrialRequestLabels"
import type { AdminTrialRequest } from "@/types/admin"

type OperatorDetailsDrawerProps = {
  request: AdminTrialRequest | null
  open: boolean
  dismissible?: boolean
  onOpenChange: (open: boolean) => void
  showDelete: boolean
  actionsDisabled?: boolean
  onApprove: (request: AdminTrialRequest) => void
  onDecline: (request: AdminTrialRequest) => void
  onRequestMoreInfo: (request: AdminTrialRequest) => void
  onResendInvite: (request: AdminTrialRequest) => void
  onDelete: (request: AdminTrialRequest) => void
}

function DetailSection({
  title,
  children,
}: {
  title: string
  children: ReactNode
}) {
  return (
    <section className="flex flex-col gap-3">
      <h3 className="text-sm font-semibold text-foreground">{title}</h3>
      <dl className="grid grid-cols-2 gap-x-4 gap-y-4">{children}</dl>
    </section>
  )
}

function DetailField({
  label,
  children,
}: {
  label: string
  children: ReactNode
}) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
        {label}
      </dt>
      <dd className="text-sm text-foreground">{children}</dd>
    </div>
  )
}

function getRegisteredLocationTitle(
  location: { locationName: string },
  index: number,
  total: number
) {
  const locationName = location.locationName.trim()

  if (locationName) {
    return locationName
  }

  return total > 1 ? `Location ${index + 1}` : "Primary location"
}

function RegisteredLocationCard({
  location,
  index,
  total,
}: {
  location: {
    locationName: string
    address: string
    postcode?: string | null
    locationPhone?: string | null
    localContact?: string | null
  }
  index: number
  total: number
}) {
  const title = getRegisteredLocationTitle(location, index, total)

  return (
    <article className="flex flex-col gap-3 rounded-xl border bg-muted/20 p-4">
      <h4 className="text-sm font-medium text-foreground">{title}</h4>
      <dl className="grid grid-cols-2 gap-x-4 gap-y-4">
        <DetailField label="Address">
          {formatAdminText(location.address)}
        </DetailField>
        <DetailField label="Postcode">
          {formatAdminText(location.postcode)}
        </DetailField>
        {location.locationPhone && (
          <DetailField label="Location phone">
            {formatAdminText(location.locationPhone)}
          </DetailField>
        )}
        {location.localContact && (
          <DetailField label="Local contact">
            {formatAdminText(location.localContact)}
          </DetailField>
        )}
      </dl>
    </article>
  )
}

export function OperatorDetailsDrawer({
  request,
  open,
  dismissible = true,
  onOpenChange,
  showDelete,
  actionsDisabled = false,
  onApprove,
  onDecline,
  onRequestMoreInfo,
  onResendInvite,
  onDelete,
}: OperatorDetailsDrawerProps) {
  if (!request) {
    return null
  }

  const businessLink = request.businessLink?.trim()
  const operatorLocations =
    request.operatorLocations && request.operatorLocations.length > 0
      ? request.operatorLocations
      : request.primaryAddress || request.primaryPostcode
        ? [
            {
              locationName: request.businessName,
              address: request.primaryAddress ?? "",
              postcode: request.primaryPostcode ?? null,
              locationPhone: null,
              localContact: null,
            },
          ]
        : []
  const showRegisteredLocations = hasCreatedOperatorAccount(request)

  return (
    <Drawer
      open={open}
      onOpenChange={onOpenChange}
      direction="right"
      dismissible={dismissible}
    >
      <DrawerContent className="h-full max-h-dvh data-[vaul-drawer-direction=right]:sm:max-w-lg">
        <DrawerHeader className="border-b pb-4">
          <div className="flex items-start justify-between gap-3">
            <div className="flex min-w-0 flex-1 flex-col gap-0.5">
              <DrawerTitle className="text-xl font-semibold">
                Operator details
              </DrawerTitle>
              <DrawerDescription>
                {request.businessName} · #{request.id}
              </DrawerDescription>
            </div>
            <TrialRequestActionsMenu
              request={request}
              showDelete={showDelete}
              disabled={actionsDisabled}
              trigger="button"
              menuContentClassName="z-[120]"
              onApprove={onApprove}
              onDecline={onDecline}
              onRequestMoreInfo={onRequestMoreInfo}
              onResendInvite={onResendInvite}
              onDelete={onDelete}
            />
          </div>
        </DrawerHeader>

        <div className="flex min-h-0 flex-1 flex-col gap-6 overflow-y-auto px-4 py-4">
          <DetailSection title="Application">
            <DetailField label="Business name">
              {formatAdminText(request.businessName)}
            </DetailField>
            <DetailField label="Category">
              {formatAdminText(formatTrialRequestCategory(request.businessCategory))}
            </DetailField>
            <DetailField label="Locations">
              {formatAdminText(formatTrialRequestLocations(request.locations))}
            </DetailField>
            <DetailField label="Business link">
              {businessLink ? (
                <a
                  href={businessLink}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="break-all text-primary underline-offset-4 hover:underline"
                >
                  {businessLink}
                </a>
              ) : (
                "—"
              )}
            </DetailField>
            <DetailField label="Main location">
              {formatAdminText(request.mainLocation)}
            </DetailField>
            <DetailField label="Town/City">
              {formatAdminText(request.townCity)}
            </DetailField>
            <DetailField label="Postcode">
              {formatAdminText(request.mainLocationPostcode)}
            </DetailField>
            <DetailField label="Main goal">
              {formatAdminText(formatTrialRequestGoal(request.goal))}
            </DetailField>
            <DetailField label="Account type">
              <AccountTypeBadge accountType={request.accountType} />
            </DetailField>
          </DetailSection>

          <Separator />

          <DetailSection title="Applicant">
            <DetailField label="Full name">
              {formatAdminText(request.fullName)}
            </DetailField>
            <DetailField label="Email">
              <a
                href={`mailto:${request.email}`}
                className="break-all text-primary underline-offset-4 hover:underline"
              >
                {request.email}
              </a>
            </DetailField>
            <DetailField label="Mobile">
              {formatAdminText(request.mobile)}
            </DetailField>
            <DetailField label="Role">
              {formatAdminText(formatTrialRequestRole(request.role))}
            </DetailField>
          </DetailSection>

          <Separator />

          <DetailSection title="Status">
            <DetailField label="Review status">
              <TrialRequestStatusBadge request={request} />
            </DetailField>
            <DetailField label="Email verified">
              {formatAdminBoolean(request.isEmailVerified)}
            </DetailField>
            <DetailField label="Approved">
              {formatAdminBoolean(request.isApproved)}
            </DetailField>
            <DetailField label="Operator account created">
              {formatAdminBoolean(request.isAccountCreated)}
            </DetailField>
          </DetailSection>

          <Separator />

          {showRegisteredLocations && (
              <>
                <section className="flex flex-col gap-3">
                  <div className="flex items-center justify-between gap-3">
                    <h3 className="text-sm font-semibold text-foreground">
                      Registered locations
                    </h3>
                    {operatorLocations.length > 1 && (
                      <span className="text-xs font-medium text-muted-foreground">
                        {operatorLocations.length} venues
                      </span>
                    )}
                  </div>
                  {operatorLocations.length > 0 ? (
                    <div className="flex flex-col gap-3">
                      {operatorLocations.map((location, index) => (
                        <RegisteredLocationCard
                          key={`${location.locationName}-${location.address}-${index}`}
                          location={location}
                          index={index}
                          total={operatorLocations.length}
                        />
                      ))}
                    </div>
                  ) : (
                    <p className="text-sm text-muted-foreground">
                      No registered location details are available for this
                      account yet.
                    </p>
                  )}
                </section>
                <Separator />
              </>
            )}

          <DetailSection title="Review history">
            <DetailField label="Submitted">
              {formatAdminText(formatAdminDate(request.createdAt))}
            </DetailField>
            <DetailField label="Reviewed">
              {request.reviewedAt
                ? `${formatAdminDate(request.reviewedAt)}${request.reviewedBy ? ` · ${request.reviewedBy}` : ""}`
                : "—"}
            </DetailField>
            <DetailField label="Approved">
              {formatAdminText(formatAdminDate(request.approvedAt))}
            </DetailField>
            <DetailField label="Declined">
              {formatAdminText(formatAdminDate(request.declinedAt))}
            </DetailField>
            <DetailField label="Decline reason">
              {formatAdminText(request.declineReason)}
            </DetailField>
            <DetailField label="More info requested">
              {formatAdminText(formatAdminDate(request.moreInfoRequestedAt))}
            </DetailField>
            <DetailField label="Information requested">
              {formatAdminText(request.moreInfoMessage)}
            </DetailField>
            <DetailField label="Invite sent">
              {formatAdminText(formatAdminDate(request.inviteSentAt))}
            </DetailField>
            <DetailField label="Account created">
              {formatAdminText(formatAdminDate(request.accountCreatedAt))}
            </DetailField>
          </DetailSection>
        </div>
      </DrawerContent>
    </Drawer>
  )
}

import { useEffect, useSyncExternalStore } from "react"
import { MoreVerticalIcon, XIcon } from "lucide-react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { GuestsFilterChipRow } from "@/components/dashboard/operator/Guests/GuestsFilterChipRow"
import { OperatorSearchIcon } from "@/components/dashboard/operator/OperatorSearchIcon"
import { TeamMemberDialogForm } from "@/components/dashboard/operator/TeamPermissions/TeamMemberDialogForm"
import { useTeamPermissionsPageModuleApi } from "@/components/dashboard/operator/TeamPermissions/utils/teamPermissionsPageModuleContext"
import {
  ACCOUNT_WORKSPACE_FULL_BLEED_BOTTOM,
  ACCOUNT_WORKSPACE_FULL_BLEED_X,
  ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS,
  ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS,
  ACCOUNT_WORKSPACE_PAGE_COPY,
  ACCOUNT_WORKSPACE_PAGE_STACK_CLASS,
  ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS,
  ACCOUNT_WORKSPACE_SHELL_PAD_BOTTOM,
  ACCOUNT_WORKSPACE_SHELL_PAD_X,
  ACCOUNT_WORKSPACE_TAB_BODY_CLASS,
  ACCOUNT_WORKSPACE_TAB_LIST_CLASS,
  ACCOUNT_WORKSPACE_TAB_TRIGGER_CLASS,
  ACCOUNT_WORKSPACE_TABS_RULE_CLASS,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  BROWSER_BACK_HREF,
  registerLeaveDirtyGuard,
} from "@/lib/operatorNavigation/leaveDirtyGuard"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
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
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { teamPermissionsFilterSheetSchema } from "@/lib/operatorTeamPermissions/teamPermissionsFilterSheetSchema"
import { assignableRolesForActor } from "@/lib/operatorTeamPermissions/permissionRoles"
import type {
  AccessActivityViewRow,
  TeamInvitationRow,
  TeamMemberRow,
} from "@/lib/operatorTeamPermissions/createOperatorTeamPermissionsPageModule"
import {
  displayPermissionLabel,
  formatAccessActivityOccurredAt,
  formatTeamPermissionsOwnersStat,
  legalAdminLevels,
  PERMISSION_MATRIX_ROLES,
  revokeConfirmCopy,
  suspendConfirmCopy,
  TEAM_PERMISSIONS_PAGE_COPY as copy,
  TEAM_PERMISSIONS_INVITE_BODY_STACK_CLASS,
  TEAM_PERMISSIONS_INVITE_DIALOG_CONTENT_CLASS,
  TEAM_PERMISSIONS_SELECT_MENU_CLASS,
  TEAM_PERMISSIONS_STATS_CARD_CLASS,
  TEAM_PERMISSIONS_STATS_DIVIDER_CLASS,
  TEAM_PERMISSIONS_STATS_LABEL_CLASS,
  TEAM_PERMISSIONS_STATS_OWNERS_ROW_CLASS,
  TEAM_PERMISSIONS_STATS_PAIR_CLASS,
  TEAM_PERMISSIONS_STATS_ROW_CLASS,
  TEAM_PERMISSIONS_STATS_STACK_CLASS,
  TEAM_PERMISSIONS_STATS_VALUE_CLASS,
} from "@/lib/operatorTeamPermissions/teamPermissionsPresentation"
import {
  GUESTS_MARKETING_STATUS_BADGE_CLASS,
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS,
  GUESTS_PAGE_SECONDARY_BUTTON_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
  GUESTS_SEARCH_FIELD_CLASS,
  GUESTS_SEARCH_WRAP_CLASS,
  GUESTS_TABLE_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_FRAME_CLASS,
  GUESTS_TABLE_GUEST_NAME_CLASS,
  GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
  GUESTS_TABLE_LOCATION_CLASS,
  GUESTS_TOOLBAR_ACTIONS_CLASS,
  GUESTS_TOOLBAR_ROW_CLASS,
  OPERATOR_GUEST_CONTACT_LINK_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"

function actionLabel(action: string): string {
  switch (action) {
    case "view":
      return copy.view
    case "edit-role":
      return copy.editRole
    case "edit-access":
      return copy.editAccess
    case "suspend":
      return copy.suspend
    case "reactivate":
      return copy.reactivate
    case "resend":
      return copy.resend
    case "revoke":
      return copy.revoke
    default:
      return action
  }
}

export function TeamPermissionsPage() {
  const pageModule = useTeamPermissionsPageModuleApi()
  const snap = useSyncExternalStore(
    pageModule.subscribe,
    pageModule.getSnapshot,
    pageModule.getSnapshot
  )
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const roleOptions = assignableRolesForActor(snap.actorPermissionRole)

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
  }, [pageModule, snap.isDirty])

  const schema = teamPermissionsFilterSheetSchema({
    isSingleLocation: snap.isSingleLocation,
    locations: snap.locations,
  })
  const confirmMembershipId =
    snap.dialog.kind === "suspend" ? snap.dialog.membershipId : null
  const confirmMember =
    confirmMembershipId != null
      ? snap.members.find((row) => row.membershipId === confirmMembershipId)
      : null
  const revokeInvitationId =
    snap.dialog.kind === "revoke" ? snap.dialog.invitationId : null
  const revokeInvitation =
    revokeInvitationId != null
      ? snap.invitations.find((row) => row.invitationId === revokeInvitationId)
      : null
  const confirmCopy =
    snap.dialog.kind === "suspend" && confirmMember != null
      ? suspendConfirmCopy(confirmMember.fullName)
      : snap.dialog.kind === "revoke"
        ? revokeConfirmCopy(revokeInvitation?.email ?? "")
        : null

  return (
    <div className={ACCOUNT_WORKSPACE_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={ACCOUNT_WORKSPACE_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          {snap.actorCanManage ? (
            <Button
              type="button"
              variant="op-primary"
              className={GUESTS_PAGE_PRIMARY_BUTTON_CLASS}
              disabled={snap.inviteAtCap}
              aria-disabled={snap.inviteAtCap}
              title={
                snap.inviteAtCap ? copy.inviteAtCapHelper : undefined
              }
              onClick={() => pageModule.openInvite()}
            >
              {copy.invite}
            </Button>
          ) : null}
          <Button
            type="button"
            variant="op-secondary"
            className={GUESTS_PAGE_SECONDARY_BUTTON_CLASS}
            onClick={() => pageModule.openNotes()}
          >
            {copy.viewNotes}
          </Button>
        </div>
      </div>

      <Tabs
        value={snap.activeTabId}
        onValueChange={(value) => {
          pageModule.requestTabChange(value as typeof snap.activeTabId)
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
          <TabsContent value="members" className="mt-0">
            <MembersBody snap={snap} pageModule={pageModule} />
          </TabsContent>
          <TabsContent value="roles-permissions" className="mt-0">
            <RolesPermissionsBody snap={snap} pageModule={pageModule} />
          </TabsContent>
          <TabsContent value="invitations" className="mt-0">
            <InvitationsBody snap={snap} pageModule={pageModule} />
          </TabsContent>
          <TabsContent value="access-activity" className="mt-0">
            <AccessActivityBody snap={snap} pageModule={pageModule} />
          </TabsContent>
        </div>
      </Tabs>

      <OperatorFilterSheetDialog
        open={snap.filtersOpen}
        title={copy.filters}
        schema={schema}
        session={snap.filtersSession}
        onSessionChange={pageModule.setFiltersSession}
        onOpenChange={(open) => pageModule.setFiltersOpen(open)}
        onApply={() => pageModule.applyFilters()}
      />

      <Dialog
        open={snap.dialog.kind === "notes"}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeDialog()
          }
        }}
      >
        <DialogContent
          showCloseButton={false}
          className="gap-8 rounded-op-md bg-op-surface-secondary p-8 sm:max-w-[633px]"
        >
          <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className="text-2xl font-bold">
                {copy.notesTitle}
              </DialogTitle>
              <DialogDescription className="text-base font-medium text-op-text-muted">
                {copy.notesBody}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                aria-label="Close"
                className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
                onClick={() => pageModule.closeDialog()}
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>
          <DialogFooter className="sm:justify-start">
            <Button
              type="button"
              variant="op-tertiary"
              onClick={() => pageModule.closeDialog()}
            >
              {copy.notesDone}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={snap.dialog.kind === "invite"}
        onOpenChange={(open) => {
          if (!open && !snap.busy) {
            pageModule.closeDialog()
          }
        }}
      >
        <DialogContent
          showCloseButton={false}
          className={TEAM_PERMISSIONS_INVITE_DIALOG_CONTENT_CLASS}
        >
          <div className={TEAM_PERMISSIONS_INVITE_BODY_STACK_CLASS}>
            <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
              <DialogHeader className="min-w-0 flex-1 gap-3">
                <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                  {copy.inviteTitle}
                </DialogTitle>
                <DialogDescription className="max-w-none text-base font-medium leading-normal text-op-text-muted">
                  {copy.inviteSubtitle}
                </DialogDescription>
              </DialogHeader>
              <DialogClose asChild>
                <Button
                  type="button"
                  variant="op-collapse"
                  disabled={snap.busy}
                  aria-label="Close"
                  className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
                  onClick={() => pageModule.closeDialog()}
                >
                  <XIcon aria-hidden />
                </Button>
              </DialogClose>
            </div>

            <TeamMemberDialogForm
              idPrefix="invite"
              values={{
                email: snap.inviteDraft.email,
                fullName: snap.inviteDraft.fullName,
                permissionRole: snap.inviteDraft.permissionRole,
                locationScope: snap.inviteDraft.locationScope,
                namedLocationIds: snap.inviteDraft.namedLocationIds,
              }}
              onChange={(values) =>
                pageModule.setInviteDraft({
                  ...snap.inviteDraft,
                  ...values,
                })
              }
              roleOptions={roleOptions}
              locations={snap.locations}
              isSingleLocation={snap.isSingleLocation}
              busy={snap.busy}
              showMessage
              message={snap.inviteDraft.message}
              onMessageChange={(message) =>
                pageModule.setInviteDraft({
                  ...snap.inviteDraft,
                  message,
                })
              }
              emailError={snap.inviteEmailError}
            />
          </div>

          <DialogFooter className="flex flex-row flex-wrap justify-start gap-3 sm:justify-start">
            <Button
              type="button"
              variant="op-primary"
              disabled={snap.busy}
              onClick={() => {
                void pageModule.confirmDialogPrimary()
              }}
            >
              {copy.sendInvite}
            </Button>
            <Button
              type="button"
              variant="op-tertiary"
              disabled={snap.busy}
              onClick={() => pageModule.closeDialog()}
            >
              {copy.cancel}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <EditMemberDialog
        snap={snap}
        pageModule={pageModule}
        roleOptions={roleOptions}
      />

      {confirmCopy != null ? (
        <AccountWorkspaceConfirmDialog
          open
          title={confirmCopy.title}
          body={confirmCopy.body}
          primaryLabel={confirmCopy.primaryLabel}
          busy={snap.busy}
          onOpenChange={(open) => {
            if (!open) {
              pageModule.closeDialog()
            }
          }}
          onPrimary={() => {
            void pageModule.confirmDialogPrimary()
          }}
          onCancel={() => pageModule.closeDialog()}
        />
      ) : null}

      <AccountWorkspaceConfirmDialog
        open={snap.leaveDirtyOpen}
        title={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtyTitle}
        body={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtyBody}
        primaryLabel={ACCOUNT_WORKSPACE_PAGE_COPY.leaveDirtySave}
        busy={snap.busy}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeLeaveDirty()
          }
        }}
        onPrimary={() => {
          void pageModule.confirmLeaveDirtySave()
        }}
        onCancel={() => {
          void pageModule.confirmLeaveDirtyCancel()
        }}
      />
    </div>
  )
}

function RolesPermissionsBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useTeamPermissionsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
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
      <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
        <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>{copy.loadError}</h2>
        <Button
          type="button"
          variant="op-secondary"
          onClick={() => {
            void pageModule.load()
          }}
        >
          {copy.retry}
        </Button>
      </section>
    )
  }

  return (
    <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.matrixTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {snap.canEditAdminColumn
              ? copy.matrixSubtitleEditable
              : copy.matrixSubtitle}
          </p>
        </div>
        {snap.saveEnabled ? (
          <Button
            type="button"
            variant="op-primary"
            disabled={snap.busy}
            onClick={() => {
              void pageModule.requestSave()
            }}
          >
            {copy.saveChanges}
          </Button>
        ) : null}
      </div>
      <div className="w-full overflow-x-auto">
        <table className={GUESTS_TABLE_CLASS}>
          <thead>
            <tr className={GUESTS_TABLE_HEAD_ROW_CLASS}>
              <th
                className={cn(GUESTS_TABLE_HEAD_CELL_CLASS, "w-[230px] min-w-[230px]")}
              >
                {copy.productArea}
              </th>
              {PERMISSION_MATRIX_ROLES.map((role) => (
                <th key={role} className={GUESTS_TABLE_HEAD_CELL_CLASS}>
                  {role}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {snap.matrix.map((area) => (
              <tr key={area.id} className={GUESTS_TABLE_BODY_ROW_CLASS}>
                <th
                  scope="row"
                  className={cn(
                    GUESTS_TABLE_BODY_CELL_CLASS,
                    "w-[230px] min-w-[230px] text-sm font-semibold"
                  )}
                >
                  {area.label}
                </th>
                {PERMISSION_MATRIX_ROLES.map((role) => {
                  const stored = area.cells[role] ?? "No access"
                  const editable =
                    snap.canEditAdminColumn && role === "Admin"
                  return (
                    <td key={role} className={GUESTS_TABLE_BODY_CELL_CLASS}>
                      {editable ? (
                        <Select
                          value={stored}
                          disabled={snap.busy}
                          onValueChange={(value) => {
                            pageModule.setAdminCell(area.id, value)
                          }}
                        >
                          <SelectTrigger
                            size="sm"
                            aria-label={`${area.label} Admin`}
                            className="h-auto min-h-8 w-full min-w-[7.5rem] rounded-[2px]"
                          >
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent
                            position="popper"
                            className={TEAM_PERMISSIONS_SELECT_MENU_CLASS}
                          >
                            {legalAdminLevels(area.id).map((level) => (
                              <SelectItem key={level} value={level}>
                                {level}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      ) : (
                        <Badge variant="soft">
                          {displayPermissionLabel(role, area.id, stored)}
                        </Badge>
                      )}
                    </td>
                  )
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

function MembersBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useTeamPermissionsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
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
      <div className="flex flex-col items-start gap-3 py-8">
        <p className="m-0 font-semibold">{copy.loadError}</p>
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

  const showLocationCard =
    !snap.isSingleLocation && snap.namedListMembers.length > 0
  const ownerCount = snap.members.filter(
    (row) => row.permissionRole === "Owner" && row.status === "active"
  ).length

  return (
    <div className="flex flex-col gap-6">
      <MembersStatsCard
        activeMembers={snap.stats.activeMembers}
        pendingInvites={snap.stats.pendingInvites}
        locationManagers={snap.stats.locationManagers}
        limitedAccessUsers={snap.stats.limitedAccessUsers}
        ownerCount={ownerCount}
        teamMembersUsageLabel={snap.teamMembersUsageLabel}
      />

      <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.membersTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.membersSubtitle}
          </p>
        </div>

        <div className="flex flex-col gap-3">
          <div className={GUESTS_TOOLBAR_ROW_CLASS}>
            <div className={GUESTS_SEARCH_WRAP_CLASS}>
              <OperatorSearchIcon className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-op-icon-default" />
              <Input
                value={snap.searchQuery}
                placeholder={copy.searchPlaceholder}
                aria-label={copy.searchPlaceholder}
                onChange={(event) =>
                  pageModule.setSearchQuery(event.target.value)
                }
                className={GUESTS_SEARCH_FIELD_CLASS}
              />
            </div>
            <div className={GUESTS_TOOLBAR_ACTIONS_CLASS}>
              <Button
                type="button"
                variant="op-secondary"
                className="rounded-[2px]"
                onClick={() => pageModule.openFilters()}
              >
                {copy.filters}
                {snap.filterChips.length > 0
                  ? ` (${snap.filterChips.length})`
                  : ""}
              </Button>
            </div>
          </div>

          <GuestsFilterChipRow
            chips={snap.filterChips}
            onRemoveChip={(chip) => pageModule.removeFilterChip(chip)}
          />
        </div>

        {snap.visibleMembers.length === 0 ? (
          <div className="flex flex-col items-start gap-3">
            <p className="m-0 font-semibold">{copy.emptyTitle}</p>
            <p className="m-0 text-muted-foreground">{copy.emptyHelper}</p>
            <Button
              type="button"
              variant="op-tertiary"
              onClick={() => pageModule.clearFiltersAndSearch()}
            >
              {copy.clearFilters}
            </Button>
          </div>
        ) : (
          <MembersTable
            rows={snap.visibleMembers}
            pageModule={pageModule}
          />
        )}
      </section>

      {showLocationCard ? (
        <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
          <div className="flex flex-col gap-2">
            <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
              {copy.locationAccessCardTitle}
            </h2>
            <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
              {copy.locationAccessCardSubtitle}
            </p>
          </div>
          <LocationAccessTable
            rows={snap.namedListMembers}
            locations={snap.locations}
          />
        </section>
      ) : null}
    </div>
  )
}

function MembersStatsCard({
  activeMembers,
  pendingInvites,
  locationManagers,
  limitedAccessUsers,
  ownerCount,
  teamMembersUsageLabel,
}: {
  activeMembers: number
  pendingInvites: number
  locationManagers: number
  limitedAccessUsers: number
  ownerCount: number
  teamMembersUsageLabel: string
}) {
  return (
    <section className={TEAM_PERMISSIONS_STATS_CARD_CLASS}>
      <div className={TEAM_PERMISSIONS_STATS_STACK_CLASS}>
        {teamMembersUsageLabel !== "" ? (
          <>
            <div className={TEAM_PERMISSIONS_STATS_OWNERS_ROW_CLASS}>
              <MembersStatsPair
                label={copy.teamPlanUsage}
                value={teamMembersUsageLabel}
              />
            </div>
            <hr className={TEAM_PERMISSIONS_STATS_DIVIDER_CLASS} />
          </>
        ) : null}
        <div className={TEAM_PERMISSIONS_STATS_ROW_CLASS}>
          <MembersStatsPair
            label={copy.teamMembers}
            value={`${activeMembers} ${copy.statActive}`}
          />
          <MembersStatsPair
            label={copy.locationManagers}
            value={`${locationManagers} ${copy.statManagers}`}
          />
        </div>
        <hr className={TEAM_PERMISSIONS_STATS_DIVIDER_CLASS} />
        <div className={TEAM_PERMISSIONS_STATS_ROW_CLASS}>
          <MembersStatsPair
            label={copy.pendingInvites}
            value={`${pendingInvites} ${copy.statPending}`}
          />
          <MembersStatsPair
            label={copy.limitedAccessUsers}
            value={`${limitedAccessUsers} ${copy.statRestricted}`}
          />
        </div>
        <hr className={TEAM_PERMISSIONS_STATS_DIVIDER_CLASS} />
        <div className={TEAM_PERMISSIONS_STATS_OWNERS_ROW_CLASS}>
          <MembersStatsPair
            label={copy.owners}
            value={formatTeamPermissionsOwnersStat(ownerCount)}
          />
        </div>
      </div>
    </section>
  )
}

function MembersStatsPair({
  label,
  value,
}: {
  label: string
  value: string
}) {
  return (
    <div className={TEAM_PERMISSIONS_STATS_PAIR_CLASS}>
      <p className={TEAM_PERMISSIONS_STATS_LABEL_CLASS}>{label}</p>
      <p className={TEAM_PERMISSIONS_STATS_VALUE_CLASS}>{value}</p>
    </div>
  )
}

function AccessActivityBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useTeamPermissionsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
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
      <div className="flex flex-col items-start gap-3 py-8">
        <p className="m-0 font-semibold">{copy.loadError}</p>
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
      <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
            {copy.accessActivityTitle}
          </h2>
          <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
            {copy.accessActivitySubtitle}
          </p>
        </div>
        {snap.accessActivityEmpty ? (
          <div className="flex flex-col items-start gap-2">
            <p className="m-0 font-semibold">{copy.accessActivityEmptyTitle}</p>
            <p className="m-0 text-muted-foreground">
              {copy.accessActivityEmptyHelper}
            </p>
          </div>
        ) : (
          <>
            <AccessActivityRows rows={snap.accessActivityPreview} />
            <div>
              <Button
                type="button"
                variant="op-secondary"
                onClick={() => {
                  void pageModule.openAuditLog()
                }}
              >
                {copy.viewFullAuditLog}
              </Button>
            </div>
          </>
        )}
      </section>
      <Sheet
        open={snap.auditLogOpen}
        onOpenChange={(open) => {
          if (!open) {
            pageModule.closeAuditLog()
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
              {copy.accessActivitySheetTitle}
            </SheetTitle>
          </SheetHeader>
          <AccessActivityRows
            rows={snap.auditLogRows}
            className="overflow-y-auto p-4"
          />
          {snap.auditLogHasPrevious || snap.auditLogHasNext ? (
            <div className="flex gap-3 p-4">
              <Button
                type="button"
                variant="op-tertiary"
                disabled={!snap.auditLogHasPrevious}
                onClick={() => {
                  void pageModule.goToPreviousAuditPage()
                }}
              >
                Previous
              </Button>
              <Button
                type="button"
                variant="op-tertiary"
                disabled={!snap.auditLogHasNext}
                onClick={() => {
                  void pageModule.goToNextAuditPage()
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

function AccessActivityRows({
  rows,
  className,
}: {
  rows: AccessActivityViewRow[]
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

function InvitationsBody({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useTeamPermissionsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
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
      <div className="flex flex-col items-start gap-3 py-8">
        <p className="m-0 font-semibold">{copy.loadError}</p>
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
    <section className={ACCOUNT_WORKSPACE_IDENTITY_CARD_CLASS}>
      <div className="flex flex-col gap-2">
        <h2 className={ACCOUNT_WORKSPACE_IDENTITY_TITLE_CLASS}>
          {copy.invitationsTitle}
        </h2>
        <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
          {copy.invitationsSubtitle}
        </p>
      </div>

      {snap.invitations.length === 0 ? (
        <div className="flex flex-col items-start gap-6">
          <div className="flex flex-col gap-2">
            <p className="m-0 text-base font-semibold leading-normal text-op-card-title-color">
              {copy.invitationsEmptyTitle}
            </p>
            <p className={ACCOUNT_WORKSPACE_IDENTITY_SUBTITLE_CLASS}>
              {copy.invitationsEmptyHelper}
            </p>
          </div>
          {snap.actorCanManage ? (
            <Button
              type="button"
              variant="op-primary"
              className="w-auto shrink-0"
              disabled={snap.inviteAtCap}
              aria-disabled={snap.inviteAtCap}
              title={
                snap.inviteAtCap ? copy.inviteAtCapHelper : undefined
              }
              onClick={() => pageModule.openInvite()}
            >
              {copy.invite}
            </Button>
          ) : null}
        </div>
      ) : (
        <InvitationsTable rows={snap.invitations} pageModule={pageModule} />
      )}
    </section>
  )
}

function InvitationsTable({
  rows,
  pageModule,
}: {
  rows: TeamInvitationRow[]
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
}) {
  return (
    <div className={GUESTS_TABLE_FRAME_CLASS}>
      <Table className={GUESTS_TABLE_CLASS}>
        <TableHeader className="[&_tr]:border-0">
          <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnEmail}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnRole}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnLocationAccess}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnInvitedBy}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnSent}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnExpires}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
              {copy.columnActions}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow
              key={row.invitationId}
              className={GUESTS_TABLE_BODY_ROW_CLASS}
            >
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="inline-flex items-center gap-2">
                  <a
                    href={`mailto:${row.email}`}
                    className={OPERATOR_GUEST_CONTACT_LINK_CLASS}
                  >
                    {row.email}
                  </a>
                  {row.expired ? (
                    <Badge variant="soft">{copy.expired}</Badge>
                  ) : null}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.permissionRole}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm font-normal leading-[19px] text-foreground">
                  {row.locationAccessLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.invitedBy}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.sentLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.expiresLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_ACTIONS_CELL_CLASS}>
                {row.actions.length === 0 ? null : (
                  <div className="flex items-center justify-center">
                    <DropdownMenu modal={false}>
                      <DropdownMenuTrigger asChild>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          aria-label={`Actions for ${row.email}`}
                          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
                        >
                          <MoreVerticalIcon className="size-4" aria-hidden />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent
                        align="end"
                        className={GUESTS_ROW_ACTIONS_MENU_CLASS}
                      >
                        {row.actions.map((action) => (
                          <DropdownMenuItem
                            key={action}
                            className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                            onSelect={() => {
                              if (action === "resend") {
                                void pageModule.resendInvite(row.invitationId)
                              } else if (action === "revoke") {
                                pageModule.openRevoke(row.invitationId)
                              }
                            }}
                          >
                            {actionLabel(action)}
                          </DropdownMenuItem>
                        ))}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

function LocationAccessTable({
  rows,
  locations,
}: {
  rows: TeamMemberRow[]
  locations: Array<{ id: number; name: string }>
}) {
  return (
    <div className={GUESTS_TABLE_FRAME_CLASS}>
      <Table className={GUESTS_TABLE_CLASS}>
        <TableHeader className="[&_tr]:border-0">
          <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnUser}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnRole}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnAccess}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnLocations}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow key={row.membershipId} className={GUESTS_TABLE_BODY_ROW_CLASS}>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                  {row.fullName}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.permissionRole}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {copy.locationAccessSelectedOnly}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.namedLocationIds
                    .map(
                      (id) =>
                        locations.find((location) => location.id === id)
                          ?.name ?? `#${id}`
                    )
                    .join(", ")}
                </span>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

function MembersTable({
  rows,
  pageModule,
}: {
  rows: TeamMemberRow[]
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
}) {
  return (
    <div className={GUESTS_TABLE_FRAME_CLASS}>
      <Table className={GUESTS_TABLE_CLASS}>
        <TableHeader className="[&_tr]:border-0">
          <TableRow className={GUESTS_TABLE_HEAD_ROW_CLASS}>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnName}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnEmail}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnRole}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnLocationAccess}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnStatus}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_CELL_CLASS}>
              {copy.columnLastActive}
            </TableHead>
            <TableHead className={GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS}>
              {copy.columnActions}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map((row) => (
            <TableRow key={row.membershipId} className={GUESTS_TABLE_BODY_ROW_CLASS}>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_GUEST_NAME_CLASS}>
                  {row.fullName}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <a
                  href={`mailto:${row.email}`}
                  className={OPERATOR_GUEST_CONTACT_LINK_CLASS}
                >
                  {row.email}
                </a>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.permissionRole}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className="text-sm font-normal leading-[19px] text-foreground">
                  {row.locationAccessLabel}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <Badge
                  variant="soft"
                  className={GUESTS_MARKETING_STATUS_BADGE_CLASS}
                >
                  {row.status === "active"
                    ? copy.statusActive
                    : copy.statusInactive}
                </Badge>
              </TableCell>
              <TableCell className={GUESTS_TABLE_BODY_CELL_CLASS}>
                <span className={GUESTS_TABLE_LOCATION_CLASS}>
                  {row.lastActiveAt != null
                    ? formatAccessActivityOccurredAt(
                        row.lastActiveAt,
                        new Date()
                      )
                    : copy.lastActiveEmpty}
                </span>
              </TableCell>
              <TableCell className={GUESTS_TABLE_ACTIONS_CELL_CLASS}>
                {row.actions.length === 0 ? null : (
                  <div className="flex items-center justify-center">
                    <DropdownMenu modal={false}>
                      <DropdownMenuTrigger asChild>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          aria-label={`Actions for ${row.fullName}`}
                          className={GUESTS_ROW_ACTIONS_TRIGGER_CLASS}
                        >
                          <MoreVerticalIcon className="size-4" aria-hidden />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent
                        align="end"
                        className={GUESTS_ROW_ACTIONS_MENU_CLASS}
                      >
                        {row.actions.map((action) => (
                          <DropdownMenuItem
                            key={action}
                            className={GUESTS_ROW_ACTIONS_ITEM_CLASS}
                            onSelect={() => {
                              if (action === "view") {
                                pageModule.openViewMember(row.membershipId)
                              } else if (
                                action === "edit-role"
                                || action === "edit-access"
                              ) {
                                pageModule.openEditMember(row.membershipId)
                              } else if (action === "suspend") {
                                pageModule.openSuspend(row.membershipId)
                              } else if (action === "reactivate") {
                                void pageModule.confirmReactivate(
                                  row.membershipId
                                )
                              }
                            }}
                          >
                            {actionLabel(action)}
                          </DropdownMenuItem>
                        ))}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </div>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

function EditMemberDialog({
  snap,
  pageModule,
  roleOptions,
}: {
  snap: ReturnType<
    ReturnType<typeof useTeamPermissionsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
  roleOptions: string[]
}) {
  if (snap.dialog.kind !== "edit-member") {
    return null
  }

  const isView = snap.dialog.mode === "view"
  const member = snap.members.find(
    (row) => row.membershipId === snap.dialog.membershipId
  )

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !snap.busy) {
          pageModule.closeDialog()
        }
      }}
    >
      <DialogContent
        showCloseButton={false}
        className={TEAM_PERMISSIONS_INVITE_DIALOG_CONTENT_CLASS}
      >
        <div className={TEAM_PERMISSIONS_INVITE_BODY_STACK_CLASS}>
          <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className="pr-0 text-2xl font-bold tracking-normal text-op-text-primary">
                {isView ? copy.viewMemberTitle : copy.editMemberTitle}
              </DialogTitle>
              <DialogDescription className="max-w-none text-base font-medium leading-normal text-op-text-muted">
                {isView ? copy.viewMemberSubtitle : copy.editMemberSubtitle}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                disabled={snap.busy}
                aria-label="Close"
                className={CAPTURE_DIALOG_CLOSE_BUTTON_CLASS}
                onClick={() => pageModule.closeDialog()}
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <TeamMemberDialogForm
            idPrefix="edit-member"
            values={snap.dialog.draft}
            onChange={(values) => pageModule.setEditMemberDraft(values)}
            roleOptions={roleOptions}
            locations={snap.locations}
            isSingleLocation={
              snap.isSingleLocation || member?.isAccountOwner === true
            }
            busy={snap.busy}
            readOnly={isView}
            readOnlyIdentity
          />
        </div>

        <DialogFooter className="flex flex-row flex-wrap justify-start gap-3 sm:justify-start">
          {isView ? null : (
            <Button
              type="button"
              variant="op-primary"
              disabled={snap.busy}
              onClick={() => {
                void pageModule.confirmDialogPrimary()
              }}
            >
              {copy.save}
            </Button>
          )}
          <Button
            type="button"
            variant="op-tertiary"
            disabled={snap.busy}
            onClick={() => pageModule.closeDialog()}
          >
            {copy.cancel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

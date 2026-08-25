import { useEffect, useSyncExternalStore } from "react"
import { MoreVerticalIcon, XIcon } from "lucide-react"
import { useNavigate, useSearchParams } from "react-router-dom"

import { AccountWorkspaceConfirmDialog } from "@/components/dashboard/operator/AccountWorkspace/AccountWorkspaceConfirmDialog"
import { OperatorFilterSheetDialog } from "@/components/dashboard/operator/FilterSheet/OperatorFilterSheetDialog"
import { useTeamPermissionsPageModuleApi } from "@/components/dashboard/operator/TeamPermissions/utils/teamPermissionsPageModuleContext"
import { ACCOUNT_WORKSPACE_PAGE_COPY } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"
import {
  BROWSER_BACK_HREF,
  registerLeaveDirtyGuard,
} from "@/lib/operatorNavigation/leaveDirtyGuard"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { CheckboxLabel } from "@/components/ui/checkbox-label"
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { Textarea } from "@/components/ui/textarea"
import { teamPermissionsFilterSheetSchema } from "@/lib/operatorTeamPermissions/teamPermissionsFilterSheetSchema"
import { assignableRolesForActor } from "@/lib/operatorTeamPermissions/permissionRoles"
import type {
  AccessActivityViewRow,
  TeamInvitationRow,
  TeamMemberRow,
} from "@/lib/operatorTeamPermissions/createOperatorTeamPermissionsPageModule"
import {
  deactivateConfirmCopy,
  displayPermissionLabel,
  legalAdminLevels,
  PERMISSION_MATRIX_ROLES,
  removeConfirmCopy,
  revokeConfirmCopy,
  TEAM_PERMISSIONS_PAGE_COPY as copy,
  TEAM_PERMISSIONS_SELECT_MENU_CLASS,
} from "@/lib/operatorTeamPermissions/teamPermissionsPresentation"
import {
  GUESTS_KPI_CARD_CLASS,
  GUESTS_PAGE_HEADER_COPY_CLASS,
  GUESTS_PAGE_HEADER_ROW_CLASS,
  GUESTS_PAGE_STACK_CLASS,
  GUESTS_PAGE_SUBTITLE_CLASS,
  GUESTS_PAGE_TITLE_CLASS,
  GUESTS_ROW_ACTIONS_ITEM_CLASS,
  GUESTS_ROW_ACTIONS_MENU_CLASS,
  GUESTS_ROW_ACTIONS_TRIGGER_CLASS,
  GUESTS_SECTION_CLASS,
  GUESTS_SECTION_SUBTITLE_CLASS,
  GUESTS_SECTION_TITLE_CLASS,
  GUESTS_TABLE_BODY_CELL_CLASS,
  GUESTS_TABLE_BODY_ROW_CLASS,
  GUESTS_TABLE_CLASS,
  GUESTS_TABLE_HEAD_CELL_CLASS,
  GUESTS_TABLE_HEAD_ROW_CLASS,
} from "@/lib/operatorGuests/guestsPresentation"
import { cn } from "@/lib/utils"
import {
  CAPTURE_DIALOG_CLOSE_BUTTON_CLASS,
  CAPTURE_DIALOG_HEADER_ROW_CLASS,
} from "@/lib/operatorCapture/capturePresentation"

function actionLabel(action: string): string {
  switch (action) {
    case "change-role":
      return copy.changeRole
    case "change-location":
      return copy.changeLocation
    case "deactivate":
      return copy.deactivate
    case "reactivate":
      return copy.reactivate
    case "remove":
      return copy.remove
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
    snap.dialog.kind === "deactivate" || snap.dialog.kind === "remove"
      ? snap.dialog.membershipId
      : null
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
    snap.dialog.kind === "deactivate" && confirmMember != null
      ? deactivateConfirmCopy(confirmMember.fullName)
      : snap.dialog.kind === "remove" && confirmMember != null
        ? removeConfirmCopy(confirmMember.fullName)
        : snap.dialog.kind === "revoke"
          ? revokeConfirmCopy(revokeInvitation?.email ?? "")
          : null

  return (
    <div className={GUESTS_PAGE_STACK_CLASS}>
      <div className={GUESTS_PAGE_HEADER_ROW_CLASS}>
        <div className={GUESTS_PAGE_HEADER_COPY_CLASS}>
          <h1 className={GUESTS_PAGE_TITLE_CLASS}>{copy.title}</h1>
          <p className={GUESTS_PAGE_SUBTITLE_CLASS}>{copy.subtitle}</p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          {snap.actorCanManage ? (
            <Button
              type="button"
              variant="op-primary"
              onClick={() => pageModule.openInvite()}
            >
              {copy.invite}
            </Button>
          ) : null}
          <Button
            type="button"
            variant="op-secondary"
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
        className="gap-6"
      >
        <TabsList
          variant="line"
          className="h-auto w-full justify-start overflow-x-auto"
        >
          {snap.tabs.map((tab) => (
            <TabsTrigger
              key={tab.id}
              value={tab.id}
              className={cn(
                "rounded-none px-3 py-2 text-sm font-medium text-op-button-date-text",
                "data-active:font-semibold data-active:text-foreground",
                "group-data-[variant=line]/tabs-list:data-active:after:bg-op-button-primary-background"
              )}
            >
              {tab.label}
            </TabsTrigger>
          ))}
        </TabsList>

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
          className="gap-8 rounded-op-md bg-op-surface-secondary p-8 sm:max-w-[633px]"
        >
          <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3">
              <DialogTitle className="text-2xl font-bold">
                {copy.inviteTitle}
              </DialogTitle>
              <DialogDescription>{copy.inviteSubtitle}</DialogDescription>
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
          <div className="flex flex-col gap-4">
            <label className="text-sm font-medium">
              {copy.email}
              <Input
                value={snap.inviteDraft.email}
                disabled={snap.busy}
                onChange={(event) =>
                  pageModule.setInviteDraft({
                    ...snap.inviteDraft,
                    email: event.target.value,
                  })
                }
              />
              {snap.inviteEmailError != null ? (
                <p className="mt-1 text-sm text-destructive" role="alert">
                  {snap.inviteEmailError}
                </p>
              ) : null}
            </label>
            <label className="text-sm font-medium">
              {copy.fullName}
              <Input
                value={snap.inviteDraft.fullName}
                disabled={snap.busy}
                onChange={(event) =>
                  pageModule.setInviteDraft({
                    ...snap.inviteDraft,
                    fullName: event.target.value,
                  })
                }
              />
            </label>
            <label className="text-sm font-medium">
              {copy.role}
              <Select
                value={snap.inviteDraft.permissionRole}
                onValueChange={(value) =>
                  pageModule.setInviteDraft({
                    ...snap.inviteDraft,
                    permissionRole: value,
                  })
                }
                disabled={snap.busy}
              >
                <SelectTrigger className="h-8 w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent
                  position="popper"
                  className={TEAM_PERMISSIONS_SELECT_MENU_CLASS}
                >
                  {roleOptions.map((role) => (
                    <SelectItem key={role} value={role}>
                      {role}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </label>
            {snap.isSingleLocation ? null : (
              <div className="flex flex-col gap-2">
                <p className="text-sm font-medium">{copy.locationAccess}</p>
                <Select
                  value={snap.inviteDraft.locationScope}
                  onValueChange={(value) =>
                    pageModule.setInviteDraft({
                      ...snap.inviteDraft,
                      locationScope: value as "all" | "named",
                    })
                  }
                  disabled={snap.busy}
                >
                  <SelectTrigger className="h-8 w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent
                    position="popper"
                    className={TEAM_PERMISSIONS_SELECT_MENU_CLASS}
                  >
                    <SelectItem value="all">{copy.allLocations}</SelectItem>
                    <SelectItem value="named">
                      {copy.selectedLocations}
                    </SelectItem>
                  </SelectContent>
                </Select>
                {snap.inviteDraft.locationScope === "all" ? (
                  <p className="text-sm text-muted-foreground">
                    {copy.allLocationsHelper}
                  </p>
                ) : (
                  snap.locations.map((location) => (
                    <CheckboxLabel
                      key={location.id}
                      id={`invite-loc-${location.id}`}
                      checked={snap.inviteDraft.namedLocationIds.includes(
                        location.id
                      )}
                      onCheckedChange={(checked) => {
                        pageModule.setInviteDraft({
                          ...snap.inviteDraft,
                          namedLocationIds: checked
                            ? [...snap.inviteDraft.namedLocationIds, location.id]
                            : snap.inviteDraft.namedLocationIds.filter(
                                (id) => id !== location.id
                              ),
                        })
                      }}
                    >
                      {location.name}
                    </CheckboxLabel>
                  ))
                )}
              </div>
            )}
            <label className="text-sm font-medium">
              {copy.message}
              <Textarea
                placeholder={copy.messagePlaceholder}
                value={snap.inviteDraft.message}
                disabled={snap.busy}
                onChange={(event) =>
                  pageModule.setInviteDraft({
                    ...snap.inviteDraft,
                    message: event.target.value,
                  })
                }
              />
            </label>
          </div>
          <DialogFooter className="sm:justify-start">
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

      <ChangeRoleDialog snap={snap} pageModule={pageModule} />
      <ChangeLocationDialog snap={snap} pageModule={pageModule} />

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
    <section className={GUESTS_SECTION_CLASS}>
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.matrixTitle}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.matrixSubtitle}</p>
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

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-5">
        <StatCard
          label={copy.teamMembers}
          value={`${snap.stats.activeMembers} ${copy.statActive}`}
        />
        <StatCard
          label={copy.pendingInvites}
          value={`${snap.stats.pendingInvites} ${copy.statPending}`}
        />
        <StatCard label={copy.owners} value={copy.statOwner} />
        <StatCard
          label={copy.locationManagers}
          value={`${snap.stats.locationManagers} ${copy.statManagers}`}
        />
        <StatCard
          label={copy.limitedAccessUsers}
          value={`${snap.stats.limitedAccessUsers} ${copy.statRestricted}`}
        />
      </div>

      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>{copy.membersTitle}</h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>{copy.membersSubtitle}</p>
        </div>
        <div className="flex flex-wrap gap-3">
          <Input
            value={snap.searchQuery}
            placeholder={copy.searchPlaceholder}
            onChange={(event) => pageModule.setSearchQuery(event.target.value)}
            className="max-w-md"
          />
          <Button
            type="button"
            variant="op-secondary"
            onClick={() => pageModule.openFilters()}
          >
            {copy.filters}
            {snap.filterChips.length > 0
              ? ` (${snap.filterChips.length})`
              : ""}
          </Button>
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
        <section className={GUESTS_SECTION_CLASS}>
          <div className="flex flex-col gap-2">
            <h2 className={GUESTS_SECTION_TITLE_CLASS}>
              {copy.locationAccessCardTitle}
            </h2>
            <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
              {copy.locationAccessCardSubtitle}
            </p>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-op-border-default">
                  <th className="py-2 font-semibold">User</th>
                  <th className="py-2 font-semibold">Role</th>
                  <th className="py-2 font-semibold">Access</th>
                  <th className="py-2 font-semibold">Locations</th>
                </tr>
              </thead>
              <tbody>
                {snap.namedListMembers.map((row) => (
                  <tr
                    key={row.membershipId}
                    className="border-b border-op-border-default"
                  >
                    <td className="py-3">{row.fullName}</td>
                    <td>{row.permissionRole}</td>
                    <td>Selected only</td>
                    <td>
                      {row.namedLocationIds
                        .map(
                          (id) =>
                            snap.locations.find((location) => location.id === id)
                              ?.name ?? `#${id}`
                        )
                        .join(", ")}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}
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
      <section className={GUESTS_SECTION_CLASS}>
        <div className="flex flex-col gap-2">
          <h2 className={GUESTS_SECTION_TITLE_CLASS}>
            {copy.accessActivityTitle}
          </h2>
          <p className={GUESTS_SECTION_SUBTITLE_CLASS}>
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

  if (snap.invitations.length === 0) {
    return (
      <div className="flex flex-col items-start gap-3 py-8">
        <p className="m-0 font-semibold">{copy.invitationsEmptyTitle}</p>
        <p className="m-0 text-muted-foreground">
          {copy.invitationsEmptyHelper}
        </p>
        {snap.actorCanManage ? (
          <Button
            type="button"
            variant="op-primary"
            onClick={() => pageModule.openInvite()}
          >
            {copy.invite}
          </Button>
        ) : null}
      </div>
    )
  }

  return (
    <InvitationsTable rows={snap.invitations} pageModule={pageModule} />
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
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-op-border-default">
            <th className="py-2 font-semibold">{copy.email}</th>
            <th className="py-2 font-semibold">{copy.role}</th>
            <th className="py-2 font-semibold">{copy.locationAccess}</th>
            <th className="py-2 font-semibold">Invited by</th>
            <th className="py-2 font-semibold">Sent</th>
            <th className="py-2 font-semibold">Expires</th>
            <th className="py-2 font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={row.invitationId}
              className="border-b border-op-border-default"
            >
              <td className="py-3">
                <span className="inline-flex items-center gap-2">
                  {row.email}
                  {row.expired ? (
                    <Badge variant="soft">{copy.expired}</Badge>
                  ) : null}
                </span>
              </td>
              <td>{row.permissionRole}</td>
              <td>{row.locationAccessLabel}</td>
              <td>{row.invitedBy}</td>
              <td>{row.sentLabel}</td>
              <td>{row.expiresLabel}</td>
              <td>
                {row.actions.length === 0 ? null : (
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
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className={GUESTS_KPI_CARD_CLASS}>
      <p className="m-0 text-sm text-muted-foreground">{label}</p>
      <p className="m-0 text-lg font-semibold">{value}</p>
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
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-op-border-default">
            <th className="py-2 font-semibold">Name</th>
            <th className="py-2 font-semibold">Email</th>
            <th className="py-2 font-semibold">Role</th>
            <th className="py-2 font-semibold">Location access</th>
            <th className="py-2 font-semibold">Status</th>
            <th className="py-2 font-semibold">Actions</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr
              key={row.membershipId}
              className="border-b border-op-border-default"
            >
              <td className="py-3">{row.fullName}</td>
              <td>{row.email}</td>
              <td>{row.permissionRole}</td>
              <td>{row.locationAccessLabel}</td>
              <td>
                <Badge variant={row.status === "active" ? "positive" : "soft"}>
                  {row.status === "active" ? "Active" : "Deactivated"}
                </Badge>
              </td>
              <td>
                {row.actions.length === 0 ? null : (
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
                            if (action === "change-role") {
                              pageModule.openChangeRole(row.membershipId)
                            } else if (action === "change-location") {
                              pageModule.openChangeLocation(row.membershipId)
                            } else if (action === "deactivate") {
                              pageModule.openDeactivate(row.membershipId)
                            } else if (action === "reactivate") {
                              void pageModule.confirmReactivate(
                                row.membershipId
                              )
                            } else if (action === "remove") {
                              pageModule.openRemove(row.membershipId)
                            }
                          }}
                        >
                          {actionLabel(action)}
                        </DropdownMenuItem>
                      ))}
                    </DropdownMenuContent>
                  </DropdownMenu>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function ChangeRoleDialog({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useTeamPermissionsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
}) {
  if (snap.dialog.kind !== "change-role") {
    return null
  }
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
        className="gap-8 rounded-op-md bg-op-surface-secondary p-8 sm:max-w-[480px]"
      >
        <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
          <DialogHeader>
            <DialogTitle>{copy.changeRole}</DialogTitle>
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
        <Select
          value={snap.dialog.draftRole}
          onValueChange={(value) => pageModule.setChangeRoleDraft(value)}
          disabled={snap.busy}
        >
          <SelectTrigger className="h-8 w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent
            position="popper"
            className={TEAM_PERMISSIONS_SELECT_MENU_CLASS}
          >
            {assignableRolesForActor(snap.actorPermissionRole).map((role) => (
              <SelectItem key={role} value={role}>
                {role}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <DialogFooter className="sm:justify-start">
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

function ChangeLocationDialog({
  snap,
  pageModule,
}: {
  snap: ReturnType<
    ReturnType<typeof useTeamPermissionsPageModuleApi>["getSnapshot"]
  >
  pageModule: ReturnType<typeof useTeamPermissionsPageModuleApi>
}) {
  if (snap.dialog.kind !== "change-location") {
    return null
  }
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
        className="gap-8 rounded-op-md bg-op-surface-secondary p-8 sm:max-w-[480px]"
      >
        <div className={CAPTURE_DIALOG_HEADER_ROW_CLASS}>
          <DialogHeader>
            <DialogTitle>{copy.changeLocation}</DialogTitle>
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
        <Select
          value={snap.dialog.draftScope}
          onValueChange={(value) =>
            pageModule.setChangeLocationDraft(
              value as "all" | "named",
              snap.dialog.kind === "change-location"
                ? snap.dialog.draftNamedIds
                : []
            )
          }
          disabled={snap.busy}
        >
          <SelectTrigger className="h-8 w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent
            position="popper"
            className={TEAM_PERMISSIONS_SELECT_MENU_CLASS}
          >
            <SelectItem value="all">{copy.allLocations}</SelectItem>
            <SelectItem value="named">{copy.selectedLocations}</SelectItem>
          </SelectContent>
        </Select>
        {snap.dialog.draftScope === "named"
          ? snap.locations.map((location) => (
              <CheckboxLabel
                key={location.id}
                id={`change-loc-${location.id}`}
                checked={snap.dialog.kind === "change-location"
                  && snap.dialog.draftNamedIds.includes(location.id)}
                onCheckedChange={(checked) => {
                  if (snap.dialog.kind !== "change-location") {
                    return
                  }
                  const next = checked
                    ? [...snap.dialog.draftNamedIds, location.id]
                    : snap.dialog.draftNamedIds.filter(
                        (id) => id !== location.id
                      )
                  pageModule.setChangeLocationDraft("named", next)
                }}
              >
                {location.name}
              </CheckboxLabel>
            ))
          : (
            <p className="m-0 text-sm text-muted-foreground">
              {copy.allLocationsHelper}
            </p>
          )}
        <DialogFooter className="sm:justify-start">
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

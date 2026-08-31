import {
  commitPending,
  emptySelection,
  getMultiSelectIds,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import { teamPermissionsFilterSheetSchema } from "@/lib/operatorTeamPermissions/teamPermissionsFilterSheetSchema"
import { assignableRolesForActor } from "@/lib/operatorTeamPermissions/permissionRoles"
import { isTeamPermissionsMatrixEditEnabled } from "@/lib/env"
import type { PlanEntitlementsAccountSnapshot } from "@/lib/planEntitlements/planEntitlementsPresentation"
import {
  normalizePlanEntitlementsAccount,
  teamMemberCapReachedMessage,
} from "@/lib/planEntitlements/planEntitlementsPresentation"
import {
  legalAdminLevels,
  resolveTeamPermissionsTabId,
  TEAM_PERMISSIONS_TAB_IDS,
  formatAccessActivityCopy,
  formatAccessActivityOccurredAt,
  type TeamPermissionsTabId,
} from "@/lib/operatorTeamPermissions/teamPermissionsPresentation"

export { TEAM_PERMISSIONS_TAB_IDS, resolveTeamPermissionsTabId }
export type { TeamPermissionsTabId }

export type TeamMemberRow = {
  membershipId: number
  userId: number
  fullName: string
  email: string
  permissionRole: string
  locationScope: "all" | "named"
  namedLocationIds: number[]
  locationAccessLabel: string
  status: "active" | "deactivated"
  isAccountOwner: boolean
  lastActiveAt: string | null
  actions: string[]
}

export type TeamMemberFormDraft = {
  email: string
  fullName: string
  permissionRole: string
  locationScope: "all" | "named"
  namedLocationIds: number[]
}

export type PermissionMatrixArea = {
  id: string
  label: string
  cells: Record<string, string>
}

export type AdminMatrixCell = {
  areaId: string
  level: string
}

export type TeamInvitationRow = {
  invitationId: number
  email: string
  permissionRole: string
  locationAccessLabel: string
  invitedBy: string
  sentLabel: string
  expiresLabel: string
  expired: boolean
  actions: string[]
}

export type AccessActivityItem = {
  id: number
  kind: string
  occurredAt: string
  actorDisplayName: string
  targetDisplayName: string | null
  targetEmail: string | null
  fromValue: string | null
  toValue: string | null
}

export type AccessActivityList = {
  items: AccessActivityItem[]
  totalCount: number
  page: number
  pageSize: number
}

export type AccessActivityViewRow = {
  id: number
  occurredAtLabel: string
  sentence: string
}

export type TeamInviteDraft = {
  email: string
  fullName: string
  permissionRole: string
  locationScope: "all" | "named"
  namedLocationIds: number[]
  message: string
}

export type TeamPermissionsPageData = {
  actorCanManage: boolean
  actorPermissionRole: string
  privacyConsentHasAccess: boolean
  isSingleLocation: boolean
  stats: {
    activeMembers: number
    pendingInvites: number
    locationManagers: number
    limitedAccessUsers: number
  }
  locations: Array<{ id: number; name: string }>
  members: TeamMemberRow[]
  matrix: PermissionMatrixArea[]
  invitations: TeamInvitationRow[]
  entitlements: PlanEntitlementsAccountSnapshot
}

export type TeamPermissionsPageAdapters = {
  getPage: () => Promise<TeamPermissionsPageData>
  updateMemberProfile: (
    membershipId: number,
    payload: {
      permissionRole?: string
      locationScope?: {
        locationScope: "all" | "named"
        namedLocationIds: number[]
      }
    }
  ) => Promise<void>
  reactivate: (membershipId: number) => Promise<void>
  remove: (membershipId: number) => Promise<void>
  saveMatrix: (cells: AdminMatrixCell[]) => Promise<void>
  sendInvite: (payload: TeamInviteDraft) => Promise<void>
  resendInvite: (invitationId: number) => Promise<void>
  revokeInvite: (invitationId: number) => Promise<void>
  getAccessActivity: (params: {
    page: number
    pageSize: number
  }) => Promise<AccessActivityList>
}

export type TeamPermissionsDialog =
  | { kind: "none" }
  | { kind: "notes" }
  | { kind: "invite" }
  | {
      kind: "edit-member"
      membershipId: number
      mode: "view" | "edit"
      draft: TeamMemberFormDraft
    }
  | { kind: "suspend"; membershipId: number }
  | { kind: "revoke"; invitationId: number }

export type TeamPermissionsSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  activeTabId: TeamPermissionsTabId
  tabs: Array<{ id: TeamPermissionsTabId; label: string }>
  actorCanManage: boolean
  actorPermissionRole: string
  privacyConsentHasAccess: boolean
  isSingleLocation: boolean
  stats: TeamPermissionsPageData["stats"]
  locations: TeamPermissionsPageData["locations"]
  members: TeamMemberRow[]
  visibleMembers: TeamMemberRow[]
  namedListMembers: TeamMemberRow[]
  invitations: TeamInvitationRow[]
  inviteDraft: TeamInviteDraft
  inviteEmailError: string | null
  searchQuery: string
  filtersSession: FilterSheetSession | null
  filterChips: FilterChip[]
  filtersOpen: boolean
  dialog: TeamPermissionsDialog
  busy: boolean
  matrix: PermissionMatrixArea[]
  isDirty: boolean
  canEditAdminColumn: boolean
  saveEnabled: boolean
  leaveDirtyOpen: boolean
  pendingNavigationHref: string | null
  accessActivityPreview: AccessActivityViewRow[]
  accessActivityEmpty: boolean
  auditLogOpen: boolean
  auditLogRows: AccessActivityViewRow[]
  auditLogPage: number
  auditLogPageSize: number
  auditLogTotalCount: number
  auditLogHasNext: boolean
  auditLogHasPrevious: boolean
  entitlements: PlanEntitlementsAccountSnapshot
  inviteAtCap: boolean
  teamMembersUsageLabel: string
}

export type OperatorTeamPermissionsPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => TeamPermissionsSnapshot
  load: () => Promise<void>
  setActiveTabFromUrl: (raw: string | null) => void
  requestTabChange: (tabId: TeamPermissionsTabId) => void
  setSearchQuery: (query: string) => void
  setFiltersSession: (session: FilterSheetSession) => void
  setFiltersOpen: (open: boolean) => void
  openFilters: () => void
  applyFilters: () => void
  clearFiltersAndSearch: () => void
  removeFilterChip: (chip: FilterChip) => void
  openNotes: () => void
  openInvite: () => void
  openViewMember: (membershipId: number) => void
  openEditMember: (membershipId: number) => void
  openSuspend: (membershipId: number) => void
  openRevoke: (invitationId: number) => void
  resendInvite: (invitationId: number) => Promise<void>
  closeDialog: () => void
  setInviteDraft: (draft: TeamInviteDraft) => void
  setEditMemberDraft: (draft: TeamMemberFormDraft) => void
  confirmReactivate: (membershipId: number) => Promise<void>
  confirmDialogPrimary: () => Promise<void>
  setAdminCell: (areaId: string, level: string) => void
  requestSave: () => Promise<void>
  requestNavigateAway: (href: string) => boolean
  confirmLeaveDirtySave: () => Promise<void>
  confirmLeaveDirtyCancel: () => Promise<void>
  closeLeaveDirty: () => void
  consumePendingNavigation: () => string | null
  openAuditLog: () => Promise<void>
  closeAuditLog: () => void
  goToNextAuditPage: () => Promise<void>
  goToPreviousAuditPage: () => Promise<void>
}

const TAB_LABELS: Record<TeamPermissionsTabId, string> = {
  members: "Members",
  "roles-permissions": "Roles & permissions",
  invitations: "Invitations",
  "access-activity": "Access activity",
}

function emptyStats(): TeamPermissionsPageData["stats"] {
  return {
    activeMembers: 0,
    pendingInvites: 0,
    locationManagers: 0,
    limitedAccessUsers: 0,
  }
}

function emptyInviteDraft(actorRole: string): TeamInviteDraft {
  const roles = assignableRolesForActor(actorRole)
  return {
    email: "",
    fullName: "",
    permissionRole: roles[0] ?? "Reporting Only",
    locationScope: "all",
    namedLocationIds: [],
    message: "",
  }
}

function needsNamedLocations(role: string): boolean {
  return role === "Area Manager" || role === "Location Manager"
}

function applyMemberFormDraft(draft: TeamMemberFormDraft): TeamMemberFormDraft {
  if (!needsNamedLocations(draft.permissionRole)) {
    return draft
  }
  return { ...draft, locationScope: "named" }
}

function memberFormDraftFromRow(row: TeamMemberRow): TeamMemberFormDraft {
  return {
    email: row.email,
    fullName: row.fullName,
    permissionRole: row.permissionRole,
    locationScope: row.locationScope,
    namedLocationIds: [...row.namedLocationIds],
  }
}

function sameLocationIds(left: number[], right: number[]): boolean {
  if (left.length !== right.length) {
    return false
  }
  const sortedLeft = [...left].sort((a, b) => a - b)
  const sortedRight = [...right].sort((a, b) => a - b)
  return sortedLeft.every((id, index) => id === sortedRight[index])
}

function applyInviteDraft(draft: TeamInviteDraft): TeamInviteDraft {
  if (!needsNamedLocations(draft.permissionRole)) {
    return draft
  }
  return { ...draft, locationScope: "named" }
}

function memberMatches(
  row: TeamMemberRow,
  searchQuery: string,
  applied: OperatorFilterSelection | undefined,
  isSingleLocation: boolean
): boolean {
  const q = searchQuery.trim().toLowerCase()
  if (q !== "") {
    const hay = `${row.fullName} ${row.email}`.toLowerCase()
    if (!hay.includes(q)) {
      return false
    }
  }
  if (applied == null) {
    return true
  }
  const statuses = getMultiSelectIds(applied, "status")
  if (statuses.length > 0 && !statuses.includes(row.status)) {
    return false
  }
  const roles = getMultiSelectIds(applied, "role")
  if (roles.length > 0 && !roles.includes(row.permissionRole)) {
    return false
  }
  if (!isSingleLocation) {
    const locationIds = getMultiSelectIds(applied, "location")
    if (locationIds.length > 0) {
      const match =
        row.locationScope === "all"
        || row.namedLocationIds.some((id) => locationIds.includes(String(id)))
      if (!match) {
        return false
      }
    }
  }
  return true
}

export function createOperatorTeamPermissionsPageModule(
  adapters: TeamPermissionsPageAdapters,
  options: {
    initialTabId?: string | null
    getNow?: () => Date
    /** When false, the Admin permission matrix is read-only (production default). */
    matrixEditEnabled?: boolean
  } = {}
): OperatorTeamPermissionsPageModule {
  const matrixEditEnabled =
    options.matrixEditEnabled ?? isTeamPermissionsMatrixEditEnabled()
  let privacyConsentHasAccess = true
  let data: TeamPermissionsPageData | null = null
  let loadStatus: TeamPermissionsSnapshot["loadStatus"] = "idle"
  let activeTabId = resolveTeamPermissionsTabId(
    options.initialTabId,
    true
  )
  let searchQuery = ""
  let filtersSession: FilterSheetSession | null = null
  let filtersOpen = false
  let dialog: TeamPermissionsDialog = { kind: "none" }
  let busy = false
  let adminDraft: Record<string, string> = {}
  let leaveDirtyOpen = false
  let pendingLeave:
    | { kind: "tab"; tabId: TeamPermissionsTabId }
    | { kind: "href"; href: string }
    | null = null
  let pendingNavigationHref: string | null = null
  let inviteDraft = emptyInviteDraft("")
  let inviteEmailError: string | null = null
  let accessActivityPreview: AccessActivityViewRow[] = []
  let accessActivityEmpty = true
  let auditLogOpen = false
  let auditLogRows: AccessActivityViewRow[] = []
  let auditLogPage = 1
  let auditLogPageSize = 20
  let auditLogTotalCount = 0
  const listeners = new Set<() => void>()
  const getNow = options.getNow ?? (() => new Date())
  let snapshot: TeamPermissionsSnapshot

  const emit = () => {
    snapshot = projectSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  const canEditAdminColumn = () =>
    matrixEditEnabled && data?.actorPermissionRole === "Owner"

  const projectedMatrix = (): PermissionMatrixArea[] => {
    return (data?.matrix ?? []).map((area) => ({
      ...area,
      cells: {
        ...area.cells,
        Admin: adminDraft[area.id] ?? area.cells.Admin,
      },
    }))
  }

  const dirtyCells = (): AdminMatrixCell[] => {
    return (data?.matrix ?? [])
      .filter((area) => {
        const draft = adminDraft[area.id]
        return draft != null && draft !== area.cells.Admin
      })
      .map((area) => ({
        areaId: area.id,
        level: adminDraft[area.id],
      }))
  }

  const isDirty = () => dirtyCells().length > 0

  const continuePendingLeave = () => {
    if (pendingLeave == null) {
      return
    }
    if (pendingLeave.kind === "tab") {
      activeTabId = resolveTeamPermissionsTabId(
        pendingLeave.tabId,
        privacyConsentHasAccess
      )
    } else {
      pendingNavigationHref = pendingLeave.href
    }
    pendingLeave = null
  }

  const persistMatrix = async (): Promise<boolean> => {
    const cells = dirtyCells()
    if (cells.length === 0) {
      return true
    }
    busy = true
    emit()
    try {
      await adapters.saveMatrix(cells)
      if (data != null) {
        const saved = new Map(cells.map((cell) => [cell.areaId, cell.level]))
        data = {
          ...data,
          matrix: data.matrix.map((area) => {
            const next = saved.get(area.id)
            if (next == null) {
              return area
            }
            return {
              ...area,
              cells: { ...area.cells, Admin: next },
            }
          }),
        }
      }
      adminDraft = {}
      return true
    } catch {
      return false
    } finally {
      busy = false
      emit()
    }
  }

  const visibleTabs = (): TeamPermissionsSnapshot["tabs"] => {
    return TEAM_PERMISSIONS_TAB_IDS.filter((id) => {
      if (id === "access-activity" && !privacyConsentHasAccess) {
        return false
      }
      return true
    }).map((id) => ({ id, label: TAB_LABELS[id] }))
  }

  const projectSnapshot = (): TeamPermissionsSnapshot => {
    const members = data?.members ?? []
    const isSingleLocation = data?.isSingleLocation ?? true
    const schema = teamPermissionsFilterSheetSchema({
      isSingleLocation,
      locations: data?.locations ?? [],
    })
    const applied = filtersSession?.applied
    const visibleMembers = members.filter((row) =>
      memberMatches(row, searchQuery, applied, isSingleLocation)
    )
    const namedListMembers = members.filter(
      (row) =>
        row.status === "active" && row.locationScope === "named"
    )
    const dirty = isDirty()
    return {
      loadStatus,
      activeTabId: resolveTeamPermissionsTabId(
        activeTabId,
        privacyConsentHasAccess
      ),
      tabs: visibleTabs(),
      actorCanManage: data?.actorCanManage ?? false,
      actorPermissionRole: data?.actorPermissionRole ?? "",
      privacyConsentHasAccess,
      isSingleLocation,
      stats: data?.stats ?? emptyStats(),
      locations: data?.locations ?? [],
      members,
      visibleMembers,
      namedListMembers,
      invitations: data?.invitations ?? [],
      inviteDraft,
      inviteEmailError,
      searchQuery,
      filtersSession,
      filterChips:
        filtersSession == null
          ? []
          : projectChips(schema, filtersSession.applied),
      filtersOpen,
      dialog,
      busy,
      matrix: projectedMatrix(),
      isDirty: dirty,
      canEditAdminColumn: canEditAdminColumn(),
      saveEnabled: dirty && canEditAdminColumn() && !busy,
      leaveDirtyOpen,
      pendingNavigationHref,
      accessActivityPreview,
      accessActivityEmpty,
      auditLogOpen,
      auditLogRows,
      auditLogPage,
      auditLogPageSize,
      auditLogTotalCount,
      auditLogHasNext: auditLogPage * auditLogPageSize < auditLogTotalCount,
      auditLogHasPrevious: auditLogPage > 1,
      entitlements: data?.entitlements ?? normalizePlanEntitlementsAccount(null),
      inviteAtCap: data?.entitlements.teamMembers.atCap ?? false,
      teamMembersUsageLabel: formatTeamMembersUsageLabel(
        data?.entitlements.teamMembers
      ),
    }
  }

function formatTeamMembersUsageLabel(
  limit: PlanEntitlementsAccountSnapshot["teamMembers"] | undefined
): string {
  if (limit == null || !limit.available || limit.cap < 1) {
    return ""
  }
  return `${limit.current} of ${limit.cap} team users`
}

  snapshot = projectSnapshot()

  const mapRows = (items: AccessActivityItem[]): AccessActivityViewRow[] => {
    const now = getNow()
    return items.map((item) => ({
      id: item.id,
      occurredAtLabel: formatAccessActivityOccurredAt(item.occurredAt, now),
      sentence: formatAccessActivityCopy({
        kind: item.kind,
        actorDisplayName: item.actorDisplayName,
        targetDisplayName: item.targetDisplayName,
        fromValue: item.fromValue,
        toValue: item.toValue,
      }),
    }))
  }

  const loadPreview = async () => {
    if (!privacyConsentHasAccess) {
      accessActivityPreview = []
      accessActivityEmpty = true
      return
    }
    const list = await adapters.getAccessActivity({ page: 1, pageSize: 10 })
    accessActivityPreview = mapRows(list.items)
    accessActivityEmpty = list.totalCount === 0
  }

  const loadAuditPage = async (page: number) => {
    const list = await adapters.getAccessActivity({ page, pageSize: 20 })
    auditLogRows = mapRows(list.items)
    auditLogPage = list.page
    auditLogPageSize = list.pageSize
    auditLogTotalCount = list.totalCount
  }

  const reload = async () => {
    loadStatus = "loading"
    emit()
    try {
      data = await adapters.getPage()
      data = {
        ...data,
        matrix: data.matrix ?? [],
        invitations: data.invitations ?? [],
        entitlements: normalizePlanEntitlementsAccount(
          data.entitlements as Record<string, unknown> | undefined
        ),
      }
      privacyConsentHasAccess = data.privacyConsentHasAccess
      activeTabId = resolveTeamPermissionsTabId(
        activeTabId,
        privacyConsentHasAccess
      )
      adminDraft = {}
      await loadPreview()
      loadStatus = "loaded"
    } catch {
      loadStatus = "error"
    }
    emit()
  }

  const findMember = (membershipId: number) =>
    data?.members.find((row) => row.membershipId === membershipId) ?? null

  const findInvitation = (invitationId: number) =>
    data?.invitations.find((row) => row.invitationId === invitationId) ?? null

  const runWrite = async (work: () => Promise<void>) => {
    busy = true
    emit()
    try {
      await work()
      await reload()
      dialog = { kind: "none" }
      inviteEmailError = null
    } catch {
      // Keep the current dialog. The adapter may surface the error.
    } finally {
      busy = false
      emit()
    }
  }

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot: () => snapshot,
    load: reload,
    setActiveTabFromUrl: (raw) => {
      activeTabId = resolveTeamPermissionsTabId(raw, privacyConsentHasAccess)
      emit()
    },
    requestTabChange: (tabId) => {
      const next = resolveTeamPermissionsTabId(
        tabId,
        privacyConsentHasAccess
      )
      if (next === activeTabId) {
        return
      }
      if (isDirty()) {
        pendingLeave = { kind: "tab", tabId: next }
        leaveDirtyOpen = true
        emit()
        return
      }
      activeTabId = next
      emit()
    },
    setSearchQuery: (query) => {
      searchQuery = query
      emit()
    },
    setFiltersSession: (session) => {
      filtersSession = session
      emit()
    },
    setFiltersOpen: (open) => {
      filtersOpen = open
      if (!open) {
        emit()
        return
      }
      const schema = teamPermissionsFilterSheetSchema({
        isSingleLocation: data?.isSingleLocation ?? true,
        locations: data?.locations ?? [],
      })
      filtersSession =
        filtersSession ?? openSession(emptySelection(schema))
      emit()
    },
    openFilters: () => {
      const schema = teamPermissionsFilterSheetSchema({
        isSingleLocation: data?.isSingleLocation ?? true,
        locations: data?.locations ?? [],
      })
      filtersSession =
        filtersSession ?? openSession(emptySelection(schema))
      filtersOpen = true
      emit()
    },
    applyFilters: () => {
      if (filtersSession == null) {
        return
      }
      filtersSession = commitPending(filtersSession)
      filtersOpen = false
      emit()
    },
    clearFiltersAndSearch: () => {
      searchQuery = ""
      filtersSession = null
      emit()
    },
    removeFilterChip: (chip) => {
      if (filtersSession == null) {
        return
      }
      const schema = teamPermissionsFilterSheetSchema({
        isSingleLocation: data?.isSingleLocation ?? true,
        locations: data?.locations ?? [],
      })
      filtersSession = openSession(
        removeAppliedChip(schema, filtersSession.applied, chip)
      )
      emit()
    },
    openNotes: () => {
      dialog = { kind: "notes" }
      emit()
    },
    openInvite: () => {
      if (!(data?.actorCanManage ?? false)) {
        return
      }
      if (data.entitlements.teamMembers.atCap) {
        inviteEmailError = teamMemberCapReachedMessage(
          data.entitlements.teamMembers
        )
        dialog = { kind: "invite" }
        emit()
        return
      }
      inviteDraft = emptyInviteDraft(data?.actorPermissionRole ?? "")
      inviteEmailError = null
      dialog = { kind: "invite" }
      emit()
    },
    openViewMember: (membershipId) => {
      const row = findMember(membershipId)
      if (row == null || !row.actions.includes("view")) {
        return
      }
      dialog = {
        kind: "edit-member",
        membershipId,
        mode: "view",
        draft: memberFormDraftFromRow(row),
      }
      emit()
    },
    openEditMember: (membershipId) => {
      const row = findMember(membershipId)
      if (
        row == null
        || (!row.actions.includes("edit-role")
          && !row.actions.includes("edit-access"))
      ) {
        return
      }
      dialog = {
        kind: "edit-member",
        membershipId,
        mode: "edit",
        draft: memberFormDraftFromRow(row),
      }
      emit()
    },
    openSuspend: (membershipId) => {
      const row = findMember(membershipId)
      if (row == null || !row.actions.includes("suspend")) {
        return
      }
      dialog = { kind: "suspend", membershipId }
      emit()
    },
    openRevoke: (invitationId) => {
      const row = findInvitation(invitationId)
      if (row == null || !row.actions.includes("revoke") || busy) {
        return
      }
      dialog = { kind: "revoke", invitationId }
      emit()
    },
    resendInvite: async (invitationId) => {
      const row = findInvitation(invitationId)
      if (row == null || !row.actions.includes("resend") || busy) {
        return
      }
      await runWrite(async () => {
        await adapters.resendInvite(invitationId)
      })
    },
    closeDialog: () => {
      if (busy) {
        return
      }
      dialog = { kind: "none" }
      inviteEmailError = null
      emit()
    },
    setInviteDraft: (draft) => {
      if (dialog.kind !== "invite") {
        return
      }
      inviteDraft = applyInviteDraft(draft)
      inviteEmailError = null
      emit()
    },
    setEditMemberDraft: (draft) => {
      if (dialog.kind !== "edit-member") {
        return
      }
      dialog = {
        ...dialog,
        draft: applyMemberFormDraft(draft),
      }
      emit()
    },
    confirmReactivate: async (membershipId) => {
      const row = findMember(membershipId)
      if (row == null || !row.actions.includes("reactivate") || busy) {
        return
      }
      busy = true
      emit()
      try {
        await adapters.reactivate(membershipId)
        await reload()
        dialog = { kind: "none" }
      } finally {
        busy = false
        emit()
      }
    },
    confirmDialogPrimary: async () => {
      if (busy) {
        return
      }
      if (dialog.kind === "invite") {
        busy = true
        inviteEmailError = null
        emit()
        try {
          await adapters.sendInvite(inviteDraft)
          await reload()
          dialog = { kind: "none" }
          inviteDraft = emptyInviteDraft(data?.actorPermissionRole ?? "")
          inviteEmailError = null
        } catch (error) {
          inviteEmailError =
            error instanceof Error
              ? error.message
              : "Could not send invite."
        } finally {
          busy = false
          emit()
        }
        return
      }
      if (dialog.kind === "notes") {
        dialog = { kind: "none" }
        emit()
        return
      }
      if (dialog.kind === "edit-member") {
        if (dialog.mode === "view") {
          return
        }
        const row = findMember(dialog.membershipId)
        if (row == null) {
          return
        }
        const draft = applyMemberFormDraft(dialog.draft)
        const roleChanged = draft.permissionRole !== row.permissionRole
        const scopeChanged =
          draft.locationScope !== row.locationScope
          || !sameLocationIds(draft.namedLocationIds, row.namedLocationIds)
        if (
          needsNamedLocations(draft.permissionRole)
          && (draft.locationScope !== "named"
            || draft.namedLocationIds.length === 0)
        ) {
          return
        }
        busy = true
        emit()
        try {
          await adapters.updateMemberProfile(dialog.membershipId, {
            ...(roleChanged ? { permissionRole: draft.permissionRole } : {}),
            ...(scopeChanged && !row.isAccountOwner
              ? {
                  locationScope: {
                    locationScope: draft.locationScope,
                    namedLocationIds: draft.namedLocationIds,
                  },
                }
              : {}),
          })
          await reload()
          dialog = { kind: "none" }
        } finally {
          busy = false
          emit()
        }
        return
      }
      if (dialog.kind === "suspend") {
        const membershipId = dialog.membershipId
        await runWrite(async () => {
          await adapters.remove(membershipId)
        })
        return
      }
      if (dialog.kind === "revoke") {
        const invitationId = dialog.invitationId
        await runWrite(async () => {
          await adapters.revokeInvite(invitationId)
        })
      }
    },
    setAdminCell: (areaId, level) => {
      if (!canEditAdminColumn()) {
        return
      }
      if (!legalAdminLevels(areaId).includes(level as never)) {
        return
      }
      const persisted = data?.matrix.find((row) => row.id === areaId)
        ?.cells.Admin
      if (persisted === level) {
        const next = { ...adminDraft }
        delete next[areaId]
        adminDraft = next
      } else {
        adminDraft = { ...adminDraft, [areaId]: level }
      }
      emit()
    },
    requestSave: async () => {
      if (!canEditAdminColumn() || busy) {
        return
      }
      await persistMatrix()
    },
    requestNavigateAway: (href) => {
      if (!isDirty()) {
        return true
      }
      pendingLeave = { kind: "href", href }
      leaveDirtyOpen = true
      emit()
      return false
    },
    confirmLeaveDirtySave: async () => {
      if (!leaveDirtyOpen) {
        return
      }
      leaveDirtyOpen = false
      emit()
      const ok = await persistMatrix()
      if (ok) {
        continuePendingLeave()
      } else {
        pendingLeave = null
      }
      emit()
    },
    confirmLeaveDirtyCancel: async () => {
      if (!leaveDirtyOpen) {
        return
      }
      leaveDirtyOpen = false
      adminDraft = {}
      continuePendingLeave()
      emit()
    },
    closeLeaveDirty: () => {
      leaveDirtyOpen = false
      pendingLeave = null
      emit()
    },
    consumePendingNavigation: () => {
      const href = pendingNavigationHref
      pendingNavigationHref = null
      return href
    },
    openAuditLog: async () => {
      if (accessActivityEmpty || !privacyConsentHasAccess) {
        return
      }
      await loadAuditPage(1)
      auditLogOpen = true
      emit()
    },
    closeAuditLog: () => {
      auditLogOpen = false
      emit()
    },
    goToNextAuditPage: async () => {
      if (auditLogPage * auditLogPageSize >= auditLogTotalCount) {
        return
      }
      await loadAuditPage(auditLogPage + 1)
      emit()
    },
    goToPreviousAuditPage: async () => {
      if (auditLogPage <= 1) {
        return
      }
      await loadAuditPage(auditLogPage - 1)
      emit()
    },
  }
}

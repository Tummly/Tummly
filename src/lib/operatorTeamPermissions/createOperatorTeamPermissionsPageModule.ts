import {
  commitPending,
  emptySelection,
  getMultiSelectIds,
  openSession,
  projectChips,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import { teamPermissionsFilterSheetSchema } from "@/lib/operatorTeamPermissions/teamPermissionsFilterSheetSchema"
import {
  legalAdminLevels,
  resolveTeamPermissionsTabId,
  TEAM_PERMISSIONS_TAB_IDS,
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
  actions: string[]
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
}

export type TeamPermissionsPageAdapters = {
  getPage: () => Promise<TeamPermissionsPageData>
  updateRole: (membershipId: number, permissionRole: string) => Promise<void>
  updateLocationScope: (
    membershipId: number,
    payload: { locationScope: "all" | "named"; namedLocationIds: number[] }
  ) => Promise<void>
  deactivate: (membershipId: number) => Promise<void>
  reactivate: (membershipId: number) => Promise<void>
  remove: (membershipId: number) => Promise<void>
  saveMatrix: (cells: AdminMatrixCell[]) => Promise<void>
}

export type TeamPermissionsDialog =
  | { kind: "none" }
  | { kind: "notes" }
  | { kind: "invite" }
  | { kind: "change-role"; membershipId: number; draftRole: string }
  | {
      kind: "change-location"
      membershipId: number
      draftScope: "all" | "named"
      draftNamedIds: number[]
    }
  | { kind: "deactivate"; membershipId: number }
  | { kind: "remove"; membershipId: number }

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
  openNotes: () => void
  openInvite: () => void
  openChangeRole: (membershipId: number) => void
  openChangeLocation: (membershipId: number) => void
  openDeactivate: (membershipId: number) => void
  openRemove: (membershipId: number) => void
  closeDialog: () => void
  setChangeRoleDraft: (role: string) => void
  setChangeLocationDraft: (
    scope: "all" | "named",
    namedIds: number[]
  ) => void
  confirmReactivate: (membershipId: number) => Promise<void>
  confirmDialogPrimary: () => Promise<void>
  setAdminCell: (areaId: string, level: string) => void
  requestSave: () => Promise<void>
  requestNavigateAway: (href: string) => boolean
  confirmLeaveDirtySave: () => Promise<void>
  confirmLeaveDirtyCancel: () => Promise<void>
  closeLeaveDirty: () => void
  consumePendingNavigation: () => string | null
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
  options: { initialTabId?: string | null } = {}
): OperatorTeamPermissionsPageModule {
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
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const canEditAdminColumn = () => data?.actorPermissionRole === "Owner"

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
    if (pendingLeave?.kind === "tab") {
      activeTabId = resolveTeamPermissionsTabId(
        pendingLeave.tabId,
        privacyConsentHasAccess
      )
      pendingLeave = null
    }
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

  const getSnapshot = (): TeamPermissionsSnapshot => {
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
      pendingNavigationHref:
        pendingLeave?.kind === "href" ? pendingLeave.href : null,
    }
  }

  const reload = async () => {
    loadStatus = "loading"
    emit()
    try {
      data = await adapters.getPage()
      privacyConsentHasAccess = data.privacyConsentHasAccess
      activeTabId = resolveTeamPermissionsTabId(
        activeTabId,
        privacyConsentHasAccess
      )
      adminDraft = {}
      loadStatus = "loaded"
    } catch {
      loadStatus = "error"
    }
    emit()
  }

  const findMember = (membershipId: number) =>
    data?.members.find((row) => row.membershipId === membershipId) ?? null

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot,
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
    openNotes: () => {
      dialog = { kind: "notes" }
      emit()
    },
    openInvite: () => {
      if (!(data?.actorCanManage ?? false)) {
        return
      }
      dialog = { kind: "invite" }
      emit()
    },
    openChangeRole: (membershipId) => {
      const row = findMember(membershipId)
      if (row == null || !row.actions.includes("change-role")) {
        return
      }
      dialog = {
        kind: "change-role",
        membershipId,
        draftRole: row.permissionRole,
      }
      emit()
    },
    openChangeLocation: (membershipId) => {
      const row = findMember(membershipId)
      if (row == null || !row.actions.includes("change-location")) {
        return
      }
      dialog = {
        kind: "change-location",
        membershipId,
        draftScope: row.locationScope,
        draftNamedIds: [...row.namedLocationIds],
      }
      emit()
    },
    openDeactivate: (membershipId) => {
      const row = findMember(membershipId)
      if (row == null || !row.actions.includes("deactivate")) {
        return
      }
      dialog = { kind: "deactivate", membershipId }
      emit()
    },
    openRemove: (membershipId) => {
      const row = findMember(membershipId)
      if (row == null || !row.actions.includes("remove")) {
        return
      }
      dialog = { kind: "remove", membershipId }
      emit()
    },
    closeDialog: () => {
      if (busy) {
        return
      }
      dialog = { kind: "none" }
      emit()
    },
    setChangeRoleDraft: (role) => {
      if (dialog.kind !== "change-role") {
        return
      }
      dialog = { ...dialog, draftRole: role }
      emit()
    },
    setChangeLocationDraft: (scope, namedIds) => {
      if (dialog.kind !== "change-location") {
        return
      }
      dialog = { ...dialog, draftScope: scope, draftNamedIds: namedIds }
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
        return
      }
      if (dialog.kind === "notes") {
        dialog = { kind: "none" }
        emit()
        return
      }
      if (dialog.kind === "change-role") {
        const row = findMember(dialog.membershipId)
        const needsNamed =
          dialog.draftRole === "Area Manager"
          || dialog.draftRole === "Location Manager"
        if (
          needsNamed
          && (row == null || row.locationScope !== "named" || row.namedLocationIds.length === 0)
        ) {
          return
        }
        busy = true
        emit()
        try {
          await adapters.updateRole(dialog.membershipId, dialog.draftRole)
          await reload()
          dialog = { kind: "none" }
        } finally {
          busy = false
          emit()
        }
        return
      }
      if (dialog.kind === "change-location") {
        busy = true
        emit()
        try {
          await adapters.updateLocationScope(dialog.membershipId, {
            locationScope: dialog.draftScope,
            namedLocationIds: dialog.draftNamedIds,
          })
          await reload()
          dialog = { kind: "none" }
        } finally {
          busy = false
          emit()
        }
        return
      }
      if (dialog.kind === "deactivate") {
        busy = true
        emit()
        try {
          await adapters.deactivate(dialog.membershipId)
          await reload()
          dialog = { kind: "none" }
        } finally {
          busy = false
          emit()
        }
        return
      }
      if (dialog.kind === "remove") {
        busy = true
        emit()
        try {
          await adapters.remove(dialog.membershipId)
          await reload()
          dialog = { kind: "none" }
        } finally {
          busy = false
          emit()
        }
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
      if (pendingLeave?.kind !== "href") {
        return null
      }
      const href = pendingLeave.href
      pendingLeave = null
      return href
    },
  }
}

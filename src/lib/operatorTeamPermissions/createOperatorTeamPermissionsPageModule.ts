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
import { assignableRolesForActor } from "@/lib/operatorTeamPermissions/permissionRoles"
import {
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
  invitations: TeamInvitationRow[]
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
  sendInvite: (payload: TeamInviteDraft) => Promise<void>
  resendInvite: (invitationId: number) => Promise<void>
  revokeInvite: (invitationId: number) => Promise<void>
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
  openRevoke: (invitationId: number) => void
  resendInvite: (invitationId: number) => Promise<void>
  closeDialog: () => void
  setChangeRoleDraft: (role: string) => void
  setInviteDraft: (draft: TeamInviteDraft) => void
  setChangeLocationDraft: (
    scope: "all" | "named",
    namedIds: number[]
  ) => void
  confirmReactivate: (membershipId: number) => Promise<void>
  confirmDialogPrimary: () => Promise<void>
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
  let inviteDraft = emptyInviteDraft("")
  let inviteEmailError: string | null = null
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
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
    }
  }

  const reload = async () => {
    loadStatus = "loading"
    emit()
    try {
      data = await adapters.getPage()
      data = {
        ...data,
        invitations: data.invitations ?? [],
      }
      privacyConsentHasAccess = data.privacyConsentHasAccess
      activeTabId = resolveTeamPermissionsTabId(
        activeTabId,
        privacyConsentHasAccess
      )
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
    getSnapshot,
    load: reload,
    setActiveTabFromUrl: (raw) => {
      activeTabId = resolveTeamPermissionsTabId(raw, privacyConsentHasAccess)
      emit()
    },
    requestTabChange: (tabId) => {
      activeTabId = resolveTeamPermissionsTabId(
        tabId,
        privacyConsentHasAccess
      )
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
      inviteDraft = emptyInviteDraft(data?.actorPermissionRole ?? "")
      inviteEmailError = null
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
    setChangeRoleDraft: (role) => {
      if (dialog.kind !== "change-role") {
        return
      }
      dialog = { ...dialog, draftRole: role }
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
  }
}

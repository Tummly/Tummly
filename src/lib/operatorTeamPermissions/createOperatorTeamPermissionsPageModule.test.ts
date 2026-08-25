import { describe, expect, it, vi } from "vitest"

import {
  createOperatorTeamPermissionsPageModule,
  resolveTeamPermissionsTabId,
  TEAM_PERMISSIONS_TAB_IDS,
  type TeamMemberRow,
  type TeamPermissionsPageAdapters,
  type TeamPermissionsPageData,
} from "@/lib/operatorTeamPermissions/createOperatorTeamPermissionsPageModule"
import { emptySelection, openSession } from "@/lib/operatorFilterSheet"
import { teamPermissionsFilterSheetSchema } from "@/lib/operatorTeamPermissions/teamPermissionsFilterSheetSchema"

function member(
  overrides: Partial<TeamMemberRow> = {}
): TeamMemberRow {
  return {
    membershipId: 1,
    userId: 1,
    fullName: "Alex Owner",
    email: "alex@example.com",
    permissionRole: "Owner",
    locationScope: "all",
    namedLocationIds: [],
    locationAccessLabel: "All locations",
    status: "active",
    isAccountOwner: true,
    actions: [],
    ...overrides,
  }
}

function page(
  overrides: Partial<TeamPermissionsPageData> = {}
): TeamPermissionsPageData {
  return {
    actorCanManage: true,
    actorPermissionRole: "Owner",
    privacyConsentHasAccess: true,
    isSingleLocation: false,
    stats: {
      activeMembers: 1,
      pendingInvites: 0,
      locationManagers: 0,
      limitedAccessUsers: 0,
    },
    locations: [{ id: 10, name: "Camden" }],
    members: [member()],
    matrix: [
      {
        id: "locations",
        label: "Locations",
        cells: {
          Owner: "Manage",
          Admin: "Manage",
        },
      },
      {
        id: "billing-credits",
        label: "Billing & credits",
        cells: {
          Owner: "Manage",
          Admin: "View",
        },
      },
    ],
    invitations: [],
    ...overrides,
  }
}

function adapters(
  overrides: Partial<TeamPermissionsPageAdapters> = {}
): TeamPermissionsPageAdapters {
  return {
    getPage: vi.fn(async () => page()),
    updateRole: vi.fn(async () => undefined),
    updateLocationScope: vi.fn(async () => undefined),
    deactivate: vi.fn(async () => undefined),
    reactivate: vi.fn(async () => undefined),
    remove: vi.fn(async () => undefined),
    saveMatrix: vi.fn(async () => undefined),
    sendInvite: vi.fn(async () => undefined),
    resendInvite: vi.fn(async () => undefined),
    revokeInvite: vi.fn(async () => undefined),
    ...overrides,
  }
}

describe("resolveTeamPermissionsTabId", () => {
  it("defaults unknown values to members", () => {
    expect(resolveTeamPermissionsTabId(null, true)).toBe("members")
    expect(resolveTeamPermissionsTabId("nope", true)).toBe("members")
  })

  it("round-trips known tab ids", () => {
    for (const id of TEAM_PERMISSIONS_TAB_IDS) {
      expect(resolveTeamPermissionsTabId(id, true)).toBe(id)
    }
  })

  it("rewrites access-activity when privacy-consent is No access", () => {
    expect(resolveTeamPermissionsTabId("access-activity", false)).toBe(
      "members"
    )
  })
})

describe("createOperatorTeamPermissionsPageModule", () => {
  it("hides the Invite action when the actor cannot Manage", async () => {
    const api = adapters({
      getPage: vi.fn(async () => page({ actorCanManage: false })),
    })
    const module = createOperatorTeamPermissionsPageModule(api)
    await module.load()
    module.openInvite()
    expect(module.getSnapshot().dialog.kind).toBe("none")
  })

  it("hides access-activity tab when privacy consent has no access", async () => {
    const api = adapters({
      getPage: vi.fn(async () =>
        page({ privacyConsentHasAccess: false })
      ),
    })
    const module = createOperatorTeamPermissionsPageModule(api, {
      initialTabId: "access-activity",
    })
    await module.load()
    expect(
      module.getSnapshot().tabs.map((tab) => tab.id)
    ).not.toContain("access-activity")
    expect(module.getSnapshot().activeTabId).toBe("members")
  })

  it("hides unauthorized row actions", async () => {
    const api = adapters({
      getPage: vi.fn(async () =>
        page({
          members: [member({ actions: [] })],
        })
      ),
    })
    const module = createOperatorTeamPermissionsPageModule(api)
    await module.load()
    module.openDeactivate(1)
    expect(module.getSnapshot().dialog.kind).toBe("none")
  })

  it("shows filter-search empty when no members match", async () => {
    const api = adapters()
    const module = createOperatorTeamPermissionsPageModule(api)
    await module.load()
    module.setSearchQuery("zzz")
    expect(module.getSnapshot().visibleMembers).toEqual([])
  })

  it("ignores filters for stats", async () => {
    const staff = member({
      membershipId: 2,
      userId: 2,
      fullName: "Sam Staff",
      email: "sam@example.com",
      permissionRole: "Staff",
      isAccountOwner: false,
      actions: ["deactivate"],
    })
    const api = adapters({
      getPage: vi.fn(async () =>
        page({
          stats: {
            activeMembers: 2,
            pendingInvites: 0,
            locationManagers: 0,
            limitedAccessUsers: 0,
          },
          members: [member(), staff],
        })
      ),
    })
    const module = createOperatorTeamPermissionsPageModule(api)
    await module.load()
    const schema = teamPermissionsFilterSheetSchema({
      isSingleLocation: false,
      locations: [{ id: 10, name: "Camden" }],
    })
    const applied = emptySelection(schema)
    applied.role = { kind: "multi-select", ids: ["Staff"] }
    module.setFiltersSession(openSession(applied))
    expect(module.getSnapshot().visibleMembers).toHaveLength(1)
    expect(module.getSnapshot().stats.activeMembers).toBe(2)
  })

  it("Owner dirty matrix opens leave-dirty Save then continues", async () => {
    const api = adapters()
    const module = createOperatorTeamPermissionsPageModule(api, {
      initialTabId: "roles-permissions",
    })
    await module.load()
    module.setAdminCell("billing-credits", "Manage")
    expect(module.getSnapshot().isDirty).toBe(true)
    expect(module.getSnapshot().canEditAdminColumn).toBe(true)

    module.requestTabChange("members")
    expect(module.getSnapshot().leaveDirtyOpen).toBe(true)
    expect(module.getSnapshot().activeTabId).toBe("roles-permissions")

    await module.confirmLeaveDirtySave()
    expect(api.saveMatrix).toHaveBeenCalledWith([
      { areaId: "billing-credits", level: "Manage" },
    ])
    expect(module.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(module.getSnapshot().activeTabId).toBe("members")
    expect(module.getSnapshot().isDirty).toBe(false)
  })

  it("leave-dirty Cancel discards matrix edits then continues", async () => {
    const api = adapters()
    const module = createOperatorTeamPermissionsPageModule(api, {
      initialTabId: "roles-permissions",
    })
    await module.load()
    module.setAdminCell("locations", "View")
    module.requestTabChange("invitations")
    await module.confirmLeaveDirtyCancel()

    expect(api.saveMatrix).not.toHaveBeenCalled()
    expect(module.getSnapshot().activeTabId).toBe("invitations")
    expect(module.getSnapshot().isDirty).toBe(false)
    expect(
      module.getSnapshot().matrix.find((row) => row.id === "locations")
        ?.cells.Admin
    ).toBe("Manage")
  })

  it("leave-dirty Close stays on the matrix with the draft", async () => {
    const api = adapters()
    const module = createOperatorTeamPermissionsPageModule(api, {
      initialTabId: "roles-permissions",
    })
    await module.load()
    module.setAdminCell("locations", "View")
    module.requestTabChange("members")
    module.closeLeaveDirty()

    expect(module.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(module.getSnapshot().activeTabId).toBe("roles-permissions")
    expect(module.getSnapshot().isDirty).toBe(true)
    expect(
      module.getSnapshot().matrix.find((row) => row.id === "locations")
        ?.cells.Admin
    ).toBe("View")
  })

  it("Admin cannot edit the Admin column", async () => {
    const api = adapters({
      getPage: vi.fn(async () =>
        page({ actorPermissionRole: "Admin", actorCanManage: true })
      ),
    })
    const module = createOperatorTeamPermissionsPageModule(api, {
      initialTabId: "roles-permissions",
    })
    await module.load()
    module.setAdminCell("billing-credits", "Manage")
    expect(module.getSnapshot().canEditAdminColumn).toBe(false)
    expect(module.getSnapshot().isDirty).toBe(false)
    expect(module.getSnapshot().saveEnabled).toBe(false)
  })

  it("SideNav leave-dirty waits for Save before publishing the href", async () => {
    const api = adapters()
    const module = createOperatorTeamPermissionsPageModule(api, {
      initialTabId: "roles-permissions",
    })
    await module.load()
    module.setAdminCell("billing-credits", "Manage")

    expect(
      module.requestNavigateAway("/single-dashboard/guests?location=1")
    ).toBe(false)
    expect(module.getSnapshot().leaveDirtyOpen).toBe(true)
    expect(module.getSnapshot().pendingNavigationHref).toBeNull()

    await module.confirmLeaveDirtySave()
    expect(api.saveMatrix).toHaveBeenCalledWith([
      { areaId: "billing-credits", level: "Manage" },
    ])
    expect(module.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(module.getSnapshot().pendingNavigationHref).toBe(
      "/single-dashboard/guests?location=1"
    )
    expect(module.consumePendingNavigation()).toBe(
      "/single-dashboard/guests?location=1"
    )
  })

  it("SideNav leave-dirty Cancel discards then publishes the href", async () => {
    const api = adapters()
    const module = createOperatorTeamPermissionsPageModule(api, {
      initialTabId: "roles-permissions",
    })
    await module.load()
    module.setAdminCell("locations", "View")
    module.requestNavigateAway("/multi-dashboard/settings/account?location=1")
    await module.confirmLeaveDirtyCancel()

    expect(api.saveMatrix).not.toHaveBeenCalled()
    expect(module.getSnapshot().isDirty).toBe(false)
    expect(module.getSnapshot().pendingNavigationHref).toBe(
      "/multi-dashboard/settings/account?location=1"
    )
  })

  it("leave-dirty Close keeps the href unpublished", async () => {
    const api = adapters()
    const module = createOperatorTeamPermissionsPageModule(api, {
      initialTabId: "roles-permissions",
    })
    await module.load()
    module.setAdminCell("locations", "View")
    module.requestNavigateAway("/single-dashboard/home?location=1")
    module.closeLeaveDirty()

    expect(module.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(module.getSnapshot().pendingNavigationHref).toBeNull()
    expect(module.consumePendingNavigation()).toBeNull()
    expect(module.getSnapshot().isDirty).toBe(true)
  })

  it("shows inline email error and keeps the invite dialog open", async () => {
    const sendInvite = vi.fn(async () => {
      throw new Error("An invitation is already pending for this email.")
    })
    const api = adapters({ sendInvite })
    const module = createOperatorTeamPermissionsPageModule(api)
    await module.load()
    module.openInvite()
    module.setInviteDraft({
      email: "mark@example.com",
      fullName: "Mark Invitee",
      permissionRole: "Reporting Only",
      locationScope: "all",
      namedLocationIds: [],
      message: "",
    })
    await module.confirmDialogPrimary()
    const snap = module.getSnapshot()
    expect(sendInvite).toHaveBeenCalledTimes(1)
    expect(snap.dialog.kind).toBe("invite")
    expect(snap.inviteEmailError).toBe(
      "An invitation is already pending for this email."
    )
    expect(snap.busy).toBe(false)
  })

  it("ignores a second send while the first send is busy", async () => {
    let release!: () => void
    const sendInvite = vi.fn(
      () =>
        new Promise<void>((resolve) => {
          release = resolve
        })
    )
    const api = adapters({ sendInvite })
    const module = createOperatorTeamPermissionsPageModule(api)
    await module.load()
    module.openInvite()
    module.setInviteDraft({
      email: "mark@example.com",
      fullName: "Mark Invitee",
      permissionRole: "Reporting Only",
      locationScope: "all",
      namedLocationIds: [],
      message: "",
    })
    const first = module.confirmDialogPrimary()
    expect(module.getSnapshot().busy).toBe(true)
    await module.confirmDialogPrimary()
    expect(sendInvite).toHaveBeenCalledTimes(1)
    release()
    await first
    expect(module.getSnapshot().busy).toBe(false)
    expect(module.getSnapshot().dialog.kind).toBe("none")
  })

  it("resends and revokes through adapters with a busy lock", async () => {
    const resendInvite = vi.fn(async () => undefined)
    const revokeInvite = vi.fn(async () => undefined)
    const api = adapters({
      getPage: vi.fn(async () =>
        page({
          invitations: [
            {
              invitationId: 9,
              email: "mark@example.com",
              permissionRole: "Reporting Only",
              locationAccessLabel: "All locations",
              invitedBy: "Owner Seventeen",
              sentLabel: "18 Aug 2026",
              expiresLabel: "25 Aug 2026",
              expired: false,
              actions: ["resend", "revoke"],
            },
          ],
        })
      ),
      resendInvite,
      revokeInvite,
    })
    const module = createOperatorTeamPermissionsPageModule(api)
    await module.load()
    await module.resendInvite(9)
    expect(resendInvite).toHaveBeenCalledWith(9)
    module.openRevoke(9)
    expect(module.getSnapshot().dialog).toEqual({
      kind: "revoke",
      invitationId: 9,
    })
    await module.confirmDialogPrimary()
    expect(revokeInvite).toHaveBeenCalledWith(9)
    expect(module.getSnapshot().dialog.kind).toBe("none")
  })
})

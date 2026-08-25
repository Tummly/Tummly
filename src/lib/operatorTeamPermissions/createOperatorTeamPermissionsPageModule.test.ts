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
})

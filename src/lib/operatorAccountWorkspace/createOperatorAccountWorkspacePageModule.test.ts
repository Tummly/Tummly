import { afterEach, describe, expect, it, vi, type Mock } from "vitest"

import {
  ACCOUNT_WORKSPACE_TAB_IDS,
  createOperatorAccountWorkspacePageModule,
  resolveAccountWorkspaceTabId,
  type AccountWorkspaceDetails,
  type OperatorAccountWorkspacePageAdapters,
} from "@/lib/operatorAccountWorkspace/createOperatorAccountWorkspacePageModule"

function createDetails(
  overrides: Partial<AccountWorkspaceDetails> = {}
): AccountWorkspaceDetails {
  return {
    workspaceName: "Camden Group",
    accountStructure: "Single location",
    businessCategory: "cafe",
    businessCategoryLabel: "Café / coffee shop",
    mainOperatingCountry: "United Kingdom",
    brandLogoOperatorUrl: null,
    brandLogoPublicUrl: null,
    lastSavedAt: "2026-07-22T13:41:00.000Z",
    status: {
      workspaceStatus: "Active",
      planStatus: "Pilot",
      billingStatus: "Active",
      accountCreatedAt: "2026-01-10T10:00:00.000Z",
      activeLocations: 1,
      teamMembers: 1,
      guestProfiles: 4,
      guestFormStatus: "Live",
      lastAccountUpdateAt: "2026-07-22T13:41:00.000Z",
    },
    ...overrides,
  }
}

function createAdapters(
  overrides: Partial<OperatorAccountWorkspacePageAdapters> = {}
): OperatorAccountWorkspacePageAdapters & {
  getDetails: Mock
  updateAccountDetails: Mock
} {
  return {
    getDetails: vi.fn(async () => createDetails()),
    updateAccountDetails: vi.fn(async ({ name }) =>
      createDetails({
        workspaceName: name,
        lastSavedAt: "2026-08-24T12:00:00.000Z",
        status: {
          ...createDetails().status,
          lastAccountUpdateAt: "2026-08-24T12:00:00.000Z",
        },
      })
    ),
    ...overrides,
  } as OperatorAccountWorkspacePageAdapters & {
    getDetails: Mock
    updateAccountDetails: Mock
  }
}

describe("resolveAccountWorkspaceTabId", () => {
  it("defaults unknown and missing values to account-details", () => {
    expect(resolveAccountWorkspaceTabId(null)).toBe("account-details")
    expect(resolveAccountWorkspaceTabId(undefined)).toBe("account-details")
    expect(resolveAccountWorkspaceTabId("")).toBe("account-details")
    expect(resolveAccountWorkspaceTabId("nope")).toBe("account-details")
  })

  it("round-trips known tab ids", () => {
    for (const id of ACCOUNT_WORKSPACE_TAB_IDS) {
      expect(resolveAccountWorkspaceTabId(id)).toBe(id)
    }
  })
})

describe("createOperatorAccountWorkspacePageModule", () => {
  afterEach(() => {
    vi.restoreAllMocks()
  })

  it("loads details and keeps Save disabled when clean", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)

    await page.load()

    const snap = page.getSnapshot()
    expect(snap.loadStatus).toBe("loaded")
    expect(snap.activeTabId).toBe("account-details")
    expect(snap.accountDetails.workspaceName).toBe("Camden Group")
    expect(snap.isDirty).toBe(false)
    expect(snap.saveEnabled).toBe(false)
    expect(snap.lastSavedAt).toBe("2026-07-22T13:41:00.000Z")
  })

  it("dirties Account details on name edit and enables Save", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("New Name")

    expect(page.getSnapshot().isDirty).toBe(true)
    expect(page.getSnapshot().saveEnabled).toBe(true)
    expect(page.getSnapshot().accountDetails.workspaceName).toBe("New Name")
  })

  it("dirties Account details on logo stage without writing", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    const file = new File(["x"], "logo.png", { type: "image/png" })
    page.stageBrandLogo(file)

    expect(page.getSnapshot().isDirty).toBe(true)
    expect(page.getSnapshot().saveEnabled).toBe(true)
    expect(adapters.updateAccountDetails).not.toHaveBeenCalled()
  })

  it("keeps Save disabled on Account controls even when another tab was dirty", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("Draft")
    expect(page.getSnapshot().saveEnabled).toBe(true)

    page.requestTabChange("account-controls")
    // leave-dirty opens; cancel discard then switch
    await page.confirmLeaveDirtyCancel()

    expect(page.getSnapshot().activeTabId).toBe("account-controls")
    expect(page.getSnapshot().saveEnabled).toBe(false)
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("opens rename confirm when saving with a changed workspace name", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("Renamed")
    await page.requestSave()

    expect(page.getSnapshot().renameConfirmOpen).toBe(true)
    expect(adapters.updateAccountDetails).not.toHaveBeenCalled()
  })

  it("aborts whole save including staged logo when rename confirm cancels", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("Renamed")
    page.stageBrandLogo(new File(["x"], "logo.png", { type: "image/png" }))
    await page.requestSave()
    page.cancelRenameConfirm()

    expect(page.getSnapshot().renameConfirmOpen).toBe(false)
    expect(page.getSnapshot().isDirty).toBe(true)
    expect(page.getSnapshot().accountDetails.workspaceName).toBe("Renamed")
    expect(adapters.updateAccountDetails).not.toHaveBeenCalled()
  })

  it("persists name and logo after rename confirm", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    const file = new File(["x"], "logo.png", { type: "image/png" })
    page.setWorkspaceName("Renamed")
    page.stageBrandLogo(file)
    await page.requestSave()
    await page.confirmRename()

    expect(adapters.updateAccountDetails).toHaveBeenCalledWith({
      name: "Renamed",
      logo: file,
    })
    expect(page.getSnapshot().isDirty).toBe(false)
    expect(page.getSnapshot().lastSavedAt).toBe("2026-08-24T12:00:00.000Z")
    expect(page.getSnapshot().toast).toEqual({
      kind: "success",
      message: "Account details saved.",
    })
  })

  it("keeps draft and lastSavedAt when persist fails", async () => {
    const adapters = createAdapters({
      updateAccountDetails: vi.fn(async () => {
        throw new Error("save failed")
      }),
    })
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("Renamed")
    await page.requestSave()
    await page.confirmRename()

    expect(page.getSnapshot().isDirty).toBe(true)
    expect(page.getSnapshot().accountDetails.workspaceName).toBe("Renamed")
    expect(page.getSnapshot().lastSavedAt).toBe("2026-07-22T13:41:00.000Z")
    expect(page.getSnapshot().toast).toEqual({
      kind: "error",
      message: "Could not save account details. Please try again.",
    })
  })

  it("leave-dirty Save persists then continues to the target tab", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("Renamed")
    page.requestTabChange("business-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(true)
    expect(page.getSnapshot().activeTabId).toBe("account-details")

    await page.confirmLeaveDirtySave()
    expect(page.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(page.getSnapshot().renameConfirmOpen).toBe(true)

    await page.confirmRename()

    expect(adapters.updateAccountDetails).toHaveBeenCalled()
    expect(page.getSnapshot().activeTabId).toBe("business-details")
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("leave-dirty Save without rename persists then continues", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.stageBrandLogo(new File(["x"], "logo.png", { type: "image/png" }))
    page.requestTabChange("business-details")
    await page.confirmLeaveDirtySave()

    expect(adapters.updateAccountDetails).toHaveBeenCalled()
    expect(page.getSnapshot().activeTabId).toBe("business-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("failed leave-dirty Save stays on the tab with the draft", async () => {
    const adapters = createAdapters({
      updateAccountDetails: vi.fn(async () => {
        throw new Error("save failed")
      }),
    })
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.stageBrandLogo(new File(["x"], "logo.png", { type: "image/png" }))
    page.requestTabChange("business-details")
    await page.confirmLeaveDirtySave()

    expect(page.getSnapshot().activeTabId).toBe("account-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(page.getSnapshot().isDirty).toBe(true)
  })

  it("leave-dirty Cancel discards then continues", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("Renamed")
    page.requestTabChange("key-contacts")
    await page.confirmLeaveDirtyCancel()

    expect(adapters.updateAccountDetails).not.toHaveBeenCalled()
    expect(page.getSnapshot().activeTabId).toBe("key-contacts")
    expect(page.getSnapshot().accountDetails.workspaceName).toBe("Camden Group")
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("leave-dirty Close stays with the draft", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("Renamed")
    page.requestTabChange("workspace-defaults")
    page.closeLeaveDirty()

    expect(page.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(page.getSnapshot().activeTabId).toBe("account-details")
    expect(page.getSnapshot().accountDetails.workspaceName).toBe("Renamed")
    expect(page.getSnapshot().isDirty).toBe(true)
  })

  it("does not open rename confirm when workspace name is empty", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("   ")
    await page.requestSave()

    expect(page.getSnapshot().renameConfirmOpen).toBe(false)
    expect(page.getSnapshot().accountDetails.workspaceNameError).toBe(
      "Workspace name is required."
    )
    expect(adapters.updateAccountDetails).not.toHaveBeenCalled()
  })

  it("uses status lastAccountUpdateAt for Last saved before first form save", async () => {
    const adapters = createAdapters({
      getDetails: vi.fn(async () =>
        createDetails({
          lastSavedAt: null,
          status: {
            ...createDetails().status,
            lastAccountUpdateAt: "2026-01-10T10:00:00.000Z",
          },
        })
      ),
    })
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    expect(page.getSnapshot().lastSavedAt).toBe("2026-01-10T10:00:00.000Z")
  })

  it("requestNavigateAway opens leave-dirty when dirty", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.setWorkspaceName("Renamed")
    expect(page.requestNavigateAway("/single-dashboard/guests?location=1")).toBe(
      false
    )
    expect(page.getSnapshot().leaveDirtyOpen).toBe(true)

    await page.confirmLeaveDirtyCancel()
    expect(page.getSnapshot().pendingNavigationHref).toBe(
      "/single-dashboard/guests?location=1"
    )
    expect(page.consumePendingNavigation()).toBe(
      "/single-dashboard/guests?location=1"
    )
  })

  it("calls onIdentityPersisted after a successful save", async () => {
    const onIdentityPersisted = vi.fn()
    const adapters = createAdapters({ onIdentityPersisted })
    const page = createOperatorAccountWorkspacePageModule(adapters)
    await page.load()

    page.stageBrandLogo(new File(["x"], "logo.png", { type: "image/png" }))
    await page.requestSave()

    expect(onIdentityPersisted).toHaveBeenCalledWith(
      expect.objectContaining({
        workspaceName: "Camden Group",
      })
    )
  })
})

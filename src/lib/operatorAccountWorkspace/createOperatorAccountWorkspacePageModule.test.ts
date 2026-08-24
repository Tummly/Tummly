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
    isAccountOwner: true,
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
    businessDetails: {
      legalStructure: "",
      legalBusinessName: "",
      tradingName: "",
      companyNumber: "",
      vatNumber: "",
      countryOfRegistration: "United Kingdom",
      addressLine1: "",
      addressLine2: "",
      townCity: "",
      county: "",
      postcode: "",
      country: "United Kingdom",
    },
    keyContacts: {
      accountOwner: {
        userId: 42,
        fullName: "Alex Owner",
        email: "alex@example.com",
      },
      billingContactUserId: 42,
      privacyContactUserId: 42,
      supportContactUserId: 42,
      eligibleMembers: [
        {
          userId: 42,
          fullName: "Alex Owner",
          email: "alex@example.com",
        },
      ],
    },
    workspaceDefaults: {
      weekStartsOn: "monday",
      defaultReportingPeriod: "7days",
      defaultCampaignSenderName: "",
      defaultTimezone: "Europe/London",
      defaultCurrency: "GBP",
      defaultLanguage: "English",
      dateFormat: "DD/MM/YYYY",
    },
    ...overrides,
  }
}

function createAdapters(
  overrides: Partial<OperatorAccountWorkspacePageAdapters> = {}
): OperatorAccountWorkspacePageAdapters & {
  getDetails: Mock
  updateAccountDetails: Mock
  updateBusinessDetails: Mock
  updateKeyContacts: Mock
  updateWorkspaceDefaults: Mock
  pauseWorkspace: Mock
  resumeWorkspace: Mock
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
    updateBusinessDetails: vi.fn(async (payload) =>
      createDetails({
        lastSavedAt: "2026-08-24T13:00:00.000Z",
        status: {
          ...createDetails().status,
          lastAccountUpdateAt: "2026-08-24T13:00:00.000Z",
        },
        businessDetails: {
          legalStructure: payload.legalStructure,
          legalBusinessName: payload.legalBusinessName,
          tradingName: payload.sameAsLegalBusinessName
            ? payload.legalBusinessName
            : payload.tradingName,
          companyNumber: payload.companyNumber,
          vatNumber: payload.vatNumber,
          countryOfRegistration: payload.countryOfRegistration,
          addressLine1: payload.addressLine1,
          addressLine2: payload.addressLine2,
          townCity: payload.townCity,
          county: payload.county,
          postcode: payload.postcode,
          country: payload.country,
        },
      })
    ),
    updateKeyContacts: vi.fn(async (payload) =>
      createDetails({
        lastSavedAt: "2026-08-24T14:00:00.000Z",
        status: {
          ...createDetails().status,
          lastAccountUpdateAt: "2026-08-24T14:00:00.000Z",
        },
        keyContacts: {
          ...createDetails().keyContacts,
          billingContactUserId: payload.billingContactUserId,
          privacyContactUserId: payload.privacyContactUserId,
          supportContactUserId: payload.supportContactUserId,
        },
      })
    ),
    updateWorkspaceDefaults: vi.fn(async (payload) =>
      createDetails({
        lastSavedAt: "2026-08-24T15:00:00.000Z",
        status: {
          ...createDetails().status,
          lastAccountUpdateAt: "2026-08-24T15:00:00.000Z",
        },
        workspaceDefaults: {
          ...createDetails().workspaceDefaults,
          weekStartsOn: payload.weekStartsOn,
          defaultReportingPeriod: payload.defaultReportingPeriod,
          defaultCampaignSenderName: payload.defaultCampaignSenderName.trim(),
        },
      })
    ),
    pauseWorkspace: vi.fn(async () =>
      createDetails({
        status: {
          ...createDetails().status,
          workspaceStatus: "Paused",
          guestFormStatus: "Paused",
        },
      })
    ),
    resumeWorkspace: vi.fn(async () =>
      createDetails({
        status: {
          ...createDetails().status,
          workspaceStatus: "Active",
          guestFormStatus: "Live",
        },
      })
    ),
    ...overrides,
  } as OperatorAccountWorkspacePageAdapters & {
    getDetails: Mock
    updateAccountDetails: Mock
    updateBusinessDetails: Mock
    updateKeyContacts: Mock
    updateWorkspaceDefaults: Mock
    pauseWorkspace: Mock
    resumeWorkspace: Mock
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

  it("dirties Business details on edit and enables Save", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "business-details",
    })
    await page.load()

    expect(page.getSnapshot().saveEnabled).toBe(false)
    page.setLegalBusinessName("Mehmet's Grill Ltd")

    expect(page.getSnapshot().isDirty).toBe(true)
    expect(page.getSnapshot().saveEnabled).toBe(true)
    expect(page.getSnapshot().businessDetails.legalBusinessName).toBe(
      "Mehmet's Grill Ltd"
    )
  })

  it("empty Business details save calls updateBusinessDetails", async () => {
    const adapters = createAdapters({
      getDetails: vi.fn(async () =>
        createDetails({
          businessDetails: {
            ...createDetails().businessDetails,
            legalBusinessName: "Prior name",
            tradingName: "Prior trading",
          },
        })
      ),
    })
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "business-details",
    })
    await page.load()

    page.setLegalBusinessName("")
    page.setTradingName("")
    await page.requestSave()

    expect(adapters.updateBusinessDetails).toHaveBeenCalledWith(
      expect.objectContaining({
        legalBusinessName: "",
        tradingName: "",
        sameAsLegalBusinessName: false,
        country: "United Kingdom",
        countryOfRegistration: "United Kingdom",
      })
    )
    expect(page.getSnapshot().toast).toEqual({
      kind: "success",
      message: "Business details saved.",
    })
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("sameAsLegal copies trading name on persist payload", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "business-details",
    })
    await page.load()

    page.setLegalBusinessName("Mehmet's Grill Ltd")
    page.setTradingName("Should Be Overwritten")
    page.setSameAsLegalBusinessName(true)
    // Checkbox is UI-only until persist; snapshot may preview legal name.
    expect(page.getSnapshot().businessDetails.tradingName).toBe(
      "Mehmet's Grill Ltd"
    )

    await page.requestSave()

    expect(adapters.updateBusinessDetails).toHaveBeenCalledWith(
      expect.objectContaining({
        legalBusinessName: "Mehmet's Grill Ltd",
        tradingName: "Mehmet's Grill Ltd",
        sameAsLegalBusinessName: true,
      })
    )
  })

  it("sameAsLegal does not mutate draft trading before persist", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "business-details",
    })
    await page.load()

    page.setLegalBusinessName("Legal Co")
    page.setTradingName("Trading Co")
    page.setSameAsLegalBusinessName(true)
    page.setSameAsLegalBusinessName(false)

    expect(page.getSnapshot().businessDetails.tradingName).toBe("Trading Co")
  })

  it("invalid UK postcode keeps draft and does not call adapter", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "business-details",
    })
    await page.load()

    page.setLegalBusinessName("Draft Co")
    page.setCountry("United Kingdom")
    page.setPostcode("NOT A POSTCODE")
    await page.requestSave()

    expect(adapters.updateBusinessDetails).not.toHaveBeenCalled()
    expect(page.getSnapshot().businessDetails.postcodeError).toBe(
      "Enter a valid UK postcode."
    )
    expect(page.getSnapshot().businessDetails.legalBusinessName).toBe(
      "Draft Co"
    )
    expect(page.getSnapshot().isDirty).toBe(true)
  })

  it("leave-dirty on Business details Save then continues", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "business-details",
    })
    await page.load()

    page.setLegalBusinessName("Draft Co")
    page.requestTabChange("account-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(true)
    expect(page.getSnapshot().activeTabId).toBe("business-details")

    await page.confirmLeaveDirtySave()

    expect(adapters.updateBusinessDetails).toHaveBeenCalled()
    expect(page.getSnapshot().activeTabId).toBe("account-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(false)
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("dirties Key contacts on edit and enables Save", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "key-contacts",
    })
    await page.load()

    expect(page.getSnapshot().saveEnabled).toBe(false)
    expect(page.getSnapshot().keyContacts.billingContactUserId).toBe(42)

    // Same id is not dirty; force a no-op then re-set after load keeps clean.
    page.setBillingContactUserId(42)
    expect(page.getSnapshot().isDirty).toBe(false)

    // Until Team ships there is one member; dirty still works if values change
    // then restore — use a temporary different id then back.
    page.setBillingContactUserId(99)
    expect(page.getSnapshot().isDirty).toBe(true)
    expect(page.getSnapshot().saveEnabled).toBe(true)
    page.setBillingContactUserId(42)
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("Key contacts Save calls updateKeyContacts", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "key-contacts",
    })
    await page.load()

    page.setPrivacyContactUserId(42)
    page.setBillingContactUserId(99)
    await page.requestSave()

    expect(adapters.updateKeyContacts).toHaveBeenCalledWith({
      billingContactUserId: 99,
      privacyContactUserId: 42,
      supportContactUserId: 42,
    })
    expect(page.getSnapshot().toast).toEqual({
      kind: "success",
      message: "Key contacts saved.",
    })
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("leave-dirty on Key contacts Save then continues", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "key-contacts",
    })
    await page.load()

    page.setSupportContactUserId(99)
    page.requestTabChange("account-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(true)

    await page.confirmLeaveDirtySave()

    expect(adapters.updateKeyContacts).toHaveBeenCalled()
    expect(page.getSnapshot().activeTabId).toBe("account-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(false)
  })

  it("Workspace defaults dirty and Save calls updateWorkspaceDefaults", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "workspace-defaults",
    })
    await page.load()

    expect(page.getSnapshot().saveEnabled).toBe(false)
    page.setWeekStartsOn("friday")
    page.setDefaultReportingPeriod("30days")
    page.setDefaultCampaignSenderName(" Harbour Kitchen ")
    expect(page.getSnapshot().isDirty).toBe(true)
    expect(page.getSnapshot().saveEnabled).toBe(true)

    await page.requestSave()

    expect(adapters.updateWorkspaceDefaults).toHaveBeenCalledWith({
      weekStartsOn: "friday",
      defaultReportingPeriod: "30days",
      defaultCampaignSenderName: " Harbour Kitchen ",
    })
    expect(page.getSnapshot().toast).toEqual({
      kind: "success",
      message: "Workspace defaults saved.",
    })
    expect(page.getSnapshot().isDirty).toBe(false)
  })

  it("leave-dirty on Workspace defaults Save then continues", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "workspace-defaults",
    })
    await page.load()

    page.setDefaultReportingPeriod("thisMonth")
    page.requestTabChange("account-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(true)

    await page.confirmLeaveDirtySave()

    expect(adapters.updateWorkspaceDefaults).toHaveBeenCalled()
    expect(page.getSnapshot().activeTabId).toBe("account-details")
    expect(page.getSnapshot().leaveDirtyOpen).toBe(false)
  })

  it("pause success updates workspaceStatus and guestFormStatus without changing lastSavedAt", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "account-controls",
    })
    await page.load()

    const lastSavedBefore = page.getSnapshot().lastSavedAt
    page.requestPauseWorkspace()
    expect(page.getSnapshot().workspaceStatusConfirm).toBe("pause")

    await page.confirmWorkspaceStatusChange()

    expect(adapters.pauseWorkspace).toHaveBeenCalled()
    expect(adapters.resumeWorkspace).not.toHaveBeenCalled()
    expect(page.getSnapshot().workspaceStatusConfirm).toBeNull()
    expect(page.getSnapshot().accountDetails.status?.workspaceStatus).toBe(
      "Paused"
    )
    expect(page.getSnapshot().accountDetails.status?.guestFormStatus).toBe(
      "Paused"
    )
    expect(page.getSnapshot().lastSavedAt).toBe(lastSavedBefore)
    expect(page.getSnapshot().isDirty).toBe(false)
    expect(page.getSnapshot().saveEnabled).toBe(false)
    expect(page.getSnapshot().toast).toEqual({
      kind: "success",
      message: "Workspace paused.",
    })
  })

  it("cancel workspace status confirm does not call adapter", async () => {
    const adapters = createAdapters()
    const page = createOperatorAccountWorkspacePageModule(adapters, {
      initialTabId: "account-controls",
    })
    await page.load()

    page.requestPauseWorkspace()
    expect(page.getSnapshot().workspaceStatusConfirm).toBe("pause")

    page.cancelWorkspaceStatusConfirm()

    expect(adapters.pauseWorkspace).not.toHaveBeenCalled()
    expect(page.getSnapshot().workspaceStatusConfirm).toBeNull()
    expect(page.getSnapshot().accountDetails.status?.workspaceStatus).toBe(
      "Active"
    )
  })

})
import { describe, expect, it } from "vitest"

import {
  buildLocationControlsStatus,
  buildLocationGuestActivityChecklist,
  buildLocationSetupChecklist,
  buildLocationTeamAccessRows,
  locationControlsActionNeedsConfirm,
  locationControlsDangerActions,
  locationControlsLifecycleConfirmCopy,
} from "@/lib/operatorLocations/locationDetailPresentation"

describe("buildLocationSetupChecklist", () => {
  it("marks a ready active location as complete for required steps", () => {
    expect(
      buildLocationSetupChecklist({
        lifecycleStatus: "active",
        setupStatus: "ready",
        managerName: "Aisha",
        qrCount: 2,
        hasOffer: true,
      })
    ).toEqual({
      locationDetailsAdded: "complete",
      qrCodePublishedLive: "complete",
      guestFormConnected: "complete",
      teamAccessAssigned: "complete",
      guestPrivacyNotice: "complete",
      firstOfferCreated: "complete",
      atLeastOneQrCreated: "complete",
    })
  })

  it("shows optional team access and offer when not configured", () => {
    expect(
      buildLocationSetupChecklist({
        lifecycleStatus: "active",
        setupStatus: "ready",
        managerName: null,
        qrCount: 1,
        hasOffer: false,
      })
    ).toMatchObject({
      teamAccessAssigned: "optional",
      firstOfferCreated: "optional",
      atLeastOneQrCreated: "complete",
    })
  })

  it("marks draft locations as not started", () => {
    expect(
      buildLocationSetupChecklist({
        lifecycleStatus: "draft",
        setupStatus: "not-started",
        managerName: null,
        qrCount: 0,
        hasOffer: false,
      })
    ).toEqual({
      locationDetailsAdded: "not-started",
      qrCodePublishedLive: "not-started",
      guestFormConnected: "not-started",
      teamAccessAssigned: "optional",
      guestPrivacyNotice: "not-started",
      firstOfferCreated: "optional",
      atLeastOneQrCreated: "not-started",
    })
  })

  it("flags needs-attention setup as incomplete for QR and privacy", () => {
    expect(
      buildLocationSetupChecklist({
        lifecycleStatus: "active",
        setupStatus: "needs-attention",
        managerName: null,
        qrCount: 0,
        hasOffer: false,
      })
    ).toMatchObject({
      qrCodePublishedLive: "incomplete",
      guestPrivacyNotice: "incomplete",
      atLeastOneQrCreated: "incomplete",
    })
  })
})

describe("buildLocationGuestActivityChecklist", () => {
  it("marks needs recovery when pending recovery feedback exists", () => {
    expect(
      buildLocationGuestActivityChecklist({
        guestsCaptured: 12,
        optIns: 8,
        feedback: 3,
        offersClaimed: 2,
        offersRedeemed: 0,
        pendingRecoveryCount: 1,
        pendingFeedbackActionCount: 1,
      })
    ).toMatchObject({
      guestProfilesCreated: "complete",
      feedbackSubmitted: "needs-action",
      needsRecovery: "needs-action",
      offerRedemptions: "optional",
      unsubscribes: "optional",
    })
  })

  it("defaults empty activity to optional checklist rows", () => {
    expect(
      buildLocationGuestActivityChecklist({
        guestsCaptured: 0,
        optIns: 0,
        feedback: 0,
        offersClaimed: 0,
        offersRedeemed: 0,
        pendingRecoveryCount: 0,
        pendingFeedbackActionCount: 0,
      })
    ).toMatchObject({
      guestProfilesCreated: "optional",
      needsRecovery: "complete",
    })
  })
})

describe("buildLocationTeamAccessRows", () => {
  it("returns a manager row when the list row has a manager", () => {
    expect(
      buildLocationTeamAccessRows({
        managerName: "Amira Khan",
        managerUserId: 12,
      })
    ).toEqual([
      {
        id: "12",
        name: "Amira Khan",
        role: "Manager",
        accessLabel: "This location only",
        lastActiveLabel: "—",
      },
    ])
  })

  it("returns an empty list when no manager is assigned", () => {
    expect(
      buildLocationTeamAccessRows({
        managerName: null,
        managerUserId: null,
      })
    ).toEqual([])
  })
})

describe("buildLocationControlsStatus", () => {
  it("maps lifecycle and setup fields for an active ready location", () => {
    expect(
      buildLocationControlsStatus({
        lifecycleStatus: "active",
        setupStatus: "ready",
        liveQrCount: 6,
        lastScanLabel: "13:42",
        lastFeedbackLabel: "Today, 13:50",
      })
    ).toEqual({
      locationStatus: "Active",
      billingStatus: "Active",
      guestForm: "Live",
      lastScan: "13:42",
      qrCodes: "6 live",
      lastFeedback: "Today, 13:50",
      privacySetup: "Complete",
    })
  })

  it("uses placeholders when scan and feedback timestamps are missing", () => {
    expect(
      buildLocationControlsStatus({
        lifecycleStatus: "paused",
        setupStatus: "needs-attention",
        liveQrCount: 0,
      })
    ).toMatchObject({
      locationStatus: "Paused",
      guestForm: "Paused",
      lastScan: "—",
      qrCodes: "0 live",
      privacySetup: "Incomplete",
    })
  })
})

describe("locationControlsDangerActions", () => {
  it("enables pause for active locations and archive for paused locations", () => {
    expect(locationControlsDangerActions("active")).toEqual([
      {
        id: "pause",
        label: "Pause location",
        variant: "op-secondary",
        enabled: true,
      },
      {
        id: "archive",
        label: "Archive location",
        variant: "op-tertiary",
        enabled: false,
      },
    ])

    expect(locationControlsDangerActions("paused")).toEqual([
      {
        id: "resume",
        label: "Resume location",
        variant: "op-secondary",
        enabled: true,
      },
      {
        id: "archive",
        label: "Archive location",
        variant: "op-tertiary",
        enabled: true,
      },
    ])
  })
})

describe("locationControlsActionNeedsConfirm", () => {
  it("requires confirm for pause, archive, and restore only", () => {
    expect(locationControlsActionNeedsConfirm("pause")).toBe(true)
    expect(locationControlsActionNeedsConfirm("archive")).toBe(true)
    expect(locationControlsActionNeedsConfirm("restore")).toBe(true)
    expect(locationControlsActionNeedsConfirm("resume")).toBe(false)
  })
})

describe("locationControlsLifecycleConfirmCopy", () => {
  it("reuses locations list lifecycle confirm copy", () => {
    expect(
      locationControlsLifecycleConfirmCopy("pause", "Alpha Venue").title
    ).toBe("Pause location?")
  })
})

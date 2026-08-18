import { describe, expect, it, vi } from "vitest"

import type {
  FeedbackDetailsResponse,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import type { SetWorkflowStatusResponse } from "./createFeedbackDetailsModule"
import {
  createStartRecoveryEntryModule,
  type StartRecoveryEntryAdapters,
} from "./createStartRecoveryEntryModule"

const sampleDetails: FeedbackDetailsResponse = {
  success: true,
  id: 2418,
  guestName: "Mohamed M.",
  guestContact: "mohamed@email.com",
  contactType: "Email",
  comment: "Food was cold and delivery took too long.",
  createdAt: "2026-07-14T11:48:00.000Z",
  locationName: "Camden",
  address: "12 High Street",
  qrSource: "Delivery insert",
  classificationStatus: "Succeeded",
  sentiment: "negative",
  detectedTags: ["FoodTemperature", "FoodQuality", "SlowDelivery"],
  locationGuestId: 501,
  workflowStatus: "new",
  marketingPreference: "allowed",
  internalNotes: [],
  activityHistory: [],
}

function createAdapters(
  overrides: Partial<StartRecoveryEntryAdapters> & {
    details?: FeedbackDetailsResponse
  } = {}
): StartRecoveryEntryAdapters & {
  setWorkflowStatus: ReturnType<
    typeof vi.fn<
      (
        feedbackId: number,
        workflowStatus: FeedbackWorkflowStatus
      ) => Promise<SetWorkflowStatusResponse>
    >
  >
} {
  const details = overrides.details ?? sampleDetails
  const setWorkflowStatus =
    overrides.setWorkflowStatus
    ?? vi.fn(
      async (
        _feedbackId: number,
        _workflowStatus: FeedbackWorkflowStatus
      ): Promise<SetWorkflowStatusResponse> => ({
        workflowStatus: "in_progress",
        needsAttention: true,
      })
    )

  return {
    getFeedbackDetails:
      overrides.getFeedbackDetails
      ?? (async () => ({ ...details })),
    setWorkflowStatus: setWorkflowStatus as ReturnType<
      typeof vi.fn<
        (
          feedbackId: number,
          workflowStatus: FeedbackWorkflowStatus
        ) => Promise<SetWorkflowStatusResponse>
      >
    >,
  }
}

describe("createStartRecoveryEntryModule", () => {
  it("opens the shell, loads Feedback summary, and advances New → In progress", async () => {
    const adapters = createAdapters()
    const module = createStartRecoveryEntryModule(adapters)

    const openPromise = module.open(2418)
    expect(module.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "loading",
      feedbackId: 2418,
    })

    await openPromise

    const snapshot = module.getSnapshot()
    expect(snapshot).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      headerSubtitle: "FDB-002418 · Camden · Delivery insert",
      summary: {
        guestName: "Mohamed M.",
        classificationSentiment: "negative",
        contactLabel: "Email available",
        feedbackComment: "Food was cold and delivery took too long.",
      },
    })
    expect(snapshot.summary?.contactLabel).not.toContain("@")
    expect(snapshot.intents).toHaveLength(4)
    expect(snapshot.intents.every((intent) => intent.enabled)).toBe(true)
    expect(adapters.setWorkflowStatus).toHaveBeenCalledWith(
      2418,
      "in_progress"
    )
    expect(snapshot.workflowStatus).toBe("in_progress")
  })

  it("does not call setWorkflowStatus when already In progress", async () => {
    const adapters = createAdapters({
      details: { ...sampleDetails, workflowStatus: "in_progress" },
    })
    const module = createStartRecoveryEntryModule(adapters)

    await module.open(2418)

    expect(adapters.setWorkflowStatus).not.toHaveBeenCalled()
    expect(module.getSnapshot().workflowStatus).toBe("in_progress")
  })

  it("keeps In progress after Close/X", async () => {
    const adapters = createAdapters()
    const module = createStartRecoveryEntryModule(adapters)

    await module.open(2418)
    expect(module.getSnapshot().workflowStatus).toBe("in_progress")

    module.close()

    expect(module.getSnapshot()).toMatchObject({
      isOpen: false,
      loadStatus: "idle",
      feedbackId: null,
      summary: null,
    })
    expect(adapters.setWorkflowStatus).toHaveBeenCalledTimes(1)
  })

  it("disables Respond* when No contact; Record internal action only stays enabled", async () => {
    const adapters = createAdapters({
      details: {
        ...sampleDetails,
        contactType: "Unknown",
        guestContact: "",
      },
    })
    const module = createStartRecoveryEntryModule(adapters)

    await module.open(2418)

    const byId = Object.fromEntries(
      module.getSnapshot().intents.map((intent) => [intent.id, intent])
    )
    expect(byId["respond-to-guest"]).toMatchObject({
      enabled: false,
      disableReason: "No contact method available",
    })
    expect(byId["record-internal-action-only"]).toMatchObject({
      enabled: true,
    })
  })

  it("disables recovery-offer intent when Location Guest offers opt-out", async () => {
    const adapters = createAdapters({
      details: { ...sampleDetails, marketingPreference: "opted_out" },
    })
    const module = createStartRecoveryEntryModule(adapters)

    await module.open(2418)

    expect(
      module
        .getSnapshot()
        .intents.find((intent) => intent.id === "respond-with-recovery-offer")
    ).toMatchObject({
      enabled: false,
      disableReason: "Guest has opted out of offers",
    })
  })

  it("refuses to keep shell useful when Resolved — all intents disabled", async () => {
    const adapters = createAdapters({
      details: { ...sampleDetails, workflowStatus: "resolved" },
    })
    const module = createStartRecoveryEntryModule(adapters)

    await module.open(2418)

    expect(adapters.setWorkflowStatus).not.toHaveBeenCalled()
    expect(
      module.getSnapshot().intents.every((intent) => !intent.enabled)
    ).toBe(true)
  })

  it("records workflow advance error when New → In progress fails", async () => {
    const adapters = createAdapters({
      setWorkflowStatus: vi.fn(async () => {
        throw new Error("network")
      }),
    })
    const module = createStartRecoveryEntryModule(adapters)

    await module.open(2418)

    expect(module.getSnapshot()).toMatchObject({
      isOpen: true,
      loadStatus: "loaded",
      workflowStatus: "new",
      workflowAdvanceStatus: "error",
      workflowAdvanceError:
        "Could not update follow-up status. Please try again.",
    })
  })

  it("selecting an enabled intent exits the shell into that intent route stub", async () => {
    const adapters = createAdapters()
    const module = createStartRecoveryEntryModule(adapters)

    await module.open(2418)

    expect(module.getLoadedDetails()?.id).toBe(2418)

    const selected = module.selectIntent("respond-to-guest")

    expect(selected).toBe(true)
    expect(module.getSnapshot()).toMatchObject({
      isOpen: false,
      selectedIntentId: "respond-to-guest",
      feedbackId: 2418,
    })
    expect(module.getLoadedDetails()?.id).toBe(2418)
  })

  it("does not select a disabled intent", async () => {
    const adapters = createAdapters({
      details: {
        ...sampleDetails,
        contactType: "Unknown",
        guestContact: "",
      },
    })
    const module = createStartRecoveryEntryModule(adapters)

    await module.open(2418)

    expect(module.selectIntent("respond-to-guest")).toBe(false)
    expect(module.getSnapshot()).toMatchObject({
      isOpen: true,
      selectedIntentId: null,
    })
  })
})

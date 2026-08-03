import { describe, expect, it, vi } from "vitest"

import type { FeedbackDetailsResponse } from "@/types/dashboard"
import {
  createRecordInternalActionModule,
  type CompleteRecoveryResult,
  type RecordInternalActionAdapters,
  type RecordInternalActionRequest,
  type RecordInternalActionResult,
} from "./createRecordInternalActionModule"

const sampleDetails: FeedbackDetailsResponse = {
  success: true,
  id: 2418,
  guestName: "Mohamed M.",
  guestContact: "",
  contactType: "Unknown",
  comment: "Food was cold and delivery took too long.",
  createdAt: "2026-07-14T11:48:00.000Z",
  locationName: "Camden",
  address: "12 High Street",
  qrSource: "Delivery insert",
  classificationStatus: "Succeeded",
  sentiment: "negative",
  detectedTags: ["FoodTemperature"],
  locationGuestId: 501,
  workflowStatus: "in_progress",
  guestOffersOptOut: false,
  internalNotes: [],
  activityHistory: [],
}

function createAdapters(
  overrides: Partial<RecordInternalActionAdapters> = {}
): RecordInternalActionAdapters & {
  recordInternalAction: ReturnType<
    typeof vi.fn<
      (req: RecordInternalActionRequest) => Promise<RecordInternalActionResult>
    >
  >
  completeRecovery: ReturnType<
    typeof vi.fn<
      (
        feedbackId: number,
        intent: "respond_to_guest" | "record_internal_action_only"
      ) => Promise<CompleteRecoveryResult>
    >
  >
} {
  const recordInternalAction =
    overrides.recordInternalAction
    ?? vi.fn(async (): Promise<RecordInternalActionResult> => ({
      workflowStatus: "in_progress",
      needsAttention: true,
      activityEvent: {
        kind: "internal_action_recorded",
        at: "2026-08-03T12:00:00.000Z",
        actorDisplayName: "Alex",
        category: "team_briefed",
        categoryLabel: "Team briefed",
        note: "Briefed the floor team.",
      },
    }))

  const completeRecovery =
    overrides.completeRecovery
    ?? vi.fn(async (): Promise<CompleteRecoveryResult> => ({
      workflowStatus: "resolved",
      needsAttention: false,
      activityEvent: {
        kind: "recovery_completed",
        at: "2026-08-03T12:05:00.000Z",
        actorDisplayName: "Alex",
        recoveryIntent: "record_internal_action_only",
        fromWorkflowStatus: "in_progress",
        toWorkflowStatus: "resolved",
      },
    }))

  return {
    getFeedbackDetails:
      overrides.getFeedbackDetails ?? (async () => ({ ...sampleDetails })),
    recordInternalAction: recordInternalAction as ReturnType<
      typeof vi.fn<
        (req: RecordInternalActionRequest) => Promise<RecordInternalActionResult>
      >
    >,
    completeRecovery: completeRecovery as ReturnType<
      typeof vi.fn<
        (
          feedbackId: number,
          intent: "respond_to_guest" | "record_internal_action_only"
        ) => Promise<CompleteRecoveryResult>
      >
    >,
  }
}

async function openAtReview(
  module: ReturnType<typeof createRecordInternalActionModule>
) {
  await module.open(2418)
  module.setCategory("team_briefed")
  module.setNote("Briefed the floor team.")
  module.continueRecorder()
}

describe("createRecordInternalActionModule", () => {
  it("opens at Internal action without requiring contact", async () => {
    const module = createRecordInternalActionModule(createAdapters())
    await module.open(2418)

    expect(module.getSnapshot()).toMatchObject({
      isOpen: true,
      step: "recorder",
      feedbackId: 2418,
      canContinueRecorder: false,
      followUpStateLabel: "Mark follow-up complete",
      recoveryStatusLabel: "In progress",
    })
    expect(module.getSnapshot().headerSubtitle).toContain("FDB-2418")
  })

  it("advances recorder → review when category and note are set", async () => {
    const module = createRecordInternalActionModule(createAdapters())
    await module.open(2418)

    module.setCategory("team_briefed")
    module.setNote("Briefed the floor team.")
    expect(module.getSnapshot().canContinueRecorder).toBe(true)
    module.continueRecorder()
    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      category: "team_briefed",
      note: "Briefed the floor team.",
    })
    expect(module.getSnapshot().summary?.categoryLabel).toBe("Team briefed")
  })

  it("Back from recorder returns to entry shell; Back from review returns to recorder", async () => {
    const module = createRecordInternalActionModule(createAdapters())
    await module.open(2418)

    expect(module.back()).toBe("return-to-shell")
    expect(module.getSnapshot().isOpen).toBe(false)

    await openAtReview(module)
    expect(module.back()).toBe("stayed")
    expect(module.getSnapshot().step).toBe("recorder")
  })

  it("Save and exit keeps intent-scoped draft; resume restores furthest step", async () => {
    const module = createRecordInternalActionModule(createAdapters())
    await module.open(2418)
    module.setCategory("product_quality_checked")
    module.setNote("Checked the kitchen batch.")
    module.saveAndExit()

    expect(module.getSnapshot().isOpen).toBe(false)

    await module.open(2418)
    expect(module.getSnapshot()).toMatchObject({
      isOpen: true,
      step: "recorder",
      category: "product_quality_checked",
      note: "Checked the kitchen batch.",
    })

    module.continueRecorder()
    module.saveAndExit()
    await module.open(2418)
    expect(module.getSnapshot().step).toBe("review")
  })

  it("confirm records internal-action fact only and stays In progress", async () => {
    const adapters = createAdapters()
    const module = createRecordInternalActionModule(adapters)
    await openAtReview(module)

    module.openRecordConfirm()
    expect(module.getSnapshot().recordConfirmOpen).toBe(true)

    await module.confirmRecord()

    expect(adapters.recordInternalAction).toHaveBeenCalledWith({
      feedbackId: 2418,
      category: "team_briefed",
      note: "Briefed the floor team.",
      intent: "record_internal_action_only",
    })
    expect(module.getSnapshot()).toMatchObject({
      step: "success",
      recordConfirmOpen: false,
      workflowStatus: "in_progress",
    })

    module.saveAndExit()
    await module.open(2418)
    expect(module.getSnapshot().step).toBe("recorder")
    expect(module.getSnapshot().note).toBe("")
  })

  it("record failure stays on confirm with retry; no success step", async () => {
    const adapters = createAdapters({
      recordInternalAction: vi.fn(async () => {
        throw new Error("persist failed")
      }),
    })
    const module = createRecordInternalActionModule(adapters)
    await openAtReview(module)
    module.openRecordConfirm()

    await module.confirmRecord()

    expect(module.getSnapshot()).toMatchObject({
      step: "review",
      recordConfirmOpen: true,
      recordStatus: "error",
    })
    expect(module.getSnapshot().recordError).toBeTruthy()
  })

  it("Keep in progress closes; Mark resolved completes recovery for this intent", async () => {
    const adapters = createAdapters()
    const module = createRecordInternalActionModule(adapters)
    await openAtReview(module)
    await module.confirmRecord()

    module.keepInProgress()
    expect(module.getSnapshot().isOpen).toBe(false)
    expect(adapters.completeRecovery).not.toHaveBeenCalled()

    await openAtReview(module)
    await module.confirmRecord()
    await module.markResolved()

    expect(adapters.completeRecovery).toHaveBeenCalledWith(
      2418,
      "record_internal_action_only"
    )
    expect(module.getSnapshot().isOpen).toBe(false)
  })
})

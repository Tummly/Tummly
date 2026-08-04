import { describe, expect, it } from "vitest"

import { buildFeedbackInboxRowActions } from "./feedbackInboxRowActions"

describe("buildFeedbackInboxRowActions", () => {
  it("orders PRD labels and enables close-out when not Resolved", () => {
    const actions = buildFeedbackInboxRowActions("new")

    expect(actions.map((action) => action.id)).toEqual([
      "start-recovery",
      "view-feedback",
      "mark-resolved",
      "mark-no-action-needed",
    ])
    expect(actions.map((action) => action.label)).toEqual([
      "Start recovery",
      "View feedback",
      "Mark resolved",
      "Mark no action needed",
    ])
    expect(actions.every((action) => action.visible)).toBe(true)
    expect(actions.every((action) => action.enabled)).toBe(true)
  })

  it("enables Start recovery for In progress and keeps View feedback on", () => {
    const actions = buildFeedbackInboxRowActions("in_progress")

    expect(actions.find((a) => a.id === "start-recovery")).toMatchObject({
      enabled: true,
      visible: true,
    })
    expect(actions.find((a) => a.id === "view-feedback")).toMatchObject({
      enabled: true,
      visible: true,
    })
  })

  it("disables Start recovery and hides close-out when Resolved", () => {
    const actions = buildFeedbackInboxRowActions("resolved")

    expect(actions.find((a) => a.id === "start-recovery")).toMatchObject({
      enabled: false,
      visible: true,
    })
    expect(actions.find((a) => a.id === "view-feedback")).toMatchObject({
      enabled: true,
      visible: true,
    })
    expect(actions.find((a) => a.id === "mark-resolved")).toMatchObject({
      visible: false,
    })
    expect(actions.find((a) => a.id === "mark-no-action-needed")).toMatchObject({
      visible: false,
    })
  })
})

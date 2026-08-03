import { describe, expect, it } from "vitest"

import {
  recoverySendConfirmCopy,
  type RecoverySendConfirmIntent,
} from "./recoverySendConfirmPresentation"

const MASKED = "m••••••@email.com"

describe("recoverySendConfirmPresentation", () => {
  it("Respond to guest confirm matches Figma Send Response (masked destination)", () => {
    const copy = recoverySendConfirmCopy({
      intent: "respond_to_guest",
      maskedDestination: MASKED,
      sendStatus: "idle",
    })
    expect(copy.title).toBe("Send this response?")
    expect(copy.description).toBe(
      `This will send the message to ${MASKED} and record the response against this feedback.`
    )
    expect(copy.confirmLabel).toBe("Send response")
  })

  it("Respond to guest confirm retry label stays Send again after failure", () => {
    expect(
      recoverySendConfirmCopy({
        intent: "respond_to_guest",
        maskedDestination: MASKED,
        sendStatus: "error",
      }).confirmLabel
    ).toBe("Send again")
  })

  it("Respond and record confirm matches Figma overlay copy", () => {
    const copy = recoverySendConfirmCopy({
      intent: "respond_and_record_internal_action",
      maskedDestination: MASKED,
      sendStatus: "idle",
    })
    expect(copy.title).toBe("Send response and record internal action?")
    expect(copy.description).toBe(
      `This will send the response to ${MASKED} and record the internal follow-up against this feedback.`
    )
    expect(copy.confirmLabel).toBe("Send and record")
  })

  it("Record internal only confirm keeps PRD-allowed Figma debt copy", () => {
    const copy = recoverySendConfirmCopy({
      intent: "record_internal_action_only",
      maskedDestination: null,
      sendStatus: "idle",
    })
    expect(copy.title).toBe("Record internal follow up?")
    expect(copy.description).toBe(
      "This will record the internal follow-up against this feedback."
    )
    expect(copy.confirmLabel).toBe("Send and record")
  })

  it("Send and issue offer confirm matches Figma overlay copy", () => {
    const copy = recoverySendConfirmCopy({
      intent: "respond_with_recovery_offer",
      maskedDestination: MASKED,
      sendStatus: "idle",
    })
    expect(copy.title).toBe("Send response and issue offer?")
    expect(copy.description).toBe(
      `This will send the response to ${MASKED} and activate the recovery offer for this guest.`
    )
    expect(copy.confirmLabel).toBe("Send and issue offer")
  })

  it("guest-send intents omit destination clause when mask is missing", () => {
    const cases: {
      intent: RecoverySendConfirmIntent
      description: string
    }[] = [
      {
        intent: "respond_to_guest",
        description:
          "This will send the message and record the response against this feedback.",
      },
      {
        intent: "respond_and_record_internal_action",
        description:
          "This will send the response and record the internal follow-up against this feedback.",
      },
      {
        intent: "respond_with_recovery_offer",
        description:
          "This will send the response and activate the recovery offer for this guest.",
      },
    ]
    for (const { intent, description } of cases) {
      expect(
        recoverySendConfirmCopy({
          intent,
          maskedDestination: null,
          sendStatus: "idle",
        }).description
      ).toBe(description)
    }
  })
})

import { describe, expect, it } from "vitest"

import {
  RESPOND_TO_GUEST_PURPOSE_OPTIONS,
  RESPOND_TO_GUEST_TONE_OPTIONS,
  availableRespondToGuestChannels,
  canContinueRespondToGuestMessage,
  canContinueRespondToGuestSetup,
  defaultRespondToGuestChannel,
  furthestRespondToGuestStep,
  maskRespondToGuestDestination,
  type RespondToGuestDraft,
} from "./respondToGuestPresentation"

describe("respondToGuestPresentation", () => {
  it("lists PRD purposes without Include a recovery offer", () => {
    expect(RESPOND_TO_GUEST_PURPOSE_OPTIONS.map((o) => o.id)).toEqual([
      "acknowledge_feedback",
      "apologise_and_confirm_follow_up",
      "ask_for_more_information",
      "confirm_operational_action",
      "create_custom_response",
    ])
    expect(RESPOND_TO_GUEST_PURPOSE_OPTIONS.map((o) => o.label)).toEqual([
      "Acknowledge the feedback",
      "Apologise and confirm follow-up",
      "Ask for more information",
      "Confirm an operational action",
      "Create a custom response",
    ])
    expect(
      RESPOND_TO_GUEST_PURPOSE_OPTIONS.some((o) =>
        o.label.toLowerCase().includes("recovery offer")
      )
    ).toBe(false)
  })

  it("lists PRD tones", () => {
    expect(RESPOND_TO_GUEST_TONE_OPTIONS.map((o) => o.id)).toEqual([
      "warm_and_apologetic",
      "direct_and_practical",
      "appreciative",
      "use_restaurant_tone",
    ])
    expect(RESPOND_TO_GUEST_TONE_OPTIONS.map((o) => o.label)).toEqual([
      "Warm and apologetic",
      "Direct and practical",
      "Appreciative",
      "Use restaurant tone",
    ])
  })

  it("derives available channels and defaults from contact capability", () => {
    expect(availableRespondToGuestChannels("email_available")).toEqual([
      "email",
    ])
    expect(defaultRespondToGuestChannel("email_available")).toBe("email")

    expect(availableRespondToGuestChannels("sms_available")).toEqual(["sms"])
    expect(defaultRespondToGuestChannel("sms_available")).toBe("sms")

    expect(availableRespondToGuestChannels("no_contact")).toEqual([])
    expect(defaultRespondToGuestChannel("no_contact")).toBeNull()
  })

  it("masks destination for Email and Phone — never raw contact", () => {
    expect(
      maskRespondToGuestDestination("Email", "mohamed@email.com")
    ).toBe("m••••@email.com")
    expect(
      maskRespondToGuestDestination("Phone", "+447700900123")
    ).toBe("••••0123")
    expect(maskRespondToGuestDestination("Email", "m••••@email.com")).not.toBe(
      "mohamed@email.com"
    )
  })

  it("gates Response setup Continue on channel, purpose, and tone", () => {
    expect(
      canContinueRespondToGuestSetup({
        channel: null,
        purpose: "acknowledge_feedback",
        tone: "warm_and_apologetic",
      })
    ).toBe(false)
    expect(
      canContinueRespondToGuestSetup({
        channel: "email",
        purpose: null,
        tone: "warm_and_apologetic",
      })
    ).toBe(false)
    expect(
      canContinueRespondToGuestSetup({
        channel: "email",
        purpose: "acknowledge_feedback",
        tone: null,
      })
    ).toBe(false)
    expect(
      canContinueRespondToGuestSetup({
        channel: "email",
        purpose: "acknowledge_feedback",
        tone: "warm_and_apologetic",
      })
    ).toBe(true)
  })

  it("requires Message; Subject only for Email", () => {
    expect(
      canContinueRespondToGuestMessage({
        channel: "email",
        subject: "",
        message: "Hello",
      })
    ).toBe(false)
    expect(
      canContinueRespondToGuestMessage({
        channel: "email",
        subject: "  ",
        message: "Hello",
      })
    ).toBe(false)
    expect(
      canContinueRespondToGuestMessage({
        channel: "email",
        subject: "Sorry",
        message: "  ",
      })
    ).toBe(false)
    expect(
      canContinueRespondToGuestMessage({
        channel: "email",
        subject: "Sorry",
        message: "We are looking into this.",
      })
    ).toBe(true)
    expect(
      canContinueRespondToGuestMessage({
        channel: "sms",
        subject: "",
        message: "We are looking into this.",
      })
    ).toBe(true)
    expect(
      canContinueRespondToGuestMessage({
        channel: "sms",
        subject: null,
        message: "",
      })
    ).toBe(false)
  })

  it("resumes at furthest incomplete step from draft", () => {
    const empty: RespondToGuestDraft = {
      channel: null,
      purpose: null,
      tone: null,
      includeNotes: "",
      subject: "",
      message: "",
      setupComplete: false,
      messageComplete: false,
    }
    expect(furthestRespondToGuestStep(empty)).toBe("setup")

    const setupDone: RespondToGuestDraft = {
      ...empty,
      channel: "email",
      purpose: "acknowledge_feedback",
      tone: "warm_and_apologetic",
      setupComplete: true,
    }
    expect(furthestRespondToGuestStep(setupDone)).toBe("write")

    const messageDone: RespondToGuestDraft = {
      ...setupDone,
      subject: "Sorry about your visit",
      message: "Thank you for telling us.",
      messageComplete: true,
    }
    expect(furthestRespondToGuestStep(messageDone)).toBe("review")
  })
})

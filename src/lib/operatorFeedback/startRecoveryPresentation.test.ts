import { describe, expect, it } from "vitest"

import {
  START_RECOVERY_INTENT_CARD_IDLE_CLASS,
  START_RECOVERY_INTENT_CARD_SURFACE_CLASS,
  START_RECOVERY_SUMMARY_CLASS,
  START_RECOVERY_SUMMARY_DIVIDER_CLASS,
  buildStartRecoveryIntents,
  deriveStartRecoveryContactCapability,
  startRecoveryContactCapabilityLabel,
} from "./startRecoveryPresentation"

describe("startRecoveryPresentation", () => {
  it("maps contact to capability-only labels — never raw address or phone", () => {
    expect(
      deriveStartRecoveryContactCapability("Email", "guest@example.com")
    ).toBe("email_available")
    expect(
      startRecoveryContactCapabilityLabel("email_available")
    ).toBe("Email available")

    expect(
      deriveStartRecoveryContactCapability("Phone", "+447700900123")
    ).toBe("sms_available")
    expect(startRecoveryContactCapabilityLabel("sms_available")).toBe(
      "SMS available"
    )

    expect(deriveStartRecoveryContactCapability("Unknown", "")).toBe(
      "no_contact"
    )
    expect(deriveStartRecoveryContactCapability("Email", "  ")).toBe(
      "no_contact"
    )
    expect(startRecoveryContactCapabilityLabel("no_contact")).toBe(
      "No contact"
    )
  })

  it("orders four PRD intents with canonical copy when contact is available", () => {
    const intents = buildStartRecoveryIntents({
      contactCapability: "email_available",
      marketingPreference: "allowed",
      workflowStatus: "new",
    })

    expect(intents.map((intent) => intent.id)).toEqual([
      "respond-to-guest",
      "respond-and-record-internal-action",
      "record-internal-action-only",
      "respond-with-recovery-offer",
    ])
    expect(intents.map((intent) => intent.title)).toEqual([
      "Respond to the guest",
      "Respond and record an internal action",
      "Record an internal action only",
      "Respond with a recovery offer",
    ])
    expect(intents.map((intent) => intent.description)).toEqual([
      "Prepare and send a private response using an available contact method.",
      "Prepare a guest response and record what the restaurant will review or change.",
      "Document what the restaurant reviewed or changed without contacting the guest.",
      "Prepare a controlled offer and include it in the guest’s recovery response.",
    ])
    expect(intents.every((intent) => intent.enabled)).toBe(true)
    expect(intents.every((intent) => intent.disableReason == null)).toBe(true)
  })

  it("disables Respond* intents when No contact; keeps Record internal action only enabled", () => {
    const intents = buildStartRecoveryIntents({
      contactCapability: "no_contact",
      marketingPreference: "allowed",
      workflowStatus: "in_progress",
    })

    expect(intents.find((i) => i.id === "respond-to-guest")).toMatchObject({
      enabled: false,
      disableReason: "No contact method available",
    })
    expect(
      intents.find((i) => i.id === "respond-and-record-internal-action")
    ).toMatchObject({
      enabled: false,
      disableReason: "No contact method available",
    })
    expect(
      intents.find((i) => i.id === "record-internal-action-only")
    ).toMatchObject({
      enabled: true,
      disableReason: null,
    })
    expect(
      intents.find((i) => i.id === "respond-with-recovery-offer")
    ).toMatchObject({
      enabled: false,
      disableReason: "No contact method available",
    })
  })

  it("disables recovery-offer intent when Location Guest offers opt-out", () => {
    const intents = buildStartRecoveryIntents({
      contactCapability: "sms_available",
      marketingPreference: "opted_out",
      workflowStatus: "new",
    })

    expect(intents.find((i) => i.id === "respond-to-guest")).toMatchObject({
      enabled: true,
    })
    expect(
      intents.find((i) => i.id === "respond-with-recovery-offer")
    ).toMatchObject({
      enabled: false,
      disableReason: "Guest has opted out of offers",
    })
  })

  it("disables recovery-offer intent when Location Guest preference is not recorded", () => {
    const intents = buildStartRecoveryIntents({
      contactCapability: "email_available",
      marketingPreference: "not_recorded",
      workflowStatus: "new",
    })

    expect(
      intents.find((i) => i.id === "respond-with-recovery-offer")
    ).toMatchObject({
      enabled: false,
      disableReason: "Guest has opted out of offers",
    })
  })

  it("disables all intents when Feedback is Resolved", () => {
    const intents = buildStartRecoveryIntents({
      contactCapability: "email_available",
      marketingPreference: "allowed",
      workflowStatus: "resolved",
    })

    expect(intents.every((intent) => !intent.enabled)).toBe(true)
  })

  it("uses gray-60 / gray-1000 fill, divider idle border, and gray-550 idle hover", () => {
    expect(START_RECOVERY_INTENT_CARD_SURFACE_CLASS).toContain("bg-op-color-gray-60")
    expect(START_RECOVERY_INTENT_CARD_SURFACE_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
    expect(START_RECOVERY_INTENT_CARD_SURFACE_CLASS).toContain("border-op-divider")
    expect(START_RECOVERY_INTENT_CARD_SURFACE_CLASS).not.toContain(
      "dark:border-[var(--op-color-gray-550)]"
    )
    expect(START_RECOVERY_INTENT_CARD_IDLE_CLASS).toContain(
      "hover:border-[var(--op-color-gray-550)]"
    )
    expect(START_RECOVERY_SUMMARY_CLASS).toContain(
      START_RECOVERY_INTENT_CARD_SURFACE_CLASS
    )
    expect(START_RECOVERY_SUMMARY_DIVIDER_CLASS).toBe("bg-op-divider")
  })
})

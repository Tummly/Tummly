import { describe, expect, it } from "vitest"

import {
  RECOVERY_AI_NO_CREDITS_REMAINING,
  RECOVERY_BUY_AI_CREDITS_LABEL,
  RECOVERY_BUY_SMS_CREDITS_LABEL,
  RECOVERY_CHANGE_PLAN_LABEL,
  RECOVERY_CHOOSE_PLAN_LABEL,
  RECOVERY_EMAIL_AVAILABILITY_LINE,
  RECOVERY_SMS_AVAILABILITY_FLOOR_LINE,
  RECOVERY_UPDATE_PAYMENT_LABEL,
  isRecoveryEmailCreditBlocked,
  isRecoverySendBlocked,
  recoveryChannelAvailabilityLine,
  resolveRecoveryAiActionChipChrome,
  resolveRecoveryPaidWriteChrome,
  resolveRecoverySmsEstimateCredits,
  resolveRecoverySmsShortfall,
  type RecoveryCreditChromeContext,
} from "./recoveryCreditChromePresentation"

function ownerContext(
  overrides: Partial<RecoveryCreditChromeContext> = {}
): RecoveryCreditChromeContext {
  return {
    smsRemaining: 10,
    aiRemaining: 10,
    isPilot: false,
    paidActionsLocked: false,
    restorationCause: null,
    accessLevel: "manage",
    permissionRole: "Owner",
    mode: "single",
    locationId: 42,
    ...overrides,
  }
}

describe("recoveryCreditChromePresentation", () => {
  it("Email availability never credit-blocks", () => {
    expect(RECOVERY_EMAIL_AVAILABILITY_LINE).toBe(
      "Available · No email credits required"
    )
    expect(
      recoveryChannelAvailabilityLine({ channel: "email", messageBody: "" })
    ).toBe(RECOVERY_EMAIL_AVAILABILITY_LINE)
    expect(isRecoveryEmailCreditBlocked()).toBe(false)
    expect(
      isRecoverySendBlocked({
        channel: "email",
        messageBody: "Hello",
        context: ownerContext({ smsRemaining: 0, aiRemaining: 0 }),
      })
    ).toBe(false)
  })

  it("SMS floor before body; estimate after body; shortfall blocks send", () => {
    expect(
      recoveryChannelAvailabilityLine({ channel: "sms", messageBody: "" })
    ).toBe(RECOVERY_SMS_AVAILABILITY_FLOOR_LINE)
    expect(RECOVERY_SMS_AVAILABILITY_FLOOR_LINE).toContain("at least 1")

    const longBody = "x".repeat(161)
    expect(resolveRecoverySmsEstimateCredits(longBody)).toBe(2)
    expect(
      recoveryChannelAvailabilityLine({
        channel: "sms",
        messageBody: longBody,
      })
    ).toBe("Estimated usage: 2 SMS credits")

    const shortfall = resolveRecoverySmsShortfall({
      channel: "sms",
      messageBody: longBody,
      smsRemaining: 1,
      context: ownerContext({ smsRemaining: 1 }),
    })
    expect(shortfall.blocked).toBe(true)
    expect(shortfall.buyCta?.label).toBe(RECOVERY_BUY_SMS_CREDITS_LABEL)
    expect(shortfall.buyCta?.href).toContain("channel=sms")
    expect(shortfall.changePlanCta?.label).toBe(RECOVERY_CHANGE_PLAN_LABEL)
    expect(
      isRecoverySendBlocked({
        channel: "sms",
        messageBody: longBody,
        context: ownerContext({ smsRemaining: 1 }),
      })
    ).toBe(true)
  })

  it("AI chip at 0 disables Prepare and keeps Write manually; Pilot hides Buy", () => {
    const depleted = resolveRecoveryAiActionChipChrome({
      context: ownerContext({ aiRemaining: 0 }),
    })
    expect(depleted.prepareAllowed).toBe(false)
    expect(depleted.writeManuallyAllowed).toBe(true)
    expect(depleted.depletedMessage).toBe(RECOVERY_AI_NO_CREDITS_REMAINING)
    expect(depleted.buyCta?.label).toBe(RECOVERY_BUY_AI_CREDITS_LABEL)
    expect(depleted.changePlanCta?.label).toBe(RECOVERY_CHANGE_PLAN_LABEL)

    const pilot = resolveRecoveryAiActionChipChrome({
      context: ownerContext({ aiRemaining: 0, isPilot: true }),
    })
    expect(pilot.buyCta).toBeNull()
    expect(pilot.changePlanCta?.label).toBe(RECOVERY_CHANGE_PLAN_LABEL)

    const viewOnly = resolveRecoveryAiActionChipChrome({
      context: ownerContext({
        aiRemaining: 0,
        accessLevel: "view",
        permissionRole: "Marketing",
      }),
    })
    expect(viewOnly.buyCta).toBeNull()
    expect(viewOnly.changePlanCta).toBeNull()
  })

  it("Soft lock disables burn control and exposes restoration helper", () => {
    const pilotLock = resolveRecoveryPaidWriteChrome({
      context: ownerContext({
        paidActionsLocked: true,
        restorationCause: "unpaid-pilot",
        isPilot: true,
      }),
    })
    expect(pilotLock.burnDisabled).toBe(true)
    expect(pilotLock.helperCta?.label).toBe(RECOVERY_CHOOSE_PLAN_LABEL)
    expect(pilotLock.helperCta?.href).toContain("/manage-plan")
    expect(pilotLock.helperCta?.href).not.toContain("section=credit-top-ups")

    const dunning = resolveRecoveryPaidWriteChrome({
      context: ownerContext({
        paidActionsLocked: true,
        restorationCause: "dunning",
      }),
    })
    expect(dunning.helperCta?.label).toBe(RECOVERY_UPDATE_PAYMENT_LABEL)
    expect(dunning.helperCta?.href).toContain("tab=payment-invoices")

    expect(
      isRecoverySendBlocked({
        channel: "email",
        messageBody: "Hi",
        context: ownerContext({
          paidActionsLocked: true,
          restorationCause: "unpaid-pilot",
        }),
      })
    ).toBe(true)
  })
})

import { AxiosError } from "axios"
import { describe, expect, it } from "vitest"

import {
  RECOVERY_COMPOSER_COPY,
  RECOVERY_COMPOSER_STATUS_BANNER_CLASS,
  buildRecoveryComposerStatusBanner,
  mapRecoveryComposerSendFailure,
} from "./recoveryComposerPresentation"

describe("recoveryComposerPresentation", () => {
  it("uses theme-aware Main Bg tokens for status banner", () => {
    expect(RECOVERY_COMPOSER_STATUS_BANNER_CLASS).toContain(
      "bg-op-background-primary"
    )
    expect(RECOVERY_COMPOSER_STATUS_BANNER_CLASS).not.toContain(
      "op-color-gray-995"
    )
  })

  it("status banner is always service-reply (marketing not required)", () => {
    expect(buildRecoveryComposerStatusBanner()).toEqual({
      kind: "service-only",
      message: RECOVERY_COMPOSER_COPY.serviceReply,
    })
  })

  it("server follow-up denial maps to no_permission", () => {
    expect(
      mapRecoveryComposerSendFailure(
        new Error("Guest has not granted feedback follow-up permission.")
      )
    ).toEqual({
      kind: "no_permission",
      message: RECOVERY_COMPOSER_COPY.noPermission,
    })
  })

  it("maps feedback follow-up permission substring case-insensitively", () => {
    expect(
      mapRecoveryComposerSendFailure(
        new Error("Denied: Feedback Follow-Up Permission required")
      )
    ).toMatchObject({ kind: "no_permission" })
  })

  it("an 'eligibility changed' server message maps to kind failed, not a detach message", () => {
    const mapped = mapRecoveryComposerSendFailure(
      new Error("Marketing eligibility changed. Permission changed.")
    )
    expect(mapped).toEqual({
      kind: "failed",
      message: RECOVERY_COMPOSER_COPY.sendFailed,
    })
    expect(mapped.message).not.toMatch(/attached Offer was removed/i)
    expect(mapped.message).not.toMatch(/marketing permission/i)
  })

  it("RC-03: maps provider unavailable failures", () => {
    expect(
      mapRecoveryComposerSendFailure(
        new Error("Billing Reserve is not available. Recovery SMS send stays blocked.")
      )
    ).toEqual({
      kind: "provider_unavailable",
      message: RECOVERY_COMPOSER_COPY.providerUnavailable,
    })
  })

  it("a permanent Email hard bounce shows the safe delivery status, not provider copy", () => {
    expect(
      mapRecoveryComposerSendFailure(
        new Error("Email delivery unavailable. Use a verified alternative contact method.")
      )
    ).toEqual({
      kind: "refused",
      message: "Email delivery unavailable. Use a verified alternative contact method.",
    })
  })

  it("RC-03: maps unknown failures to generic send failed", () => {
    expect(mapRecoveryComposerSendFailure(new Error("boom"))).toEqual({
      kind: "failed",
      message: RECOVERY_COMPOSER_COPY.sendFailed,
    })
  })
})

describe("mapRecoveryComposerSendFailure refusals", () => {
  it("409 refusal keeps the server message for the user", () => {
    const error = new AxiosError(
      "Request failed with status code 409",
      AxiosError.ERR_BAD_REQUEST,
      undefined,
      undefined,
      {
        status: 409,
        statusText: "Conflict",
        headers: {},
        config: {} as never,
        data: {
          success: false,
          code: "duplicate_reply",
          message: "This reply has already been submitted. Check its delivery status.",
        },
      }
    )

    expect(mapRecoveryComposerSendFailure(error)).toEqual({
      kind: "refused",
      message: "This reply has already been submitted. Check its delivery status.",
    })
  })

  it("409 sms_stopped shows the server message", () => {
    const serverMessage =
      "This message cannot be sent for the selected purpose or recipient. Review the reason."
    const error = new AxiosError(
      "Request failed with status code 409",
      AxiosError.ERR_BAD_REQUEST,
      undefined,
      undefined,
      {
        status: 409,
        statusText: "Conflict",
        headers: {},
        config: {} as never,
        data: {
          success: false,
          code: "sms_stopped",
          message: serverMessage,
        },
      }
    )

    expect(mapRecoveryComposerSendFailure(error)).toEqual({
      kind: "refused",
      message: serverMessage,
    })
  })

  it("502 SMS send failure keeps the server message for the user", () => {
    const error = new AxiosError(
      "Request failed with status code 502",
      AxiosError.ERR_BAD_RESPONSE,
      undefined,
      undefined,
      {
        status: 502,
        statusText: "Bad Gateway",
        headers: {},
        config: {} as never,
        data: {
          success: false,
          code: "sms_send_failed",
          message:
            "The reply was not confirmed as sent. Check its status before trying again.",
        },
      }
    )

    expect(mapRecoveryComposerSendFailure(error)).toEqual({
      kind: "refused",
      message:
        "The reply was not confirmed as sent. Check its status before trying again.",
    })
  })
})

describe("mapRecoveryComposerSendFailure refusal codes", () => {
  const SMS_STOP_MESSAGE =
    "SMS not sent. This number has opted out of SMS from this sender, and delivery is currently blocked. This does not change the guest's Feedback follow-up permission."

  function refusal(status: number, code: string, message: string) {
    return new AxiosError(
      `Request failed with status code ${status}`,
      AxiosError.ERR_BAD_REQUEST,
      undefined,
      undefined,
      {
        status,
        statusText: "Bad Request",
        headers: {},
        config: {} as never,
        data: { success: false, code, message },
      }
    )
  }

  it("400 sms_provider_restriction shows the SMS STOP copy, not the permission copy", () => {
    expect(
      mapRecoveryComposerSendFailure(
        refusal(400, "sms_provider_restriction", SMS_STOP_MESSAGE)
      )
    ).toEqual({
      kind: "refused",
      message: SMS_STOP_MESSAGE,
    })
    expect(RECOVERY_COMPOSER_COPY.smsProviderRestriction).toBe(SMS_STOP_MESSAGE)
  })

  it("400 feedback_follow_up_not_granted keeps the permission copy", () => {
    expect(
      mapRecoveryComposerSendFailure(
        refusal(
          400,
          "feedback_follow_up_not_granted",
          "Guest has not granted feedback follow-up permission."
        )
      )
    ).toEqual({
      kind: "no_permission",
      message: RECOVERY_COMPOSER_COPY.noPermission,
    })
  })

  it("400 email_delivery_unavailable keeps the Email delivery copy", () => {
    expect(
      mapRecoveryComposerSendFailure(
        refusal(
          400,
          "email_delivery_unavailable",
          "Email delivery unavailable. Use a verified alternative contact method."
        )
      )
    ).toEqual({
      kind: "refused",
      message: RECOVERY_COMPOSER_COPY.emailDeliveryUnavailable,
    })
  })

  it("SMS STOP message without a code is not read as a permission refusal", () => {
    expect(mapRecoveryComposerSendFailure(new Error(SMS_STOP_MESSAGE))).toEqual({
      kind: "refused",
      message: SMS_STOP_MESSAGE,
    })
  })
})

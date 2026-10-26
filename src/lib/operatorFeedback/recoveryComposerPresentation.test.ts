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
          message: "This reply was already sent to the guest.",
        },
      }
    )

    expect(mapRecoveryComposerSendFailure(error)).toEqual({
      kind: "refused",
      message: "This reply was already sent to the guest.",
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

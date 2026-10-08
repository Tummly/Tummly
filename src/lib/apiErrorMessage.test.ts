import { AxiosError } from "axios"
import { describe, expect, it } from "vitest"

import {
  API_PERMISSION_DENIED_MESSAGE,
  getUserFacingApiErrorMessage,
} from "./apiErrorMessage"

function axiosError(input: {
  status?: number
  message?: string
  data?: unknown
}): AxiosError {
  return new AxiosError(
    input.message ?? "Request failed with status code 403",
    undefined,
    undefined,
    undefined,
    input.status == null
      ? undefined
      : {
          status: input.status,
          statusText: "Forbidden",
          headers: {},
          config: {} as never,
          data: input.data,
        }
  )
}

describe("getUserFacingApiErrorMessage", () => {
  it("prefers response body message", () => {
    expect(
      getUserFacingApiErrorMessage(
        axiosError({
          status: 403,
          data: { message: "You do not have access to this location." },
        }),
        "fallback"
      )
    ).toBe("You do not have access to this location.")
  })

  it("maps bare 403 to permission copy", () => {
    expect(
      getUserFacingApiErrorMessage(
        axiosError({ status: 403, data: {} }),
        "fallback"
      )
    ).toBe(API_PERMISSION_DENIED_MESSAGE)
  })

  it("does not surface Axios status-code defaults", () => {
    expect(
      getUserFacingApiErrorMessage(
        axiosError({
          status: 500,
          message: "Request failed with status code 500",
          data: {},
        }),
        "Could not save."
      )
    ).toBe("Could not save.")
  })

  it("keeps non-Axios Error messages", () => {
    expect(
      getUserFacingApiErrorMessage(new Error("Soft lock"), "fallback")
    ).toBe("Soft lock")
  })

  it("uses fallback when nothing usable is present", () => {
    expect(getUserFacingApiErrorMessage({}, "Could not load.")).toBe(
      "Could not load."
    )
  })
})

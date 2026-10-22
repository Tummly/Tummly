import MockAdapter from "axios-mock-adapter"
import { afterEach, describe, expect, it } from "vitest"

import axiosInstance from "@/api/axiosInstance"
import {
  signupSessionLogin,
  SignupSessionLoginError,
} from "@/api/signupApi"

describe("signupSessionLogin", () => {
  const mock = new MockAdapter(axiosInstance)

  afterEach(() => {
    mock.reset()
  })

  it("returns the operator session from the data envelope", async () => {
    mock.onPost("/Signup/session-login").reply(200, {
      success: true,
      data: {
        token: "jwt",
        refreshToken: "refresh",
        accountType: "Multi",
        workspaceCount: 1,
        restaurantId: 9,
        selectedLocationId: 3,
        activationRequired: false,
        ownerActivation: "ok",
      },
    })

    const session = await signupSessionLogin("11111111-1111-1111-1111-111111111111")
    expect(session.token).toBe("jwt")
    expect(session.accountType).toBe("Multi")
    expect(session.selectedLocationId).toBe(3)
  })

  it("throws payment_pending on 409", async () => {
    mock.onPost("/Signup/session-login").reply(409, {
      success: false,
      code: "payment_pending",
      message: "Payment pending",
    })

    await expect(
      signupSessionLogin("11111111-1111-1111-1111-111111111111")
    ).rejects.toMatchObject({
      name: "SignupSessionLoginError",
      code: "payment_pending",
    } satisfies Partial<SignupSessionLoginError>)
  })

  it("throws session_login_used on 409", async () => {
    mock.onPost("/Signup/session-login").reply(409, {
      success: false,
      code: "session_login_used",
    })

    await expect(
      signupSessionLogin("11111111-1111-1111-1111-111111111111")
    ).rejects.toBeInstanceOf(SignupSessionLoginError)

    try {
      await signupSessionLogin("11111111-1111-1111-1111-111111111111")
    } catch (error) {
      expect(error).toBeInstanceOf(SignupSessionLoginError)
      expect((error as SignupSessionLoginError).code).toBe("session_login_used")
    }
  })
})

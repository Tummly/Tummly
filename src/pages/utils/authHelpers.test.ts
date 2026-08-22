import { afterEach, beforeEach, describe, expect, it } from "vitest"

import {
  resetAuthStore,
  useAuthStore,
} from "@/stores/authStore"
import {
  clearAuthSession,
  completeUserSession,
  DEVICE_TOKEN_KEY,
  getPostLoginDestination,
  parseTrustSkipLoginResponse,
  parseVerifyOtpResponse,
  persistAuthSession,
  persistDeviceToken,
  SELECTED_LOCATION_KEY,
} from "./authHelpers"

describe("parseVerifyOtpResponse", () => {
  it("reads activationRequired from the wrapped API envelope", () => {
    expect(
      parseVerifyOtpResponse({
        success: true,
        data: {
          token: "jwt-token",
          accountType: "Single",
          activationRequired: true,
        },
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Single",
      activationRequired: true,
    })
  })

  it("reads token and accountType from the wrapped API envelope", () => {
    expect(
      parseVerifyOtpResponse({
        success: true,
        message: "OTP verified successfully.",
        data: {
          token: "jwt-token",
          accountType: "Single",
          workspaceSetupRequired: false,
        },
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Single",
      workspaceSetupRequired: false,
    })
  })

  it("reads refreshToken from the verify-otp payload", () => {
    const parsed = parseVerifyOtpResponse({
      success: true,
      data: {
        token: "jwt-token",
        refreshToken: "refresh-token",
        accountType: "Single",
      },
    })

    expect(parsed).toEqual({
      token: "jwt-token",
      accountType: "Single",
      refreshToken: "refresh-token",
    })
  })

  it("reads PascalCase fields and deviceToken from the API envelope", () => {
    expect(
      parseVerifyOtpResponse({
        success: true,
        data: {
          Token: "jwt-token",
          AccountType: "Multi",
          WorkspaceSetupRequired: false,
          DeviceToken: "device-token",
        },
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Multi",
      workspaceSetupRequired: false,
      deviceToken: "device-token",
    })
  })

  it("reads selectedLocationId from the API envelope", () => {
    expect(
      parseVerifyOtpResponse({
        success: true,
        data: {
          token: "jwt-token",
          accountType: "Multi",
          selectedLocationId: 7,
        },
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Multi",
      selectedLocationId: 7,
    })
  })

  it("reads token and accountType from an unwrapped payload", () => {
    expect(
      parseVerifyOtpResponse({
        token: "jwt-token",
        accountType: "Multi",
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Multi",
    })
  })

  it("returns null when required fields are missing", () => {
    expect(parseVerifyOtpResponse({ success: true, data: {} })).toBeNull()
    expect(parseVerifyOtpResponse(null)).toBeNull()
  })
})

describe("parseTrustSkipLoginResponse", () => {
  it("reads a trust-skip universal-login payload", () => {
    expect(
      parseTrustSkipLoginResponse({
        loginType: "USER",
        token: "jwt-token",
        refreshToken: "refresh-token",
        accountType: "Single",
        workspaceSetupRequired: false,
      })
    ).toEqual({
      token: "jwt-token",
      refreshToken: "refresh-token",
      accountType: "Single",
      workspaceSetupRequired: false,
    })
  })

  it("reads selectedLocationId from a trust-skip payload", () => {
    expect(
      parseTrustSkipLoginResponse({
        loginType: "USER",
        token: "jwt-token",
        accountType: "Multi",
        selectedLocationId: 9,
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Multi",
      selectedLocationId: 9,
    })
  })

  it("returns null when OTP is still required", () => {
    expect(parseTrustSkipLoginResponse({ loginType: "USER" })).toBeNull()
  })

  it("returns null for admin login payloads", () => {
    expect(
      parseTrustSkipLoginResponse({
        loginType: "ADMIN",
        token: "jwt-token",
      })
    ).toBeNull()
  })
})

describe("auth session store", () => {
  beforeEach(async () => {
    resetAuthStore()
    await useAuthStore.persist.rehydrate()
  })

  afterEach(() => {
    resetAuthStore()
    localStorage.clear()
  })

  it("persists and retains device token on session clear", () => {
    persistDeviceToken("trusted-device-token")
    persistAuthSession("jwt-token", "USER")

    clearAuthSession()

    expect(useAuthStore.getState().token).toBeNull()
    expect(useAuthStore.getState().role).toBeNull()
    expect(localStorage.getItem(DEVICE_TOKEN_KEY)).toBe("trusted-device-token")
  })

  it("migrates legacy token and role keys on hydrate", async () => {
    resetAuthStore()
    localStorage.setItem("token", "legacy-jwt")
    localStorage.setItem("role", "USER")

    await useAuthStore.persist.rehydrate()

    expect(useAuthStore.getState().token).toBe("legacy-jwt")
    expect(useAuthStore.getState().role).toBe("USER")
    expect(localStorage.getItem("token")).toBeNull()
    expect(localStorage.getItem("role")).toBeNull()
  })
})

describe("completeUserSession", () => {
  beforeEach(async () => {
    resetAuthStore()
    await useAuthStore.persist.rehydrate()
  })

  afterEach(() => {
    resetAuthStore()
    localStorage.clear()
  })

  it("routes pending operators to the activation step", () => {
    expect(
      getPostLoginDestination("Single", false, null, true)
    ).toBe("/login?step=activation-code")
  })

  it("persists session, refresh token, and optional device token", () => {
    const path = completeUserSession(
      {
        token: "jwt-token",
        refreshToken: "refresh-token",
        accountType: "Single",
      },
      "trusted-device-token"
    )

    expect(path).toBe("/single-dashboard")
    expect(useAuthStore.getState().token).toBe("jwt-token")
    expect(useAuthStore.getState().refreshToken).toBe("refresh-token")
    expect(useAuthStore.getState().role).toBe("USER")
    expect(localStorage.getItem(DEVICE_TOKEN_KEY)).toBe("trusted-device-token")
  })

  it("forwards selectedLocationId to the multi-dashboard path and persists it", () => {
    const path = completeUserSession({
      token: "jwt-token",
      accountType: "Multi",
      selectedLocationId: 15,
    })

    expect(path).toBe("/multi-dashboard?location=15")
    expect(localStorage.getItem(SELECTED_LOCATION_KEY)).toBe("15")
  })
})

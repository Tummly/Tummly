import { afterEach, beforeEach, describe, expect, it } from "vitest"

import {
  resetAuthStore,
  useAuthStore,
} from "@/stores/authStore"
import {
  clearAuthSession,
  completeUserSession,
  DEVICE_TOKEN_KEY,
  getPostVerifyDashboardPath,
  parseTrustSkipLoginResponse,
  parseVerifyOtpResponse,
  persistAuthSession,
  persistDeviceToken,
} from "./authHelpers"

describe("parseVerifyOtpResponse", () => {
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
        accountType: "Single",
        workspaceSetupRequired: false,
      })
    ).toEqual({
      token: "jwt-token",
      accountType: "Single",
      workspaceSetupRequired: false,
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

  it("persists session and optional device token", () => {
    const path = completeUserSession(
      {
        token: "jwt-token",
        accountType: "Single",
      },
      "trusted-device-token"
    )

    expect(path).toBe("/single-dashboard")
    expect(useAuthStore.getState().token).toBe("jwt-token")
    expect(useAuthStore.getState().role).toBe("USER")
    expect(localStorage.getItem(DEVICE_TOKEN_KEY)).toBe("trusted-device-token")
  })
})

describe("getPostVerifyDashboardPath", () => {
  it("routes Single accounts to single-dashboard", () => {
    expect(getPostVerifyDashboardPath("Single")).toBe("/single-dashboard")
  })

  it("routes Multi accounts to multi-dashboard", () => {
    expect(getPostVerifyDashboardPath("Multi")).toBe("/multi-dashboard")
  })

  it("routes workspace setup when required", () => {
    expect(getPostVerifyDashboardPath("Multi", true)).toBe(
      "/login?step=workspace-setup"
    )
  })

  it("routes multi dashboard with selected location context", () => {
    expect(getPostVerifyDashboardPath("Multi", false)).toBe("/multi-dashboard")
  })
})

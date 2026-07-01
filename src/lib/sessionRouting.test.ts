import { AxiosError } from "axios"
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"

import { resetAuthStore, useAuthStore } from "@/stores/authStore"
import {
  getAuthenticatedLoginDestination,
  fetchCurrentUserRouting,
  getFallbackLoginDestination,
  parseCurrentUserRouting,
} from "./sessionRouting"
import { SELECTED_LOCATION_KEY } from "@/pages/utils/authHelpers"

vi.mock("@/api/loginContextClient", () => ({
  fetchCurrentUser: vi.fn(),
}))

import { fetchCurrentUser } from "@/api/loginContextClient"

describe("parseCurrentUserRouting", () => {
  it("reads routing fields from the wrapped /me response", () => {
    expect(
      parseCurrentUserRouting(
        {
          success: true,
          data: {
            role: "Owner",
            accountType: "Multi",
            selectedLocationId: 12,
            workspaceSetupRequired: false,
          },
        },
        "USER"
      )
    ).toEqual({
      role: "USER",
      accountType: "Multi",
      selectedLocationId: 12,
      workspaceSetupRequired: false,
      activationRequired: false,
      activationExpiresAt: null,
    })
  })
})

describe("fetchCurrentUserRouting", () => {
  beforeEach(async () => {
    resetAuthStore()
    await useAuthStore.persist.rehydrate()
    vi.mocked(fetchCurrentUser).mockReset()
  })

  afterEach(() => {
    resetAuthStore()
  })

  it("returns null when no token is stored", async () => {
    await expect(fetchCurrentUserRouting()).resolves.toBeNull()
    expect(fetchCurrentUser).not.toHaveBeenCalled()
  })

  it("parses a successful /me response", async () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Single")
    vi.mocked(fetchCurrentUser).mockResolvedValue({
      success: true,
      data: {
        accountType: "Multi",
        selectedLocationId: 3,
        workspaceSetupRequired: false,
      },
    })

    await expect(fetchCurrentUserRouting()).resolves.toEqual({
      role: "USER",
      accountType: "Multi",
      selectedLocationId: 3,
      workspaceSetupRequired: false,
      activationRequired: false,
      activationExpiresAt: null,
    })
  })

  it("clears session and returns null on 401", async () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Single")

    const error = new AxiosError(
      "Unauthorized",
      AxiosError.ERR_BAD_REQUEST,
      { skipAuthRedirect: true },
      undefined,
      {
        status: 401,
        statusText: "Unauthorized",
        headers: {},
        config: { headers: {} } as never,
        data: { success: false, message: "Invalid token." },
      }
    )

    vi.mocked(fetchCurrentUser).mockRejectedValue(error)

    await expect(fetchCurrentUserRouting()).resolves.toBeNull()
    expect(useAuthStore.getState().token).toBeNull()
  })
})

describe("getAuthenticatedLoginDestination", () => {
  beforeEach(() => {
    localStorage.clear()
    resetAuthStore()
  })

  afterEach(() => {
    localStorage.clear()
    resetAuthStore()
  })

  it("routes admins to the admin dashboard", () => {
    expect(
      getAuthenticatedLoginDestination({
        role: "ADMIN",
        accountType: "Single",
        selectedLocationId: null,
        workspaceSetupRequired: false,
        activationRequired: false,
        activationExpiresAt: null,
      })
    ).toBe("/admin-dashboard")
  })

  it("routes pending operators to activation", () => {
    expect(
      getAuthenticatedLoginDestination({
        role: "USER",
        accountType: "Single",
        selectedLocationId: null,
        workspaceSetupRequired: false,
        activationRequired: true,
        activationExpiresAt: null,
      })
    ).toBe("/login?step=activation-code")
  })

  it("routes multi users without a workspace to setup", () => {
    expect(
      getAuthenticatedLoginDestination({
        role: "USER",
        accountType: "Multi",
        selectedLocationId: null,
        workspaceSetupRequired: true,
        activationRequired: false,
        activationExpiresAt: null,
      })
    ).toBe("/login?step=workspace-setup")
  })

  it("routes single users to the single dashboard", () => {
    expect(
      getAuthenticatedLoginDestination({
        role: "USER",
        accountType: "Single",
        selectedLocationId: null,
        workspaceSetupRequired: false,
        activationRequired: false,
        activationExpiresAt: null,
      })
    ).toBe("/single-dashboard")
  })

  it("persists selectedLocationId and routes multi users to the location dashboard", () => {
    expect(
      getAuthenticatedLoginDestination({
        role: "USER",
        accountType: "Multi",
        selectedLocationId: 42,
        workspaceSetupRequired: false,
        activationRequired: false,
        activationExpiresAt: null,
      })
    ).toBe("/multi-dashboard?location=42")

    expect(localStorage.getItem(SELECTED_LOCATION_KEY)).toBe("42")
  })

  it("routes fallback destinations to activation when activation is persisted", () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Single")
    localStorage.setItem("activationRequired", "true")

    expect(getFallbackLoginDestination()).toBe("/login?step=activation-code")
  })
})

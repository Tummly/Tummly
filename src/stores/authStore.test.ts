import { afterEach, beforeEach, describe, expect, it } from "vitest"

import {
  getAuthToken,
  hasAuthSession,
  resetAuthStore,
  useAuthStore,
} from "./authStore"

describe("useAuthStore", () => {
  beforeEach(async () => {
    resetAuthStore()
    await useAuthStore.persist.rehydrate()
  })

  afterEach(() => {
    resetAuthStore()
    localStorage.clear()
  })

  it("stores token and role via setSession", () => {
    useAuthStore.getState().setSession("jwt-token", "ADMIN")

    expect(useAuthStore.getState().token).toBe("jwt-token")
    expect(useAuthStore.getState().role).toBe("ADMIN")
    expect(useAuthStore.getState().accountType).toBeNull()
    expect(useAuthStore.getState().refreshToken).toBeNull()
    expect(getAuthToken()).toBe("jwt-token")
    expect(hasAuthSession()).toBe(true)
  })

  it("stores accountType when provided", () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Single")

    expect(useAuthStore.getState().accountType).toBe("Single")
  })

  it("stores a refresh token without dropping it on later token-only updates", () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Single", "refresh-token")
    useAuthStore.getState().setSession("jwt-token-2", "USER", "Multi")

    expect(useAuthStore.getState().token).toBe("jwt-token-2")
    expect(useAuthStore.getState().refreshToken).toBe("refresh-token")
  })

  it("clears session via clearSession", () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Multi", "refresh-token")
    useAuthStore.getState().clearSession()

    expect(useAuthStore.getState().token).toBeNull()
    expect(useAuthStore.getState().refreshToken).toBeNull()
    expect(useAuthStore.getState().role).toBeNull()
    expect(useAuthStore.getState().accountType).toBeNull()
    expect(hasAuthSession()).toBe(false)
  })

  it("persists session under tummly-auth", () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Multi", "refresh-token")

    const raw = localStorage.getItem("tummly-auth")
    expect(raw).toBeTruthy()

    const parsed = JSON.parse(raw!) as {
      state: {
        token: string
        role: string
        accountType: string
        refreshToken: string
      }
    }

    expect(parsed.state.token).toBe("jwt-token")
    expect(parsed.state.role).toBe("USER")
    expect(parsed.state.accountType).toBe("Multi")
    expect(parsed.state.refreshToken).toBe("refresh-token")
  })
})

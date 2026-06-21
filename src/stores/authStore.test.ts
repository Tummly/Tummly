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
    expect(getAuthToken()).toBe("jwt-token")
    expect(hasAuthSession()).toBe(true)
  })

  it("stores accountType when provided", () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Single")

    expect(useAuthStore.getState().accountType).toBe("Single")
  })

  it("clears session via clearSession", () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Multi")
    useAuthStore.getState().clearSession()

    expect(useAuthStore.getState().token).toBeNull()
    expect(useAuthStore.getState().role).toBeNull()
    expect(useAuthStore.getState().accountType).toBeNull()
    expect(hasAuthSession()).toBe(false)
  })

  it("persists session under tummly-auth", () => {
    useAuthStore.getState().setSession("jwt-token", "USER", "Multi")

    const raw = localStorage.getItem("tummly-auth")
    expect(raw).toBeTruthy()

    const parsed = JSON.parse(raw!) as {
      state: { token: string; role: string; accountType: string }
    }

    expect(parsed.state.token).toBe("jwt-token")
    expect(parsed.state.role).toBe("USER")
    expect(parsed.state.accountType).toBe("Multi")
  })
})

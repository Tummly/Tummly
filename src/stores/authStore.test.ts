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
    expect(getAuthToken()).toBe("jwt-token")
    expect(hasAuthSession()).toBe(true)
  })

  it("clears session via clearSession", () => {
    useAuthStore.getState().setSession("jwt-token", "USER")
    useAuthStore.getState().clearSession()

    expect(useAuthStore.getState().token).toBeNull()
    expect(useAuthStore.getState().role).toBeNull()
    expect(hasAuthSession()).toBe(false)
  })

  it("persists session under tummly-auth", () => {
    useAuthStore.getState().setSession("jwt-token", "USER")

    const raw = localStorage.getItem("tummly-auth")
    expect(raw).toBeTruthy()

    const parsed = JSON.parse(raw!) as {
      state: { token: string; role: string }
    }

    expect(parsed.state.token).toBe("jwt-token")
    expect(parsed.state.role).toBe("USER")
  })
})

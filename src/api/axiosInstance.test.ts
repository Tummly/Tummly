import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"
import MockAdapter from "axios-mock-adapter"

import { resetAuthStore, useAuthStore } from "@/stores/authStore"
import axiosInstance from "./axiosInstance"

describe("axiosInstance 401 interceptor", () => {
  let mock: MockAdapter
  let locationHref: string

  beforeEach(async () => {
    resetAuthStore()
    await useAuthStore.persist.rehydrate()
    useAuthStore.getState().setSession("jwt-token", "USER", "Single")

    mock = new MockAdapter(axiosInstance)
    locationHref = ""
    vi.stubGlobal("window", {
      location: {
        get href() {
          return locationHref
        },
        set href(value: string) {
          locationHref = value
        },
      },
    })
  })

  afterEach(() => {
    mock.restore()
    resetAuthStore()
    vi.unstubAllGlobals()
  })

  it("clears session and redirects to /login on 401 by default", async () => {
    mock.onGet("/dashboard-test").reply(401)

    await expect(axiosInstance.get("/dashboard-test")).rejects.toThrow()

    expect(useAuthStore.getState().token).toBeNull()
    expect(locationHref).toBe("/login")
  })

  it("does not redirect when skipAuthRedirect is set", async () => {
    mock.onGet("/auth/me").reply(401)

    await expect(
      axiosInstance.get("/auth/me", { skipAuthRedirect: true })
    ).rejects.toThrow()

    expect(useAuthStore.getState().token).toBe("jwt-token")
    expect(locationHref).toBe("")
  })
})

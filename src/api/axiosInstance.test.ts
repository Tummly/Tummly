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
    expect(
      mock.history.post.filter((request) =>
        request.url?.includes("/auth/refresh")
      )
    ).toHaveLength(0)
  })

  it("renews the session via refresh instead of logging out on 401", async () => {
    useAuthStore.getState().setSession(
      "jwt-token",
      "USER",
      "Single",
      "refresh-token"
    )
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({
          success: true,
          data: {
            token: "new-jwt",
            refreshToken: "next-refresh",
          },
        }),
      })
    )
    mock.onGet("/dashboard-test").replyOnce(401)
    mock.onGet("/dashboard-test").reply(200, { ok: true })

    const response = await axiosInstance.get("/dashboard-test")

    expect(response.status).toBe(200)
    expect(response.data).toEqual({ ok: true })
    expect(useAuthStore.getState().token).toBe("new-jwt")
    expect(useAuthStore.getState().refreshToken).toBe("next-refresh")
    expect(locationHref).toBe("")
    expect(fetch).toHaveBeenCalledWith(
      expect.stringContaining("/auth/refresh"),
      expect.objectContaining({
        method: "POST",
      })
    )
  })

  it("clears session when refresh fails after 401", async () => {
    useAuthStore.getState().setSession(
      "jwt-token",
      "USER",
      "Single",
      "refresh-token"
    )
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        json: async () => ({
          success: false,
          message: "Invalid refresh token.",
        }),
      })
    )
    mock.onGet("/dashboard-test").reply(401)

    await expect(axiosInstance.get("/dashboard-test")).rejects.toThrow()

    expect(useAuthStore.getState().token).toBeNull()
    expect(useAuthStore.getState().refreshToken).toBeNull()
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

  it("clears session and redirects to /login on activation-expired 403", async () => {
    mock.onGet("/dashboard-test").reply(403, {
      success: false,
      activationExpired: true,
      message: "Your 30 day free trial is over",
    })

    await expect(axiosInstance.get("/dashboard-test")).rejects.toThrow()

    expect(useAuthStore.getState().token).toBeNull()
    expect(locationHref).toBe("/login")
  })

  it("redirects to activation step on activationRequired 403 without clearing session", async () => {
    mock.onGet("/dashboard-test").reply(403, {
      success: false,
      activationRequired: true,
      message: "Account activation is required before accessing this resource.",
    })

    await expect(axiosInstance.get("/dashboard-test")).rejects.toThrow()

    expect(useAuthStore.getState().token).toBe("jwt-token")
    expect(locationHref).toBe("/login?step=activation-code")
  })
})

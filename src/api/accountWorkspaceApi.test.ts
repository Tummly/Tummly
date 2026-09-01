import MockAdapter from "axios-mock-adapter"
import { afterEach, beforeEach, describe, expect, it } from "vitest"

import axiosInstance from "./axiosInstance"
import {
  exportAccountWorkspaceGuestData,
  updateAccountWorkspaceDetails,
} from "./accountWorkspaceApi"

describe("exportAccountWorkspaceGuestData", () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(axiosInstance)
  })

  afterEach(() => {
    mock.restore()
  })

  it("downloads the restaurant guest-data file for the chosen format", async () => {
    mock.onGet("/account-workspace/guest-data-export").reply((config) => {
      expect(config.params).toEqual({ format: "csv" })
      return [
        200,
        new Blob(["csv"]),
        {
          "content-disposition":
            'attachment; filename="tummly-guest-data-9-20260824-120000Z.csv"',
        },
      ]
    })

    await expect(
      exportAccountWorkspaceGuestData("csv")
    ).resolves.toMatchObject({
      filename: "tummly-guest-data-9-20260824-120000Z.csv",
    })
  })
})

describe("updateAccountWorkspaceDetails", () => {
  let mock: MockAdapter

  beforeEach(() => {
    mock = new MockAdapter(axiosInstance)
  })

  afterEach(() => {
    mock.restore()
  })

  it("sends multipart FormData and clears the default JSON Content-Type", async () => {
    mock.onPut("/account-workspace/account-details").reply((config) => {
      expect(config.data).toBeInstanceOf(FormData)
      const form = config.data as FormData
      expect(form.get("name")).toBe("Prefill Workspace")
      expect(form.get("logo")).toBeInstanceOf(File)

      const headers = config.headers as {
        get?: (name: string) => unknown
        ["Content-Type"]?: unknown
        ["content-type"]?: unknown
      }
      const contentType =
        (typeof headers.get === "function"
          ? headers.get("Content-Type")
          : null)
        ?? headers["Content-Type"]
        ?? headers["content-type"]
        ?? null
      expect(contentType).not.toBe("application/json")

      return [
        200,
        {
          success: true,
          workspaceName: "Prefill Workspace",
          guestFacingBusinessName: "Main",
          accountStructure: "Single",
          businessCategory: null,
          businessCategoryLabel: null,
          mainOperatingCountry: "United Kingdom",
          brandLogoOperatorUrl: "/api/account-workspace/brand-logo",
          brandLogoPublicUrl: "/api/public/brand-logos/x.png",
          lastSavedAt: "2026-08-31T00:00:00Z",
          isAccountOwner: true,
          restaurantId: 1,
          status: {
            workspaceStatus: "Active",
            planStatus: "Active",
            billingStatus: "Ok",
            accountCreatedAt: "2026-01-01T00:00:00Z",
            activeLocations: 1,
            teamMembers: 1,
            guestProfiles: 0,
            guestFormStatus: "Live",
            lastAccountUpdateAt: "2026-08-31T00:00:00Z",
          },
        },
      ]
    })

    const logo = new File([new Uint8Array([1, 2, 3])], "logo.png", {
      type: "image/png",
    })

    await expect(
      updateAccountWorkspaceDetails({
        name: "Prefill Workspace",
        logo,
      })
    ).resolves.toMatchObject({
      workspaceName: "Prefill Workspace",
    })
  })
})

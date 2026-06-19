import { afterEach, beforeEach, describe, expect, it } from "vitest"

import {
  getAuthenticatedLoginDestination,
  parseCurrentUserRouting,
} from "./sessionRouting"
import { SELECTED_LOCATION_KEY } from "@/pages/utils/authHelpers"

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
    })
  })
})

describe("getAuthenticatedLoginDestination", () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    localStorage.clear()
  })

  it("routes admins to the admin dashboard", () => {
    expect(
      getAuthenticatedLoginDestination({
        role: "ADMIN",
        accountType: "Single",
        selectedLocationId: null,
        workspaceSetupRequired: false,
      })
    ).toBe("/admin-dashboard")
  })

  it("routes multi users without a workspace to setup", () => {
    expect(
      getAuthenticatedLoginDestination({
        role: "USER",
        accountType: "Multi",
        selectedLocationId: null,
        workspaceSetupRequired: true,
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
      })
    ).toBe("/multi-dashboard?location=42")

    expect(localStorage.getItem(SELECTED_LOCATION_KEY)).toBe("42")
  })
})

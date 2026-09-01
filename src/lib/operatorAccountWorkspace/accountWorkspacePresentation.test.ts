import { describe, expect, it } from "vitest"

import {
  resolveAccountWorkspacePlanStatusPresentation,
  resolveCampaignSenderDisplayName,
} from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"

describe("resolveCampaignSenderDisplayName", () => {
  it("prefers the stored sender name", () => {
    expect(
      resolveCampaignSenderDisplayName({
        storedSenderName: " Harbour Kitchen ",
        workspaceName: "Workspace",
        locationName: "Location",
      })
    ).toBe("Harbour Kitchen")
  })

  it("falls back to workspace name when sender is unset", () => {
    expect(
      resolveCampaignSenderDisplayName({
        storedSenderName: "",
        workspaceName: " KFC Chicken ",
        locationName: "Location",
      })
    ).toBe("KFC Chicken")
  })

  it("falls back to location name when workspace name is empty", () => {
    expect(
      resolveCampaignSenderDisplayName({
        storedSenderName: null,
        workspaceName: "   ",
        locationName: " Soho ",
      })
    ).toBe("Soho")
  })
})

describe("resolveAccountWorkspacePlanStatusPresentation", () => {
  const href =
    "/multi-dashboard/settings/billing-credits?location=7&tab=plan-subscription"

  it("links Plan status when the operator may open Billing & credits tabs", () => {
    expect(
      resolveAccountWorkspacePlanStatusPresentation({
        planStatus: "Growth",
        billingCreditsAccess: "view",
        planSubscriptionHref: href,
      })
    ).toEqual({
      kind: "link",
      label: "Growth",
      href,
    })

    expect(
      resolveAccountWorkspacePlanStatusPresentation({
        planStatus: "Pilot",
        billingCreditsAccess: "manage",
        planSubscriptionHref: href,
      })
    ).toEqual({
      kind: "link",
      label: "Pilot",
      href,
    })
  })

  it("shows plain text Plan status when the operator has No access", () => {
    expect(
      resolveAccountWorkspacePlanStatusPresentation({
        planStatus: "Starter",
        billingCreditsAccess: "none",
        planSubscriptionHref: href,
      })
    ).toEqual({
      kind: "text",
      label: "Starter",
    })
  })
})

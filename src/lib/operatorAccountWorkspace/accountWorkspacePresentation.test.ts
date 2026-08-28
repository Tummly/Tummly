import { describe, expect, it } from "vitest"

import { resolveAccountWorkspacePlanStatusPresentation } from "@/lib/operatorAccountWorkspace/accountWorkspacePresentation"

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

import { describe, expect, it } from "vitest"

import type { AdminEnvironment } from "@/types/adminOps"

import { environmentBannerLabel } from "./environmentBanner"

function environment(overrides: Partial<AdminEnvironment> = {}): AdminEnvironment {
  return {
    environmentName: "QA",
    isProduction: false,
    commitSha: "abc1234",
    apiStartedAtUtc: "2026-10-28T08:00:00Z",
    latestMigrationId: null,
    revolut: { status: "not_configured", mode: "unset" },
    resend: { status: "not_configured", senderDomain: null },
    twilio: { status: "not_configured", fromNumberMasked: null },
    vatModeActive: false,
    ...overrides,
  }
}

describe("environmentBannerLabel", () => {
  it("shows the API environment name on QA", () => {
    expect(environmentBannerLabel(environment(), null)).toBe("QA")
  })

  it("never shows a banner when the API reports production", () => {
    expect(
      environmentBannerLabel(
        environment({ environmentName: "Production", isProduction: true }),
        "QA"
      )
    ).toBeNull()
  })

  it("falls back to the build environment when the API environment is unavailable", () => {
    expect(environmentBannerLabel(null, "Development")).toBe("Development")
  })

  it("shows nothing when neither source names a non-production environment", () => {
    expect(environmentBannerLabel(null, null)).toBeNull()
  })
})

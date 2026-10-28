import { describe, expect, it, beforeEach } from "vitest"

import { encodeCustomPlanIntentV2 } from "@/lib/marketingCustomPlan/customPlanEstimate"
import {
  activeCustomConfig,
  buildSignupPath,
  captureSignupPlanIntentFromSearch,
  clearSignupPlanIntent,
  parseSignupPlanIntent,
  readSignupPlanIntent,
  saveSignupCustomPlanConfig,
  saveSignupPlanIntent,
  signupStepAfterLocation,
} from "@/lib/signupPlanIntent"

describe("buildSignupPath", () => {
  it("returns plain signup when no intent", () => {
    expect(buildSignupPath()).toBe("/signup")
    expect(buildSignupPath(null)).toBe("/signup")
  })

  it("builds Pilot path without cadence", () => {
    expect(buildSignupPath({ plan: "Pilot" })).toBe("/signup?plan=Pilot")
  })

  it("builds paid path with cadence", () => {
    expect(
      buildSignupPath({ plan: "Starter", cadence: "annual" }),
    ).toBe("/signup?plan=Starter&cadence=annual")
  })

  it("appends email for marketing CTA handoff", () => {
    expect(
      buildSignupPath({ plan: "Pilot" }, { email: " ops@example.com " }),
    ).toBe("/signup?plan=Pilot&email=ops%40example.com")
    expect(buildSignupPath(null, { email: "ops@example.com" })).toBe(
      "/signup?email=ops%40example.com",
    )
  })

  it("includes v2 customIntent when config is present", () => {
    const path = buildSignupPath({
      plan: "Custom",
      cadence: "monthly",
      config: {
        version: 2,
        cadence: "monthly",
        basePlan: "growth",
        locations: 3,
        packs: { offer: 1 },
      },
    })
    expect(path).toContain("plan=Custom")
    expect(path).toContain("customIntent=")
  })
})

describe("parseSignupPlanIntent", () => {
  it("returns null when plan missing", () => {
    expect(parseSignupPlanIntent(new URLSearchParams())).toBeNull()
  })

  it("parses Pilot", () => {
    expect(
      parseSignupPlanIntent(new URLSearchParams("plan=Pilot")),
    ).toEqual({ plan: "Pilot" })
  })

  it("defaults paid cadence to monthly", () => {
    expect(
      parseSignupPlanIntent(new URLSearchParams("plan=Growth")),
    ).toEqual({ plan: "Growth", cadence: "monthly" })
  })

  it("decodes v2 customIntent into config", () => {
    const customIntent = encodeCustomPlanIntentV2({
      version: 2,
      cadence: "monthly",
      basePlan: "growth",
      locations: 3,
      packs: { offer: 1 },
    })
    const parsed = parseSignupPlanIntent(
      new URLSearchParams(
        `plan=Custom&cadence=monthly&customIntent=${encodeURIComponent(customIntent)}`,
      ),
    )
    expect(parsed?.plan).toBe("Custom")
    expect(parsed?.config?.basePlan).toBe("growth")
    expect(parsed?.config?.locations).toBe(3)
    expect(parsed?.config?.packs.offer).toBe(1)
  })
})

describe("signupPlanIntent session", () => {
  beforeEach(() => {
    clearSignupPlanIntent()
  })

  it("drops a stored config with a replaced capacity pack and flags it", () => {
    saveSignupPlanIntent({
      plan: "Custom",
      cadence: "monthly",
      config: {
        version: 2,
        cadence: "monthly",
        basePlan: "starter",
        locations: 1,
        packs: { team_users_5: 1 } as never,
      },
    })
    const read = readSignupPlanIntent()
    expect(read?.config).toBeUndefined()
    expect(read?.capacityOptionRetired).toBe(true)
  })

  it("flags a signup URL whose saved selection names a replaced capacity pack", () => {
    const legacy = btoa(
      JSON.stringify({
        version: 2,
        cadence: "monthly",
        basePlan: "starter",
        locations: 1,
        packs: { offers_5: 1 },
      }),
    )
    const parsed = parseSignupPlanIntent(
      new URLSearchParams({ plan: "Custom", customIntent: legacy }),
    )
    expect(parsed?.config).toBeUndefined()
    expect(parsed?.capacityOptionRetired).toBe(true)
  })

  it("round-trips intent in sessionStorage", () => {
    saveSignupPlanIntent({ plan: "Group", cadence: "annual" })
    expect(readSignupPlanIntent()).toEqual({
      plan: "Group",
      cadence: "annual",
    })
  })

  it("round-trips v2 config through sessionStorage", () => {
    saveSignupCustomPlanConfig({
      basePlan: "starter",
      cadence: "monthly",
      locations: 1,
      packs: { email_20k: 2 },
      asCustom: true,
    })
    const read = readSignupPlanIntent()
    expect(read?.plan).toBe("Custom")
    expect(read?.config).toMatchObject({
      version: 2,
      basePlan: "starter",
      locations: 1,
      packs: expect.objectContaining({ email_20k: 2 }),
    })
  })

  it("keeps the saved intent when signup URL has no plan (OAuth error, different email)", () => {
    const config = {
      version: 2 as const,
      cadence: "monthly" as const,
      basePlan: "starter" as const,
      locations: 1,
      packs: { offer: 1 },
    }
    saveSignupPlanIntent({ plan: "Custom", cadence: "monthly", config })
    expect(
      captureSignupPlanIntentFromSearch(new URLSearchParams("oauthError=x"))?.config,
    ).toEqual(config)
    expect(readSignupPlanIntent()?.config).toEqual(config)
  })
})

describe("quoteHash and invalid counts", () => {
  beforeEach(() => {
    clearSignupPlanIntent()
  })

  it("drops the config when the Location count is 0", () => {
    saveSignupPlanIntent({
      plan: "Custom",
      cadence: "monthly",
      config: {
        version: 2,
        cadence: "monthly",
        basePlan: "starter",
        locations: 0,
        packs: {},
      },
    })
    expect(readSignupPlanIntent()).toEqual({ plan: "Custom", cadence: "monthly" })
  })

  it("keeps quoteHash through encode and decode", () => {
    const customIntent = encodeCustomPlanIntentV2({
      version: 2,
      cadence: "monthly",
      basePlan: "growth",
      locations: 3,
      packs: {},
      quoteHash: "abc",
    })
    const parsed = parseSignupPlanIntent(
      new URLSearchParams(
        `plan=Custom&cadence=monthly&customIntent=${encodeURIComponent(customIntent)}`,
      ),
    )
    expect(parsed?.config?.quoteHash).toBe("abc")
  })

  it("keeps quoteHash through sessionStorage", () => {
    saveSignupCustomPlanConfig({
      basePlan: "growth",
      cadence: "monthly",
      locations: 3,
      packs: {},
      asCustom: true,
      quoteHash: "abc",
    })
    expect(readSignupPlanIntent()?.config?.quoteHash).toBe("abc")
  })
})

describe("invalid Location count in a shared link", () => {
  beforeEach(() => {
    clearSignupPlanIntent()
  })

  function captureRaw(json: string) {
    return captureSignupPlanIntentFromSearch(
      new URLSearchParams(
        `plan=Custom&cadence=monthly&customIntent=${encodeURIComponent(json)}`,
      ),
    )
  }

  it("drops the config when locations is missing and keeps the plan", () => {
    const json = JSON.stringify({
      version: 2,
      cadence: "monthly",
      basePlan: "growth",
      packs: {},
    })
    expect(captureRaw(json)?.plan).toBe("Custom")
    expect(captureRaw(json)?.config).toBeUndefined()
    expect(readSignupPlanIntent()?.plan).toBe("Custom")
    expect(readSignupPlanIntent()?.config).toBeUndefined()
  })

  it("drops the config when locations is a string", () => {
    const json = JSON.stringify({
      version: 2,
      cadence: "monthly",
      basePlan: "growth",
      locations: "3",
      packs: {},
    })
    expect(captureRaw(json)?.plan).toBe("Custom")
    expect(captureRaw(json)?.config).toBeUndefined()
  })

  it("drops the config when locations is fractional and keeps the plan", () => {
    const json = JSON.stringify({
      version: 2,
      cadence: "monthly",
      basePlan: "growth",
      locations: 2.5,
      packs: {},
    })
    expect(captureRaw(json)?.plan).toBe("Custom")
    expect(captureRaw(json)?.config).toBeUndefined()
    expect(readSignupPlanIntent()?.plan).toBe("Custom")
    expect(readSignupPlanIntent()?.config).toBeUndefined()
  })
})

describe("activeCustomConfig", () => {
  const config = {
    version: 2 as const,
    cadence: "monthly" as const,
    basePlan: "starter" as const,
    locations: 1,
    packs: { sms_1000: 1 },
  }

  it("applies to Custom and to the card on its base plan", () => {
    expect(activeCustomConfig({ plan: "Custom", config })).toEqual(config)
    expect(activeCustomConfig({ plan: "Starter", config })).toEqual(config)
  })

  it("is ignored (but kept) while another card is selected", () => {
    expect(activeCustomConfig({ plan: "Growth", config })).toBeNull()
    expect(activeCustomConfig({ plan: "Pilot" })).toBeNull()
  })
})

describe("signupStepAfterLocation", () => {
  const config = {
    version: 2 as const,
    cadence: "monthly" as const,
    basePlan: "group" as const,
    locations: 8,
    packs: { team_user: 2 },
  }

  it("goes to review when the plan was built on the Custom plan page", () => {
    expect(signupStepAfterLocation({ plan: "Group", config })).toBe("review")
    expect(signupStepAfterLocation({ plan: "Custom", config })).toBe("review")
  })

  it("goes to choose a plan for a standard plan or no plan", () => {
    expect(signupStepAfterLocation({ plan: "Starter" })).toBe("choosePlan")
    expect(signupStepAfterLocation({ plan: "Pilot" })).toBe("choosePlan")
    expect(signupStepAfterLocation(null)).toBe("choosePlan")
  })
})

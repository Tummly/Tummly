import { describe, expect, it } from "vitest"
import { mapSignupCardToPlanId } from "./signupPlanMap"

describe("signupPlanMap", () => {
  it("maps essential to Pilot and pro to Growth", () => {
    expect(mapSignupCardToPlanId("essential")).toBe("Pilot")
    expect(mapSignupCardToPlanId("pro")).toBe("Growth")
  })
})

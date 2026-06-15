import { describe, expect, it } from "vitest"

import { maskEmailForDisplay } from "./forgotPasswordFlow"

describe("maskEmailForDisplay", () => {
  it("masks the local part for the check-your-email screen", () => {
    expect(maskEmailForDisplay("owner@restaurant.com")).toBe(
      "o••••@restaurant.com"
    )
  })
})

import { describe, expect, it } from "vitest"

import {
  normalizeActivationCodeInput,
  signInActivationCodeSchema,
} from "@/schemas/signInActivation"

describe("signInActivationCodeSchema", () => {
  it("accepts codes with or without a dash", () => {
    expect(
      signInActivationCodeSchema.safeParse({ activationCode: "ABCD-2345" })
        .success
    ).toBe(true)
    expect(
      signInActivationCodeSchema.safeParse({ activationCode: "abcd2345" })
        .success
    ).toBe(true)
  })

  it("rejects ambiguous characters and short codes", () => {
    expect(
      signInActivationCodeSchema.safeParse({ activationCode: "ABCD1234" })
        .success
    ).toBe(false)
    expect(
      signInActivationCodeSchema.safeParse({ activationCode: "ABCD" }).success
    ).toBe(false)
  })
})

describe("normalizeActivationCodeInput", () => {
  it("uppercases and strips invalid characters", () => {
    expect(normalizeActivationCodeInput(" ab-cd23 ")).toBe("AB-CD23")
  })
})

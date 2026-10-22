import { describe, expect, it } from "vitest"
import {
  joinSignupFullName,
  signupAccountStepSchema,
  signupAccountStepSocialSchema,
  toSignupOnboardingPayload,
  signupOnboardingDefaultValues,
} from "./signupOnboarding"

describe("joinSignupFullName", () => {
  it("joins trimmed first and last", () => {
    expect(joinSignupFullName("  Ada ", " Lovelace ")).toBe("Ada Lovelace")
  })
})

describe("signupAccountStepSchema", () => {
  it("rejects empty first name", () => {
    const result = signupAccountStepSchema.safeParse({
      email: "a@b.com",
      firstName: "",
      lastName: "L",
      password: "Password1!",
      confirmPassword: "Password1!",
    })
    expect(result.success).toBe(false)
  })
})

describe("signupAccountStepSocialSchema", () => {
  it("accepts email and names without password", () => {
    const result = signupAccountStepSocialSchema.safeParse({
      email: "social@example.com",
      firstName: "Ada",
      lastName: "Lovelace",
    })
    expect(result.success).toBe(true)
  })

  it("rejects empty first name", () => {
    const result = signupAccountStepSocialSchema.safeParse({
      email: "social@example.com",
      firstName: "",
      lastName: "Lovelace",
    })
    expect(result.success).toBe(false)
  })
})

describe("toSignupOnboardingPayload", () => {
  it("maps one location and joined fullName", () => {
    const payload = toSignupOnboardingPayload({
      ...signupOnboardingDefaultValues,
      token: "t",
      email: "a@b.com",
      firstName: "Ada",
      lastName: "Lovelace",
      password: "Password1!",
      confirmPassword: "Password1!",
      restaurantName: "Cafe",
      businessCategory: "cafe",
      locationName: "Main",
      address: "1 High St",
      city: "London",
      postcode: "SW1A 1AA",
    })
    expect(payload.fullName).toBe("Ada Lovelace")
    expect(payload.locations).toHaveLength(1)
    expect(payload).not.toHaveProperty("country")
  })

  it("allows empty password when isSocial", () => {
    const payload = toSignupOnboardingPayload(
      {
        ...signupOnboardingDefaultValues,
        token: "t",
        email: "social@example.com",
        firstName: "Ada",
        lastName: "Lovelace",
        password: "",
        confirmPassword: "",
        restaurantName: "Cafe",
        businessCategory: "cafe",
        locationName: "Main",
        address: "1 High St",
        city: "London",
        postcode: "SW1A 1AA",
      },
      { isSocial: true }
    )
    expect(payload.password).toBe("")
    expect(payload.confirmPassword).toBe("")
    expect(payload.fullName).toBe("Ada Lovelace")
  })
})

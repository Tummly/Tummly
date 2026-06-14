import { describe, expect, it } from "vitest"

import { validationMessages } from "@/schemas/messages"
import {
  toTrialRequestPayload,
  trialRequestDefaultValues,
  trialRequestSchema,
} from "@/schemas/trialRequest"

const validTrialRequest = {
  ...trialRequestDefaultValues,
  businessName: "The Golden Fork",
  businessCategory: "cafe",
  locations: "1",
  businessLink: "https://example.com/menu",
  fullName: "Alex Operator",
  email: "alex@example.com",
  mobile: "+44 7700 900123",
  role: "owner-operator",
  goal: "collect-feedback",
  termsAccepted: true,
}

describe("trialRequestSchema", () => {
  it("accepts a valid trial request", () => {
    const result = trialRequestSchema.safeParse(validTrialRequest)
    expect(result.success).toBe(true)
  })

  it("accepts an empty optional business link", () => {
    const result = trialRequestSchema.safeParse({
      ...validTrialRequest,
      businessLink: "",
    })
    expect(result.success).toBe(true)
  })

  it("rejects a missing business name", () => {
    const result = trialRequestSchema.safeParse({
      ...validTrialRequest,
      businessName: "",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.trialRequest.businessName.required
      )
    }
  })

  it("rejects a non-https business link", () => {
    const result = trialRequestSchema.safeParse({
      ...validTrialRequest,
      businessLink: "http://example.com",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.url.invalid
      )
    }
  })

  it("rejects an invalid email", () => {
    const result = trialRequestSchema.safeParse({
      ...validTrialRequest,
      email: "not-an-email",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues.some(
        (issue) => issue.message === validationMessages.email.invalid
      )).toBe(true)
    }
  })

  it("rejects unchecked terms", () => {
    const result = trialRequestSchema.safeParse({
      ...validTrialRequest,
      termsAccepted: false,
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const termsIssue = result.error.issues.find(
        (issue) => issue.path[0] === "termsAccepted"
      )
      expect(termsIssue?.message).toBe(
        validationMessages.trialRequest.terms.required
      )
    }
  })
})

describe("toTrialRequestPayload", () => {
  it("maps parsed form values to the trial API DTO shape", () => {
    const payload = toTrialRequestPayload({
      ...validTrialRequest,
      businessName: "  The Golden Fork  ",
      businessLink: "",
      email: "  Alex@Example.COM ",
      mobile: "  +44 7700 900123 ",
    })

    expect(payload).toEqual({
      businessName: "The Golden Fork",
      businessCategory: "cafe",
      locations: "1",
      businessLink: undefined,
      fullName: "Alex Operator",
      email: "alex@example.com",
      mobile: "+44 7700 900123",
      role: "owner-operator",
      goal: "collect-feedback",
      termsAccepted: true,
    })
  })
})

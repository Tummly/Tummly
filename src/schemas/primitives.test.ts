import { describe, expect, it } from "vitest"

import {
  emailSchema,
  mobileSchema,
  optionalUrlSchema,
  otpSchema,
  passwordPairSchema,
  passwordSchema,
} from "@/schemas/primitives"
import { validationMessages } from "@/schemas/messages"

describe("emailSchema", () => {
  it("accepts a valid email", () => {
    const result = emailSchema.safeParse("operator@example.com")
    expect(result.success).toBe(true)
  })

  it("rejects an empty value", () => {
    const result = emailSchema.safeParse("")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.email.required
      )
    }
  })

  it("rejects a malformed email", () => {
    const result = emailSchema.safeParse("not-an-email")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues.some(
        (issue) => issue.message === validationMessages.email.invalid
      )).toBe(true)
    }
  })
})

describe("passwordSchema", () => {
  it("accepts a password with at least 8 characters", () => {
    const result = passwordSchema.safeParse("secure-pass")
    expect(result.success).toBe(true)
  })

  it("rejects an empty password", () => {
    const result = passwordSchema.safeParse("")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.password.required
      )
    }
  })

  it("rejects a password shorter than 8 characters", () => {
    const result = passwordSchema.safeParse("short")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues.some(
        (issue) => issue.message === validationMessages.password.minLength
      )).toBe(true)
    }
  })
})

describe("passwordPairSchema", () => {
  it("accepts matching passwords", () => {
    const result = passwordPairSchema.safeParse({
      password: "secure-pass",
      confirmPassword: "secure-pass",
    })
    expect(result.success).toBe(true)
  })

  it("rejects mismatched passwords on confirmPassword", () => {
    const result = passwordPairSchema.safeParse({
      password: "secure-pass",
      confirmPassword: "different-pass",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      const confirmIssue = result.error.issues.find(
        (issue) => issue.path[0] === "confirmPassword"
      )
      expect(confirmIssue?.message).toBe(validationMessages.password.mismatch)
    }
  })
})

describe("mobileSchema", () => {
  it("accepts a valid mobile number", () => {
    const result = mobileSchema.safeParse("+44 7700 900123")
    expect(result.success).toBe(true)
  })

  it("rejects an empty mobile number", () => {
    const result = mobileSchema.safeParse("")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.mobile.required
      )
    }
  })

  it("rejects an invalid mobile number", () => {
    const result = mobileSchema.safeParse("123")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues.some(
        (issue) => issue.message === validationMessages.mobile.invalid
      )).toBe(true)
    }
  })
})

describe("optionalUrlSchema", () => {
  it("accepts an empty value", () => {
    const result = optionalUrlSchema.safeParse("")
    expect(result.success).toBe(true)
  })

  it("accepts a valid https URL", () => {
    const result = optionalUrlSchema.safeParse("https://example.com/menu")
    expect(result.success).toBe(true)
  })

  it("rejects a non-https URL", () => {
    const result = optionalUrlSchema.safeParse("http://example.com")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.url.invalid
      )
    }
  })

  it("rejects a malformed URL", () => {
    const result = optionalUrlSchema.safeParse("not-a-url")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.url.invalid
      )
    }
  })
})

describe("otpSchema", () => {
  it("accepts a 6-digit code", () => {
    const result = otpSchema.safeParse("123456")
    expect(result.success).toBe(true)
  })

  it("rejects an incomplete code", () => {
    const result = otpSchema.safeParse("12345")
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.otp.incomplete
      )
    }
  })
})

import { describe, expect, it } from "vitest"

import { validationMessages } from "@/schemas/messages"
import {
  signInCredentialsDefaultValues,
  signInCredentialsSchema,
  signInEmailDefaultValues,
  signInEmailSchema,
  toSignInEmailPayload,
  toSignInPayload,
} from "@/schemas/signIn"

describe("signInCredentialsSchema", () => {
  it("accepts valid credentials", () => {
    const result = signInCredentialsSchema.safeParse({
      ...signInCredentialsDefaultValues,
      email: "owner@restaurant.com",
      password: "secure-pass",
      rememberDevice: true,
    })

    expect(result.success).toBe(true)
  })

  it("rejects an invalid email", () => {
    const result = signInCredentialsSchema.safeParse({
      ...signInCredentialsDefaultValues,
      email: "not-an-email",
      password: "secure-pass",
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(
        result.error.issues.some(
          (issue) => issue.message === validationMessages.email.invalid
        )
      ).toBe(true)
    }
  })

  it("rejects an empty password", () => {
    const result = signInCredentialsSchema.safeParse({
      ...signInCredentialsDefaultValues,
      email: "owner@restaurant.com",
      password: "",
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.password.required
      )
    }
  })

  it("rejects a password shorter than 8 characters", () => {
    const result = signInCredentialsSchema.safeParse({
      ...signInCredentialsDefaultValues,
      email: "owner@restaurant.com",
      password: "short",
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(
        result.error.issues.some(
          (issue) => issue.message === validationMessages.password.minLength
        )
      ).toBe(true)
    }
  })
})

describe("signInEmailSchema", () => {
  it("accepts a valid email", () => {
    const result = signInEmailSchema.safeParse({
      ...signInEmailDefaultValues,
      email: "owner@restaurant.com",
    })

    expect(result.success).toBe(true)
  })

  it("rejects an empty email", () => {
    const result = signInEmailSchema.safeParse({
      ...signInEmailDefaultValues,
      email: "",
    })

    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.email.required
      )
    }
  })
})

describe("toSignInPayload", () => {
  it("maps credentials to the universal-login API DTO shape", () => {
    const payload = toSignInPayload({
      email: "  Owner@Restaurant.com ",
      password: "secure-pass",
      rememberDevice: true,
    })

    expect(payload).toEqual({
      email: "owner@restaurant.com",
      password: "secure-pass",
      rememberDevice: true,
    })
  })
})

describe("toSignInEmailPayload", () => {
  it("maps email-only forms to forgot-password and send-otp payloads", () => {
    const payload = toSignInEmailPayload({
      email: "  Owner@Restaurant.com ",
    })

    expect(payload).toEqual({
      email: "owner@restaurant.com",
    })
  })
})

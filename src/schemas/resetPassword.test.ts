import { describe, expect, it } from "vitest"

import { validationMessages } from "@/schemas/messages"
import {
  resetPasswordDefaultValues,
  resetPasswordFormSchema,
  toResetPasswordPayload,
} from "@/schemas/resetPassword"

describe("resetPasswordFormSchema", () => {
  it("accepts matching passwords", () => {
    const result = resetPasswordFormSchema.safeParse({
      ...resetPasswordDefaultValues,
      newPassword: "Password1",
      confirmPassword: "Password1",
    })
    expect(result.success).toBe(true)
  })

  it("rejects an empty new password", () => {
    const result = resetPasswordFormSchema.safeParse({
      ...resetPasswordDefaultValues,
      newPassword: "",
      confirmPassword: "",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues[0]?.message).toBe(
        validationMessages.password.required
      )
    }
  })

  it("rejects a password shorter than 8 characters", () => {
    const result = resetPasswordFormSchema.safeParse({
      ...resetPasswordDefaultValues,
      newPassword: "short",
      confirmPassword: "short",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues.some(
        (issue) => issue.message === validationMessages.password.minLength
      )).toBe(true)
    }
  })

  it("rejects an 8+ character password without a number or symbol", () => {
    const result = resetPasswordFormSchema.safeParse({
      ...resetPasswordDefaultValues,
      newPassword: "Password",
      confirmPassword: "Password",
    })
    expect(result.success).toBe(false)
    if (!result.success) {
      expect(result.error.issues.some(
        (issue) =>
          issue.message === validationMessages.password.characterRequirement
      )).toBe(true)
    }
  })

  it("rejects mismatched passwords on confirmPassword", () => {
    const result = resetPasswordFormSchema.safeParse({
      ...resetPasswordDefaultValues,
      newPassword: "Password1",
      confirmPassword: "Password2",
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

describe("toResetPasswordPayload", () => {
  it("maps form values to the reset-password API DTO shape", () => {
    const payload = toResetPasswordPayload(
      {
        newPassword: "Password1",
        confirmPassword: "Password1",
      },
      "reset-token-123"
    )

    expect(payload).toEqual({
      token: "reset-token-123",
      newPassword: "Password1",
      confirmPassword: "Password1",
    })
  })
})

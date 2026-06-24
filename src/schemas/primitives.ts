import { z } from "zod"

import { tryNormalizePhoneToE164 } from "@/lib/phoneNumber"
import { validationMessages } from "@/schemas/messages"

export const emailSchema = z
  .string()
  .min(1, validationMessages.email.required)
  .email(validationMessages.email.invalid)

export const passwordSchema = z
  .string()
  .min(1, validationMessages.password.required)
  .min(12, validationMessages.password.minLength)
  .refine(
    (value) => /[0-9]/.test(value) || /[^A-Za-z0-9]/.test(value),
    validationMessages.password.characterRequirement
  )

export const mobileSchema = z
  .string()
  .min(1, validationMessages.mobile.required)
  .superRefine((value, ctx) => {
    if (!tryNormalizePhoneToE164(value)) {
      ctx.addIssue({
        code: "custom",
        message: validationMessages.mobile.invalid,
      })
    }
  })
  .transform((value) => tryNormalizePhoneToE164(value)!)

export const optionalMobileSchema = z
  .string()
  .superRefine((value, ctx) => {
    const trimmed = value.trim()
    if (!trimmed) {
      return
    }

    if (!tryNormalizePhoneToE164(trimmed)) {
      ctx.addIssue({
        code: "custom",
        message: validationMessages.mobile.invalid,
      })
    }
  })
  .transform((value) => {
    const trimmed = value.trim()
    return trimmed ? tryNormalizePhoneToE164(trimmed)! : ""
  })

export const optionalUrlSchema = z.string().superRefine((value, ctx) => {
  const trimmed = value.trim()
  if (!trimmed) {
    return
  }

  try {
    const url = new URL(trimmed)
    if (url.protocol !== "https:") {
      ctx.addIssue({
        code: "custom",
        message: validationMessages.url.invalid,
      })
    }
  } catch {
    ctx.addIssue({
      code: "custom",
      message: validationMessages.url.invalid,
    })
  }
})

export const otpSchema = z
  .string()
  .length(6, validationMessages.otp.incomplete)

export type ConfirmPasswordFields = {
  password: string
  confirmPassword: string
}

export function refineConfirmPassword<
  T extends z.ZodType<ConfirmPasswordFields>,
>(schema: T) {
  return schema.refine(
    (data) => data.password === data.confirmPassword,
    {
      message: validationMessages.password.mismatch,
      path: ["confirmPassword"],
    }
  )
}

export const passwordPairSchema = refineConfirmPassword(
  z.object({
    password: passwordSchema,
    confirmPassword: z.string().min(1, validationMessages.password.required),
  })
)

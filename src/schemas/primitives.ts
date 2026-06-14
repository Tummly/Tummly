import { z } from "zod"

import { validationMessages } from "@/schemas/messages"

export const emailSchema = z
  .string()
  .min(1, validationMessages.email.required)
  .email(validationMessages.email.invalid)

export const passwordSchema = z
  .string()
  .min(1, validationMessages.password.required)
  .min(8, validationMessages.password.minLength)

export const mobileSchema = z
  .string()
  .min(1, validationMessages.mobile.required)
  .regex(/^[0-9+\-\s]{10,15}$/, validationMessages.mobile.invalid)

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

import { z } from "zod"

import { validationMessages } from "@/schemas/messages"
import { emailSchema, otpSchema } from "@/schemas/primitives"

export const signupStartSchema = z.object({
  email: emailSchema,
  termsAccepted: z.boolean().refine((value) => value === true, {
    message: validationMessages.trialRequest.terms.required,
  }),
})

export type SignupStartValues = z.infer<typeof signupStartSchema>

export const signupStartDefaultValues: SignupStartValues = {
  email: "",
  termsAccepted: false,
}

export type SignupStartPayload = {
  email: string
  termsAccepted: true
}

export function toSignupStartPayload(
  values: SignupStartValues
): SignupStartPayload {
  const parsed = signupStartSchema.parse({
    ...values,
    email: values.email.trim().toLowerCase(),
  })

  return {
    email: parsed.email,
    termsAccepted: true,
  }
}

export const signupVerifyOtpSchema = z.object({
  email: emailSchema,
  otpCode: otpSchema,
})

export type SignupVerifyOtpValues = z.infer<typeof signupVerifyOtpSchema>

export type SignupVerifyOtpPayload = {
  email: string
  otpCode: string
}

export function toSignupVerifyOtpPayload(
  values: SignupVerifyOtpValues
): SignupVerifyOtpPayload {
  const parsed = signupVerifyOtpSchema.parse({
    ...values,
    email: values.email.trim().toLowerCase(),
    otpCode: values.otpCode.trim(),
  })

  return {
    email: parsed.email,
    otpCode: parsed.otpCode,
  }
}

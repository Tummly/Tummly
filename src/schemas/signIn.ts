import { z } from "zod"

import { emailSchema } from "@/schemas/primitives"
import { validationMessages } from "@/schemas/messages"
import { getDeviceToken } from "@/pages/utils/authHelpers"

/** Sign-in only checks presence — length/format is validated server-side on login. */
export const signInPasswordSchema = z
  .string()
  .min(1, validationMessages.password.required)

export const signInCredentialsSchema = z.object({
  email: emailSchema,
  password: signInPasswordSchema,
  rememberDevice: z.boolean(),
})

export type SignInCredentialsValues = z.infer<typeof signInCredentialsSchema>

export const signInCredentialsDefaultValues: SignInCredentialsValues = {
  email: "",
  password: "",
  rememberDevice: true,
}

export const signInEmailSchema = z.object({
  email: emailSchema,
})

export type SignInEmailValues = z.infer<typeof signInEmailSchema>

export const signInEmailDefaultValues: SignInEmailValues = {
  email: "",
}

export type SignInPayload = {
  email: string
  password: string
  rememberDevice: boolean
  deviceToken?: string
}

export type SignInEmailPayload = {
  email: string
}

function normalizeEmail(email: string) {
  return email.trim().toLowerCase()
}

export function toSignInPayload(values: SignInCredentialsValues): SignInPayload {
  const parsed = signInCredentialsSchema.parse({
    ...values,
    email: normalizeEmail(values.email),
  })

  return {
    email: parsed.email,
    password: parsed.password,
    rememberDevice: parsed.rememberDevice,
    ...(getDeviceToken() ? { deviceToken: getDeviceToken()! } : {}),
  }
}

export function toSignInEmailPayload(values: SignInEmailValues): SignInEmailPayload {
  const parsed = signInEmailSchema.parse({
    email: normalizeEmail(values.email),
  })

  return {
    email: parsed.email,
  }
}

import { z } from "zod"

import { emailSchema, passwordSchema } from "@/schemas/primitives"

export const signInCredentialsSchema = z.object({
  email: emailSchema,
  password: passwordSchema,
  rememberDevice: z.boolean(),
})

export type SignInCredentialsValues = z.infer<typeof signInCredentialsSchema>

export const signInCredentialsDefaultValues: SignInCredentialsValues = {
  email: "",
  password: "",
  rememberDevice: false,
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

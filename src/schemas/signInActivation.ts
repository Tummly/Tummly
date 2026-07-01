import { z } from "zod"

const ACTIVATION_CODE_PATTERN = /^[23456789A-HJ-KM-NP-Z]{4}-?[23456789A-HJ-KM-NP-Z]{4}$/i

export function normalizeActivationCodeInput(value: string) {
  return value
    .trim()
    .replace(/[^23456789A-HJ-KM-NP-Za-hj-km-np-z-]/g, "")
    .toUpperCase()
}

export const signInActivationCodeSchema = z.object({
  activationCode: z
    .string()
    .trim()
    .min(1, "Activation code is required.")
    .refine(
      (value) =>
        ACTIVATION_CODE_PATTERN.test(normalizeActivationCodeInput(value)),
      "Enter a valid activation code."
    ),
})

export type SignInActivationCodeValues = z.infer<
  typeof signInActivationCodeSchema
>

export const signInActivationCodeDefaultValues: SignInActivationCodeValues = {
  activationCode: "",
}

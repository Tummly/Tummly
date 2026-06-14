import { z } from "zod"

import { validationMessages } from "@/schemas/messages"
import { passwordSchema } from "@/schemas/primitives"

export const resetPasswordFormSchema = z
  .object({
    newPassword: passwordSchema,
    confirmPassword: z.string().min(1, validationMessages.password.required),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: validationMessages.password.mismatch,
    path: ["confirmPassword"],
  })

export type ResetPasswordFormValues = z.infer<typeof resetPasswordFormSchema>

export const resetPasswordDefaultValues: ResetPasswordFormValues = {
  newPassword: "",
  confirmPassword: "",
}

export type ResetPasswordPayload = {
  token: string
  newPassword: string
  confirmPassword: string
}

export function toResetPasswordPayload(
  values: ResetPasswordFormValues,
  token: string
): ResetPasswordPayload {
  const parsed = resetPasswordFormSchema.parse(values)

  return {
    token,
    newPassword: parsed.newPassword,
    confirmPassword: parsed.confirmPassword,
  }
}

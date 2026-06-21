import { z } from "zod"

import { validationMessages } from "@/schemas/messages"

function isValidEmail(value: string) {
  return z.string().email().safeParse(value.trim()).success
}

function isValidPhone(value: string) {
  const trimmed = value.trim()
  const digitsOnly = trimmed.replace(/\D/g, "")

  if (digitsOnly.length < 7) {
    return false
  }

  return /^[0-9+\-\s().]+$/.test(trimmed)
}

export const guestContactSchema = z
  .string()
  .min(1, validationMessages.guestFeedback.contact.required)
  .superRefine((value, ctx) => {
    const trimmed = value.trim()

    if (trimmed.includes("@")) {
      if (!isValidEmail(trimmed)) {
        ctx.addIssue({
          code: "custom",
          message: validationMessages.guestFeedback.contact.invalid,
        })
      }
      return
    }

    if (!isValidPhone(trimmed)) {
      ctx.addIssue({
        code: "custom",
        message: validationMessages.guestFeedback.contact.invalid,
      })
    }
  })

export const guestFeedbackSchema = z.object({
  guestName: z
    .string()
    .min(1, validationMessages.guestFeedback.guestName.required)
    .max(150, "Name must be 150 characters or fewer."),
  guestContact: guestContactSchema,
  comment: z
    .string()
    .min(1, validationMessages.guestFeedback.comment.required)
    .max(1000, "Message must be 1000 characters or fewer."),
})

export type GuestFeedbackFormValues = z.infer<typeof guestFeedbackSchema>

export const guestFeedbackDefaultValues: GuestFeedbackFormValues = {
  guestName: "",
  guestContact: "",
  comment: "",
}

export const guestFeedbackFields = [
  "guestName",
  "guestContact",
  "comment",
] as const satisfies readonly (keyof GuestFeedbackFormValues)[]

export function toGuestFeedbackPayload(values: GuestFeedbackFormValues) {
  const parsed = guestFeedbackSchema.parse(values)

  return {
    guestName: parsed.guestName.trim(),
    guestContact: parsed.guestContact.trim(),
    comment: parsed.comment.trim(),
  }
}
